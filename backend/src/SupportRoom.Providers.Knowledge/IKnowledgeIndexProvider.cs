namespace SupportRoom.Providers.Knowledge;

/// <summary>Well-known namespace keys shared across the Application layer and this project, so
/// "kb-global" isn't a magic string duplicated at every call site that needs it.</summary>
public static class KnowledgeNamespaces
{
    /// <summary>Standalone documents (no lessonSlug) - queried alongside every lesson's own
    /// namespace so a CS-uploaded document answers questions in any lesson, not just one.
    /// Always company-scoped via ForGlobal: an unprefixed "kb-global" would be shared by every
    /// company on the index, and it is queried on EVERY question, so one company's standalone
    /// document would answer another company's questions.</summary>
    private const string GlobalSuffix = "kb-global";

    /// <summary>
    /// Vectors live outside PostgreSQL, so the database's company query filter cannot reach them -
    /// isolation here has to be built into the namespace key itself. Every key is
    /// "{companyId}:{scope}" and nothing may query a bare scope.
    /// </summary>
    public static string For(string companyId, string lessonSlug) => $"{companyId}:{lessonSlug}";

    public static string ForGlobal(string companyId) => $"{companyId}:{GlobalSuffix}";
}

public sealed class KnowledgeChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required float[] Vector { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class ScoredChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required float Score { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// namespaceKey is the partition a chunk lives in - Phase 1 uses LessonConfig.Slug (one
/// namespace per lesson); Phase 2 (broader CS-uploaded knowledge base, not built yet) can add
/// further namespaces (e.g. "kb-global") and query more than one before merging results,
/// without any interface change here.
/// </summary>
public interface IKnowledgeIndexProvider
{
    Task UpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeChunk> chunks);
    Task<IReadOnlyList<ScoredChunk>> QueryAsync(string namespaceKey, float[] queryVector, int topK);
    Task DeleteNamespaceAsync(string namespaceKey);
}
