namespace SupportRoom.Application.ViewModel;

/// <summary>ResolvedSlide merged with the admin-configured VideoDurationMs - what the Tutor Engine consumes.</summary>
public sealed class TeachingSlideViewModel
{
    public required string SlideObjectId { get; init; }
    public required int Index { get; init; }
    public required string SpeakerNotes { get; init; }
    public string? SlideUrl { get; init; }
    public required int VideoDurationMs { get; init; }
}
