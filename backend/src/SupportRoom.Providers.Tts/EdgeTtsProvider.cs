using System.Diagnostics;
using EdgeTTS.DotNet;
using EdgeTTS.DotNet.Models;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Tts;

/// <summary>
/// Real Microsoft Edge Read-Aloud integration - no API key needed. Mirrors src/providers/tts/
/// edge-tts-provider.ts (same default voice/rate).
/// </summary>
public sealed class EdgeTtsProvider(ILogger<EdgeTtsProvider> logger) : ITtsProvider
{
    // The WebSocket to Microsoft's Edge Read-Aloud service occasionally drops mid-stream
    // ("WebSocket receive error", observed live hanging 36s before failing with no cap) - unlike
    // Gemini's HTTP status codes there's no way to tell transient from permanent here, so retry
    // any failure with the same short backoff GeminiRest uses, and cap each attempt so one hung
    // socket can't block a lesson's narration for tens of seconds.
    //
    // Two attempts, not three: when the service is throttling a burst, every attempt hangs the
    // full timeout, so a third only adds dead wait (seen live: 3x15s = 46s before a 502). One
    // retry still recovers a single transient drop, which is what the retry is actually for.
    // 12s per attempt clears the slowest real synthesis observed (~7.8s for a long slide's
    // notes) with margin, while capping the worst case at ~24s instead of ~46s.
    private const int MaxAttempts = 2;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(12);

    public async Task<TtsResult> SynthesizeAsync(TtsInput input)
    {
        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await SynthesizeOnceAsync(input).WaitAsync(AttemptTimeout);
                logger.LogInformation(
                    "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})",
                    "edge-tts", "synthesize", stopwatch.ElapsedMilliseconds, attempt);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms attempt={Attempt}/{Max} - {Error}",
                    "edge-tts", "synthesize", stopwatch.ElapsedMilliseconds, attempt, MaxAttempts, ex.Message);

                if (attempt >= MaxAttempts)
                {
                    throw;
                }
                await Task.Delay(500 * (1 << (attempt - 1))); // 500ms, then 1000ms
            }
        }
    }

    private static async Task<TtsResult> SynthesizeOnceAsync(TtsInput input)
    {
        var defaults = ExternalServiceEnv.GetEdgeTts();
        var communicate = new Communicate(input.Text, voice: input.Voice ?? defaults.Voice, rate: input.Rate ?? defaults.Rate);

        using var buffer = new MemoryStream();
        await foreach (var chunk in communicate.StreamAsync())
        {
            if (chunk is AudioChunk audio)
            {
                buffer.Write(audio.Data);
            }
        }
        return new TtsResult { Audio = buffer.ToArray(), MimeType = "audio/mpeg" };
    }
}
