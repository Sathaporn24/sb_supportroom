namespace SupportRoom.Application.Dto;

public sealed class AskVoiceQuestionDto
{
    public required byte[] Audio { get; init; }
    public required string MimeType { get; init; }
    public required string LessonSlug { get; init; }
    public required string SessionId { get; init; }
    public required int DurationMs { get; init; }
    public string? CurrentSlideObjectId { get; init; }

    /// <summary>"readiness" answers the start prompt; "question" (default) is a normal lesson question.</summary>
    public string Expecting { get; init; } = "question";
}
