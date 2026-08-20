namespace SupportRoom.Application.Services;

/// <summary>
/// DI-6 - a heuristic that only helps CS sort which chunks to look at first, never a correctness
/// check. It catches NUL/control bytes, PUA glyph substitution (the Thai-tone-mark-into-PUA bug
/// Google Slides PDF export produces - see PdfSlidesRenderer.ThaiPuaGlyphFixups) and the Unicode
/// replacement character, but a chunk that extracted into plain-looking but scrambled Thai text
/// passes right through it (design.md R-5). Must never be used to block indexing or set a
/// document's status to failed.
/// </summary>
public static class DocumentChunkTextAnalyzer
{
    private const char PuaStart = '';
    private const char PuaEnd = '';
    private const char ReplacementCharacter = '�';
    private const char C0ControlMax = '';

    public static bool HasSuspectCharacters(string text)
    {
        foreach (var c in text)
        {
            if (IsSuspectControlCharacter(c) || (c >= PuaStart && c <= PuaEnd) || c == ReplacementCharacter)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSuspectControlCharacter(char c)
        => c <= C0ControlMax && c is not ('\t' or '\n' or '\r');
}
