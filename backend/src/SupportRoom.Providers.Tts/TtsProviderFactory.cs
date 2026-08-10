using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Tts;

/// <summary>Mirrors src/providers/tts/index.ts's createTextToSpeechProvider().</summary>
public static class TtsProviderFactory
{
    public static ITtsProvider Create(string ttsProvider, ILoggerFactory loggerFactory) => ttsProvider switch
    {
        TtsProvider.Edge => new EdgeTtsProvider(loggerFactory.CreateLogger<EdgeTtsProvider>()),
        // Unreachable in practice - ProviderSelectionReader already validates against
        // TtsProvider.Allowed before this value ever reaches here.
        _ => throw new ArgumentOutOfRangeException(nameof(ttsProvider), ttsProvider, "Unknown TTS provider"),
    };
}
