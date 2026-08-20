using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IKnowledgeQnAConflictRepository : IRepositoryBase<KnowledgeQnAConflict, string>
{
    /// <summary>QQ-10 - the conflict-flags screen's data source. Its own page, not a badge on the
    /// queue, because the follow-up action is different: fix the document, not write an answer.</summary>
    IQueryable<KnowledgeQnAConflict> GetUnresolved();
}

public sealed class KnowledgeQnAConflictRepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeQnAConflict, string>(dbContext), IKnowledgeQnAConflictRepository
{
    public IQueryable<KnowledgeQnAConflict> GetUnresolved()
        => FindBy(x => x.ResolvedAt == null);
}
