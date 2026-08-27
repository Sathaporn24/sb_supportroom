using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Tests;

/// <summary>
/// R9/Module L - LT-1..LT-4/LT-10 archive/restore/permanent-delete state machine, LT-2's role
/// matrix, and LT-23's tenant isolation on the new trash-specific repository methods.
/// </summary>
public class LessonTrashServiceTests
{
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public LessonTrashServiceTests()
    {
        MapsterConfig.Apply();
        _lessons.TrainingLinks = _links;
        _lessons.BackgroundJobs = _jobs;
        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<IDocumentResourceRepository>(new FakeDocumentResourceRepository());
        _unitOfWork.Register<IKnowledgeCategoryRepository>(new FakeKnowledgeCategoryRepository());
        _unitOfWork.Register<ILessonSlideNarrationRepository>(new FakeLessonSlideNarrationRepository());
        _unitOfWork.Register<ILessonExcludedSlideRepository>(new FakeLessonExcludedSlideRepository());
        _unitOfWork.Register<ICompanyRepository>(new FakeCompanyRepository());
        _unitOfWork.Register<ITrainingLinkRepository>(_links);
        _unitOfWork.Register<ILearningSessionRepository>(new FakeLearningSessionRepository());
        _unitOfWork.Register<IBackgroundJobRepository>(_jobs);
    }

    private LessonConfigService BuildService(string role, string companyId, string userId = "user-test")
    {
        var (guard, currentUser) = TestFixtures.AdminContext(role, companyId, userId);
        var serviceProvider = new FakeServiceProvider(companyId);
        return new LessonConfigService(
            _unitOfWork,
            serviceProvider,
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            new FakeKnowledgeIndexingService(),
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            new MemoryCache(new MemoryCacheOptions()),
            new LessonSlideNarrationResolver(_unitOfWork),
            guard,
            currentUser);
    }

    private LessonConfig SeedActiveLesson(string id = "lesson-1", string companyId = null!)
    {
        companyId ??= TestFixtures.CompanyId;
        var lesson = new LessonConfig
        {
            Id = id,
            CompanyId = companyId,
            Slug = $"{id}-slug",
            CategoryId = "kbcat-child",
            Title = "บทเรียนทดสอบ",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
            CreateDate = DateTime.UtcNow,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    private TrainingLink SeedLink(string lessonId, string companyId, string id = "link-1")
    {
        var link = new TrainingLink
        {
            Id = id,
            CompanyId = companyId,
            Token = $"tok-{id}",
            LessonId = lessonId,
            LessonSlug = $"{lessonId}-slug",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _links.Items.Add(link);
        return link;
    }

    // ---- LT-2 - role matrix -------------------------------------------------------------------

    [Fact]
    public async Task ArchiveAsync_Cs_IsForbidden()
    {
        SeedActiveLesson();
        var service = BuildService(AdminRole.Cs, TestFixtures.CompanyId);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => service.ArchiveAsync("lesson-1"));
        Assert.Equal(403, (int)ex.StatusCode);
    }

    [Fact]
    public async Task RestoreAsync_Cs_IsForbidden()
    {
        var lesson = SeedActiveLesson();
        var ownerService = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await ownerService.ArchiveAsync(lesson.Id);

        var csService = BuildService(AdminRole.Cs, TestFixtures.CompanyId);
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => csService.RestoreAsync(lesson.Id));
        Assert.Equal(403, (int)ex.StatusCode);
    }

    [Fact]
    public void GetTrash_Cs_IsForbidden()
    {
        // P12-03 regression - GET /api/lessons/trash must enforce the same LT-2 role guard as
        // every other Module L endpoint; there is no reason for the read path alone to skip it.
        var service = BuildService(AdminRole.Cs, TestFixtures.CompanyId);

        var ex = Assert.Throws<HttpStatusCodeException>(() => service.GetTrash());
        Assert.Equal(403, (int)ex.StatusCode);
    }

    [Fact]
    public async Task ArchiveAsync_Admin_Succeeds()
    {
        SeedActiveLesson();
        var service = BuildService(AdminRole.Admin, TestFixtures.CompanyId);

        var result = await service.ArchiveAsync("lesson-1");

        Assert.NotNull(result);
        var entity = _lessons.Items.Single(l => l.Id == "lesson-1");
        Assert.True(entity.IsDelete);
        Assert.NotNull(entity.PurgeJobId);
        Assert.Null(entity.PurgeStartedAt);
    }

    [Fact]
    public async Task RequestPermanentDeleteAsync_Admin_IsForbidden_OwnerOnly()
    {
        var lesson = SeedActiveLesson();
        var ownerService = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await ownerService.ArchiveAsync(lesson.Id);

        var adminService = BuildService(AdminRole.Admin, TestFixtures.CompanyId);
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => adminService.RequestPermanentDeleteAsync(lesson.Id, lesson.Title));
        Assert.Equal(403, (int)ex.StatusCode);
    }

    // ---- LT-3/LT-1 - archive is one transaction, idempotent --------------------------------

    [Fact]
    public async Task ArchiveAsync_RevokesEveryTrainingLink()
    {
        var lesson = SeedActiveLesson();
        SeedLink(lesson.Id, lesson.CompanyId, "link-1");
        SeedLink(lesson.Id, lesson.CompanyId, "link-2");
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);

        await service.ArchiveAsync(lesson.Id);

        Assert.All(_links.Items, l => Assert.True(l.IsDelete));
    }

    [Fact]
    public async Task ArchiveAsync_CalledTwice_SecondCallIsNotFound_NoSecondJob()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => service.ArchiveAsync(lesson.Id));

        Assert.Equal(404, (int)ex.StatusCode);
        Assert.Single(_jobs.Items, j => j.TargetId == lesson.Id);
    }

    // ---- LT-4 - restore ------------------------------------------------------------------------

    [Fact]
    public async Task RestoreAsync_TrashedLesson_ClearsAllDeletionFieldsAndCancelsJob()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        var jobId = _lessons.Items.Single(l => l.Id == lesson.Id).PurgeJobId!;

        await service.RestoreAsync(lesson.Id);

        var entity = _lessons.Items.Single(l => l.Id == lesson.Id);
        Assert.False(entity.IsDelete);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.PurgeJobId);
        Assert.Null(entity.PurgeStartedAt);
        Assert.Equal(BackgroundJobStatus.Canceled, _jobs.Items.Single(j => j.Id == jobId).Status);
    }

    [Fact]
    public async Task RestoreAsync_NeverRestoresTrainingLinks()
    {
        var lesson = SeedActiveLesson();
        SeedLink(lesson.Id, lesson.CompanyId);
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        await service.RestoreAsync(lesson.Id);

        Assert.All(_links.Items, l => Assert.True(l.IsDelete));
    }

    [Fact]
    public async Task RestoreAsync_OnActiveLesson_IsNotFound_Idempotent()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => service.RestoreAsync(lesson.Id));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task RestoreAsync_AfterWorkerClaimedPurge_ReturnsConflict()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        // Simulate the worker's conditional claim (LT-13) having already won.
        var entity = _lessons.Items.Single(l => l.Id == lesson.Id);
        entity.PurgeStartedAt = DateTime.UtcNow;

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => service.RestoreAsync(lesson.Id));
        Assert.Equal(409, (int)ex.StatusCode);
    }

    // ---- LT-10 - manual permanent delete ---------------------------------------------------

    [Fact]
    public async Task RequestPermanentDeleteAsync_WrongTitle_IsValidationError_JobNotAccelerated()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        var job = _jobs.Items.Single(j => j.TargetId == lesson.Id);
        var originalNextAttemptAt = job.NextAttemptAt;

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => service.RequestPermanentDeleteAsync(lesson.Id, "ชื่อผิด"));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Equal(originalNextAttemptAt, job.NextAttemptAt);
    }

    [Fact]
    public async Task RequestPermanentDeleteAsync_IsOrdinalExact_NotCaseInsensitive()
    {
        // Thai script has no case, so this specifically needs a Latin-scripted title to prove the
        // comparison is ordinal (StringComparison.Ordinal), not OrdinalIgnoreCase.
        var lesson = SeedActiveLesson();
        lesson.Title = "Onboarding Lesson";
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        // An ordinal-case-insensitive compare would wrongly accept this - the contract (LT-10)
        // requires an exact match, not "close enough".
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => service.RequestPermanentDeleteAsync(lesson.Id, "ONBOARDING LESSON"));
        Assert.Equal(400, (int)ex.StatusCode);
    }

    [Fact]
    public async Task RequestPermanentDeleteAsync_TrimsSurroundingWhitespaceOnBothSides()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        // Server trims the input before comparing (LT-10) - surrounding whitespace on what the
        // caller typed must not fail the match.
        await service.RequestPermanentDeleteAsync(lesson.Id, $"  {lesson.Title}  ");

        Assert.True(_lessons.Items.Single(l => l.Id == lesson.Id).IsDelete);
    }

    [Fact]
    public async Task RequestPermanentDeleteAsync_CorrectTitle_AcceleratesTheExistingJob_NeverCreatesASecondOne()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        var jobCountBefore = _jobs.Items.Count(j => j.TargetId == lesson.Id);

        // Server trims before comparing - surrounding whitespace on the input itself is fine.
        await service.RequestPermanentDeleteAsync(lesson.Id, $"  {lesson.Title}  ");

        var job = _jobs.Items.Single(j => j.TargetId == lesson.Id);
        Assert.True(job.NextAttemptAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(jobCountBefore, _jobs.Items.Count(j => j.TargetId == lesson.Id));
        // Still queued, not deleted inline - LessonConfig row itself is untouched by this call.
        Assert.True(_lessons.Items.Single(l => l.Id == lesson.Id).IsDelete);
    }

    // ---- LT-23 - tenant isolation on the new trash-specific repository paths ----------------

    [Fact]
    public async Task RestoreAsync_CompanyA_CannotRestoreCompanyBsLesson()
    {
        var lessonB = SeedActiveLesson("lesson-b", TestFixtures.OtherCompanyId);
        var ownerB = BuildService(AdminRole.Owner, TestFixtures.OtherCompanyId);
        await ownerB.ArchiveAsync(lessonB.Id);

        var ownerA = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => ownerA.RestoreAsync(lessonB.Id));

        Assert.Equal(404, (int)ex.StatusCode);
        // B's row must not have changed at all.
        var entity = _lessons.Items.Single(l => l.Id == lessonB.Id);
        Assert.True(entity.IsDelete);
    }

    [Fact]
    public async Task RequestPermanentDeleteAsync_CompanyA_CannotTargetCompanyBsLesson()
    {
        var lessonB = SeedActiveLesson("lesson-b", TestFixtures.OtherCompanyId);
        var ownerB = BuildService(AdminRole.Owner, TestFixtures.OtherCompanyId);
        await ownerB.ArchiveAsync(lessonB.Id);

        var ownerA = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => ownerA.RequestPermanentDeleteAsync(lessonB.Id, lessonB.Title));

        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task GetTrash_OnlyReturnsTheCallersCompany()
    {
        SeedActiveLesson("lesson-a", TestFixtures.CompanyId);
        SeedActiveLesson("lesson-b", TestFixtures.OtherCompanyId);
        var ownerA = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        var ownerB = BuildService(AdminRole.Owner, TestFixtures.OtherCompanyId);

        // Archive both.
        await ownerA.ArchiveAsync("lesson-a");
        await ownerB.ArchiveAsync("lesson-b");

        var trashA = ownerA.GetTrash();

        Assert.Single(trashA);
        Assert.Equal("lesson-a", trashA[0].Id);
    }

    // ---- LT-9 - trash view model ------------------------------------------------------------

    [Fact]
    public async Task GetTrash_ComputesScheduledPurgeAtAndUrgency()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);

        var item = service.GetTrash().Single();

        Assert.Equal(LessonPurgeState.Trash, item.PurgeState);
        Assert.Equal(LessonTrashUrgency.Neutral, item.Urgency); // 60 days out, far from every threshold
        Assert.True(DateTime.Parse(item.ScheduledPurgeAt) > DateTime.Parse(item.DeletedAt));
    }

    [Fact]
    public async Task GetTrash_ReturnsRedToday_WhenLessThan24HoursRemain()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        // 60-day retention minus 23h left => 23h remaining => red_today band.
        _lessons.Items.Single(l => l.Id == lesson.Id).DeletedAt = DateTime.UtcNow.AddDays(-60).AddHours(23);

        var item = service.GetTrash().Single();

        Assert.Equal(LessonTrashUrgency.RedToday, item.Urgency);
    }

    [Fact]
    public async Task GetTrash_ReturnsRed_WhenJustOver24HoursRemain()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        // 60-day retention minus 25h left => 25h remaining => still red, not red_today.
        _lessons.Items.Single(l => l.Id == lesson.Id).DeletedAt = DateTime.UtcNow.AddDays(-60).AddHours(25);

        var item = service.GetTrash().Single();

        Assert.Equal(LessonTrashUrgency.Red, item.Urgency);
    }

    [Fact]
    public async Task GetTrash_ShowsPurgingState_WhenWorkerHasClaimed()
    {
        var lesson = SeedActiveLesson();
        var service = BuildService(AdminRole.Owner, TestFixtures.CompanyId);
        await service.ArchiveAsync(lesson.Id);
        _lessons.Items.Single(l => l.Id == lesson.Id).PurgeStartedAt = DateTime.UtcNow;

        var item = service.GetTrash().Single();

        Assert.Equal(LessonPurgeState.Purging, item.PurgeState);
    }

    // ---- P12-08 - LT-23 tenant isolation on the four raw-SQL-guarded repository methods -----
    //
    // These bypass LessonConfigService entirely and call the repository methods directly: the
    // service's own CurrentCompanyId/EnsureCanAccessCompany checks already stop a cross-company
    // call before it ever reaches these methods, so a service-level test alone would never
    // exercise the CompanyId predicate baked into TryClaimPurge/TryRestoreAndCancelPurge/
    // CancelPendingLessonPurge/AccelerateLessonPurge themselves. The fakes mirror the real
    // WHERE-clause guard exactly (same ILessonConfigRepository/IBackgroundJobRepository
    // contract) - see CompanyIsolationTests.cs's note on why the real Postgres-specific
    // ExecuteSqlRaw calls behind them cannot run against EF Core InMemory.

    private LessonConfig SeedTrashedLessonDirectly(string id, string companyId, string jobId, DateTime? purgeStartedAt = null)
    {
        var lesson = new LessonConfig
        {
            Id = id,
            CompanyId = companyId,
            Slug = $"{id}-slug",
            CategoryId = "kbcat-child",
            Title = $"บทเรียนของ {companyId}",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
            IsDelete = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            PurgeJobId = jobId,
            PurgeStartedAt = purgeStartedAt,
            CreateDate = DateTime.UtcNow.AddDays(-1),
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    private BackgroundJob SeedPendingPurgeJobDirectly(string id, string companyId, string lessonId)
    {
        var job = new BackgroundJob
        {
            Id = id,
            CompanyId = companyId,
            JobType = BackgroundJobType.LessonPurge,
            TargetId = lessonId,
            Status = BackgroundJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = DateTime.UtcNow.AddDays(59),
            CreateDate = DateTime.UtcNow.AddDays(-1),
        };
        _jobs.Items.Add(job);
        return job;
    }

    [Fact]
    public void TryClaimPurge_CompanyACannotClaimCompanyBsPendingPurgeJob()
    {
        var lessonB = SeedTrashedLessonDirectly("lesson-b", TestFixtures.OtherCompanyId, "job-b");

        var claimedByA = _lessons.TryClaimPurge(TestFixtures.CompanyId, "lesson-b", "job-b", DateTime.UtcNow);

        Assert.False(claimedByA);
        Assert.Null(lessonB.PurgeStartedAt);
        // The correct company can still claim its own job - proves the guard is scoped by
        // CompanyId specifically, not just always failing.
        Assert.True(_lessons.TryClaimPurge(TestFixtures.OtherCompanyId, "lesson-b", "job-b", DateTime.UtcNow));
    }

    [Fact]
    public void TryRestoreAndCancelPurge_CompanyACannotRestoreOrCancelCompanyBsJob()
    {
        var lessonB = SeedTrashedLessonDirectly("lesson-b", TestFixtures.OtherCompanyId, "job-b");
        var jobB = SeedPendingPurgeJobDirectly("job-b", TestFixtures.OtherCompanyId, "lesson-b");

        var restoredByA = _lessons.TryRestoreAndCancelPurge(
            TestFixtures.CompanyId, "lesson-b", "job-b", "user-a", DateTime.UtcNow);

        Assert.False(restoredByA);
        Assert.True(lessonB.IsDelete);
        Assert.Equal("job-b", lessonB.PurgeJobId);
        Assert.Equal(BackgroundJobStatus.Pending, jobB.Status);
    }

    [Fact]
    public void CancelPendingLessonPurge_CompanyACannotCancelCompanyBsJob()
    {
        SeedTrashedLessonDirectly("lesson-b", TestFixtures.OtherCompanyId, "job-b");
        var jobB = SeedPendingPurgeJobDirectly("job-b", TestFixtures.OtherCompanyId, "lesson-b");

        var canceledByA = _jobs.CancelPendingLessonPurge(TestFixtures.CompanyId, "lesson-b", "job-b");

        Assert.False(canceledByA);
        Assert.Equal(BackgroundJobStatus.Pending, jobB.Status);
        // Owning company can still cancel its own job.
        Assert.True(_jobs.CancelPendingLessonPurge(TestFixtures.OtherCompanyId, "lesson-b", "job-b"));
    }

    [Fact]
    public void AccelerateLessonPurge_CompanyACannotAccelerateCompanyBsJob()
    {
        SeedTrashedLessonDirectly("lesson-b", TestFixtures.OtherCompanyId, "job-b");
        var jobB = SeedPendingPurgeJobDirectly("job-b", TestFixtures.OtherCompanyId, "lesson-b");
        var originalNextAttemptAt = jobB.NextAttemptAt;

        var acceleratedByA = _jobs.AccelerateLessonPurge(TestFixtures.CompanyId, "lesson-b", "job-b", "actor-a");

        Assert.False(acceleratedByA);
        Assert.Equal(originalNextAttemptAt, jobB.NextAttemptAt);
        // Owning company can still accelerate its own job.
        Assert.True(_jobs.AccelerateLessonPurge(TestFixtures.OtherCompanyId, "lesson-b", "job-b", "actor-b"));
    }
}
