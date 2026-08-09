using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SupportRoom.Providers.DocumentParsing;

/// <summary>Word documents have no natural "slide"/"page" unit at the XML level - groups every
/// N paragraphs into one chunk instead.</summary>
public sealed class DocxTextExtractor : IDocumentTextExtractor
{
    private const int ParagraphsPerChunk = 10;

    public IReadOnlyList<DocumentTextChunk> Extract(Stream content)
    {
        using var doc = WordprocessingDocument.Open(content, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return [];
        }

        var paragraphs = body.Elements<Paragraph>()
            .Select(p => p.InnerText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var chunks = new List<DocumentTextChunk>();
        for (var i = 0; i < paragraphs.Count; i += ParagraphsPerChunk)
        {
            var group = paragraphs.Skip(i).Take(ParagraphsPerChunk);
            chunks.Add(new DocumentTextChunk
            {
                ChunkId = $"para-{i / ParagraphsPerChunk + 1}",
                Text = string.Join("\n", group).Trim(),
            });
        }

        return chunks;
    }
}
