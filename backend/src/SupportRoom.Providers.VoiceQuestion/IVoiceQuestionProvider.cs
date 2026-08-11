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

public sealed class VoiceQuestionResult
{
    public string Transcript { get; init; } = "";
    public string Answer { get; init; } = "";
    public required string AnswerStatus { get; init; }
    public string? RelatedSlideObjectId { get; init; }

    /// <summary>Only set when Expecting == "readiness": did the teacher say they're ready to start?</summary>
    public string? Readiness { get; init; }
}

public interface IVoiceQuestionProvider
{
    Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input);
}
