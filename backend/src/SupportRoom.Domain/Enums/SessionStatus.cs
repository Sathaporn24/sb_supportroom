namespace SupportRoom.Domain.Enums;

/// <summary>
/// String constants, not a C# enum - matches the exact SCREAMING_SNAKE_CASE the TS union type
/// (SessionStatus in domain.ts) serializes as, without fighting JsonStringEnumConverter naming
/// policies for a wire format that isn't camelCase.
/// </summary>
public static class SessionStatus
{
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Ended = "ENDED";
    public const string Expired = "EXPIRED";
}
