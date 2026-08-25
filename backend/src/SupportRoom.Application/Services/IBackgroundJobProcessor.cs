using System.Text.Json;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.DocumentParsing;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

/// <summary>Every way a document_index job can end - design.md DI-5. Kept as its own enum (not
/// just DocumentFailureReason) so Success has a place in the same switch as the four failure
/// cases; DocumentIndexingResultMapper is the single place that turns one of these into the pair
/// of columns CS actually sees.</summary>
public enum DocumentIndexOutcome
{
    Success,
    ExtractFailed,
    NoText,
    EmbeddingFailed,
    IndexFailed,
}

/// <summary>DI-5's result -> status mapping, pulled out as a pure function specifically so it can
/// be unit tested without a database, storage, or provider in the loop (design.md R-12 calls this
/// out as one of three spots worth a real test). Every failure case maps to Failed - the four
/// DocumentFailureReason values only tell CS WHY, they are not different statuses.</summary>
public static class DocumentIndexingResultMapper
{
    public static (string IndexingStatus, string? FailureReason) Map(DocumentIndexOutcome outcome) => outcome switch
    {
        DocumentIndexOutcome.Success => (DocumentIndexingStatus.Indexed, null),
        DocumentIndexOutcome.ExtractFailed => (DocumentIndexingStatus.Failed, DocumentFailureReason.ExtractFailed),
        DocumentIndexOutcome.NoText => (DocumentIndexingStatus.Failed, DocumentFailureReason.NoText),
        DocumentIndexOutcome.EmbeddingFailed => (DocumentIndexingStatus.Failed, DocumentFailureReason.EmbeddingFailed),
        DocumentIndexOutcome.IndexFailed => (DocumentIndexingStatus.Failed, DocumentFailureReason.IndexFailed),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };
}

/// <summary>Carries a DocumentIndexOutcome out of the pipeline in ProcessDocumentIndexAsync/
/// ProcessVectorDeleteAsync so the single catch in ProcessAsync can write the right
/// IndexingStatus/FailureReason before handing the failure to the retry/backoff bookkeeping.</summary>
public sealed class DocumentIndexingException(DocumentIndexOutcome outcome, string messageTh, Exception? inner = null)
    : Exception(messageTh, inner)
{
    public DocumentIndexOutcome Outcome { get; } = outcome;
}

/// <summary>DI-9's retry schedule, pulled out as a pure function for the same reason as
/// DocumentIndexingResultMapper - unit-testable without a running worker.</summary>
public static class BackgroundJobBackoff
{
    /// <summary>Step 4 (embedding) spends real money, so retries are capped rather than
    /// unbounded - an unlimited retry on a permanently-broken embed call would burn money
    /// silently forever (design.md DI-9).</summary>
    public const int MaxAttempts = 3;

    public static TimeSpan Calculate(int attemptCount) => attemptCount switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15),
    };
}

public interface IBackgroundJobProcessor
{
    Task ProcessAsync(BackgroundJob job, CancellationToken cancellationToken);
}

/// <summary>
/// Runs one BackgroundJob to completion (or failure) - resolved fresh per job from the worker's
/// own IServiceScope (see SupportRoom.Api.BackgroundJobHostedService), the same shape
/// IndexUploadedDocumentAsync used before this replaced it.
///
/// A successful document_index run replaces the document's whole DocumentChunk set in the same
/// transaction as the status update (DI-8, design.md DM-4) - the ids in those rows are exactly
/// what IDocumentResourceService.DeleteAsync later reads to build a vector_delete job's
/// PayloadJson, so ProcessVectorDeleteAsync never re-downloads or re-parses the file.
/// </summary>
public sealed class BackgroundJobProcessor(
    ICompanyContext companyContext,
    IUnitOfWork unitOfWork,
    IDocumentStorageProvider storageProvider,
    IKnowledgeIndexingService knowledgeIndexingService,
    IKnowledgeIndexProvider knowledgeIndexProvider,
    IKnowledgeNamespaceResolver namespaceResolver,
    ILessonConfigService lessonConfigService,
    ILessonSlideNarrationResolver narrationResolver,
    ILogger<IBackgroundJobProcessor> logger) : IBackgroundJobProcessor
{
    /// <summary>DM-10: LastErrorDetail is for logs/debugging only and must stay bounded.</summary>
    private const int MaxErrorDetailLength = 2000;

    public async Task ProcessAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        // DI-4: first thing, before any other repository call - BackgroundJob has no query
        // filter, so every other company-scoped query in this method depends on this running
        // first. Skipping it means every subsequent query silently matches nothing.
        companyContext.Resolve(job.CompanyId);

        var jobRepository = unitOfWork.GetRepository<IBackgroundJobRepository>();

        try
        {
            switch (job.JobType)
            {
                case BackgroundJobType.DocumentIndex:
                    await ProcessDocumentIndexAsync(job.TargetId);
                    break;
                case BackgroundJobType.VectorDelete:
                    await ProcessVectorDeleteAsync(job.TargetId, job.PayloadJson);
                    break;
                case BackgroundJobType.LessonIndex:
                    await ProcessLessonIndexAsync(job.TargetId, job.PayloadJson);
                    break;
                case BackgroundJobType.QnaIndex:
                    await ProcessQnaIndexAsync(job.TargetId, job.PayloadJson);
                    break;
                default:
                    throw new InvalidOperationException($"ยังไม่รองรับ BackgroundJobType \"{job.JobType}\"");
            }

            job.Status = BackgroundJobStatus.Succeeded;
            job.FinishedAt = DateTime.UtcNow;
            jobRepository.Update(job);
            unitOfWork.Commit();
        }
        catch (Exception ex)
        {
            HandleFailure(job, ex, jobRepository);
        }
    }

    private async Task ProcessDocumentIndexAsync(string documentId)
    {
        var documentRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
        var entity = documentRepository.Get(documentId);
        if (entity is null)
        {
            // Deleted before its turn came up - nothing left to index (same "quietly done" shape
            // the old IndexUploadedDocumentAsync had for this case).
            return;
        }

        try
        {
            var namespaceKey = namespaceResolver.Resolve(entity.CompanyId, entity.ScopeType, entity.ScopeId);

            IDocumentTextExtractor extractor;
            try
            {
                extractor = DocumentParserFactory.Create(entity.ContentType, entity.FileName);
            }
            catch (UnsupportedDocumentTypeException ex)
            {
                // Not expected in practice - UploadAsync already rejects this content type
                // synchronously (DI-2) before a job is ever created - but a worker must not
                // assume that invariant holds forever, so it maps the same way rather than
                // crashing unhandled.
                throw new DocumentIndexingException(DocumentIndexOutcome.ExtractFailed, ex.Message, ex);
            }

            IReadOnlyList<DocumentTextChunk> extracted;
            try
            {
                await using var stream = await storageProvider.DownloadAsync(entity.ObsKey);
                extracted = extractor.Extract(stream);
            }
            catch (Exception ex)
            {
                throw new DocumentIndexingException(DocumentIndexOutcome.ExtractFailed, "แปลงไฟล์เป็นข้อความไม่สำเร็จ", ex);
            }

            var chunks = extracted.Select(c => new KnowledgeSourceChunk
            {
                Id = $"{documentId}-{c.ChunkId}",
                Text = c.Text,
                Metadata = new Dictionary<string, string>
                {
                    ["documentId"] = documentId,
                    ["chunkId"] = c.ChunkId,
                    ["fileName"] = entity.FileName,
                    ["sourceType"] = KnowledgeSourceType.Document,
                },
            }).ToList();

            if (chunks.All(c => string.IsNullOrWhiteSpace(c.Text)))
            {
                // Almost always a scanned PDF with no extractable text at all (design.md DI-5/R6.3).
                throw new DocumentIndexingException(DocumentIndexOutcome.NoText, "แปลงไฟล์ได้ แต่ไม่พบข้อความ");
            }

            int indexedCount;
            try
            {
                indexedCount = await knowledgeIndexingService.EmbedAndUpsertAsync(namespaceKey, chunks);
            }
            catch (KnowledgeEmbeddingFailedException ex)
            {
                throw new DocumentIndexingException(DocumentIndexOutcome.EmbeddingFailed, "แปลงข้อความเป็นเวกเตอร์ไม่สำเร็จ", ex);
            }
            catch (KnowledgeIndexUpsertFailedException ex)
            {
                throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "บันทึกเข้าคลังความรู้ไม่สำเร็จ", ex);
            }

            var (status, failureReason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.Success);
            entity.IndexingStatus = status;
            entity.FailureReason = failureReason;
            entity.IndexedChunkCount = indexedCount;
            documentRepository.Update(entity);

            // DI-8: replace the whole DocumentChunk set for this document in the same
            // transaction as the status update above (both commit together in
            // ProcessAsync's single unitOfWork.Commit()) - never merged row by row, because a
            // ChunkKey present in the old set is not guaranteed to still exist in the new one.
            // Only non-blank chunks are written: a blank chunk was skipped by
            // EmbedAndUpsertAsync and never became a real vector, so it has no VectorId to record.
            var chunkRepository = unitOfWork.GetRepository<IDocumentChunkRepository>();
            chunkRepository.DeleteByDocumentId(documentId);
            var seqNo = 1;
            foreach (var chunk in extracted.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
            {
                var text = StripNulBytes(chunk.Text);
                chunkRepository.Add(new DocumentChunk
                {
                    Id = IdGenerator.GenerateId("chunk"),
                    CompanyId = entity.CompanyId,
                    CreateDate = DateTime.UtcNow,
                    DocumentId = documentId,
                    ChunkKey = chunk.ChunkId,
                    VectorId = $"{documentId}-{chunk.ChunkId}",
                    NamespaceKey = namespaceKey,
                    SeqNo = seqNo++,
                    Text = text,
                    CharCount = text.Length,
                    HasSuspectCharacters = DocumentChunkTextAnalyzer.HasSuspectCharacters(text),
                });
            }

            logger.LogInformation(
                "Document indexed: {DocumentId} chunks={ChunkCount} namespace={Namespace}", documentId, indexedCount, namespaceKey);
        }
        catch (DocumentIndexingException ex)
        {
            var (status, failureReason) = DocumentIndexingResultMapper.Map(ex.Outcome);
            entity.IndexingStatus = status;
            entity.FailureReason = failureReason;
            documentRepository.Update(entity);
            throw;
        }
    }

    /// <summary>DI-13/DI-16/QQ-5 - the DB row is already gone (soft-deleted) by the time this job
    /// runs; this only cleans up the vectors that were left behind, and never blocks the deletion
    /// that already committed.
    ///
    /// For a document (payload.Kind = Document): if it was restored before this ran (DI-15),
    /// GetDeleted() no longer contains it and there is nothing to clean up - its freshly
    /// re-indexed vectors must not be touched, which matters here specifically because a restore
    /// re-runs the same deterministic "{documentId}-{chunkId}" id scheme, so the ids captured in
    /// this job's PayloadJson could otherwise collide with vectors that were just re-created.
    ///
    /// For a Q&A (payload.Kind = Qna): there is no restore path at all (QQ-5's deletion is
    /// permanent), so that check does not apply and must not run against the document repository.
    ///
    /// The id list itself comes from PayloadJson (written by IDocumentResourceService.DeleteAsync
    /// from the DocumentChunk rows that existed at delete time, or by
    /// IKnowledgeQnAService.DeleteAsync with the Q&A's own single VectorId) rather than from
    /// re-downloading and re-extracting anything - this is what replaced the Phase 3 workaround.</summary>
    private async Task ProcessVectorDeleteAsync(string targetId, string? payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson))
        {
            // Nothing was indexed before deletion (e.g. a document that never got past
            // "pending"/"failed") - the caller only enqueues this job when there is something to
            // report, so an empty payload here means there is nothing to clean up either.
            return;
        }

        var payload = JsonSerializer.Deserialize<VectorDeleteJobPayload>(payloadJson)
            ?? throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "ลบข้อมูลออกจากคลังความรู้ไม่สำเร็จ: payload ของงานไม่ถูกต้อง");

        if (payload.Kind == VectorDeleteTargetKind.Document)
        {
            var documentRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
            // ProcessAsync already resolved companyContext to job.CompanyId (DI-4) before
            // dispatching here, so this is the job's own company - not a cross-company read.
            var stillDeleted = documentRepository.GetDeleted(companyContext.CompanyId!).Any(x => x.Id == targetId);
            if (!stillDeleted)
            {
                return;
            }
        }

        try
        {
            await knowledgeIndexProvider.DeleteVectorsAsync(payload.NamespaceKey, payload.VectorIds);
        }
        catch (Exception ex)
        {
            throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "ลบข้อมูลออกจากคลังความรู้ไม่สำเร็จ", ex);
        }

        logger.LogInformation(
            "Vectors deleted for {Kind} {TargetId}: {Count} ids in namespace {Namespace}",
            payload.Kind, targetId, payload.VectorIds.Count, payload.NamespaceKey);
    }

    /// <summary>QQ-6 - re-indexes one Q&A. NeedsReEmbed=false (only the Answer changed) skips the
    /// paid embedding call entirely and updates just the stored text via
    /// IKnowledgeIndexProvider.UpdateMetadataAsync, since KS-5 embeds the Question alone and it did
    /// not change. NeedsReEmbed=true (first index, or the Question changed) goes through the normal
    /// embed-then-upsert path.</summary>
    private async Task ProcessQnaIndexAsync(string qnaId, string? payloadJson)
    {
        var qnaRepository = unitOfWork.GetRepository<IKnowledgeQnARepository>();
        var entity = qnaRepository.Get(qnaId);
        if (entity is null)
        {
            // Deleted before its turn came up - nothing left to index.
            return;
        }

        var payload = string.IsNullOrEmpty(payloadJson)
            ? new QnaIndexJobPayload { NeedsReEmbed = true }
            : JsonSerializer.Deserialize<QnaIndexJobPayload>(payloadJson) ?? new QnaIndexJobPayload { NeedsReEmbed = true };

        try
        {
            var namespaceKey = namespaceResolver.Resolve(entity.CompanyId, entity.ScopeType, entity.ScopeId);
            var text = $"ถาม: {entity.Question}\nตอบ: {entity.Answer}";
            var metadata = new Dictionary<string, string>
            {
                ["qnaId"] = entity.Id,
                ["sourceType"] = KnowledgeSourceType.Qna,
            };

            if (payload.NeedsReEmbed)
            {
                var chunk = new KnowledgeSourceChunk { Id = entity.VectorId, Text = text, EmbedText = entity.Question, Metadata = metadata };
                try
                {
                    await knowledgeIndexingService.EmbedAndUpsertAsync(namespaceKey, [chunk]);
                }
                catch (KnowledgeEmbeddingFailedException ex)
                {
                    throw new DocumentIndexingException(DocumentIndexOutcome.EmbeddingFailed, "แปลงคำถามเป็นเวกเตอร์ไม่สำเร็จ", ex);
                }
                catch (KnowledgeIndexUpsertFailedException ex)
                {
                    throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "บันทึก Q&A เข้าคลังความรู้ไม่สำเร็จ", ex);
                }
            }
            else
            {
                try
                {
                    await knowledgeIndexProvider.UpdateMetadataAsync(namespaceKey, entity.VectorId, text, metadata);
                }
                catch (Exception ex)
                {
                    throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "บันทึก Q&A เข้าคลังความรู้ไม่สำเร็จ", ex);
                }
            }

            var (status, failureReason) = DocumentIndexingResultMapper.Map(DocumentIndexOutcome.Success);
            entity.IndexingStatus = status;
            entity.FailureReason = failureReason;
            entity.IndexedNamespaceKey = namespaceKey;
            qnaRepository.Update(entity);

            logger.LogInformation("Q&A indexed: {QnAId} namespace={Namespace} reEmbedded={ReEmbedded}", qnaId, namespaceKey, payload.NeedsReEmbed);
        }
        catch (DocumentIndexingException ex)
        {
            var (status, failureReason) = DocumentIndexingResultMapper.Map(ex.Outcome);
            entity.IndexingStatus = status;
            entity.FailureReason = failureReason;
            qnaRepository.Update(entity);
            throw;
        }
    }

    /// <summary>NR-6 - re-indexes only the pages named in PayloadJson.SlideObjectIds, never the
    /// whole deck. Reuses ILessonSlideNarrationResolver (NR-1) so the text embedded here is
    /// exactly what GetTeachingContentBySlugAsync would resolve for the same page right now -
    /// PdfSlidesRenderer's chunk id for a lesson is already the SlideObjectId itself, so no id
    /// mapping is needed. A page that resolves to blank text (its override was deleted and the
    /// extractor also has nothing for that page) gets its vector deleted instead of upserted -
    /// EmbedAndUpsertAsync silently skips blank chunks, which would otherwise leave a stale
    /// vector from a previous non-blank version behind.</summary>
    private async Task ProcessLessonIndexAsync(string lessonId, string? payloadJson)
    {
        var lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
        var lesson = lessonRepository.Get(lessonId);
        if (lesson is null || lesson.ContentSourceType != LessonContentSourceType.Pdf || string.IsNullOrEmpty(lesson.PdfDocumentResourceId))
        {
            // Not expected in practice - only the PDF narration save/delete path enqueues this
            // job type (NR-6) - but a worker must not assume that invariant holds forever.
            return;
        }

        var payload = string.IsNullOrEmpty(payloadJson)
            ? null
            : JsonSerializer.Deserialize<LessonIndexJobPayload>(payloadJson);
        if (payload is null || payload.SlideObjectIds.Count == 0)
        {
            return;
        }

        var baseContent = await lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId);
        var resolvedSlides = await narrationResolver.ResolveAsync(lessonId, baseContent.Slides);
        var resolvedById = resolvedSlides.ToDictionary(s => s.SlideObjectId);

        var namespaceKey = KnowledgeNamespaces.For(lesson.CompanyId, lesson.Slug);
        var chunks = payload.SlideObjectIds
            .Where(resolvedById.ContainsKey)
            .Select(id => resolvedById[id])
            .Select(slide => new KnowledgeSourceChunk
            {
                Id = slide.SlideObjectId,
                Text = slide.SpeakerNotes,
                Metadata = new Dictionary<string, string>
                {
                    ["slideObjectId"] = slide.SlideObjectId,
                    ["index"] = slide.Index.ToString(),
                    ["sourceType"] = KnowledgeSourceType.Slide,
                },
            })
            .ToList();

        var toUpsert = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();
        var toDelete = chunks.Where(c => string.IsNullOrWhiteSpace(c.Text)).Select(c => c.Id).ToList();

        try
        {
            if (toDelete.Count > 0)
            {
                await knowledgeIndexProvider.DeleteVectorsAsync(namespaceKey, toDelete);
            }
            if (toUpsert.Count > 0)
            {
                await knowledgeIndexingService.EmbedAndUpsertAsync(namespaceKey, toUpsert);
            }
        }
        catch (KnowledgeEmbeddingFailedException ex)
        {
            throw new DocumentIndexingException(DocumentIndexOutcome.EmbeddingFailed, "แปลงบทพูดเป็นเวกเตอร์ไม่สำเร็จ", ex);
        }
        catch (KnowledgeIndexUpsertFailedException ex)
        {
            throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "บันทึกบทพูดเข้าคลังความรู้ไม่สำเร็จ", ex);
        }
        catch (Exception ex) when (ex is not DocumentIndexingException)
        {
            throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "ลบบทพูดเก่าออกจากคลังความรู้ไม่สำเร็จ", ex);
        }

        logger.LogInformation(
            "Lesson slides re-indexed: {LessonId} upserted={UpsertCount} deleted={DeleteCount} namespace={Namespace}",
            lessonId, toUpsert.Count, toDelete.Count, namespaceKey);
    }

    private void HandleFailure(BackgroundJob job, Exception ex, IBackgroundJobRepository jobRepository)
    {
        logger.LogWarning(ex, "Background job {JobId} ({JobType}) failed on attempt {Attempt}", job.Id, job.JobType, job.AttemptCount + 1);

        job.AttemptCount++;
        job.LastErrorCode = (ex as DocumentIndexingException)?.Outcome is { } outcome
            ? DocumentIndexingResultMapper.Map(outcome).FailureReason
            : null;
        job.LastErrorDetail = Truncate(ex.ToString(), MaxErrorDetailLength);
        job.FinishedAt = DateTime.UtcNow;

        if (job.AttemptCount >= BackgroundJobBackoff.MaxAttempts)
        {
            job.Status = BackgroundJobStatus.Failed;
        }
        else
        {
            job.Status = BackgroundJobStatus.Pending;
            job.StartedAt = null;
            job.NextAttemptAt = DateTime.UtcNow.Add(BackgroundJobBackoff.Calculate(job.AttemptCount));
        }

        jobRepository.Update(job);
        unitOfWork.Commit();
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>PostgreSQL text columns reject any NUL byte outright (22021), and PDF-extracted
    /// chunk text can carry NUL-byte artifacts from the source document's binary content. Strip
    /// only the NUL byte here - unlike ILessonSlideNarrationService.SanitizeNarrationText, other
    /// control characters must stay untouched because DocumentChunkTextAnalyzer.HasSuspectCharacters
    /// relies on them to flag suspect content for a human to review, not to be silently rewritten.</summary>
    private static string StripNulBytes(string text)
        => text.IndexOf('\0') < 0 ? text : text.Replace("\0", "");
}
