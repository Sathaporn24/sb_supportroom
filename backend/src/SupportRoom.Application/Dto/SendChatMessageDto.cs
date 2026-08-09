using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class SendChatMessageDto
{
    [Required]
    public required string SessionId { get; init; }

    /// <summary>"teacher" | "cs" | "system".</summary>
    [Required]
    public required string SenderRole { get; init; }

    public string? SenderName { get; init; }

    [Required, StringLength(DtoLimits.MaxTextLength, MinimumLength = 1)]
    public required string Text { get; init; }
}
