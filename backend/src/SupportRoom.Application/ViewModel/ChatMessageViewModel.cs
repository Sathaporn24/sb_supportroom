namespace SupportRoom.Application.ViewModel;

public sealed class ChatMessageViewModel
{
    public required string Id { get; init; }
    public required string SessionId { get; init; }
    public required string SenderRole { get; init; }
    public string? SenderName { get; init; }
    public required string Text { get; init; }
    public required string CreatedAt { get; init; }
}
