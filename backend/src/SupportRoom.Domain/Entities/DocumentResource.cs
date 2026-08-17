using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A CS-uploaded file (PowerPoint/PDF/DOCX) whose extracted text is embedded into the same
/// knowledge store as Google Slides notes. LessonId is nullable on purpose - a document can be
/// attached to one lesson (indexed into that lesson's Pinecone namespace) or stand alone as
/// part of a broader knowledge base (indexed into "kb-global") not tied to any single lesson.
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
    public string? DeleteBy { get; init; }
    public bool IsDelete { get; init; }
    public DateTime? DeletedAt { get; init; }

    /// <summary>Null = standalone/global knowledge-base document, not attached to a lesson.</summary>
    public string? LessonId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string ObsBucket { get; init; }
    public required string ObsKey { get; init; }

    /// <summary>"pending" | "indexed" | "failed" - same plain-string-constant convention as AnswerStatus/Status.</summary>
    public required string IndexingStatus { get; set; }
    public int IndexedChunkCount { get; set; }
}
