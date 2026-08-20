namespace SupportRoom.Application.ViewModel;

/// <summary>Mirrors TrainingLink in domain.ts - timestamps are ISO-8601 strings on the wire.</summary>
public sealed class TrainingLinkViewModel
{
    public required string Id { get; init; }
    public required string Token { get; init; }
    public required string LessonId { get; init; }
    public required string LessonSlug { get; init; }
    public string? RecipientOrgName { get; init; }
    public required string CreatedAt { get; init; }
    public required string ExpiresAt { get; init; }
    public int? MaxAttendees { get; init; }

    /// <summary>LinkStatus.Active | LinkStatus.Expired - computed from ExpiresAt at read time,
    /// never stored (see LinkStatus).</summary>
    public required string Status { get; init; }

    /// <summary>Legacy total-round count retained while the admin frontend moves to the three
    /// explicit aggregates below.</summary>
    public required int LearningSessionCount { get; init; }

    /// <summary>Distinct browsers (LearnerKey values) that have opened this link. A learner who
    /// starts another round still counts as one learner.</summary>
    public required int LearnerCount { get; init; }

    /// <summary>Number of learning rounds currently in progress.</summary>
    public required int InProgressCount { get; init; }

    /// <summary>Number of learning rounds that have ended.</summary>
    public required int EndedCount { get; init; }
}
