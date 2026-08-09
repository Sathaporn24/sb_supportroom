using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Knowledge;

/// <summary>
/// Plain REST calls (no SDK) to Pinecone's data-plane API against the index host from
/// ExternalServiceEnv.GetPinecone() - see docs on KNOWLEDGE_PROVIDER=pinecone setup for how to
/// create the index and get that host. Pinecone doesn't return the original chunk text on
/// query, only metadata, so the chunk's Text is folded into its own metadata on upsert and read
/// back out on query.
/// </summary>
public sealed class PineconeKnowledgeIndexProvider(IHttpClientFactory httpClientFactory, ILogger<PineconeKnowledgeIndexProvider> logger) : IKnowledgeIndexProvider
{
    private const string TextMetadataKey = "__text";

    private sealed class UpsertRequest
    {
        [JsonPropertyName("vectors")]
        public required List<UpsertVector> Vectors { get; init; }

        [JsonPropertyName("namespace")]
        public required string Namespace { get; init; }
    }

    private sealed class UpsertVector
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("values")]
        public required float[] Values { get; init; }

        [JsonPropertyName("metadata")]
        public required Dictionary<string, string> Metadata { get; init; }
    }

    private sealed class QueryRequest
    {
        [JsonPropertyName("namespace")]
        public required string Namespace { get; init; }

        [JsonPropertyName("vector")]
        public required float[] Vector { get; init; }

        [JsonPropertyName("topK")]
        public required int TopK { get; init; }

        [JsonPropertyName("includeMetadata")]
        public bool IncludeMetadata { get; init; } = true;
    }

    private sealed class QueryResponse
    {
        [JsonPropertyName("matches")]
        public List<Match>? Matches { get; init; }

        public sealed class Match
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("score")]
            public float Score { get; init; }

            [JsonPropertyName("metadata")]
            public Dictionary<string, string>? Metadata { get; init; }
        }
    }

    private sealed class DeleteRequest
    {
        [JsonPropertyName("deleteAll")]
        public bool DeleteAll { get; init; } = true;

        [JsonPropertyName("namespace")]
        public required string Namespace { get; init; }
    }

    public async Task UpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await TimedCallAsync("upsert", async () =>
        {
            var client = CreateClient();
            var vectors = chunks.Select(c =>
            {
                var metadata = c.Metadata is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(c.Metadata);
                metadata[TextMetadataKey] = c.Text;
                return new UpsertVector { Id = c.Id, Values = c.Vector, Metadata = metadata };
            }).ToList();

            var response = await client.PostAsJsonAsync(
                "/vectors/upsert", new UpsertRequest { Vectors = vectors, Namespace = namespaceKey });
            await EnsureSuccessAsync(response, "upsert");
            return true;
        });
    }

    public Task<IReadOnlyList<ScoredChunk>> QueryAsync(string namespaceKey, float[] queryVector, int topK) => TimedCallAsync("query", async () =>
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/query", new QueryRequest { Namespace = namespaceKey, Vector = queryVector, TopK = topK });
        await EnsureSuccessAsync(response, "query");

        var data = await response.Content.ReadFromJsonAsync<QueryResponse>();
        if (data?.Matches is null)
        {
            return (IReadOnlyList<ScoredChunk>)[];
        }

        return data.Matches.Select(m =>
        {
            var metadata = m.Metadata ?? [];
            var text = metadata.TryGetValue(TextMetadataKey, out var t) ? t : "";
            var metadataWithoutText = metadata.Where(kv => kv.Key != TextMetadataKey).ToDictionary(kv => kv.Key, kv => kv.Value);
            return new ScoredChunk { Id = m.Id ?? "", Text = text, Score = m.Score, Metadata = metadataWithoutText };
        }).ToList();
    });

    public async Task DeleteNamespaceAsync(string namespaceKey)
    {
        await TimedCallAsync("delete", async () =>
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("/vectors/delete", new DeleteRequest { Namespace = namespaceKey });
            await EnsureSuccessAsync(response, "delete");
            return true;
        });
    }

    private async Task<T> TimedCallAsync<T>(string operation, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await call();
            logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms", "pinecone", operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                "pinecone", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private HttpClient CreateClient()
    {
        var creds = ExternalServiceEnv.GetPinecone();
        var client = httpClientFactory.CreateClient(nameof(PineconeKnowledgeIndexProvider));
        client.BaseAddress = new Uri($"https://{creds.IndexHost}");
        client.DefaultRequestHeaders.Remove("Api-Key");
        client.DefaultRequestHeaders.Add("Api-Key", creds.ApiKey);
        client.DefaultRequestHeaders.Remove("X-Pinecone-API-Version");
        client.DefaultRequestHeaders.Add("X-Pinecone-API-Version", "2024-07");
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Pinecone {operation} failed ({(int)response.StatusCode}): {errorText[..Math.Min(200, errorText.Length)]}");
        }
    }
}
