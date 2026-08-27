using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Tests;

/// <summary>
/// R9/Module L - the durable purge worker (LT-11..LT-14, P12-05/P12-08): claim races, the
/// generation guard, the active-session deferral, and the LT-14 "never permanently failed" retry
/// rule for lesson_purge specifically. Runs BackgroundJobProcessor end to end against the fake
/// repositories - ProcessLessonPurgeAsync is private, so this is the only way to exercise it.
/// </summary>
public class LessonPurgeWorkerTests
{
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeLearningSessionRepository _sessions = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeLessonSlideNarrationRepository _narrations = new();
    private readonly FakeLessonExcludedSlideRepository _excludedSlides = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeDocumentChunkRepository _chunks = new();
    private readonly FakeKnowledgeQnARepository _qnas = new();
    private readonly FakeKnowledgeQnASourceRepository _qnaSources = new();
    private readonly FakeKnowledgeQnAConflictRepository _qnaConflicts = new();
    private readonly FakeSessionQuestionReviewExclusionRepository _exclusions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RecordingKnowledgeIndexProvider _knowledgeIndexProvider = new();
    private readonly RecordingDocumentStorageProvider _storageProvider = new();
    private readonly CompanyContext _companyContext = new();
    private readonly BackgroundJobProcessor _processor;

    public LessonPurgeWorkerTests()
    {
        MapsterConfig.Apply();
        _lessons.TrainingLinks = _links;
        _lessons.BackgroundJobs = _jobs;

        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<ITrainingLinkRepository>(_links);
        _unitOfWork.Register<IBackgroundJobRepository>(_jobs);
        _unitOfWork.Register<ILearningSessionRepository>(_sessions);
        _unitOfWork.Register<ISessionQuestionRepository>(_questions);
        _unitOfWork.Register<ILessonSlideNarrationRepository>(_narrations);
        _unitOfWork.Register<ILessonExcludedSlideRepository>(_excludedSlides);
        _unitOfWork.Register<IDocumentResourceRepository>(_documents);
        _unitOfWork.Register<IDocumentChunkRepository>(_chunks);
        _unitOfWork.Register<IKnowledgeQnARepository>(_qnas);
        _unitOfWork.Register<IKnowledgeQnASourceRepository>(_qnaSources);
        _unitOfWork.Register<IKnowledgeQnAConflictRepository>(_qnaConflicts);
        _unitOfWork.Register<ISessionQuestionReviewExclusionRepository>(_exclusions);
        _unitOfWork.Register<IKnowledgeCategoryRepository>(new FakeKnowledgeCategoryRepository());
        _unitOfWork.Register<ICompanyRepository>(new FakeCompanyRepository());

        _companyContext.Resolve(TestFixtures.CompanyId);

        var serviceProvider = new FakeServiceProvider();
        var (guard, currentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        var lessonConfigService = new LessonConfigService(
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

        _processor = new BackgroundJobProcessor(
            _companyContext,
            _unitOfWork,
            _storageProvider,
            new FakeKnowledgeIndexingService(),
            _knowledgeIndexProvider,
            new KnowledgeNamespaceResolver(_unitOfWork),
            lessonConfigService,
            new LessonSlideNarrationResolver(_unitOfWork),
            NullLogger<IBackgroundJobProcessor>.Instance);
    }

    private LessonConfig SeedTrashedLesson(string id, string jobId, DateTime? purgeStartedAt = null)
    {
        var lesson = new LessonConfig
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            Slug = $"{id}-slug",
            CategoryId = "kbcat-child",
            Title = "บทเรียนทดสอบ",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
            IsDelete = true,
            DeletedAt = DateTime.UtcNow.AddDays(-60),
            PurgeJobId = jobId,
            PurgeStartedAt = purgeStartedAt,
            CreateDate = DateTime.UtcNow.AddDays(-60),
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    private BackgroundJob SeedPurgeJob(string id, string lessonId, int attemptCount = 0)
    {
        var job = new BackgroundJob
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            JobType = BackgroundJobType.LessonPurge,
            TargetId = lessonId,
            Status = BackgroundJobStatus.Running,
            AttemptCount = attemptCount,
            NextAttemptAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow.AddDays(-60),
        };
        _jobs.Items.Add(job);
        return job;
    }

    // ---- LT-3 - 60-day schedule math --------------------------------------------------------

    [Fact]
    public void RetentionDays_IsSixty()
    {
        Assert.Equal(60, LessonTrashPolicy.RetentionDays);
    }

    // ---- LT-11 - stale/generation-mismatch no-op --------------------------------------------

    [Fact]
    public async Task ProcessAsync_LessonRestoredSinceArchive_IsNoOp_DoesNotThrow()
    {
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        lesson.IsDelete = false; // restored concurrently
        var job = SeedPurgeJob("job-1", "lesson-1");

        await _processor.ProcessAsync(job, CancellationToken.None);

        // LT-11 - a no-op is a SUCCESS for this attempt, not an error or a retry.
        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.Empty(_knowledgeIndexProvider.DeletedNamespaces);
    }

    [Fact]
    public async Task ProcessAsync_JobIdDoesNotMatchLessonsCurrentPurgeJobId_IsNoOp()
    {
        // A stale generation: the lesson was restored and re-archived, so its PurgeJobId now
        // points at a newer job than the one this (old) job represents.
        SeedTrashedLesson("lesson-1", "job-new");
        var staleJob = SeedPurgeJob("job-old", "lesson-1");

        await _processor.ProcessAsync(staleJob, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, staleJob.Status);
        Assert.Empty(_knowledgeIndexProvider.DeletedNamespaces);
        Assert.Empty(_storageProvider.DeletedKeys);
    }

    [Fact]
    public async Task ProcessAsync_LessonAlreadyHardDeleted_IsNoOp()
    {
        var job = SeedPurgeJob("job-gone", "lesson-gone");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.Empty(_knowledgeIndexProvider.DeletedNamespaces);
    }

    // ---- LT-12 - active-session one-hour deferral -------------------------------------------

    [Fact]
    public async Task ProcessAsync_ActiveSessionUnderLesson_DefersOneHour_DoesNotClaimOrSpendAttempt()
    {
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        _links.Items.Add(new TrainingLink
        {
            Id = "link-1",
            CompanyId = TestFixtures.CompanyId,
            Token = "tok-1",
            LessonId = "lesson-1",
            LessonSlug = "lesson-1-slug",
            IsDelete = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = "learner-1",
            RecipientName = "ผู้เรียน",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });
        var job = SeedPurgeJob("job-1", "lesson-1", attemptCount: 1);

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal(1, job.AttemptCount); // unchanged - a deferral is not a failed attempt
        Assert.True(job.NextAttemptAt <= DateTime.UtcNow.AddHours(LessonTrashPolicy.ActiveSessionDeferralHours).AddSeconds(5));
        Assert.True(job.NextAttemptAt > DateTime.UtcNow.AddMinutes(55));
        Assert.Null(_lessons.Items.Single(l => l.Id == "lesson-1").PurgeStartedAt); // never claimed
    }

    // ---- LT-13 - conditional claim race -------------------------------------------------------

    [Fact]
    public void TryClaimPurge_SecondWorker_LosesTheRace()
    {
        // Unit-level proxy for LT-13's real guarantee: the actual conditional atomicity is
        // `FOR UPDATE`-equivalent raw SQL that only a real Postgres instance can prove under true
        // concurrency (see ILessonConfigRepository.TryClaimPurge's own doc comment). This proves
        // the fake - and therefore every fake-backed test above - models the same "second caller
        // sees the row already claimed" outcome the real repository guarantees.
        var lesson = SeedTrashedLesson("lesson-1", "job-1");

        var firstClaim = _lessons.TryClaimPurge(TestFixtures.CompanyId, "lesson-1", "job-1", DateTime.UtcNow);
        var secondClaim = _lessons.TryClaimPurge(TestFixtures.CompanyId, "lesson-1", "job-1", DateTime.UtcNow);

        Assert.True(firstClaim);
        Assert.False(secondClaim);
        Assert.NotNull(lesson.PurgeStartedAt);
    }

    [Fact]
    public async Task ProcessAsync_RestartRequeue_JobAlreadyHadClaimedTheLesson_ContinuesThePurgeInstead()
    {
        // DI-11/LT-13 - a process restart requeues a `running` job back to `pending` without
        // touching whatever it already committed. If it already flipped PurgeStartedAt on a
        // previous attempt before crashing, the retry must continue the purge, not re-claim
        // (TryClaimPurge's WHERE requires PurgeStartedAt IS NULL, so a re-claim attempt here would
        // wrongly no-op forever).
        var lesson = SeedTrashedLesson("lesson-1", "job-1", purgeStartedAt: DateTime.UtcNow.AddMinutes(-5));
        var job = SeedPurgeJob("job-1", "lesson-1");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.DoesNotContain(_lessons.Items, l => l.Id == "lesson-1");
        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
    }

    // ---- LT-14/P12-05 - lesson_purge never becomes permanently failed -----------------------

    [Fact]
    public async Task ProcessAsync_ExternalDeleteFails_BelowMaxAttempts_UsesNormalBackoff()
    {
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        var job = SeedPurgeJob("job-1", "lesson-1", attemptCount: 1);
        _knowledgeIndexProvider.ThrowOnNextDelete = true;

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal(2, job.AttemptCount);
        Assert.True(job.NextAttemptAt <= DateTime.UtcNow.AddMinutes(5).AddSeconds(5));
    }

    [Fact]
    public async Task ProcessAsync_ExternalDeleteFailsOnThirdAttempt_StaysPending_RetriesInTwentyFourHours()
    {
        // P12-05 - unlike every other job type, the third failure must NOT flip lesson_purge to
        // `failed`. It must stay `pending` with NextAttemptAt pushed a day out, forever.
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        var job = SeedPurgeJob("job-1", "lesson-1", attemptCount: BackgroundJobBackoff.MaxAttempts - 1);
        _knowledgeIndexProvider.ThrowOnNextDelete = true;

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.NotEqual(BackgroundJobStatus.Failed, job.Status);
        Assert.Equal(BackgroundJobBackoff.MaxAttempts, job.AttemptCount);
        Assert.True(job.NextAttemptAt >= DateTime.UtcNow.AddHours(23));
        Assert.True(job.NextAttemptAt <= DateTime.UtcNow.AddHours(25));
    }

    [Fact]
    public async Task ProcessAsync_ExternalDeleteKeepsFailingPastThirdAttempt_StillNeverBecomesFailed()
    {
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        var job = SeedPurgeJob("job-1", "lesson-1", attemptCount: 10);
        _knowledgeIndexProvider.ThrowOnNextDelete = true;

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal(11, job.AttemptCount);
        Assert.True(job.NextAttemptAt >= DateTime.UtcNow.AddHours(23));
    }

    [Fact]
    public async Task ProcessAsync_NonLessonPurgeJobType_StillBecomesPermanentlyFailedAtMaxAttempts()
    {
        // Contrast case - LT-14's "never permanently failed" exception is scoped to lesson_purge
        // only; DI-9's original permanent-failure behavior must be unchanged for every other job
        // type. Uses an unrecognized JobType to reach HandleFailure without standing up a full
        // document_index pipeline - ProcessAsync's default-case exception is exactly the kind of
        // Exception HandleFailure normally handles.
        var job = new BackgroundJob
        {
            Id = "job-other",
            CompanyId = TestFixtures.CompanyId,
            JobType = "not_a_real_job_type",
            TargetId = "whatever",
            Status = BackgroundJobStatus.Running,
            AttemptCount = BackgroundJobBackoff.MaxAttempts - 1,
            NextAttemptAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow,
        };
        _jobs.Items.Add(job);

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Failed, job.Status);
    }

    // ---- LT-15..LT-20 - the actual purge, once claimed --------------------------------------

    [Fact]
    public async Task ProcessAsync_SuccessfulPurge_DeletesNamespaceAndHardDeletesLesson()
    {
        SeedTrashedLesson("lesson-1", "job-1");
        var job = SeedPurgeJob("job-1", "lesson-1");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Contains(_knowledgeIndexProvider.DeletedNamespaces, ns => ns.Contains("lesson-1-slug"));
        Assert.DoesNotContain(_lessons.Items, l => l.Id == "lesson-1");
        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
    }

    [Fact]
    public async Task ProcessAsync_SuccessfulPurge_CreatesReviewExclusionsBeforeDeletingSourcesAndSurvivesAfterLessonIsGone()
    {
        // (d) queue/purge finalization - the exclusion tombstone must exist and keep suppressing
        // the question even after the lesson/Q&A/source rows that produced it are hard-deleted.
        var lesson = SeedTrashedLesson("lesson-1", "job-1");
        _links.Items.Add(new TrainingLink
        {
            Id = "link-1",
            CompanyId = TestFixtures.CompanyId,
            Token = "tok-1",
            LessonId = "lesson-1",
            LessonSlug = "lesson-1-slug",
            IsDelete = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = "learner-1",
            RecipientName = "ผู้เรียน",
            Status = SessionStatus.Ended,
            StartedAt = DateTime.UtcNow.AddDays(-61),
            EndedAt = DateTime.UtcNow.AddDays(-61),
            LastActivityAt = DateTime.UtcNow.AddDays(-61),
        });
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "session-1",
            AnswerStatus = AnswerStatus.NotFound,
            Source = QuestionSource.Voice,
            CreateDate = DateTime.UtcNow.AddDays(-61),
        });
        var job = SeedPurgeJob("job-1", "lesson-1");

        await _processor.ProcessAsync(job, CancellationToken.None);

        var exclusion = Assert.Single(_exclusions.Items);
        Assert.Equal("q-1", exclusion.SessionQuestionId);
        Assert.Equal(QuestionReviewExclusionReason.LessonPermanentlyDeleted, exclusion.Reason);
        // The lesson itself is now gone, but the exclusion (and the question it references) stay -
        // proving suppression does not depend on the lesson/Q&A rows still existing (LT-19).
        Assert.DoesNotContain(_lessons.Items, l => l.Id == "lesson-1");
        Assert.Single(_questions.Items);

        // (LT-19 retention, the rest of it) - everything else this snapshot touched must also
        // survive untouched: revoked TrainingLink, ended LearningSession, and the purge job's own
        // BackgroundJob history row. Only the LessonConfig row itself is ever hard-deleted.
        Assert.Contains(_links.Items, l => l.Id == "link-1");
        Assert.Contains(_sessions.Items, s => s.Id == "session-1");
        Assert.Contains(_jobs.Items, j => j.Id == "job-1" && j.Status == BackgroundJobStatus.Succeeded);
    }

    // ---- LT-17..LT-20/P12-08 - LessonExcludedSlide is hard-deleted by purge, not left behind ----

    [Fact]
    public async Task ProcessAsync_SuccessfulPurge_HardDeletesLessonExcludedSlideRows()
    {
        SeedTrashedLesson("lesson-1", "job-1");
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-1",
            CompanyId = TestFixtures.CompanyId,
            LessonId = "lesson-1",
            SlideObjectId = "pdf-page-1",
            CreateDate = DateTime.UtcNow.AddDays(-61),
        });
        var job = SeedPurgeJob("job-1", "lesson-1");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.DoesNotContain(_excludedSlides.Items, x => x.LessonId == "lesson-1");
    }

    // ---- LT-18/Q-L3 - shared-PDF guard: another lesson still referencing the same document -----

    [Fact]
    public async Task ProcessAsync_DocumentStillReferencedByAnotherLesson_PreservesResourceBytesChunksAndVectors()
    {
        // Lesson A is active and keeps using the shared PDF; lesson B is the one being purged.
        var lessonA = new LessonConfig
        {
            Id = "lesson-a",
            CompanyId = TestFixtures.CompanyId,
            Slug = "lesson-a-slug",
            CategoryId = "kbcat-child",
            Title = "บทเรียน A",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.Pdf,
            PdfDocumentResourceId = "doc-shared",
            SlideConfigs = [],
            IsActive = true,
            CreateDate = DateTime.UtcNow,
        };
        _lessons.Items.Add(lessonA);

        var lessonB = SeedTrashedLesson("lesson-b", "job-1");
        lessonB.ContentSourceType = LessonContentSourceType.Pdf;
        lessonB.PdfDocumentResourceId = "doc-shared";

        _documents.Items.Add(new DocumentResource
        {
            Id = "doc-shared",
            CompanyId = TestFixtures.CompanyId,
            ScopeType = KnowledgeScopeType.Company,
            ScopeId = null,
            FileName = "shared.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            ObsBucket = "documents",
            ObsKey = "documents/doc-shared/shared.pdf",
            IndexingStatus = DocumentIndexingStatus.Indexed,
            IndexedChunkCount = 1,
            CreateDate = DateTime.UtcNow.AddDays(-61),
        });
        _chunks.Items.Add(new DocumentChunk
        {
            Id = "chunk-1",
            CompanyId = TestFixtures.CompanyId,
            DocumentId = "doc-shared",
            ChunkKey = "page-1",
            VectorId = "doc-shared-page-1",
            NamespaceKey = "kb-global",
            SeqNo = 1,
            Text = "เนื้อหา",
            CharCount = 6,
            HasSuspectCharacters = false,
            CreateDate = DateTime.UtcNow.AddDays(-61),
        });

        var job = SeedPurgeJob("job-1", "lesson-b");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.DoesNotContain(_lessons.Items, l => l.Id == "lesson-b");
        Assert.Contains(_lessons.Items, l => l.Id == "lesson-a");
        // The shared document, its chunk, its storage bytes, and its vector must all survive -
        // lesson A still references the same DocumentResource.
        Assert.Contains(_documents.Items, d => d.Id == "doc-shared");
        Assert.Contains(_chunks.Items, c => c.Id == "chunk-1");
        Assert.DoesNotContain(_storageProvider.DeletedKeys, k => k == "documents/doc-shared/shared.pdf");
        Assert.DoesNotContain(_knowledgeIndexProvider.DeletedVectors, v => v.Id == "doc-shared-page-1");
    }

    // ---- LT-17/LT-18 - idempotent external delete: a resource already gone must not fail or retry ----

    [Fact]
    public async Task ProcessAsync_DocumentBytesAlreadyMissingFromStorage_StillSucceeds_DoesNotRetry()
    {
        // Uses the REAL LocalDocumentStorageProvider (not the recording fake) - its DeleteAsync
        // already treats a missing file as a no-op success (see LocalDocumentStorageProvider),
        // which is exactly the "already gone" behavior LT-18's comment claims for both real
        // IDocumentStorageProvider implementations. This proves it against the actual provider,
        // not just a comment.
        var realStorageProvider = new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance);
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork
            .Register<ILessonConfigRepository>(_lessons)
            .Register<ITrainingLinkRepository>(_links)
            .Register<IBackgroundJobRepository>(_jobs)
            .Register<ILearningSessionRepository>(_sessions)
            .Register<ISessionQuestionRepository>(_questions)
            .Register<ILessonSlideNarrationRepository>(_narrations)
            .Register<ILessonExcludedSlideRepository>(_excludedSlides)
            .Register<IDocumentResourceRepository>(_documents)
            .Register<IDocumentChunkRepository>(_chunks)
            .Register<IKnowledgeQnARepository>(_qnas)
            .Register<IKnowledgeQnASourceRepository>(_qnaSources)
            .Register<IKnowledgeQnAConflictRepository>(_qnaConflicts)
            .Register<ISessionQuestionReviewExclusionRepository>(_exclusions)
            .Register<IKnowledgeCategoryRepository>(new FakeKnowledgeCategoryRepository())
            .Register<ICompanyRepository>(new FakeCompanyRepository());

        var serviceProvider = new FakeServiceProvider();
        var (guard, currentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        var lessonConfigService = new LessonConfigService(
            unitOfWork,
            serviceProvider,
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            new FakeKnowledgeIndexingService(),
            realStorageProvider,
            new MemoryCache(new MemoryCacheOptions()),
            new LessonSlideNarrationResolver(unitOfWork),
            guard,
            currentUser);
        var processor = new BackgroundJobProcessor(
            _companyContext,
            unitOfWork,
            realStorageProvider,
            new FakeKnowledgeIndexingService(),
            _knowledgeIndexProvider,
            new KnowledgeNamespaceResolver(unitOfWork),
            lessonConfigService,
            new LessonSlideNarrationResolver(unitOfWork),
            NullLogger<IBackgroundJobProcessor>.Instance);

        SeedTrashedLesson("lesson-missing-file", "job-1");
        // Never uploaded - ResolvePath is deterministic and this key is guaranteed not to exist on
        // disk under the default LOCAL_STORAGE_PATH.
        var missingKey = $"documents/missing-{Guid.NewGuid()}/never-uploaded.pdf";
        _documents.Items.Add(new DocumentResource
        {
            Id = "doc-missing",
            CompanyId = TestFixtures.CompanyId,
            ScopeType = KnowledgeScopeType.Lesson,
            ScopeId = "lesson-missing-file",
            FileName = "never-uploaded.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            ObsBucket = "documents",
            ObsKey = missingKey,
            IndexingStatus = DocumentIndexingStatus.Indexed,
            IndexedChunkCount = 0,
            CreateDate = DateTime.UtcNow.AddDays(-61),
        });
        var job = SeedPurgeJob("job-1", "lesson-missing-file");

        await processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.Equal(0, job.AttemptCount);
        Assert.DoesNotContain(_documents.Items, d => d.Id == "doc-missing");
        Assert.DoesNotContain(_lessons.Items, l => l.Id == "lesson-missing-file");
    }

    [Fact]
    public async Task ProcessAsync_CompleteDependencyGraph_RemovesOnlyPurgeDependentsAndRetainsLearnerHistory()
    {
        // P12-08 / LT-15..LT-20: deliberately seed every node of the purge graph. The callback
        // observes the live exclusion list at the instant a Q&A source is deleted, proving the
        // worker inserts permanent exclusions before it starts removing source/Q&A rows.
        SeedTrashedLesson("lesson-graph", "job-graph");
        _links.Items.Add(new TrainingLink
        {
            Id = "link-graph", CompanyId = TestFixtures.CompanyId, Token = "tok-graph",
            LessonId = "lesson-graph", LessonSlug = "lesson-graph-slug", IsDelete = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1), CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-graph", CompanyId = TestFixtures.CompanyId, TrainingLinkId = "link-graph",
            LearnerKey = "learner-graph", RecipientName = "ผู้เรียน", Status = SessionStatus.Ended,
            StartedAt = DateTime.UtcNow.AddDays(-60), EndedAt = DateTime.UtcNow.AddDays(-59),
            LastActivityAt = DateTime.UtcNow.AddDays(-59), CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _questions.Items.Add(new SessionQuestion
        {
            Id = "question-graph", CompanyId = TestFixtures.CompanyId, SessionId = "session-graph",
            AnswerStatus = AnswerStatus.NotFound, Source = QuestionSource.Text, CreateDate = DateTime.UtcNow.AddDays(-59),
        });
        _narrations.Items.Add(new LessonSlideNarration
        {
            Id = "narr-graph", CompanyId = TestFixtures.CompanyId, LessonId = "lesson-graph",
            SlideObjectId = "pdf-page-1", NarrationText = "บทพูด", CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "excluded-graph", CompanyId = TestFixtures.CompanyId, LessonId = "lesson-graph",
            SlideObjectId = "pdf-page-2", CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _documents.Items.Add(new DocumentResource
        {
            Id = "doc-graph", CompanyId = TestFixtures.CompanyId, ScopeType = KnowledgeScopeType.Lesson,
            ScopeId = "lesson-graph", FileName = "graph.pdf", ContentType = "application/pdf", SizeBytes = 2,
            ObsBucket = "documents", ObsKey = "documents/graph.pdf", IndexingStatus = DocumentIndexingStatus.Indexed,
            IndexedChunkCount = 1, CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _chunks.Items.Add(new DocumentChunk
        {
            Id = "chunk-graph", CompanyId = TestFixtures.CompanyId, DocumentId = "doc-graph", ChunkKey = "1",
            VectorId = "doc-graph-page-1", NamespaceKey = "graph-namespace", SeqNo = 1, Text = "เนื้อหา",
            CharCount = 6, HasSuspectCharacters = false, CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _qnas.Items.Add(new KnowledgeQnA
        {
            Id = "qna-graph", CompanyId = TestFixtures.CompanyId, Question = "ถาม", Answer = "ตอบ",
            ScopeType = KnowledgeScopeType.Lesson, ScopeId = "lesson-graph", VectorId = "qna-graph-vector",
            IndexedNamespaceKey = "graph-namespace", IndexingStatus = DocumentIndexingStatus.Indexed,
            CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _qnaSources.Items.Add(new KnowledgeQnASource
        {
            Id = "source-graph", CompanyId = TestFixtures.CompanyId, QnAId = "qna-graph",
            SessionQuestionId = "question-graph", CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _qnaConflicts.Items.Add(new KnowledgeQnAConflict
        {
            Id = "conflict-graph", CompanyId = TestFixtures.CompanyId, QnAId = "qna-graph",
            SessionQuestionId = "question-graph", ConflictingSourceLabel = "graph.pdf", CreateDate = DateTime.UtcNow.AddDays(-60),
        });
        _qnaSources.OnDelete = _ => Assert.Contains(_exclusions.Items, x => x.SessionQuestionId == "question-graph");
        var job = SeedPurgeJob("job-graph", "lesson-graph");

        await _processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(BackgroundJobStatus.Succeeded, job.Status);
        Assert.Contains(_exclusions.Items, x => x.SessionQuestionId == "question-graph"
            && x.LessonId == "lesson-graph" && x.Reason == QuestionReviewExclusionReason.LessonPermanentlyDeleted);
        Assert.DoesNotContain(_narrations.Items, x => x.LessonId == "lesson-graph");
        Assert.DoesNotContain(_excludedSlides.Items, x => x.LessonId == "lesson-graph");
        Assert.DoesNotContain(_documents.Items, x => x.Id == "doc-graph");
        Assert.DoesNotContain(_chunks.Items, x => x.DocumentId == "doc-graph");
        Assert.DoesNotContain(_qnas.Items, x => x.Id == "qna-graph");
        Assert.DoesNotContain(_qnaSources.Items, x => x.QnAId == "qna-graph");
        Assert.DoesNotContain(_qnaConflicts.Items, x => x.QnAId == "qna-graph");
        Assert.DoesNotContain(_lessons.Items, x => x.Id == "lesson-graph");
        Assert.Contains(_links.Items, x => x.Id == "link-graph");
        Assert.Contains(_sessions.Items, x => x.Id == "session-graph");
        Assert.Contains(_questions.Items, x => x.Id == "question-graph");
        Assert.Contains(_jobs.Items, x => x.Id == "job-graph");
        Assert.Contains(_storageProvider.DeletedKeys, x => x == "documents/graph.pdf");
        Assert.Contains(_knowledgeIndexProvider.DeletedVectors, x => x.Id == "doc-graph-page-1");
        Assert.Contains(_knowledgeIndexProvider.DeletedVectors, x => x.Id == "qna-graph-vector");
    }
}
