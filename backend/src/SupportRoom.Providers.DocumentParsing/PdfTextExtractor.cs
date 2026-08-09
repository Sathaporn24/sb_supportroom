using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SupportRoom.Providers.DocumentParsing;

/// <summary>One chunk per page.</summary>
public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    /// <summary>
    /// Same PUA fixup table as SupportRoom.Providers.Slides' PdfSlidesRenderer (see its doc
    /// comment on ThaiPuaGlyphFixups for how each mapping was reverse-engineered) - duplicated
    /// here rather than shared via a project reference because this project has no other reason
    /// to depend on Slides, whose own dependencies (Google Slides API client, PDF-to-image
    /// rendering) are unrelated to generic document parsing for RAG chunking. Left unfixed, tone
    /// marks are dropped entirely from extracted text (not just "off"), which degrades embedding
    /// quality and Gemini's grounded answers for PDF-sourced lessons.
    /// </summary>
    private static readonly Dictionary<char, char> ThaiPuaGlyphFixups = new()
    {
        // Explicit \u escapes on both sides (not literal glyphs) - the PUA keys render as
        // nothing in most fonts/editors, making literal-glyph text silently easy to corrupt on
        // copy/paste.
        [''] = 'ิ', // sara i
        [''] = 'ี', // sara ii
        [''] = '่', // mai ek (stacked-above-vowel variant)
        [''] = '่', // mai ek
        [''] = '้', // mai tho
        [''] = '์', // thanthakhat (karan)
        [''] = 'ั', // mai han akat
        [''] = '็', // mai tai khu
    };

    /// <summary>Bullet glyphs a document author uses as list markers - meaningless noise for RAG
    /// embeddings, and glued straight onto the next word with no pause since they carry no
    /// trailing space in the source PDF.</summary>
    private static readonly char[] BulletMarkers = ['●', '○', '■', '□', '▪', '-', '*'];

    public IReadOnlyList<DocumentTextChunk> Extract(Stream content)
    {
        using var pdf = PdfDocument.Open(content);
        var chunks = new List<DocumentTextChunk>();

        foreach (var page in pdf.GetPages())
        {
            var text = BuildPageText(page);
            if (!string.IsNullOrWhiteSpace(text))
            {
                chunks.Add(new DocumentTextChunk { ChunkId = $"page-{page.Number}", Text = text.Trim() });
            }
        }

        return chunks;
    }

    /// <summary>
    /// page.Text (used here until this fix) concatenates letters in raw content-stream order
    /// with no regard for layout, so unrelated paragraphs/headings land glued together with zero
    /// separator - hurting embedding/retrieval quality for RAG chunking. ContentOrderTextExtractor
    /// groups that same raw text into lines using the glyphs' actual positions instead.
    /// </summary>
    private static string BuildPageText(UglyToad.PdfPig.Content.Page page)
        => JoinContentOrderedLines(FixThaiPuaGlyphs(ContentOrderTextExtractor.GetText(page, true)));

    /// <summary>
    /// Each line is one paragraph/heading on the page - joined with a full stop so retrieval
    /// text doesn't read as one run-on sentence. Public for the same reason as
    /// PdfSlidesRenderer's equivalent - unit-testable without a real PdfPig Page/PDF fixture.
    /// </summary>
    public static string JoinContentOrderedLines(string contentOrderedText)
    {
        var sb = new StringBuilder();
        foreach (var rawLine in contentOrderedText.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim().TrimStart(BulletMarkers).Trim();
            if (line.Length == 0)
            {
                continue;
            }
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(line);
            if (!".!?:๚๏".Contains(line[^1]))
            {
                sb.Append('.');
            }
        }
        return sb.ToString();
    }

    /// <summary>Public for the same reason as JoinContentOrderedLines - unit-testable without a
    /// real PDF fixture.</summary>
    public static string FixThaiPuaGlyphs(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (ThaiPuaGlyphFixups.TryGetValue(chars[i], out var mapped))
            {
                chars[i] = mapped;
            }
        }
        return new string(chars);
    }
}
