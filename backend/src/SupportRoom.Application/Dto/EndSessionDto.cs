using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class EndSessionDto
{
    [Required]
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
}
