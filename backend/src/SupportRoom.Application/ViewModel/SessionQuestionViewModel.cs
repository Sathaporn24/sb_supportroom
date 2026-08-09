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
}
