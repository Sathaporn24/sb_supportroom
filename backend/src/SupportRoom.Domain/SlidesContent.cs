namespace SupportRoom.Domain;

/// <summary>A single resolved slide as returned live by ISlidesContentProvider (no admin-only fields).</summary>
public sealed class ResolvedSlide
{
    public required string SlideObjectId { get; init; }
    public required int Index { get; init; }
    public required string SpeakerNotes { get; init; }
    public string? SlideUrl { get; init; }
}

public sealed class SlidesLessonContent
{
    public required string PresentationId { get; init; }
    public required string Title { get; init; }
    public required string EmbedUrl { get; init; }
    public required IReadOnlyList<ResolvedSlide> Slides { get; init; }
    public required string SyncedAt { get; init; }
}

/// <summary>ResolvedSlide merged with the admin-configured VideoDurationMs - what the Tutor Engine consumes.</summary>
public sealed class TeachingSlide
{
    public required string SlideObjectId { get; init; }
    public required int Index { get; init; }
    public required string SpeakerNotes { get; init; }
    public string? SlideUrl { get; init; }
    public required int VideoDurationMs { get; init; }
}

public sealed class ResolvedPresentation
{
    public string? PresentationId { get; init; }
    public required string EmbedUrl { get; init; }
    public required bool IsEmbedOnly { get; init; }
    public string? Warning { get; init; }
}
