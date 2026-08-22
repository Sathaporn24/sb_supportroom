using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// Teaching content comes from either Google Slides or an uploaded PDF (ContentSourceType) -
/// LessonConfig only stores admin-set metadata (URLs/PDF pointer, timing, per-slide video
/// duration) - the actual slide content (speaker notes, images/video) is resolved live via
/// ISlidesProvider or PdfSlidesRenderer and is never persisted as a copy. Mirrors
/// src/types/domain.ts.
/// </summary>
public sealed class SlideConfig
{
    public required string SlideObjectId { get; init; }
    public required int SlideIndex { get; init; }

    /// <summary>Null for slides with no video.</summary>
    public int? VideoDurationMs { get; init; }
}

public sealed class LessonConfig : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    // set, not init: SaveAsync is an upsert that mutates the tracked instance in place, so an
    // edit must be able to record who made it.
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; init; }
    public bool IsDelete { get; init; }
    public DateTime? DeletedAt { get; init; }

    public required string Slug { get; init; }
    public required string CategoryId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string SlidesSourceUrl { get; set; }

    /// <summary>Extracted from SlidesSourceUrl when possible; required for the Google Slides API.</summary>
    public string? PresentationId { get; set; }

    /// <summary>Published/embed URL used to render the Shared Screen iframe.</summary>
    public string? SlidesEmbedUrl { get; set; }

    /// <summary>"google_slides" (default) or "pdf" - see LessonContentSourceType.</summary>
    public required string ContentSourceType { get; set; }

    /// <summary>Set only when ContentSourceType is "pdf" - points at the DocumentResource holding the PDF.</summary>
    public string? PdfDocumentResourceId { get; set; }

    public required List<SlideConfig> SlideConfigs { get; set; }
    public required bool IsActive { get; set; }
}
