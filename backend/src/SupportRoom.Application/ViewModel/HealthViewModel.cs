using System.Text.Json.Serialization;

namespace SupportRoom.Application.ViewModel;

/// <summary>
/// Mirrors src/app/api/health/route.ts exactly, including the inconsistency: the "providers"
/// keys are the raw SCREAMING_SNAKE_CASE env-var names (getProviderSelection()'s zod schema
/// keys), NOT camelCase like the rest of the app's JSON - hence the explicit JsonPropertyName
/// overrides instead of relying on the app-wide camelCase naming policy.
/// </summary>
public sealed class HealthProvidersViewModel
{
    [JsonPropertyName("SLIDES_PROVIDER")]
    public required string SlidesProvider { get; init; }

    [JsonPropertyName("TTS_PROVIDER")]
    public required string TtsProvider { get; init; }

    [JsonPropertyName("VOICE_QUESTION_PROVIDER")]
    public required string VoiceQuestionProvider { get; init; }
}

public sealed class HealthViewModel
{
    public required string Status { get; init; }
    public required HealthProvidersViewModel Providers { get; init; }
    public required string Timestamp { get; init; }
}
