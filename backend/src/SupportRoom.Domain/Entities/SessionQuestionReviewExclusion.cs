using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// R9.8/Module L - a permanent tombstone saying "this past question must never re-enter the
/// active review queue" after its lesson has been permanently purged (design.md DM-18). Not a
/// column on SessionQuestion: that entity belongs to the learning-session module and this
/// requirement (Module L) must not redesign it from over here - same reasoning as
/// KnowledgeQnASource's own cross-module pointer.
///
/// Created for every SessionQuestion of a lesson right before its KnowledgeQnA/KnowledgeQnASource
/// rows are hard-deleted (LT-16), so a purged lesson's questions read as permanently suppressed
/// rather than reappearing as unanswered - see design.md's warning against using
/// KnowledgeQnASource as a tombstone instead (a source can belong to another lesson's question).
/// </summary>
public sealed class SessionQuestionReviewExclusion : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qex")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }                // null when created by the purge worker
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>Logical FK -> SessionQuestion.Id; the question row itself must survive (R9.8 -
    /// history is retained even after its lesson is gone).</summary>
    public required string SessionQuestionId { get; init; }

    /// <summary>Id of the LessonConfig that has since been hard-deleted. No FK by design - the
    /// row it would point at no longer exists once purge finishes.</summary>
    public required string LessonId { get; init; }

    /// <summary>A QuestionReviewExclusionReason value - one value exists today.</summary>
    public required string Reason { get; init; }
}
