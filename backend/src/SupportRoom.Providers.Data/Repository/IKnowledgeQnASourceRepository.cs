using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IKnowledgeQnASourceRepository : IRepositoryBase<KnowledgeQnASource, string>
{
    /// <summary>QQ-1/QQ-4 - one batched lookup for the whole review-queue page, never one call per
    /// row: given every candidate question's id, returns the subset that already has a Q&A closing
    /// it (so the caller can exclude them). A per-row Get would turn a page of N questions into N
    /// queries.</summary>
    IQueryable<KnowledgeQnASource> GetBySessionQuestionIds(IReadOnlyList<string> sessionQuestionIds);

    IQueryable<KnowledgeQnASource> GetByQnAId(string qnaId);
}

public sealed class KnowledgeQnASourceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeQnASource, string>(dbContext), IKnowledgeQnASourceRepository
{
    public IQueryable<KnowledgeQnASource> GetBySessionQuestionIds(IReadOnlyList<string> sessionQuestionIds)
        => FindBy(x => sessionQuestionIds.Contains(x.SessionQuestionId));

    public IQueryable<KnowledgeQnASource> GetByQnAId(string qnaId)
        => FindBy(x => x.QnAId == qnaId);
}
