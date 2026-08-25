using Microsoft.Extensions.DependencyInjection;
using Mapster;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface ISessionQuestionService
{
    /// <summary>learningSessionId, not a link id - a question belongs to the person who asked it.</summary>
    SessionQuestionViewModel Create(string learningSessionId, CreateSessionQuestionDto input);

    /// <summary>The learner's own questions. Keyed on (token, learnerKey) so one learner can only
    /// ever read back their own - two people on the same link must not see each other's
    /// (CORE_FEATURE_SPEC §2.4).</summary>
    IReadOnlyList<LearnerSessionQuestionViewModel> GetForLearner(string token, string learnerKey);

    /// <summary>CS-facing: every question in one learning session, review fields included.</summary>
    IReadOnlyList<SessionQuestionViewModel> GetByLearningSessionId(string learningSessionId);

    SessionQuestionViewModel Review(string questionId, ReviewSessionQuestionDto input);
}

public sealed class SessionQuestionService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ISessionQuestionService> logger)
    : ServiceBase<ISessionQuestionService>(unitOfWork, serviceProvider, logger), ISessionQuestionService
{
    private readonly ISessionQuestionRepository _repository = unitOfWork.GetRepository<ISessionQuestionRepository>();
    private readonly ILearningSessionRepository _learningSessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();

    public SessionQuestionViewModel Create(string learningSessionId, CreateSessionQuestionDto input)
    {
        _ = _learningSessionRepository.Get(learningSessionId) ?? throw GeneralException.NotFound("การเรียน");

        var entity = new SessionQuestion
        {
            Id = IdGenerator.GenerateId("question"),
            CompanyId = CurrentCompanyId,
            SessionId = learningSessionId,
            SlideObjectId = input.SlideObjectId,
            Transcript = input.Transcript,
            Answer = input.Answer,
            AnswerStatus = input.AnswerStatus,
            Source = input.Source,
            // Stays null in practice: a question is only ever written from an anonymous learner
            // endpoint (POST /api/voice-question or /api/text-question). Kept anyway - if that ever
            // changes, the row records who without anyone remembering to add it.
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        // Never log Transcript/Answer - only the outcome, so CS can grep "not_found" volume
        // without exposing what learners actually asked.
        Logger.LogInformation(
            "Question recorded: {SessionId} slide={SlideObjectId} status={AnswerStatus}",
            learningSessionId, input.SlideObjectId, input.AnswerStatus);

        return entity.Adapt<SessionQuestionViewModel>();
    }

    public IReadOnlyList<LearnerSessionQuestionViewModel> GetForLearner(string token, string learnerKey)
    {
        var session = ServiceProvider.GetRequiredService<ILearningSessionService>().GetEntityByLearnerKey(token, learnerKey);
        return _repository.GetBySessionId(session.Id)
            .OrderBy(x => x.CreateDate)
            .Select(question => new LearnerSessionQuestionViewModel
            {
                Id = question.Id,
                SessionId = question.SessionId,
                SlideObjectId = question.SlideObjectId,
                Transcript = question.Transcript,
                Answer = question.Answer,
                AnswerStatus = question.AnswerStatus,
                CreatedAt = question.CreateDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    public IReadOnlyList<SessionQuestionViewModel> GetByLearningSessionId(string learningSessionId)
        => _repository.GetBySessionId(learningSessionId)
            .OrderBy(x => x.CreateDate)
            .ToList()
            .Adapt<List<SessionQuestionViewModel>>();

    public SessionQuestionViewModel Review(string questionId, ReviewSessionQuestionDto input)
    {
        if (input.ReviewResult is not null && !ReviewResult.IsValid(input.ReviewResult))
        {
            throw GeneralException.ValidationError("ผลรีวิวไม่ถูกต้อง");
        }

        var reviewNote = string.IsNullOrWhiteSpace(input.ReviewNote) ? null : input.ReviewNote.Trim();
        if (reviewNote?.Length > DtoLimits.ReviewNoteMaxLength)
        {
            throw GeneralException.ValidationError($"หมายเหตุรีวิวต้องไม่เกิน {DtoLimits.ReviewNoteMaxLength} ตัวอักษร");
        }

        var entity = _repository.Get(questionId) ?? throw GeneralException.NotFound("คำถาม");

        entity.ReviewResult = input.ReviewResult;
        entity.ReviewNote = input.ReviewResult is null ? null : reviewNote;
        entity.ReviewedAt = input.ReviewResult is null ? null : DateTime.UtcNow;
        // The one place a back-office user edits a row the learner created. Without this there is
        // no way to answer "who reviewed this?" afterwards, and the answer cannot be reconstructed
        // later from anything else - ReviewedAt only says when.
        entity.UpdateBy = CurrentUserId;
        entity.UpdateDate = DateTime.UtcNow;

        _repository.Update(entity);
        UnitOfWork.Commit();

        // The note itself stays out of the log - it quotes the answer it is about.
        Logger.LogInformation(
            "Question reviewed: {QuestionId} result={ReviewResult} hasNote={HasNote} by={ActorId}",
            questionId, entity.ReviewResult, entity.ReviewNote is not null, CurrentUserId);

        return entity.Adapt<SessionQuestionViewModel>();
    }
}
