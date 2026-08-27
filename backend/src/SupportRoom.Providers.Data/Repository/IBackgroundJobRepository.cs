using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IBackgroundJobRepository : IRepositoryBase<BackgroundJob, string>
{
    /// <summary>
    /// Atomically claims the single oldest ready job (design.md DI-12): flips it from pending to
    /// running and returns it in one statement, so two worker loops (or a retried claim inside the
    /// same process) can never grab the same row. `FOR UPDATE SKIP LOCKED` is why - a concurrent
    /// claim just skips a row another transaction is already holding, instead of blocking on it or
    /// racing to update it after both read it as pending.
    ///
    /// IgnoreQueryFilters(): BackgroundJob carries no company filter at all (see
    /// ApplicationDbContext), so this call bypasses nothing - it's here for the same reason
    /// ITrainingLinkRepository.GetByToken documents it: the caller (the worker loop) doesn't know
    /// which company's job it's about to pick up until this returns one.
    /// </summary>
    BackgroundJob? ClaimNext(DateTime now);

    /// <summary>
    /// Resets every job stuck at "running" back to "pending" without bumping AttemptCount - called
    /// once at worker startup (design.md DI-11). A "running" row surviving to the next process
    /// start didn't fail, it was killed mid-job (the app restarted), so it shouldn't burn one of
    /// its three retry attempts for that. Returns the number of rows reset, for a startup log line.
    ///
    /// IgnoreQueryFilters() for the same reason as ClaimNext - startup has no company context yet
    /// and this must catch orphaned jobs across every company.
    /// </summary>
    int RequeueOrphanedRunning();

    /// <summary>R9/LT-4 - restore cancels the specific lesson_purge job it is undoing, never "the
    /// job with this TargetId" alone: BackgroundJob has no query filter, so company + target +
    /// job id must all match in the same predicate or one company's restore could cancel another
    /// company's job that happens to share a TargetId collision window. Only flips a job still
    /// Pending - a job the worker already claimed (Running/PurgeStartedAt set) must not be
    /// canceled from here, that case is 409 at the LessonConfig level instead (LT-4). Returns
    /// whether a row was actually canceled.</summary>
    bool CancelPendingLessonPurge(string companyId, string lessonId, string purgeJobId);

    /// <summary>R9/LT-10 - manual permanent-delete's "accelerate the existing job" step: pulls
    /// NextAttemptAt to now so the worker picks it up on its next poll instead of creating a
    /// second job. Same company+target+job id guard as CancelPendingLessonPurge, for the same
    /// reason. Only affects a job still Pending.
    ///
    /// RS-5 (design.md, Module B) - `actorUserId` exists purely so this raw-SQL write can create
    /// its own AuditLog row: ExecuteSqlRaw bypasses ChangeTracker entirely, so the SaveChanges
    /// interceptor (AU-*) never sees this write. Null actor -> no row (mirrors AU-2/OQ-3).</summary>
    bool AccelerateLessonPurge(string companyId, string lessonId, string purgeJobId, string? actorUserId);
}

public sealed class BackgroundJobRepository(ApplicationDbContext dbContext)
    : RepositoryBase<BackgroundJob, string>(dbContext), IBackgroundJobRepository
{
    public BackgroundJob? ClaimNext(DateTime now)
    {
        const string sql = """
            UPDATE "BackgroundJob"
            SET "Status" = {0}, "StartedAt" = {2}
            WHERE "Id" = (
                SELECT "Id" FROM "BackgroundJob"
                WHERE "Status" = {1} AND "NextAttemptAt" <= {2}
                ORDER BY "NextAttemptAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            )
            RETURNING *
            """;

        return Context.BackgroundJob
            .FromSqlRaw(sql, BackgroundJobStatus.Running, BackgroundJobStatus.Pending, now)
            .IgnoreQueryFilters()
            .AsEnumerable()
            .FirstOrDefault();
    }

    public int RequeueOrphanedRunning()
    {
        const string sql = """
            UPDATE "BackgroundJob"
            SET "Status" = {0}, "StartedAt" = NULL
            WHERE "Status" = {1}
            """;

        return Context.Database.ExecuteSqlRaw(sql, BackgroundJobStatus.Pending, BackgroundJobStatus.Running);
    }

    public bool CancelPendingLessonPurge(string companyId, string lessonId, string purgeJobId)
    {
        const string sql = """
            UPDATE "BackgroundJob"
            SET "Status" = {0}
            WHERE "Id" = {1} AND "CompanyId" = {2} AND "JobType" = {3} AND "TargetId" = {4} AND "Status" = {5}
            """;
        var rows = Context.Database.ExecuteSqlRaw(
            sql, BackgroundJobStatus.Canceled, purgeJobId, companyId, BackgroundJobType.LessonPurge, lessonId, BackgroundJobStatus.Pending);
        return rows == 1;
    }

    public bool AccelerateLessonPurge(string companyId, string lessonId, string purgeJobId, string? actorUserId)
    {
        using var transaction = Context.Database.BeginTransaction();

        const string sql = """
            UPDATE "BackgroundJob"
            SET "NextAttemptAt" = {0}
            WHERE "Id" = {1} AND "CompanyId" = {2} AND "JobType" = {3} AND "TargetId" = {4} AND "Status" = {5}
            """;
        var now = DateTime.UtcNow;
        var rows = Context.Database.ExecuteSqlRaw(
            sql, now, purgeJobId, companyId, BackgroundJobType.LessonPurge, lessonId, BackgroundJobStatus.Pending);
        if (rows != 1)
        {
            transaction.Rollback();
            return false;
        }

        // RS-5: 1 แถวเท่านั้น (update/BackgroundJob/purgeJobId) - ห้ามเขียนแถวของ LessonConfig
        // ที่นี่ คำสั่งนี้แตะแค่แถว job · null actorUserId = ไม่เขียนแถว (มติ OQ-3)
        if (!string.IsNullOrEmpty(actorUserId))
        {
            Context.Set<AuditLog>().Add(new AuditLog
            {
                Id = IdGenerator.GenerateId("audit"),
                CompanyId = companyId,
                ActorUserId = actorUserId,
                Action = AuditAction.Update,
                EntityName = nameof(BackgroundJob),
                EntityId = purgeJobId,
                OccurredAt = now,
            });
            Context.SaveChanges();
        }

        transaction.Commit();
        return true;
    }
}
