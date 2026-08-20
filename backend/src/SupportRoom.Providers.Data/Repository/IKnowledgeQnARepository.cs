using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IKnowledgeQnARepository : IRepositoryBase<KnowledgeQnA, string>
{
    IQueryable<KnowledgeQnA> GetByScope(string scopeType, string? scopeId);

    /// <summary>Simple substring match against Question/Answer - a keyword search, not full-text
    /// ranking (the knowledge base itself is what handles ranked retrieval).</summary>
    IQueryable<KnowledgeQnA> Search(string keyword);
}

public sealed class KnowledgeQnARepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeQnA, string>(dbContext), IKnowledgeQnARepository
{
    public IQueryable<KnowledgeQnA> GetByScope(string scopeType, string? scopeId)
        => FindBy(x => x.ScopeType == scopeType && x.ScopeId == scopeId);

    public IQueryable<KnowledgeQnA> Search(string keyword)
        => FindBy(x => x.Question.Contains(keyword) || x.Answer.Contains(keyword));
}
