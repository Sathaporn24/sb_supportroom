using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ILearningSessionRepository : IRepositoryBase<LearningSession, string>
{
    /// <summary>
    /// Resolves "the person behind this browser, on this link". Scoped by link as well as key so
    /// one learner opening two different links gets two sessions, which is what the reused
    /// browser key would otherwise collapse into one.
    ///
    /// Returns the newest match: pressing "เรียนอีกครั้ง" starts a fresh round under the same
    /// LearnerKey, and the caller always wants the current round, not the first one.
    /// </summary>
    LearningSession? GetActiveByLearnerKey(string trainingLinkId, string learnerKey);

    IQueryable<LearningSession> GetByTrainingLinkId(string trainingLinkId);
}

public sealed class LearningSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<LearningSession, string>(dbContext), ILearningSessionRepository
{
    public LearningSession? GetActiveByLearnerKey(string trainingLinkId, string learnerKey)
        => FindBy(x => x.TrainingLinkId == trainingLinkId && x.LearnerKey == learnerKey)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault();

    public IQueryable<LearningSession> GetByTrainingLinkId(string trainingLinkId)
        => FindBy(x => x.TrainingLinkId == trainingLinkId);
}
