using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

public sealed class SessionQuestion : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; init; }
    public bool IsDelete { get; init; }
    public DateTime? DeletedAt { get; init; }

    /// <summary>Points at LearningSession - the question belongs to the person who asked it, not
    /// to the link they came in through.</summary>
    public required string SessionId { get; init; }
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }

    /// <summary>QuestionSource.Voice | QuestionSource.Text - NOT NULL always: every row created
    /// after RemoveChatMessageAndAddQuestionSource knows exactly which channel produced it, and
    /// "unknown" is never a valid state to backfill going forward. Rows from before that migration
    /// are backfilled to Voice because, as a matter of fact, typing a question did not exist yet.</summary>
    public required string Source { get; init; }

    /// <summary>ReviewResult.Correct | ReviewResult.Incorrect, or null while unreviewed.</summary>
    public string? ReviewResult { get; set; }

    /// <summary>
    /// Free text, not an enum. "Wrong answer" has at least three causes that are fixed in
    /// completely different places - missing document, retrievable-but-not-retrieved, or the model
    /// answering from general knowledge - and storing only correct/incorrect collapses them into
    /// one bucket that usually gets "resolved" by adding documents even when the real cause was
    /// the prompt. Left as free text because we do not yet know the full set of causes
    /// (CORE_FEATURE_SPEC §2.7).
    /// </summary>
    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
