namespace SupportRoom.Providers.VoiceQuestion;

public sealed class VoiceQuestionSlideContext
{
    public required string SlideObjectId { get; init; }
    public required string SpeakerNotes { get; init; }
}

public sealed class VoiceQuestionInput
{
    public required byte[] Audio { get; init; }
    public required string MimeType { get; init; }

    /// <summary>Client-measured hold duration, used for the no_speech / too-short check.</summary>
    public required int DurationMs { get; init; }

    /// <summary>Every slide's speaker notes in the lesson - the full grounding knowledge base.</summary>
    public required IReadOnlyList<VoiceQuestionSlideContext> LessonSlides { get; init; }
    public string? CurrentSlideObjectId { get; init; }

    /// <summary>Company-scoped knowledge-store namespace for this lesson, built by the caller
    /// (KnowledgeNamespaces.For) - not a bare slug. Providers must never assemble a namespace
    /// themselves: a provider that queried a plain slug would read across companies.</summary>
    public required string LessonNamespace { get; init; }

    /// <summary>Namespace of the knowledge category this lesson belongs to
    /// (KnowledgeNamespaces.ForCategory). Required, not nullable: every lesson has a CategoryId
    /// (real or the system-default "ยังไม่จัดหมวด" leaf) as of Phase 1, so a nullable field here
    /// would just create a silent way to forget to pass it. Queried on every question alongside
    /// LessonNamespace and GlobalNamespace.</summary>
    public required string CategoryNamespace { get; init; }

    /// <summary>This company's shared standalone-document namespace (KnowledgeNamespaces.ForGlobal).
    /// Queried on every question alongside LessonNamespace.</summary>
    public required string GlobalNamespace { get; init; }
}

/// <summary>
/// F10/TQ-7 - the typed-question equivalent of VoiceQuestionInput. QuestionText stands in for the
/// transcript a voice question would otherwise produce from step 1 (transcription) - everything
/// downstream of "we now have question text" is identical between the two channels. No Audio,
/// MimeType or DurationMs: those describe a voice recording, which this input never has.
/// </summary>
public sealed class TextQuestionInput
{
    /// <summary>The learner's typed question (trimmed). Used as the retrieval query and placed
    /// directly into the answer prompt exactly as a voice transcript would be.</summary>
    public required string QuestionText { get; init; }
    public required IReadOnlyList<VoiceQuestionSlideContext> LessonSlides { get; init; }
    public string? CurrentSlideObjectId { get; init; }
    public required string LessonNamespace { get; init; }
    public required string CategoryNamespace { get; init; }
    public required string GlobalNamespace { get; init; }
}

/// <summary>KS-9 - the model's own report that a Q&A it was shown conflicted with the
/// document/slide block it was told to prefer (KS-7). Never validated by the provider itself -
/// QnAId can be a hallucinated id, so the caller (VoiceQuestionService) must confirm it is real and
/// belongs to this company before recording it (KS-10).</summary>
public sealed class VoiceQuestionConflictResult
{
    public required string QnAId { get; init; }
    public required string SourceLabel { get; init; }
    public string? Note { get; init; }
}

public sealed class VoiceQuestionResult
{
    public string Transcript { get; init; } = "";
    public string Answer { get; init; } = "";
    public required string AnswerStatus { get; init; }
    public string? RelatedSlideObjectId { get; init; }

    /// <summary>KS-9 - null on every path except RagVoiceQuestionProvider's retrieval-grounded
    /// answer step, which is the only place Q&A content ever reaches the model.</summary>
    public VoiceQuestionConflictResult? Conflict { get; init; }
}

public interface IVoiceQuestionProvider
{
    Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input);

    /// <summary>F10/TQ-1 - text-in-text-out counterpart to TranscribeAndAnswerAsync, sharing the
    /// exact same grounding/answer pipeline behind the same contract, so switching
    /// VOICE_QUESTION_PROVIDER never leaves typed questions unsupported.</summary>
    Task<VoiceQuestionResult> AnswerTextAsync(TextQuestionInput input);
}
