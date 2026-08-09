using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ITrainingSessionRepository : IRepositoryBase<TrainingSession, string>
{
    TrainingSession? GetByToken(string token);
}

public sealed class TrainingSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<TrainingSession, string>(dbContext), ITrainingSessionRepository
{
    public TrainingSession? GetByToken(string token)
        => FindBy(x => x.Token == token).SingleOrDefault();
}
