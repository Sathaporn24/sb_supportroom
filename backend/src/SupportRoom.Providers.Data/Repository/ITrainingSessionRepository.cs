using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ITrainingSessionRepository : IRepositoryBase<TrainingSession, string>
{
    /// <summary>
    /// The one lookup that deliberately bypasses the company query filter. It is the entry point
    /// for every recipient-side request: the caller holds only a join token and no company has
    /// been resolved yet, so filtering here would match zero rows and no session could ever be
    /// opened. The token is itself the credential (unguessable, globally unique), and callers
    /// must resolve ICompanyContext from the returned session before touching anything else -
    /// see ITrainingSessionService.GetByToken.
    /// </summary>
    TrainingSession? GetByToken(string token);
}

public sealed class TrainingSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<TrainingSession, string>(dbContext), ITrainingSessionRepository
{
    public TrainingSession? GetByToken(string token)
        => Context.TrainingSession.IgnoreQueryFilters().SingleOrDefault(x => x.Token == token);
}
