using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ILessonExcludedSlideRepository : IRepositoryBase<LessonExcludedSlide, string>
{
    /// <summary>Every row of this lesson, soft-deleted included - EX-4's toggle needs to find a
    /// previously soft-deleted row and un-delete it instead of adding a second row for the same
    /// page (the "หน้าละหนึ่งแถว" rule DM-17 pushes to the service layer, same as NR-2/TX-3).</summary>
    IQueryable<LessonExcludedSlide> GetByLessonId(string lessonId);

    /// <summary>Soft-deleted included, for the same reason as GetByLessonId above.</summary>
    LessonExcludedSlide? GetOne(string lessonId, string slideObjectId);

    /// <summary>EX-10 - soft deletes every exclusion row of this lesson (a PDF re-upload, or EX-9
    /// replacing the whole exclusion set, both invalidate every previous exclusion in one shot).
    /// Returns the count deleted.</summary>
    int DeleteByLessonId(string lessonId);
}

public sealed class LessonExcludedSlideRepository(ApplicationDbContext dbContext, ICompanyContext companyContext)
    : RepositoryBase<LessonExcludedSlide, string>(dbContext), ILessonExcludedSlideRepository
{
    /// <summary>IgnoreQueryFilters() only exists here to see past the `!IsDelete` half of the
    /// query filter (see ApplicationDbContext) - the soft-deleted rows EX-4 needs to find are
    /// exactly what that half hides. IgnoreQueryFilters also removes the tenant predicate, so it
    /// is reapplied explicitly here even though callers load the lesson through its scoped
    /// repository first. Keeping this predicate at the bypass boundary prevents a future caller
    /// from turning a soft-delete lookup into a cross-company read.</summary>
    public IQueryable<LessonExcludedSlide> GetByLessonId(string lessonId)
        => Context.LessonExcludedSlide.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyContext.CompanyId && x.LessonId == lessonId);

    /// <summary>P11-01 - a legacy duplicate (LessonId, SlideObjectId) pair from before the
    /// ApplyExcludedSlidesAsync fix must never make this throw. FirstOrDefault with the same
    /// tie-break ApplyExcludedSlidesAsync's own reconciliation uses (prefer a live row, else the
    /// most recently touched) so both places agree on which row is "the" row - even though the
    /// reconciliation also hard-deletes the other siblings when it runs, this lookup can be hit
    /// (EX-4's toggle) before that cleanup has ever executed for this lesson.</summary>
    public LessonExcludedSlide? GetOne(string lessonId, string slideObjectId)
        => GetByLessonId(lessonId)
            .Where(x => x.SlideObjectId == slideObjectId)
            .OrderBy(x => x.IsDelete)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefault();

    public int DeleteByLessonId(string lessonId)
    {
        var now = DateTime.UtcNow;
        var rows = GetByLessonId(lessonId).Where(x => !x.IsDelete).ToList();
        foreach (var row in rows)
        {
            row.IsDelete = true;
            row.DeletedAt = now;
            Update(row);
        }
        return rows.Count;
    }
}
