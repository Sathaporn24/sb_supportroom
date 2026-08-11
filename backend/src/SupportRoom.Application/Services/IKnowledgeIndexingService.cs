using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Application.Services;

public sealed class KnowledgeSourceChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public interface IKnowledgeIndexingService
{
    Task IndexLessonAsync(string namespaceKey, IReadOnlyList<ResolvedSlide> slides);

    /// <summary>Shared embed-then-upsert core, reused by document indexing
    /// (IDocumentResourceService) so it doesn't duplicate the embed/upsert loop. Returns the
    /// number of chunks actually indexed (blank-text chunks are skipped). Never throws - logs
    /// and returns 0 on failure, same non-fatal contract IndexLessonAsync already had.</summary>
    Task<int> IndexChunksAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks);
}

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
                Metadata = new Dictionary<string, string> { ["slideObjectId"] = s.SlideObjectId, ["index"] = s.Index.ToString() },
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
            var nonEmpty = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();
            var knowledgeChunks = new ConcurrentBag<KnowledgeChunk>();

            // Each embed is an independent HTTP round-trip to Gemini - awaiting them one at a
            // time in a foreach turned a 10-chunk document into 10 sequential waits before an
            // upload could even respond. Bounded parallelism (not unbounded Task.WhenAll) keeps
            // a very large document from hammering the API in one burst.
            await Parallel.ForEachAsync(nonEmpty, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentEmbeds }, async (chunk, _) =>
            {
                var vector = await embeddingProvider.EmbedAsync(chunk.Text, EmbeddingTaskType.RetrievalDocument);
                knowledgeChunks.Add(new KnowledgeChunk { Id = chunk.Id, Text = chunk.Text, Vector = vector, Metadata = chunk.Metadata });
            });

            if (knowledgeChunks.IsEmpty)
            {
                return 0;
            }

            var list = knowledgeChunks.ToList();
            await knowledgeIndexProvider.UpsertAsync(namespaceKey, list);
            logger.LogInformation("Indexed {Count} chunks into knowledge namespace {Namespace}", list.Count, namespaceKey);
            return list.Count;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index into knowledge store namespace {Namespace}", namespaceKey);
            return 0;
        }
    }
}
