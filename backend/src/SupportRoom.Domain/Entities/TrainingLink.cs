using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// An invite link CS hands out - NOT one person's run through the lesson. One link is opened by
/// many people and each of them gets their own LearningSession row (see CORE_FEATURE_SPEC §1).
/// This entity was called TrainingSession until the two were split apart; the word "session" now
/// belongs exclusively to LearningSession, which is what SessionQuestion.SessionId and
/// ChatMessage.SessionId point at.
///
/// EF Core entity (see .claude/skills/dotnet-layered-backend/SKILL.md). Timestamps are real
/// DateTime columns here - the ISO-8601 string wire format the frontend expects is restored
/// at the ViewModel mapping boundary (TrainingLinkViewModel), not on the entity itself.
/// </summary>
public sealed class TrainingLink : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string Token { get; init; }
    public required string LessonId { get; init; }
    public required string LessonSlug { get; init; }

    /// <summary>The recipient's own organization (a school, a branch, a department) - a label only,
    /// typed by CS when creating the link. This is NOT CompanyId: it carries no isolation meaning
    /// and is never used in a query filter. CompanyId is the company that owns this support room.</summary>
    public string? RecipientOrgName { get; init; }

    public required DateTime ExpiresAt { get; init; }

    /// <summary>null = unlimited. Reserved by CORE_FEATURE_SPEC §6 - the column exists so adding
    /// the cap later is not a migration, but nothing enforces it yet.</summary>
    public int? MaxAttendees { get; init; }
}
