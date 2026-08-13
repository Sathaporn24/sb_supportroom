using Mapster;
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

public interface IAdminUserService
{
    IReadOnlyList<AdminUserViewModel> GetByCompany(string companyId);
    AdminUserViewModel Create(CreateAdminUserDto input);
    AdminUserViewModel Update(string id, UpdateAdminUserDto input);
}

/// <summary>
/// User management for the back office. Every method starts by asking IAuthorizationGuard, because
/// AdminUser carries no company query filter - there is no second line of defence behind these
/// checks (see ApplicationDbContext).
/// </summary>
public sealed class AdminUserService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IAdminUserService> logger,
    ICurrentUser currentUser,
    IAuthorizationGuard guard,
    IPasswordHasher<AdminUser> passwordHasher)
    : ServiceBase<IAdminUserService>(unitOfWork, serviceProvider, logger), IAdminUserService
{
    private readonly IAdminUserRepository _users = unitOfWork.GetRepository<IAdminUserRepository>();
    private readonly ICompanyRepository _companies = unitOfWork.GetRepository<ICompanyRepository>();

    public IReadOnlyList<AdminUserViewModel> GetByCompany(string companyId)
    {
        guard.EnsureCanManageUsers(companyId);
        return _users.GetByCompanyId(companyId).ToList().Select(ToViewModel).ToList();
    }

    public AdminUserViewModel Create(CreateAdminUserDto input)
    {
        var role = input.Role;
        var companyId = ResolveTargetCompany(role, input.CompanyId);

        // Order matters. "May you manage users here at all" is asked before "may you hand out this
        // particular role", so a cs user gets the honest "you cannot manage users" rather than a
        // message about role ranks that implies they were close.
        if (role == AdminRole.Owner)
        {
            guard.EnsureOwner();
        }
        else
        {
            guard.EnsureCanManageUsers(companyId!);
        }
        guard.EnsureCanAssignRole(role);

        var email = input.Email.Trim();
        if (_users.GetByEmail(email) is not null)
        {
            throw GeneralException.ValidationError("อีเมลนี้ถูกใช้งานแล้ว");
        }

        var user = new AdminUser
        {
            Id = IdGenerator.GenerateId("user"),
            CompanyId = companyId,
            Role = role,
            Email = email,
            DisplayName = input.DisplayName.Trim(),
            IsActive = true,
            // Whoever creates the account knows the password they typed, so it cannot stay.
            MustChangePassword = true,
            CreateBy = currentUser.UserId,
            CreateDate = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, input.InitialPassword);

        _users.Add(user);
        UnitOfWork.Commit();

        Logger.LogInformation(
            "Admin user created: {UserId} role={Role} company={CompanyId} by={ActorId}",
            user.Id, user.Role, user.CompanyId, currentUser.UserId);

        return ToViewModel(user);
    }

    public AdminUserViewModel Update(string id, UpdateAdminUserDto input)
    {
        var user = _users.Get(id) ?? throw GeneralException.NotFound("ผู้ใช้");

        // Guard against the user's CURRENT role and company before anything else - otherwise a
        // company admin could name someone else's user id and start editing them.
        if (user.Role == AdminRole.Owner)
        {
            guard.EnsureOwner();
        }
        else if (string.IsNullOrEmpty(user.CompanyId))
        {
            // A company-scoped row with no company is corrupt. Refuse rather than fall through to
            // a check that would compare against null and could read as "matches".
            throw GeneralException.ConfigError("ข้อมูลผู้ใช้ไม่สมบูรณ์ - ไม่พบบริษัทของผู้ใช้นี้");
        }
        else
        {
            guard.EnsureCanManageUsers(user.CompanyId);
        }

        // And again for the role being assigned, which is what stops a company admin promoting
        // anyone - themselves included - to owner and walking into every other customer's data.
        guard.EnsureCanAssignRole(input.Role);

        EnsureNotRemovingLastGuardian(user, input);

        user.DisplayName = input.DisplayName.Trim();
        user.Role = input.Role;
        user.IsActive = input.IsActive;
        user.UpdateBy = currentUser.UserId;
        user.UpdateDate = DateTime.UtcNow;

        _users.Update(user);
        UnitOfWork.Commit();

        Logger.LogInformation(
            "Admin user updated: {UserId} role={Role} active={IsActive} by={ActorId}",
            user.Id, user.Role, user.IsActive, currentUser.UserId);

        return ToViewModel(user);
    }

    /// <summary>
    /// Refuses any change that would remove the last person able to manage a given scope.
    ///
    /// Neither case is a security hole - both are lockouts that can only be undone by editing the
    /// database by hand. Losing a company's last admin forces every future user change back through
    /// School Bright, which is the exact bottleneck this role split exists to remove. Losing the
    /// last owner is worse: nobody can manage companies or system settings again at all.
    ///
    /// "Removing" covers demotion as well as deactivation, because a demoted admin is just as gone
    /// from the admin list as a disabled one.
    /// </summary>
    private void EnsureNotRemovingLastGuardian(AdminUser user, UpdateAdminUserDto input)
    {
        var wasActiveOwner = user.Role == AdminRole.Owner && user.IsActive;
        var staysActiveOwner = input.Role == AdminRole.Owner && input.IsActive;
        if (wasActiveOwner && !staysActiveOwner && _users.CountActiveOwners() <= 1)
        {
            throw GeneralException.ValidationError(
                "ไม่สามารถปิดหรือลดสิทธิ์ผู้ดูแลระบบคนสุดท้ายได้ กรุณาตั้งผู้ดูแลระบบคนใหม่ก่อน");
        }

        var wasActiveAdmin = user.Role == AdminRole.Admin && user.IsActive;
        var staysActiveAdmin = input.Role == AdminRole.Admin && input.IsActive;
        if (wasActiveAdmin && !staysActiveAdmin
            && user.CompanyId is { Length: > 0 } companyId
            && _users.CountActiveAdmins(companyId) <= 1)
        {
            throw GeneralException.ValidationError(
                "ไม่สามารถปิดหรือลดสิทธิ์ผู้ดูแลคนสุดท้ายของบริษัทได้ กรุณาตั้งผู้ดูแลคนใหม่ก่อน");
        }
    }

    /// <summary>
    /// Works out which company a new user belongs to, and never simply trusts the request: a
    /// company admin's own CompanyId wins over whatever was posted, so a crafted body cannot place
    /// a user in someone else's company.
    /// </summary>
    private string? ResolveTargetCompany(string role, string? requestedCompanyId)
    {
        if (!AdminRole.IsValid(role))
        {
            throw GeneralException.ValidationError("สิทธิ์ที่ระบุไม่ถูกต้อง");
        }

        if (role == AdminRole.Owner)
        {
            return null;
        }

        var companyId = currentUser.Role == AdminRole.Owner
            ? requestedCompanyId
            : currentUser.CompanyId;

        if (string.IsNullOrWhiteSpace(companyId))
        {
            throw GeneralException.ValidationError("กรุณาระบุบริษัทของผู้ใช้");
        }

        if (!_companies.ExistsActive(companyId))
        {
            throw GeneralException.NotFound("บริษัท");
        }

        return companyId;
    }

    private static AdminUserViewModel ToViewModel(AdminUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Role = user.Role,
        CompanyId = user.CompanyId,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt.Adapt<string>(),
        CreatedAt = user.CreateDate.Adapt<string>(),
    };
}
