using Mapster;
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

public interface ITrainingLinkService
{
    IReadOnlyList<TrainingLinkViewModel> GetAll();
    TrainingLinkViewModel Create(CreateTrainingLinkDto input);
    TrainingLinkViewModel GetById(string id);

    /// <summary>
    /// Resolves the link AND the request's company. Every recipient-side flow starts here.
    /// </summary>
    TrainingLinkViewModel GetByToken(string token);

    /// <summary>Entity-level variant for callers that need the row itself (the learning-session
    /// service needs the id and expiry, not a wire model). Resolves company identically.</summary>
    TrainingLink GetEntityByToken(string token);
}

public sealed class TrainingLinkService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ITrainingLinkService> logger)
    : ServiceBase<ITrainingLinkService>(unitOfWork, serviceProvider, logger), ITrainingLinkService
{
    private readonly ITrainingLinkRepository _repository = unitOfWork.GetRepository<ITrainingLinkRepository>();
    private readonly ILearningSessionRepository _learningSessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();
    private readonly ILessonConfigRepository _lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();

    public IReadOnlyList<TrainingLinkViewModel> GetAll()
    {
        var links = _repository.GetAll().OrderByDescending(x => x.CreateDate).ToList();

        // One grouped count instead of a count per link - the admin list is the only caller and it
        // renders every link at once.
        var countsByLinkId = _learningSessionRepository.GetAll()
            .GroupBy(x => x.TrainingLinkId)
            .Select(g => new { TrainingLinkId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.TrainingLinkId, x => x.Count);

        return links
            .Select(link => ToViewModel(link, countsByLinkId.GetValueOrDefault(link.Id)))
            .ToList();
    }

    public TrainingLinkViewModel Create(CreateTrainingLinkDto input)
    {
        var lesson = _lessonConfigRepository.GetBySlug(input.LessonSlug) ?? throw GeneralException.NotFound("บทเรียน");

        var expiresAt = input.ExpiresAt is { Length: > 0 }
            ? input.ExpiresAt.Adapt<DateTime>()
            : DateTime.UtcNow.AddHours(ServerDefaults.GetDefaultSessionExpiryHours());

        var entity = new TrainingLink
        {
            Id = IdGenerator.GenerateId("link"),
            CompanyId = CurrentCompanyId,
            Token = IdGenerator.GeneratePublicToken(),
            LessonId = lesson.Id,
            LessonSlug = input.LessonSlug,
            RecipientOrgName = input.RecipientOrgName,
            ExpiresAt = expiresAt,
            MaxAttendees = input.MaxAttendees,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Training link created: {LinkId} lesson={LessonSlug}", entity.Id, entity.LessonSlug);

        return ToViewModel(entity, 0);
    }

    public TrainingLinkViewModel GetById(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("ลิงก์");
        return ToViewModel(entity, _learningSessionRepository.GetByTrainingLinkId(id).Count());
    }

    public TrainingLinkViewModel GetByToken(string token)
    {
        var entity = GetEntityByToken(token);
        return ToViewModel(entity, _learningSessionRepository.GetByTrainingLinkId(entity.Id).Count());
    }

    /// <summary>
    /// The single doorway for every recipient-side request. The caller holds only a join token
    /// and no company has been resolved yet, so the lookup itself must bypass the company query
    /// filter (see ITrainingLinkRepository.GetByToken) - the token is unguessable and globally
    /// unique, which is what makes it usable as the credential.
    ///
    /// Resolving ICompanyContext from the row found here is what scopes every FOLLOWING query in
    /// the request (lesson, learning sessions, questions, chat, documents) to the right company.
    /// Skip this step and those queries run against whatever company the middleware guessed
    /// instead - which is exactly the cross-company read this design exists to prevent.
    /// </summary>
    public TrainingLink GetEntityByToken(string token)
    {
        var entity = _repository.GetByToken(token) ?? throw GeneralException.NotFound("ลิงก์ หรือลิงก์หมดอายุ");
        CompanyContext.Resolve(entity.CompanyId);
        return entity;
    }

    private static TrainingLinkViewModel ToViewModel(TrainingLink entity, int learningSessionCount) => new()
    {
        Id = entity.Id,
        Token = entity.Token,
        LessonId = entity.LessonId,
        LessonSlug = entity.LessonSlug,
        RecipientOrgName = entity.RecipientOrgName,
        CreatedAt = entity.CreateDate.Adapt<string>(),
        ExpiresAt = entity.ExpiresAt.Adapt<string>(),
        MaxAttendees = entity.MaxAttendees,
        Status = entity.ExpiresAt <= DateTime.UtcNow ? LinkStatus.Expired : LinkStatus.Active,
        LearningSessionCount = learningSessionCount,
    };
}
