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

    /// <summary>KL-23/Q-H2 - the pre-save duplicate-question gate is unconditional (no
    /// CheckDuplicate flag to opt in). This is the opt-out: true skips the check entirely and
    /// saves normally even when a genuine duplicate exists, mirroring UploadDocumentDto's
    /// CheckDuplicate=false resubmit path.</summary>
    public bool ConfirmDuplicate { get; init; }
}

public sealed class UpdateKnowledgeQnADto
{
    [Required]
    public required string Question { get; init; }

    [Required]
    public required string Answer { get; init; }
}

/// <summary>KL-8/KL-9 - GET /api/knowledge-qna's query, interpreted identically to
/// GET /api/documents' (KL-2..KL-5, KL-11..KL-13). All four are optional; every combination is
/// AND'd together (KL-12).</summary>
public sealed class KnowledgeQnAFilter
{
    public string? ScopeType { get; init; }
    public string? ScopeId { get; init; }
    public string? Status { get; init; }
    public string? Q { get; init; }
}
