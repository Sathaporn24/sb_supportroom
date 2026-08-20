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

    /// <summary>R3 - category-level namespace. categoryId already comes prefixed "kbcat-" from
    /// IdGenerator, so it cannot collide with a lessonSlug as long as TX-7 keeps rejecting slugs
    /// that start with "kbcat-" or equal "kb-global". Keyed by id, not category name, on purpose:
    /// renaming a category must not orphan its vectors the way renaming a lesson slug already does
    /// (see R-8) - that would be the same debt twice.</summary>
    public static string ForCategory(string companyId, string categoryId) => $"{companyId}:{categoryId}";
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

    /// <summary>R6.1 - deletes specific vectors by id, instead of the whole namespace. `ids` empty
    /// is a no-op (never fires a request). Same contract as DeleteNamespaceAsync: deleting an id
    /// that no longer exists is not a failure.</summary>
    Task DeleteVectorsAsync(string namespaceKey, IReadOnlyList<string> ids);

    /// <summary>
    /// QQ-6 - updates only the text/metadata of an existing vector, leaving its embedding
    /// untouched. Exists specifically so editing a Q&A's Answer (but not its Question) never
    /// re-embeds: KS-5 embeds the Question only, so if the Question did not change the vector is
    /// still correct, and only the "ถาม: ...\nตอบ: ..." text stored in metadata needs to catch up.
    /// Pinecone's /vectors/update accepts setMetadata without values for exactly this case.
    /// </summary>
    Task UpdateMetadataAsync(string namespaceKey, string id, string text, IReadOnlyDictionary<string, string>? metadata);
}
