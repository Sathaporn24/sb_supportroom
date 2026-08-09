namespace SupportRoom.Domain.Enums;

/// <summary>
/// Mirrors the Gemini grounded-answer result types from the spec - never a plain boolean, so
/// the UI/summary can distinguish *why* a question wasn't answered. String constants for the
/// same reason as SessionStatus (see SessionStatus.cs).
/// </summary>
public static class AnswerStatus
{
    public const string Answered = "answered";
    public const string NotFound = "not_found";
    public const string OutOfScope = "out_of_scope";
    public const string NoSpeech = "no_speech";
    public const string TranscriptionFailed = "transcription_failed";
}
