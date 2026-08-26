using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Common;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.DocumentParsing;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

public interface IDocumentResourceService
{
    Task<DocumentResourceViewModel> UploadAsync(UploadDocumentDto input);

    /// <summary>DS-4/KL-2 - replaces GetByLessonSlug/GetStandalone with the one method every scope
    /// funnels through. scopeType == null means "no query sent" == every scope of this company
    /// (KL-2 - superseded DS-4's old "== company" default). status/q are KL-11..KL-13's optional
    /// content filters, AND'd with the scope filter and with each other.</summary>
    IReadOnlyList<DocumentResourceViewModel> GetByScope(string? scopeType, string? scopeId, string? status = null, string? q = null);

    IReadOnlyList<DocumentResourceViewModel> GetDeleted();

    /// <summary>DI-7 - every chunk the knowledge store received for this document, ordered by
    /// SeqNo. Explicitly authenticated (not just relying on the query filter) because this is the
    /// first endpoint in the system that returns raw uploaded-file content back out.</summary>
    IReadOnlyList<DocumentChunkViewModel> GetChunks(string documentId);

    Task DeleteAsync(string id);
    Task RestoreAsync(string id);

    /// <summary>DS-5/DS-6 - the first call site of KS-4 ("changing scope moves the document, it
    /// does not just update a column"): re-embeds into the new namespace and queues cleanup of the
    /// old one.</summary>
    Task<DocumentResourceViewModel> MoveScopeAsync(string id, MoveDocumentScopeDto input);
}

/// <summary>
/// Upload -> object storage -> [respond] -> a durable BackgroundJob picks up the slow part (text
/// extraction, embedding, Pinecone upsert) after the response is sent - see
/// IBackgroundJobProcessor and SupportRoom.Api's BackgroundJobHostedService, which replaced the
/// in-memory IBackgroundTaskQueue/QueuedHostedService (design.md DI-1/DI-17): that queue lived
/// only in process memory, so a restart mid-index left the document stuck at "pending" forever
/// with no error anywhere. The content-type check (DocumentParserFactory.Create) stays
/// synchronous, before the job is created, so an unsupported file still gets an immediate 400
/// instead of silently landing in the queue and failing later (DI-2).
///
/// Deleting a document only soft-deletes the DB row and enqueues a `vector_delete` job - the file
/// itself is left in object storage (needed for restore) and the row stays reachable via
/// GetDeleted() until an admin restores it (design.md DI-13/DI-15). The vector_delete job's
/// PayloadJson carries the exact VectorId list read from DocumentChunk at delete time (DM-4) -
/// the worker no longer re-downloads and re-parses the file to reconstruct those ids.
/// </summary>
public sealed class DocumentResourceService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IDocumentResourceService> logger,
    IDocumentStorageProvider storageProvider,
    IAuthorizationGuard guard,
    IKnowledgeNamespaceResolver namespaceResolver)
    : ServiceBase<IDocumentResourceService>(unitOfWork, serviceProvider, logger), IDocumentResourceService
{
    private readonly IDocumentResourceRepository _repository = unitOfWork.GetRepository<IDocumentResourceRepository>();
    private readonly ILessonConfigRepository _lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IDocumentChunkRepository _chunkRepository = unitOfWork.GetRepository<IDocumentChunkRepository>();

    public async Task<DocumentResourceViewModel> UploadAsync(UploadDocumentDto input)
    {
        // DS-2 - must run before storageProvider.UploadAsync, not after: a file that lands in
        // object storage but fails validation would have no DB row pointing at it, so nobody
        // could ever delete it again.
        namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId);

        // KL-18 - the exact same byte buffer that is about to go to storage, computed before any
        // DB row exists. Every upload path gets a hash, checkDuplicate or not (KL-21) - only
        // whether we ACT on a match is opt-in.
        var contentHash = ComputeContentHash(input.Content);

        if (input.CheckDuplicate)
        {
            var (byHash, byFileName) = FindDuplicates(contentHash, input.FileName);
            if (byHash.Count > 0 || byFileName.Count > 0)
            {
                // KL-21 - nothing is written when a match is found: no DB row, no storage object,
                // no index job. The caller resubmits with CheckDuplicate=false to upload anyway.
                throw GeneralException.Conflict(
                    "พบเอกสารที่อาจซ้ำกับที่มีอยู่แล้วในคลัง",
                    new DuplicateDocumentDto
                    {
                        DuplicateByHash = byHash.Select(MapDuplicateEntry).ToList(),
                        DuplicateByFileName = byFileName.Select(MapDuplicateEntry).ToList(),
                    });
            }
        }

        var id = IdGenerator.GenerateId("doc");
        var obsKey = $"documents/{id}/{input.FileName}";

        using (var uploadStream = new MemoryStream(input.Content))
        {
            await storageProvider.UploadAsync(obsKey, uploadStream, input.ContentType);
        }

        var entity = new DocumentResource
        {
            Id = id,
            CompanyId = CurrentCompanyId,
            ScopeType = input.ScopeType,
            ScopeId = input.ScopeId,
            FileName = input.FileName,
            ContentType = input.ContentType,
            SizeBytes = input.Content.Length,
            ObsBucket = storageProvider.BucketName,
            ObsKey = obsKey,
            IndexingStatus = DocumentIndexingStatus.Pending,
            IndexedChunkCount = 0,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
            ContentHash = contentHash,
        };
        _repository.Add(entity);
        UnitOfWork.Commit();

        // Cheap content-type dispatch, not parsing - runs now so an unsupported file type still
        // fails the request immediately, exactly like before this change (DI-2).
        try
        {
            DocumentParserFactory.Create(input.ContentType, input.FileName);
        }
        catch (UnsupportedDocumentTypeException ex)
        {
            entity.IndexingStatus = DocumentIndexingStatus.Failed;
            entity.FailureReason = DocumentFailureReason.UnsupportedType;
            _repository.Update(entity);
            UnitOfWork.Commit();
            throw GeneralException.ValidationError(ex.Message);
        }

        EnqueueJob(BackgroundJobType.DocumentIndex, id);
        UnitOfWork.Commit();

        Logger.LogInformation("Document uploaded: {DocumentId}, indexing job queued", id);

        // A document that was just uploaded cannot have a vector_delete job yet - that only
        // exists once something has deleted it, which hasn't happened between the line above and here.
        return BuildViewModel(entity, latestJob: null, hasPendingVectorDelete: false);
    }

    public IReadOnlyList<DocumentResourceViewModel> GetByScope(string? scopeType, string? scopeId, string? status = null, string? q = null)
    {
        var query = ResolveScopeQuery(scopeType, scopeId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.IndexingStatus == status);
        }

        var documents = query.OrderByDescending(x => x.CreateDate).ToList();

        // KL-11/KL-12 - content search happens after materializing the scope+status result: the
        // FileName/DocumentChunk.Text match itself is computed via a couple of targeted queries
        // (FindDocumentIdsMatchingContent), then intersected here rather than folded into one
        // giant query, because chunks live in a different table with no navigation property.
        var normalizedQuery = KnowledgeLibrarySearch.Normalize(q);
        if (normalizedQuery is not null)
        {
            var matchingIds = FindDocumentIdsMatchingContent(normalizedQuery);
            documents = documents.Where(x => matchingIds.Contains(x.Id)).ToList();
        }

        return BuildViewModels(documents);
    }

    private IQueryable<DocumentResource> ResolveScopeQuery(string? scopeType, string? scopeId)
    {
        // KL-2 - no scopeType sent at all now means every scope of this company, replacing DS-4's
        // old "== company" default.
        if (string.IsNullOrEmpty(scopeType))
        {
            return _repository.GetAllInCompany();
        }

        // KL-5 - filtering by a category also has to surface the documents of every lesson that
        // belongs to it (LessonConfig.CategoryId == scopeId), or CS filtering to a category would
        // see it as empty even though lessons under it hold documents. Retrieval (KS-3) is
        // unaffected - this is visibility only.
        if (scopeType == KnowledgeScopeType.Category && !string.IsNullOrEmpty(scopeId))
        {
            var lessonIdsInCategory = _lessonConfigRepository.GetByCategoryId(scopeId).Select(l => l.Id);
            return _repository.FindBy(x =>
                (x.ScopeType == KnowledgeScopeType.Category && x.ScopeId == scopeId)
                || (x.ScopeType == KnowledgeScopeType.Lesson && lessonIdsInCategory.Contains(x.ScopeId)));
        }

        // KL-3 - reads never reject an unrecognized scopeType; the repository's exact-match filter
        // just resolves to an empty list.
        return _repository.GetByScope(scopeType, scopeId);
    }

    /// <summary>KL-11 - a document matches when its FileName matches or any of its
    /// DocumentChunk.Text rows match, de-duplicated to one entry per document
    /// ("DISTINCT ระดับเอกสาร ไม่ใช่ระดับ chunk"). EF.Functions.ILike keeps this parameterized -
    /// never string-built SQL.</summary>
    private HashSet<string> FindDocumentIdsMatchingContent(string normalizedQuery)
    {
        var pattern = $"%{normalizedQuery}%";
        var idsByFileName = _repository.FindBy(x => EF.Functions.ILike(x.FileName, pattern))
            .Select(x => x.Id)
            .ToList();
        var idsByChunkText = _chunkRepository.FindBy(x => EF.Functions.ILike(x.Text, pattern))
            .Select(x => x.DocumentId)
            .Distinct()
            .ToList();
        return idsByFileName.Concat(idsByChunkText).ToHashSet();
    }

    /// <summary>KL-19/KL-20 - both checks are scoped to CurrentCompanyId explicitly in the
    /// predicate, not left to the repository's own query filter alone (same defence-in-depth
    /// reasoning as GetDeleted() - see IDocumentResourceRepository.cs). KL-20's filename match is
    /// deliberately done as an in-memory case-insensitive comparison rather than
    /// EF.Functions.ILike: this keeps the check runnable against the fakes-based unit tests this
    /// project's Application.Tests use everywhere else (EF.Functions.ILike has no LINQ-to-Objects
    /// implementation and throws when evaluated outside a real EF Core provider), and at this
    /// module's stated per-company scale (O-11 - tens to low hundreds of rows) materializing the
    /// company's documents once is not a meaningful cost.</summary>
    private (List<DocumentResource> ByHash, List<DocumentResource> ByFileName) FindDuplicates(string contentHash, string fileName)
    {
        var companyDocuments = _repository.FindBy(x => x.CompanyId == CurrentCompanyId).ToList();

        var byHash = companyDocuments
            .Where(x => x.ContentHash is not null && x.ContentHash == contentHash)
            .ToList();

        var trimmedName = fileName.Trim();
        var byFileName = companyDocuments
            .Where(x => string.Equals(x.FileName.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return (byHash, byFileName);
    }

    private static string ComputeContentHash(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static DuplicateDocumentEntryViewModel MapDuplicateEntry(DocumentResource entity) => new()
    {
        Id = entity.Id,
        FileName = entity.FileName,
        ScopeType = entity.ScopeType,
        ScopeId = entity.ScopeId,
        CreatedAt = entity.CreateDate.ToString("O", CultureInfo.InvariantCulture),
    };

    public IReadOnlyList<DocumentResourceViewModel> GetDeleted()
    {
        var documents = _repository.GetDeleted(CurrentCompanyId).OrderByDescending(x => x.DeletedAt).ToList();
        return BuildViewModels(documents);
    }

    public IReadOnlyList<DocumentChunkViewModel> GetChunks(string documentId)
    {
        guard.EnsureAuthenticated();

        var entity = _repository.Get(documentId) ?? throw GeneralException.NotFound("เอกสาร");
        guard.EnsureCanAccessCompany(entity.CompanyId);

        return _chunkRepository.GetByDocumentId(documentId)
            .ToList()
            .Select(c => new DocumentChunkViewModel
            {
                Id = c.Id,
                ChunkKey = c.ChunkKey,
                SeqNo = c.SeqNo,
                Text = c.Text,
                CharCount = c.CharCount,
                HasSuspectCharacters = c.HasSuspectCharacters,
            })
            .ToList();
    }

    public Task DeleteAsync(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("เอกสาร");

        // A lesson with ContentSourceType=pdf points straight at this row's id - deleting it out
        // from under the lesson breaks GetTeachingContentBySlugAsync entirely (500, not just a
        // missing attachment), so block it here rather than letting that surface as a crash the
        // next time someone opens the room.
        var lessonUsingThisAsPdfSource = _lessonConfigRepository.FindBy(l => l.PdfDocumentResourceId == id).FirstOrDefault();
        if (lessonUsingThisAsPdfSource is not null)
        {
            throw GeneralException.ValidationError(
                $"ลบไม่ได้ - เอกสารนี้ถูกใช้เป็นเนื้อหาสอนหลัก (PDF) ของบทเรียน \"{lessonUsingThisAsPdfSource.Title}\" อยู่ กรุณาเปลี่ยนแหล่งเนื้อหาของบทเรียนนั้นก่อน");
        }

        // DI-13 - read the vector ids this document actually has *before* soft-deleting the chunk
        // rows, group by namespace (normally exactly one - see DocumentChunk.NamespaceKey), and
        // hand each group to its own vector_delete job via PayloadJson. This is what replaced the
        // Phase 3 workaround of re-downloading and re-extracting the file to recompute the same
        // ids: these are the ids that were actually upserted, not ones recomputed after the fact.
        var chunks = _chunkRepository.GetByDocumentId(id).ToList();
        foreach (var group in chunks.GroupBy(c => c.NamespaceKey))
        {
            var payload = new VectorDeleteJobPayload
            {
                NamespaceKey = group.Key,
                VectorIds = group.Select(c => c.VectorId).ToList(),
            };
            EnqueueJob(BackgroundJobType.VectorDelete, id, JsonSerializer.Serialize(payload));
        }

        _chunkRepository.DeleteByDocumentId(id);

        // Soft delete, not storageProvider.DeleteAsync + _repository.Delete(): the file has to
        // stay in object storage for restore (DI-15), and the vector cleanup happens in the
        // background via a vector_delete job (DI-13/DI-16) rather than inline here, so a slow or
        // failing Pinecone call never blocks the delete response.
        entity.IsDelete = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeleteBy = CurrentUserId;
        _repository.Update(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Document soft-deleted: {DocumentId}, vector cleanup job(s) queued", id);
        return Task.CompletedTask;
    }

    public Task RestoreAsync(string id)
    {
        // GetDeleted(CurrentCompanyId) already scopes this to the caller's company, but the
        // explicit guard below is defense in depth against the query filter ever being weakened
        // again the way it was before this fix (IgnoreQueryFilters() dropping CompanyId too).
        var entity = _repository.GetDeleted(CurrentCompanyId).SingleOrDefault(x => x.Id == id) ?? throw GeneralException.NotFound("เอกสาร");
        guard.EnsureCanAccessCompany(entity.CompanyId);

        entity.IsDelete = false;
        entity.DeletedAt = null;
        entity.DeleteBy = null;
        entity.IndexingStatus = DocumentIndexingStatus.Pending;
        entity.FailureReason = null;
        _repository.Update(entity);

        // DI-15: re-index from scratch, spending embedding cost again - the soft-deleted
        // DocumentChunk set from before is not resurrected, a fresh one replaces it once indexed.
        EnqueueJob(BackgroundJobType.DocumentIndex, id);
        UnitOfWork.Commit();

        Logger.LogInformation("Document restored: {DocumentId}, indexing job re-queued", id);
        return Task.CompletedTask;
    }

    public Task<DocumentResourceViewModel> MoveScopeAsync(string id, MoveDocumentScopeDto input)
    {
        // The query filter behind _repository.Get() already excludes soft-deleted rows (see
        // RepositoryBase.Get -> DbSet.Find, which still applies HasQueryFilter), so a
        // soft-deleted document falls straight into NotFound here - DS-7 forbids moving anything
        // out of the recovery bin without restoring it first.
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("เอกสาร");

        // DS-5 - same EnsureValidScope call as DS-3, the first call site of KS-4.
        namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId);

        // DS-7 - moving to the exact same scope is a no-op: nothing is enqueued, nothing is
        // touched, 200 is returned as-is.
        if (entity.ScopeType == input.ScopeType && entity.ScopeId == input.ScopeId)
        {
            return Task.FromResult(BuildViewModels([entity]).Single());
        }

        // DS-6 - group the document's chunks by the namespace they were actually upserted into
        // (not the namespace the document is about to move to) and queue one vector_delete job
        // per group, same payload shape as DI-13's delete path. DS-7: a document that was never
        // successfully indexed has no DocumentChunk rows, so no vector_delete job is created here
        // at all - only the document_index re-queue below applies to it.
        var chunks = _chunkRepository.GetByDocumentId(id).ToList();
        foreach (var group in chunks.GroupBy(c => c.NamespaceKey))
        {
            var payload = new VectorDeleteJobPayload
            {
                NamespaceKey = group.Key,
                VectorIds = group.Select(c => c.VectorId).ToList(),
            };
            EnqueueJob(BackgroundJobType.VectorDelete, id, JsonSerializer.Serialize(payload));
        }

        _chunkRepository.DeleteByDocumentId(id);

        entity.ScopeType = input.ScopeType;
        entity.ScopeId = input.ScopeId;
        entity.IndexingStatus = DocumentIndexingStatus.Pending;
        entity.IndexedChunkCount = 0;
        entity.FailureReason = null;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateDate = DateTime.UtcNow;
        _repository.Update(entity);

        // DS-7 - an in-flight document_index job from before the move is left alone: the worker
        // resolves namespace from the entity at process time, so it lands in the new namespace
        // automatically, and this fresh job replaces its output anyway (DI-8 always replaces the
        // whole chunk set).
        EnqueueJob(BackgroundJobType.DocumentIndex, id);
        UnitOfWork.Commit();

        Logger.LogInformation(
            "Document scope moved: {DocumentId} -> {ScopeType}/{ScopeId}, re-index queued", id, entity.ScopeType, entity.ScopeId);

        return Task.FromResult(BuildViewModels([entity]).Single());
    }

    private void EnqueueJob(string jobType, string targetId, string? payloadJson = null)
    {
        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        jobRepository.Add(new BackgroundJob
        {
            Id = IdGenerator.GenerateId("job"),
            CompanyId = CurrentCompanyId,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
            JobType = jobType,
            TargetId = targetId,
            PayloadJson = payloadJson,
            Status = BackgroundJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = DateTime.UtcNow,
        });
    }

    private IReadOnlyList<DocumentResourceViewModel> BuildViewModels(IReadOnlyList<DocumentResource> entities)
    {
        if (entities.Count == 0)
        {
            return [];
        }

        var ids = entities.Select(e => e.Id).ToList();
        // BackgroundJob has no company query filter (see ApplicationDbContext) - this request
        // already knows CurrentCompanyId, so it must filter explicitly rather than relying on one.
        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        var latestByDocumentId = jobRepository
            .FindBy(j => j.CompanyId == CurrentCompanyId && j.JobType == BackgroundJobType.DocumentIndex && ids.Contains(j.TargetId))
            .ToList()
            .GroupBy(j => j.TargetId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(j => j.CreateDate).First());

        // R-4/DI-16: TargetId on a vector_delete job is the DocumentResource.Id it cleans up
        // (see DeleteAsync). A document can have several of these (one per namespace its chunks
        // spanned) - only the existence of an unfinished one matters here, not which.
        var pendingVectorDeleteIds = jobRepository
            .FindBy(j => j.CompanyId == CurrentCompanyId
                && j.JobType == BackgroundJobType.VectorDelete
                && ids.Contains(j.TargetId)
                && (j.Status == BackgroundJobStatus.Pending || j.Status == BackgroundJobStatus.Running))
            .Select(j => j.TargetId)
            .ToHashSet();

        return entities.Select(e => BuildViewModel(e, latestByDocumentId.GetValueOrDefault(e.Id), pendingVectorDeleteIds.Contains(e.Id))).ToList();
    }

    private static DocumentResourceViewModel BuildViewModel(DocumentResource entity, BackgroundJob? latestJob, bool hasPendingVectorDelete)
    {
        DateTime? willRetryAt = null;
        if (latestJob is not null
            && (latestJob.Status == BackgroundJobStatus.Pending || latestJob.Status == BackgroundJobStatus.Running)
            && latestJob.AttemptCount < BackgroundJobBackoff.MaxAttempts)
        {
            willRetryAt = latestJob.NextAttemptAt;
        }

        return new DocumentResourceViewModel
        {
            Id = entity.Id,
            ScopeType = entity.ScopeType,
            ScopeId = entity.ScopeId,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            IndexingStatus = entity.IndexingStatus,
            IndexedChunkCount = entity.IndexedChunkCount,
            FailureReason = entity.FailureReason,
            CreatedAt = entity.CreateDate.ToString("O", CultureInfo.InvariantCulture),
            WillRetryAt = willRetryAt?.ToString("O", CultureInfo.InvariantCulture),
            HasPendingVectorDelete = hasPendingVectorDelete,
        };
    }
}
