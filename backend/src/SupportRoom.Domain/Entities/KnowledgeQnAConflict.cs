using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A flag raised when the model reports that a Q&A it was given conflicted with a document/slide -
/// the document already won at answer time (KS-7). This row exists so CS can go fix the document
/// that is actually the root cause, not to block anything.
///
/// Kept as its own table rather than a column on KnowledgeQnA because one Q&A can conflict with
/// different documents on different occasions, and CS needs to see the evidence (the real question
/// that triggered it), not just a counter.
/// </summary>
public sealed class KnowledgeQnAConflict : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qnacf")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }                // null - raised by the system, not a person
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string QnAId { get; init; }

    /// <summary>The real question that raised the flag - SessionQuestion.Id (logical cross-module
    /// FK). Null when that question was never recorded (e.g. a readiness check).</summary>
    public string? SessionQuestionId { get; init; }

    /// <summary>Human-readable name of the conflicting source - a document's FileName, or
    /// "สไลด์หน้า N" - built from the winning chunk's metadata. **Never a raw error or provider
    /// name** (R6.4).</summary>
    public required string ConflictingSourceLabel { get; init; }

    /// <summary>The model's own sentence describing where the conflict is - truncated to 1000
    /// characters. This is model output, not a system error, so it is safe to show CS.</summary>
    public string? ModelNote { get; init; }

    /// <summary>CS closes the flag once they have fixed the source document - null = still open.</summary>
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }               // AdminUser.Id
}
