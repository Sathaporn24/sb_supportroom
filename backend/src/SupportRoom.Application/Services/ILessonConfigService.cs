using SupportRoom.Providers.Knowledge;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

public sealed class LessonTeachingContentViewModel
{
    public required LessonConfigViewModel Lesson { get; init; }
    public required string EmbedUrl { get; init; }
    public required IReadOnlyList<TeachingSlideViewModel> Slides { get; init; }
}

public sealed class LearnerLessonConfigViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ContentSourceType { get; init; }
    public required int IntroWaitMs { get; init; }
    public required int BreathPauseMs { get; init; }
    public required int FinalQuestionWaitMs { get; init; }
}

public sealed class LearnerLessonTeachingContentViewModel
{
    public required LearnerLessonConfigViewModel Lesson { get; init; }
    public required string EmbedUrl { get; init; }
    public required IReadOnlyList<TeachingSlideViewModel> Slides { get; init; }
}

/// <summary>NR-10 - same shape as LessonNarrationSlideViewModel minus IsOverridden, which has no
/// meaning yet: nothing has been persisted for a file that only exists in a preview session.</summary>
public sealed class PdfPreviewSlideViewModel
{
    public required string SlideObjectId { get; init; }
    public required int Index { get; init; }
    public required string NarrationText { get; init; }
}

/// <summary>NR-10/NR-5 - response of POST /api/lessons/pdf-preview/session.</summary>
public sealed class PdfPreviewSessionViewModel
{
    public required string PreviewId { get; init; }
    public required string Title { get; init; }
    public required int PageCount { get; init; }
    public required bool IsLikelyScanned { get; init; }
    public required IReadOnlyList<PdfPreviewSlideViewModel> Slides { get; init; }
}

public interface ILessonConfigService
{
    IReadOnlyList<LessonConfigViewModel> GetAll();
    LessonConfigViewModel GetBySlug(string slug);

    /// <summary>R9/LT-6 - the PDF-page image endpoint's lesson lookup. Must still find a trashed
    /// lesson (unlike GetBySlug/GetAll) for the same reason GetTeachingContentByLinkAsync does -
    /// the caller (LessonController.GetPdfPage) has already passed
    /// ITrainingLinkService.GetEntityByTokenForContentAccess before calling this.</summary>
    LessonConfigViewModel GetByIdIncludingDeleted(string id);

    /// <summary>Upsert by slug - mirrors lessons/route.ts's POST (re-resolves presentationId server-side on every save).</summary>
    Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input);
    Task<LessonConfigViewModel> MoveCategoryAsync(string id, string categoryId);
    Task<LessonTeachingContentViewModel> GetTeachingContentBySlugAsync(string slug);

    /// <summary>R9/LT-5/LT-6 - the trash-aware sibling of GetTeachingContentBySlugAsync, for
    /// recipient-side callers (learner content, question-answering) that have already resolved a
    /// legitimate access grant via ITrainingLinkService.GetEntityByTokenForContentAccess. Loads by
    /// id via GetIncludingDeleted rather than by slug, since the normal query filter behind
    /// GetBySlug hides a trashed lesson entirely.</summary>
    Task<LessonTeachingContentViewModel> GetTeachingContentByIdIncludingDeletedAsync(string lessonId);

    /// <summary>Learner-side variant. Adds the link token to any PDF page URLs after the link has
    /// resolved company context, so later anonymous image requests can repeat the same safe lookup.
    ///
    /// R9/LT-5/LT-6 - learnerKey is required so a revoked link's content is reachable only by a
    /// learner whose (token, learnerKey) is bound to that link's own IN_PROGRESS session - see
    /// ITrainingLinkService.GetEntityByTokenForContentAccess.</summary>
    Task<LearnerLessonTeachingContentViewModel> GetTeachingContentByLinkAsync(string token, string? learnerKey);

    /// <summary>Preview a PDF already uploaded via /api/documents, before saving the lesson -
    /// mirrors POST /api/slides/resolve + GET /api/slides/content collapsed into one call, since
    /// the file is already stored (no separate "resolve a URL" step needed for an upload).</summary>
    Task<SlidesLessonContent> PreviewPdfAsync(string documentId);

    /// <summary>1-based pageNumber. Rendered on demand from the stored PDF bytes and cached
    /// briefly in memory (a document's bytes never change for a given id) - still resolved live
    /// on a cache miss/expiry, never persisted as a durable copy, same precedent as everything
    /// else here.</summary>
    Task<byte[]> RenderPdfPageAsync(string documentId, int pageNumber);

    /// <summary>NR-10 - the only way to get draft text and page images from a file that has not
    /// been persisted anywhere yet (no DocumentResource, no BackgroundJob). Parses fileStream
    /// entirely in memory and stashes its bytes (plus the caller's CompanyId, for NR-11) under a
    /// fresh previewId - nothing is written to PostgreSQL, object storage, or Pinecone here.</summary>
    Task<PdfPreviewSessionViewModel> CreatePdfPreviewSessionAsync(Stream fileStream, string fileName);

    /// <summary>NR-10/NR-11 - 1-based pageNumber. Throws the exact same GeneralException.NotFound
    /// whether previewId never existed, already expired, or belongs to another company - see
    /// NR-11 for why those three cases must be indistinguishable to the caller.</summary>
    Task<byte[]> RenderPdfPreviewPageAsync(string previewId, int pageNumber);

    /// <summary>R9/LT-1..LT-3 - moves an active lesson to the trash: one transaction that creates
    /// the lesson_purge job (60 days out), marks the lesson trashed, and revokes every one of its
    /// TrainingLinks. owner/admin only (LT-2) - cs gets 403. Idempotent: calling this again on an
    /// already-trashed lesson naturally 404s (the active-only query filter hides it), never
    /// creates a second job.</summary>
    Task<LessonConfigViewModel> ArchiveAsync(string id);

    /// <summary>R9/LT-1/LT-4/LT-21 - restores a trashed lesson back to active and cancels its
    /// pending purge job, conditionally: only while PurgeStartedAt is still null. Never re-indexes
    /// (archive never touched vectors/bytes) and never restores TrainingLinks (LT-4 - a new link
    /// must be issued). Throws Conflict if the worker already started purging.</summary>
    Task<LessonConfigViewModel> RestoreAsync(string id);

    /// <summary>R9/LT-7/LT-9 - every trashed lesson of this company with its countdown/urgency
    /// computed at read time.</summary>
    IReadOnlyList<LessonTrashItemViewModel> GetTrash();

    /// <summary>R9/LT-2/LT-10 - owner-only manual permanent delete. confirmationTitle must match
    /// the trashed lesson's real title (server-trimmed, ordinal-exact). On success, accelerates
    /// the lesson's existing purge job to run immediately rather than deleting inline or creating
    /// a second job - the caller gets 202, not a completed deletion.</summary>
    Task RequestPermanentDeleteAsync(string id, string confirmationTitle);
}

public sealed class LessonConfigService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ILessonConfigService> logger,
    ISlidesProvider slidesProvider,
    IKnowledgeIndexingService knowledgeIndexingService,
    IDocumentStorageProvider documentStorageProvider,
    IMemoryCache memoryCache,
    ILessonSlideNarrationResolver narrationResolver,
    IAuthorizationGuard guard,
    ICurrentUser currentUser)
    : ServiceBase<ILessonConfigService>(unitOfWork, serviceProvider, logger), ILessonConfigService
{
    private readonly ILessonConfigRepository _repository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IDocumentResourceRepository _documentResourceRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
    private readonly IKnowledgeCategoryRepository _knowledgeCategoryRepository = unitOfWork.GetRepository<IKnowledgeCategoryRepository>();
    private readonly ILessonSlideNarrationRepository _narrationRepository = unitOfWork.GetRepository<ILessonSlideNarrationRepository>();
    private readonly ILessonExcludedSlideRepository _excludedSlideRepository = unitOfWork.GetRepository<ILessonExcludedSlideRepository>();
    private readonly ICompanyRepository _companyRepository = unitOfWork.GetRepository<ICompanyRepository>();

    public IReadOnlyList<LessonConfigViewModel> GetAll()
        => _repository.GetAll().ToList().Adapt<List<LessonConfigViewModel>>();

    public LessonConfigViewModel GetBySlug(string slug)
    {
        var entity = _repository.GetBySlug(slug) ?? throw GeneralException.NotFound("บทเรียน");
        return entity.Adapt<LessonConfigViewModel>();
    }

    public LessonConfigViewModel GetByIdIncludingDeleted(string id)
    {
        var entity = _repository.GetIncludingDeleted(CurrentCompanyId, id) ?? throw GeneralException.NotFound("บทเรียน");
        return entity.Adapt<LessonConfigViewModel>();
    }

    public async Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input)
    {
        ValidateSlug(input.Slug);
        ValidateCategory(input.CategoryId);
        if (!LessonContentSourceType.Allowed.Contains(input.ContentSourceType))
        {
            throw GeneralException.ValidationError($"contentSourceType ต้องเป็น {string.Join(" หรือ ", LessonContentSourceType.Allowed)}");
        }

        var existing = _repository.GetBySlug(input.Slug);

        // Re-resolve presentationId from the source URL server-side so saving never keeps a
        // stale/mismatched id. Sync failures here don't block saving - CS uses the dedicated
        // "Validate/Sync" button (POST /api/slides/resolve) to see the real error. No-ops for a
        // PDF-sourced lesson since SlidesSourceUrl is empty there.
        var presentationId = existing?.PresentationId;
        if (!string.IsNullOrEmpty(input.SlidesSourceUrl))
        {
            try
            {
                var resolved = await slidesProvider.ResolvePresentationAsync(new ResolvePresentationInput
                {
                    SlidesSourceUrl = input.SlidesSourceUrl,
                    SlidesEmbedUrl = input.SlidesEmbedUrl,
                });
                presentationId = resolved.PresentationId;
            }
            catch (Exception ex)
            {
                // Non-blocking: keep the previous presentationId so the save still commits. But log
                // it - a silently-swallowed resolve failure here used to look like "saving randomly
                // doesn't update the slides" with zero trace in the logs.
                Logger.LogWarning(ex, "Slides resolve failed on save for lesson {Slug}; keeping previous presentationId", input.Slug);
            }
        }
        if (input.ContentSourceType == LessonContentSourceType.Pdf)
        {
            // A lesson that switched from Google Slides to PDF shouldn't keep a stale
            // presentationId around - GetTeachingContentBySlugAsync branches on
            // ContentSourceType anyway, but this avoids a dangling, unused reference.
            presentationId = null;
        }

        var now = DateTime.UtcNow;
        var slideConfigs = input.SlideConfigs.Adapt<List<Domain.Entities.SlideConfig>>();

        var isNew = existing is null;
        var previousPdfDocumentResourceId = existing?.PdfDocumentResourceId;
        LessonConfig entity;
        if (existing is null)
        {
            entity = new LessonConfig
            {
                Id = IdGenerator.GenerateId("lesson"),
                CompanyId = CurrentCompanyId,
                Slug = input.Slug,
                CategoryId = input.CategoryId,
                Title = input.Title,
                Description = input.Description,
                SlidesSourceUrl = input.SlidesSourceUrl,
                PresentationId = presentationId,
                SlidesEmbedUrl = input.SlidesEmbedUrl,
                ContentSourceType = input.ContentSourceType,
                PdfDocumentResourceId = input.PdfDocumentResourceId,
                SlideConfigs = slideConfigs,
                IsActive = input.IsActive,
                CreateBy = CurrentUserId,
                CreateDate = now,
                UpdateBy = CurrentUserId,
                UpdateDate = now,
            };
            _repository.Add(entity);
        }
        else
        {
            // Mutate the already-tracked instance in place - constructing a new object with the
            // same Id and calling Update() on it conflicts with EF Core's change tracker, which
            // is still tracking `existing` from the GetBySlug() lookup above.
            existing.Title = input.Title;
            existing.CategoryId = input.CategoryId;
            existing.Description = input.Description;
            existing.SlidesSourceUrl = input.SlidesSourceUrl;
            existing.PresentationId = presentationId;
            existing.SlidesEmbedUrl = input.SlidesEmbedUrl;
            existing.ContentSourceType = input.ContentSourceType;
            existing.PdfDocumentResourceId = input.PdfDocumentResourceId;
            existing.SlideConfigs = slideConfigs;
            existing.IsActive = input.IsActive;
            existing.UpdateBy = CurrentUserId;
            existing.UpdateDate = now;
            _repository.Update(existing);
            entity = existing;
        }

        // NR-3 - a PDF re-upload (or switching away from PDF) invalidates every CS-authored
        // narration override in one shot: pdf-page-N is a raw page index, so a different file
        // silently shifts every later page onto the wrong narration with no error (NR-4 - no
        // heuristic page-matching is attempted). Soft-deleted in the same transaction as this
        // save, not a separate request, so there is no window where the two are inconsistent.
        if (!isNew && previousPdfDocumentResourceId != input.PdfDocumentResourceId)
        {
            _narrationRepository.DeleteByLessonId(entity.Id);
            _excludedSlideRepository.DeleteByLessonId(entity.Id); // EX-10
        }

        // EX-9 - null/omitted leaves the lesson's existing exclusions untouched (every ordinary
        // edit falls into this case); any value (including []) replaces the whole set. Must run
        // before UnitOfWork.Commit() below so the write lands in the same transaction as the
        // lesson save, and before the whole-deck reindex further down so that reindex sees the
        // final exclusion state.
        if (input.ExcludedSlideObjectIds is not null)
        {
            await ApplyExcludedSlidesAsync(entity, input.ExcludedSlideObjectIds);
        }

        UnitOfWork.Commit();

        Logger.LogInformation("Lesson {Action}: {Slug} slides={SlideCount}", isNew ? "created" : "updated", input.Slug, slideConfigs.Count);

        // Best-effort re-index for RAG grounding - a Slides API hiccup here must not undo the
        // save that already committed above, it just leaves the knowledge store stale until the
        // next successful save.
        if (!string.IsNullOrEmpty(presentationId))
        {
            try
            {
                var content = await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = presentationId });
                await knowledgeIndexingService.IndexLessonAsync(KnowledgeNamespaces.For(CurrentCompanyId, input.Slug), content.Slides);
            }
            catch (Exception ex)
            {
                // Non-blocking: keep the previously-indexed content. Log it so a broken RAG index
                // (stale answers to voice questions) is diagnosable instead of silent.
                Logger.LogWarning(ex, "RAG re-index failed on save for lesson {Slug}; knowledge store left stale", input.Slug);
            }
        }
        else if (input.ContentSourceType == LessonContentSourceType.Pdf && !string.IsNullOrEmpty(entity.PdfDocumentResourceId))
        {
            // NR-7 - a PDF lesson was never indexed into its own namespace at all before this
            // (only the `if (!string.IsNullOrEmpty(presentationId))` branch above ever ran).
            // Narration overrides go through the same NR-1 resolver as the tutor-facing content
            // path, so what gets indexed here always matches what CS just heard/read in preview.
            try
            {
                var pdfContent = await BuildPdfContentAsync(entity.PdfDocumentResourceId);
                var resolvedSlides = await narrationResolver.ResolveAsync(entity.Id, pdfContent.Slides);

                // EX-1 (consumer #3) - excluded pages never enter this namespace at all; this runs
                // after UnitOfWork.Commit() above, so it always sees the exclusion state this
                // request just wrote (EX-9's ordering requirement).
                var excludedIds = _excludedSlideRepository.GetByLessonId(entity.Id)
                    .Where(x => !x.IsDelete)
                    .Select(x => x.SlideObjectId)
                    .ToHashSet();
                var slidesToIndex = excludedIds.Count == 0
                    ? resolvedSlides
                    : resolvedSlides.Where(s => !excludedIds.Contains(s.SlideObjectId)).ToList();

                await knowledgeIndexingService.IndexLessonAsync(KnowledgeNamespaces.For(CurrentCompanyId, input.Slug), slidesToIndex);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "RAG re-index failed on save for PDF lesson {Slug}; knowledge store left stale", input.Slug);
            }
        }

        return entity.Adapt<LessonConfigViewModel>();
    }

    public Task<LessonConfigViewModel> MoveCategoryAsync(string id, string categoryId)
    {
        ValidateCategory(categoryId);
        var lesson = _repository.Get(id) ?? throw GeneralException.NotFound("บทเรียน");
        lesson.CategoryId = categoryId;
        lesson.UpdateBy = CurrentUserId;
        lesson.UpdateDate = DateTime.UtcNow;
        _repository.Update(lesson);
        UnitOfWork.Commit();
        return Task.FromResult(lesson.Adapt<LessonConfigViewModel>());
    }

    // ---- R9/Module L - trash/restore/permanent-delete (LT-1..LT-10) ---------------------------

    public Task<LessonConfigViewModel> ArchiveAsync(string id)
    {
        EnsureCanArchiveOrRestore();
        var now = DateTime.UtcNow;
        var jobId = IdGenerator.GenerateId("job");
        var scheduledPurgeAt = now.AddDays(LessonTrashPolicy.RetentionDays);
        if (!_repository.TryArchive(CurrentCompanyId, id, CurrentUserId, jobId, now, scheduledPurgeAt))
        {
            // LT-1/LT-3: an archive generation is only created by the request whose conditional
            // update wins. A stale/repeated request must never manufacture a second purge job.
            throw GeneralException.NotFound("บทเรียน");
        }

        var lesson = _repository.GetIncludingDeleted(CurrentCompanyId, id)
            ?? throw GeneralException.NotFound("บทเรียน");

        Logger.LogInformation("Lesson archived: {LessonId} purgeJob={JobId} scheduledPurgeAt={ScheduledPurgeAt}", lesson.Id, jobId, scheduledPurgeAt);

        return Task.FromResult(lesson.Adapt<LessonConfigViewModel>());
    }

    public Task<LessonConfigViewModel> RestoreAsync(string id)
    {
        EnsureCanArchiveOrRestore();

        var lesson = _repository.GetIncludingDeleted(CurrentCompanyId, id) ?? throw GeneralException.NotFound("บทเรียนในถัง");
        if (!lesson.IsDelete)
        {
            // Already active - LT-1's idempotency rule: a repeated restore on the same state
            // reads as NotFound (the trash-only lookup no longer finds it), not a silent success.
            throw GeneralException.NotFound("บทเรียนในถัง");
        }

        var purgeJobId = lesson.PurgeJobId;
        if (string.IsNullOrEmpty(purgeJobId))
        {
            throw GeneralException.ConfigError("ไม่พบงานลบถาวรของบทเรียนนี้");
        }
        var now = DateTime.UtcNow;
        var restored = _repository.TryRestoreAndCancelPurge(CurrentCompanyId, id, purgeJobId, CurrentUserId, now);
        if (!restored)
        {
            // LT-4 - the worker won the claim race first (PurgeStartedAt is already set).
            throw GeneralException.Conflict("บทเรียนนี้เริ่มลบถาวรแล้ว ไม่สามารถกู้คืนได้");
        }

        // TryRestore ran as raw SQL, which EF's change tracker does not see - `lesson` (already
        // tracked from GetIncludingDeleted above) must be updated by hand to match, or a caller
        // re-reading it through the same tracked instance (Get() -> DbSet.Find() -> identity map)
        // would see stale pre-restore values instead of what was actually just written.
        lesson.IsDelete = false;
        lesson.DeletedAt = null;
        lesson.DeleteBy = null;
        lesson.PurgeJobId = null;
        lesson.PurgeStartedAt = null;
        lesson.UpdateBy = CurrentUserId;
        lesson.UpdateDate = now;

        Logger.LogInformation("Lesson restored: {LessonId}", id);

        return Task.FromResult(lesson.Adapt<LessonConfigViewModel>());
    }

    public IReadOnlyList<LessonTrashItemViewModel> GetTrash()
    {
        EnsureCanArchiveOrRestore();
        var now = DateTime.UtcNow;
        return _repository.GetTrash(CurrentCompanyId)
            .OrderBy(x => x.DeletedAt)
            .ToList()
            .Select(x => BuildTrashItemViewModel(x, now))
            .ToList();
    }

    private static LessonTrashItemViewModel BuildTrashItemViewModel(LessonConfig lesson, DateTime now)
    {
        var deletedAt = lesson.DeletedAt ?? lesson.CreateDate; // DeletedAt is always set once IsDelete=true (DM-2 invariant)
        var scheduledPurgeAt = deletedAt.AddDays(LessonTrashPolicy.RetentionDays);
        var remaining = scheduledPurgeAt - now;
        var remainingDays = Math.Max(0, (int)Math.Floor(remaining.TotalDays));

        // LT-9 thresholds, evaluated against the real remaining timespan (not the rounded day
        // count) so the boundary at exactly 7x24h/14x24h lands on the right side.
        var urgency = remaining.TotalHours > 14 * 24
            ? LessonTrashUrgency.Neutral
            : remaining.TotalHours > 7 * 24
                ? LessonTrashUrgency.Yellow
                : remaining.TotalHours > 24
                    ? LessonTrashUrgency.Red
                    : LessonTrashUrgency.RedToday;

        return new LessonTrashItemViewModel
        {
            Id = lesson.Id,
            Slug = lesson.Slug,
            Title = lesson.Title,
            CategoryId = lesson.CategoryId,
            DeletedAt = deletedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ScheduledPurgeAt = scheduledPurgeAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            RemainingDays = remainingDays,
            Urgency = urgency,
            PurgeState = lesson.PurgeStartedAt is not null ? LessonPurgeState.Purging : LessonPurgeState.Trash,
        };
    }

    public Task RequestPermanentDeleteAsync(string id, string confirmationTitle)
    {
        // LT-2 - owner only, no exception for admin/cs.
        guard.EnsureOwner();

        var lesson = _repository.GetIncludingDeleted(CurrentCompanyId, id) ?? throw GeneralException.NotFound("บทเรียนในถัง");
        if (!lesson.IsDelete)
        {
            throw GeneralException.NotFound("บทเรียนในถัง");
        }
        if (lesson.PurgeStartedAt is not null)
        {
            // Already purging - nothing left to accelerate, and there is no "undo" from here.
            throw GeneralException.Conflict("บทเรียนนี้กำลังถูกลบถาวรอยู่แล้ว");
        }

        // LT-10 - server-side trim + ordinal-exact compare. Case-insensitive or client-only
        // validation would defeat the point of a typed confirmation.
        var trimmedInput = (confirmationTitle ?? string.Empty).Trim();
        if (!string.Equals(trimmedInput, lesson.Title.Trim(), StringComparison.Ordinal))
        {
            throw GeneralException.ValidationError("ชื่อบทเรียนที่พิมพ์ไม่ตรงกับชื่อบทเรียนจริง");
        }

        if (string.IsNullOrEmpty(lesson.PurgeJobId))
        {
            // Should never happen - ArchiveAsync always creates the job in the same transaction
            // that sets IsDelete=true. Defensive only.
            throw GeneralException.ConfigError("ไม่พบงานลบถาวรของบทเรียนนี้");
        }

        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        // LT-10 - accelerates the EXISTING job to run now; never deletes inline, never creates a
        // second job. If the worker has already claimed it in the instant since the check above,
        // this simply has no effect (the job is no longer Pending) - the deletion proceeds via the
        // claim that already won, not this one.
        jobRepository.AccelerateLessonPurge(CurrentCompanyId, id, lesson.PurgeJobId, CurrentUserId);
        UnitOfWork.Commit();

        Logger.LogInformation("Lesson permanent delete requested: {LessonId} job={JobId}", id, lesson.PurgeJobId);

        return Task.CompletedTask;
    }

    /// <summary>LT-2 - archive/restore are owner or admin only; cs gets 403. Owner must still have
    /// a selected company context (CurrentCompanyId throws otherwise) and passes
    /// EnsureCanAccessCompany the same as admin - the role check below is on top of that, not
    /// instead of it.</summary>
    private void EnsureCanArchiveOrRestore()
    {
        guard.EnsureCanAccessCompany(CurrentCompanyId);
        if (currentUser.Role == AdminRole.Cs)
        {
            throw GeneralException.Forbidden("เฉพาะ admin หรือ owner เท่านั้นที่จัดการถังบทเรียนได้");
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (slug.StartsWith("kbcat-", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slug, "kb-global", StringComparison.OrdinalIgnoreCase))
        {
            throw GeneralException.ValidationError("Slug ห้ามขึ้นต้นด้วย kbcat- หรือเท่ากับ kb-global");
        }
    }

    private void ValidateCategory(string categoryId)
    {
        var category = _knowledgeCategoryRepository.Get(categoryId) ?? throw GeneralException.ValidationError("ไม่พบหมวดที่เลือก");
        if (category.Level != 2)
        {
            throw GeneralException.ValidationError("บทเรียนต้องอยู่ในหมวดย่อยเท่านั้น");
        }
    }

    /// <summary>EX-9 - replaces the lesson's whole exclusion set with excludedSlideObjectIds
    /// (an empty list clears it to none). Must run before UnitOfWork.Commit() in SaveAsync so the
    /// write below lands in the same transaction as everything else.
    ///
    /// Reconciles against whatever rows this lesson already has (soft-deleted included) instead of
    /// blindly soft-deleting the whole set and inserting fresh rows every call - a retried save
    /// (the same excludedSlideObjectIds resubmitted after a timed-out response, for example) must
    /// land on the same rows, not add a second (LessonId, SlideObjectId) row on top of the one the
    /// first attempt already committed. Two live rows for the same page is exactly what later makes
    /// ILessonExcludedSlideRepository.GetOne(...).SingleOrDefault() throw instead of toggling
    /// (P11-01 - design.md EX-4).</summary>
    private async Task ApplyExcludedSlidesAsync(LessonConfig lesson, IReadOnlyList<string> excludedSlideObjectIds)
    {
        var distinctIds = excludedSlideObjectIds.Distinct().ToHashSet();

        if (distinctIds.Count > 0)
        {
            if (lesson.ContentSourceType != LessonContentSourceType.Pdf || string.IsNullOrEmpty(lesson.PdfDocumentResourceId))
            {
                throw GeneralException.ValidationError("ตัดหน้าออกได้เฉพาะบทเรียนที่ใช้ไฟล์ PDF เท่านั้น");
            }

            var baseContent = await BuildPdfContentAsync(lesson.PdfDocumentResourceId);
            var validIds = baseContent.Slides.Select(s => s.SlideObjectId).ToHashSet();
            foreach (var slideObjectId in distinctIds)
            {
                // EX-12(ข) - the same page-must-exist check EX-4 does: this value is used the exact
                // same way, to build a vector id a later job deletes for real.
                if (!validIds.Contains(slideObjectId))
                {
                    throw GeneralException.NotFound("หน้าเอกสาร");
                }
            }

            // EX-8 - hard floor, no confirm flag: at least one page must remain.
            if (baseContent.Slides.Count - distinctIds.Count < 1)
            {
                throw GeneralException.ValidationError("บทเรียนต้องเหลืออย่างน้อย 1 หน้า - ตัดหน้าสุดท้ายไม่ได้");
            }
        }

        // P11-01 - LessonExcludedSlideReconciler.ReconcileAndLoad collapses any legacy duplicate
        // (LessonId, SlideObjectId) group down to one row, regardless of whether this call's
        // distinctIds even mentions that SlideObjectId - a legacy duplicate on a page nobody is
        // touching in this save must still get cleaned up here, since EX-4's toggle endpoint runs
        // the exact same reconciliation independently (ILessonExcludedSlideService.ToggleAsync).
        var now = DateTime.UtcNow;
        var existingBySlideObjectId = LessonExcludedSlideReconciler.ReconcileAndLoad(_excludedSlideRepository, lesson.Id);

        foreach (var slideObjectId in distinctIds)
        {
            if (existingBySlideObjectId.TryGetValue(slideObjectId, out var existing))
            {
                if (existing.IsDelete)
                {
                    existing.IsDelete = false;
                    existing.DeletedAt = null;
                    existing.DeleteBy = null;
                    existing.UpdateBy = CurrentUserId;
                    existing.UpdateDate = now;
                    _excludedSlideRepository.Update(existing);
                }
                // else: already excluded and live - idempotent no-op, leave the row untouched.
            }
            else
            {
                _excludedSlideRepository.Add(new LessonExcludedSlide
                {
                    Id = IdGenerator.GenerateId("exsl"),
                    CompanyId = CurrentCompanyId,
                    LessonId = lesson.Id,
                    SlideObjectId = slideObjectId,
                    CreateBy = CurrentUserId,
                    CreateDate = now,
                });
            }
        }

        // "มีค่า" means replace the whole set (EX-9) - anything currently live that is not in the
        // replacement set gets soft-deleted, including when distinctIds is empty (clears all).
        foreach (var (slideObjectId, row) in existingBySlideObjectId)
        {
            if (!row.IsDelete && !distinctIds.Contains(slideObjectId))
            {
                row.IsDelete = true;
                row.DeletedAt = now;
                row.DeleteBy = CurrentUserId;
                _excludedSlideRepository.Update(row);
            }
        }
    }

    public async Task<LessonTeachingContentViewModel> GetTeachingContentBySlugAsync(string slug)
    {
        // Normal admin/back-office lookup, filtered: a trashed lesson simply is not found here
        // (LT-7) - the recipient-side path that CAN still see one under R9/LT-5/LT-6 goes through
        // GetTeachingContentByLinkAsync, which loads the lesson via GetIncludingDeleted instead.
        var lesson = _repository.GetBySlug(slug) ?? throw GeneralException.NotFound("บทเรียนนี้ หรือยังไม่เปิดใช้งาน");
        return await BuildTeachingContentAsync(lesson);
    }

    public async Task<LessonTeachingContentViewModel> GetTeachingContentByIdIncludingDeletedAsync(string lessonId)
    {
        var lesson = _repository.GetIncludingDeleted(CurrentCompanyId, lessonId)
            ?? throw GeneralException.NotFound("บทเรียนนี้ หรือยังไม่เปิดใช้งาน");
        return await BuildTeachingContentAsync(lesson);
    }

    private async Task<LessonTeachingContentViewModel> BuildTeachingContentAsync(LessonConfig lesson)
    {
        if (!lesson.IsActive)
        {
            throw GeneralException.NotFound("บทเรียนนี้ หรือยังไม่เปิดใช้งาน");
        }

        var content = lesson.ContentSourceType == LessonContentSourceType.Pdf
            ? await GetPdfContentAsync(lesson)
            : await GetGoogleSlidesContentAsync(lesson);

        // EX-1 (consumer #1)/EX-3(ก) - an excluded page disappears from the list entirely and
        // every remaining page's Index is renumbered 0..M-1 in file order, never left as a gap.
        // Only meaningful for a PDF lesson - a Google Slides lesson never has exclusion rows.
        var excludedIds = lesson.ContentSourceType == LessonContentSourceType.Pdf
            ? _excludedSlideRepository.GetByLessonId(lesson.Id).Where(x => !x.IsDelete).Select(x => x.SlideObjectId).ToHashSet()
            : [];

        var durationBySlide = lesson.SlideConfigs.ToDictionary(s => s.SlideObjectId, s => s.VideoDurationMs ?? 0);
        var slides = content.Slides
            .Where(s => !excludedIds.Contains(s.SlideObjectId))
            .OrderBy(s => s.Index)
            .Select((s, i) => new TeachingSlideViewModel
            {
                SlideObjectId = s.SlideObjectId,
                Index = i,
                SpeakerNotes = s.SpeakerNotes,
                SlideUrl = s.SlideUrl,
                VideoDurationMs = durationBySlide.GetValueOrDefault(s.SlideObjectId, 0),
            })
            .ToList();

        return new LessonTeachingContentViewModel
        {
            Lesson = lesson.Adapt<LessonConfigViewModel>(),
            EmbedUrl = !string.IsNullOrEmpty(content.EmbedUrl) ? content.EmbedUrl : (lesson.SlidesEmbedUrl ?? ""),
            Slides = slides,
        };
    }

    public async Task<LearnerLessonTeachingContentViewModel> GetTeachingContentByLinkAsync(string token, string? learnerKey)
    {
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByTokenForContentAccess(token, learnerKey);

        // R9/LT-5/LT-6 - the trash-aware lookup: the normal query filter behind GetBySlug hides a
        // trashed lesson, but a learner whose (token, learnerKey) just passed the gate above is
        // exactly the case that must still see it (an IN_PROGRESS session on a revoked link, or
        // any session on a link that isn't revoked at all).
        var content = await GetTeachingContentByIdIncludingDeletedAsync(link.LessonId);

        // LP-1/LP-4 - pacing is a company-level default with no per-lesson override anymore
        // (N1/N2/N3, 2026-08-22) - read straight off Company.Default*Ms. This is the one place in
        // the system this is read; ICompanyService.Create/SeedFirstCompanyIfEmpty are the only
        // places it is written (LP-2).
        var company = _companyRepository.Get(link.CompanyId) ?? throw GeneralException.NotFound("บริษัท");

        var slides = content.Slides.Select(slide => new TeachingSlideViewModel
        {
            SlideObjectId = slide.SlideObjectId,
            Index = slide.Index,
            SpeakerNotes = slide.SpeakerNotes,
            SlideUrl = ToPublicPdfPageUrl(slide.SlideUrl, token),
            VideoDurationMs = slide.VideoDurationMs,
        }).ToList();

        return new LearnerLessonTeachingContentViewModel
        {
            Lesson = new LearnerLessonConfigViewModel
            {
                Slug = content.Lesson.Slug,
                Title = content.Lesson.Title,
                Description = content.Lesson.Description,
                ContentSourceType = content.Lesson.ContentSourceType,
                IntroWaitMs = company.DefaultIntroWaitMs,
                BreathPauseMs = company.DefaultBreathPauseMs,
                FinalQuestionWaitMs = company.DefaultFinalQuestionWaitMs,
            },
            EmbedUrl = content.EmbedUrl,
            Slides = slides,
        };
    }

    private static string? ToPublicPdfPageUrl(string? slideUrl, string token)
    {
        if (string.IsNullOrEmpty(slideUrl) || !slideUrl.StartsWith("pdf-page:", StringComparison.Ordinal))
        {
            return slideUrl;
        }

        var parts = slideUrl.Split(':', 3);
        return parts.Length == 3
            ? $"/api/lessons/pdf-pages/{Uri.EscapeDataString(token)}/{Uri.EscapeDataString(parts[1])}/{parts[2]}"
            : null;
    }

    private async Task<SlidesLessonContent> GetGoogleSlidesContentAsync(LessonConfig lesson)
    {
        if (string.IsNullOrEmpty(lesson.PresentationId))
        {
            throw GeneralException.ConfigError("บทเรียนนี้ยังไม่ได้ตั้งค่า Google Slides");
        }
        try
        {
            return await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = lesson.PresentationId });
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }

    private async Task<SlidesLessonContent> GetPdfContentAsync(LessonConfig lesson)
    {
        if (string.IsNullOrEmpty(lesson.PdfDocumentResourceId))
        {
            throw GeneralException.ConfigError("บทเรียนนี้ยังไม่ได้อัปโหลดไฟล์ PDF");
        }
        var content = await BuildPdfContentAsync(lesson.PdfDocumentResourceId);

        // NR-1 - apply CS-authored narration overrides on top of the extracted text. This is the
        // tutor-facing consumer of the shared resolver; ProcessLessonIndexAsync (Application
        // layer, BackgroundJobProcessor) is the other, so what the tutor engine speaks and what
        // the RAG index answers from can never disagree.
        var resolvedSlides = await narrationResolver.ResolveAsync(lesson.Id, content.Slides);
        return ReferenceEquals(resolvedSlides, content.Slides)
            ? content
            : new SlidesLessonContent
            {
                PresentationId = content.PresentationId,
                Title = content.Title,
                EmbedUrl = content.EmbedUrl,
                Slides = resolvedSlides,
                SyncedAt = content.SyncedAt,
            };
    }

    public async Task<SlidesLessonContent> PreviewPdfAsync(string documentId) => await BuildPdfContentAsync(documentId);

    /// <summary>
    /// Parsing (ContentOrderTextExtractor + the Thai PUA-glyph fixups + line-joining, per page)
    /// used to re-run in full on every single room-open - cached the same way GetPdfBytesAsync
    /// already caches the raw bytes, since a document's bytes (and therefore its parsed content)
    /// never change for a given id. GetOrCreateAsync only caches a successful result, so a
    /// corrupt/non-PDF file's exception below is never cached - it's re-validated next time too.
    /// </summary>
    private async Task<SlidesLessonContent> BuildPdfContentAsync(string documentId)
    {
        return await memoryCache.GetOrCreateAsync($"pdf-content:{documentId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            var (document, bytes) = await GetPdfBytesAsync(documentId);
            try
            {
                using var pdfStream = new MemoryStream(bytes, writable: false);
                return PdfSlidesRenderer.BuildContent(pdfStream, document.Id, document.FileName);
            }
            catch (Exception ex) when (ex is not HttpStatusCodeException)
            {
                // A non-PDF upload (the picker also accepts .pptx/.docx/.xlsx) or a corrupt file used to
                // surface as an opaque 500. Give CS a clear, actionable message instead.
                Logger.LogWarning(ex, "PDF content build failed for document {DocumentId} ({FileName})", document.Id, document.FileName);
                throw GeneralException.ValidationError($"ไฟล์ \"{document.FileName}\" อ่านเป็น PDF ไม่ได้ - แหล่งเนื้อหาแบบ PDF ต้องเป็นไฟล์ .pdf เท่านั้น");
            }
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
    }

    public async Task<byte[]> RenderPdfPageAsync(string documentId, int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw GeneralException.ValidationError("เลขหน้าต้องเริ่มจาก 1");
        }

        // Page count comes from the cached parsed content instead of a second PDFium parse just
        // to count pages - PdfSlidesRenderer.BuildContent already keeps one ResolvedSlide per PDF
        // page (blank pages included, for continuous numbering), so the two counts are always
        // identical, and after the first call this is a cache hit rather than real work.
        var content = await BuildPdfContentAsync(documentId);
        var pageCount = content.Slides.Count;
        if (pageNumber > pageCount)
        {
            throw GeneralException.NotFound($"หน้า {pageNumber} (เอกสารนี้มี {pageCount} หน้า)");
        }

        return await memoryCache.GetOrCreateAsync($"pdf-page-png:{documentId}:{pageNumber}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            var (_, bytes) = await GetPdfBytesAsync(documentId);
            using var pdfStream = new MemoryStream(bytes, writable: false);
            return PdfSlidesRenderer.RenderPagePng(pdfStream, pageNumber);
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
    }

    /// <summary>Cache key format shared by CreatePdfPreviewSessionAsync/RenderPdfPreviewPageAsync -
    /// the one and only place preview session bytes live (NR-10 forbids a second cache mechanism).</summary>
    private static string PdfPreviewCacheKey(string previewId) => $"pdf-preview:{previewId}";

    /// <summary>NR-11 - CompanyId is stored alongside the bytes precisely because IMemoryCache has
    /// no HasQueryFilter equivalent; every read has to check it by hand.</summary>
    private sealed class PdfPreviewCacheEntry
    {
        public required byte[] Bytes { get; init; }
        public required string CompanyId { get; init; }
        public required int PageCount { get; init; }
    }

    public async Task<PdfPreviewSessionViewModel> CreatePdfPreviewSessionAsync(Stream fileStream, string fileName)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        var previewId = IdGenerator.GenerateId("pdfprev");
        SlidesLessonContent content;
        try
        {
            using var pdfStream = new MemoryStream(bytes, writable: false);
            content = PdfSlidesRenderer.BuildContent(pdfStream, previewId, fileName);
        }
        catch (Exception ex)
        {
            // Gate reason (1) - an unparseable/non-PDF upload must fail clean as a 4xx here, not
            // as an opaque 500 or a crashed worker process (there is no worker in this path).
            Logger.LogWarning(ex, "PDF preview build failed for {FileName}", fileName);
            throw GeneralException.ValidationError($"ไฟล์ \"{fileName}\" อ่านเป็น PDF ไม่ได้ - แหล่งเนื้อหาแบบ PDF ต้องเป็นไฟล์ .pdf เท่านั้น");
        }

        memoryCache.Set(
            PdfPreviewCacheKey(previewId),
            new PdfPreviewCacheEntry { Bytes = bytes, CompanyId = CurrentCompanyId, PageCount = content.Slides.Count },
            new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });

        // NR-5 - same formula as LessonSlideNarrationService.GetAllAsync, byte for byte.
        var isLikelyScanned = content.Slides.Count > 0
            && content.Slides.All(s => string.IsNullOrWhiteSpace(s.SpeakerNotes));

        var slides = content.Slides
            .OrderBy(s => s.Index)
            .Select(s => new PdfPreviewSlideViewModel
            {
                SlideObjectId = s.SlideObjectId,
                Index = s.Index,
                NarrationText = s.SpeakerNotes,
            })
            .ToList();

        return new PdfPreviewSessionViewModel
        {
            PreviewId = previewId,
            Title = content.Title,
            PageCount = content.Slides.Count,
            IsLikelyScanned = isLikelyScanned,
            Slides = slides,
        };
    }

    public Task<byte[]> RenderPdfPreviewPageAsync(string previewId, int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw GeneralException.ValidationError("เลขหน้าต้องเริ่มจาก 1");
        }

        // NR-11 - a missing entry (never created, or expired) and a CompanyId mismatch (someone
        // else's previewId) both throw the exact same NotFound below. Do not split this into two
        // branches with different messages - that difference is exactly what would let a caller
        // tell the two cases apart.
        var entry = memoryCache.Get<PdfPreviewCacheEntry>(PdfPreviewCacheKey(previewId));
        if (entry is null || !string.Equals(entry.CompanyId, CurrentCompanyId, StringComparison.Ordinal))
        {
            throw GeneralException.NotFound("ไฟล์ตัวอย่าง PDF");
        }

        if (pageNumber > entry.PageCount)
        {
            throw GeneralException.NotFound($"หน้า {pageNumber} (เอกสารนี้มี {entry.PageCount} หน้า)");
        }

        using var pdfStream = new MemoryStream(entry.Bytes, writable: false);
        return Task.FromResult(PdfSlidesRenderer.RenderPagePng(pdfStream, pageNumber));
    }

    /// <summary>
    /// Opening a PDF room re-renders every page as an image (one HTTP call per page), and each of
    /// those used to re-download the whole file from storage. A document's bytes never change (a
    /// re-upload gets a fresh id), so cache them briefly - this collapses N storage round-trips
    /// per room-open into one, which matters most once storage is remote (Huawei OBS) rather than
    /// local disk.
    /// </summary>
    private async Task<(DocumentResource Document, byte[] Bytes)> GetPdfBytesAsync(string documentId)
    {
        var document = _documentResourceRepository.Get(documentId)
            ?? throw GeneralException.NotFound("ไฟล์ PDF");
        var bytes = await memoryCache.GetOrCreateAsync($"pdf-bytes:{documentId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            try
            {
                using var pdfStream = await documentStorageProvider.DownloadAsync(document.ObsKey);
                using var buffer = new MemoryStream();
                await pdfStream.CopyToAsync(buffer);
                return buffer.ToArray();
            }
            catch (Exception ex)
            {
                // The DB row can outlive the physical object (manual deletion, a storage reset
                // without a matching migration, etc.) - used to leak the storage provider's raw
                // exception (local file path / S3 error) as an opaque 500 instead of the same
                // clean 404 a missing DB row already gets.
                Logger.LogWarning(ex, "PDF storage download failed for document {DocumentId} ({FileName})", document.Id, document.FileName);
                throw GeneralException.NotFound("ไฟล์ PDF");
            }
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
        return (document, bytes);
    }
}
