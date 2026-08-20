namespace SupportRoom.Application.Dto;

/// <summary>Shared [StringLength]/[MaxLength] bounds - kept in one place so a change to one
/// caller's limit doesn't silently drift from another's identical-looking literal.</summary>
internal static class DtoLimits
{
    /// <summary>Chat message text and TTS input text share this ceiling today; split them out
    /// into their own constants if either input's requirements diverge.</summary>
    public const int MaxTextLength = 2000;

    /// <summary>A display label typed on the join screen, not an identity. The learning-session
    /// contract limits it to 80 characters after trimming.</summary>
    public const int RecipientNameMaxLength = 80;

    /// <summary>The browser-generated bearer-key component used by learner flows.</summary>
    public const int LearnerKeyMinLength = 8;
    public const int LearnerKeyMaxLength = 128;

    /// <summary>CS's free-text reason on a reviewed answer. Roomy on purpose: the whole point of
    /// the field is that the cause doesn't fit a fixed set of options yet.</summary>
    public const int ReviewNoteMaxLength = 2000;

    /// <summary>LessonSlideNarration.NarrationText (design.md DM-5) - equal to what Edge TTS can
    /// synthesize for one page without needing to split it.</summary>
    public const int NarrationTextMaxLength = 5000;

    /// <summary>KnowledgeQnA.Question (design.md DM-6) - the value that gets embedded (KS-5).</summary>
    public const int QnAQuestionMaxLength = 1000;

    /// <summary>KnowledgeQnA.Answer (design.md DM-6).</summary>
    public const int QnAAnswerMaxLength = 5000;

    /// <summary>KnowledgeQnAConflict.ModelNote (design.md DM-8) - model output, truncated so a
    /// verbose explanation never grows the row unbounded.</summary>
    public const int ConflictNoteMaxLength = 1000;

    /// <summary>KnowledgeQnAConflict.ConflictingSourceLabel - also model output (the label the
    /// model names for the winning document/slide), so it gets the same defensive cap even though
    /// design.md does not give it an exact number the way it does for ModelNote.</summary>
    public const int ConflictSourceLabelMaxLength = 300;
}
