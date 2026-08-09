namespace SupportRoom.Application.Dto;

public sealed class UploadDocumentDto
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    /// <summary>Omit to store as a standalone/global knowledge-base document, not tied to any lesson.</summary>
    public string? LessonSlug { get; init; }
}
