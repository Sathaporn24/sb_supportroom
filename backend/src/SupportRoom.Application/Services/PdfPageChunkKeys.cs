namespace SupportRoom.Application.Services;

/// <summary>EX-6 - the one place "pdf-page-N" (LessonExcludedSlide.SlideObjectId /
/// LessonSlideNarration.SlideObjectId) gets converted to "page-N" (DocumentChunk.ChunkKey,
/// PdfTextExtractor's key format). N is a direct, exact copy between the two - never guessed, never
/// re-derived from file parsing - so this conversion is the only place that mapping should ever be
/// written, shared by LessonExcludedSlideService (toggling one page) and
/// BackgroundJobProcessor.ProcessDocumentIndexAsync (re-indexing a whole document).</summary>
internal static class PdfPageChunkKeys
{
    private const string SlideObjectIdPrefix = "pdf-page-";

    public static string? ToDocumentChunkKey(string slideObjectId)
        => slideObjectId.StartsWith(SlideObjectIdPrefix, StringComparison.Ordinal)
            && int.TryParse(slideObjectId[SlideObjectIdPrefix.Length..], out var pageNumber)
                ? $"page-{pageNumber}"
                : null;
}
