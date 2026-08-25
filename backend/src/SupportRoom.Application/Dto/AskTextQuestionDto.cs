namespace SupportRoom.Application.Dto;

/// <summary>
/// F10 - a typed lesson question, the text-input equivalent of AskVoiceQuestionDto. No Audio,
/// MimeType or DurationMs, and no "is this a start-of-lesson yes/no reply" concept either: those
/// are all properties of the voice channel that a typed question does not have (TQ-7/TQ-11).
/// </summary>
public sealed class AskTextQuestionDto
{
    /// <summary>See AskVoiceQuestionDto.Token.</summary>
    public required string Token { get; init; }

    /// <summary>See AskVoiceQuestionDto.LearnerKey.</summary>
    public required string LearnerKey { get; init; }

    /// <summary>Trimmed by the controller before this DTO is built - the value here is always the
    /// exact text that ends up in SessionQuestion.Transcript.</summary>
    public required string Text { get; init; }

    public string? CurrentSlideObjectId { get; init; }
}
