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

public interface ITrainingSessionService
{
    IReadOnlyList<TrainingSessionViewModel> GetAll();
    TrainingSessionViewModel Create(CreateSessionDto input);
    TrainingSessionViewModel GetById(string id);
    TrainingSessionViewModel GetByToken(string token);
    TrainingSessionViewModel MarkStarted(string token);
    TrainingSessionViewModel End(string token, EndSessionDto input);
}

public sealed class TrainingSessionService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ITrainingSessionService> logger)
    : ServiceBase<ITrainingSessionService>(unitOfWork, serviceProvider, logger), ITrainingSessionService
{
    private readonly ITrainingSessionRepository _repository = unitOfWork.GetRepository<ITrainingSessionRepository>();
    private readonly ILessonConfigRepository _lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();

    public IReadOnlyList<TrainingSessionViewModel> GetAll()
        => _repository.GetAll().OrderByDescending(x => x.CreateDate).ToList().Adapt<List<TrainingSessionViewModel>>();

    public TrainingSessionViewModel Create(CreateSessionDto input)
    {
        var lesson = _lessonConfigRepository.GetBySlug(input.LessonSlug) ?? throw GeneralException.NotFound("บทเรียน");

        var expiresAt = input.ExpiresAt is { Length: > 0 }
            ? input.ExpiresAt.Adapt<DateTime>()
            : DateTime.UtcNow.AddHours(ServerDefaults.GetDefaultSessionExpiryHours());

        var entity = new TrainingSession
        {
            Id = IdGenerator.GenerateId("session"),
            CompanyId = CurrentCompanyId,
            Token = IdGenerator.GeneratePublicToken(),
            LessonId = lesson.Id,
            LessonSlug = input.LessonSlug,
            RecipientName = input.RecipientName,
            RecipientOrgName = input.RecipientOrgName,
            Status = SessionStatus.NotStarted,
            ExpiresAt = expiresAt,
            CompletedAllSlides = false,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Session created: {SessionId} lesson={LessonSlug}", entity.Id, entity.LessonSlug);

        return entity.Adapt<TrainingSessionViewModel>();
    }

    public TrainingSessionViewModel GetById(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("เซสชัน");
        return entity.Adapt<TrainingSessionViewModel>();
    }

    public TrainingSessionViewModel GetByToken(string token)
    {
        var entity = LoadByTokenAndResolveCompany(token) ?? throw GeneralException.NotFound("เซสชัน หรือลิงก์หมดอายุ");
        return entity.Adapt<TrainingSessionViewModel>();
    }

    /// <summary>
    /// The single doorway for every recipient-side request. The caller holds only a join token
    /// and no company has been resolved yet, so the lookup itself must bypass the company query
    /// filter (see ITrainingSessionRepository.GetByToken) - the token is unguessable and globally
    /// unique, which is what makes it usable as the credential.
    ///
    /// Resolving ICompanyContext from the row found here is what scopes every FOLLOWING query in
    /// the request (lesson, questions, chat, documents) to the right company. Skip this step and
    /// those queries run against whatever company the middleware guessed instead - which is
    /// exactly the cross-company read this design exists to prevent.
    /// </summary>
    private TrainingSession? LoadByTokenAndResolveCompany(string token)
    {
        var entity = _repository.GetByToken(token);
        if (entity is not null)
        {
            CompanyContext.Resolve(entity.CompanyId);
        }
        return entity;
    }

    public TrainingSessionViewModel MarkStarted(string token)
    {
        var entity = LoadByTokenAndResolveCompany(token) ?? throw GeneralException.NotFound("เซสชัน");

        entity.Status = SessionStatus.InProgress;
        entity.StartedAt = DateTime.UtcNow;
        entity.UpdateDate = DateTime.UtcNow;

        _repository.Update(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Session started: {SessionId}", entity.Id);

        return entity.Adapt<TrainingSessionViewModel>();
    }

    public TrainingSessionViewModel End(string token, EndSessionDto input)
    {
        var entity = LoadByTokenAndResolveCompany(token) ?? throw GeneralException.NotFound("เซสชัน");

        entity.Status = SessionStatus.Ended;
        entity.CompletedAllSlides = input.CompletedAllSlides;
        entity.LastSlideObjectId = input.LastSlideObjectId;
        entity.EndedAt = DateTime.UtcNow;
        entity.UpdateDate = DateTime.UtcNow;

        _repository.Update(entity);

        var summaryService = ServiceProvider.GetRequiredService<ISessionSummaryService>();
        summaryService.Save(entity.Id, input.CompletedAllSlides, input.LastSlideObjectId);

        UnitOfWork.Commit();

        Logger.LogInformation(
            "Session ended: {SessionId} completedAllSlides={CompletedAllSlides}", entity.Id, entity.CompletedAllSlides);

        return entity.Adapt<TrainingSessionViewModel>();
    }
}
