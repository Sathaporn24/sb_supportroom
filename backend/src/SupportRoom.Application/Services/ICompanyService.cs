using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface ICompanyService
{
    /// <summary>What the signed-in user may switch between: every active company for an owner,
    /// exactly their own for anyone else.</summary>
    IReadOnlyList<CompanyViewModel> GetSwitchableCompanies();
    IReadOnlyList<CompanyViewModel> GetAllIncludingInactive();

    CompanyViewModel Create(CreateCompanyDto input);
    CompanyViewModel Update(string id, UpdateCompanyDto input);
}

public sealed class CompanyService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ICompanyService> logger,
    ICurrentUser currentUser,
    IAuthorizationGuard guard,
    IPasswordHasher<AdminUser> passwordHasher,
    IKnowledgeCategoryService knowledgeCategoryService)
    : ServiceBase<ICompanyService>(unitOfWork, serviceProvider, logger), ICompanyService
{
    private readonly ICompanyRepository _companies = unitOfWork.GetRepository<ICompanyRepository>();
    private readonly IAdminUserRepository _users = unitOfWork.GetRepository<IAdminUserRepository>();

    /// <summary>
    /// Company has no query filter (it is the tenant registry), so the scoping below is written by
    /// hand and is the only thing stopping one customer from enumerating the others. A customer's
    /// own company name is theirs to see; every other customer's name is not.
    /// </summary>
    public IReadOnlyList<CompanyViewModel> GetSwitchableCompanies()
    {
        guard.EnsureAuthenticated();

        if (currentUser.Role == AdminRole.Owner)
        {
            return _companies.GetAllActive().ToList().Select(ToViewModel).ToList();
        }

        if (string.IsNullOrEmpty(currentUser.CompanyId))
        {
            return [];
        }

        var own = _companies.Get(currentUser.CompanyId);
        return own is { IsActive: true } ? [ToViewModel(own)] : [];
    }

    public IReadOnlyList<CompanyViewModel> GetAllIncludingInactive()
    {
        guard.EnsureOwner();
        return _companies.GetAllIncludingInactive().ToList().Select(ToViewModel).ToList();
    }

    public CompanyViewModel Create(CreateCompanyDto input)
    {
        guard.EnsureOwner();

        var id = input.Id.Trim().ToLowerInvariant();
        if (!CompanySlug.IsValid(id))
        {
            throw GeneralException.ValidationError(CompanySlug.RuleTh);
        }

        var existingCompany = _companies.Get(id);
        if (existingCompany is { IsActive: true })
        {
            throw GeneralException.ValidationError("รหัสบริษัทนี้ถูกใช้งานแล้ว");
        }
        if (existingCompany is not null)
        {
            throw GeneralException.ValidationError(
                "มีบริษัทรหัสนี้อยู่แล้วแต่ถูกปิดใช้งาน หากต้องการใช้งานอีกครั้ง ให้เปิดกลับจากหน้ารายการบริษัท ไม่ใช่สร้างใหม่");
        }

        var email = input.AdminEmail.Trim();
        if (_users.GetByEmail(email) is not null)
        {
            throw GeneralException.ValidationError("อีเมลนี้ถูกใช้งานแล้ว");
        }

        var createdAt = DateTime.UtcNow;
        var company = new Company
        {
            Id = id,
            Name = input.Name.Trim(),
            IsActive = true,
            CreateBy = currentUser.UserId,
            CreateDate = createdAt,
        };
        var adminUser = new AdminUser
        {
            Id = IdGenerator.GenerateId("user"),
            CompanyId = id,
            Role = AdminRole.Admin,
            Email = email,
            DisplayName = input.AdminDisplayName.Trim(),
            IsActive = true,
            MustChangePassword = true,
            CreateBy = currentUser.UserId,
            CreateDate = createdAt,
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, input.AdminInitialPassword);

        _companies.Add(company);
        _users.Add(adminUser);
        knowledgeCategoryService.CreateDefaultChain(company.Id);
        UnitOfWork.Commit();

        Logger.LogInformation(
            "Company created: {CompanyId} admin={AdminUserId} by={ActorId}",
            company.Id, adminUser.Id, currentUser.UserId);

        return ToViewModel(company);
    }

    public CompanyViewModel Update(string id, UpdateCompanyDto input)
    {
        guard.EnsureOwner();

        var company = _companies.Get(id) ?? throw GeneralException.NotFound("บริษัท");

        company.Name = input.Name.Trim();
        company.IsActive = input.IsActive;
        company.UpdateBy = currentUser.UserId;
        company.UpdateDate = DateTime.UtcNow;

        _companies.Update(company);
        UnitOfWork.Commit();

        // Deactivating is how a customer is offboarded: their data stays, but their staff can no
        // longer sign in (see AuthService.EnsureCompanyStillUsable). Worth a louder log line than
        // a rename.
        if (!input.IsActive)
        {
            Logger.LogWarning("Company deactivated: {CompanyId} by={ActorId}", company.Id, currentUser.UserId);
        }

        return ToViewModel(company);
    }

    private static CompanyViewModel ToViewModel(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        IsActive = company.IsActive,
    };
}
