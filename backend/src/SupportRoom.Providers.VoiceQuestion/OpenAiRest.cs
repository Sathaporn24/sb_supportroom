using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.VoiceQuestion;

/// <summary>
/// OpenAI chat-completions plumbing for the RAG answer step (text-only). Kept parallel to GeminiRest:
/// it returns the assistant message content as a raw JSON string so RagVoiceQuestionProvider parses
/// the answer the same way regardless of which vendor produced it. Only the answer step (3) uses
/// this - audio transcription (1) stays on Gemini.
/// </summary>
internal static class OpenAiRest
{
    // Same transient statuses the other providers retry. 4xx (bad key/quota) fails fast.
    private static readonly int[] RetryableStatuses = [429, 500, 503];
    private const int MaxAttempts = 3;

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; init; }

        public sealed class Choice
        {
            [JsonPropertyName("message")]
            public Message? Message { get; init; }
        }

        public sealed class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; init; }
        }
    }

    /// <summary>Returns the assistant's reply as a JSON string (response_format=json_object), or null.</summary>
    public static async Task<string?> CallAnswerAsync(
        IHttpClientFactory httpClientFactory, OpenAiCredentials creds, ILogger logger, string systemPrompt, string userPrompt)
    {
        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["model"] = creds.Model,
                    ["messages"] = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt },
                    },
                    ["response_format"] = new { type = "json_object" },
                    // GLM-5.2 and other reasoning models spend output tokens on hidden reasoning before
                    // the visible answer. Without a generous cap the reasoning can eat the whole budget
                    // and the JSON answer comes back truncated/empty (parses to null, surfacing as
                    // transcription_failed). 4096 leaves ample room for a short answer; harmless otherwise.
                    ["max_tokens"] = 4096,
                };
                if (creds.DisableReasoning)
                {
                    // Turns off the reasoning pass on GLM/ModelArts - faster and cleaner JSON for the
                    // grounded-answer task, which doesn't benefit from chain-of-thought.
                    payload["thinking"] = new { type = "disabled" };
                }

                var client = httpClientFactory.CreateClient(nameof(OpenAiRest));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{creds.BaseUrl}/chat/completions")
                {
                    Content = JsonContent.Create(payload),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.ApiKey);

                var response = await client.SendAsync(request);
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
                            "openai", "chat.completions", status, attempt, MaxAttempts, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw new HttpRequestException($"OpenAI request failed ({status}): {trimmed}");
                }

                var data = await response.Content.ReadFromJsonAsync<ChatResponse>();
                logger.LogInformation(
                    "Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})", "openai", "chat.completions", stopwatch.ElapsedMilliseconds, attempt);
                return data?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch (Exception ex)
            {
                // Message-only - never the prompt (carries the teacher's question / slide notes).
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                    "openai", "chat.completions", stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
