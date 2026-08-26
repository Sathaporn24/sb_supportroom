using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>
/// R9/LT-5/LT-6 - the public link policy split: a revoked TrainingLink must reject a brand new
/// join/restart outright, but must not cut off a learner already mid-session on it.
/// </summary>
public class RevokedLinkPolicyTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _sessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TrainingLinkService _trainingLinkService;
    private readonly LearningSessionService _learningSessionService;

    public RevokedLinkPolicyTests()
    {
        // FakeServiceProvider pre-resolves ICompanyContext to TestFixtures.CompanyId, matching
        // every seeded row's CompanyId below.
        var serviceProvider = new FakeServiceProvider();

        _unitOfWork.Register<ITrainingLinkRepository>(_links);
        _unitOfWork.Register<ILearningSessionRepository>(_sessions);
        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<ISessionQuestionRepository>(new FakeSessionQuestionRepository());

        _trainingLinkService = new TrainingLinkService(_unitOfWork, serviceProvider, NullLogger<ITrainingLinkService>.Instance);
        _learningSessionService = new LearningSessionService(_unitOfWork, serviceProvider, NullLogger<ILearningSessionService>.Instance);

        serviceProvider.Register<ITrainingLinkService>(_trainingLinkService);
        serviceProvider.Register<ILearningSessionService>(_learningSessionService);
    }

    private TrainingLink SeedLink(bool revoked, string token = "tok-1", string id = "link-1")
    {
        var link = new TrainingLink
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            Token = token,
            LessonId = "lesson-1",
            LessonSlug = "lesson-1-slug",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsDelete = revoked,
            DeletedAt = revoked ? DateTime.UtcNow : null,
        };
        _links.Items.Add(link);
        return link;
    }

    // ---- GetEntityByTokenForContentAccess (LT-5/LT-6) --------------------------------------

    [Fact]
    public void ContentAccess_ActiveLink_AlwaysPasses_RegardlessOfLearnerKey()
    {
        SeedLink(revoked: false);

        var link = _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", null);

        Assert.NotNull(link);
    }

    [Fact]
    public void ContentAccess_RevokedLink_NoLearnerKey_ThrowsNotFound()
    {
        SeedLink(revoked: true);

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", null));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void ContentAccess_RevokedLink_LearnerKeyWithNoSession_ThrowsNotFound()
    {
        SeedLink(revoked: true);

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", "learner-abc"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void ContentAccess_RevokedLink_EndedSession_ThrowsNotFound()
    {
        var link = SeedLink(revoked: true);
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = link.Id,
            LearnerKey = "learner-abc",
            RecipientName = "Somchai",
            Status = SessionStatus.Ended,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", "learner-abc"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void ContentAccess_RevokedLink_WrongLearnerKey_ThrowsNotFound()
    {
        var link = SeedLink(revoked: true);
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = link.Id,
            LearnerKey = "learner-abc",
            RecipientName = "Somchai",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        // Someone else's learnerKey must not ride along on this revoked link's IN_PROGRESS session.
        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", "learner-xyz"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void ContentAccess_RevokedLink_MatchingInProgressSession_Succeeds()
    {
        var link = SeedLink(revoked: true);
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = link.Id,
            LearnerKey = "learner-abc",
            RecipientName = "Somchai",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        var resolved = _trainingLinkService.GetEntityByTokenForContentAccess("tok-1", "learner-abc");

        Assert.Equal(link.Id, resolved.Id);
    }

    // ---- Join()/Restart() - new session vs resume (LT-5) -----------------------------------

    [Fact]
    public void Join_RevokedLink_NoExistingSession_IsRejected()
    {
        SeedLink(revoked: true);

        var ex = Assert.Throws<HttpStatusCodeException>(() => _learningSessionService.Join(
            "tok-1", new JoinLearningSessionDto { LearnerKey = "learner-new-visitor", RecipientName = "Somchai" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void Join_RevokedLink_ExistingInProgressSession_StillReturnsIt()
    {
        var link = SeedLink(revoked: true);
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = link.Id,
            LearnerKey = "learner-abc",
            RecipientName = "Somchai",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        var result = _learningSessionService.Join(
            "tok-1", new JoinLearningSessionDto { LearnerKey = "learner-abc", RecipientName = "Somchai" });

        Assert.Equal("session-1", result.Id);
    }

    [Fact]
    public void Restart_RevokedLink_AlwaysRejected_EvenWithAnExistingSession()
    {
        var link = SeedLink(revoked: true);
        _sessions.Items.Add(new LearningSession
        {
            Id = "session-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = link.Id,
            LearnerKey = "learner-abc",
            RecipientName = "Somchai",
            Status = SessionStatus.Ended,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        // Restart() always creates a brand new round - a revoked link must reject this outright,
        // unlike Join()'s resume branch.
        var ex = Assert.Throws<HttpStatusCodeException>(() => _learningSessionService.Restart(
            "tok-1", new JoinLearningSessionDto { LearnerKey = "learner-abc", RecipientName = "Somchai" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void Join_ActiveLink_NewVisitor_StillWorks()
    {
        SeedLink(revoked: false);

        var result = _learningSessionService.Join(
            "tok-1", new JoinLearningSessionDto { LearnerKey = "learner-new-visitor", RecipientName = "Somchai" });

        Assert.Equal(SessionStatus.InProgress, result.Status);
    }
}
