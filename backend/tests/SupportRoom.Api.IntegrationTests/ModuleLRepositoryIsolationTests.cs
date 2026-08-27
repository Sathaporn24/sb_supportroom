using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Api.IntegrationTests;

/// <summary>
/// LT-23 / P12-08: executes the production PostgreSQL repositories and raw SQL rather than an
/// in-memory fake. Every Module L bypass is asked to operate on company B data with company A.
/// The fixture uses unique IDs and deletes only those rows in finally, so no test data survives.
/// </summary>
public sealed class ModuleLRepositoryIsolationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompanyACannotReadOrMutateCompanyBsTrashJobsOrPurgeDependencies()
    {
        var connectionString = GetConnectionString();
        var suffix = Guid.NewGuid().ToString("N");
        var companyA = $"qa-p12-a-{suffix}";
        var companyB = $"qa-p12-b-{suffix}";
        var activeLessonId = $"qa-p12-active-{suffix}";
        var trashLessonId = $"qa-p12-trash-{suffix}";
        var purgeJobId = $"qa-p12-job-{suffix}";
        var documentId = $"qa-p12-doc-{suffix}";
        var qnaId = $"qa-p12-qna-{suffix}";

        try
        {
            await SeedCompanyBGraphAsync(connectionString, companyB, activeLessonId, trashLessonId, purgeJobId, documentId, qnaId);

            var contextA = new CompanyContext();
            contextA.Resolve(companyA);
            await using var db = CreateContext(connectionString, contextA);

            var lessons = new LessonConfigRepository(db);
            var jobs = new BackgroundJobRepository(db);
            var documents = new DocumentResourceRepository(db);
            var chunks = new DocumentChunkRepository(db);
            var narrations = new LessonSlideNarrationRepository(db);
            var exclusions = new LessonExcludedSlideRepository(db, contextA);
            var qnas = new KnowledgeQnARepository(db);
            var sources = new KnowledgeQnASourceRepository(db);
            var conflicts = new KnowledgeQnAConflictRepository(db);

            // Every write-bearing LT-23 boundary must reject company B's identifiers when the
            // caller is company A. These call the production raw SQL/ExecuteUpdate paths.
            Assert.False(lessons.TryArchive(companyA, activeLessonId, "qa-actor", $"qa-p12-new-{suffix}", DateTime.UtcNow, DateTime.UtcNow.AddDays(60)));
            Assert.False(lessons.TryClaimPurge(companyA, trashLessonId, purgeJobId, DateTime.UtcNow));
            Assert.False(lessons.TryRestore(companyA, trashLessonId, "qa-actor", DateTime.UtcNow));
            Assert.False(lessons.TryRestoreAndCancelPurge(companyA, trashLessonId, purgeJobId, "qa-actor", DateTime.UtcNow));
            Assert.False(jobs.CancelPendingLessonPurge(companyA, trashLessonId, purgeJobId));
            Assert.False(jobs.AccelerateLessonPurge(companyA, trashLessonId, purgeJobId, "qa-actor"));

            // Every Module L IgnoreQueryFilters dependency path must also return no B rows.
            Assert.Empty(await lessons.GetTrash(companyA).ToListAsync());
            Assert.Null(lessons.GetIncludingDeleted(companyA, trashLessonId));
            Assert.Empty(await documents.GetByScopeIncludingDeleted(companyA, KnowledgeScopeType.Lesson, trashLessonId).ToListAsync());
            Assert.Null(documents.GetByIdIncludingDeleted(companyA, documentId));
            Assert.Empty(await chunks.GetAllByDocumentIdIncludingDeleted(companyA, documentId).ToListAsync());
            Assert.Empty(await narrations.GetAllByLessonIdIncludingDeleted(companyA, trashLessonId).ToListAsync());
            Assert.Empty(await exclusions.GetByLessonId(trashLessonId).ToListAsync());
            Assert.Empty(await qnas.GetByScopeIncludingDeleted(companyA, KnowledgeScopeType.Lesson, trashLessonId).ToListAsync());
            Assert.Empty(await sources.GetByQnAIdsIncludingDeleted(companyA, [qnaId]).ToListAsync());
            Assert.Empty(await conflicts.GetByQnAIdsIncludingDeleted(companyA, [qnaId]).ToListAsync());

            // Re-open under B and prove every rejected operation left its graph unchanged.
            var contextB = new CompanyContext();
            contextB.Resolve(companyB);
            await using var verifyDb = CreateContext(connectionString, contextB);
            var active = await verifyDb.LessonConfig.IgnoreQueryFilters().SingleAsync(x => x.Id == activeLessonId);
            var trashed = await verifyDb.LessonConfig.IgnoreQueryFilters().SingleAsync(x => x.Id == trashLessonId);
            var job = await verifyDb.BackgroundJob.SingleAsync(x => x.Id == purgeJobId);
            var link = await verifyDb.TrainingLink.SingleAsync(x => x.LessonId == activeLessonId);
            Assert.False(active.IsDelete);
            Assert.True(trashed.IsDelete);
            Assert.Null(trashed.PurgeStartedAt);
            Assert.Equal(purgeJobId, trashed.PurgeJobId);
            Assert.Equal(BackgroundJobStatus.Pending, job.Status);
            Assert.False(link.IsDelete);
        }
        finally
        {
            await DeleteFixtureRowsAsync(connectionString, companyA, companyB);
        }
    }

    private static async Task SeedCompanyBGraphAsync(
        string connectionString, string companyId, string activeLessonId, string trashLessonId,
        string purgeJobId, string documentId, string qnaId)
    {
        var context = new CompanyContext();
        context.Resolve(companyId);
        await using var db = CreateContext(connectionString, context);
        var now = DateTime.UtcNow;

        db.LessonConfig.AddRange(
            Lesson(activeLessonId, companyId, "active", now, isDelete: false, purgeJobId: null),
            Lesson(trashLessonId, companyId, "trash", now, isDelete: true, purgeJobId));
        db.TrainingLink.Add(new TrainingLink
        {
            Id = $"link-{activeLessonId}", CompanyId = companyId, Token = $"tok-{activeLessonId}",
            LessonId = activeLessonId, LessonSlug = "active", ExpiresAt = now.AddDays(1), CreateDate = now,
        });
        db.BackgroundJob.Add(new BackgroundJob
        {
            Id = purgeJobId, CompanyId = companyId, JobType = BackgroundJobType.LessonPurge,
            TargetId = trashLessonId, Status = BackgroundJobStatus.Pending, AttemptCount = 0,
            NextAttemptAt = now.AddDays(60), CreateDate = now,
        });
        db.DocumentResource.Add(new DocumentResource
        {
            Id = documentId, CompanyId = companyId, ScopeType = KnowledgeScopeType.Lesson,
            ScopeId = trashLessonId, FileName = "fixture.pdf", ContentType = "application/pdf",
            SizeBytes = 1, ObsBucket = "fixture", ObsKey = $"fixture/{documentId}",
            IndexingStatus = DocumentIndexingStatus.Indexed, IndexedChunkCount = 1, CreateDate = now,
        });
        db.DocumentChunk.Add(new DocumentChunk
        {
            Id = $"chunk-{documentId}", CompanyId = companyId, DocumentId = documentId, ChunkKey = "1",
            VectorId = $"vector-{documentId}", NamespaceKey = "fixture", SeqNo = 1, Text = "fixture",
            CharCount = 7, HasSuspectCharacters = false, CreateDate = now,
        });
        db.LessonSlideNarration.Add(new LessonSlideNarration
        {
            Id = $"narr-{trashLessonId}", CompanyId = companyId, LessonId = trashLessonId,
            SlideObjectId = "pdf-page-1", NarrationText = "fixture", CreateDate = now,
        });
        db.LessonExcludedSlide.Add(new LessonExcludedSlide
        {
            Id = $"excl-{trashLessonId}", CompanyId = companyId, LessonId = trashLessonId,
            SlideObjectId = "pdf-page-2", CreateDate = now,
        });
        db.KnowledgeQnA.Add(new KnowledgeQnA
        {
            Id = qnaId, CompanyId = companyId, Question = "question", Answer = "answer",
            ScopeType = KnowledgeScopeType.Lesson, ScopeId = trashLessonId, VectorId = $"vector-{qnaId}",
            IndexingStatus = DocumentIndexingStatus.Indexed, CreateDate = now,
        });
        db.KnowledgeQnASource.Add(new KnowledgeQnASource
        {
            Id = $"source-{qnaId}", CompanyId = companyId, QnAId = qnaId, SessionQuestionId = $"question-{qnaId}", CreateDate = now,
        });
        db.KnowledgeQnAConflict.Add(new KnowledgeQnAConflict
        {
            Id = $"conflict-{qnaId}", CompanyId = companyId, QnAId = qnaId,
            ConflictingSourceLabel = "fixture", CreateDate = now,
        });
        await db.SaveChangesAsync();
    }

    private static LessonConfig Lesson(string id, string companyId, string slug, DateTime now, bool isDelete, string? purgeJobId) => new()
    {
        Id = id, CompanyId = companyId, CategoryId = "fixture-category", Slug = slug, Title = slug,
        SlidesSourceUrl = "", ContentSourceType = LessonContentSourceType.GoogleSlides, SlideConfigs = [],
        IsActive = true, IsDelete = isDelete, DeletedAt = isDelete ? now : null,
        PurgeJobId = purgeJobId, CreateDate = now,
    };

    private static ApplicationDbContext CreateContext(string connectionString, CompanyContext companyContext)
        // AU-2: currentUser ยังไม่ resolve โดยตั้งใจ - fixture ของ Module L ไม่ต้องการให้เกิดแถว
        // AuditLog ปนกับข้อมูลที่ทดสอบ isolation อยู่แล้ว
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options, companyContext, new CurrentUser());

    private static string GetConnectionString()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SupportRoom.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        DotEnv.Load(Path.Combine(directory.FullName, "src", "SupportRoom.Api", ".env"));
        return Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is required for Module L integration tests.");
    }

    private static async Task DeleteFixtureRowsAsync(string connectionString, string companyA, string companyB)
    {
        if (string.IsNullOrEmpty(connectionString)) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var table in new[]
        {
            "SessionQuestionReviewExclusion", "KnowledgeQnAConflict", "KnowledgeQnASource", "KnowledgeQnA",
            "DocumentChunk", "DocumentResource", "LessonExcludedSlide", "LessonSlideNarration",
            "LearningSession", "TrainingLink", "BackgroundJob", "LessonConfig",
        })
        {
            await using var command = new NpgsqlCommand($"DELETE FROM \"{table}\" WHERE \"CompanyId\" = @a OR \"CompanyId\" = @b", connection);
            command.Parameters.AddWithValue("a", companyA);
            command.Parameters.AddWithValue("b", companyB);
            await command.ExecuteNonQueryAsync();
        }
    }
}
