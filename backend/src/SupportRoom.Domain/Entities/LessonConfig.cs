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
    // R9/Module L - init -> set: these three become the real trash-lifecycle state on this
    // entity (see the state invariant note below), which needs to be toggled by archive/restore,
    // not only ever set once at creation like every other entity's DeleteBy/IsDelete/DeletedAt.
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>R9/Module L - BackgroundJob.Id of the single lesson_purge job created by the most
    /// recent archive (LT-3). Null on active/restored. Doubles as a generation token: a stale job
    /// from an earlier trash round whose id no longer matches this column must no-op (LT-11) -
    /// logical FK only, no database FK, same pattern as every other cross-entity pointer here.</summary>
    public string? PurgeJobId { get; set; }

    /// <summary>R9/Module L - null while the lesson is still restorable; the worker sets this
    /// (UTC now, via a conditional update - LT-13) the instant it commits to deleting for real.
    /// Once set, restore must be rejected with 409 (LT-4) and retries of the same job id continue
    /// idempotently (LT-13/LT-14).
    ///
    /// State invariant (design.md DM-2):
    ///   active   = !IsDelete   &amp;&amp; DeletedAt/DeleteBy/PurgeJobId/PurgeStartedAt all null
    ///   trash    = IsDelete    &amp;&amp; DeletedAt/PurgeJobId set                &amp;&amp; PurgeStartedAt null
    ///   purging  = IsDelete    &amp;&amp; DeletedAt/PurgeJobId set                &amp;&amp; PurgeStartedAt set
    ///   purged   = row hard-deleted (no in-between representation left behind)</summary>
    public DateTime? PurgeStartedAt { get; set; }

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
