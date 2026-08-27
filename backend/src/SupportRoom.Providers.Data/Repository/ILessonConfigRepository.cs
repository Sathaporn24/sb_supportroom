using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ILessonConfigRepository : IRepositoryBase<LessonConfig, string>
{
    LessonConfig? GetBySlug(string slug);
    IQueryable<LessonConfig> GetActive();
    IQueryable<LessonConfig> GetByCategoryId(string categoryId);
    int CountByCategoryId(string categoryId);

    /// <summary>R9/LT-7/LT-9 - every trashed lesson of this company, regardless of purge state.
    /// IgnoreQueryFilters() only exists to see past the `!IsDelete` half of the query filter (see
    /// ApplicationDbContext) - CompanyId is reapplied explicitly here (LT-23), the same pattern as
    /// IDocumentResourceRepository.GetDeleted.</summary>
    IQueryable<LessonConfig> GetTrash(string companyId);

    /// <summary>R9/LT-23 - the one lookup every trash/restore/manual-purge/purge-worker path must
    /// use instead of Get(id): it has to see a trashed row (the normal query filter hides it) while
    /// still refusing to leak company B's lesson to company A (IgnoreQueryFilters() drops that half
    /// of the filter too, so CompanyId is reapplied explicitly in the same predicate).</summary>
    LessonConfig? GetIncludingDeleted(string companyId, string lessonId);

    /// <summary>LT-3 - creates this trash generation exactly once. The conditional lesson update,
    /// purge-job insert, and link revocation share one database transaction so concurrent archive
    /// requests cannot leave a second job behind.</summary>
    bool TryArchive(string companyId, string lessonId, string? actorUserId, string purgeJobId, DateTime now, DateTime scheduledPurgeAt);

    /// <summary>R9/LT-13 - the worker's conditional claim: flips PurgeStartedAt only if this row is
    /// still exactly `(CompanyId, Id, IsDelete=true, PurgeJobId=purgeJobId, PurgeStartedAt=null)`,
    /// atomically at the database level (same FOR-UPDATE-style reasoning as
    /// IBackgroundJobRepository.ClaimNext) - a restore racing the same instant either wins this
    /// update or loses it, never both. Returns whether THIS call was the one that won.</summary>
    bool TryClaimPurge(string companyId, string lessonId, string purgeJobId, DateTime now);

    /// <summary>R9/LT-4 - restore's own conditional transaction, the mirror image of
    /// TryClaimPurge: flips the lesson back to fully active only if it is still
    /// `(CompanyId, Id, IsDelete=true, PurgeStartedAt=null)` at the moment this runs. Returns false
    /// (never throws) when the worker won the race first or the row is not in that state, so the
    /// caller can turn that into 409 "เริ่มลบถาวรแล้ว" (LT-4) without a lost-update window between
    /// reading the row and writing it back.</summary>
    bool TryRestore(string companyId, string lessonId, string? actorUserId, DateTime now);

    /// <summary>LT-4 - restores the exact trash generation and cancels its matching pending job
    /// in the same transaction. A failed conditional update means purge claimed or another
    /// restore/archive transition won first.</summary>
    bool TryRestoreAndCancelPurge(string companyId, string lessonId, string purgeJobId, string? actorUserId, DateTime now);
}

public sealed class LessonConfigRepository(ApplicationDbContext dbContext)
    : RepositoryBase<LessonConfig, string>(dbContext), ILessonConfigRepository
{
    public LessonConfig? GetBySlug(string slug)
        => FindBy(x => x.Slug == slug).SingleOrDefault();

    public IQueryable<LessonConfig> GetActive()
        => FindBy(x => x.IsActive);

    public IQueryable<LessonConfig> GetByCategoryId(string categoryId)
        => FindBy(x => x.CategoryId == categoryId);

    public int CountByCategoryId(string categoryId)
        => FindBy(x => x.CategoryId == categoryId).Count();

    public IQueryable<LessonConfig> GetTrash(string companyId)
        => Context.LessonConfig.IgnoreQueryFilters().Where(x => x.CompanyId == companyId && x.IsDelete);

    public LessonConfig? GetIncludingDeleted(string companyId, string lessonId)
        => Context.LessonConfig.IgnoreQueryFilters().SingleOrDefault(x => x.CompanyId == companyId && x.Id == lessonId);

    public bool TryArchive(string companyId, string lessonId, string? actorUserId, string purgeJobId, DateTime now, DateTime scheduledPurgeAt)
    {
        using var transaction = Context.Database.BeginTransaction();

        var archived = Context.LessonConfig.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && x.Id == lessonId && !x.IsDelete)
            .ExecuteUpdate(setters => setters
                .SetProperty(x => x.IsDelete, true)
                .SetProperty(x => x.DeletedAt, now)
                .SetProperty(x => x.DeleteBy, actorUserId)
                .SetProperty(x => x.PurgeJobId, purgeJobId)
                .SetProperty(x => x.PurgeStartedAt, (DateTime?)null)
                .SetProperty(x => x.UpdateBy, actorUserId)
                .SetProperty(x => x.UpdateDate, now));
        if (archived != 1)
        {
            transaction.Rollback();
            return false;
        }

        Context.BackgroundJob.Add(new BackgroundJob
        {
            Id = purgeJobId,
            CompanyId = companyId,
            CreateBy = actorUserId,
            CreateDate = now,
            JobType = SupportRoom.Domain.Enums.BackgroundJobType.LessonPurge,
            TargetId = lessonId,
            Status = SupportRoom.Domain.Enums.BackgroundJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = scheduledPurgeAt,
        });
        Context.SaveChanges();

        Context.TrainingLink
            .Where(x => x.CompanyId == companyId && x.LessonId == lessonId && !x.IsDelete)
            .ExecuteUpdate(setters => setters
                .SetProperty(x => x.IsDelete, true)
                .SetProperty(x => x.DeletedAt, now)
                .SetProperty(x => x.DeleteBy, actorUserId)
                .SetProperty(x => x.UpdateBy, actorUserId)
                .SetProperty(x => x.UpdateDate, now));

        transaction.Commit();
        return true;
    }

    public bool TryClaimPurge(string companyId, string lessonId, string purgeJobId, DateTime now)
    {
        const string sql = """
            UPDATE "LessonConfig"
            SET "PurgeStartedAt" = {0}
            WHERE "Id" = {1} AND "CompanyId" = {2} AND "IsDelete" = TRUE
                  AND "PurgeJobId" = {3} AND "PurgeStartedAt" IS NULL
            """;
        var rows = Context.Database.ExecuteSqlRaw(sql, now, lessonId, companyId, purgeJobId);
        return rows == 1;
    }

    public bool TryRestore(string companyId, string lessonId, string? actorUserId, DateTime now)
    {
        const string sql = """
            UPDATE "LessonConfig"
            SET "IsDelete" = FALSE, "DeletedAt" = NULL, "DeleteBy" = NULL,
                "PurgeJobId" = NULL, "PurgeStartedAt" = NULL,
                "UpdateBy" = {2}, "UpdateDate" = {3}
            WHERE "Id" = {0} AND "CompanyId" = {1} AND "IsDelete" = TRUE AND "PurgeStartedAt" IS NULL
            """;
        var rows = Context.Database.ExecuteSqlRaw(sql, lessonId, companyId, actorUserId, now);
        return rows == 1;
    }

    public bool TryRestoreAndCancelPurge(string companyId, string lessonId, string purgeJobId, string? actorUserId, DateTime now)
    {
        using var transaction = Context.Database.BeginTransaction();

        const string restoreSql = """
            UPDATE "LessonConfig"
            SET "IsDelete" = FALSE, "DeletedAt" = NULL, "DeleteBy" = NULL,
                "PurgeJobId" = NULL, "PurgeStartedAt" = NULL,
                "UpdateBy" = {3}, "UpdateDate" = {4}
            WHERE "Id" = {0} AND "CompanyId" = {1} AND "IsDelete" = TRUE
                  AND "PurgeStartedAt" IS NULL AND "PurgeJobId" = {2}
            """;
        var restored = Context.Database.ExecuteSqlRaw(restoreSql, lessonId, companyId, purgeJobId, actorUserId, now);
        if (restored != 1)
        {
            transaction.Rollback();
            return false;
        }

        const string cancelSql = """
            UPDATE "BackgroundJob"
            SET "Status" = {0}, "UpdateBy" = {5}, "UpdateDate" = {6}
            WHERE "Id" = {1} AND "CompanyId" = {2} AND "JobType" = {3}
                  AND "TargetId" = {4} AND "Status" = {7}
            """;
        var canceled = Context.Database.ExecuteSqlRaw(
            cancelSql,
            SupportRoom.Domain.Enums.BackgroundJobStatus.Canceled,
            purgeJobId,
            companyId,
            SupportRoom.Domain.Enums.BackgroundJobType.LessonPurge,
            lessonId,
            actorUserId,
            now,
            SupportRoom.Domain.Enums.BackgroundJobStatus.Pending);
        if (canceled != 1)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }
}
