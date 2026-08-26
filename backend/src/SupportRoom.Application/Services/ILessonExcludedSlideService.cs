using System.Text.Json;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Application.Services;

public interface ILessonExcludedSlideService
{
    /// <summary>EX-4 - the single toggle endpoint's business logic. Idempotent: setting a page to
    /// the state it already has is a silent no-op, and only an actual state change enqueues the
    /// EX-5/EX-6 background work.</summary>
    Task ToggleAsync(string lessonId, string slideObjectId, bool excluded);
}

/// <summary>
/// R4.7/Module K - toggles one PDF page's exclusion (design.md EX-1..EX-12). Every write here goes
/// through EX-12(ข)'s membership check before it can ever reach a vector id, and EX-8's hard floor
/// before it can ever remove the lesson's last remaining page.
/// </summary>
public sealed class LessonExcludedSlideService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ILessonExcludedSlideService> logger,
    ILessonConfigService lessonConfigService,
    IKnowledgeIndexingService knowledgeIndexingService)
    : ServiceBase<ILessonExcludedSlideService>(unitOfWork, serviceProvider, logger), ILessonExcludedSlideService
{
    private readonly ILessonConfigRepository _lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly ILessonExcludedSlideRepository _excludedSlideRepository = unitOfWork.GetRepository<ILessonExcludedSlideRepository>();
    private readonly IDocumentChunkRepository _documentChunkRepository = unitOfWork.GetRepository<IDocumentChunkRepository>();

    public async Task ToggleAsync(string lessonId, string slideObjectId, bool excluded)
    {
        var lesson = _lessonRepository.Get(lessonId) ?? throw GeneralException.NotFound("บทเรียน");
        LessonSlideNarrationService.EnsurePdfSource(lesson);

        // EX-12(ข) - slideObjectId must be a real page of THIS lesson's own document before it can
        // be used to build any vector id, security gate reason (1)/(2): PreviewPdfAsync is scoped
        // to lesson.PdfDocumentResourceId, which itself only came from a lesson already loaded
        // through the caller's own company-filtered repository above.
        var baseContent = await lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId!);
        if (!baseContent.Slides.Any(s => s.SlideObjectId == slideObjectId))
        {
            throw GeneralException.NotFound("หน้าเอกสาร");
        }

        // P11-01 - reconcile this whole lesson's exclusion rows first, same helper
        // LessonConfigService.ApplyExcludedSlidesAsync uses, so a legacy duplicate for THIS slide
        // (or any other page of this lesson) is already collapsed to one row before the toggle
        // logic below ever runs - without this, GetOne's tie-break could pick a row that the
        // no-op checks and ApplyExclusionState below then leave a live sibling behind.
        var reconciled = LessonExcludedSlideReconciler.ReconcileAndLoad(_excludedSlideRepository, lessonId);
        var existing = reconciled.TryGetValue(slideObjectId, out var reconciledExisting) ? reconciledExisting : null;
        if (excluded && existing is { IsDelete: false })
        {
            return; // already excluded - idempotent no-op
        }
        if (!excluded && existing is null or { IsDelete: true })
        {
            return; // already not excluded - idempotent no-op
        }

        if (excluded)
        {
            // EX-8 - hard floor, no confirm flag: excluding this page must not drop the lesson
            // below one remaining page.
            var currentExcludedCount = _excludedSlideRepository.GetByLessonId(lessonId).Count(x => !x.IsDelete);
            if (baseContent.Slides.Count - (currentExcludedCount + 1) < 1)
            {
                throw GeneralException.ValidationError("บทเรียนต้องเหลืออย่างน้อย 1 หน้า - ตัดหน้าสุดท้ายไม่ได้");
            }
        }

        ApplyExclusionState(lessonId, slideObjectId, existing, excluded);

        // EX-5 - track 1 (pdf-page-N, the narration/teaching vector) always goes through the
        // existing lesson_index job, both directions.
        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        jobRepository.Add(LessonIndexJobFactory.Create(CurrentCompanyId, CurrentUserId, lessonId, [slideObjectId]));

        // EX-6 - track 2 (the document's own copy of this page, {documentId}-page-N). A blank
        // page never got a DocumentChunk row at index time (PdfTextExtractor skips it) - nothing
        // to remove or restore in that case.
        var documentChunk = FindDocumentChunk(lesson.PdfDocumentResourceId!, slideObjectId);
        if (excluded && documentChunk is not null)
        {
            jobRepository.Add(BuildVectorDeleteJob(documentChunk));
        }

        UnitOfWork.Commit();

        if (!excluded && documentChunk is not null)
        {
            // EX-6 - restoring a page's document-copy vector is not enqueued (unlike the delete
            // direction above): a single page is one embedding call, done inline the same
            // best-effort way LessonConfigService.SaveAsync's own re-index calls are - a failure
            // here must not undo the exclusion-state change that already committed.
            try
            {
                await knowledgeIndexingService.EmbedAndUpsertAsync(
                    documentChunk.NamespaceKey,
                    [new KnowledgeSourceChunk { Id = documentChunk.VectorId, Text = documentChunk.Text }]);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "Restoring document-copy vector failed for {DocumentId}/{ChunkKey}; knowledge store left stale",
                    documentChunk.DocumentId, documentChunk.ChunkKey);
            }
        }

        Logger.LogInformation(
            "Lesson slide {Action}: {LessonId}/{SlideObjectId}", excluded ? "excluded" : "restored", lessonId, slideObjectId);
    }

    private void ApplyExclusionState(string lessonId, string slideObjectId, LessonExcludedSlide? existing, bool excluded)
    {
        var now = DateTime.UtcNow;
        if (excluded)
        {
            if (existing is not null)
            {
                // EX-4 - un-delete the existing row rather than adding a second one for the same
                // page ("หน้าละหนึ่งแถว" is a service-layer rule, the index is not unique).
                existing.IsDelete = false;
                existing.DeletedAt = null;
                existing.DeleteBy = null;
                existing.UpdateBy = CurrentUserId;
                existing.UpdateDate = now;
                _excludedSlideRepository.Update(existing);
            }
            else
            {
                _excludedSlideRepository.Add(new LessonExcludedSlide
                {
                    Id = IdGenerator.GenerateId("exsl"),
                    CompanyId = CurrentCompanyId,
                    LessonId = lessonId,
                    SlideObjectId = slideObjectId,
                    CreateBy = CurrentUserId,
                    CreateDate = now,
                });
            }
        }
        else
        {
            existing!.IsDelete = true;
            existing.DeletedAt = now;
            existing.DeleteBy = CurrentUserId;
            _excludedSlideRepository.Update(existing);
        }
    }

    private DocumentChunk? FindDocumentChunk(string documentId, string slideObjectId)
    {
        var chunkKey = PdfPageChunkKeys.ToDocumentChunkKey(slideObjectId);
        return chunkKey is null
            ? null
            : _documentChunkRepository.GetByDocumentId(documentId).FirstOrDefault(c => c.ChunkKey == chunkKey);
    }

    private BackgroundJob BuildVectorDeleteJob(DocumentChunk chunk) => new()
    {
        Id = IdGenerator.GenerateId("job"),
        CompanyId = CurrentCompanyId,
        CreateBy = CurrentUserId,
        CreateDate = DateTime.UtcNow,
        JobType = BackgroundJobType.VectorDelete,
        TargetId = chunk.DocumentId,
        PayloadJson = JsonSerializer.Serialize(new VectorDeleteJobPayload
        {
            Kind = VectorDeleteTargetKind.LessonPage,
            NamespaceKey = chunk.NamespaceKey,
            VectorIds = [chunk.VectorId],
        }),
        Status = BackgroundJobStatus.Pending,
        AttemptCount = 0,
        NextAttemptAt = DateTime.UtcNow,
    };
}
