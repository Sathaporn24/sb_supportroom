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
}
