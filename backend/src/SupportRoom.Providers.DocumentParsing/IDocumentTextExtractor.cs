namespace SupportRoom.Providers.DocumentParsing;

public sealed class DocumentTextChunk
{
    public required string ChunkId { get; init; }
    public required string Text { get; init; }
}

public interface IDocumentTextExtractor
{
    IReadOnlyList<DocumentTextChunk> Extract(Stream content);
}
