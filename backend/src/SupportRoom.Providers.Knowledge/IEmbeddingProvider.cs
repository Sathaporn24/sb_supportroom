namespace SupportRoom.Providers.Knowledge;

/// <summary>
/// Gemini's embedding task types. For RAG, indexing content as <see cref="RetrievalDocument"/> and
/// embedding the search query as <see cref="RetrievalQuery"/> retrieves measurably better than
/// embedding both the same way - it's Google's documented recommendation for gemini-embedding-001.
/// The two are NOT interchangeable: a query vector and the document vectors it searches must each
/// be embedded with their matching task type, or cosine similarity drops. That coupling is exactly
/// why switching an already-populated index to task types requires re-indexing every vector.
/// </summary>
public enum EmbeddingTaskType
{
    /// <summary>Stored content (slide notes, document chunks) being indexed for later retrieval.</summary>
    RetrievalDocument,

    /// <summary>A user's question being embedded to search the index.</summary>
    RetrievalQuery,
}

public interface IEmbeddingProvider
{
    Task<float[]> EmbedAsync(string text, EmbeddingTaskType taskType);
}
