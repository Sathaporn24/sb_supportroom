namespace SupportRoom.Application.ViewModel;

public sealed class SessionQuestionViewModel
{
    public required string Id { get; init; }
    public required string SessionId { get; init; }
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }
    public required string CreatedAt { get; init; }

    /// <summary>"voice" | "text" (QuestionSource) - CS-facing only. Not on
    /// LearnerSessionQuestionViewModel: the learner already knows how they asked (RR-5).</summary>
    public required string Source { get; init; }

    /// <summary>"correct" | "incorrect" | null while unreviewed. CS-facing only - the learner's
    /// own end-of-lesson recap never carries these (CORE_FEATURE_SPEC §2.5).</summary>
    public string? ReviewResult { get; init; }
    public string? ReviewNote { get; init; }
    public string? ReviewedAt { get; init; }
}
