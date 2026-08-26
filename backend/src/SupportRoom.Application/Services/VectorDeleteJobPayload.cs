namespace SupportRoom.Application.Services;

/// <summary>QQ-5 vs DI-13 - which repository ProcessVectorDeleteAsync must check before deleting.
/// A document can be restored (DI-15), so its vector_delete job re-checks
/// IDocumentResourceRepository.GetDeleted() first to avoid deleting vectors a restore just
/// re-created (DI-16's collision note). A KnowledgeQnA has no restore path at all - QQ-5's
/// deletion is permanent - so that check does not apply and must not be run against the wrong
/// repository for a qna-typed job.</summary>
public static class VectorDeleteTargetKind
{
    public const string Document = "document";
    public const string Qna = "qna";

    /// <summary>EX-6 - a PDF lesson page's document-copy vector ({documentId}-page-N, the row in
    /// DocumentChunk). Deliberately its own kind, not Document: ProcessVectorDeleteAsync's
    /// Document branch guards on the document still being soft-deleted (DI-16's restore-collision
    /// note) - the deck's DocumentResource is still active when a page gets excluded, so that
    /// guard would return early and silently drop this job every time.</summary>
    public const string LessonPage = "lesson_page";
}

/// <summary>
/// DI-13/QQ-5 - BackgroundJob.PayloadJson shape for JobType = vector_delete. Written by
/// IDocumentResourceService.DeleteAsync (Kind = Document, from the DocumentChunk rows persisted at
/// index time - DM-4) and IKnowledgeQnAService.DeleteAsync (Kind = Qna, a single VectorId = the
/// Q&A's own Id), read back by IBackgroundJobProcessor.ProcessVectorDeleteAsync. Carrying the ids
/// here instead of recomputing them is what replaces the Phase 3 document workaround: these ids
/// are guaranteed to match what was actually upserted, because they were captured at the moment
/// they were written, not recomputed after the fact. PayloadJson has no fixed schema (design.md
/// DM-10), so adding Kind here to disambiguate is a payload-shape decision, not a contract change.
/// </summary>
public sealed class VectorDeleteJobPayload
{
    public string Kind { get; init; } = VectorDeleteTargetKind.Document;
    public required string NamespaceKey { get; init; }
    public required IReadOnlyList<string> VectorIds { get; init; }
}
