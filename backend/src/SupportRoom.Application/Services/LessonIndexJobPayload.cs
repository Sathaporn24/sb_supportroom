namespace SupportRoom.Application.Services;

/// <summary>
/// NR-6 - BackgroundJob.PayloadJson shape for JobType = lesson_index. TargetId is the LessonId.
/// Written whenever a PDF lesson's narration is saved/deleted (ILessonSlideNarrationService),
/// read back by IBackgroundJobProcessor.ProcessLessonIndexAsync - only the listed pages are
/// re-embedded/upserted, never the whole deck.
/// </summary>
public sealed class LessonIndexJobPayload
{
    public required IReadOnlyList<string> SlideObjectIds { get; init; }
}
