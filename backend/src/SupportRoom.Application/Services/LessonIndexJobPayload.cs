using System.Text.Json;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Application.Services;

/// <summary>
/// NR-6 - BackgroundJob.PayloadJson shape for JobType = lesson_index. TargetId is the LessonId.
/// Written whenever a PDF lesson's narration is saved/deleted (ILessonSlideNarrationService) or a
/// page is excluded/restored (EX-5, ILessonExcludedSlideService), read back by
/// IBackgroundJobProcessor.ProcessLessonIndexAsync - only the listed pages are re-embedded/
/// upserted (or deleted), never the whole deck.
/// </summary>
public sealed class LessonIndexJobPayload
{
    public required IReadOnlyList<string> SlideObjectIds { get; init; }
}

/// <summary>Shared by ILessonSlideNarrationService.SaveAsync (NR-6) and
/// ILessonExcludedSlideService.ToggleAsync (EX-5) - both enqueue the exact same job shape for a
/// single page, so the construction lives in one place instead of two independent copies.</summary>
internal static class LessonIndexJobFactory
{
    public static BackgroundJob Create(string companyId, string? userId, string lessonId, IReadOnlyList<string> slideObjectIds) => new()
    {
        Id = IdGenerator.GenerateId("job"),
        CompanyId = companyId,
        CreateBy = userId,
        CreateDate = DateTime.UtcNow,
        JobType = BackgroundJobType.LessonIndex,
        TargetId = lessonId,
        PayloadJson = JsonSerializer.Serialize(new LessonIndexJobPayload { SlideObjectIds = slideObjectIds }),
        Status = BackgroundJobStatus.Pending,
        AttemptCount = 0,
        NextAttemptAt = DateTime.UtcNow,
    };
}
