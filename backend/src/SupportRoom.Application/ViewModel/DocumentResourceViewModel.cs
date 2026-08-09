namespace SupportRoom.Application.ViewModel;

public sealed class DocumentResourceViewModel
{
    public required string Id { get; init; }
    public string? LessonId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string IndexingStatus { get; init; }
    public required int IndexedChunkCount { get; init; }
    public required string CreatedAt { get; init; }
}
