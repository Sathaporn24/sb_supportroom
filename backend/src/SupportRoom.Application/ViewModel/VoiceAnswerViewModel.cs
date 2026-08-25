namespace SupportRoom.Application.ViewModel;

public sealed class VoiceAnswerViewModel
{
    public string Transcript { get; init; } = "";
    public string Answer { get; init; } = "";
    public required string AnswerStatus { get; init; }
    public string? RelatedSlideObjectId { get; init; }
}
