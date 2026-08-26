using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

/// <summary>Input for PUT /api/lessons/{id}/slides/{slideObjectId}/excluded - see EX-4.</summary>
public sealed class ToggleSlideExcludedDto
{
    [Required]
    public required bool Excluded { get; init; }
}
