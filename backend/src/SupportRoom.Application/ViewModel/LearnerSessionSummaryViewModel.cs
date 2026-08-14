namespace SupportRoom.Application.ViewModel;

/// <summary>
/// Public recap shape. The internal UnansweredPoints list and CS review fields never cross the
/// learner API boundary.
/// </summary>
public sealed class LearnerSessionSummaryViewModel
{
    public required string SessionId { get; init; }
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
    public required IReadOnlyList<LearnerSessionQuestionViewModel> Questions { get; init; }
    public required string CreatedAt { get; init; }
}
