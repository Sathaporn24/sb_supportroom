using System.Text;
using PDFtoImage;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using SupportRoom.Domain;

namespace SupportRoom.Providers.Slides;

/// <summary>
/// Mirrors ISlidesProvider's job (produce SlidesLessonContent) for a PDF instead of a Google
/// Slides deck. Unlike Google, there's no separate "resolve" step - the file is already
/// uploaded (via the existing DocumentResource/IDocumentStorageProvider pipeline), so this only
/// needs to turn PDF bytes into the same DTOs GoogleSlidesProvider produces. Narration is the
/// page's own extracted text spoken as-is (no summarization, see BuildNarration below for the
/// cleanup that *is* applied). Text extraction keeps blank pages (unlike PdfTextExtractor's
/// RAG-chunking path, which drops them) since a slide deck needs continuous page numbering.
/// </summary>
public static class PdfSlidesRenderer
{
    /// <summary>DPI tuned for on-screen slide display, not print quality - keeps page images a
    /// reasonable size/render time for a training-room use case.</summary>
    private const int RenderDpi = 150;

    /// <summary>
    /// PDFs exported from Google Slides commonly embed a subsetted Sarabun/Kanit font whose
    /// internal cmap assigns tone marks and vowel signs to Private Use Area codepoints instead
    /// of their real Thai Unicode values - reverse-engineered from a real exported deck by
    /// reading page.Letters codepoint-by-codepoint and reconstructing the Thai words around each
    /// PUA glyph (e.g. "ผู[U+F70B]ช[U+F70A]วย" only reads as
    /// "ผู้ช่วย" ("assistant") once mapped). Left unfixed,
    /// PdfPig's extracted text is missing every tone mark for these glyphs (so TTS mispronounces
    /// or slurs the affected words) rather than just looking odd. Multiple PUA codepoints can
    /// map to the same character - the font uses separate glyph variants for the same mark
    /// depending on what it stacks above (e.g. mai ek directly over a bare consonant vs. over
    /// one that already carries an upper vowel sign).
    /// </summary>
    private static readonly Dictionary<char, char> ThaiPuaGlyphFixups = new()
    {
        [''] = 'ิ', // sara i
        [''] = 'ี', // sara ii
        [''] = '่', // mai ek (stacked-above-vowel variant)
        [''] = '่', // mai ek
        [''] = '้', // mai tho
        [''] = '์', // thanthakhat (karan)
        [''] = 'ั', // mai han akat
        [''] = '็', // mai tai khu
    };

    /// <summary>Bullet glyphs a slide author uses as list markers - read literally by TTS
    /// ("dot", "circle", ...) or, worse, glued straight onto the next word with no pause since
    /// they carry no trailing space in the source PDF.</summary>
    private static readonly char[] BulletMarkers = ['●', '○', '■', '□', '▪', '-', '*'];

    /// <summary>
    /// A first-page "line" longer than this is almost certainly wrapped body prose picked up by
    /// ContentOrderTextExtractor, not a heading - using it as a title would just swap one
    /// meaningless title for another.
    /// </summary>
    private const int MaxHeadingCandidateLength = 120;

    /// <summary>
    /// Tries, in order, the PDF's own metadata title, then a heading-shaped first line of page 1,
    /// before giving up and using <paramref name="fallbackTitle"/> (the uploaded filename) - most
    /// PDFs exported from Slides/Word/PowerPoint carry a real title in the doc metadata, and the
    /// handful that don't usually still put the actual heading as the first line of the deck.
    /// </summary>
    public static SlidesLessonContent BuildContent(Stream pdfStream, string documentId, string fallbackTitle)
    {
        using var pdf = PdfDocument.Open(pdfStream);
        var slides = new List<ResolvedSlide>();
        string? headingCandidate = null;

        foreach (var page in pdf.GetPages())
        {
            var cleanedText = GetCleanedPageText(page);
            if (page.Number == 1)
            {
                headingCandidate = ExtractHeadingCandidate(cleanedText);
            }

            slides.Add(new ResolvedSlide
            {
                SlideObjectId = $"pdf-page-{page.Number}",
                Index = page.Number - 1,
                SpeakerNotes = JoinLinesForNarration(cleanedText),
                // The public page endpoint also needs the training-link token to recover company
                // context. LessonConfigService replaces this provider-local marker at the API
                // boundary; persisting or guessing a token down in this provider would violate
                // the layer separation.
                SlideUrl = $"pdf-page:{documentId}:{page.Number}",
            });
        }

        return new SlidesLessonContent
        {
            PresentationId = documentId,
            Title = ResolveTitle(pdf.Information?.Title, headingCandidate, fallbackTitle),
            EmbedUrl = "",
            Slides = slides,
            SyncedAt = DateTime.UtcNow.ToString("O"),
        };
    }

    /// <summary>
    /// page.Text concatenates letters in raw content-stream order with no regard for layout, so a
    /// title textbox added last in the file lands stuck onto the end of the previous bullet's
    /// sentence with zero separator. ContentOrderTextExtractor groups that same raw text into
    /// lines using the glyphs' actual positions, which is enough for TTS purposes even though it
    /// doesn't fully re-derive human "reading order" for a slide whose text boxes were arranged
    /// very unconventionally. Also used as the source for the page-1 title heuristic in
    /// BuildContent, so it lives on its own rather than folded into JoinLinesForNarration.
    /// </summary>
    private static string GetCleanedPageText(UglyToad.PdfPig.Content.Page page)
        => FixThaiPuaGlyphs(ContentOrderTextExtractor.GetText(page, true));

    /// <summary>Strips a line's surrounding whitespace and any leading bullet marker glyph -
    /// shared between narration joining and the title-heuristic line scan so both agree on what
    /// counts as "the same line" once cleaned.</summary>
    private static string CleanLine(string rawLine) => rawLine.Trim().TrimStart(BulletMarkers).Trim();

    /// <summary>
    /// Each line is one bullet/paragraph on the slide - joined with a full stop so TTS pauses
    /// between unrelated points instead of reading them as one run-on sentence. Public so it's
    /// unit-testable without needing a real PdfPig Page/PDF fixture.
    /// </summary>
    public static string JoinLinesForNarration(string contentOrderedText)
    {
        var sb = new StringBuilder();
        foreach (var rawLine in contentOrderedText.Replace("\r\n", "\n").Split('\n'))
        {
            var line = CleanLine(rawLine);
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

    /// <summary>
    /// Picks out the first non-empty cleaned line of page 1 as a candidate slide-deck title, e.g.
    /// a title textbox rendered as its own line above the body content. Returns null (rather than
    /// falling through to a later line) when that first line is clearly body prose rather than a
    /// heading - a long wrapped sentence is not a better guess than the filename fallback. Public
    /// so it's unit-testable without a real PdfPig Page/PDF fixture.
    /// </summary>
    public static string? ExtractHeadingCandidate(string contentOrderedText)
    {
        foreach (var rawLine in contentOrderedText.Replace("\r\n", "\n").Split('\n'))
        {
            var line = CleanLine(rawLine);
            if (line.Length == 0)
            {
                continue;
            }
            return line.Length <= MaxHeadingCandidateLength ? line : null;
        }
        return null;
    }

    /// <summary>
    /// The three-source title fallback described on BuildContent, factored out as a pure function
    /// of already-extracted strings so it's unit-testable without a real PdfDocument/PdfPig
    /// fixture (PDF metadata Title itself can only be exercised against a real PDF).
    /// </summary>
    public static string ResolveTitle(string? metadataTitle, string? headingCandidate, string fallbackTitle)
    {
        if (!string.IsNullOrWhiteSpace(metadataTitle))
        {
            return metadataTitle.Trim();
        }
        if (!string.IsNullOrWhiteSpace(headingCandidate))
        {
            return headingCandidate.Trim();
        }
        return fallbackTitle;
    }

    /// <summary>Public for the same reason as JoinLinesForNarration - unit-testable without a
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

    /// <summary>Page count via PDFium - cheap validation before rendering a specific page, and it
    /// throws on a non-PDF/corrupt stream so callers can turn that into a clean 4xx.</summary>
    public static int GetPageCount(Stream pdfStream) => Conversion.GetPageCount(pdfStream);

    /// <summary>1-based pageNumber, matching PdfPig's Page.Number convention used above.</summary>
    public static byte[] RenderPagePng(Stream pdfStream, int pageNumber)
    {
        using var bitmap = Conversion.ToImage(pdfStream, page: pageNumber - 1, options: new RenderOptions(Dpi: RenderDpi));
        using var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
