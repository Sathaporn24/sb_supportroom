using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Slides;

/// <summary>Mirrors src/providers/slides/index.ts's createSlidesContentProvider().</summary>
public static class SlidesProviderFactory
{
    public static ISlidesProvider Create(string slidesProvider, ILoggerFactory loggerFactory) => slidesProvider switch
    {
        SlidesProvider.Google => new GoogleSlidesProvider(loggerFactory.CreateLogger<GoogleSlidesProvider>()),
        // Unreachable in practice - ProviderSelectionReader already validates against
        // SlidesProvider.Allowed before this value ever reaches here.
        _ => throw new ArgumentOutOfRangeException(nameof(slidesProvider), slidesProvider, "Unknown slides provider"),
    };
}
