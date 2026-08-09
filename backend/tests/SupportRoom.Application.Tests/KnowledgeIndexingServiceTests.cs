using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Services;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Application.Tests;

/// <summary>
/// Exercises IndexChunksAsync against the real Gemini embedding + Pinecone index (Mock has been
/// removed) under a disposable per-test namespace - deleted again in DisposeAsync so test data
/// doesn't accumulate in the real index. Specifically guards the switch from a sequential
/// foreach-await to Parallel.ForEachAsync (see IKnowledgeIndexingService.cs): every chunk must
/// still make it into the index and come back out correctly, regardless of the order embeddings
/// complete in.
/// </summary>
public class KnowledgeIndexingServiceTests : IAsyncLifetime
{
    private readonly GeminiEmbeddingProvider _embedding = new(new RealHttpClientFactory(), NullLogger<GeminiEmbeddingProvider>.Instance);
    private readonly PineconeKnowledgeIndexProvider _index = new(new RealHttpClientFactory(), NullLogger<PineconeKnowledgeIndexProvider>.Instance);
    private readonly KnowledgeIndexingService _service;
    private readonly List<string> _namespacesToDelete = [];

    public KnowledgeIndexingServiceTests()
    {
        _service = new KnowledgeIndexingService(_embedding, _index, NullLogger<IKnowledgeIndexingService>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var namespaceKey in _namespacesToDelete)
        {
            await _index.DeleteNamespaceAsync(namespaceKey);
        }
    }

    [Fact]
    public async Task IndexChunksAsync_IndexesEveryChunk_DespiteRunningEmbedsInParallel()
    {
        var namespaceKey = $"test-{Guid.NewGuid()}";
        _namespacesToDelete.Add(namespaceKey);

        var chunks = Enumerable.Range(1, 8)
            .Select(i => new KnowledgeSourceChunk { Id = $"chunk-{i}", Text = $"หน้าที่ {i} พูดถึงหัวข้อทดสอบหมายเลข {i}" })
            .ToList();

        var indexedCount = await _service.IndexChunksAsync(namespaceKey, chunks);

        Assert.Equal(chunks.Count, indexedCount);

        // Pinecone upserts are eventually consistent - a short pause avoids a false negative
        // from querying before the just-written vectors are visible.
        await Task.Delay(TimeSpan.FromSeconds(2));

        var queryVector = await _embedding.EmbedAsync("หัวข้อทดสอบหมายเลข 5", EmbeddingTaskType.RetrievalQuery);
        var results = await _index.QueryAsync(namespaceKey, queryVector, topK: chunks.Count);
        var resultIds = results.Select(r => r.Id).ToHashSet();

        Assert.All(chunks, c => Assert.Contains(c.Id, resultIds));
    }

    [Fact]
    public async Task IndexChunksAsync_SkipsBlankChunks_AndReturnsZero_WhenNothingIsIndexable()
    {
        // A blank-only chunk list never reaches UpsertAsync at all (see IndexChunksAsync), so
        // there's no namespace created here to clean up in DisposeAsync.
        var namespaceKey = $"test-{Guid.NewGuid()}";

        var indexedCount = await _service.IndexChunksAsync(namespaceKey, [new KnowledgeSourceChunk { Id = "blank", Text = "   " }]);

        Assert.Equal(0, indexedCount);
    }
}
