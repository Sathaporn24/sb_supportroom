using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class SessionQuestionServiceTests
{
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeTrainingSessionRepository _sessions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly SessionQuestionService _service;

    public SessionQuestionServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ISessionQuestionRepository>(_questions)
            .Register<ITrainingSessionRepository>(_sessions)
            .Register<ILessonConfigRepository>(new FakeLessonConfigRepository());
        // Real TrainingSessionService for the same reason as ChatMessageServiceTests: GetByToken
        // is what resolves the company, so it should not be stubbed away.
        var serviceProvider = new FakeServiceProvider();
        serviceProvider.Register<ITrainingSessionService>(
            new TrainingSessionService(_unitOfWork, serviceProvider, NullLogger<ITrainingSessionService>.Instance));
        _service = new SessionQuestionService(_unitOfWork, serviceProvider, NullLogger<ISessionQuestionService>.Instance);
    }

    private void SeedSession(string id = "session-1")
        => _sessions.Items.Add(new TrainingSession
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            Token = "tok",
            LessonId = "lesson-1",
            LessonSlug = "lesson-a",
            Status = SessionStatus.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });

    [Fact]
    public void Create_ThrowsNotFound_WhenSessionMissing()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Create("ghost", new CreateSessionQuestionDto { AnswerStatus = AnswerStatus.Answered }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void Create_PersistsTheQuestion_AndCommits()
    {
        SeedSession("session-1");

        var vm = _service.Create("session-1", new CreateSessionQuestionDto
        {
            SlideObjectId = "slide-2",
            Transcript = "ถามอะไรสักอย่าง",
            Answer = "ตอบ",
            AnswerStatus = AnswerStatus.Answered,
        });

        Assert.Equal(AnswerStatus.Answered, vm.AnswerStatus);
        Assert.Equal("slide-2", vm.SlideObjectId);
        Assert.Single(_questions.Items);
        Assert.Equal(1, _unitOfWork.CommitCount);
    }

    [Fact]
    public void GetByToken_ReturnsQuestionsOldestFirst()
    {
        SeedSession("session-1");
        _questions.Items.Add(new SessionQuestion { Id = "q-late", CompanyId = TestFixtures.CompanyId, SessionId = "session-1", AnswerStatus = AnswerStatus.Answered, CreateDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        _questions.Items.Add(new SessionQuestion { Id = "q-early", CompanyId = TestFixtures.CompanyId, SessionId = "session-1", AnswerStatus = AnswerStatus.NotFound, CreateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        _questions.Items.Add(new SessionQuestion { Id = "q-other", CompanyId = TestFixtures.CompanyId, SessionId = "session-2", AnswerStatus = AnswerStatus.Answered, CreateDate = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) });

        var list = _service.GetByToken("tok");

        Assert.Equal(2, list.Count);            // the other session's question is excluded
        Assert.Equal("q-early", list[0].Id);    // oldest first
        Assert.Equal("q-late", list[1].Id);
    }
}
