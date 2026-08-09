using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class ResolveSlidesDto
{
    [Required]
    public required string SlidesSourceUrl { get; init; }
    public string? SlidesEmbedUrl { get; init; }
}
