namespace SupportRoom.Application.ViewModel;

public sealed class KnowledgeCategoryViewModel
{
    public required string Id { get; init; }
    public string? ParentId { get; init; }
    public required int Level { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsSystemDefault { get; init; }
}

public sealed class CategoryMovePreviewViewModel
{
    public required int LosingDocuments { get; init; }
    public required int LosingQnAs { get; init; }
    public required int GainingDocuments { get; init; }
    public required int GainingQnAs { get; init; }
}
