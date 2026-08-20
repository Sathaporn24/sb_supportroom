using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A background job that survives a restart - replaces IBackgroundTaskQueue, which held closures
/// (including a whole uploaded file's bytes) in an in-memory Channel that vanished on restart,
/// leaving the document stuck at "pending" forever with no error anywhere (design.md R6.2).
///
/// This row deliberately stores only ids, not payloads: the worker reloads everything it needs
/// (the file itself, the document row) fresh from its own source of truth every time it processes
/// a job - see IDocumentStorageProvider.DownloadAsync(document.ObsKey). A closure capturing
/// byte[] Content was exactly the bug being fixed here.
///
/// CompanyId on this row is not just an audit column - the worker must call
/// ICompanyContext.Resolve(job.CompanyId) before touching any other repository. Skipping that
/// makes every company-scoped query match zero rows, so the job silently "succeeds" without doing
/// anything and the document is stuck pending with no visible error (design.md DI-4).
/// </summary>
public sealed class BackgroundJob : IEntityMaster<string>, ICompanyScoped
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

    /// <summary>BackgroundJobType.</summary>
    public required string JobType { get; init; }

    /// <summary>DocumentResource.Id | LessonConfig.Id | KnowledgeQnA.Id, depending on JobType.</summary>
    public required string TargetId { get; init; }

    /// <summary>Small JSON for parameters that aren't ids - null when a job type needs nothing
    /// beyond TargetId. Must never carry file content or long text.</summary>
    public string? PayloadJson { get; init; }

    /// <summary>BackgroundJobStatus - pending | running | succeeded | failed.</summary>
    public required string Status { get; set; }

    public required int AttemptCount { get; set; }

    /// <summary>When this job is next eligible to be claimed - CreateDate at creation, or
    /// now + backoff after a failed attempt (see design.md DI-9).</summary>
    public required DateTime NextAttemptAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>A value from DocumentFailureReason - safe to show to CS.</summary>
    public string? LastErrorCode { get; set; }

    /// <summary>Raw exception text, for logs/debugging only, truncated to 2000 characters.
    /// Must never be mapped onto any ViewModel or leave the API (design.md R6.4).</summary>
    public string? LastErrorDetail { get; set; }
}
