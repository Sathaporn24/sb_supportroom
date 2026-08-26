using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IKnowledgeQnAConflictRepository : IRepositoryBase<KnowledgeQnAConflict, string>
{
    /// <summary>QQ-10 - the conflict-flags screen's data source. Its own page, not a badge on the
    /// queue, because the follow-up action is different: fix the document, not write an answer.</summary>
    IQueryable<KnowledgeQnAConflict> GetUnresolved();

    /// <summary>R9/LT-19 - every conflict row of these Q&amp;A ids regardless of soft-delete state,
    /// for purge's hard-delete step. IgnoreQueryFilters() only exists to see past `!IsDelete` -
    /// CompanyId is reapplied explicitly (LT-23).</summary>
    IQueryable<KnowledgeQnAConflict> GetByQnAIdsIncludingDeleted(string companyId, IReadOnlyList<string> qnaIds);
}

public sealed class KnowledgeQnAConflictRepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeQnAConflict, string>(dbContext), IKnowledgeQnAConflictRepository
{
    public IQueryable<KnowledgeQnAConflict> GetUnresolved()
        => FindBy(x => x.ResolvedAt == null);

    public IQueryable<KnowledgeQnAConflict> GetByQnAIdsIncludingDeleted(string companyId, IReadOnlyList<string> qnaIds)
        => Context.KnowledgeQnAConflict.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && qnaIds.Contains(x.QnAId));
}
