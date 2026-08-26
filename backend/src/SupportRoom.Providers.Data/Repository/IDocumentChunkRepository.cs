using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IDocumentChunkRepository : IRepositoryBase<DocumentChunk, string>
{
    IQueryable<DocumentChunk> GetByDocumentId(string documentId);

    /// <summary>DI-8 - soft deletes every existing chunk row of this document, so a caller can
    /// write the freshly-extracted replacement set right after without a duplicate SeqNo/ChunkKey
    /// ever lingering from a previous index run.</summary>
    void DeleteByDocumentId(string documentId);

    /// <summary>R9/LT-15/LT-17/LT-19 - every chunk row of this document regardless of soft-delete
    /// state, used both to compute the full set of vector ids to delete externally and, after that
    /// succeeds, to hard-delete each row via Delete(). IgnoreQueryFilters() only exists to see past
    /// `!IsDelete` - CompanyId is reapplied explicitly (LT-23).</summary>
    IQueryable<DocumentChunk> GetAllByDocumentIdIncludingDeleted(string companyId, string documentId);
}

public sealed class DocumentChunkRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DocumentChunk, string>(dbContext), IDocumentChunkRepository
{
    public IQueryable<DocumentChunk> GetByDocumentId(string documentId)
        => FindBy(x => x.DocumentId == documentId).OrderBy(x => x.SeqNo);

    public void DeleteByDocumentId(string documentId)
    {
        var now = DateTime.UtcNow;
        foreach (var chunk in FindBy(x => x.DocumentId == documentId))
        {
            chunk.IsDelete = true;
            chunk.DeletedAt = now;
            Update(chunk);
        }
    }

    public IQueryable<DocumentChunk> GetAllByDocumentIdIncludingDeleted(string companyId, string documentId)
        => Context.DocumentChunk.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && x.DocumentId == documentId);
}
