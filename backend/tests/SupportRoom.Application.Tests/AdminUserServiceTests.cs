using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

/// <summary>
/// The escalation and lockout rules, tested through the service rather than only through the guard
/// - the guard proves the rules are right, these prove the service actually asks them, in the
/// right order, against the right company.
/// </summary>
public class AdminUserServiceTests
{
    private const string CompanyA = "company-a";
    private const string CompanyB = "company-b";

    private readonly FakeAdminUserRepository _users = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public AdminUserServiceTests()
    {
        _unitOfWork
            .Register<IAdminUserRepository>(_users)
            .Register<ICompanyRepository>(_companies);

        _companies.Items.Add(NewCompany(CompanyA));
        _companies.Items.Add(NewCompany(CompanyB));
    }

    private static Company NewCompany(string id) => new()
    {
        Id = id,
        Name = id,
        IsActive = true,
        CreateDate = DateTime.UtcNow,
        DefaultIntroWaitMs = 5000,
        DefaultBreathPauseMs = 500,
        DefaultFinalQuestionWaitMs = 5000,
    };

    private AdminUser SeedUser(string id, string role, string? companyId, bool isActive = true)
    {
        var user = new AdminUser
        {
            Id = id,
            CompanyId = companyId,
            Role = role,
            Email = $"{id}@example.com",
            DisplayName = id,
            IsActive = isActive,
            MustChangePassword = false,
            CreateDate = DateTime.UtcNow,
        };
        _users.Items.Add(user);
        return user;
    }

    /// <summary>Builds the service as seen by one particular signed-in user.</summary>
    private AdminUserService ServiceAs(string role, string? companyId, string userId = "actor")
    {
        var currentUser = new CurrentUser();
        currentUser.Resolve(userId, role, companyId);

        return new AdminUserService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IAdminUserService>.Instance,
            currentUser,
            new AuthorizationGuard(currentUser),
            new PasswordHasher<AdminUser>());
    }

    private static CreateAdminUserDto NewUserDto(string role, string? companyId = null, string email = "new@example.com") => new()
    {
        Email = email,
        DisplayName = "คนใหม่",
        Role = role,
        CompanyId = companyId,
        InitialPassword = "correct-horse-battery",
    };

    // ── privilege escalation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// The headline rule. A customer's admin uses the user-management feature exactly as intended
    /// and still must not be able to mint an owner - otherwise creating one account would hand them
    /// every other customer's data.
    /// </summary>
    [Fact]
    public void CompanyAdmin_CannotCreateAnOwner()
    {
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        var ex = Assert.Throws<HttpStatusCodeException>(() => service.Create(NewUserDto(AdminRole.Owner)));

        Assert.Equal(ApiErrorCode.Forbidden, ex.Code);
        Assert.Empty(_users.Items);
    }

    /// <summary>Same escalation by the back door: promote an existing account instead of creating
    /// one.</summary>
    [Fact]
    public void CompanyAdmin_CannotPromoteAnExistingUserToOwner()
    {
        var target = SeedUser("u-cs", AdminRole.Cs, CompanyA);
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        Assert.Throws<HttpStatusCodeException>(() => service.Update(target.Id, new UpdateAdminUserDto
        {
            DisplayName = target.DisplayName,
            Role = AdminRole.Owner,
            IsActive = true,
        }));

        Assert.Equal(AdminRole.Cs, target.Role);
    }

    /// <summary>And the same escalation aimed at themselves, which is the version someone would
    /// actually try.</summary>
    [Fact]
    public void CompanyAdmin_CannotPromoteThemselvesToOwner()
    {
        var self = SeedUser("u-admin", AdminRole.Admin, CompanyA);
        SeedUser("u-admin-2", AdminRole.Admin, CompanyA); // so the last-admin rule isn't what refuses
        var service = ServiceAs(AdminRole.Admin, CompanyA, userId: self.Id);

        Assert.Throws<HttpStatusCodeException>(() => service.Update(self.Id, new UpdateAdminUserDto
        {
            DisplayName = self.DisplayName,
            Role = AdminRole.Owner,
            IsActive = true,
        }));

        Assert.Equal(AdminRole.Admin, self.Role);
    }

    // ── crossing companies ────────────────────────────────────────────────────────────────────

    /// <summary>A posted companyId must never win over the caller's own - otherwise a crafted body
    /// plants a user inside another customer.</summary>
    [Fact]
    public void CompanyAdmin_CreatingWithAnotherCompanyId_LandsInTheirOwnCompany()
    {
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        var created = service.Create(NewUserDto(AdminRole.Cs, companyId: CompanyB));

        Assert.Equal(CompanyA, created.CompanyId);
    }

    [Fact]
    public void CompanyAdmin_CannotEditAUserOfAnotherCompany()
    {
        var target = SeedUser("u-other", AdminRole.Cs, CompanyB);
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        var ex = Assert.Throws<HttpStatusCodeException>(() => service.Update(target.Id, new UpdateAdminUserDto
        {
            DisplayName = "แก้ไข",
            Role = AdminRole.Cs,
            IsActive = false,
        }));

        Assert.Equal(ApiErrorCode.Forbidden, ex.Code);
        Assert.True(target.IsActive);
    }

    [Fact]
    public void CompanyAdmin_CannotListAnotherCompanysUsers()
    {
        Assert.Throws<HttpStatusCodeException>(() => ServiceAs(AdminRole.Admin, CompanyA).GetByCompany(CompanyB));
    }

    [Fact]
    public void Cs_CannotCreateUsersAtAll()
    {
        Assert.Throws<HttpStatusCodeException>(() => ServiceAs(AdminRole.Cs, CompanyA).Create(NewUserDto(AdminRole.Cs)));
    }

    // ── lockout protection ────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeactivatingTheLastAdminOfACompany_IsRefused()
    {
        var onlyAdmin = SeedUser("u-admin", AdminRole.Admin, CompanyA);
        var service = ServiceAs(AdminRole.Owner, null);

        var ex = Assert.Throws<HttpStatusCodeException>(() => service.Update(onlyAdmin.Id, new UpdateAdminUserDto
        {
            DisplayName = onlyAdmin.DisplayName,
            Role = AdminRole.Admin,
            IsActive = false,
        }));

        Assert.Equal(ApiErrorCode.ValidationError, ex.Code);
        Assert.True(onlyAdmin.IsActive);
    }

    /// <summary>Demotion removes an admin just as effectively as deactivation, so it is refused
    /// the same way.</summary>
    [Fact]
    public void DemotingTheLastAdminOfACompany_IsRefused()
    {
        var onlyAdmin = SeedUser("u-admin", AdminRole.Admin, CompanyA);
        var service = ServiceAs(AdminRole.Owner, null);

        Assert.Throws<HttpStatusCodeException>(() => service.Update(onlyAdmin.Id, new UpdateAdminUserDto
        {
            DisplayName = onlyAdmin.DisplayName,
            Role = AdminRole.Cs,
            IsActive = true,
        }));

        Assert.Equal(AdminRole.Admin, onlyAdmin.Role);
    }

    [Fact]
    public void DeactivatingAnAdminWhenAnotherRemains_IsAllowed()
    {
        var first = SeedUser("u-admin-1", AdminRole.Admin, CompanyA);
        SeedUser("u-admin-2", AdminRole.Admin, CompanyA);
        var service = ServiceAs(AdminRole.Owner, null);

        var updated = service.Update(first.Id, new UpdateAdminUserDto
        {
            DisplayName = first.DisplayName,
            Role = AdminRole.Admin,
            IsActive = false,
        });

        Assert.False(updated.IsActive);
    }

    /// <summary>The same protection one level up - losing the last owner means nobody can manage
    /// companies or system settings again without hand-editing the database.</summary>
    [Fact]
    public void DemotingTheLastOwner_IsRefused()
    {
        var onlyOwner = SeedUser("u-owner", AdminRole.Owner, null);
        var service = ServiceAs(AdminRole.Owner, null, userId: onlyOwner.Id);

        Assert.Throws<HttpStatusCodeException>(() => service.Update(onlyOwner.Id, new UpdateAdminUserDto
        {
            DisplayName = onlyOwner.DisplayName,
            Role = AdminRole.Cs,
            IsActive = true,
        }));

        Assert.Equal(AdminRole.Owner, onlyOwner.Role);
    }

    // ── ordinary behaviour ────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompanyAdmin_CanCreateCsInTheirOwnCompany()
    {
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        var created = service.Create(NewUserDto(AdminRole.Cs));

        Assert.Equal(CompanyA, created.CompanyId);
        Assert.Equal(AdminRole.Cs, created.Role);
        Assert.True(created.IsActive);
    }

    /// <summary>Whoever created the account knows the password they typed into the form, so it
    /// cannot be allowed to remain in use.</summary>
    [Fact]
    public void CreatedUser_MustChangePasswordOnFirstSignIn()
    {
        ServiceAs(AdminRole.Admin, CompanyA).Create(NewUserDto(AdminRole.Cs));

        Assert.True(_users.Items.Single().MustChangePassword);
    }

    [Fact]
    public void CreatedUser_HasHashedPasswordNotPlaintext()
    {
        ServiceAs(AdminRole.Admin, CompanyA).Create(NewUserDto(AdminRole.Cs));

        var stored = _users.Items.Single().PasswordHash;
        Assert.NotNull(stored);
        Assert.NotEqual("correct-horse-battery", stored);
    }

    [Fact]
    public void CreatedUser_RecordsWhoCreatedIt()
    {
        ServiceAs(AdminRole.Admin, CompanyA, userId: "actor-1").Create(NewUserDto(AdminRole.Cs));

        Assert.Equal("actor-1", _users.Items.Single().CreateBy);
    }

    [Fact]
    public void DuplicateEmail_IsRejected()
    {
        SeedUser("u-existing", AdminRole.Cs, CompanyA);
        var service = ServiceAs(AdminRole.Admin, CompanyA);

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => service.Create(NewUserDto(AdminRole.Cs, email: "u-existing@example.com")));

        Assert.Equal(ApiErrorCode.ValidationError, ex.Code);
    }

    /// <summary>Email uniqueness is global, not per company - sign-in supplies only an address, so
    /// the same one under two companies would make the account ambiguous.</summary>
    [Fact]
    public void DuplicateEmail_IsRejectedEvenAcrossCompanies()
    {
        SeedUser("u-existing", AdminRole.Cs, CompanyB);
        var service = ServiceAs(AdminRole.Owner, null);

        Assert.Throws<HttpStatusCodeException>(
            () => service.Create(NewUserDto(AdminRole.Cs, companyId: CompanyA, email: "u-existing@example.com")));
    }

    [Fact]
    public void CreatingInAnUnknownCompany_IsNotFound()
    {
        var service = ServiceAs(AdminRole.Owner, null);

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => service.Create(NewUserDto(AdminRole.Cs, companyId: "company-nope")));

        Assert.Equal(ApiErrorCode.NotFound, ex.Code);
    }

    [Fact]
    public void Owner_CanCreateUsersInAnyCompany()
    {
        var service = ServiceAs(AdminRole.Owner, null);

        var created = service.Create(NewUserDto(AdminRole.Admin, companyId: CompanyB));

        Assert.Equal(CompanyB, created.CompanyId);
    }

    [Fact]
    public void GetByCompany_ReturnsOnlyThatCompany()
    {
        SeedUser("u-a", AdminRole.Cs, CompanyA);
        SeedUser("u-b", AdminRole.Cs, CompanyB);

        var listed = ServiceAs(AdminRole.Owner, null).GetByCompany(CompanyA);

        Assert.Equal("u-a", Assert.Single(listed).Id);
    }
}
