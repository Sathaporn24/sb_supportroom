namespace SupportRoom.Domain.Configuration;

/// <summary>Mirrors src/config/tutor-config.ts's plain constants.</summary>
public static class TutorConfig
{
    public const int DefaultIntroWaitMs = 5_000;
    public const int DefaultBreathPauseMs = 500;
    public const int DefaultFinalQuestionWaitMs = 5_000;
    public const int DefaultSessionExpiryHours = 24;
}

public sealed class LessonTimingDefaults
{
    public required int IntroWaitMs { get; init; }
    public required int BreathPauseMs { get; init; }
    public required int FinalQuestionWaitMs { get; init; }
}

/// <summary>Mirrors src/config/server-defaults.ts.</summary>
public static class ServerDefaults
{
    /// <summary>
    /// A blank (but present) env var must fall back to the default exactly like an unset one -
    /// naively parsing "" as a number would otherwise silently produce 0.
    /// </summary>
    private static int NumberEnv(string envVar, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrEmpty(raw)) return fallback;
        return int.TryParse(raw, out var parsed) ? parsed : fallback;
    }

    /// <summary>Applied when a LessonConfig is created without overriding the timing fields.</summary>
    public static LessonTimingDefaults GetLessonTimingDefaults() => new()
    {
        IntroWaitMs = NumberEnv("DEFAULT_INTRO_WAIT_MS", TutorConfig.DefaultIntroWaitMs),
        BreathPauseMs = NumberEnv("DEFAULT_BREATH_PAUSE_MS", TutorConfig.DefaultBreathPauseMs),
        FinalQuestionWaitMs = NumberEnv("DEFAULT_FINAL_QUESTION_WAIT_MS", TutorConfig.DefaultFinalQuestionWaitMs),
    };

    public static int GetDefaultSessionExpiryHours() =>
        NumberEnv("DEFAULT_SESSION_EXPIRY_HOURS", TutorConfig.DefaultSessionExpiryHours);
}

/// <summary>Mirrors src/config/env.ts's uploadLimits.</summary>
public static class UploadLimits
{
    private static int NumberEnv(string envVar, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrEmpty(raw)) return fallback;
        return int.TryParse(raw, out var parsed) ? parsed : fallback;
    }

    public static int MaxVoiceUploadMb => NumberEnv("MAX_VOICE_UPLOAD_MB", 5);
    public static int MinVoiceDurationMs => NumberEnv("MIN_VOICE_DURATION_MS", 300);

    /// <summary>Documents (pptx/pdf/docx) are naturally bigger than a short voice clip.</summary>
    public static int MaxDocumentUploadMb => NumberEnv("MAX_DOCUMENT_UPLOAD_MB", 20);
}

public sealed class GoogleServiceAccountCredentials
{
    public required string ProjectId { get; init; }
    public required string ClientEmail { get; init; }
    public required string PrivateKey { get; init; }
}

public sealed class GeminiCredentials
{
    public required string ApiKey { get; init; }
    public required string Model { get; init; }
}

public sealed class EdgeTtsSettings
{
    public required string Voice { get; init; }
    public required string Rate { get; init; }
}

public sealed class OpenAiCredentials
{
    public required string ApiKey { get; init; }

    /// <summary>API base (up to and including the version segment), no trailing slash - e.g.
    /// "https://api.openai.com/v1" for OpenAI, or an OpenAI-compatible gateway like Zhipu GLM
    /// ("https://open.bigmodel.cn/api/paas/v4"). Set via OPENAI_BASE_URL. The provider appends
    /// "/chat/completions" and "/embeddings".</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Chat model for the RAG answer step. gpt-4o-mini by default (cheap, JSON-mode capable);
    /// override with OPENAI_MODEL.</summary>
    public required string Model { get; init; }

    /// <summary>Embedding model for retrieval. text-embedding-3-small by default; override with
    /// OPENAI_EMBEDDING_MODEL.</summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>Fixed to 768 to match the existing Pinecone index (text-embedding-3 supports
    /// requesting a reduced dimension). Override with OPENAI_EMBEDDING_DIMENSIONS only alongside a
    /// matching index.</summary>
    public required int EmbeddingDimensions { get; init; }
}

public sealed class ElevenLabsSettings
{
    public required string ApiKey { get; init; }

    /// <summary>The voice to speak with - from the ElevenLabs dashboard (Voices). For Thai, pick a
    /// voice built on a multilingual model.</summary>
    public required string VoiceId { get; init; }

    /// <summary>eleven_multilingual_v2 (good quality, supports Thai) by default. Swap to
    /// eleven_turbo_v2_5 for lower latency, or eleven_v3 for the newest model, via ELEVENLABS_MODEL.</summary>
    public required string Model { get; init; }
}

public sealed class PineconeCredentials
{
    public required string ApiKey { get; init; }

    /// <summary>The index's own data-plane host, from the Pinecone console after creating an
    /// index there (e.g. "my-index-abcd123.svc.us-east-1-aws.pinecone.io") - not the same as
    /// the control-plane host used to create/list indexes.</summary>
    public required string IndexHost { get; init; }
}

public sealed class HuaweiObsCredentials
{
    public required string Endpoint { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string Bucket { get; init; }
    public required string Region { get; init; }
}

/// <summary>Mirrors src/config/env.ts's requireEnv()-backed getters.</summary>
public class MissingEnvException(string[] missing) : Exception(
    $"Missing required environment variable(s): {string.Join(", ", missing)}. See docs/ENVIRONMENT_SETUP.md for where to get each value.");

public static class ExternalServiceEnv
{
    private static string Require(params string[] keys)
    {
        var missing = keys.Where(k => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k))).ToArray();
        if (missing.Length > 0)
        {
            throw new MissingEnvException(missing);
        }
        return keys[0];
    }

    public static GoogleServiceAccountCredentials GetGoogleServiceAccount()
    {
        Require("GOOGLE_SERVICE_ACCOUNT_PROJECT_ID", "GOOGLE_SERVICE_ACCOUNT_EMAIL", "GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY");
        return new GoogleServiceAccountCredentials
        {
            ProjectId = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_PROJECT_ID")!,
            ClientEmail = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_EMAIL")!,
            // .env files can't hold real newlines - the key is pasted with literal "\n" sequences.
            PrivateKey = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY")!.Replace("\\n", "\n"),
        };
    }

    public static GeminiCredentials GetGemini()
    {
        Require("GEMINI_API_KEY");
        return new GeminiCredentials
        {
            ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")!,
            // gemini-1.5-flash is retired - verified gemini-flash-latest works against the real API.
            Model = Environment.GetEnvironmentVariable("GEMINI_MODEL") is { Length: > 0 } m ? m : "gemini-flash-latest",
        };
    }

    public static OpenAiCredentials GetOpenAi()
    {
        Require("OPENAI_API_KEY");
        return new OpenAiCredentials
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
            BaseUrl = (Environment.GetEnvironmentVariable("OPENAI_BASE_URL") is { Length: > 0 } b ? b : "https://api.openai.com/v1").TrimEnd('/'),
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") is { Length: > 0 } m ? m : "gpt-4o-mini",
            EmbeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") is { Length: > 0 } em ? em : "text-embedding-3-small",
            EmbeddingDimensions = int.TryParse(Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DIMENSIONS"), out var d) && d > 0 ? d : 768,
        };
    }

    public static EdgeTtsSettings GetEdgeTts() => new()
    {
        Voice = Environment.GetEnvironmentVariable("EDGE_TTS_VOICE") is { Length: > 0 } v ? v : "th-TH-PremwadeeNeural",
        // -10% verified to sound more natural for instructional narration than the raw default rate.
        Rate = Environment.GetEnvironmentVariable("EDGE_TTS_RATE") is { Length: > 0 } r ? r : "-10%",
    };

    public static ElevenLabsSettings GetElevenLabs()
    {
        Require("ELEVENLABS_API_KEY", "ELEVENLABS_VOICE_ID");
        return new ElevenLabsSettings
        {
            ApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY")!,
            VoiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID")!,
            Model = Environment.GetEnvironmentVariable("ELEVENLABS_MODEL") is { Length: > 0 } m ? m : "eleven_multilingual_v2",
        };
    }

    public static PineconeCredentials GetPinecone()
    {
        Require("PINECONE_API_KEY", "PINECONE_INDEX_HOST");
        return new PineconeCredentials
        {
            ApiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY")!,
            IndexHost = Environment.GetEnvironmentVariable("PINECONE_INDEX_HOST")!,
        };
    }

    public static HuaweiObsCredentials GetHuaweiObs()
    {
        Require("HUAWEI_OBS_ENDPOINT", "HUAWEI_OBS_ACCESS_KEY", "HUAWEI_OBS_SECRET_KEY", "HUAWEI_OBS_BUCKET", "HUAWEI_OBS_REGION");
        return new HuaweiObsCredentials
        {
            Endpoint = Environment.GetEnvironmentVariable("HUAWEI_OBS_ENDPOINT")!,
            AccessKey = Environment.GetEnvironmentVariable("HUAWEI_OBS_ACCESS_KEY")!,
            SecretKey = Environment.GetEnvironmentVariable("HUAWEI_OBS_SECRET_KEY")!,
            Bucket = Environment.GetEnvironmentVariable("HUAWEI_OBS_BUCKET")!,
            Region = Environment.GetEnvironmentVariable("HUAWEI_OBS_REGION")!,
        };
    }
}
