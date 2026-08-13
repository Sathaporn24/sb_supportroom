using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>
/// The guarantee this whole change exists for: one company can never read another's rows.
///
/// These run against a real ApplicationDbContext (EF Core InMemory provider) rather than the
/// hand-rolled list fakes used elsewhere in this project. That is the entire point - the fakes
/// are plain List&lt;T&gt; lookups with no notion of a query filter, so isolation "passing" against
/// them would prove nothing at all. Only a real DbContext actually executes the HasQueryFilter
/// calls in OnModelCreating.
///
/// InMemory is not a real database and does not prove the generated SQL is right; it proves the
/// filters are configured and applied on every query, which is the part that would silently rot
/// if someone adds an entity later and forgets one.
/// </summary>
public class CompanyIsolationTests : IDisposable
{
    private const string CompanyA = "company-a";
    private const string CompanyB = "company-b";

    private readonly CompanyContext _companyContext = new();
    private readonly ApplicationDbContext _db;

    public CompanyIsolationTests()
    {
        MapsterConfig.Apply();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            // Unique name per test instance so xUnit's parallel runs don't share state.
            .UseInMemoryDatabase($"isolation-{Guid.NewGuid()}")
            // The filters compare CompanyId against a context that is deliberately null until
            // resolved, and InMemory warns about that being evaluated client-side. That null IS
            // the fail-closed behaviour under test, so the warning is expected here.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(options, _companyContext);
    }

    public void Dispose() => _db.Dispose();

    private static LessonConfig Lesson(string companyId, string slug) => new()
    {
        Id = $"lesson-{companyId}-{slug}",
        CompanyId = companyId,
        Slug = slug,
        Title = $"บทเรียนของ {companyId}",
        SlidesSourceUrl = "",
        ContentSourceType = "google_slides",
        IntroWaitMs = 5000,
        BreathPauseMs = 500,
        FinalQuestionWaitMs = 5000,
        SlideConfigs = [],
        IsActive = true,
        CreateDate = DateTime.UtcNow,
    };

    private static TrainingLink Link(string companyId, string token) => new()
    {
        Id = $"link-{token}",
        CompanyId = companyId,
        Token = token,
        LessonId = $"lesson-{companyId}-shared-slug",
        LessonSlug = "shared-slug",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        CreateDate = DateTime.UtcNow,
    };

    private static LearningSession Learning(string companyId, string token) => new()
    {
        Id = $"learning-{token}",
        CompanyId = companyId,
        TrainingLinkId = $"link-{token}",
        LearnerKey = $"key-{token}",
        RecipientName = "ผู้เรียน",
        Status = SessionStatus.InProgress,
        StartedAt = DateTime.UtcNow,
        LastActivityAt = DateTime.UtcNow,
        CreateDate = DateTime.UtcNow,
    };

    /// <summary>Seeding writes rows for both companies. Inserts are not filtered - only reads
    /// are - so one context can set up both sides.</summary>
    private void SeedBothCompanies()
    {
        _db.LessonConfig.AddRange(Lesson(CompanyA, "shared-slug"), Lesson(CompanyB, "shared-slug"));
        _db.TrainingLink.AddRange(Link(CompanyA, "token-a"), Link(CompanyB, "token-b"));
        _db.LearningSession.AddRange(Learning(CompanyA, "token-a"), Learning(CompanyB, "token-b"));
        _db.ChatMessage.AddRange(
            new ChatMessage { Id = "chat-a", CompanyId = CompanyA, SessionId = "learning-token-a", SenderRole = ChatSenderRole.Agent, Text = "ของบริษัท A", CreateDate = DateTime.UtcNow },
            new ChatMessage { Id = "chat-b", CompanyId = CompanyB, SessionId = "learning-token-b", SenderRole = ChatSenderRole.Agent, Text = "ของบริษัท B", CreateDate = DateTime.UtcNow });
        _db.SaveChanges();
    }

    [Fact]
    public void AQueryOnlyEverSeesItsOwnCompanysRows()
    {
        SeedBothCompanies();
        _companyContext.Resolve(CompanyA);

        Assert.Equal([CompanyA], _db.LessonConfig.Select(x => x.CompanyId).Distinct().ToList());
        Assert.Equal([CompanyA], _db.TrainingLink.Select(x => x.CompanyId).Distinct().ToList());
        Assert.Equal([CompanyA], _db.LearningSession.Select(x => x.CompanyId).Distinct().ToList());
        Assert.Equal([CompanyA], _db.ChatMessage.Select(x => x.CompanyId).Distinct().ToList());
    }

    [Fact]
    public void FetchingAnotherCompanysRowByItsExactIdStillReturnsNothing()
    {
        // The dangerous shape: the caller already knows a real id (leaked, guessed, or copied
        // from an admin screen) and asks for it directly. Filtering only list endpoints would
        // leave this wide open.
        SeedBothCompanies();
        _companyContext.Resolve(CompanyA);

        Assert.Null(_db.LessonConfig.FirstOrDefault(x => x.Id == $"lesson-{CompanyB}-shared-slug"));
        Assert.Null(_db.ChatMessage.FirstOrDefault(x => x.Id == "chat-b"));
        Assert.Null(_db.TrainingLink.FirstOrDefault(x => x.Id == "link-token-b"));
        Assert.Null(_db.LearningSession.FirstOrDefault(x => x.Id == "learning-token-b"));
    }

    [Fact]
    public void TwoCompaniesCanBothOwnALessonWithTheSameSlug()
    {
        // Slug used to be unique across the whole system, so the second company onboarded could
        // not use any of the obvious names. It is now unique per company instead.
        SeedBothCompanies();

        _companyContext.Resolve(CompanyA);
        var fromA = _db.LessonConfig.Single(x => x.Slug == "shared-slug");
        _companyContext.Resolve(CompanyB);
        var fromB = _db.LessonConfig.Single(x => x.Slug == "shared-slug");

        Assert.NotEqual(fromA.Id, fromB.Id);
        Assert.Equal(CompanyA, fromA.CompanyId);
        Assert.Equal(CompanyB, fromB.CompanyId);
    }

    [Fact]
    public void AnUnresolvedCompanySeesNothingRatherThanEverything()
    {
        // Fail-closed. If resolution is ever missed - a new endpoint, a background job, a code
        // path nobody thought about - the damage is "no data" and not "all companies' data".
        SeedBothCompanies();

        Assert.Empty(_db.LessonConfig.ToList());
        Assert.Empty(_db.TrainingLink.ToList());
        Assert.Empty(_db.LearningSession.ToList());
        Assert.Empty(_db.ChatMessage.ToList());
    }

    [Fact]
    public void LookingUpALinkByTokenCrossesTheFilterAndSwitchesTheRequestToThatCompany()
    {
        // The one deliberate hole in the filter, and the mechanism the whole recipient-side flow
        // depends on: someone holding a join link has no identity and no company yet, so the
        // token lookup has to run unfiltered - then hand the company it found to everything after.
        SeedBothCompanies();
        _companyContext.Resolve(CompanyA);

        var repository = new TrainingLinkRepository(_db);
        var found = repository.GetByToken("token-b");

        Assert.NotNull(found);
        Assert.Equal(CompanyB, found.CompanyId);

        _companyContext.Resolve(found.CompanyId);
        Assert.Equal(CompanyB, _db.LessonConfig.Single(x => x.Slug == "shared-slug").CompanyId);
    }

    [Fact]
    public void EveryEntityIsCompanyScoped()
    {
        // Guards the failure mode a per-entity test cannot: someone adds a seventh entity later,
        // forgets HasQueryFilter, and every existing test still passes while the new table is
        // readable by every company.
        // GetDeclaredQueryFilters, not the obsolete single-filter GetQueryFilter: EF Core 10
        // allows several named filters per entity, so the question is "are there any" rather
        // than "is there one".
        var unscoped = _db.Model.GetEntityTypes()
            .Where(e => typeof(ICompanyScoped).IsAssignableFrom(e.ClrType))
            .Where(e => e.GetDeclaredQueryFilters().Count == 0)
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.True(unscoped.Count == 0, $"เอนทิตีที่ยังไม่มี company query filter: {string.Join(", ", unscoped)}");
    }
}
