namespace SupportRoom.Application.ViewModel;

/// <summary>
/// Computed on every read, not stored. The SessionSummary table this used to be loaded from was
/// dropped (TD-013): two of its three columns duplicated LearningSession, and UnansweredPoints is
/// derivable from SessionQuestion. Keeping it as a persisted snapshot would also have drifted -
/// CS edits question reviews *after* a session ends, so a frozen copy slowly stops matching what
/// CS is looking at.
///
/// The shape is unchanged from when it was table-backed, so the frontend contract survived the
/// removal untouched.
/// </summary>
public sealed class SessionSummaryViewModel
{
    public required string SessionId { get; init; }
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
    public required IReadOnlyList<SessionQuestionViewModel> Questions { get; init; }

    /// <summary>Transcripts of the questions the tutor could not ground an answer for. Internal:
    /// this is what CS follows up on, and CORE_FEATURE_SPEC §2.5 keeps it off the learner's own
    /// recap screen.</summary>
    public required IReadOnlyList<string> UnansweredPoints { get; init; }

    public required string CreatedAt { get; init; }
}
