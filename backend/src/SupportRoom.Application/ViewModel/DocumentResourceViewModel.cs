namespace SupportRoom.Application.ViewModel;

public sealed class DocumentResourceViewModel
{
    public required string Id { get; init; }
    public required string ScopeType { get; init; }
    public string? ScopeId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string IndexingStatus { get; init; }
    public required int IndexedChunkCount { get; init; }
    public string? FailureReason { get; init; }
    public required string CreatedAt { get; init; }

    /// <summary>DI-10 - set only while a retry is still scheduled (job pending/running and under
    /// the attempt cap), so the UI can tell "failed, but will retry itself" apart from "failed,
    /// needs a person". Null once the job has given up or when nothing has ever run yet.</summary>
    public string? WillRetryAt { get; init; }

    /// <summary>R-4/DI-16 - true while a vector_delete BackgroundJob for this document is still
    /// pending/running. Only meaningful on a soft-deleted row (GetDeleted()): the document itself
    /// is already gone from the UI, but its vectors may still be reachable in Pinecone until this
    /// clears - shown so an admin doesn't assume "deleted" already means "unsearchable".</summary>
    public required bool HasPendingVectorDelete { get; init; }
}

/// <summary>KL-21 - the 409 Conflict payload when UploadDocumentDto.CheckDuplicate finds a match.
/// Both lists carry only the fields KL-21 allows out (id/fileName/scopeType/scopeId/createDate) -
/// no ContentHash, no other metadata.</summary>
public sealed class DuplicateDocumentDto
{
    public required IReadOnlyList<DuplicateDocumentEntryViewModel> DuplicateByHash { get; init; }
    public required IReadOnlyList<DuplicateDocumentEntryViewModel> DuplicateByFileName { get; init; }
}

public sealed class DuplicateDocumentEntryViewModel
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required string ScopeType { get; init; }
    public string? ScopeId { get; init; }
    public required string CreatedAt { get; init; }
}
