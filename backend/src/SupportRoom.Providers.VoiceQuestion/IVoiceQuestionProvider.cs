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

    /// <summary>
    /// "readiness" is the reply to the "พร้อมหรือยังคะ?" prompt - a yes/no, not a question. It
    /// skips the speaker-notes grounding entirely, which also makes it markedly faster than a
    /// full question round-trip.
    /// </summary>
    public string Expecting { get; init; } = "question";
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

    /// <summary>Only set when Expecting == "readiness": did the teacher say they're ready to start?</summary>
    public string? Readiness { get; init; }

    /// <summary>KS-9 - null on every path except RagVoiceQuestionProvider's retrieval-grounded
    /// answer step, which is the only place Q&A content ever reaches the model.</summary>
    public VoiceQuestionConflictResult? Conflict { get; init; }
}

public interface IVoiceQuestionProvider
{
    Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input);
}
