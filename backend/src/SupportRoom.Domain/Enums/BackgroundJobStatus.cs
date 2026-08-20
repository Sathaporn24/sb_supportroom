namespace SupportRoom.Domain.Enums;

/// <summary>String constants for the same reason as DocumentIndexingStatus/AnswerStatus - see
/// design.md DM-11.</summary>
public static class BackgroundJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}
