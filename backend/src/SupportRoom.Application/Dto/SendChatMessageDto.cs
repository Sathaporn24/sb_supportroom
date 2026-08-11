using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class SendChatMessageDto
{
    [Required]
    public required string SessionId { get; init; }

    /// <summary>"recipient" (whoever opened the join link) | "agent" (the company's support staff)
    /// | "system". Renamed away from "teacher"/"cs" - those were School Bright's words and this
    /// product is used by companies whose users are not teachers.</summary>
    [Required]
    public required string SenderRole { get; init; }

    public string? SenderName { get; init; }

    [Required, StringLength(DtoLimits.MaxTextLength, MinimumLength = 1)]
    public required string Text { get; init; }
}
