namespace SupportRoom.Application.ViewModel;

/// <summary>DI-7 - what CS sees on the "extracted text" screen for one document, one row per
/// chunk. Text here is exactly what the knowledge store received (DM-4) - never re-parsed on the
/// fly - so it stays true even after the parser that produced it changes.</summary>
public sealed class DocumentChunkViewModel
{
    public required string Id { get; init; }
    public required string ChunkKey { get; init; }
    public required int SeqNo { get; init; }
    public required string Text { get; init; }
    public required int CharCount { get; init; }

    /// <summary>DI-6 - sort hint only, never a correctness verdict.</summary>
    public required bool HasSuspectCharacters { get; init; }
}
