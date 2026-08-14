namespace SupportRoom.Application.ViewModel;

/// <summary>
/// Public question shape. ReviewResult, ReviewNote and ReviewedAt are deliberately absent because
/// they are internal QA metadata, not part of the learner's transcript.
/// </summary>
public sealed class LearnerSessionQuestionViewModel
{
    public required string Id { get; init; }
    public required string SessionId { get; init; }
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }
    public required string CreatedAt { get; init; }
}
