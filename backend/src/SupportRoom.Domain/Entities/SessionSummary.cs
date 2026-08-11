using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// Snapshot written once, when a TrainingSession ends (mirrors sessions/[token]/route.ts's
/// PATCH action=end behavior) - not recomputed live.
/// </summary>
public sealed class SessionSummary : IEntityMaster<string>, ICompanyScoped
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

    public required string SessionId { get; init; }
    public required bool CompletedAllSlides { get; set; }
    public string? LastSlideObjectId { get; set; }

    /// <summary>
    /// The question list itself is not duplicated here - it's re-joined from SessionQuestion by
    /// SessionId at read time (see ISessionSummaryService.GetBySessionId).
    /// </summary>
    public required List<string> UnansweredPoints { get; set; }
}
