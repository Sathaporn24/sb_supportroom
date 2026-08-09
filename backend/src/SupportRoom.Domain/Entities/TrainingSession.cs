using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// EF Core entity (see .claude/skills/dotnet-layered-backend/SKILL.md). Timestamps are real
/// DateTime columns here - the ISO-8601 string wire format the frontend expects is restored
/// at the ViewModel mapping boundary (TrainingSessionViewModel), not on the entity itself.
/// </summary>
public sealed class TrainingSession : IEntityMaster<string>
{
    public required string Id { get; init; }
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
    public string? TeacherName { get; init; }
    public string? SchoolName { get; init; }
    public required string Status { get; set; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool CompletedAllSlides { get; set; }
    public string? LastSlideObjectId { get; set; }
}
