using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class CreateKnowledgeCategoryDto
{
    public string? ParentId { get; init; }
    [Required]
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int SortOrder { get; init; }
}

public sealed class UpdateKnowledgeCategoryDto
{
    [Required]
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int SortOrder { get; init; }
}
