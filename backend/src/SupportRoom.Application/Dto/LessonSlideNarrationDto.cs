using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

/// <summary>Input for PUT /api/lessons/{id}/narrations/{slideObjectId} - see NR-2. An empty/
/// whitespace-only NarrationText is a valid request (it means "delete this override"), so it is
/// intentionally not [Required] - the service layer decides what an empty value means, not model
/// binding.</summary>
public sealed class LessonSlideNarrationDto
{
    [MaxLength(DtoLimits.NarrationTextMaxLength)]
    public string? NarrationText { get; init; }
}
