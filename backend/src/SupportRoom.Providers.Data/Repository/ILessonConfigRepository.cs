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
}
