using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IDocumentResourceRepository : IRepositoryBase<DocumentResource, string>
{
    IQueryable<DocumentResource> GetByScope(string scopeType, string? scopeId);

    /// <summary>
    /// KL-2 - every non-deleted document of the caller's company, across every scope. Just
    /// FindBy(_ => true): isolation comes entirely from the EF global query filter (CompanyId +
    /// !IsDelete), same as every other scoped query in this project. IgnoreQueryFilters() must
    /// never be added here - see the GetDeleted comment below for what that mistake costs.
    /// </summary>
    IQueryable<DocumentResource> GetAllInCompany();

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

    public IQueryable<DocumentResource> GetAllInCompany() => FindBy(_ => true);

    public IQueryable<DocumentResource> GetDeleted(string companyId)
        => Context.DocumentResource.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.IsDelete);
}
