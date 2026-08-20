namespace SupportRoom.Application.ViewModel;

/// <summary>Mirrors LearningSession in domain.ts - one person's run through a lesson.</summary>
public sealed class LearningSessionViewModel
{
    public required string Id { get; init; }
    public required string TrainingLinkId { get; init; }
    public required string RecipientName { get; init; }
    public required string Status { get; init; }
    public required string StartedAt { get; init; }
    public string? EndedAt { get; init; }
    public required string LastActivityAt { get; init; }
    public string? LastSlideObjectId { get; init; }
    public int? LastSlideIndex { get; init; }

    /// <summary>Pairs with LastSlideIndex so CS reads "7/20" instead of a bare "slide 7". Null
    /// until the learner's browser has reported a resolved deck.</summary>
    public int? TotalSlideCount { get; init; }
    public required bool CompletedAllSlides { get; init; }

    /// <summary>
    /// Derived at read time from LastActivityAt, never stored: (now - LastActivityAt) is past
    /// INACTIVE_THRESHOLD_MINUTES while still IN_PROGRESS. The browser signal that would let us
    /// store this is missed exactly when it matters (closed laptop, dead battery, lost
    /// connection), so the clock is the only reliable witness - CORE_FEATURE_SPEC §2.6.
    /// </summary>
    public required bool IsStalled { get; init; }

    /// <summary>LearnerKey is deliberately NOT on this ViewModel. It is the browser's private key
    /// for resuming, and it travels only between one learner and the server - never into an admin
    /// listing where every learner's key would sit in one payload.</summary>
    public required int QuestionCount { get; init; }
}
