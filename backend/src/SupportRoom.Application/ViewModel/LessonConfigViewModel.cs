namespace SupportRoom.Application.ViewModel;

public sealed class SlideConfigViewModel
{
    public required string SlideObjectId { get; init; }
    public required int SlideIndex { get; init; }
    public int? VideoDurationMs { get; init; }
}

public sealed class LessonConfigViewModel
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string CategoryId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string SlidesSourceUrl { get; init; }
    public string? PresentationId { get; init; }
    public string? SlidesEmbedUrl { get; init; }
    public required string ContentSourceType { get; init; }
    public string? PdfDocumentResourceId { get; init; }

    public required IReadOnlyList<SlideConfigViewModel> SlideConfigs { get; init; }
    public required bool IsActive { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}
