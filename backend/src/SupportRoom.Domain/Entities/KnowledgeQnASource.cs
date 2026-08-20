using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// The real question a Q&A was written to answer - one Q&A can close several questions (QQ-7).
///
/// The link lives here, not as a column on SessionQuestion: SessionQuestion is an entity owned by
/// `_docs/module/learning-session/design.md` (see conventions §1) - this module may reference it
/// but must never redesign it from over here. Keeping the pointer on our own table means all of R5
/// needs **zero** changes to another module's entity.
/// </summary>
public sealed class KnowledgeQnASource : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qnasrc")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string QnAId { get; init; }

    /// <summary>SessionQuestion.Id - logical FK across modules, no real FK constraint.</summary>
    public required string SessionQuestionId { get; init; }
}
