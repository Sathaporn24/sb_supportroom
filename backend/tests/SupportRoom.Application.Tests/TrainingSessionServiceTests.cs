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

public class TrainingSessionServiceTests
{
    private readonly FakeTrainingSessionRepository _sessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionSummaryRepository _summaries = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TrainingSessionService _service;

    public TrainingSessionServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ITrainingSessionRepository>(_sessions)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<ISessionSummaryRepository>(_summaries)
            .Register<ISessionQuestionRepository>(_questions);

        // End() resolves ISessionSummaryService via ServiceProvider - give it the real one on the
        // same UnitOfWork so the summary actually lands in the fake summary repo.
        var summaryService = new SessionSummaryService(_unitOfWork, new FakeServiceProvider(), NullLogger<ISessionSummaryService>.Instance);
        var serviceProvider = new FakeServiceProvider().Register<ISessionSummaryService>(summaryService);

        _service = new TrainingSessionService(_unitOfWork, serviceProvider, NullLogger<ITrainingSessionService>.Instance);
    }

    private LessonConfig SeedLesson(string slug = "lesson-a")
    {
        var lesson = new LessonConfig
        {
            Id = $"lesson-{slug}",
            Slug = slug,
            Title = "บทเรียน",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            IntroWaitMs = 5000,
            BreathPauseMs = 500,
            FinalQuestionWaitMs = 5000,
            SlideConfigs = [],
            IsActive = true,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    [Fact]
    public void Create_MintsANotStartedSession_LinkedToTheLesson()
    {
        SeedLesson("lesson-a");

        var vm = _service.Create(new CreateSessionDto { LessonSlug = "lesson-a", TeacherName = "ครูเอ" });

        Assert.Equal(SessionStatus.NotStarted, vm.Status);
        Assert.False(string.IsNullOrEmpty(vm.Token));
        Assert.Equal("lesson-a", vm.LessonSlug);
        Assert.Equal(1, _unitOfWork.CommitCount);
        Assert.Single(_sessions.Items);
    }

    [Fact]
    public void Create_ThrowsNotFound_ForAnUnknownLesson()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Create(new CreateSessionDto { LessonSlug = "ghost" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void GetById_And_GetByToken_ThrowNotFound_WhenMissing()
    {
        Assert.Throws<HttpStatusCodeException>(() => _service.GetById("nope"));
        Assert.Throws<HttpStatusCodeException>(() => _service.GetByToken("nope"));
    }

    [Fact]
    public void MarkStarted_SetsInProgressAndStartedAt()
    {
        SeedLesson();
        var created = _service.Create(new CreateSessionDto { LessonSlug = "lesson-a" });

        var started = _service.MarkStarted(created.Token);

        Assert.Equal(SessionStatus.InProgress, started.Status);
        Assert.NotNull(started.StartedAt);
    }

    [Fact]
    public void End_MarksEnded_AndWritesASummary()
    {
        SeedLesson();
        var created = _service.Create(new CreateSessionDto { LessonSlug = "lesson-a" });

        var ended = _service.End(created.Token, new EndSessionDto { CompletedAllSlides = true, LastSlideObjectId = "slide-6" });

        Assert.Equal(SessionStatus.Ended, ended.Status);
        Assert.NotNull(ended.EndedAt);
        Assert.True(ended.CompletedAllSlides);
        // The summary was created through the injected ISessionSummaryService on the same UnitOfWork.
        var summary = Assert.Single(_summaries.Items);
        Assert.Equal(created.Id, summary.SessionId);
        Assert.True(summary.CompletedAllSlides);
    }
}
