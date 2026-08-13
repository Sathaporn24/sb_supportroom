namespace SupportRoom.Domain.Enums;

/// <summary>
/// Status of a LearningSession - one person's run through a lesson.
///
/// NOT_STARTED is gone: a row now only exists once someone has actually joined, so there is no
/// state before IN_PROGRESS. EXPIRED is gone too - expiry is a property of the link, not of
/// anyone's learning, and now lives in LinkStatus.
///
/// "Stalled" is deliberately absent. It is derived from LastActivityAt at read time rather than
/// stored (CORE_FEATURE_SPEC §2.6).
///
/// String constants, not a C# enum - matches the exact SCREAMING_SNAKE_CASE the TS union type
/// (LearningSessionStatus in domain.ts) serializes as, without fighting JsonStringEnumConverter
/// naming policies for a wire format that isn't camelCase.
/// </summary>
public static class SessionStatus
{
    public const string InProgress = "IN_PROGRESS";
    public const string Ended = "ENDED";
}

/// <summary>
/// Status of a TrainingLink. Computed from ExpiresAt at read time, never stored - a link does not
/// change state on its own, and a stored copy would just be a clock that needs winding.
/// </summary>
public static class LinkStatus
{
    public const string Active = "ACTIVE";
    public const string Expired = "EXPIRED";
}

/// <summary>
/// CS's verdict on one AI answer. Free-text reasoning lives in SessionQuestion.ReviewNote -
/// see the comment there for why the "why" is not an enum.
/// </summary>
public static class ReviewResult
{
    public const string Correct = "correct";
    public const string Incorrect = "incorrect";

    public static bool IsValid(string? value)
        => value is Correct or Incorrect;
}
