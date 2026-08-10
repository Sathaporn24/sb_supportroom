using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Knowledge;

/// <summary>
/// Plain REST call to OpenAI's /v1/embeddings (no SDK, matching this codebase's external-API style).
/// Requests a reduced <c>dimensions</c> so the vectors match the existing 768-dim Pinecone index -
/// text-embedding-3 models natively support returning a shorter vector. Used in place of
/// GeminiEmbeddingProvider when KNOWLEDGE_PROVIDER=pinecone-openai.
/// </summary>
public sealed class OpenAiEmbeddingProvider(IHttpClientFactory httpClientFactory, ILogger<OpenAiEmbeddingProvider> logger) : IEmbeddingProvider
{
    // Same transient statuses the other providers retry (429/500/503). 4xx (bad key/quota) fails fast.
    private static readonly int[] RetryableStatuses = [429, 500, 503];
    private const int MaxAttempts = 3;

    private sealed class EmbedResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingItem>? Data { get; init; }

        public sealed class EmbeddingItem
        {
            [JsonPropertyName("embedding")]
            public List<float>? Embedding { get; init; }
        }
    }

    // OpenAI embeddings have no task-type concept (unlike Gemini's RETRIEVAL_DOCUMENT/RETRIEVAL_QUERY):
    // the same model embeds both stored documents and search queries into one space. taskType is
    // accepted for interface parity but intentionally unused.
    public async Task<float[]> EmbedAsync(string text, EmbeddingTaskType taskType)
    {
        var creds = ExternalServiceEnv.GetOpenAi();
        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var client = httpClientFactory.CreateClient(nameof(OpenAiEmbeddingProvider));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{creds.BaseUrl}/embeddings")
                {
                    Content = JsonContent.Create(new
                    {
                        model = creds.EmbeddingModel,
                        input = text,
                        dimensions = creds.EmbeddingDimensions,
                    }),
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
                            "openai", "embeddings", status, attempt, MaxAttempts, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw new HttpRequestException($"OpenAI embedding request failed ({status}): {trimmed}");
                }

                var data = await response.Content.ReadFromJsonAsync<EmbedResponse>();
                logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})", "openai", "embeddings", stopwatch.ElapsedMilliseconds, attempt);
                return data?.Data?.FirstOrDefault()?.Embedding?.ToArray() ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                    "openai", "embeddings", stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
