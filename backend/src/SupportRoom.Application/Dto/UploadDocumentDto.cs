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

    /// <summary>KL-21 - opt-in, defaults false. False preserves the old upload behaviour byte for
    /// byte (including handlePdfUpload/UC-5, which never sends this) - true means "check KL-19/
    /// KL-20 before writing anything and 409 on a match" instead of uploading straight through.</summary>
    public bool CheckDuplicate { get; init; }
}
