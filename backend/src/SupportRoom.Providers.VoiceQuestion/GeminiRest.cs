using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Providers.VoiceQuestion;

/// <summary>
/// Shared Gemini REST plumbing (request envelope, response JSON shapes) - both
/// GeminiVoiceQuestionProvider (single call, full-deck grounding) and RagVoiceQuestionProvider
/// (multiple smaller calls, retrieval grounding) build on the same wire format, just with
/// different prompts and different numbers of round trips.
/// </summary>
internal static class GeminiRest
{
    private static readonly string[] AnswerStatuses =
        [AnswerStatus.Answered, AnswerStatus.NotFound, AnswerStatus.OutOfScope, AnswerStatus.NoSpeech, AnswerStatus.TranscriptionFailed];

    public static bool IsAnswerStatus(string? value) => value is not null && AnswerStatuses.Contains(value);

    public sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; init; }

        public sealed class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; init; }
        }

        public sealed class Content
        {
            [JsonPropertyName("parts")]
            public List<Part>? Parts { get; init; }
        }

        public sealed class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; init; }
        }
    }

    /// <summary>KS-9's structured-output shape - only populated by the retrieval-grounded answer
    /// prompt (RagVoiceQuestionProvider), which is the only place Q&A content is shown to the
    /// model at all.</summary>
    public sealed class GeminiConflictJson
    {
        public string? QnaId { get; init; }
        public string? SourceLabel { get; init; }
        public string? Note { get; init; }
    }

    public sealed class GeminiAnswerJson
    {
        public string? Transcript { get; init; }
        public string? Answer { get; init; }
        public string? AnswerStatus { get; init; }
        public string? RelatedSlideObjectId { get; init; }
        public GeminiConflictJson? Conflict { get; init; }
    }

    // Gemini returns 503 UNAVAILABLE ("high demand") and 429 RESOURCE_EXHAUSTED sporadically even
    // on a healthy key - both are transient, so a couple of quick retries turn a user-facing failure
    // into a slightly slower success. 4xx (bad key/quota/malformed request) is never retried.
    private static readonly int[] RetryableStatuses = [429, 500, 503];
    private const int MaxAttempts = 3;
    // A provider connection that stays open without returning a status used to inherit
    // HttpClient's 100-second default. The tutor kept playing waiting fillers during that whole
    // time, and a voice question can call Gemini twice (transcribe, then answer). Twenty seconds
    // preserves ample headroom over the measured ~9-second healthy round-trip while turning a
    // silent upstream stall into the existing 502/QUESTION_FAILED path promptly.
    internal static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// audio/mimeType left null for a text-only request (the RAG answer step). systemInstruction,
    /// when given, is sent in its own top-level request field - the same mechanism Gemini itself
    /// uses to separate "rules the caller cannot override" from user content - rather than folded
    /// into promptText. SEC-01: a rule spliced into promptText next to untrusted input is only text;
    /// nothing stops that input from containing the same fence marker and confusing the model about
    /// where the real boundary is. systemInstruction is a structurally separate field the model
    /// never mistakes for content the caller supplied - promptText itself is what should hold only
    /// the untrusted question when this is used, but this method does not enforce that; the caller
    /// decides what goes in each field.
    /// </summary>
    public static async Task<string?> CallAsync(
        IHttpClientFactory httpClientFactory, GeminiCredentials creds, ILogger logger, string promptText,
        byte[]? audio = null, string? mimeType = null, string? systemInstruction = null)
    {
        var operation = audio is not null ? "generateContent.audio" : "generateContent.text";

        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var parts = new List<object> { new { text = promptText } };
                if (audio is not null && mimeType is not null)
                {
                    parts.Add(new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(audio) } });
                }

                var client = httpClientFactory.CreateClient(nameof(GeminiRest));
                client.Timeout = RequestTimeout;
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{creds.Model}:generateContent?key={creds.ApiKey}";

                var requestBody = new Dictionary<string, object?>
                {
                    ["contents"] = new[] { new { parts = parts.ToArray() } },
                    ["generationConfig"] = new { responseMimeType = "application/json" },
                };
                if (systemInstruction is not null)
                {
                    requestBody["systemInstruction"] = new { parts = new[] { new { text = systemInstruction } } };
                }

                var response = await client.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var status = (int)response.StatusCode;
                    var errorText = await response.Content.ReadAsStringAsync();
                    var trimmed = errorText[..Math.Min(200, errorText.Length)];

                    if (RetryableStatuses.Contains(status) && attempt < MaxAttempts)
                    {
                        var delayMs = 500 * (1 << (attempt - 1)); // 500ms, then 1000ms
                        logger.LogWarning(
                            "Provider call transient failure: {Provider} {Operation} status={Status} attempt={Attempt}/{Max}, retrying in {DelayMs}ms",
                            "gemini", operation, status, attempt, MaxAttempts, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw new HttpRequestException($"Gemini request failed ({status}): {trimmed}");
                }

                var data = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                logger.LogInformation(
                    "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})", "gemini", operation, stopwatch.ElapsedMilliseconds, attempt);
                return data?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            }
            catch (Exception ex)
            {
                // Message-only, never the exception's full data - HttpRequestException above already
                // truncates the provider's error body to 200 chars and never includes promptText
                // (which can carry a teacher's question or slide notes).
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                    "gemini", operation, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
