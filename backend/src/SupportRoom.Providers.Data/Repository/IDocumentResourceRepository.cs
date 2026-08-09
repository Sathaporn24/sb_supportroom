using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IDocumentResourceRepository : IRepositoryBase<DocumentResource, string>
{
    IQueryable<DocumentResource> GetByLessonId(string lessonId);

    /// <summary>Standalone/global knowledge-base documents (LessonId is null).</summary>
    IQueryable<DocumentResource> GetStandalone();
}

public sealed class DocumentResourceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DocumentResource, string>(dbContext), IDocumentResourceRepository
{
    public IQueryable<DocumentResource> GetByLessonId(string lessonId)
        => FindBy(x => x.LessonId == lessonId);

    public IQueryable<DocumentResource> GetStandalone()
        => FindBy(x => x.LessonId == null);
}
