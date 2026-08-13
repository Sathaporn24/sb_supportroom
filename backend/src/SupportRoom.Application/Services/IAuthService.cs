using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

/// <summary>Claim names this API issues and reads. Constants because a typo in either the issuing
/// or the reading half produces an unauthenticated user rather than an error.</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Role = "role";
    public const string CompanyId = "company_id";
}

public interface IAuthService
{
    LoginResultViewModel Login(LoginDto input);
    SignedInUserViewModel GetSignedInUser();
    void ChangePassword(ChangePasswordDto input);

    /// <summary>Creates the very first owner from environment variables, but only when no account
    /// exists at all. Called once at startup - see SeedFirstOwnerHostedService.</summary>
    void SeedFirstOwnerIfEmpty();
}

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IAuthService> logger,
    ICurrentUser currentUser,
    IPasswordHasher<AdminUser> passwordHasher)
    : ServiceBase<IAuthService>(unitOfWork, serviceProvider, logger), IAuthService
{
    private readonly IAdminUserRepository _users = unitOfWork.GetRepository<IAdminUserRepository>();
    private readonly ICompanyRepository _companies = unitOfWork.GetRepository<ICompanyRepository>();

    public LoginResultViewModel Login(LoginDto input)
    {
        var user = _users.GetByEmail(input.Email.Trim());

        // One message for "no such email" and for "wrong password" on purpose: distinguishing them
        // turns this endpoint into an oracle for which addresses hold accounts here.
        if (user is null || user.PasswordHash is null)
        {
            // Log the attempt, never the password. Email is the whole point of the record.
            Logger.LogWarning("Login failed: no account or no password set for {Email}", input.Email);
            throw GeneralException.Unauthorized("อีเมลหรือรหัสผ่านไม่ถูกต้อง");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            Logger.LogWarning("Login failed: wrong password for {UserId}", user.Id);
            throw GeneralException.Unauthorized("อีเมลหรือรหัสผ่านไม่ถูกต้อง");
        }

        // Checked after the password, not before, so a deactivated account is indistinguishable
        // from a wrong password to someone guessing.
        if (!user.IsActive)
        {
            Logger.LogWarning("Login refused: account deactivated {UserId}", user.Id);
            throw GeneralException.Unauthorized("บัญชีนี้ถูกปิดการใช้งาน กรุณาติดต่อผู้ดูแล");
        }

        EnsureCompanyStillUsable(user);

        // Identity tells us when a stored hash used older parameters than the current ones. Silently
        // upgrading on a successful login is the only moment the plaintext is available to do it.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
        }

        user.LastLoginAt = DateTime.UtcNow;
        _users.Update(user);
        UnitOfWork.Commit();

        Logger.LogInformation("Login ok: {UserId} role={Role} company={CompanyId}", user.Id, user.Role, user.CompanyId);

        return IssueToken(user);
    }

    /// <summary>
    /// A company-scoped account whose company was deactivated must not be able to sign in. Without
    /// this, offboarding a customer would leave their staff with working logins into data that is
    /// supposed to be closed off.
    /// </summary>
    private void EnsureCompanyStillUsable(AdminUser user)
    {
        if (!AdminRole.IsCompanyScoped(user.Role))
        {
            return;
        }

        if (string.IsNullOrEmpty(user.CompanyId) || !_companies.ExistsActive(user.CompanyId))
        {
            Logger.LogWarning("Login refused: company inactive or missing for {UserId}", user.Id);
            throw GeneralException.Unauthorized("บริษัทของบัญชีนี้ถูกปิดการใช้งาน กรุณาติดต่อผู้ดูแล");
        }
    }

    private static LoginResultViewModel IssueToken(AdminUser user)
    {
        var settings = AuthEnv.GetJwt();
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaims.UserId, user.Id),
            new(AuthClaims.Role, user.Role),
        };

        // Absent rather than empty for an owner: an empty-string company would compare equal to
        // nothing and read as a real value at a glance. Absent forces the reader to handle it.
        if (user.CompanyId is { Length: > 0 })
        {
            claims.Add(new Claim(AuthClaims.CompanyId, user.CompanyId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResultViewModel
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt.Adapt<string>(),
            User = ToViewModel(user),
        };
    }

    public SignedInUserViewModel GetSignedInUser()
    {
        if (!currentUser.IsAuthenticated)
        {
            throw GeneralException.Unauthorized("กรุณาเข้าสู่ระบบก่อน");
        }

        var user = _users.Get(currentUser.UserId!) ?? throw GeneralException.Unauthorized("ไม่พบบัญชีผู้ใช้");
        return ToViewModel(user);
    }

    public void ChangePassword(ChangePasswordDto input)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw GeneralException.Unauthorized("กรุณาเข้าสู่ระบบก่อน");
        }

        var user = _users.Get(currentUser.UserId!) ?? throw GeneralException.Unauthorized("ไม่พบบัญชีผู้ใช้");

        if (user.PasswordHash is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw GeneralException.ValidationError("รหัสผ่านปัจจุบันไม่ถูกต้อง");
        }

        if (input.NewPassword == input.CurrentPassword)
        {
            throw GeneralException.ValidationError("รหัสผ่านใหม่ต้องไม่ซ้ำกับรหัสผ่านเดิม");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, input.NewPassword);
        user.MustChangePassword = false;
        user.UpdateBy = user.Id;
        user.UpdateDate = DateTime.UtcNow;
        _users.Update(user);
        UnitOfWork.Commit();

        Logger.LogInformation("Password changed: {UserId}", user.Id);
    }

    public void SeedFirstOwnerIfEmpty()
    {
        if (!_users.IsEmpty())
        {
            return;
        }

        var seed = AuthEnv.GetFirstOwnerSeed();
        if (seed is null)
        {
            Logger.LogWarning(
                "No admin accounts exist and FIRST_OWNER_EMAIL/FIRST_OWNER_PASSWORD are unset - "
                + "nobody can sign in. Set them and restart to create the first owner.");
            return;
        }

        var owner = new AdminUser
        {
            Id = IdGenerator.GenerateId("user"),
            CompanyId = null,               // owner spans every company
            Role = AdminRole.Owner,
            Email = seed.Email,
            DisplayName = seed.DisplayName,
            IsActive = true,
            // The password came from an environment variable, so whoever deployed knows it.
            MustChangePassword = true,
            CreateDate = DateTime.UtcNow,
        };
        owner.PasswordHash = passwordHasher.HashPassword(owner, seed.Password);

        _users.Add(owner);
        UnitOfWork.Commit();

        // Email only - never the password, even at startup.
        Logger.LogWarning("Seeded first owner account {Email} - it must change its password on first sign-in", seed.Email);
    }

    private static SignedInUserViewModel ToViewModel(AdminUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Role = user.Role,
        CompanyId = user.CompanyId,
        MustChangePassword = user.MustChangePassword,
    };
}
