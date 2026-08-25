namespace SupportRoom.Domain.Configuration;

/// <summary>Mirrors src/config/tutor-config.ts's plain constants.</summary>
public static class TutorConfig
{
    public const int DefaultIntroWaitMs = 5_000;
    public const int DefaultBreathPauseMs = 500;
    public const int DefaultFinalQuestionWaitMs = 5_000;
    public const int DefaultSessionExpiryHours = 24;

    /// <summary>A LearningSession that has not moved a slide in this long is *displayed* as
    /// stalled. Never written to the database - see CORE_FEATURE_SPEC §2.6.</summary>
    public const int DefaultInactiveThresholdMinutes = 30;
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

    public static int GetInactiveThresholdMinutes() =>
        NumberEnv("INACTIVE_THRESHOLD_MINUTES", TutorConfig.DefaultInactiveThresholdMinutes);
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

public sealed class JwtSettings
{
    /// <summary>Signing key. No default on purpose - a fallback secret would mean every
    /// deployment that forgot to set it shares a key an attacker can read in this repo, and
    /// would fail silently instead of loudly.</summary>
    public required string Secret { get; init; }

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>Short-lived by design. There is no refresh token yet, so this is also how long a
    /// deactivated account keeps working - anything longer widens that window for no benefit to
    /// a back office people sign into once a day.</summary>
    public required int ExpiryMinutes { get; init; }
}

/// <summary>The very first owner account, seeded at startup only when no user exists at all.
/// Solves the chicken-and-egg problem: nobody can sign in to create the first account.</summary>
public sealed class FirstOwnerSeed
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>
/// The app's own back-office authentication settings. Separate from ExternalServiceEnv on purpose:
/// everything there is a credential for someone else's service (Google, Gemini, Pinecone), whereas
/// these are this system's own secrets. Reading them from the same class would make the name lie
/// and blur which vendor an outage belongs to.
/// </summary>
public static class AuthEnv
{
    /// <summary>Rejects a short key here rather than letting it surface as an opaque IDX10653 at
    /// the first sign-in attempt. HMAC-SHA256 gains nothing from a key shorter than its 256-bit
    /// hash, and Microsoft's handler refuses one outright.</summary>
    public const int MinSecretLength = 32;

    public static JwtSettings GetJwt()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrEmpty(secret))
        {
            throw new MissingEnvException(["JWT_SECRET"]);
        }
        if (secret.Length < MinSecretLength)
        {
            throw new MissingEnvException([$"JWT_SECRET (ต้องยาวอย่างน้อย {MinSecretLength} ตัวอักษร)"]);
        }

        return new JwtSettings
        {
            Secret = secret,
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") is { Length: > 0 } i ? i : "supportroom",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") is { Length: > 0 } a ? a : "supportroom-admin",
            ExpiryMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out var m) && m > 0 ? m : 480,
        };
    }

    /// <summary>Null when the seed variables are unset - startup then logs and skips rather than
    /// failing, so an environment that already has accounts never needs these set at all.</summary>
    public static FirstOwnerSeed? GetFirstOwnerSeed()
    {
        var email = Environment.GetEnvironmentVariable("FIRST_OWNER_EMAIL");
        var password = Environment.GetEnvironmentVariable("FIRST_OWNER_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        return new FirstOwnerSeed
        {
            Email = email.Trim(),
            Password = password,
            DisplayName = Environment.GetEnvironmentVariable("FIRST_OWNER_NAME") is { Length: > 0 } n ? n : email.Trim(),
        };
    }
}

/// <summary>
/// The tenant registry's own bootstrap problem: an owner can sign in with no companies to pick
/// from (Company has no self-registration), so the switcher and every company-scoped screen have
/// nothing to show. Defaults to School Bright itself, since it is company row zero in this system
/// (see Company.cs) - override FIRST_COMPANY_ID/FIRST_COMPANY_NAME for any deployment that isn't
/// School Bright's own.
///
/// ⚠️ A fork of this codebase for something other than School Bright's real deployment - e.g. an
/// academic thesis writeup - must override these two env vars (or blank them out) before sharing
/// the result, so "School Bright" doesn't show up as if it endorsed or commissioned that copy.
/// </summary>
public static class CompanyEnv
{
    private const string DefaultCompanyId = "schoolbright";
    private const string DefaultCompanyName = "School Bright";

    public static FirstCompanySeed GetFirstCompanySeed() => new()
    {
        Id = Environment.GetEnvironmentVariable("FIRST_COMPANY_ID") is { Length: > 0 } id ? id : DefaultCompanyId,
        Name = Environment.GetEnvironmentVariable("FIRST_COMPANY_NAME") is { Length: > 0 } name ? name : DefaultCompanyName,
    };
}

public sealed class FirstCompanySeed
{
    public required string Id { get; init; }
    public required string Name { get; init; }
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

public sealed class ElevenLabsCredentials
{
    public required string ApiKey { get; init; }
    public required string VoiceId { get; init; }

    /// <summary>eleven_v3 by default - verified against elevenlabs.io/docs that this is the only
    /// ElevenLabs model that supports Thai; eleven_multilingual_v2 and eleven_flash_v2.5 do not,
    /// despite the "multilingual" name. Do not change the default to one of those.</summary>
    public required string ModelId { get; init; }
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

    /// <summary>When true, sends thinking:{type:disabled} to turn off a reasoning model's hidden
    /// reasoning pass. For grounded RAG answers this is faster (~35% on GLM-5.2) and returns cleaner
    /// JSON, with no quality loss. Off by default - only GLM/ModelArts-style endpoints accept this
    /// field; a strict OpenAI endpoint would reject an unknown param. Set OPENAI_DISABLE_REASONING=true.</summary>
    public required bool DisableReasoning { get; init; }
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
            DisableReasoning = string.Equals(Environment.GetEnvironmentVariable("OPENAI_DISABLE_REASONING"), "true", StringComparison.OrdinalIgnoreCase),
        };
    }

    public static EdgeTtsSettings GetEdgeTts() => new()
    {
        Voice = Environment.GetEnvironmentVariable("EDGE_TTS_VOICE") is { Length: > 0 } v ? v : "th-TH-PremwadeeNeural",
        // -10% verified to sound more natural for instructional narration than the raw default rate.
        Rate = Environment.GetEnvironmentVariable("EDGE_TTS_RATE") is { Length: > 0 } r ? r : "-10%",
    };

    public static ElevenLabsCredentials GetElevenLabs()
    {
        Require("ELEVENLABS_API_KEY");
        Require("ELEVENLABS_VOICE_ID");
        return new ElevenLabsCredentials
        {
            ApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY")!,
            VoiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID")!,
            // eleven_multilingual_v2 and eleven_flash_v2.5 do NOT support Thai despite the name -
            // verified live against elevenlabs.io/docs. Only the v3 family does. Plain eleven_v3
            // measured 9.4s for a 763-char Thai narration; eleven_v3_conversational measured 3.8s
            // for the same text with no audible quality loss, so it's the default here even though
            // "v3" is the name in the docs users see first.
            ModelId = Environment.GetEnvironmentVariable("ELEVENLABS_MODEL_ID") is { Length: > 0 } m ? m : "eleven_v3_conversational",
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
