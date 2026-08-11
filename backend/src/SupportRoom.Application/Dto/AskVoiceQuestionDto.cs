namespace SupportRoom.Application.Dto;

public sealed class AskVoiceQuestionDto
{
    public required byte[] Audio { get; init; }
    public required string MimeType { get; init; }

    /// <summary>
    /// The session's public join token - the credential the recipient actually holds. Everything
    /// else about the request (company, lesson, session id) is derived from the row this points
    /// at, never taken from the caller. Sending a lesson slug alongside it would let a caller
    /// pair their own session with another company's lesson.
    /// </summary>
    public required string Token { get; init; }

    public required int DurationMs { get; init; }
    public string? CurrentSlideObjectId { get; init; }

    /// <summary>"readiness" answers the start prompt; "question" (default) is a normal lesson question.</summary>
    public string Expecting { get; init; } = "question";
}
