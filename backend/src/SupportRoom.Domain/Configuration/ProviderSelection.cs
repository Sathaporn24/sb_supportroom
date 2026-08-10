namespace SupportRoom.Domain.Configuration;

public static class SlidesProvider
{
    public const string Google = "google";
    public static readonly string[] Allowed = [Google];
}

public static class TtsProvider
{
    public const string Edge = "edge";
    public const string ElevenLabs = "elevenlabs";
    public static readonly string[] Allowed = [Edge, ElevenLabs];
}

public static class VoiceQuestionProvider
{
    public const string Gemini = "gemini";

    /// <summary>Same wire contract as Gemini - retrieval-augmented instead of full-deck-context.</summary>
    public const string GeminiRag = "gemini-rag";

    /// <summary>Retrieval-augmented, but the answer step (3) runs on OpenAI instead of Gemini -
    /// Gemini still handles audio transcription (1). Offloads the heavy answer generation off
    /// Gemini's quota. Pair with KNOWLEDGE_PROVIDER=pinecone-openai so retrieval embeds on OpenAI too.</summary>
    public const string OpenAiRag = "openai-rag";
    public static readonly string[] Allowed = [Gemini, GeminiRag, OpenAiRag];
}

public static class KnowledgeProvider
{
    public const string Pinecone = "pinecone";

    /// <summary>Same Pinecone index, but embeddings are produced by OpenAI instead of Gemini.
    /// The index dimension must match (both 768) and every vector must have been re-indexed with
    /// OpenAI - a query embedded by one vendor can't search vectors embedded by the other.</summary>
    public const string PineconeOpenAi = "pinecone-openai";
    public static readonly string[] Allowed = [Pinecone, PineconeOpenAi];
}

public static class DocumentStorageProvider
{
    /// <summary>Writes to local disk - real persistence, survives restarts, no cloud account
    /// needed. Interim step before HuaweiObs.</summary>
    public const string Local = "local";
    public const string HuaweiObs = "huawei-obs";
    public static readonly string[] Allowed = [Local, HuaweiObs];
}

public sealed class ProviderSelection
{
    public required string SlidesProvider { get; init; }
    public required string TtsProvider { get; init; }
    public required string VoiceQuestionProvider { get; init; }
    public required string KnowledgeProvider { get; init; }
    public required string DocumentStorageProvider { get; init; }
}

public sealed class InvalidProviderSelectionException(string variable, string value, string[] allowed)
    : Exception($"Invalid value \"{value}\" for {variable} - expected one of: {string.Join(", ", allowed)}");

/// <summary>
/// Every provider category requires an explicit, valid value - there is no Mock fallback and no
/// default. The data layer has no switch at all - EF Core/Postgres is a hard requirement, not
/// optional, so it's deliberately absent from this type; see ConnectionStrings:Postgres.
/// </summary>
public static class ProviderSelectionReader
{
    public static ProviderSelection Read()
    {
        return new ProviderSelection
        {
            SlidesProvider = ReadOne("SLIDES_PROVIDER", SlidesProvider.Allowed),
            TtsProvider = ReadOne("TTS_PROVIDER", TtsProvider.Allowed),
            VoiceQuestionProvider = ReadOne("VOICE_QUESTION_PROVIDER", VoiceQuestionProvider.Allowed),
            KnowledgeProvider = ReadOne("KNOWLEDGE_PROVIDER", KnowledgeProvider.Allowed),
            DocumentStorageProvider = ReadOne("DOCUMENT_STORAGE_PROVIDER", DocumentStorageProvider.Allowed),
        };
    }

    private static string ReadOne(string envVar, string[] allowed)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrEmpty(value) || !allowed.Contains(value))
        {
            throw new InvalidProviderSelectionException(envVar, value ?? "", allowed);
        }
        return value;
    }
}
