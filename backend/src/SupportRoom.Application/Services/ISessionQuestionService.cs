using Mapster;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface ISessionQuestionService
{
    SessionQuestionViewModel Create(string sessionId, CreateSessionQuestionDto input);
    IReadOnlyList<SessionQuestionViewModel> GetBySessionId(string sessionId);
}

public sealed class SessionQuestionService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ISessionQuestionService> logger)
    : ServiceBase<ISessionQuestionService>(unitOfWork, serviceProvider, logger), ISessionQuestionService
{
    private readonly ISessionQuestionRepository _repository = unitOfWork.GetRepository<ISessionQuestionRepository>();
    private readonly ITrainingSessionRepository _sessionRepository = unitOfWork.GetRepository<ITrainingSessionRepository>();

    public SessionQuestionViewModel Create(string sessionId, CreateSessionQuestionDto input)
    {
        _ = _sessionRepository.Get(sessionId) ?? throw GeneralException.NotFound("เซสชัน");

        var entity = new SessionQuestion
        {
            Id = IdGenerator.GenerateId("question"),
            SessionId = sessionId,
            SlideObjectId = input.SlideObjectId,
            Transcript = input.Transcript,
            Answer = input.Answer,
            AnswerStatus = input.AnswerStatus,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        // Never log Transcript/Answer - only the outcome, so CS can grep "not_found" volume
        // without exposing what teachers actually asked.
        Logger.LogInformation(
            "Question recorded: {SessionId} slide={SlideObjectId} status={AnswerStatus}",
            sessionId, input.SlideObjectId, input.AnswerStatus);

        return entity.Adapt<SessionQuestionViewModel>();
    }

    public IReadOnlyList<SessionQuestionViewModel> GetBySessionId(string sessionId)
        => _repository.GetBySessionId(sessionId).OrderBy(x => x.CreateDate).ToList().Adapt<List<SessionQuestionViewModel>>();
}
