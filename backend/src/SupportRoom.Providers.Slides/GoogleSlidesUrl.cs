using System.Text.RegularExpressions;

namespace SupportRoom.Providers.Slides;

public readonly record struct ParsedSlidesUrl(string? PresentationId, bool IsPublished);

/// <summary>Mirrors src/utils/google-slides-url.ts.</summary>
public static partial class GoogleSlidesUrl
{
    [GeneratedRegex(@"/presentation/d/([a-zA-Z0-9_-]+)")]
    private static partial Regex SourceIdPattern();

    [GeneratedRegex(@"/presentation/d/e/([a-zA-Z0-9_-]+)")]
    private static partial Regex PublishedIdPattern();

    public static bool IsValid(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && parsed.Host.Equals("docs.google.com", StringComparison.OrdinalIgnoreCase)
            && parsed.AbsolutePath.Contains("/presentation/");
    }

    /// <summary>
    /// Source ("edit") URLs and published ("pub"/"embed") URLs encode different, mutually
    /// incompatible identifiers - a published ID cannot be used with the Google Slides API.
    /// </summary>
    public static ParsedSlidesUrl Parse(string url)
    {
        if (PublishedIdPattern().IsMatch(url))
        {
            return new ParsedSlidesUrl(null, true);
        }

        var sourceMatch = SourceIdPattern().Match(url);
        return sourceMatch.Success
            ? new ParsedSlidesUrl(sourceMatch.Groups[1].Value, false)
            : new ParsedSlidesUrl(null, false);
    }

    public static string BuildEmbedUrl(string presentationId)
        => $"https://docs.google.com/presentation/d/{presentationId}/embed?start=false&loop=false&delayms=60000";
}
