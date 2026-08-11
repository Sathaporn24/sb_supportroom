using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// EF Core entity (see .claude/skills/dotnet-layered-backend/SKILL.md). Timestamps are real
/// DateTime columns here - the ISO-8601 string wire format the frontend expects is restored
/// at the ViewModel mapping boundary (TrainingSessionViewModel), not on the entity itself.
/// </summary>
public sealed class TrainingSession : IEntityMaster<string>, ICompanyScoped
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
    /// <summary>Display label for the person the link was sent to - not an identity, not
    /// authentication. Free text typed by whoever created the session.</summary>
    public string? RecipientName { get; init; }

    /// <summary>The recipient's own organization (a school, a branch, ...) - a label only.
    /// This is NOT CompanyId: it carries no isolation meaning and is never used in a query
    /// filter. CompanyId is the company that owns this support room.</summary>
    public string? RecipientOrgName { get; init; }
    public required string Status { get; set; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool CompletedAllSlides { get; set; }
    public string? LastSlideObjectId { get; set; }
}
