using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ITrainingLinkRepository : IRepositoryBase<TrainingLink, string>
{
    /// <summary>
    /// The one lookup that deliberately bypasses the company query filter. It is the entry point
    /// for every recipient-side request: the caller holds only a join token and no company has
    /// been resolved yet, so filtering here would match zero rows and no link could ever be
    /// opened. The token is itself the credential (unguessable, globally unique), and callers
    /// must resolve ICompanyContext from the returned link before touching anything else -
    /// see ITrainingLinkService.GetByToken.
    /// </summary>
    TrainingLink? GetByToken(string token);

    /// <summary>R9/LT-3/LT-4 - every link a lesson ever had, for archive's revoke-all-links step and
    /// admin history views. Left scoped by the normal query filter (CompanyId only, no !IsDelete -
    /// see ApplicationDbContext's O-3 note) since every caller already has a resolved company.</summary>
    IQueryable<TrainingLink> GetByLessonId(string lessonId);
}

public sealed class TrainingLinkRepository(ApplicationDbContext dbContext)
    : RepositoryBase<TrainingLink, string>(dbContext), ITrainingLinkRepository
{
    public TrainingLink? GetByToken(string token)
        => Context.TrainingLink.IgnoreQueryFilters().SingleOrDefault(x => x.Token == token);

    public IQueryable<TrainingLink> GetByLessonId(string lessonId)
        => FindBy(x => x.LessonId == lessonId);
}
