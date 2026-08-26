using Microsoft.EntityFrameworkCore;
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

    /// <summary>KL-9 - same shape as IDocumentResourceRepository.GetAllInCompany() in every
    /// respect: FindBy(_ => true), isolation left entirely to the EF global query filter, never
    /// IgnoreQueryFilters().</summary>
    IQueryable<KnowledgeQnA> GetAllInCompany();

    /// <summary>R9/LT-15/LT-19 - every Q&amp;A of this scope regardless of soft-delete state, for
    /// purge's dependency snapshot and hard-delete step. IgnoreQueryFilters() only exists to see
    /// past `!IsDelete` - CompanyId is reapplied explicitly (LT-23).</summary>
    IQueryable<KnowledgeQnA> GetByScopeIncludingDeleted(string companyId, string scopeType, string? scopeId);
}

public sealed class KnowledgeQnARepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeQnA, string>(dbContext), IKnowledgeQnARepository
{
    public IQueryable<KnowledgeQnA> GetByScope(string scopeType, string? scopeId)
        => FindBy(x => x.ScopeType == scopeType && x.ScopeId == scopeId);

    public IQueryable<KnowledgeQnA> Search(string keyword)
        => FindBy(x => x.Question.Contains(keyword) || x.Answer.Contains(keyword));

    public IQueryable<KnowledgeQnA> GetAllInCompany() => FindBy(_ => true);

    public IQueryable<KnowledgeQnA> GetByScopeIncludingDeleted(string companyId, string scopeType, string? scopeId)
        => Context.KnowledgeQnA.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && x.ScopeType == scopeType && x.ScopeId == scopeId);
}
