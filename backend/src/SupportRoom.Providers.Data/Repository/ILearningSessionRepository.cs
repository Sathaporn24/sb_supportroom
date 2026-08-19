using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
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

    /// <summary>The run this browser could still continue: IN_PROGRESS only, newest first. What
    /// the join screen asks about before letting anyone back into a lesson.</summary>
    LearningSession? GetLatestInProgressByLearnerKey(string trainingLinkId, string learnerKey);

    /// <summary>The most recently finished run for this browser - drives "คุณเรียนจบแล้ว" plus
    /// the recap and "เรียนอีกครั้ง" buttons.</summary>
    LearningSession? GetLatestEndedByLearnerKey(string trainingLinkId, string learnerKey);

    IQueryable<LearningSession> GetByTrainingLinkId(string trainingLinkId);
}

public sealed class LearningSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<LearningSession, string>(dbContext), ILearningSessionRepository
{
    public LearningSession? GetActiveByLearnerKey(string trainingLinkId, string learnerKey)
        => FindBy(x => x.TrainingLinkId == trainingLinkId && x.LearnerKey == learnerKey)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault();

    public LearningSession? GetLatestInProgressByLearnerKey(string trainingLinkId, string learnerKey)
        => FindBy(x => x.TrainingLinkId == trainingLinkId
                && x.LearnerKey == learnerKey
                && x.Status == SessionStatus.InProgress)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefault();

    public LearningSession? GetLatestEndedByLearnerKey(string trainingLinkId, string learnerKey)
        => FindBy(x => x.TrainingLinkId == trainingLinkId
                && x.LearnerKey == learnerKey
                && x.Status == SessionStatus.Ended)
            .OrderByDescending(x => x.EndedAt)
            .FirstOrDefault();

    public IQueryable<LearningSession> GetByTrainingLinkId(string trainingLinkId)
        => FindBy(x => x.TrainingLinkId == trainingLinkId);
}
