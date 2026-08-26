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

    /// <summary>R9/LT-15 - purge's dependency snapshot needs every document of a lesson's scope
    /// regardless of soft-delete state (CS may have already trashed one before the lesson itself
    /// was archived). IgnoreQueryFilters() only exists to see past `!IsDelete` - CompanyId is
    /// reapplied explicitly in the same predicate (LT-23).</summary>
    IQueryable<DocumentResource> GetByScopeIncludingDeleted(string companyId, string scopeType, string? scopeId);

    /// <summary>R9/LT-15 - a lesson's PdfDocumentResourceId regardless of soft-delete state or
    /// scope (it may not be scope=lesson - e.g. a company/category-scoped document picked as a
    /// lesson's PDF source). IgnoreQueryFilters() only exists to see past `!IsDelete` - CompanyId
    /// is reapplied explicitly (LT-23).</summary>
    DocumentResource? GetByIdIncludingDeleted(string companyId, string id);
}

public sealed class DocumentResourceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DocumentResource, string>(dbContext), IDocumentResourceRepository
{
    public IQueryable<DocumentResource> GetByScope(string scopeType, string? scopeId)
        => FindBy(x => x.ScopeType == scopeType && x.ScopeId == scopeId);

    public IQueryable<DocumentResource> GetAllInCompany() => FindBy(_ => true);

    public IQueryable<DocumentResource> GetDeleted(string companyId)
        => Context.DocumentResource.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.IsDelete);

    public IQueryable<DocumentResource> GetByScopeIncludingDeleted(string companyId, string scopeType, string? scopeId)
        => Context.DocumentResource.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && x.ScopeType == scopeType && x.ScopeId == scopeId);

    public DocumentResource? GetByIdIncludingDeleted(string companyId, string id)
        => Context.DocumentResource.IgnoreQueryFilters().SingleOrDefault(x => x.CompanyId == companyId && x.Id == id);
}
