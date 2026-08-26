namespace SupportRoom.Domain.Enums;

/// <summary>String constants for the same reason as DocumentIndexingStatus/AnswerStatus - see
/// design.md DM-11.</summary>
public static class BackgroundJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    /// <summary>R9/Module L - a lesson_purge job canceled by a restore before the worker claimed
    /// it (LT-4). ClaimNext's WHERE Status = Pending never selects this, so a canceled job can
    /// never be picked up again.</summary>
    public const string Canceled = "canceled";
}
