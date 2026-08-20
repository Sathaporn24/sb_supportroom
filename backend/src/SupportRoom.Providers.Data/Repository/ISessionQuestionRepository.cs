using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ISessionQuestionRepository : IRepositoryBase<SessionQuestion, string>
{
    IQueryable<SessionQuestion> GetBySessionId(string sessionId);

    /// <summary>
    /// QQ-1 (first half) - review-queue candidates: AnswerStatus == NotFound OR ReviewResult ==
    /// Incorrect. OutOfScope/NoSpeech/TranscriptionFailed never match either condition, so they
    /// never appear here - that filtering already happened when the question was answered
    /// (design.md's Q6 resolution), this method adds no extra logic for it.
    ///
    /// QQ-1's second half (excluding questions a KnowledgeQnASource already points at) is
    /// deliberately NOT applied here - it needs one batched cross-table lookup
    /// (IKnowledgeQnASourceRepository.GetBySessionQuestionIds), which the caller (the queue
    /// service) does once against the candidate ids this method returns, the same "join at the
    /// service layer with repositories this project already has" shape VoiceQuestionService uses
    /// elsewhere - rather than this repository reaching into another module-adjacent table itself.
    /// </summary>
    IQueryable<SessionQuestion> GetReviewQueue();
}

public sealed class SessionQuestionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SessionQuestion, string>(dbContext), ISessionQuestionRepository
{
    public IQueryable<SessionQuestion> GetBySessionId(string sessionId)
        => FindBy(x => x.SessionId == sessionId);

    public IQueryable<SessionQuestion> GetReviewQueue()
        => FindBy(x => x.AnswerStatus == AnswerStatus.NotFound || x.ReviewResult == ReviewResult.Incorrect);
}
