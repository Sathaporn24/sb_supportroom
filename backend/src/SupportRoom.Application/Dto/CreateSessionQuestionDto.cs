using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class CreateSessionQuestionDto
{
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }

    /// <summary>One of SupportRoom.Domain.Enums.AnswerStatus.</summary>
    [Required]
    public required string AnswerStatus { get; init; }
}
