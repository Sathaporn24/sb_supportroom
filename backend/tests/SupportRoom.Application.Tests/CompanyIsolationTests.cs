using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        CategoryId = "kbcat-child",
        Title = $"บทเรียนของ {companyId}",
        SlidesSourceUrl = "",
        ContentSourceType = "google_slides",
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
        _db.SaveChanges();
    }

    private static DocumentResource DeletedDocument(string companyId, string id) => new()
    {
        Id = id,
        CompanyId = companyId,
        ScopeType = KnowledgeScopeType.Company,
        ScopeId = null,
        FileName = $"{id}.pdf",
        ContentType = "application/pdf",
        SizeBytes = 1024,
        ObsBucket = "documents",
        ObsKey = $"documents/{id}/{id}.pdf",
        IndexingStatus = DocumentIndexingStatus.Pending,
        IndexedChunkCount = 0,
        IsDelete = true,
        DeletedAt = DateTime.UtcNow,
        CreateDate = DateTime.UtcNow,
    };

    /// <summary>Both companies have a soft-deleted document in the same DB - the exact shape
    /// GetDeleted() used to leak across (IgnoreQueryFilters() drops CompanyId, not just
    /// IsDelete).</summary>
    private void SeedDeletedDocumentsForBothCompanies()
    {
        _db.DocumentResource.AddRange(DeletedDocument(CompanyA, "doc-a"), DeletedDocument(CompanyB, "doc-b"));
        _db.SaveChanges();
    }

    private static DocumentResource ActiveDocument(string companyId, string id) => new()
    {
        Id = id,
        CompanyId = companyId,
        ScopeType = KnowledgeScopeType.Company,
        ScopeId = null,
        FileName = $"{id}.pdf",
        ContentType = "application/pdf",
        SizeBytes = 1024,
        ObsBucket = "documents",
        ObsKey = $"documents/{id}/{id}.pdf",
        IndexingStatus = DocumentIndexingStatus.Indexed,
        IndexedChunkCount = 3,
        IsDelete = false,
        CreateDate = DateTime.UtcNow,
    };

    /// <summary>Both companies have a non-deleted document - the shape GetAllInCompany() (Module H,
    /// R7) is supposed to keep separate purely via the EF global query filter.</summary>
    private void SeedActiveDocumentsForBothCompanies()
    {
        _db.DocumentResource.AddRange(ActiveDocument(CompanyA, "doc-active-a"), ActiveDocument(CompanyB, "doc-active-b"));
        _db.SaveChanges();
    }

    private static KnowledgeQnA QnA(string companyId, string id) => new()
    {
        Id = id,
        CompanyId = companyId,
        Question = $"คำถามของ {companyId}",
        Answer = $"คำตอบของ {companyId}",
        ScopeType = KnowledgeScopeType.Company,
        ScopeId = null,
        VectorId = id,
        IndexingStatus = DocumentIndexingStatus.Indexed,
        IsDelete = false,
        CreateDate = DateTime.UtcNow,
    };

    private static LessonExcludedSlide ExcludedSlide(string companyId, string lessonId, string slideObjectId) => new()
    {
        Id = $"exsl-{companyId}-{slideObjectId}",
        CompanyId = companyId,
        LessonId = lessonId,
        SlideObjectId = slideObjectId,
        IsDelete = true,
        DeletedAt = DateTime.UtcNow,
        CreateDate = DateTime.UtcNow,
    };

    /// <summary>Both companies have a Q&amp;A entry - same shape as SeedActiveDocumentsForBothCompanies,
    /// for KnowledgeQnARepository.GetAllInCompany() (Module H, R7).</summary>
    private void SeedQnAForBothCompanies()
    {
        _db.KnowledgeQnA.AddRange(QnA(CompanyA, "qna-a"), QnA(CompanyB, "qna-b"));
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
    }

    [Fact]
    public void DefaultChainBecomesVisibleOnlyAfterResolvingTheNewCompanyContext()
    {
        var currentUser = new CurrentUser();
        currentUser.Resolve("user-owner", AdminRole.Owner, companyId: null);
        var services = new ServiceCollection()
            .AddSingleton(_db)
            .AddSingleton<ICompanyContext>(_companyContext)
            .AddSingleton<ICurrentUser>(currentUser)
            .BuildServiceProvider();
        using (services)
        {
            var unitOfWork = new UnitOfWork(_db, services);
            var service = new KnowledgeCategoryService(
                unitOfWork,
                services,
                NullLogger<IKnowledgeCategoryService>.Instance);

            service.CreateDefaultChain(CompanyB);
            unitOfWork.Commit();

            Assert.Empty(_db.KnowledgeCategory.ToList());

            _companyContext.Resolve(CompanyB);
            var categories = _db.KnowledgeCategory.OrderBy(x => x.Level).ToList();
            Assert.Equal(2, categories.Count);
            Assert.Equal(categories[0].Id, categories[1].ParentId);
            Assert.Single(categories, x => x.IsSystemDefault && x.Level == 2);
        }
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
    public void GetDeletedOnlyReturnsTheCallersCompanysSoftDeletedDocuments()
    {
        // Regression test for the leak QA found: GetDeleted() called IgnoreQueryFilters() to see
        // past the `!IsDelete` half of the filter, but that call drops the CompanyId half too, so
        // it used to return every company's deleted documents. GetDeleted(companyId) reapplies
        // CompanyId explicitly - this proves it, against a real DbContext, not a fake.
        SeedDeletedDocumentsForBothCompanies();
        _companyContext.Resolve(CompanyA);

        var repository = new DocumentResourceRepository(_db);
        var deletedForA = repository.GetDeleted(CompanyA).ToList();

        Assert.Single(deletedForA);
        Assert.Equal("doc-a", deletedForA[0].Id);
        Assert.DoesNotContain(deletedForA, d => d.CompanyId == CompanyB);
    }

    [Fact]
    public void DocumentResourceGetAllInCompanyOnlyEverSeesItsOwnCompanysDocuments()
    {
        // Module H / R7: GetAllInCompany() is FindBy(_ => true) - isolation is left entirely to
        // the EF global query filter, so this has to run against a real DbContext to mean
        // anything. Proves both directions: A never sees B's rows and B never sees A's.
        SeedActiveDocumentsForBothCompanies();
        var repository = new DocumentResourceRepository(_db);

        _companyContext.Resolve(CompanyA);
        var fromA = repository.GetAllInCompany().ToList();
        Assert.Single(fromA);
        Assert.Equal("doc-active-a", fromA[0].Id);
        Assert.DoesNotContain(fromA, d => d.CompanyId == CompanyB);

        _companyContext.Resolve(CompanyB);
        var fromB = repository.GetAllInCompany().ToList();
        Assert.Single(fromB);
        Assert.Equal("doc-active-b", fromB[0].Id);
        Assert.DoesNotContain(fromB, d => d.CompanyId == CompanyA);
    }

    [Fact]
    public void KnowledgeQnAGetAllInCompanyOnlyEverSeesItsOwnCompanysQnA()
    {
        // Same guarantee as DocumentResourceGetAllInCompanyOnlyEverSeesItsOwnCompanysDocuments,
        // for KnowledgeQnARepository.GetAllInCompany() (Module H / R7, design.md R-16).
        SeedQnAForBothCompanies();
        var repository = new KnowledgeQnARepository(_db);

        _companyContext.Resolve(CompanyA);
        var fromA = repository.GetAllInCompany().ToList();
        Assert.Single(fromA);
        Assert.Equal("qna-a", fromA[0].Id);
        Assert.DoesNotContain(fromA, q => q.CompanyId == CompanyB);

        _companyContext.Resolve(CompanyB);
        var fromB = repository.GetAllInCompany().ToList();
        Assert.Single(fromB);
        Assert.Equal("qna-b", fromB[0].Id);
        Assert.DoesNotContain(fromB, q => q.CompanyId == CompanyA);
    }

    [Fact]
    public void LessonExcludedSlideRepository_GetByLessonIdKeepsTheCompanyPredicateWhenItBypassesTheSoftDeleteFilter()
    {
        // Module K / DM-17: IgnoreQueryFilters() is needed to find a previously restored page,
        // but it removes the CompanyId predicate too. Both companies deliberately use the same
        // lesson id here to prove the repository itself, rather than its callers, restores it.
        _db.LessonExcludedSlide.AddRange(
            ExcludedSlide(CompanyA, "lesson-shared", "pdf-page-1"),
            ExcludedSlide(CompanyB, "lesson-shared", "pdf-page-1"));
        _db.SaveChanges();
        _companyContext.Resolve(CompanyA);

        var repository = new LessonExcludedSlideRepository(_db, _companyContext);
        var rows = repository.GetByLessonId("lesson-shared").ToList();

        var row = Assert.Single(rows);
        Assert.Equal(CompanyA, row.CompanyId);
    }

    // ---- R9/LT-23 - Module L's IgnoreQueryFilters() repository methods, against a real
    // DbContext (the fakes used elsewhere in this project do not execute HasQueryFilter at all,
    // so only these prove the CompanyId predicate is actually re-applied). ---------------------

    private static LessonConfig TrashedLesson(string companyId, string id, string? purgeJobId = null) => new()
    {
        Id = id,
        CompanyId = companyId,
        Slug = $"{id}-slug",
        CategoryId = "kbcat-child",
        Title = $"บทเรียนของ {companyId}",
        SlidesSourceUrl = "",
        ContentSourceType = "google_slides",
        SlideConfigs = [],
        IsActive = true,
        IsDelete = true,
        DeletedAt = DateTime.UtcNow,
        PurgeJobId = purgeJobId,
        CreateDate = DateTime.UtcNow,
    };

    [Fact]
    public void LessonConfigRepository_GetTrash_OnlyReturnsTheCallersCompanysTrashedLessons()
    {
        _db.LessonConfig.AddRange(TrashedLesson(CompanyA, "trash-a"), TrashedLesson(CompanyB, "trash-b"));
        _db.SaveChanges();
        _companyContext.Resolve(CompanyA);

        var repository = new LessonConfigRepository(_db);
        var result = repository.GetTrash(CompanyA).ToList();

        var row = Assert.Single(result);
        Assert.Equal("trash-a", row.Id);
    }

    [Fact]
    public void LessonConfigRepository_GetIncludingDeleted_CompanyACannotReadCompanyBsTrashedLessonById()
    {
        _db.LessonConfig.Add(TrashedLesson(CompanyB, "trash-b"));
        _db.SaveChanges();
        _companyContext.Resolve(CompanyA);

        var repository = new LessonConfigRepository(_db);

        Assert.Null(repository.GetIncludingDeleted(CompanyA, "trash-b"));
        Assert.NotNull(repository.GetIncludingDeleted(CompanyB, "trash-b"));
    }

    // P12-08 - TryClaimPurge/TryArchive/TryRestore*, CancelPendingLessonPurge and
    // AccelerateLessonPurge are all raw ExecuteSqlRaw/ExecuteUpdate against Postgres-specific SQL
    // (see their own doc comments) - EF Core InMemory throws InvalidOperationException on any of
    // them ("Relational-specific methods can only be used when the context is using a relational
    // database provider"). This project has no Postgres-backed test harness (see
    // IBackgroundJobRepository.ClaimNext/RequeueOrphanedRunning, which document the identical gap
    // and are excluded from FakeBackgroundJobRepository for the same reason), so their CompanyId
    // guard is provable only by code inspection today: every one of them puts `CompanyId = {n}` in
    // the same WHERE clause as the id/jobId match. GetTrash/GetIncludingDeleted below are the
    // Module L methods this harness CAN actually execute, since they compile to plain LINQ.

    [Fact]
    public void EveryEntityIsCompanyScoped()
    {
        // Guards the failure mode a per-entity test cannot: someone adds a seventh entity later,
        // forgets HasQueryFilter, and every existing test still passes while the new table is
        // readable by every company.
        // GetDeclaredQueryFilters, not the obsolete single-filter GetQueryFilter: EF Core 10
        // allows several named filters per entity, so the question is "are there any" rather
        // than "is there one".
        // BackgroundJob is the one documented exception (design.md DM-15/DI-4/DI-12): a worker
        // claims the next ready job across every company before it knows which company that job
        // belongs to, then resolves ICompanyContext FROM the row it just claimed - a filter here
        // would make every claim query match zero rows and no job would ever run. Every other
        // read of this table happens from a request scope that already knows CompanyId and must
        // filter for itself (see ApplicationDbContext's note on the entity).
        var exemptFromCompanyFilter = new HashSet<string> { nameof(BackgroundJob) };

        var unscoped = _db.Model.GetEntityTypes()
            .Where(e => typeof(ICompanyScoped).IsAssignableFrom(e.ClrType))
            .Where(e => !exemptFromCompanyFilter.Contains(e.ClrType.Name))
            .Where(e => e.GetDeclaredQueryFilters().Count == 0)
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.True(unscoped.Count == 0, $"เอนทิตีที่ยังไม่มี company query filter: {string.Join(", ", unscoped)}");
    }
}
