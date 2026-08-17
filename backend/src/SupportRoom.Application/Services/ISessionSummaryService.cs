using Mapster;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface ISessionSummaryService
{
    /// <summary>Adds the summary to the current UnitOfWork - caller commits (see ITrainingSessionService.End).</summary>
    void Save(string sessionId, bool completedAllSlides, string? lastSlideObjectId);
    SessionSummaryViewModel? GetBySessionId(string sessionId);
}

public sealed class SessionSummaryService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ISessionSummaryService> logger)
    : ServiceBase<ISessionSummaryService>(unitOfWork, serviceProvider, logger), ISessionSummaryService
{
    private readonly ISessionSummaryRepository _repository = unitOfWork.GetRepository<ISessionSummaryRepository>();
    private readonly ISessionQuestionRepository _questionRepository = unitOfWork.GetRepository<ISessionQuestionRepository>();

    public void Save(string sessionId, bool completedAllSlides, string? lastSlideObjectId)
    {
        var questions = _questionRepository.GetBySessionId(sessionId).OrderBy(q => q.CreateDate).ToList();
        var unansweredPoints = questions
            .Where(q => q.AnswerStatus == AnswerStatus.NotFound)
            .Select(q => q.Transcript ?? q.Answer ?? "")
            .Where(text => !string.IsNullOrEmpty(text))
            .ToList();

        var existing = _repository.GetBySessionId(sessionId);
        if (existing is not null)
        {
            existing.CompletedAllSlides = completedAllSlides;
            existing.LastSlideObjectId = lastSlideObjectId;
            existing.UnansweredPoints = unansweredPoints;
            existing.UpdateDate = DateTime.UtcNow;
            _repository.Update(existing);
            Logger.LogInformation(
                "Session summary updated: {SessionId} unansweredPoints={UnansweredCount}", sessionId, unansweredPoints.Count);
            return;
        }

        _repository.Add(new SessionSummary
        {
            Id = IdGenerator.GenerateId("summary"),
            CompanyId = CurrentCompanyId,
            SessionId = sessionId,
            CompletedAllSlides = completedAllSlides,
            LastSlideObjectId = lastSlideObjectId,
            UnansweredPoints = unansweredPoints,
            CreateDate = DateTime.UtcNow,
        });
        Logger.LogInformation(
            "Session summary created: {SessionId} unansweredPoints={UnansweredCount}", sessionId, unansweredPoints.Count);
    }

    public SessionSummaryViewModel? GetBySessionId(string sessionId)
    {
        var summary = _repository.GetBySessionId(sessionId);
        if (summary is null)
        {
            return null;
        }

        var questions = _questionRepository.GetBySessionId(sessionId).OrderBy(q => q.CreateDate).ToList();
        return new SessionSummaryViewModel
        {
            SessionId = summary.SessionId,
            CompletedAllSlides = summary.CompletedAllSlides,
            LastSlideObjectId = summary.LastSlideObjectId,
            Questions = questions.Adapt<List<SessionQuestionViewModel>>(),
            UnansweredPoints = summary.UnansweredPoints,
            CreatedAt = summary.CreateDate.Adapt<string>(),
        };
    }
}
