using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class CreateKnowledgeQnADto
{
    [Required]
    public required string Question { get; init; }

    [Required]
    public required string Answer { get; init; }

    /// <summary>One of SupportRoom.Domain.Enums.KnowledgeScopeType. Prefilled by the frontend as
    /// "lesson" (QQ-8) but always sent explicitly - CS may change it before saving.</summary>
    [Required]
    public required string ScopeType { get; init; }

    public string? ScopeId { get; init; }

    /// <summary>QQ-7 - one Q&A can close several queue questions at once. At least one required:
    /// a Q&A written from scratch with nothing selected would never leave the queue.</summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> SessionQuestionIds { get; init; }
}

public sealed class UpdateKnowledgeQnADto
{
    [Required]
    public required string Question { get; init; }

    [Required]
    public required string Answer { get; init; }
}
