using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ILessonSlideNarrationRepository : IRepositoryBase<LessonSlideNarration, string>
{
    IQueryable<LessonSlideNarration> GetByLessonId(string lessonId);
    LessonSlideNarration? GetOne(string lessonId, string slideObjectId);

    /// <summary>R4.3 - soft deletes every narration row of this lesson (PDF re-upload wipes all
    /// CS-authored overrides, since pdf-page-N is a raw page-number key that a re-upload can no
    /// longer be trusted to line up with). Returns the count deleted, so the caller can report it
    /// back for the pre-confirm warning (NR-3).</summary>
    int DeleteByLessonId(string lessonId);
}

public sealed class LessonSlideNarrationRepository(ApplicationDbContext dbContext)
    : RepositoryBase<LessonSlideNarration, string>(dbContext), ILessonSlideNarrationRepository
{
    public IQueryable<LessonSlideNarration> GetByLessonId(string lessonId)
        => FindBy(x => x.LessonId == lessonId);

    public LessonSlideNarration? GetOne(string lessonId, string slideObjectId)
        => FindBy(x => x.LessonId == lessonId && x.SlideObjectId == slideObjectId).SingleOrDefault();

    public int DeleteByLessonId(string lessonId)
    {
        var now = DateTime.UtcNow;
        var rows = FindBy(x => x.LessonId == lessonId).ToList();
        foreach (var row in rows)
        {
            row.IsDelete = true;
            row.DeletedAt = now;
            Update(row);
        }
        return rows.Count;
    }
}
