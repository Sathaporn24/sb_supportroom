using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Tts;

/// <summary>
/// ElevenLabs text-to-speech, selected by TTS_PROVIDER=elevenlabs. A drop-in alternative to
/// EdgeTtsProvider on the exact same ITtsProvider contract - it returns MP3 (audio/mpeg), the
/// format the room's &lt;audio&gt; element already plays, so switching needs no frontend change.
/// Entirely independent of the document/RAG providers (separate assembly, separate env switch);
/// swapping the voice engine touches nothing in the knowledge pipeline.
/// </summary>
public sealed class ElevenLabsTtsProvider(IHttpClientFactory httpClientFactory, ILogger<ElevenLabsTtsProvider> logger) : ITtsProvider
{
    // Transient statuses worth a retry (rate limit / upstream blips) - a 4xx like 401 (bad key)
    // or 422 (bad voice id) is a config error and must fail fast, not retry. Mirrors the retry
    // shape GeminiEmbeddingProvider already uses.
    private static readonly int[] RetryableStatuses = [429, 500, 502, 503];
    private const int MaxAttempts = 3;

    private sealed class VoiceSettings
    {
        [JsonPropertyName("stability")] public double Stability { get; init; } = 0.5;
        [JsonPropertyName("similarity_boost")] public double SimilarityBoost { get; init; } = 0.75;

        /// <summary>ElevenLabs speed multiplier (1.0 = normal), supported range 0.7-1.2.</summary>
        [JsonPropertyName("speed")] public double Speed { get; init; } = 1.0;
    }

    private sealed class TtsRequest
    {
        [JsonPropertyName("text")] public required string Text { get; init; }
        [JsonPropertyName("model_id")] public required string ModelId { get; init; }
        [JsonPropertyName("voice_settings")] public required VoiceSettings VoiceSettings { get; init; }
    }

    public async Task<TtsResult> SynthesizeAsync(TtsInput input)
    {
        var settings = ExternalServiceEnv.GetElevenLabs();
        var voiceId = input.Voice is { Length: > 0 } v ? v : settings.VoiceId;
        var speed = MapRateToSpeed(input.Rate);

        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var client = httpClientFactory.CreateClient(nameof(ElevenLabsTtsProvider));
                var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=mp3_44100_128";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("xi-api-key", settings.ApiKey);
                request.Headers.Add("Accept", "audio/mpeg");
                request.Content = JsonContent.Create(new TtsRequest
                {
                    Text = input.Text,
                    ModelId = settings.Model,
                    VoiceSettings = new VoiceSettings { Speed = speed },
                });

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var status = (int)response.StatusCode;
                    var errorText = await response.Content.ReadAsStringAsync();
                    var trimmed = errorText[..Math.Min(200, errorText.Length)];

                    if (RetryableStatuses.Contains(status) && attempt < MaxAttempts)
                    {
                        logger.LogWarning(
                            "Provider call transient failure: {Provider} {Operation} status={Status} attempt={Attempt}/{Max}",
                            "elevenlabs", "synthesize", status, attempt, MaxAttempts);
                        await Task.Delay(500 * (1 << (attempt - 1))); // 500ms, then 1000ms
                        continue;
                    }

                    throw new HttpRequestException($"ElevenLabs TTS request failed ({status}): {trimmed}");
                }

                var audio = await response.Content.ReadAsByteArrayAsync();
                logger.LogInformation(
                    "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})",
                    "elevenlabs", "synthesize", stopwatch.ElapsedMilliseconds, attempt);
                return new TtsResult { Audio = audio, MimeType = "audio/mpeg" };
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                    "elevenlabs", "synthesize", stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// TtsInput.Rate is Edge's SSML percentage ("-10%", "-45%") for utterances that shouldn't run
    /// at lesson pace. ElevenLabs has no SSML rate - it uses a numeric speed multiplier - so map
    /// the percentage onto that ("-10%" -> 0.9), clamped to ElevenLabs' supported 0.7-1.2 range.
    /// An absent or unparseable rate means normal speed.
    /// </summary>
    private static double MapRateToSpeed(string? rate)
    {
        if (string.IsNullOrWhiteSpace(rate))
        {
            return 1.0;
        }
        var trimmed = rate.Trim().TrimEnd('%');
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
            ? Math.Clamp(1.0 + pct / 100.0, 0.7, 1.2)
            : 1.0;
    }
}
