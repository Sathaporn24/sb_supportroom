using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Tts;

/// <summary>
/// Real ElevenLabs Text to Speech API - has an SLA, unlike Edge's unofficial WebSocket (see
/// TD-001). eleven_v3 is the only model with Thai support, and it's noticeably slower per call
/// than Edge - a full slide/answer sent as one request measured several seconds to tens of
/// seconds. Chunking and running chunks concurrently (capped under the Starter-tier 3-concurrent-
/// request ceiling) cuts wall-clock to roughly the slowest chunk instead of the sum of them all.
/// This is still a commercial SLA-backed API, not a flaky unofficial one, so a chunk failure
/// surfaces immediately rather than being retried or silently skipped.
/// </summary>
public sealed class ElevenLabsTtsProvider(IHttpClientFactory httpClientFactory, ILogger<ElevenLabsTtsProvider> logger) : ITtsProvider
{
    private const int MaxChunkChars = 350;
    private const int MaxConcurrentChunks = 2;

    public async Task<TtsResult> SynthesizeAsync(TtsInput input)
    {
        var chunks = TextChunker.SplitIntoChunks(input.Text, MaxChunkChars);
        if (chunks.Count <= 1)
        {
            return await SynthesizeChunkAsync(input, input.Text);
        }

        var overall = Stopwatch.StartNew();
        var parts = new byte[chunks.Count][];
        using var gate = new SemaphoreSlim(MaxConcurrentChunks);
        await Task.WhenAll(chunks.Select(async (chunk, idx) =>
        {
            await gate.WaitAsync();
            try
            {
                var part = await SynthesizeChunkAsync(input, chunk);
                parts[idx] = part.Audio;
            }
            finally
            {
                gate.Release();
            }
        }));

        using var buffer = new MemoryStream();
        foreach (var part in parts)
        {
            buffer.Write(part);
        }

        logger.LogInformation(
            "Provider call succeeded: {Provider} {Operation} {Chunks} chunks in {ElapsedMs}ms",
            "elevenlabs", "synthesize", chunks.Count, overall.ElapsedMilliseconds);
        return new TtsResult { Audio = buffer.ToArray(), MimeType = "audio/mpeg" };
    }

    private async Task<TtsResult> SynthesizeChunkAsync(TtsInput input, string text)
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
                new { text, model_id = creds.ModelId });

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"ElevenLabs request failed ({(int)response.StatusCode}): {errorText[..Math.Min(200, errorText.Length)]}");
            }

            var audio = await response.Content.ReadAsByteArrayAsync();
            logger.LogInformation(
                "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms ({Len} chars)",
                "elevenlabs", "synthesize", stopwatch.ElapsedMilliseconds, text.Length);
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
