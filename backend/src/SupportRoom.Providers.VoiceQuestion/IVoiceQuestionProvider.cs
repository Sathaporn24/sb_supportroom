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

    /// <summary>Knowledge-store namespace key for retrieval-augmented providers - unused by
    /// providers that ground on the full LessonSlides list directly.</summary>
    public required string LessonSlug { get; init; }

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
