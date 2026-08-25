using System.Text;
using System.Text.RegularExpressions;

namespace SupportRoom.Providers.Tts;

/// <summary>
/// Shared by every provider that benefits from synthesizing narration as several short chunks
/// instead of one long request - either to dodge a flaky socket timeout (Edge) or to run chunks
/// concurrently and cut wall-clock on a slow-per-call model (ElevenLabs v3). Splits at
/// sentence-ish boundaries (newlines, . ! ? and Thai's "ๆ") first, then packs pieces up to the
/// caller's limit; a run with neither punctuation nor spaces is hard-cut so no chunk stays long.
/// </summary>
public static partial class TextChunker
{
    public static List<string> SplitIntoChunks(string text, int maxChunkChars)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        var pieces = SentenceBoundary().Split(text.Trim());
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }
        }

        foreach (var raw in pieces)
        {
            var piece = raw.Trim();
            if (piece.Length == 0)
            {
                continue;
            }

            if (piece.Length > maxChunkChars)
            {
                Flush();
                chunks.AddRange(HardSplit(piece, maxChunkChars));
                continue;
            }

            if (current.Length + piece.Length + 1 > maxChunkChars)
            {
                Flush();
            }
            if (current.Length > 0)
            {
                current.Append(' ');
            }
            current.Append(piece);
        }

        Flush();
        return chunks;
    }

    private static IEnumerable<string> HardSplit(string s, int maxChunkChars)
    {
        var sb = new StringBuilder();
        foreach (var word in s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = word;
            while (w.Length > maxChunkChars)
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                yield return w[..maxChunkChars];
                w = w[maxChunkChars..];
            }
            if (sb.Length > 0 && sb.Length + w.Length + 1 > maxChunkChars)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(w);
        }
        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }

    [GeneratedRegex(@"(?<=[\.\!\?\n]|ๆ)\s+")]
    private static partial Regex SentenceBoundary();
}
