using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class SessionQuestionServiceTests
{
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly SessionQuestionService _service;

    public SessionQuestionServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ISessionQuestionRepository>(_questions)
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ILessonConfigRepository>(new FakeLessonConfigRepository());
        // Real services for the same reason as ChatMessageServiceTests: resolving the link by
        // token is what resolves the company, so it should not be stubbed away.
        var serviceProvider = new FakeServiceProvider();
        serviceProvider.Register<ITrainingLinkService>(
            new TrainingLinkService(_unitOfWork, serviceProvider, NullLogger<ITrainingLinkService>.Instance));
        serviceProvider.Register<ILearningSessionService>(
            new LearningSessionService(_unitOfWork, serviceProvider, NullLogger<ILearningSessionService>.Instance));
        _service = new SessionQuestionService(_unitOfWork, serviceProvider, NullLogger<ISessionQuestionService>.Instance);
    }

    private void Seed(string learningSessionId = "learning-1", string learnerKey = "key-1")
    {
        if (_links.Items.Count == 0)
        {
            _links.Items.Add(new TrainingLink
            {
                Id = "link-1",
                CompanyId = TestFixtures.CompanyId,
                Token = "tok",
                LessonId = "lesson-1",
                LessonSlug = "lesson-a",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            });
        }
        _learningSessions.Items.Add(new LearningSession
        {
            Id = learningSessionId,
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = learnerKey,
            RecipientName = "ครูเอ",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });
    }

    [Fact]
    public void Create_ThrowsNotFound_WhenLearningSessionMissing()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Create("ghost", new CreateSessionQuestionDto { AnswerStatus = AnswerStatus.Answered }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void Create_PersistsTheQuestion_AndCommits()
    {
        Seed("learning-1");

        var vm = _service.Create("learning-1", new CreateSessionQuestionDto
        {
            SlideObjectId = "slide-2",
            Transcript = "ถามอะไรสักอย่าง",
            Answer = "ตอบ",
            AnswerStatus = AnswerStatus.Answered,
        });

        Assert.Equal(AnswerStatus.Answered, vm.AnswerStatus);
        Assert.Equal("slide-2", vm.SlideObjectId);
        Assert.Null(vm.ReviewResult); // unreviewed until CS says otherwise
        Assert.Single(_questions.Items);
        Assert.Equal(1, _unitOfWork.CommitCount);
    }

    [Fact]
    public void GetForLearner_ReturnsOnlyThatLearnersQuestions_OldestFirst()
    {
        Seed("learning-1", "key-1");
        Seed("learning-2", "key-2");
        _questions.Items.Add(new SessionQuestion { Id = "q-late", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", AnswerStatus = AnswerStatus.Answered, CreateDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        _questions.Items.Add(new SessionQuestion { Id = "q-early", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", AnswerStatus = AnswerStatus.NotFound, CreateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        _questions.Items.Add(new SessionQuestion { Id = "q-other", CompanyId = TestFixtures.CompanyId, SessionId = "learning-2", AnswerStatus = AnswerStatus.Answered, CreateDate = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) });

        var list = _service.GetForLearner("tok", "key-1");

        Assert.Equal(2, list.Count);            // the other learner's question is excluded
        Assert.Equal("q-early", list[0].Id);    // oldest first
        Assert.Equal("q-late", list[1].Id);
    }

    [Fact]
    public void Review_RecordsTheVerdictAndTheNote()
    {
        Seed("learning-1");
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "learning-1",
            AnswerStatus = AnswerStatus.Answered,
            CreateDate = DateTime.UtcNow,
        });

        var vm = _service.Review("q-1", new ReviewSessionQuestionDto
        {
            ReviewResult = ReviewResult.Incorrect,
            ReviewNote = "  AI เดาเอง ไม่มีเอกสารเรื่องนี้  ",
        });

        Assert.Equal(ReviewResult.Incorrect, vm.ReviewResult);
        Assert.Equal("AI เดาเอง ไม่มีเอกสารเรื่องนี้", vm.ReviewNote); // trimmed
        Assert.NotNull(vm.ReviewedAt);
    }

    [Fact]
    public void Review_TreatsABlankNoteAsNoNote()
    {
        Seed("learning-1");
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "learning-1",
            AnswerStatus = AnswerStatus.Answered,
            CreateDate = DateTime.UtcNow,
        });

        var vm = _service.Review("q-1", new ReviewSessionQuestionDto { ReviewResult = ReviewResult.Correct, ReviewNote = "   " });

        Assert.Null(vm.ReviewNote);
    }

    [Fact]
    public void Review_WithNullResult_ClearsTheWholeReview()
    {
        Seed("learning-1");
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "learning-1",
            AnswerStatus = AnswerStatus.Answered,
            ReviewResult = ReviewResult.Incorrect,
            ReviewNote = "ผลเดิม",
            ReviewedAt = DateTime.UtcNow.AddDays(-1),
            CreateDate = DateTime.UtcNow,
        });

        var vm = _service.Review("q-1", new ReviewSessionQuestionDto
        {
            ReviewResult = null,
            // A note has no meaning without a result and must not remain behind.
            ReviewNote = "ข้อความนี้ต้องถูกล้าง",
        });

        Assert.Null(vm.ReviewResult);
        Assert.Null(vm.ReviewNote);
        Assert.Null(vm.ReviewedAt);
        Assert.NotNull(_questions.Items.Single().UpdateDate);
    }

    [Fact]
    public void Review_RejectsANoteLongerThanTwoThousandCharacters()
    {
        Seed("learning-1");
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "learning-1",
            AnswerStatus = AnswerStatus.Answered,
            CreateDate = DateTime.UtcNow,
        });

        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.Review("q-1", new ReviewSessionQuestionDto
        {
            ReviewResult = ReviewResult.Correct,
            ReviewNote = new string('ก', 2001),
        }));

        Assert.Equal(400, (int)ex.StatusCode);
    }

    [Fact]
    public void Review_RejectsAVerdictOutsideTheTwoAllowedValues()
    {
        Seed("learning-1");
        _questions.Items.Add(new SessionQuestion
        {
            Id = "q-1",
            CompanyId = TestFixtures.CompanyId,
            SessionId = "learning-1",
            AnswerStatus = AnswerStatus.Answered,
            CreateDate = DateTime.UtcNow,
        });

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Review("q-1", new ReviewSessionQuestionDto { ReviewResult = "maybe" }));
        Assert.Equal(400, (int)ex.StatusCode);
    }
}
