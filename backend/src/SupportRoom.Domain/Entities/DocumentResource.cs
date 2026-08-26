using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A CS-uploaded file (PowerPoint/PDF/DOCX) whose extracted text is embedded into the same
/// knowledge store as Google Slides notes. ScopeType/ScopeId determine whether it belongs to a
/// lesson, category, or the whole company.
/// The file bytes live in object storage (see SupportRoom.Providers.Storage) - only metadata
/// and the storage location live here.
/// </summary>
public sealed class DocumentResource : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string ScopeType { get; set; }
    public string? ScopeId { get; set; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string ObsBucket { get; init; }
    public required string ObsKey { get; init; }

    /// <summary>"pending" | "indexed" | "failed" - same plain-string-constant convention as AnswerStatus/Status.</summary>
    public required string IndexingStatus { get; set; }
    public int IndexedChunkCount { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>R7.5 (Q-H1 = ทาง B) - the file's fingerprint for "warn on duplicate content" (KL-18).
    /// SHA-256 of the whole file's bytes, stored as lowercase hex, 64 characters, no prefix/dashes.
    /// Computed once at upload time and never touched again - moving scope, soft delete/restore,
    /// and re-index never recompute it (KL-18).
    /// Null in exactly two cases: (1) a row uploaded before MG-H1 - intentionally not backfilled
    /// (KL-24) - and (2) no other case: every upload after MG-H1 always sets it.
    /// Null must never be treated as equal to another null when checking for duplicates (KL-19).</summary>
    public string? ContentHash { get; init; }
}
