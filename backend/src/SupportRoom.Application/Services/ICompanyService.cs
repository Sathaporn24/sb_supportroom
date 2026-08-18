using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
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

    CompanyViewModel Create(CreateCompanyDto input);
    CompanyViewModel Update(string id, UpdateCompanyDto input);
}

public sealed class CompanyService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ICompanyService> logger,
    ICurrentUser currentUser,
    IAuthorizationGuard guard)
    : ServiceBase<ICompanyService>(unitOfWork, serviceProvider, logger), ICompanyService
{
    private readonly ICompanyRepository _companies = unitOfWork.GetRepository<ICompanyRepository>();

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

    public CompanyViewModel Create(CreateCompanyDto input)
    {
        guard.EnsureOwner();

        var id = input.Id.Trim().ToLowerInvariant();
        if (!CompanySlug.IsValid(id))
        {
            throw GeneralException.ValidationError(CompanySlug.RuleTh);
        }

        if (_companies.Get(id) is not null)
        {
            throw GeneralException.ValidationError("รหัสบริษัทนี้ถูกใช้งานแล้ว");
        }

        var company = new Company
        {
            Id = id,
            Name = input.Name.Trim(),
            IsActive = true,
            CreateBy = currentUser.UserId,
            CreateDate = DateTime.UtcNow,
        };

        _companies.Add(company);
        UnitOfWork.Commit();

        Logger.LogInformation("Company created: {CompanyId} by={ActorId}", company.Id, currentUser.UserId);

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
