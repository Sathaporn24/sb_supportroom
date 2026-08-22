using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class CompanyServiceTests
{
    private const string OwnerId = "user-owner";

    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeAdminUserRepository _users = new();
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeKnowledgeQnARepository _qnas = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RecordingAdminUserService _adminUserService = new();
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();
    private readonly CurrentUser _currentUser = new();
    private readonly CompanyService _service;

    public CompanyServiceTests()
    {
        _currentUser.Resolve(OwnerId, AdminRole.Owner, companyId: null);
        _unitOfWork
            .Register<ICompanyRepository>(_companies)
            .Register<IAdminUserRepository>(_users)
            .Register<IKnowledgeCategoryRepository>(_categories)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<IDocumentResourceRepository>(_documents)
            .Register<IKnowledgeQnARepository>(_qnas);

        var serviceProvider = new FakeServiceProvider()
            .Register<ICurrentUser>(_currentUser)
            .Register<IAdminUserService>(_adminUserService);
        var categoryService = new KnowledgeCategoryService(
            _unitOfWork,
            serviceProvider,
            NullLogger<IKnowledgeCategoryService>.Instance);
        _service = new CompanyService(
            _unitOfWork,
            serviceProvider,
            NullLogger<ICompanyService>.Instance,
            _currentUser,
            new AuthorizationGuard(_currentUser),
            _passwordHasher,
            categoryService);
    }

    [Fact]
    public void Create_StagesAllEntities_CommitsOnce_AndNeverCallsAdminUserServiceCreate()
    {
        var result = _service.Create(CreateInput());

        Assert.Equal("scb", result.Id);
        Assert.Equal(1, _unitOfWork.CommitCount);
        Assert.Equal(0, _adminUserService.CreateCallCount);
        Assert.Single(_companies.Items);
        Assert.Single(_users.Items);
        Assert.Equal(2, _categories.Items.Count);

        var admin = _users.Items.Single();
        Assert.Equal("scb", admin.CompanyId);
        Assert.Equal(AdminRole.Admin, admin.Role);
        Assert.True(admin.MustChangePassword);
        Assert.Equal(OwnerId, admin.CreateBy);
        Assert.NotEqual("temporary-password", admin.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash!, "temporary-password"));
    }

    /// <summary>LP-14.2 - both entry points that create a Company row must set pacing from
    /// ServerDefaults, never leave it at a language default (0) and never read it from the
    /// request (CreateCompanyDto has no pacing field at all).</summary>
    [Fact]
    public void Create_SetsCompanyPacingFromServerDefaults()
    {
        var result = _service.Create(CreateInput());

        var company = _companies.Items.Single(x => x.Id == result.Id);
        Assert.Equal(5000, company.DefaultIntroWaitMs);
        Assert.Equal(500, company.DefaultBreathPauseMs);
        Assert.Equal(5000, company.DefaultFinalQuestionWaitMs);
    }

    /// <summary>LP-14.2 - the other entry point (SeedFirstCompanyIfEmpty), same requirement.</summary>
    [Fact]
    public void SeedFirstCompanyIfEmpty_SetsCompanyPacingFromServerDefaults()
    {
        _service.SeedFirstCompanyIfEmpty();

        var company = _companies.Items.Single();
        Assert.Equal(5000, company.DefaultIntroWaitMs);
        Assert.Equal(500, company.DefaultBreathPauseMs);
        Assert.Equal(5000, company.DefaultFinalQuestionWaitMs);
        Assert.Equal(1, _unitOfWork.CommitCount);
    }

    [Fact]
    public void Create_WithAnActiveDuplicateSlug_ReturnsTheActiveDuplicateMessage()
    {
        var existing = SeedCompany("scb", isActive: true);

        var error = Assert.Throws<HttpStatusCodeException>(() => _service.Create(CreateInput()));

        Assert.Equal("รหัสบริษัทนี้ถูกใช้งานแล้ว", error.Message);
        Assert.True(existing.IsActive);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    [Fact]
    public void Create_WithAnInactiveDuplicateSlug_DirectsTheOwnerToReactivateIt()
    {
        var existing = SeedCompany("scb", isActive: false);

        var error = Assert.Throws<HttpStatusCodeException>(() => _service.Create(CreateInput()));

        Assert.Equal(
            "มีบริษัทรหัสนี้อยู่แล้วแต่ถูกปิดใช้งาน หากต้องการใช้งานอีกครั้ง ให้เปิดกลับจากหน้ารายการบริษัท ไม่ใช่สร้างใหม่",
            error.Message);
        Assert.False(existing.IsActive);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    [Fact]
    public void Create_WithDuplicateEmail_ReturnsTheFixedNonEnumeratingMessage()
    {
        _users.Items.Add(new AdminUser
        {
            Id = "user-existing",
            CompanyId = "another-company",
            Role = AdminRole.Owner,
            Email = "admin@example.com",
            DisplayName = "Existing User",
            IsActive = true,
            MustChangePassword = false,
        });

        var error = Assert.Throws<HttpStatusCodeException>(() => _service.Create(CreateInput()));

        Assert.Equal("อีเมลนี้ถูกใช้งานแล้ว", error.Message);
        Assert.DoesNotContain("another-company", error.Message);
        Assert.DoesNotContain(AdminRole.Owner, error.Message);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    [Fact]
    public void Create_IgnoresAnUnknownRolePayloadAndAlwaysCreatesAnAdmin()
    {
        const string json = """
            {
              "id": "scb",
              "name": "SCB",
              "adminEmail": "admin@example.com",
              "adminDisplayName": "First Admin",
              "adminInitialPassword": "temporary-password",
              "role": "owner"
            }
            """;
        var input = JsonSerializer.Deserialize<CreateCompanyDto>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        _service.Create(Assert.IsType<CreateCompanyDto>(input));

        Assert.Null(typeof(CreateCompanyDto).GetProperty("Role"));
        Assert.Equal(AdminRole.Admin, _users.Items.Single().Role);
    }

    [Fact]
    public void GetAllIncludingInactive_IsOwnerOnlyAndReturnsNameOrderedRows()
    {
        SeedCompany("zeta", isActive: false, name: "Zeta");
        SeedCompany("alpha", isActive: true, name: "Alpha");

        var result = _service.GetAllIncludingInactive();

        Assert.Equal(["alpha", "zeta"], result.Select(x => x.Id).ToArray());
        Assert.Contains(result, x => !x.IsActive);
    }

    [Fact]
    public void Create_AsNonOwner_FailsBeforeReadingOrWritingProvisioningData()
    {
        var companyAdmin = new CurrentUser();
        companyAdmin.Resolve("user-admin", AdminRole.Admin, TestFixtures.CompanyId);
        var serviceProvider = new FakeServiceProvider().Register<ICurrentUser>(companyAdmin);
        var categoryService = new KnowledgeCategoryService(
            _unitOfWork,
            serviceProvider,
            NullLogger<IKnowledgeCategoryService>.Instance);
        var service = new CompanyService(
            _unitOfWork,
            serviceProvider,
            NullLogger<ICompanyService>.Instance,
            companyAdmin,
            new AuthorizationGuard(companyAdmin),
            _passwordHasher,
            categoryService);

        var error = Assert.Throws<HttpStatusCodeException>(() => service.Create(CreateInput()));

        Assert.Equal(403, (int)error.StatusCode);
        Assert.Empty(_companies.Items);
        Assert.Empty(_users.Items);
        Assert.Empty(_categories.Items);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    /// <summary>LP-14.3 - cs may read the pacing section (SP-4/SP-15's visibleToRoles includes
    /// cs) but must be rejected on write.</summary>
    [Fact]
    public void UpdateLessonPacing_AsCs_IsForbidden()
    {
        var company = SeedCompany(TestFixtures.CompanyId, isActive: true);
        var service = CreateServiceAs(AdminRole.Cs, TestFixtures.CompanyId);

        var error = Assert.Throws<HttpStatusCodeException>(
            () => service.UpdateLessonPacing(TestFixtures.CompanyId, ValidPacingInput()));

        Assert.Equal(403, (int)error.StatusCode);
        Assert.Equal(5000, company.DefaultIntroWaitMs);
    }

    [Fact]
    public void GetLessonPacing_AsCs_IsAllowed()
    {
        SeedCompany(TestFixtures.CompanyId, isActive: true);
        var service = CreateServiceAs(AdminRole.Cs, TestFixtures.CompanyId);

        var result = service.GetLessonPacing(TestFixtures.CompanyId);

        Assert.Equal(5000, result.IntroWaitMs);
        Assert.Equal(500, result.BreathPauseMs);
        Assert.Equal(5000, result.FinalQuestionWaitMs);
    }

    [Fact]
    public void UpdateLessonPacing_AsAdminOfOwnCompany_Succeeds()
    {
        var company = SeedCompany(TestFixtures.CompanyId, isActive: true);
        var service = CreateServiceAs(AdminRole.Admin, TestFixtures.CompanyId);

        var result = service.UpdateLessonPacing(TestFixtures.CompanyId, ValidPacingInput());

        Assert.Equal(1000, result.IntroWaitMs);
        Assert.Equal(200, result.BreathPauseMs);
        Assert.Equal(9000, result.FinalQuestionWaitMs);
        Assert.Equal(1000, company.DefaultIntroWaitMs);
    }

    [Fact]
    public void UpdateLessonPacing_ForAnotherCompany_IsForbiddenForAdmin()
    {
        SeedCompany(TestFixtures.OtherCompanyId, isActive: true);
        var service = CreateServiceAs(AdminRole.Admin, TestFixtures.CompanyId);

        var error = Assert.Throws<HttpStatusCodeException>(
            () => service.UpdateLessonPacing(TestFixtures.OtherCompanyId, ValidPacingInput()));

        Assert.Equal(403, (int)error.StatusCode);
    }

    [Fact]
    public void GetLessonPacing_ForUnknownCompany_ReturnsNotFound()
    {
        var service = CreateServiceAs(AdminRole.Owner, companyId: null);

        var error = Assert.Throws<HttpStatusCodeException>(
            () => service.GetLessonPacing("no-such-company"));

        Assert.Equal(404, (int)error.StatusCode);
    }

    private CompanyService CreateServiceAs(string role, string? companyId)
    {
        var user = new CurrentUser();
        user.Resolve("user-" + role, role, companyId);
        var serviceProvider = new FakeServiceProvider().Register<ICurrentUser>(user);
        var categoryService = new KnowledgeCategoryService(
            _unitOfWork,
            serviceProvider,
            NullLogger<IKnowledgeCategoryService>.Instance);
        return new CompanyService(
            _unitOfWork,
            serviceProvider,
            NullLogger<ICompanyService>.Instance,
            user,
            new AuthorizationGuard(user),
            _passwordHasher,
            categoryService);
    }

    private static UpdateCompanyLessonPacingDto ValidPacingInput() => new()
    {
        IntroWaitMs = 1000,
        BreathPauseMs = 200,
        FinalQuestionWaitMs = 9000,
    };

    /// <summary>LP-14.3/LP-8 - out-of-range values on the company-level DTO must be rejected by
    /// data annotations (the same mechanism ASP.NET Core's model binder runs before the
    /// controller action is even invoked). Company is the last layer of the resolve chain
    /// (LP-1), so there is no "unset" state a null could legitimately represent here.</summary>
    [Theory]
    [InlineData(60_001, 200, 9000)]
    [InlineData(1000, 10_001, 9000)]
    [InlineData(1000, 200, 120_001)]
    [InlineData(-1, 200, 9000)]
    public void UpdateCompanyLessonPacingDto_RejectsOutOfRangeValues(int introWaitMs, int breathPauseMs, int finalQuestionWaitMs)
    {
        var dto = new UpdateCompanyLessonPacingDto
        {
            IntroWaitMs = introWaitMs,
            BreathPauseMs = breathPauseMs,
            FinalQuestionWaitMs = finalQuestionWaitMs,
        };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            dto, new System.ComponentModel.DataAnnotations.ValidationContext(dto), results, validateAllProperties: true);

        Assert.False(isValid);
    }

    private Company SeedCompany(string id, bool isActive, string name = "Existing Company")
    {
        var company = new Company
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            DefaultIntroWaitMs = 5000,
            DefaultBreathPauseMs = 500,
            DefaultFinalQuestionWaitMs = 5000,
        };
        _companies.Items.Add(company);
        return company;
    }

    private static CreateCompanyDto CreateInput() => new()
    {
        Id = " SCB ",
        Name = " SCB ",
        AdminEmail = " admin@example.com ",
        AdminDisplayName = " First Admin ",
        AdminInitialPassword = "temporary-password",
    };

    private sealed class RecordingAdminUserService : IAdminUserService
    {
        public int CreateCallCount { get; private set; }

        public IReadOnlyList<AdminUserViewModel> GetByCompany(string companyId)
            => throw new NotSupportedException();

        public AdminUserViewModel Create(CreateAdminUserDto input)
        {
            CreateCallCount++;
            throw new InvalidOperationException("CompanyService must not call IAdminUserService.Create.");
        }

        public AdminUserViewModel Update(string id, UpdateAdminUserDto input)
            => throw new NotSupportedException();
    }
}
