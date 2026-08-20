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

    /// <summary>Anonymous pre-join shape; excludes ids, lesson slug and attendance counts.</summary>
    PublicTrainingLinkViewModel GetPublicByToken(string token);

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

        // One grouped aggregate query instead of separate counts per link - the admin list renders
        // every link at once. LearnerCount is distinct browsers; status counts are learning rounds.
        var aggregateRows = _learningSessionRepository.GetAll()
            .GroupBy(x => x.TrainingLinkId)
            .Select(g => new
            {
                TrainingLinkId = g.Key,
                LearnerCount = g.Select(x => x.LearnerKey).Distinct().Count(),
                InProgressCount = g.Count(x => x.Status == SessionStatus.InProgress),
                EndedCount = g.Count(x => x.Status == SessionStatus.Ended),
            })
            .ToList();
        var aggregatesByLinkId = aggregateRows.ToDictionary(
            x => x.TrainingLinkId,
            x => new SessionAggregates(x.LearnerCount, x.InProgressCount, x.EndedCount));

        return links
            .Select(link => ToViewModel(link, aggregatesByLinkId.GetValueOrDefault(link.Id)))
            .ToList();
    }

    public TrainingLinkViewModel Create(CreateTrainingLinkDto input)
    {
        if (input.MaxAttendees is < 1)
        {
            throw GeneralException.ValidationError("จำนวนคนสูงสุดต้องมากกว่าหรือเท่ากับ 1");
        }

        var lesson = _lessonConfigRepository.GetBySlug(input.LessonSlug) ?? throw GeneralException.NotFound("บทเรียน");
        if (!lesson.IsActive)
        {
            throw GeneralException.ValidationError("ต้องเปิดใช้งานบทเรียนก่อนสร้างลิงก์");
        }

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
            // CS created this link. Null would mean the learner surface made it, which cannot
            // happen here - links are only ever created from the back office.
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Training link created: {LinkId} lesson={LessonSlug}", entity.Id, entity.LessonSlug);

        return ToViewModel(entity, null);
    }

    public TrainingLinkViewModel GetById(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("ลิงก์");
        return ToViewModel(entity, GetAggregates(id));
    }

    public TrainingLinkViewModel GetByToken(string token)
    {
        var entity = GetEntityByToken(token);
        return ToViewModel(entity, GetAggregates(entity.Id));
    }

    public PublicTrainingLinkViewModel GetPublicByToken(string token)
    {
        var entity = GetEntityByToken(token);
        return new PublicTrainingLinkViewModel
        {
            Token = entity.Token,
            RecipientOrgName = entity.RecipientOrgName,
            ExpiresAt = entity.ExpiresAt.Adapt<string>(),
            Status = entity.ExpiresAt <= DateTime.UtcNow ? LinkStatus.Expired : LinkStatus.Active,
        };
    }

    /// <summary>
    /// The single doorway for every recipient-side request. The caller holds only a join token
    /// and no company has been resolved yet, so the lookup itself must bypass the company query
    /// filter (see ITrainingLinkRepository.GetByToken) - the token is unguessable and globally
    /// unique, which makes it safe for locating the public link. Learner-owned data requires the
    /// second credential half (LearnerKey) as well.
    ///
    /// Resolving ICompanyContext from the row found here is what scopes every FOLLOWING query in
    /// the request (lesson, learning sessions, questions, chat, documents) to the right company.
    /// Skip this step and those queries run against whatever company the middleware guessed
    /// instead - which is exactly the cross-company read this design exists to prevent.
    /// </summary>
    public TrainingLink GetEntityByToken(string token)
    {
        var entity = _repository.GetByToken(token) ?? throw GeneralException.NotFound("ลิงก์ หรือลิงก์หมดอายุ");
        // A token is the link-level half of the learner credential, but it must not be able to
        // replace a tenant already selected for a signed-in request. Public learner requests start unresolved;
        // admin requests arrive resolved by middleware and a mismatch fails before any scoped
        // query can run under the token's company.
        if (CompanyContext.CompanyId is { Length: > 0 } selectedCompanyId
            && !string.Equals(selectedCompanyId, entity.CompanyId, StringComparison.Ordinal))
        {
            throw GeneralException.Forbidden("ลิงก์นี้ไม่ได้อยู่ในบริษัทที่กำลังดู");
        }
        CompanyContext.Resolve(entity.CompanyId);
        return entity;
    }

    private SessionAggregates? GetAggregates(string trainingLinkId)
    {
        var sessions = _learningSessionRepository.GetByTrainingLinkId(trainingLinkId).ToList();
        return sessions.Count == 0
            ? null
            : new SessionAggregates(
                sessions.Select(x => x.LearnerKey).Distinct().Count(),
                sessions.Count(x => x.Status == SessionStatus.InProgress),
                sessions.Count(x => x.Status == SessionStatus.Ended));
    }

    private static TrainingLinkViewModel ToViewModel(TrainingLink entity, SessionAggregates? aggregates) => new()
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
        LearningSessionCount = (aggregates?.InProgressCount ?? 0) + (aggregates?.EndedCount ?? 0),
        LearnerCount = aggregates?.LearnerCount ?? 0,
        InProgressCount = aggregates?.InProgressCount ?? 0,
        EndedCount = aggregates?.EndedCount ?? 0,
    };

    private sealed record SessionAggregates(int LearnerCount, int InProgressCount, int EndedCount);
}
