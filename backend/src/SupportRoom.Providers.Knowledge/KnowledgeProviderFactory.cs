using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SupportRoom.Providers.Knowledge;

public sealed class KnowledgeProviders
{
    public required IEmbeddingProvider Embedding { get; init; }
    public required IKnowledgeIndexProvider Index { get; init; }
}

/// <summary>
/// One switch (KNOWLEDGE_PROVIDER), not two - pinecone pairs the real Gemini-embedding +
/// Pinecone-index implementations. Keeps the env footprint small; nothing stops splitting this
/// into two independent switches later if a real deployment ever wants to mix vendors.
/// </summary>
public static class KnowledgeProviderFactory
{
    public static KnowledgeProviders Create(string knowledgeProvider, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) => knowledgeProvider switch
    {
        SupportRoom.Domain.Configuration.KnowledgeProvider.Pinecone => new KnowledgeProviders
        {
            Embedding = new GeminiEmbeddingProvider(httpClientFactory, loggerFactory.CreateLogger<GeminiEmbeddingProvider>()),
            Index = new PineconeKnowledgeIndexProvider(httpClientFactory, loggerFactory.CreateLogger<PineconeKnowledgeIndexProvider>()),
        },
        // Same Pinecone index, embeddings from OpenAI instead of Gemini (both 768-dim). Requires
        // every vector to have been re-indexed with OpenAI - see KnowledgeProvider.PineconeOpenAi.
        SupportRoom.Domain.Configuration.KnowledgeProvider.PineconeOpenAi => new KnowledgeProviders
        {
            Embedding = new OpenAiEmbeddingProvider(httpClientFactory, loggerFactory.CreateLogger<OpenAiEmbeddingProvider>()),
            Index = new PineconeKnowledgeIndexProvider(httpClientFactory, loggerFactory.CreateLogger<PineconeKnowledgeIndexProvider>()),
        },
        // Unreachable in practice - ProviderSelectionReader already validates against
        // KnowledgeProvider.Allowed before this value ever reaches here.
        _ => throw new ArgumentOutOfRangeException(nameof(knowledgeProvider), knowledgeProvider, "Unknown knowledge provider"),
    };
}
