using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>JWT claims are intentionally staleable. These tests prove the server refreshes the
/// authorization-bearing identity from the stored account on every protected request.</summary>
public sealed class AuthServiceAuthorizationStateTests
{
    private readonly FakeAdminUserRepository _users = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CurrentUser _currentUser = new();

    public AuthServiceAuthorizationStateTests()
    {
        _unitOfWork
            .Register<IAdminUserRepository>(_users)
            .Register<ICompanyRepository>(_companies);
    }

    [Fact]
    public void RefreshCurrentUser_UsesStoredRoleAndCompanyInsteadOfStaleTokenClaims()
    {
        SeedCompany("company-a");
        SeedCompany("company-b");
        SeedUser(role: AdminRole.Admin, companyId: "company-b");
        _currentUser.Resolve("user-1", AdminRole.Owner, companyId: null);

        Service().RefreshCurrentUser();

        Assert.Equal(AdminRole.Admin, _currentUser.Role);
        Assert.Equal("company-b", _currentUser.CompanyId);
        Assert.Throws<HttpStatusCodeException>(() => new AuthorizationGuard(_currentUser).EnsureOwner());
        Assert.Throws<HttpStatusCodeException>(() => new AuthorizationGuard(_currentUser).EnsureCanAccessCompany("company-a"));
    }

    [Fact]
    public void RefreshCurrentUser_RejectsADeactivatedAccount()
    {
        SeedUser(isActive: false);
        _currentUser.Resolve("user-1", AdminRole.Admin, "company-a");

        var exception = Assert.Throws<HttpStatusCodeException>(() => Service().RefreshCurrentUser());

        Assert.Equal(ApiErrorCode.Unauthorized, exception.Code);
    }

    [Fact]
    public void RefreshCurrentUser_RejectsWhenTheAssignedCompanyIsInactive()
    {
        SeedCompany("company-a", isActive: false);
        SeedUser(companyId: "company-a");
        _currentUser.Resolve("user-1", AdminRole.Admin, "company-a");

        var exception = Assert.Throws<HttpStatusCodeException>(() => Service().RefreshCurrentUser());

        Assert.Equal(ApiErrorCode.Unauthorized, exception.Code);
    }

    [Fact]
    public void RefreshCurrentUser_RejectsAnUnknownStoredRole()
    {
        SeedUser(role: "unknown", companyId: null);
        _currentUser.Resolve("user-1", AdminRole.Owner, companyId: null);

        var exception = Assert.Throws<HttpStatusCodeException>(() => Service().RefreshCurrentUser());

        Assert.Equal(ApiErrorCode.Unauthorized, exception.Code);
    }

    private AuthService Service() => new(
        _unitOfWork,
        new FakeServiceProvider(),
        NullLogger<IAuthService>.Instance,
        _currentUser,
        new PasswordHasher<AdminUser>());

    private void SeedCompany(string id, bool isActive = true) => _companies.Items.Add(new Company
    {
        Id = id,
        Name = id,
        IsActive = isActive,
        CreateDate = DateTime.UtcNow,
    });

    private void SeedUser(string role = AdminRole.Admin, string? companyId = "company-a", bool isActive = true)
        => _users.Items.Add(new AdminUser
        {
            Id = "user-1",
            CompanyId = companyId,
            Role = role,
            Email = "user-1@example.com",
            DisplayName = "User 1",
            IsActive = isActive,
            MustChangePassword = true,
            CreateDate = DateTime.UtcNow,
        });
}
