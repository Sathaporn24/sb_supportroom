namespace SupportRoom.Domain.Enums;

/// <summary>R9.8/Module L - why a SessionQuestionReviewExclusion row exists. One value today; kept
/// as a string constant (not a bare bool) for the same reason as every other status in this
/// project - see design.md DM-11.</summary>
public static class QuestionReviewExclusionReason
{
    public const string LessonPermanentlyDeleted = "lesson_permanently_deleted";
}
