using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ISessionQuestionReviewExclusionRepository : IRepositoryBase<SessionQuestionReviewExclusion, string>
{
    /// <summary>QQ-1's permanent-suppress check (LT-16) - one batched lookup for the whole review
    /// queue, never one call per candidate row, same shape as
    /// IKnowledgeQnASourceRepository.GetBySessionQuestionIds.</summary>
    IQueryable<SessionQuestionReviewExclusion> GetBySessionQuestionIds(IReadOnlyList<string> sessionQuestionIds);

    /// <summary>LT-16 - inserts an exclusion row for every id in sessionQuestionIds that does not
    /// already have one, for this lesson. Idempotent by construction: the unique
    /// (CompanyId, SessionQuestionId) index makes a retried purge attempt's re-run of this a no-op
    /// for ids it already inserted, and the pre-check here avoids ever attempting the duplicate
    /// insert in the first place. Returns the number of rows actually added.</summary>
    int AddMissingForLesson(string companyId, string lessonId, IReadOnlyList<string> sessionQuestionIds, string? actorUserId);
}

public sealed class SessionQuestionReviewExclusionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SessionQuestionReviewExclusion, string>(dbContext), ISessionQuestionReviewExclusionRepository
{
    public IQueryable<SessionQuestionReviewExclusion> GetBySessionQuestionIds(IReadOnlyList<string> sessionQuestionIds)
        => FindBy(x => sessionQuestionIds.Contains(x.SessionQuestionId));

    public int AddMissingForLesson(string companyId, string lessonId, IReadOnlyList<string> sessionQuestionIds, string? actorUserId)
    {
        if (sessionQuestionIds.Count == 0)
        {
            return 0;
        }

        var distinctIds = sessionQuestionIds.Distinct().ToList();
        var alreadyExcluded = GetBySessionQuestionIds(distinctIds).Select(x => x.SessionQuestionId).ToHashSet();
        var now = DateTime.UtcNow;
        var added = 0;

        foreach (var sessionQuestionId in distinctIds)
        {
            if (alreadyExcluded.Contains(sessionQuestionId))
            {
                continue;
            }

            Add(new SessionQuestionReviewExclusion
            {
                Id = IdGenerator.GenerateId("qex"),
                CompanyId = companyId,
                CreateBy = actorUserId,
                CreateDate = now,
                SessionQuestionId = sessionQuestionId,
                LessonId = lessonId,
                Reason = QuestionReviewExclusionReason.LessonPermanentlyDeleted,
            });
            added++;
        }

        return added;
    }
}
