using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Providers.Tests;

/// <summary>
/// Proves the fix described in IKnowledgeIndexProvider's own doc comment: a standalone document
/// (indexed into kb-global, no lessonSlug) must still answer a question asked inside an unrelated
/// lesson. This exercises MergeTopK against the real Gemini embedding + Pinecone index (Mock has
/// been removed) under disposable per-test namespaces - never the real "kb-global" namespace
/// production data lives in - deleted again in DisposeAsync so test data doesn't accumulate in
/// the real index.
/// </summary>
public class RagVoiceQuestionProviderMergeTests : IAsyncLifetime
{
    private readonly GeminiEmbeddingProvider _embedding = new(new RealHttpClientFactory(), NullLogger<GeminiEmbeddingProvider>.Instance);
    private readonly PineconeKnowledgeIndexProvider _index = new(new RealHttpClientFactory(), NullLogger<PineconeKnowledgeIndexProvider>.Instance);
    private readonly List<string> _namespacesToDelete = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var namespaceKey in _namespacesToDelete)
        {
            await _index.DeleteNamespaceAsync(namespaceKey);
        }
    }

    // Only this one is tagged: the topK test below is pure merge arithmetic over in-memory
    // matches and never touches Gemini or Pinecone.
    [Fact]
    [Trait(TestCategories.Category, TestCategories.Integration)]
    public async Task MergeTopK_PullsAStandaloneDocumentChunkIntoAnUnrelatedLessonsAnswer()
    {
        var lessonNamespace = $"test-lesson-{Guid.NewGuid()}";
        var globalNamespace = $"test-global-{Guid.NewGuid()}";
        _namespacesToDelete.Add(lessonNamespace);
        _namespacesToDelete.Add(globalNamespace);

        // Lesson content: nothing about warranties.
        var loginText = "แตะปุ่มเข้าสู่ระบบเพื่อเข้าใช้งาน";
        await _index.UpsertAsync(lessonNamespace, [new KnowledgeChunk { Id = "slide-1", Text = loginText, Vector = await _embedding.EmbedAsync(loginText, EmbeddingTaskType.RetrievalDocument) }]);

        // Standalone document (kb-global-shaped, but under a disposable test namespace) -
        // uploaded once, tied to no lesson.
        var warrantyText = "หูฟังไร้สายรุ่น SB-Pro: ระยะเวลาประกัน(เดือน): 36";
        await _index.UpsertAsync(globalNamespace, [new KnowledgeChunk { Id = "doc-1-row", Text = warrantyText, Vector = await _embedding.EmbedAsync(warrantyText, EmbeddingTaskType.RetrievalDocument) }]);

        // Pinecone upserts are eventually consistent - a short pause before querying avoids a
        // false negative from querying before the vectors just written are visible.
        await Task.Delay(TimeSpan.FromSeconds(2));

        var queryVector = await _embedding.EmbedAsync("หูฟังไร้สายรุ่น SB-Pro มีประกันกี่เดือน", EmbeddingTaskType.RetrievalQuery);
        var lessonChunks = await _index.QueryAsync(lessonNamespace, queryVector, topK: 3);
        var globalChunks = await _index.QueryAsync(globalNamespace, queryVector, topK: 3);

        var merged = RagVoiceQuestionProvider.MergeTopK(lessonChunks, globalChunks, topK: 3);

        Assert.Contains(merged, c => c.Id == "doc-1-row");
        // The warranty chunk should rank above the unrelated login-flow chunk for this question.
        Assert.Equal("doc-1-row", merged[0].Id);
    }

    [Fact]
    public void MergeTopK_RespectsTopKAcrossBothNamespacesCombined()
    {
        var lessonChunks = new List<ScoredChunk>
        {
            new() { Id = "a", Text = "a", Score = 0.9f },
            new() { Id = "b", Text = "b", Score = 0.5f },
        };
        var globalChunks = new List<ScoredChunk>
        {
            new() { Id = "c", Text = "c", Score = 0.8f },
            new() { Id = "d", Text = "d", Score = 0.1f },
        };

        var merged = RagVoiceQuestionProvider.MergeTopK(lessonChunks, globalChunks, topK: 3);

        Assert.Equal(["a", "c", "b"], merged.Select(c => c.Id));
    }
}
