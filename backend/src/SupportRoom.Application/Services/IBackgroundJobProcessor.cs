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
                case BackgroundJobType.LessonPurge:
                    await ProcessLessonPurgeAsync(job);
                    break;
                default:
                    throw new InvalidOperationException($"ยังไม่รองรับ BackgroundJobType \"{job.JobType}\"");
            }

            job.Status = BackgroundJobStatus.Succeeded;
            job.FinishedAt = DateTime.UtcNow;
            jobRepository.Update(job);
            unitOfWork.Commit();
        }
        catch (LessonPurgeDeferredException)
        {
            // R9/LT-12 - ProcessLessonPurgeAsync already committed its own terminal state for this
            // attempt (the active-session deferral: back to Pending, NextAttemptAt pushed an hour,
            // AttemptCount untouched). Must not also mark the job Succeeded, and must not run
            // HandleFailure's retry/backoff bookkeeping on top of that.
        }
        catch (Exception ex)
        {
            HandleFailure(job, ex, jobRepository);
        }
    }

    /// <summary>R9/LT-12 - signals that ProcessLessonPurgeAsync already committed its own terminal
    /// state for this attempt. See the catch clause in ProcessAsync above.</summary>
    private sealed class LessonPurgeDeferredException : Exception;

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

            // EX-6 (second enforcement point) - a page excluded from any PDF lesson that uses
            // this document must not get its document-copy vector re-created by a later re-index
            // (moving scope - DS-5, restoring the document - DI-15, or re-uploading over it).
            // DocumentChunk rows are still written for every page below, untouched either way -
            // only what gets embedded/upserted here is filtered.
            var lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
            var excludedSlideRepository = unitOfWork.GetRepository<ILessonExcludedSlideRepository>();
            var excludedChunkKeys = lessonRepository.GetAll()
                .Where(l => l.PdfDocumentResourceId == documentId)
                .Select(l => l.Id)
                .ToList()
                .SelectMany(lessonId => excludedSlideRepository.GetByLessonId(lessonId).Where(x => !x.IsDelete))
                .Select(x => PdfPageChunkKeys.ToDocumentChunkKey(x.SlideObjectId))
                .OfType<string>()
                .ToHashSet();

            var chunksToEmbed = excludedChunkKeys.Count == 0
                ? chunks
                : chunks.Where(c => !excludedChunkKeys.Contains(c.Metadata!["chunkId"])).ToList();

            int indexedCount;
            try
            {
                indexedCount = await knowledgeIndexingService.EmbedAndUpsertAsync(namespaceKey, chunksToEmbed);
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

        var excludedSlideRepository = unitOfWork.GetRepository<ILessonExcludedSlideRepository>();
        var excludedIds = excludedSlideRepository.GetByLessonId(lessonId)
            .Where(x => !x.IsDelete)
            .Select(x => x.SlideObjectId)
            .ToHashSet();

        var namespaceKey = KnowledgeNamespaces.For(lesson.CompanyId, lesson.Slug);
        var toUpsert = new List<KnowledgeSourceChunk>();
        var toDelete = new List<string>();

        foreach (var slideObjectId in payload.SlideObjectIds)
        {
            if (excludedIds.Contains(slideObjectId))
            {
                // EX-5 - an excluded page's vector must always be removed, regardless of whatever
                // narration text it resolves to. This must be decided before the blank-text check
                // below (and never by whether resolvedById happens to contain the key) - a page
                // with real narration would otherwise get silently re-upserted instead of cut.
                toDelete.Add(slideObjectId);
                continue;
            }

            if (!resolvedById.TryGetValue(slideObjectId, out var slide))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(slide.SpeakerNotes))
            {
                toDelete.Add(slideObjectId);
            }
            else
            {
                toUpsert.Add(new KnowledgeSourceChunk
                {
                    Id = slide.SlideObjectId,
                    Text = slide.SpeakerNotes,
                    Metadata = new Dictionary<string, string>
                    {
                        ["slideObjectId"] = slide.SlideObjectId,
                        ["index"] = slide.Index.ToString(),
                        ["sourceType"] = KnowledgeSourceType.Slide,
                    },
                });
            }
        }

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

    /// <summary>
    /// R9/Module L - the durable purge worker (LT-11..LT-14). Runs 60 days after archive (LT-3),
    /// or immediately once accelerated by a manual permanent-delete (LT-10).
    /// </summary>
    private async Task ProcessLessonPurgeAsync(BackgroundJob job)
    {
        var lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
        // LT-11/LT-23 - job.CompanyId was already resolved into companyContext by ProcessAsync
        // before this dispatched; GetIncludingDeleted re-applies it explicitly regardless.
        var lesson = lessonRepository.GetIncludingDeleted(companyContext.CompanyId!, job.TargetId);

        if (lesson is null || !lesson.IsDelete || !string.Equals(lesson.PurgeJobId, job.Id, StringComparison.Ordinal))
        {
            // LT-11 - stale/missing/restored/generation-mismatched: no-op succeeded. Comparing
            // job id (not a timestamp) is the generation guard - PostgreSQL can truncate
            // timestamp precision, which a timestamp comparison here could silently get wrong.
            logger.LogInformation(
                "Lesson purge job {JobId} no-op: lesson {LessonId} is not in the state this job owns", job.Id, job.TargetId);
            return;
        }

        if (lesson.PurgeStartedAt is null)
        {
            if (HasActiveSession(lesson.Id))
            {
                // LT-12 - defer an hour without claiming and without spending a retry attempt.
                // This commits its own final state for THIS attempt - see LessonPurgeDeferredException.
                var jobRepository = unitOfWork.GetRepository<IBackgroundJobRepository>();
                job.Status = BackgroundJobStatus.Pending;
                job.StartedAt = null;
                job.NextAttemptAt = DateTime.UtcNow.AddHours(LessonTrashPolicy.ActiveSessionDeferralHours);
                jobRepository.Update(job);
                unitOfWork.Commit();
                logger.LogInformation(
                    "Lesson purge job {JobId} deferred {Hours}h: lesson {LessonId} still has an IN_PROGRESS session",
                    job.Id, LessonTrashPolicy.ActiveSessionDeferralHours, lesson.Id);
                throw new LessonPurgeDeferredException();
            }

            // LT-13 - conditional claim: only a fresh claim actually flips PurgeStartedAt at the
            // database level. Losing this race means restore won the same instant - no-op
            // succeeded, same as the LT-11 check above.
            var claimedNow = DateTime.UtcNow;
            if (!lessonRepository.TryClaimPurge(companyContext.CompanyId!, lesson.Id, job.Id, claimedNow))
            {
                logger.LogInformation(
                    "Lesson purge job {JobId} lost the claim race for lesson {LessonId} (restored concurrently)", job.Id, lesson.Id);
                return;
            }
            // TryClaimPurge ran as raw SQL, invisible to EF's change tracker - keep the in-memory
            // copy consistent so the rest of this method (and its logging) sees the real state.
            lesson.PurgeStartedAt = claimedNow;
        }

        await PurgeLessonAsync(lesson);
    }

    /// <summary>LT-12 - true when any LearningSession under this lesson's TrainingLinks is still
    /// IN_PROGRESS, including a stalled one (there is no separate "stalled" status - see
    /// LearningSession.Status - a stalled session is still IN_PROGRESS by definition).</summary>
    private bool HasActiveSession(string lessonId)
    {
        var linkRepository = unitOfWork.GetRepository<ITrainingLinkRepository>();
        var linkIds = linkRepository.GetByLessonId(lessonId).Select(l => l.Id).ToList();
        if (linkIds.Count == 0)
        {
            return false;
        }
        var sessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();
        return sessionRepository.GetByTrainingLinkIds(linkIds).Any(s => s.Status == SessionStatus.InProgress);
    }

    /// <summary>
    /// R9/LT-15..LT-20 - the actual destructive work, once claimed. External deletes happen first
    /// (Pinecone namespace/vectors, then storage bytes) and every one of them is idempotent
    /// (deleting something already gone is a success), so a retried attempt after any external
    /// failure simply repeats whatever did not finish - nothing in the DB has changed yet at that
    /// point. Only once every external delete has succeeded does the final DB transaction run.
    /// </summary>
    private async Task PurgeLessonAsync(LessonConfig lesson)
    {
        var companyId = lesson.CompanyId;

        // ---- LT-15: snapshot every dependency, ids only, read fresh from the DB ---------------
        var linkRepository = unitOfWork.GetRepository<ITrainingLinkRepository>();
        var links = linkRepository.GetByLessonId(lesson.Id).ToList();
        var linkIds = links.Select(l => l.Id).ToList();

        var sessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();
        var sessionIds = linkIds.Count == 0
            ? []
            : sessionRepository.GetByTrainingLinkIds(linkIds).Select(s => s.Id).ToList();

        var sessionQuestionRepository = unitOfWork.GetRepository<ISessionQuestionRepository>();
        var questionIds = sessionIds.Count == 0
            ? []
            : sessionQuestionRepository.GetBySessionIds(sessionIds).Select(q => q.Id).ToList();

        var narrationRepository = unitOfWork.GetRepository<ILessonSlideNarrationRepository>();
        var excludedSlideRepository = unitOfWork.GetRepository<ILessonExcludedSlideRepository>();

        var documentRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
        var documentsById = documentRepository
            .GetByScopeIncludingDeleted(companyId, KnowledgeScopeType.Lesson, lesson.Id)
            .ToList()
            .ToDictionary(d => d.Id);
        // The primary PDF may not be scope=lesson (a company/category-scoped document picked as
        // this lesson's content source) - make sure it is still in scope for the shared-PDF guard
        // and for purge, even when its own ScopeType/ScopeId point elsewhere.
        if (!string.IsNullOrEmpty(lesson.PdfDocumentResourceId) && !documentsById.ContainsKey(lesson.PdfDocumentResourceId))
        {
            var primaryPdf = documentRepository.GetByIdIncludingDeleted(companyId, lesson.PdfDocumentResourceId);
            if (primaryPdf is not null)
            {
                documentsById[primaryPdf.Id] = primaryPdf;
            }
        }

        // Q-L3/LT-18 - a document still referenced by ANOTHER LessonConfig in this company (active
        // or itself still trashed but not yet purged) is preserved in full: resource row, bytes,
        // chunks, and vectors. Only this lesson's own, non-shared attachments are ever purged.
        var lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
        var candidateDocumentIds = documentsById.Keys.ToList();
        var referencedElsewhere = candidateDocumentIds.Count == 0
            ? []
            : lessonRepository.GetAll()
                .Where(l => l.Id != lesson.Id && l.PdfDocumentResourceId != null && candidateDocumentIds.Contains(l.PdfDocumentResourceId))
                .Select(l => l.PdfDocumentResourceId!)
                .Concat(lessonRepository.GetTrash(companyId)
                    .Where(l => l.Id != lesson.Id && l.PdfDocumentResourceId != null && candidateDocumentIds.Contains(l.PdfDocumentResourceId))
                    .Select(l => l.PdfDocumentResourceId!))
                .ToHashSet();

        var documentsToPurge = documentsById.Values.Where(d => !referencedElsewhere.Contains(d.Id)).ToList();
        var preservedCount = documentsById.Count - documentsToPurge.Count;

        var documentChunkRepository = unitOfWork.GetRepository<IDocumentChunkRepository>();
        var chunksByDocumentId = documentsToPurge.ToDictionary(
            d => d.Id,
            d => documentChunkRepository.GetAllByDocumentIdIncludingDeleted(companyId, d.Id).ToList());

        var qnaRepository = unitOfWork.GetRepository<IKnowledgeQnARepository>();
        var lessonQnAs = qnaRepository.GetByScopeIncludingDeleted(companyId, KnowledgeScopeType.Lesson, lesson.Id).ToList();
        var qnaIds = lessonQnAs.Select(q => q.Id).ToList();

        // ---- LT-17/LT-18: external deletes, before anything in the DB changes ----------------
        try
        {
            // The lesson's own namespace first - covers narration/slide vectors and any orphaned
            // legacy-scope vectors ever indexed there (LT-17).
            await knowledgeIndexProvider.DeleteNamespaceAsync(KnowledgeNamespaces.For(companyId, lesson.Slug));

            // Document vectors, grouped by the namespace they were ACTUALLY upserted into
            // (DocumentChunk.NamespaceKey - KS-4 means scope can move after indexing, so this can
            // differ from the document's current ScopeType/ScopeId).
            foreach (var document in documentsToPurge)
            {
                foreach (var group in chunksByDocumentId[document.Id].GroupBy(c => c.NamespaceKey))
                {
                    await knowledgeIndexProvider.DeleteVectorsAsync(group.Key, group.Select(c => c.VectorId).ToList());
                }
            }

            // Q&A vectors, one per row, by the namespace it was actually indexed into (never
            // assumed from ScopeType/ScopeId, which can be stale relative to IndexedNamespaceKey).
            foreach (var qna in lessonQnAs)
            {
                if (!string.IsNullOrEmpty(qna.IndexedNamespaceKey))
                {
                    await knowledgeIndexProvider.DeleteVectorsAsync(qna.IndexedNamespaceKey, [qna.VectorId]);
                }
            }

            // LT-18 - storage bytes, only for documents not shared with another lesson. Both real
            // IDocumentStorageProvider implementations already treat a missing key as a success.
            foreach (var document in documentsToPurge)
            {
                await storageProvider.DeleteAsync(document.ObsKey);
            }
        }
        catch (Exception ex) when (ex is not DocumentIndexingException)
        {
            // LT-14 - retries later with unbounded backoff; nothing in the DB has changed yet, so
            // the retry simply repeats these idempotent calls.
            throw new DocumentIndexingException(DocumentIndexOutcome.IndexFailed, "ลบข้อมูลบทเรียนออกจากคลังความรู้/พื้นที่จัดเก็บไม่สำเร็จ", ex);
        }

        // ---- LT-16/LT-19/LT-20: one DB transaction, only after every external delete succeeded ----
        var reviewExclusionRepository = unitOfWork.GetRepository<ISessionQuestionReviewExclusionRepository>();
        // LT-16 - inserted BEFORE the Q&A/source rows below are deleted, so QQ-1 can always find
        // them regardless of statement order within this same commit.
        reviewExclusionRepository.AddMissingForLesson(companyId, lesson.Id, questionIds, actorUserId: null);

        // LT-20 - batch/snapshot hard-delete, never IKnowledgeQnAService.DeleteAsync in a loop:
        // that path soft-deletes source rows and reopens their questions to the review queue
        // before any exclusion exists, and commits multiple times mid-purge.
        var sourceRepository = unitOfWork.GetRepository<IKnowledgeQnASourceRepository>();
        foreach (var source in sourceRepository.GetByQnAIdsIncludingDeleted(companyId, qnaIds).ToList())
        {
            sourceRepository.Delete(source);
        }
        var conflictRepository = unitOfWork.GetRepository<IKnowledgeQnAConflictRepository>();
        foreach (var conflict in conflictRepository.GetByQnAIdsIncludingDeleted(companyId, qnaIds).ToList())
        {
            conflictRepository.Delete(conflict);
        }
        foreach (var qna in lessonQnAs)
        {
            qnaRepository.Delete(qna);
        }

        foreach (var narration in narrationRepository.GetAllByLessonIdIncludingDeleted(companyId, lesson.Id).ToList())
        {
            narrationRepository.Delete(narration);
        }
        foreach (var excludedSlide in excludedSlideRepository.GetByLessonId(lesson.Id).ToList())
        {
            excludedSlideRepository.Delete(excludedSlide);
        }

        foreach (var document in documentsToPurge)
        {
            foreach (var chunk in chunksByDocumentId[document.Id])
            {
                documentChunkRepository.Delete(chunk);
            }
            documentRepository.Delete(document);
        }

        // TrainingLink (already revoked at archive time), LearningSession, SessionQuestion, the
        // exclusion rows just written above, and BackgroundJob history are all deliberately left
        // untouched (LT-19) - only this lesson row itself is hard-deleted.
        lessonRepository.Delete(lesson);

        unitOfWork.Commit();

        logger.LogInformation(
            "Lesson permanently purged: {LessonId} slug={Slug} documentsDeleted={DocumentsDeleted} documentsPreserved={DocumentsPreserved} qnaDeleted={QnaCount} questionsExcluded={QuestionCount}",
            lesson.Id, lesson.Slug, documentsToPurge.Count, preservedCount, lessonQnAs.Count, questionIds.Count);
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
