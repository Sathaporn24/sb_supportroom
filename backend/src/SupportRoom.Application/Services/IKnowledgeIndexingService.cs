using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Application.Services;

public sealed class KnowledgeSourceChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }

    /// <summary>The text actually sent to the embedding provider - null means "use Text" (every
    /// caller before Q&A). For a Q&A (KS-5), this is set to just the Question: retrieval is a
    /// question-to-question match, so embedding the Answer too would dilute that signal. Text
    /// stays the full "ถาม: ...\nตอบ: ..." pair, because that is what a prompt needs to see.</summary>
    public string? EmbedText { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public interface IKnowledgeIndexingService
{
    Task IndexLessonAsync(string namespaceKey, IReadOnlyList<ResolvedSlide> slides);

    /// <summary>Shared embed-then-upsert core. Returns the number of chunks actually indexed
    /// (blank-text chunks are skipped). Never throws - logs and returns 0 on failure, so a broken
    /// embedding/index call never blocks CS from saving a lesson.</summary>
    Task<int> IndexChunksAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks);

    /// <summary>Same embed-then-upsert work as IndexChunksAsync, for the one caller that needs to
    /// tell embedding failures apart from upsert failures instead of a single swallowed exception
    /// (design.md DI-5 - IBackgroundJobProcessor maps each to a different DocumentFailureReason).
    /// Unlike IndexChunksAsync this throws: KnowledgeEmbeddingFailedException if any embed call
    /// fails, KnowledgeIndexUpsertFailedException if the Pinecone upsert fails. All-blank input
    /// returns 0 without calling either provider.</summary>
    Task<int> EmbedAndUpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks);
}

public sealed class KnowledgeEmbeddingFailedException(Exception inner)
    : Exception("Embedding provider call failed", inner);

public sealed class KnowledgeIndexUpsertFailedException(Exception inner)
    : Exception("Knowledge index upsert failed", inner);

/// <summary>
/// One chunk per slide for lessons (SlideObjectId + SpeakerNotes) - notes are already short and
/// self-contained per slide, so no further splitting logic is needed. Never throws: a broken
/// embedding/index call must not block CS from saving a lesson, it should just leave the
/// knowledge store stale until the next successful save.
/// </summary>
public sealed class KnowledgeIndexingService(
    IEmbeddingProvider embeddingProvider,
    IKnowledgeIndexProvider knowledgeIndexProvider,
    ILogger<IKnowledgeIndexingService> logger) : IKnowledgeIndexingService
{
    public async Task IndexLessonAsync(string namespaceKey, IReadOnlyList<ResolvedSlide> slides)
    {
        var chunks = slides
            .Where(s => !string.IsNullOrWhiteSpace(s.SpeakerNotes))
            .Select(s => new KnowledgeSourceChunk
            {
                Id = s.SlideObjectId,
                Text = s.SpeakerNotes,
                Metadata = new Dictionary<string, string>
                {
                    ["slideObjectId"] = s.SlideObjectId,
                    ["index"] = s.Index.ToString(),
                    ["sourceType"] = KnowledgeSourceType.Slide,
                },
            })
            .ToList();

        await IndexChunksAsync(namespaceKey, chunks);
    }

    /// <summary>Caps how many embed calls run at once - a large document (many pages/chunks)
    /// shouldn't fire dozens of concurrent requests at Gemini's API in one burst.</summary>
    private const int MaxConcurrentEmbeds = 5;

    public async Task<int> IndexChunksAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks)
    {
        try
        {
            return await EmbedAndUpsertAsync(namespaceKey, chunks);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index into knowledge store namespace {Namespace}", namespaceKey);
            return 0;
        }
    }

    public async Task<int> EmbedAndUpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks)
    {
        var nonEmpty = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();
        if (nonEmpty.Count == 0)
        {
            return 0;
        }

        var knowledgeChunks = new ConcurrentBag<KnowledgeChunk>();
        try
        {
            // Each embed is an independent HTTP round-trip to Gemini - awaiting them one at a
            // time in a foreach turned a 10-chunk document into 10 sequential waits before an
            // upload could even respond. Bounded parallelism (not unbounded Task.WhenAll) keeps
            // a very large document from hammering the API in one burst.
            await Parallel.ForEachAsync(nonEmpty, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentEmbeds }, async (chunk, _) =>
            {
                var vector = await embeddingProvider.EmbedAsync(chunk.EmbedText ?? chunk.Text, EmbeddingTaskType.RetrievalDocument);
                knowledgeChunks.Add(new KnowledgeChunk { Id = chunk.Id, Text = chunk.Text, Vector = vector, Metadata = chunk.Metadata });
            });
        }
        catch (Exception ex)
        {
            throw new KnowledgeEmbeddingFailedException(ex);
        }

        var list = knowledgeChunks.ToList();
        try
        {
            await knowledgeIndexProvider.UpsertAsync(namespaceKey, list);
        }
        catch (Exception ex)
        {
            throw new KnowledgeIndexUpsertFailedException(ex);
        }

        logger.LogInformation("Indexed {Count} chunks into knowledge namespace {Namespace}", list.Count, namespaceKey);
        return list.Count;
    }
}
