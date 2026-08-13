namespace SupportRoom.Application.ViewModel;

/// <summary>Mirrors TrainingLink in domain.ts - timestamps are ISO-8601 strings on the wire.</summary>
public sealed class TrainingLinkViewModel
{
    public required string Id { get; init; }
    public required string Token { get; init; }
    public required string LessonId { get; init; }
    public required string LessonSlug { get; init; }
    public string? RecipientOrgName { get; init; }
    public required string CreatedAt { get; init; }
    public required string ExpiresAt { get; init; }
    public int? MaxAttendees { get; init; }

    /// <summary>LinkStatus.Active | LinkStatus.Expired - computed from ExpiresAt at read time,
    /// never stored (see LinkStatus).</summary>
    public required string Status { get; init; }

    /// <summary>How many people have opened this link. Cheap to include and it is the first thing
    /// CS looks for after sending one out.</summary>
    public required int LearningSessionCount { get; init; }
}
