using System.Text.Json;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public sealed class LessonNarrationSlideViewModel
{
    public required string SlideObjectId { get; init; }
    public required int Index { get; init; }
    public required string NarrationText { get; init; }

    /// <summary>true when a LessonSlideNarration row exists for this page - the admin editor uses
    /// this to tell a CS-authored page apart from one still showing the extracted prefill.</summary>
    public required bool IsOverridden { get; init; }
}

public sealed class LessonNarrationsViewModel
{
    public required IReadOnlyList<LessonNarrationSlideViewModel> Slides { get; init; }

    /// <summary>NR-5 - true when every page's freshly-extracted SpeakerNotes is blank after trim
    /// (almost always a scanned PDF). A warning, not an error: narration can still be saved
    /// normally, CS just has to type every page by hand.</summary>
    public required bool IsLikelyScanned { get; init; }
}

public interface ILessonSlideNarrationService
{
    Task<LessonNarrationsViewModel> GetAllAsync(string lessonId);

    /// <summary>NR-2 - narrationText is trimmed and compared against the page's current extracted
    /// prefill: equal (including empty) deletes the override row if one exists, different upserts
    /// it. Enqueues a lesson_index job for just this page on any actual change (NR-6).</summary>
    Task SaveAsync(string lessonId, string slideObjectId, string? narrationText);

    /// <summary>NR-3 - how many narration rows would be soft-deleted if the lesson's PDF source is
    /// replaced. The admin UI calls this before letting CS confirm the upload.</summary>
    int CountByLessonId(string lessonId);
}

/// <summary>
/// CRUD over LessonSlideNarration (design.md DM-5/NR-2/NR-9). Depends on ILessonConfigService only
/// for its cached PDF-content build (PreviewPdfAsync) - re-parsing the PDF here directly would
/// duplicate that caching and risk drifting from what GetTeachingContentBySlugAsync actually
/// resolves.
/// </summary>
public sealed class LessonSlideNarrationService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ILessonSlideNarrationService> logger,
    ILessonConfigService lessonConfigService,
    ILessonSlideNarrationResolver narrationResolver)
    : ServiceBase<ILessonSlideNarrationService>(unitOfWork, serviceProvider, logger), ILessonSlideNarrationService
{
    private readonly ILessonConfigRepository _lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly ILessonSlideNarrationRepository _narrationRepository = unitOfWork.GetRepository<ILessonSlideNarrationRepository>();

    public async Task<LessonNarrationsViewModel> GetAllAsync(string lessonId)
    {
        var lesson = _lessonRepository.Get(lessonId) ?? throw GeneralException.NotFound("บทเรียน");
        EnsurePdfSource(lesson);

        var baseContent = await lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId!);
        var isLikelyScanned = baseContent.Slides.Count > 0
            && baseContent.Slides.All(s => string.IsNullOrWhiteSpace(s.SpeakerNotes));

        var resolvedSlides = await narrationResolver.ResolveAsync(lessonId, baseContent.Slides);
        var overriddenIds = _narrationRepository.GetByLessonId(lessonId).Select(x => x.SlideObjectId).ToHashSet();

        var slides = resolvedSlides
            .OrderBy(s => s.Index)
            .Select(s => new LessonNarrationSlideViewModel
            {
                SlideObjectId = s.SlideObjectId,
                Index = s.Index,
                NarrationText = s.SpeakerNotes,
                IsOverridden = overriddenIds.Contains(s.SlideObjectId),
            })
            .ToList();

        return new LessonNarrationsViewModel { Slides = slides, IsLikelyScanned = isLikelyScanned };
    }

    public async Task SaveAsync(string lessonId, string slideObjectId, string? narrationText)
    {
        var lesson = _lessonRepository.Get(lessonId) ?? throw GeneralException.NotFound("บทเรียน");
        EnsurePdfSource(lesson);

        var baseContent = await lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId!);
        var baseSlide = baseContent.Slides.FirstOrDefault(s => s.SlideObjectId == slideObjectId)
            ?? throw GeneralException.NotFound("หน้าเอกสาร");

        var trimmed = SanitizeNarrationText(narrationText).Trim();
        var prefill = baseSlide.SpeakerNotes.Trim();
        var existing = _narrationRepository.GetOne(lessonId, slideObjectId);
        var changed = false;

        if (trimmed.Length == 0 || trimmed == prefill)
        {
            // NR-2: an empty submission, or one that trims back down to exactly what the
            // extractor already gives, must never leave a row behind - a row here means "CS
            // deliberately overrode this page," not "CS typed back what was already there."
            if (existing is not null)
            {
                existing.IsDelete = true;
                existing.DeletedAt = DateTime.UtcNow;
                existing.DeleteBy = CurrentUserId;
                _narrationRepository.Update(existing);
                changed = true;
            }
        }
        else if (existing is not null)
        {
            existing.NarrationText = trimmed;
            existing.UpdateBy = CurrentUserId;
            existing.UpdateDate = DateTime.UtcNow;
            _narrationRepository.Update(existing);
            changed = true;
        }
        else
        {
            _narrationRepository.Add(new LessonSlideNarration
            {
                Id = IdGenerator.GenerateId("narr"),
                CompanyId = CurrentCompanyId,
                LessonId = lessonId,
                SlideObjectId = slideObjectId,
                NarrationText = trimmed,
                CreateBy = CurrentUserId,
                CreateDate = DateTime.UtcNow,
            });
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        EnqueueLessonIndexJob(lessonId, [slideObjectId]);
        UnitOfWork.Commit();

        Logger.LogInformation("Lesson narration saved: {LessonId}/{SlideObjectId}, re-index job queued", lessonId, slideObjectId);
    }

    public int CountByLessonId(string lessonId)
    {
        var lesson = _lessonRepository.Get(lessonId) ?? throw GeneralException.NotFound("บทเรียน");
        return _narrationRepository.GetByLessonId(lesson.Id).Count();
    }

    private static void EnsurePdfSource(LessonConfig lesson)
    {
        // NR-9 server-side reject - Google Slides has no narration override path at all, the
        // editor UI hiding the button is not enough on its own.
        if (lesson.ContentSourceType != LessonContentSourceType.Pdf)
        {
            throw GeneralException.ValidationError("แก้บทพูดได้เฉพาะบทเรียนที่ใช้ไฟล์ PDF เท่านั้น");
        }
        if (string.IsNullOrEmpty(lesson.PdfDocumentResourceId))
        {
            throw GeneralException.ConfigError("บทเรียนนี้ยังไม่ได้อัปโหลดไฟล์ PDF");
        }
    }

    /// <summary>PostgreSQL text columns reject any NUL byte outright (22021), even inside otherwise
    /// valid UTF-8 - and PDF-extracted SpeakerNotes can carry NUL/control-char artifacts from the
    /// source document's binary content. Strip NUL plus other C0 control chars (keeping \n/\t,
    /// which are legitimate in multi-line spoken narration) so a save can never crash on this
    /// regardless of whether the garbage came from the extractor or from a CS paste.</summary>
    private static string SanitizeNarrationText(string? narrationText)
    {
        if (string.IsNullOrEmpty(narrationText))
        {
            return "";
        }

        return string.Concat(narrationText.Where(c => c >= ' ' || c is '\n' or '\t'));
    }

    private void EnqueueLessonIndexJob(string lessonId, IReadOnlyList<string> slideObjectIds)
    {
        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        jobRepository.Add(new BackgroundJob
        {
            Id = IdGenerator.GenerateId("job"),
            CompanyId = CurrentCompanyId,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
            JobType = BackgroundJobType.LessonIndex,
            TargetId = lessonId,
            PayloadJson = JsonSerializer.Serialize(new LessonIndexJobPayload { SlideObjectIds = slideObjectIds }),
            Status = BackgroundJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = DateTime.UtcNow,
        });
    }
}
