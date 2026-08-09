using System.Text;
using DocumentFormat.OpenXml.Packaging;

namespace SupportRoom.Providers.DocumentParsing;

/// <summary>One chunk per slide - body text plus speaker notes, mirrors how Google Slides
/// content is chunked in SupportRoom.Providers.Slides (one chunk per slide there too).</summary>
public sealed class PptxTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyList<DocumentTextChunk> Extract(Stream content)
    {
        using var doc = PresentationDocument.Open(content, false);
        var presentationPart = doc.PresentationPart;
        var slideIdList = presentationPart?.Presentation.SlideIdList;
        if (presentationPart is null || slideIdList is null)
        {
            return [];
        }

        var chunks = new List<DocumentTextChunk>();
        var index = 0;
        foreach (var slideId in slideIdList.Elements<DocumentFormat.OpenXml.Presentation.SlideId>())
        {
            index++;
            var relId = slideId.RelationshipId?.Value;
            if (string.IsNullOrEmpty(relId))
            {
                continue;
            }

            var slidePart = (SlidePart)presentationPart.GetPartById(relId);
            var text = ExtractSlideText(slidePart);
            if (!string.IsNullOrWhiteSpace(text))
            {
                chunks.Add(new DocumentTextChunk { ChunkId = $"slide-{index}", Text = text.Trim() });
            }
        }

        return chunks;
    }

    private static string ExtractSlideText(SlidePart slidePart)
    {
        var sb = new StringBuilder();

        foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
        {
            sb.Append(text.Text).Append(' ');
        }

        if (slidePart.NotesSlidePart is not null)
        {
            foreach (var text in slidePart.NotesSlidePart.NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                sb.Append(text.Text).Append(' ');
            }
        }

        return sb.ToString();
    }
}
