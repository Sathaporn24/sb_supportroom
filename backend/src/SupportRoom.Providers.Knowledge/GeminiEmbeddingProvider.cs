using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Knowledge;

/// <summary>
/// Plain REST call (no SDK, matches the rest of this codebase's external-API style) to Gemini's
/// embedContent endpoint. Reuses GEMINI_API_KEY - embeddings are a Gemini capability too, no new
/// vendor/credential needed. outputDimensionality is fixed at 768 (a good accuracy/cost balance)
/// and must match whatever dimension the Pinecone index was created with - see
/// docs on KNOWLEDGE_PROVIDER=pinecone setup.
/// </summary>
public sealed class GeminiEmbeddingProvider(IHttpClientFactory httpClientFactory, ILogger<GeminiEmbeddingProvider> logger) : IEmbeddingProvider
{
    private const string Model = "gemini-embedding-001";
    private const int OutputDimensionality = 768;

    // Same transient statuses Gemini's generateContent returns under load (503/429) - retry a couple
    // of times so a spike doesn't fail the whole RAG answer at the embedding step. 4xx isn't retried.
    private static readonly int[] RetryableStatuses = [429, 500, 503];
    private const int MaxAttempts = 3;

    private sealed class EmbedResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingValues? Embedding { get; init; }

        public sealed class EmbeddingValues
        {
            [JsonPropertyName("values")]
            public List<float>? Values { get; init; }
        }
    }

    // Gemini's API spelling of each task type. Sent on every call so stored documents and the
    // queries that search them land in the same retrieval-tuned embedding space (see
    // EmbeddingTaskType) - the whole reason task types improve relevance.
    private static string ToApiTaskType(EmbeddingTaskType taskType) => taskType switch
    {
        EmbeddingTaskType.RetrievalDocument => "RETRIEVAL_DOCUMENT",
        EmbeddingTaskType.RetrievalQuery => "RETRIEVAL_QUERY",
        _ => throw new ArgumentOutOfRangeException(nameof(taskType), taskType, "Unhandled embedding task type."),
    };

    public async Task<float[]> EmbedAsync(string text, EmbeddingTaskType taskType)
    {
        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var creds = ExternalServiceEnv.GetGemini();
                var client = httpClientFactory.CreateClient(nameof(GeminiEmbeddingProvider));
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:embedContent?key={creds.ApiKey}";

                var response = await client.PostAsJsonAsync(url, new
                {
                    content = new { parts = new[] { new { text } } },
                    taskType = ToApiTaskType(taskType),
                    outputDimensionality = OutputDimensionality,
                });

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
                            "gemini", "embedContent", status, attempt, MaxAttempts, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw new HttpRequestException($"Gemini embedding request failed ({status}): {trimmed}");
                }

                var data = await response.Content.ReadFromJsonAsync<EmbedResponse>();
                logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms (attempt {Attempt})", "gemini", "embedContent", stopwatch.ElapsedMilliseconds, attempt);
                return data?.Embedding?.Values?.ToArray() ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                    "gemini", "embedContent", stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
