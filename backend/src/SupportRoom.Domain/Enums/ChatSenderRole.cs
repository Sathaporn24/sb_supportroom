namespace SupportRoom.Domain.Enums;

/// <summary>
/// Who sent a ChatMessage. String constants for the same reason as SessionStatus - these are the
/// exact values the TS union type (ChatSenderRole in domain.ts) serializes as.
///
/// Deliberately not "teacher"/"cs": those were School Bright's words, and this product is used by
/// companies whose users are not teachers (TD-012).
/// </summary>
public static class ChatSenderRole
{
    /// <summary>Whoever opened the join link.</summary>
    public const string Recipient = "recipient";

    /// <summary>The company's own support staff.</summary>
    public const string Agent = "agent";

    public const string System = "system";
}
