namespace SupportRoom.Application.Dto;

public sealed class UploadDocumentDto
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    /// <summary>DS-1 - one of SupportRoom.Domain.Enums.KnowledgeScopeType, same shape as
    /// CreateKnowledgeQnADto.ScopeType. When "lesson", ScopeId is LessonConfig.Id (not Slug), so
    /// it goes through the same IKnowledgeNamespaceResolver as every other scoped entity.</summary>
    public required string ScopeType { get; init; }

    public string? ScopeId { get; init; }
}
