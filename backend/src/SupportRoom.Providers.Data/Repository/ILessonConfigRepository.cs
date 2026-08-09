using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ILessonConfigRepository : IRepositoryBase<LessonConfig, string>
{
    LessonConfig? GetBySlug(string slug);
    IQueryable<LessonConfig> GetActive();
}

public sealed class LessonConfigRepository(ApplicationDbContext dbContext)
    : RepositoryBase<LessonConfig, string>(dbContext), ILessonConfigRepository
{
    public LessonConfig? GetBySlug(string slug)
        => FindBy(x => x.Slug == slug).SingleOrDefault();

    public IQueryable<LessonConfig> GetActive()
        => FindBy(x => x.IsActive);
}
