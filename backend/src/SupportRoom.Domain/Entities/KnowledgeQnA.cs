using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A question-answer pair CS writes by hand - the core idea of R5, "ถามประมาณไหน ตอบประมาณนั้น".
/// Indexed into the same Pinecone namespace as documents/slides for its scope (never a namespace
/// of its own), distinguished only by metadata.sourceType="qna" - a separate namespace would
/// double the number of Pinecone queries per question (see KS-3/KS-5).
/// </summary>
public sealed class KnowledgeQnA : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qna")
    public required string CompanyId { get; init; }

    /// <summary>R5.6 - AdminUser.Id of the person who wrote it. The system has real auth by the
    /// time this table exists, so unlike SessionQuestion.ReviewNote's ReviewedBy (cut in
    /// learning-session because auth did not exist yet), this value can be trusted.</summary>
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>The question CS wrote - **this is the value that gets embedded** (KS-5). May be
    /// prefilled from the transcript of the question it originated from, but CS can edit it before
    /// saving. Trimmed, 1-1000 characters.</summary>
    public required string Question { get; set; }

    /// <summary>The correct answer. Trimmed, 1-5000 characters.</summary>
    public required string Answer { get; set; }

    /// <summary>R5.4 - "lesson" | "category" | "company" (KnowledgeScopeType).</summary>
    public required string ScopeType { get; set; }

    /// <summary>Same shape as DocumentResource.ScopeId - see KS-1.</summary>
    public string? ScopeId { get; set; }

    /// <summary>The Pinecone vector id = this row's own Id, no suffix (one Q&A = one vector,
    /// unlike a document which fans out into many chunks). Stored as a column for the same reason
    /// as DocumentChunk.VectorId.</summary>
    public required string VectorId { get; init; }

    /// <summary>The namespace this row was actually upserted into - changing ScopeType/ScopeId
    /// means deleting from this namespace before upserting into the new one (KS-4). This is the
    /// "old" value that has to be known to do that.</summary>
    public string? IndexedNamespaceKey { get; set; }

    /// <summary>Same DocumentIndexingStatus set documents use - pending | indexed | failed.</summary>
    public required string IndexingStatus { get; set; }

    /// <summary>Same DocumentFailureReason set documents use (only embedding_failed / index_failed
    /// can happen here - a Q&A has no file-conversion step).</summary>
    public string? FailureReason { get; set; }
}
