using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class SlideConfigDto
{
    [Required]
    public required string SlideObjectId { get; init; }
    [Required]
    public required int SlideIndex { get; init; }

    /// <summary>A negative value here used to save without error and then broke pacing at
    /// runtime: the tutor engine's setTimeout(ms) clamps a negative delay to ~0, which for
    /// this field means the wait-for-video effect fires instantly instead of waiting.</summary>
    [Range(0, int.MaxValue)]
    public int? VideoDurationMs { get; init; }
}

/// <summary>
/// Input for POST /api/lessons. PresentationId is always derived server-side from
/// SlidesSourceUrl - the client never sets it directly (mirrors LessonConfigInput in domain.ts).
/// </summary>
public sealed class LessonConfigDto
{
    [Required]
    public required string Slug { get; init; }
    [Required]
    public required string CategoryId { get; init; }
    [Required]
    public required string Title { get; init; }
    public string? Description { get; init; }

    /// <summary>Empty allowed - an unset/invalid URL resolves to a null PresentationId with a
    /// warning rather than failing validation (mirrors the TS zod schema, which has no .min(1)
    /// here); a lesson can be saved before its Slides URL is filled in.</summary>
    public required string SlidesSourceUrl { get; init; }
    public string? SlidesEmbedUrl { get; init; }

    /// <summary>"google_slides" | "pdf" - see LessonContentSourceType.Allowed.</summary>
    [Required]
    public required string ContentSourceType { get; init; }

    /// <summary>Required when ContentSourceType is "pdf" - the DocumentResource holding the PDF.</summary>
    public string? PdfDocumentResourceId { get; init; }

    /// <summary>A negative value used to save without error and then broke the intro-wait UX at
    /// runtime: the tutor engine's setTimeout(ms) clamps a negative delay to ~0, so the teacher's
    /// "wait for ready" window would disappear entirely instead of throwing here.</summary>
    [Required, Range(0, int.MaxValue)]
    public required int IntroWaitMs { get; init; }
    [Required, Range(0, int.MaxValue)]
    public required int BreathPauseMs { get; init; }
    [Required, Range(0, int.MaxValue)]
    public required int FinalQuestionWaitMs { get; init; }
    [Required]
    public required List<SlideConfigDto> SlideConfigs { get; init; }
    [Required]
    public required bool IsActive { get; init; }
}
