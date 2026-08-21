using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Tts;

/// <summary>
/// Real ElevenLabs Text to Speech API - has an SLA, unlike Edge's unofficial WebSocket (see
/// TD-001). A single HTTP POST per call, no chunking/retry: this is a commercial API, not a
/// flaky unofficial one, so a failure here should surface to the caller's existing graceful-
/// degradation path immediately rather than be masked by retries.
/// </summary>
public sealed class ElevenLabsTtsProvider(IHttpClientFactory httpClientFactory, ILogger<ElevenLabsTtsProvider> logger) : ITtsProvider
{
    public async Task<TtsResult> SynthesizeAsync(TtsInput input)
    {
        var creds = ExternalServiceEnv.GetElevenLabs();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient(nameof(ElevenLabsTtsProvider));
            client.DefaultRequestHeaders.Remove("xi-api-key");
            client.DefaultRequestHeaders.Add("xi-api-key", creds.ApiKey);

            var voiceId = input.Voice ?? creds.VoiceId;
            var response = await client.PostAsJsonAsync(
                $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}",
                new { text = input.Text, model_id = creds.ModelId });

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"ElevenLabs request failed ({(int)response.StatusCode}): {errorText[..Math.Min(200, errorText.Length)]}");
            }

            var audio = await response.Content.ReadAsByteArrayAsync();
            logger.LogInformation(
                "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms ({Len} chars)",
                "elevenlabs", "synthesize", stopwatch.ElapsedMilliseconds, input.Text.Length);
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
