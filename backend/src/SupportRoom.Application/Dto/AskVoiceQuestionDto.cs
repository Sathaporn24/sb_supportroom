namespace SupportRoom.Application.Dto;

public sealed class AskVoiceQuestionDto
{
    public required byte[] Audio { get; init; }
    public required string MimeType { get; init; }

    /// <summary>
    /// The link's public join token - the credential the recipient actually holds. Everything
    /// else about the request (company, lesson) is derived from the row this points at, never
    /// taken from the caller. Sending a lesson slug alongside it would let a caller pair their
    /// own link with another company's lesson.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>Which learner on that link is asking. The token alone no longer identifies a
    /// person - one link is opened by many - so the answer would otherwise be filed under, and
    /// broadcast to, the wrong session.</summary>
    public required string LearnerKey { get; init; }

    public required int DurationMs { get; init; }
    public string? CurrentSlideObjectId { get; init; }
}
