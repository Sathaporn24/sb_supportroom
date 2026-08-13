using SupportRoom.Domain.Common;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
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
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

public interface IDocumentResourceService
{
    Task<DocumentResourceViewModel> UploadAsync(UploadDocumentDto input);
    IReadOnlyList<DocumentResourceViewModel> GetByLessonSlug(string lessonSlug);
    IReadOnlyList<DocumentResourceViewModel> GetStandalone();
    Task DeleteAsync(string id);
}

/// <summary>
/// Upload -> object storage -> [respond] -> parse -> embed/index. Only the storage write and the
/// "Pending" DB row are in the request itself - the slow part (text extraction, embedding,
/// Pinecone upsert) is enqueued onto IBackgroundTaskQueue and runs after the response is sent
/// (drained by SupportRoom.Api's QueuedHostedService), since that used to make every upload wait
/// on N sequential Gemini embed calls before the caller saw anything back. The content-type check
/// (DocumentParserFactory.Create) stays synchronous, before enqueuing, so an unsupported file
/// still gets an immediate 400 instead of silently landing in the queue and failing later.
/// A document attached to a lesson (LessonSlug given) is indexed into that lesson's existing
/// Pinecone namespace (lessonSlug); a standalone document goes into "kb-global". Both are
/// retrievable - RagVoiceQuestionProvider queries the lesson namespace AND kb-global on every
/// question and merges the results, so a standalone document answers questions in any lesson.
/// </summary>
public sealed class DocumentResourceService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IDocumentResourceService> logger,
    IDocumentStorageProvider storageProvider,
    IKnowledgeIndexingService knowledgeIndexingService,
    IBackgroundTaskQueue taskQueue)
    : ServiceBase<IDocumentResourceService>(unitOfWork, serviceProvider, logger), IDocumentResourceService
{
    private readonly IDocumentResourceRepository _repository = unitOfWork.GetRepository<IDocumentResourceRepository>();
    private readonly ILessonConfigRepository _lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();

    public async Task<DocumentResourceViewModel> UploadAsync(UploadDocumentDto input)
    {
        string? lessonId = null;
        var namespaceKey = KnowledgeNamespaces.ForGlobal(CurrentCompanyId);
        if (!string.IsNullOrEmpty(input.LessonSlug))
        {
            var lesson = _lessonConfigRepository.GetBySlug(input.LessonSlug) ?? throw GeneralException.NotFound("บทเรียน");
            lessonId = lesson.Id;
            namespaceKey = KnowledgeNamespaces.For(CurrentCompanyId, lesson.Slug);
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
            LessonId = lessonId,
            FileName = input.FileName,
            ContentType = input.ContentType,
            SizeBytes = input.Content.Length,
            ObsBucket = storageProvider.BucketName,
            ObsKey = obsKey,
            IndexingStatus = DocumentIndexingStatus.Pending,
            IndexedChunkCount = 0,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
        };
        _repository.Add(entity);
        UnitOfWork.Commit();

        // Cheap content-type dispatch, not parsing - runs now so an unsupported file type still
        // fails the request immediately, exactly like before this change.
        IDocumentTextExtractor extractor;
        try
        {
            extractor = DocumentParserFactory.Create(input.ContentType, input.FileName);
        }
        catch (UnsupportedDocumentTypeException ex)
        {
            entity.IndexingStatus = DocumentIndexingStatus.Failed;
            _repository.Update(entity);
            UnitOfWork.Commit();
            throw GeneralException.ValidationError(ex.Message);
        }

        Logger.LogInformation("Document uploaded: {DocumentId} namespace={Namespace}, indexing queued", id, namespaceKey);

        taskQueue.Enqueue((sp, _) => IndexUploadedDocumentAsync(sp, CurrentCompanyId, id, input.Content, input.FileName, namespaceKey, extractor));

        return entity.Adapt<DocumentResourceViewModel>();
    }

    /// <summary>
    /// Runs on QueuedHostedService's background thread, well after UploadAsync's own request
    /// scope is disposed - every Scoped dependency (UnitOfWork, repository, indexing service) is
    /// resolved fresh from the work item's own scope (sp), never reused from the request that
    /// enqueued this. extractor/content/fileName/namespaceKey are plain captured values, safe to
    /// use as-is (DocumentParserFactory's extractors carry no injected dependencies).
    ///
    /// companyId has to be carried across explicitly and re-resolved into this scope's
    /// ICompanyContext: the new scope starts unresolved, and an unresolved context makes every
    /// company query filter match nothing - the Get below would return null and this method would
    /// quietly decide the document had been deleted, leaving it stuck at "pending" forever with
    /// no error anywhere.
    /// </summary>
    private static async Task IndexUploadedDocumentAsync(
        IServiceProvider sp, string companyId, string documentId, byte[] content, string fileName, string namespaceKey, IDocumentTextExtractor extractor)
    {
        sp.GetRequiredService<ICompanyContext>().Resolve(companyId);

        var uow = sp.GetRequiredService<IUnitOfWork>();
        var repository = uow.GetRepository<IDocumentResourceRepository>();
        var knowledgeIndexingService = sp.GetRequiredService<IKnowledgeIndexingService>();
        var logger = sp.GetRequiredService<ILogger<IDocumentResourceService>>();

        var entity = repository.Get(documentId);
        if (entity is null)
        {
            // Deleted before its indexing turn came up - nothing left to update.
            return;
        }

        try
        {
            using var parseStream = new MemoryStream(content);
            var extracted = extractor.Extract(parseStream);

            var chunks = extracted.Select(c => new KnowledgeSourceChunk
            {
                Id = $"{documentId}-{c.ChunkId}",
                Text = c.Text,
                Metadata = new Dictionary<string, string> { ["documentId"] = documentId, ["chunkId"] = c.ChunkId, ["fileName"] = fileName },
            }).ToList();

            var indexedCount = await knowledgeIndexingService.IndexChunksAsync(namespaceKey, chunks);
            entity.IndexingStatus = indexedCount > 0 ? DocumentIndexingStatus.Indexed : DocumentIndexingStatus.Failed;
            entity.IndexedChunkCount = indexedCount;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse/index document {DocumentId}", documentId);
            entity.IndexingStatus = DocumentIndexingStatus.Failed;
        }

        repository.Update(entity);
        uow.Commit();

        logger.LogInformation(
            "Document indexed in background: {DocumentId} status={Status} chunks={ChunkCount} namespace={Namespace}",
            documentId, entity.IndexingStatus, entity.IndexedChunkCount, namespaceKey);
    }

    public IReadOnlyList<DocumentResourceViewModel> GetByLessonSlug(string lessonSlug)
    {
        var lesson = _lessonConfigRepository.GetBySlug(lessonSlug) ?? throw GeneralException.NotFound("บทเรียน");
        return _repository.GetByLessonId(lesson.Id).OrderByDescending(x => x.CreateDate).ToList().Adapt<List<DocumentResourceViewModel>>();
    }

    public IReadOnlyList<DocumentResourceViewModel> GetStandalone()
        => _repository.GetStandalone().OrderByDescending(x => x.CreateDate).ToList().Adapt<List<DocumentResourceViewModel>>();

    public async Task DeleteAsync(string id)
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

        await storageProvider.DeleteAsync(entity.ObsKey);
        _repository.Delete(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Document deleted: {DocumentId}", id);
    }
}
