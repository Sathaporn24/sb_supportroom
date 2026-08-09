namespace SupportRoom.Application.ViewModel;

public sealed class SessionSummaryViewModel
{
    public required string SessionId { get; init; }
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
    public required IReadOnlyList<SessionQuestionViewModel> Questions { get; init; }
    public required IReadOnlyList<string> UnansweredPoints { get; init; }
    public required string CreatedAt { get; init; }
}
