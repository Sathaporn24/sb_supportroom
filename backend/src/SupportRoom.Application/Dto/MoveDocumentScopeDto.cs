namespace SupportRoom.Application.Dto;

/// <summary>DS-5 - PATCH /api/documents/{id}/scope body. Same ScopeType/ScopeId shape as
/// UploadDocumentDto/CreateKnowledgeQnADto, validated the same way through
/// IKnowledgeNamespaceResolver.EnsureValidScope before anything is written (DS-6).</summary>
public sealed class MoveDocumentScopeDto
{
    public required string ScopeType { get; init; }
    public string? ScopeId { get; init; }
}
