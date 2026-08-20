using Microsoft.EntityFrameworkCore;
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
}
