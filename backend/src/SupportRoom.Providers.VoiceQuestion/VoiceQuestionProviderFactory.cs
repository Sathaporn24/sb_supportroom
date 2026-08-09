using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Providers.VoiceQuestion;

/// <summary>Mirrors src/providers/voice-question/index.ts's createVoiceQuestionProvider().</summary>
public static class VoiceQuestionProviderFactory
{
    public static IVoiceQuestionProvider Create(
        string voiceQuestionProvider,
        IHttpClientFactory httpClientFactory,
        IEmbeddingProvider embeddingProvider,
        IKnowledgeIndexProvider knowledgeIndexProvider,
        ILoggerFactory loggerFactory) => voiceQuestionProvider switch
    {
        VoiceQuestionProvider.Gemini => new GeminiVoiceQuestionProvider(httpClientFactory, loggerFactory.CreateLogger<GeminiVoiceQuestionProvider>()),
        VoiceQuestionProvider.GeminiRag => new RagVoiceQuestionProvider(
            httpClientFactory, embeddingProvider, knowledgeIndexProvider, loggerFactory.CreateLogger<RagVoiceQuestionProvider>()),
        // Unreachable in practice - ProviderSelectionReader already validates against
        // VoiceQuestionProvider.Allowed before this value ever reaches here.
        _ => throw new ArgumentOutOfRangeException(nameof(voiceQuestionProvider), voiceQuestionProvider, "Unknown voice question provider"),
    };
}
