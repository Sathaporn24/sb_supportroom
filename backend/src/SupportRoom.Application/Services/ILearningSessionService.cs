using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface ILearningSessionService
{
    /// <summary>
    /// "Give me my session on this link, creating one if I have none." Called every time the room
    /// is opened, including after a reconnect - a returning browser sends the LearnerKey it
    /// already has and lands back on its own row rather than starting over.
    /// </summary>
    LearningSessionViewModel Join(string token, JoinLearningSessionDto input);

    /// <summary>Starts a fresh round for someone who already finished. Same person, same browser
    /// key, new row - one learner legitimately has several rounds (CORE_FEATURE_SPEC §2.5).</summary>
    LearningSessionViewModel Restart(string token, JoinLearningSessionDto input);

    /// <summary>
    /// What the join screen asks BEFORE anyone is let into a room: does this browser have a run it
    /// could continue, and did it finish one earlier? Read-only and never creates a row - the
    /// browser's key is not proof of who is holding the laptop, so continuing has to be a choice
    /// the person makes on screen, not something the server does for them.
    /// </summary>
    LearningResumeStateViewModel GetResumeState(string token, string? learnerKey);

    LearningSessionViewModel UpdateProgress(string token, UpdateLearningProgressDto input);
    LearningSessionViewModel End(string token, EndLearningSessionDto input);

    /// <summary>Resolves the entity behind a (token, learnerKey) pair. The recipient-side flows
    /// that need to attribute something to a person - a voice question, a typed question - go
    /// through here rather than accepting a session id from the caller.</summary>
    LearningSession GetEntityByLearnerKey(string token, string learnerKey);

    LearningSessionViewModel GetById(string learningSessionId);

    /// <summary>Everyone who has opened one link. CS-facing.</summary>
    IReadOnlyList<LearningSessionViewModel> GetByTrainingLinkId(string trainingLinkId);

    /// <summary>Computed fresh on every call - there is no summary table (TD-013).</summary>
    SessionSummaryViewModel GetSummary(string learningSessionId);

    /// <summary>Public recap without internal QA fields or the CS follow-up list.</summary>
    LearnerSessionSummaryViewModel GetLearnerSummary(string learningSessionId);
}

public sealed class LearningSessionService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ILearningSessionService> logger)
    : ServiceBase<ILearningSessionService>(unitOfWork, serviceProvider, logger), ILearningSessionService
{
    private readonly ILearningSessionRepository _repository = unitOfWork.GetRepository<ILearningSessionRepository>();
    private readonly ISessionQuestionRepository _questionRepository = unitOfWork.GetRepository<ISessionQuestionRepository>();

    public LearningSessionViewModel Join(string token, JoinLearningSessionDto input)
    {
        // Resolve the company first, then look for an existing run BEFORE enforcing expiry. A
        // learner who started while the link was valid may refresh/reconnect and finish after it
        // expires; only creating a brand-new run is gated by the clock.
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(token);

        var existing = _repository.GetActiveByLearnerKey(link.Id, input.LearnerKey);
        if (existing is not null)
        {
            // Deliberately returns an ENDED session as-is instead of silently opening a new one:
            // reopening a finished link must show the recap and let the learner decide to go again
            // (§2.5). Restart() is the explicit path for that.
            return ToViewModel(existing);
        }

        EnsureLinkNotExpired(link);

        var entity = CreateSession(link.Id, input);
        Logger.LogInformation("Learning session joined: {SessionId} link={LinkId}", entity.Id, link.Id);
        return ToViewModel(entity);
    }

    public LearningSessionViewModel Restart(string token, JoinLearningSessionDto input)
    {
        var link = ResolveLinkForJoin(token);
        var entity = CreateSession(link.Id, input);
        Logger.LogInformation("Learning session restarted: {SessionId} link={LinkId}", entity.Id, link.Id);
        return ToViewModel(entity);
    }

    public LearningResumeStateViewModel GetResumeState(string token, string? learnerKey)
    {
        var linkService = ServiceProvider.GetRequiredService<ITrainingLinkService>();
        // Resolves the company as a side effect, exactly like every other recipient-side entry
        // point. Expiry is deliberately NOT enforced here: someone who started in time still has
        // to be able to see that they have a run waiting and finish it.
        var link = linkService.GetEntityByToken(token);
        var linkExpired = link.ExpiresAt <= DateTime.UtcNow;

        // No key means a browser that has never been here (cleared storage, another device). There
        // is nothing of theirs to look up, and querying on an empty key would match nothing anyway
        // - an empty answer, not an error.
        if (string.IsNullOrWhiteSpace(learnerKey))
        {
            return new LearningResumeStateViewModel
            {
                Link = linkService.GetPublicByToken(token),
                Resumable = null,
                LastEnded = null,
                LinkExpired = linkExpired,
            };
        }

        var resumable = _repository.GetLatestInProgressByLearnerKey(link.Id, learnerKey);
        return new LearningResumeStateViewModel
        {
            Link = linkService.GetPublicByToken(token),
            Resumable = resumable is null ? null : ToViewModel(resumable),
            // Only meaningful when there is nothing to resume - a finished round is what the
            // "เรียนอีกครั้ง"/"ดูสรุป" screen is built from.
            LastEnded = resumable is not null
                ? null
                : _repository.GetLatestEndedByLearnerKey(link.Id, learnerKey) is { } ended
                    ? ToViewModel(ended)
                    : null,
            LinkExpired = linkExpired,
        };
    }

    public LearningSessionViewModel UpdateProgress(string token, UpdateLearningProgressDto input)
    {
        var entity = GetEntityByLearnerKey(token, input.LearnerKey);

        if (entity.Status == SessionStatus.Ended)
        {
            // Not an error. The tutor engine fires progress asynchronously, so a ping sent just
            // before the learner hit "จบ" routinely lands just after it - rejecting it showed the
            // learner a failure for something that worked correctly.
            return ToViewModel(entity);
        }

        entity.LastSlideObjectId = input.LastSlideObjectId ?? entity.LastSlideObjectId;
        entity.LastSlideIndex = input.LastSlideIndex ?? entity.LastSlideIndex;

        // Only ever grows into a real number: a deck that has not resolved yet reports 0/null, and
        // writing that would erase the count taken while the learner was actually on the deck.
        if (input.TotalSlideCount is > 0)
        {
            entity.TotalSlideCount = input.TotalSlideCount;
        }

        // Reaching the last slide is what "went through the whole lesson" means - waiting for the
        // explicit End call marked everyone who simply closed the tab at the end as incomplete.
        // One-way on purpose: once true it stays true, because they did reach the end.
        if (entity.LastSlideIndex is { } index
            && entity.TotalSlideCount is { } total
            && index >= total - 1)
        {
            entity.CompletedAllSlides = true;
        }

        // The whole point of the write: this timestamp is what the stalled calculation reads.
        entity.LastActivityAt = DateTime.UtcNow;
        entity.UpdateDate = DateTime.UtcNow;

        _repository.Update(entity);
        UnitOfWork.Commit();

        return ToViewModel(entity);
    }

    public LearningSessionViewModel End(string token, EndLearningSessionDto input)
    {
        var entity = GetEntityByLearnerKey(token, input.LearnerKey);

        if (entity.Status == SessionStatus.Ended)
        {
            // Idempotent: a retry after the response was lost must not rewrite EndedAt/history.
            return ToViewModel(entity);
        }

        entity.Status = SessionStatus.Ended;
        // OR, never overwrite: a row that already reached the last slide stays complete even if
        // this call reports otherwise (an end fired from a stale runtime, for example).
        entity.CompletedAllSlides = entity.CompletedAllSlides || input.CompletedAllSlides;
        entity.LastSlideObjectId = input.LastSlideObjectId ?? entity.LastSlideObjectId;
        entity.LastSlideIndex = input.LastSlideIndex ?? entity.LastSlideIndex;
        entity.EndedAt = DateTime.UtcNow;
        entity.LastActivityAt = DateTime.UtcNow;
        entity.UpdateDate = DateTime.UtcNow;

        _repository.Update(entity);
        UnitOfWork.Commit();

        Logger.LogInformation(
            "Learning session ended: {SessionId} completedAllSlides={CompletedAllSlides}", entity.Id, entity.CompletedAllSlides);

        return ToViewModel(entity);
    }

    public LearningSession GetEntityByLearnerKey(string token, string learnerKey)
    {
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(token);
        return _repository.GetActiveByLearnerKey(link.Id, learnerKey)
            ?? throw GeneralException.NotFound("การเรียนของผู้ใช้นี้");
    }

    public LearningSessionViewModel GetById(string learningSessionId)
    {
        var entity = _repository.Get(learningSessionId) ?? throw GeneralException.NotFound("การเรียน");
        return ToViewModel(entity);
    }

    public IReadOnlyList<LearningSessionViewModel> GetByTrainingLinkId(string trainingLinkId)
    {
        var sessions = _repository.GetByTrainingLinkId(trainingLinkId)
            .OrderByDescending(x => x.StartedAt)
            .ToList();

        // One grouped count for the whole list - counting per row turns a link with 40 learners
        // into 41 queries.
        var sessionIds = sessions.Select(x => x.Id).ToList();
        var questionCounts = _questionRepository.GetAll()
            .Where(q => sessionIds.Contains(q.SessionId))
            .GroupBy(q => q.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.SessionId, x => x.Count);

        return sessions
            .Select(x => ToViewModel(x, questionCounts.GetValueOrDefault(x.Id)))
            .ToList();
    }

    public SessionSummaryViewModel GetSummary(string learningSessionId)
    {
        var session = _repository.Get(learningSessionId) ?? throw GeneralException.NotFound("การเรียน");
        var questions = _questionRepository.GetBySessionId(learningSessionId).OrderBy(q => q.CreateDate).ToList();

        return new SessionSummaryViewModel
        {
            SessionId = session.Id,
            CompletedAllSlides = session.CompletedAllSlides,
            LastSlideObjectId = session.LastSlideObjectId,
            Questions = questions.Adapt<List<SessionQuestionViewModel>>(),
            UnansweredPoints = questions
                .Where(q => q.AnswerStatus == AnswerStatus.NotFound)
                .Select(q => q.Transcript ?? q.Answer ?? "")
                .Where(text => !string.IsNullOrEmpty(text))
                .ToList(),
            CreatedAt = (session.EndedAt ?? session.StartedAt).Adapt<string>(),
        };
    }

    public LearnerSessionSummaryViewModel GetLearnerSummary(string learningSessionId)
    {
        var summary = GetSummary(learningSessionId);
        return new LearnerSessionSummaryViewModel
        {
            SessionId = summary.SessionId,
            CompletedAllSlides = summary.CompletedAllSlides,
            LastSlideObjectId = summary.LastSlideObjectId,
            Questions = summary.Questions.Select(question => new LearnerSessionQuestionViewModel
            {
                Id = question.Id,
                SessionId = question.SessionId,
                SlideObjectId = question.SlideObjectId,
                Transcript = question.Transcript,
                Answer = question.Answer,
                AnswerStatus = question.AnswerStatus,
                CreatedAt = question.CreatedAt,
            }).ToList(),
            CreatedAt = summary.CreatedAt,
        };
    }

    /// <summary>
    /// Resolves the link and the request's company, then refuses an expired one. Expiry used to be
    /// enforced in the browser only, which meant it was not enforced at all - the explicit join
    /// step is the natural chokepoint for it.
    ///
    /// Only STARTING is gated. Reading back or finishing a session that began while the link was
    /// still valid keeps working after expiry: someone mid-lesson when the clock runs out should
    /// be able to finish and see their recap, and locking them out would look like data loss
    /// rather than an expiry policy.
    /// </summary>
    private TrainingLink ResolveLinkForJoin(string token)
    {
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(token);
        EnsureLinkNotExpired(link);
        return link;
    }

    private static void EnsureLinkNotExpired(TrainingLink link)
    {
        if (link.ExpiresAt <= DateTime.UtcNow)
        {
            throw GeneralException.ValidationError("ลิงก์นี้หมดอายุแล้ว กรุณาติดต่อผู้ดูแลเพื่อขอลิงก์ใหม่");
        }
    }

    private LearningSession CreateSession(string trainingLinkId, JoinLearningSessionDto input)
    {
        var name = input.RecipientName.Trim();
        if (name.Length is 0 or > DtoLimits.RecipientNameMaxLength)
        {
            throw GeneralException.ValidationError($"กรุณากรอกชื่อ (ไม่เกิน {DtoLimits.RecipientNameMaxLength} ตัวอักษร)");
        }

        if (string.IsNullOrWhiteSpace(input.LearnerKey)
            || input.LearnerKey.Length is < DtoLimits.LearnerKeyMinLength or > DtoLimits.LearnerKeyMaxLength)
        {
            throw GeneralException.ValidationError(
                $"รหัสผู้เรียนต้องมี {DtoLimits.LearnerKeyMinLength}-{DtoLimits.LearnerKeyMaxLength} ตัวอักษร");
        }

        var now = DateTime.UtcNow;
        var entity = new LearningSession
        {
            Id = IdGenerator.GenerateId("learning"),
            CompanyId = CurrentCompanyId,
            TrainingLinkId = trainingLinkId,
            LearnerKey = input.LearnerKey,
            RecipientName = name,
            Status = SessionStatus.InProgress,
            StartedAt = now,
            LastActivityAt = now,
            // Left null rather than 0: nobody has reported a slide yet, and 0 is a real position
            // that would make a fresh row look like someone already on the first slide.
            LastSlideIndex = null,
            CompletedAllSlides = false,
            CreateDate = now,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();
        return entity;
    }

    /// <summary>Single-row convenience overload - counts questions itself.</summary>
    private LearningSessionViewModel ToViewModel(LearningSession entity)
        => ToViewModel(entity, _questionRepository.GetBySessionId(entity.Id).Count());

    private static LearningSessionViewModel ToViewModel(LearningSession entity, int questionCount)
    {
        var threshold = TimeSpan.FromMinutes(ServerDefaults.GetInactiveThresholdMinutes());
        return new LearningSessionViewModel
        {
            Id = entity.Id,
            TrainingLinkId = entity.TrainingLinkId,
            RecipientName = entity.RecipientName,
            Status = entity.Status,
            StartedAt = entity.StartedAt.Adapt<string>(),
            EndedAt = entity.EndedAt.Adapt<string>(),
            LastActivityAt = entity.LastActivityAt.Adapt<string>(),
            LastSlideObjectId = entity.LastSlideObjectId,
            LastSlideIndex = entity.LastSlideIndex,
            TotalSlideCount = entity.TotalSlideCount,
            CompletedAllSlides = entity.CompletedAllSlides,
            // A finished session is never "stalled" no matter how long ago it ended - stalling
            // describes someone who walked away mid-lesson, not someone who got to the end.
            IsStalled = entity.Status == SessionStatus.InProgress
                && DateTime.UtcNow - entity.LastActivityAt > threshold,
            QuestionCount = questionCount,
        };
    }
}
