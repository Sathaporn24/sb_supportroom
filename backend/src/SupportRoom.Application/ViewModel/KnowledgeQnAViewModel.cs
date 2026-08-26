namespace SupportRoom.Application.ViewModel;

public sealed class KnowledgeQnAViewModel
{
    public required string Id { get; init; }
    public required string Question { get; init; }
    public required string Answer { get; init; }
    public required string ScopeType { get; init; }
    public string? ScopeId { get; init; }
    public required string IndexingStatus { get; init; }
    public string? FailureReason { get; init; }
    public required string CreatedAt { get; init; }
}

/// <summary>
/// One row of the review queue (P8/R5.1) - a SessionQuestion that has no Q&A answering it yet
/// (QQ-1), enriched with which lesson it happened in (QQ-4) and why it is here (QQ-3).
/// </summary>
public sealed class KnowledgeQnAQueueItemViewModel
{
    public required string Id { get; init; }
    public required string SessionId { get; init; }
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }
    public string? ReviewResult { get; init; }
    public string? ReviewNote { get; init; }
    public required string CreatedAt { get; init; }

    public string? LessonId { get; init; }
    public string? LessonSlug { get; init; }

    /// <summary>QQ-3 - a question can carry both tags at once (the AI answered not_found AND CS
    /// separately marked the same answer incorrect), so these are independent flags, not a single
    /// source enum.</summary>
    public required bool FromNotFound { get; init; }
    public required bool FromIncorrect { get; init; }
}

/// <summary>KL-23/Q-H2 - the 409 Conflict payload when CreateAsync finds an existing, non-deleted
/// Q&A of this company with the same Question after trim/whitespace-collapse/case-fold. A list,
/// not a single row: ConfirmDuplicate=true saving anyway can legitimately produce several
/// identical questions since KL-24 forbids a unique constraint. Ordered CreateDate descending.
/// Reuses KnowledgeQnAViewModel as-is - distinct shape from KL-21's DuplicateDocumentDto (a
/// different endpoint entirely).</summary>
public sealed class DuplicateQnAResponse
{
    public required IReadOnlyList<KnowledgeQnAViewModel> DuplicateByQuestion { get; init; }
}

public sealed class KnowledgeQnAConflictViewModel
{
    public required string Id { get; init; }
    public required string QnAId { get; init; }
    public string? SessionQuestionId { get; init; }
    public required string ConflictingSourceLabel { get; init; }
    public string? ModelNote { get; init; }
    public required string CreatedAt { get; init; }
    public string? ResolvedAt { get; init; }
    public string? ResolvedBy { get; init; }
}
