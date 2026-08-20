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
}
