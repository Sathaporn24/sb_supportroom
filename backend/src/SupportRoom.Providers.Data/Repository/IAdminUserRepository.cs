using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

/// <summary>
/// ⚠️ AdminUser has no company query filter (see ApplicationDbContext), so NOTHING here is scoped
/// automatically. Every method returns rows across all companies by design - the caller is
/// responsible for asking IAuthorizationGuard first. Treat each new method added here as
/// security-relevant.
/// </summary>
public interface IAdminUserRepository : IRepositoryBase<AdminUser, string>
{
    /// <summary>Sign-in lookup. Case-insensitive because people type their own email
    /// inconsistently and an address that differs only in case is the same account.</summary>
    AdminUser? GetByEmail(string email);

    /// <summary>The user list for one company's own management page.</summary>
    IQueryable<AdminUser> GetByCompanyId(string companyId);

    /// <summary>Used to refuse deactivating a company's last admin, which would leave its people
    /// unable to manage themselves and force every change back through School Bright.</summary>
    int CountActiveAdmins(string companyId);

    /// <summary>Same protection one level up: losing the last owner means nobody can manage
    /// companies or system settings again without hand-editing the database.</summary>
    int CountActiveOwners();

    /// <summary>True when no account exists at all - the only moment first-owner seeding runs.</summary>
    bool IsEmpty();
}

public sealed class AdminUserRepository(ApplicationDbContext dbContext)
    : RepositoryBase<AdminUser, string>(dbContext), IAdminUserRepository
{
    public AdminUser? GetByEmail(string email)
        => FindBy(x => x.Email.ToLower() == email.ToLower()).SingleOrDefault();

    public IQueryable<AdminUser> GetByCompanyId(string companyId)
        => FindBy(x => x.CompanyId == companyId).OrderBy(x => x.DisplayName);

    public int CountActiveAdmins(string companyId)
        => FindBy(x => x.CompanyId == companyId && x.Role == AdminRole.Admin && x.IsActive).Count();

    public int CountActiveOwners()
        => FindBy(x => x.Role == AdminRole.Owner && x.IsActive).Count();

    public bool IsEmpty() => !GetAll().Any();
}
