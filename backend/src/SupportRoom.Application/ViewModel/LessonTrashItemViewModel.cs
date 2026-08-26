namespace SupportRoom.Application.ViewModel;

/// <summary>
/// R9/LT-7/LT-9 - one row of GET /api/lessons/trash. deletedAt/scheduledPurgeAt/remainingDays/
/// urgency/purgeState are all computed at read time from LessonConfig's existing DM-2 columns
/// (see LessonTrashViewModelFactory) - none of them are stored.
/// </summary>
public sealed class LessonTrashItemViewModel
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string CategoryId { get; init; }

    /// <summary>ISO-8601 - when the lesson was archived.</summary>
    public required string DeletedAt { get; init; }

    /// <summary>ISO-8601 - DeletedAt + the fixed 60-day retention (design.md O-18).</summary>
    public required string ScheduledPurgeAt { get; init; }

    /// <summary>Floor of whole days left before ScheduledPurgeAt, clamped to 0 - never negative,
    /// even if the worker's purge attempt is still pending/deferred past the deadline (LT-12).</summary>
    public required int RemainingDays { get; init; }

    /// <summary>LessonTrashUrgency value.</summary>
    public required string Urgency { get; init; }

    /// <summary>LessonPurgeState value - "purging" means every action is disabled and no
    /// restore/permanent-delete button should be shown at all (LT-9).</summary>
    public required string PurgeState { get; init; }
}
