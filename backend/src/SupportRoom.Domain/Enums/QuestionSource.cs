namespace SupportRoom.Domain.Enums;

/// <summary>
/// How a SessionQuestion reached the system - the learner spoke it (Push-to-Talk) or typed it
/// (F10). Needed because "wrong answer" has a fourth cause unique to voice - mis-transcription -
/// that CS cannot see or fix without knowing which channel a question came in on. String
/// constants, not a C# enum, for the same reason as SessionStatus: matches the TS union type
/// (QuestionSource in domain.ts) serializes as, exactly.
/// </summary>
public static class QuestionSource
{
    public const string Voice = "voice";
    public const string Text = "text";
}
