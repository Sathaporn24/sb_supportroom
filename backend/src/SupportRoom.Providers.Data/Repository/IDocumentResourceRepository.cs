using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IDocumentResourceRepository : IRepositoryBase<DocumentResource, string>
{
    IQueryable<DocumentResource> GetByScope(string scopeType, string? scopeId);

    /// <summary>
    /// IgnoreQueryFilters() only exists here to see past the `!IsDelete` half of the query filter
    /// (see ApplicationDbContext) - the soft-deleted rows this method exists to return are exactly
    /// what that half hides. IgnoreQueryFilters() drops the CompanyId half too though, so
    /// companyId is reapplied explicitly below; without it this call would leak every company's
    /// deleted documents to whichever company happens to be asking.
    /// </summary>
    IQueryable<DocumentResource> GetDeleted(string companyId);
}

public sealed class DocumentResourceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DocumentResource, string>(dbContext), IDocumentResourceRepository
{
    public IQueryable<DocumentResource> GetByScope(string scopeType, string? scopeId)
        => FindBy(x => x.ScopeType == scopeType && x.ScopeId == scopeId);

    public IQueryable<DocumentResource> GetDeleted(string companyId)
        => Context.DocumentResource.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.IsDelete);
}
