using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

/// <summary>
/// ⚠️ Company has no company query filter (it IS the tenant registry - see ApplicationDbContext),
/// so nothing here is scoped automatically. Callers must apply IAuthorizationGuard themselves;
/// this repository only answers "what exists".
/// </summary>
public interface ICompanyRepository : IRepositoryBase<Company, string>
{
    /// <summary>Every active company, name-ordered - the owner's switcher list.</summary>
    IQueryable<Company> GetAllActive();

    bool ExistsActive(string id);
}

public sealed class CompanyRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Company, string>(dbContext), ICompanyRepository
{
    public IQueryable<Company> GetAllActive()
        => FindBy(x => x.IsActive).OrderBy(x => x.Name);

    public bool ExistsActive(string id)
        => FindBy(x => x.Id == id && x.IsActive).Any();
}
