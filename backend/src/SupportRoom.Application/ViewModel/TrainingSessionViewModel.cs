namespace SupportRoom.Application.ViewModel;

/// <summary>Mirrors TrainingSession in domain.ts - timestamps are ISO-8601 strings on the wire.</summary>
public sealed class TrainingSessionViewModel
{
    public required string Id { get; init; }
    public required string Token { get; init; }
    public required string LessonId { get; init; }
    public required string LessonSlug { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientOrgName { get; init; }
    public required string Status { get; init; }
    public required string CreatedAt { get; init; }
    public required string ExpiresAt { get; init; }
    public string? StartedAt { get; init; }
    public string? EndedAt { get; init; }
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
}
