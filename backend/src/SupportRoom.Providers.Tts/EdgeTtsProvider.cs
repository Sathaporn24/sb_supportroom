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
    private const int MaxAttempts = 2;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(12);

    // Edge hangs far more often on long text than short. A whole slide's speaker notes (or the
    // resume-bridge + notes after a question) is exactly the request that used to time out, so
    // synthesize it as several short chunks instead: each chunk is well under the timeout, one
    // flaky chunk is skipped rather than silencing the whole narration, and MP3 byte streams
    // concatenate cleanly (edge-tts itself already returns audio as concatenated chunks).
    private const int MaxChunkChars = 180;

    public async Task<TtsResult> SynthesizeAsync(TtsInput input)
    {
        var chunks = TextChunker.SplitIntoChunks(input.Text, MaxChunkChars);
        if (chunks.Count <= 1)
        {
            return await SynthesizeChunkWithRetryAsync(input, input.Text);
        }

        var overall = Stopwatch.StartNew();
        // Synthesize chunks concurrently (order preserved by index) so a multi-sentence narration
        // costs about the slowest chunk, not the sum of them all. Bounded to a few at a time so we
        // don't open a dozen Edge sockets at once and trip its burst throttling.
        byte[]?[] parts = new byte[chunks.Count][];
        using var gate = new SemaphoreSlim(3);
        await Task.WhenAll(chunks.Select(async (chunk, idx) =>
        {
            await gate.WaitAsync();
            try
            {
                var part = await SynthesizeChunkWithRetryAsync(input, chunk);
                parts[idx] = part.Audio;
            }
            catch (Exception ex)
            {
                // A single flaky chunk shouldn't take the whole narration down - skip it and keep
                // going so the rest of the sentence still gets spoken.
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} chunk ({Len} chars) skipped - {Error}",
                    "edge-tts", "synthesize", chunk.Length, ex.Message);
            }
            finally
            {
                gate.Release();
            }
        }));

        using var buffer = new MemoryStream();
        var ok = 0;
        foreach (var part in parts)
        {
            if (part is not null)
            {
                buffer.Write(part);
                ok++;
            }
        }

        if (ok == 0)
        {
            // Every chunk failed - surface it so the caller's graceful-degradation path runs.
            throw new InvalidOperationException("edge-tts: all chunks failed");
        }

        logger.LogInformation(
            "Provider call succeeded: {Provider} {Operation} {Ok}/{Total} chunks in {ElapsedMs}ms",
            "edge-tts", "synthesize", ok, chunks.Count, overall.ElapsedMilliseconds);
        return new TtsResult { Audio = buffer.ToArray(), MimeType = "audio/mpeg" };
    }

    private async Task<TtsResult> SynthesizeChunkWithRetryAsync(TtsInput input, string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await SynthesizeOnceAsync(input, text).WaitAsync(AttemptTimeout);
                logger.LogInformation(
                    "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt}, {Len} chars)",
                    "edge-tts", "synthesize", stopwatch.ElapsedMilliseconds, attempt, text.Length);
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

    private static async Task<TtsResult> SynthesizeOnceAsync(TtsInput input, string text)
    {
        var defaults = ExternalServiceEnv.GetEdgeTts();
        var communicate = new Communicate(text, voice: input.Voice ?? defaults.Voice, rate: input.Rate ?? defaults.Rate);

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
