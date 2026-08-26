using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>
/// QQ-1 - the review queue's definition: (AnswerStatus == NotFound OR ReviewResult == Incorrect)
/// AND no KnowledgeQnASource already points at the question. Exercised through
/// IKnowledgeQnAService.GetQueue() (not just the repository) because QQ-1's second half is
/// implemented at the service layer as one batched lookup - see ISessionQuestionRepository.
/// GetReviewQueue's XML doc for why.
/// </summary>
public class KnowledgeQnAServiceTests
{
    private readonly FakeKnowledgeQnARepository _qnas = new();
    private readonly FakeKnowledgeQnASourceRepository _sources = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly KnowledgeQnAService _service;

    public KnowledgeQnAServiceTests()
    {
        var lessonConfigRepository = new FakeLessonConfigRepository();
        var categoryRepository = new FakeKnowledgeCategoryRepository();

        _unitOfWork
            .Register<IKnowledgeQnARepository>(_qnas)
            .Register<IKnowledgeQnASourceRepository>(_sources)
            .Register<ISessionQuestionRepository>(_questions)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ITrainingLinkRepository>(_links)
            .Register<IBackgroundJobRepository>(_jobs)
            .Register<ILessonConfigRepository>(lessonConfigRepository)
            .Register<IKnowledgeCategoryRepository>(categoryRepository)
            .Register<ISessionQuestionReviewExclusionRepository>(new FakeSessionQuestionReviewExclusionRepository());

        var namespaceResolver = new KnowledgeNamespaceResolver(_unitOfWork);
        _service = new KnowledgeQnAService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IKnowledgeQnAService>.Instance,
            TestFixtures.GuardFor(AdminRole.Cs, TestFixtures.CompanyId),
            namespaceResolver);
    }

    private void SeedLink(string sessionId, string linkId = "link-1", string lessonSlug = "lesson-slug")
    {
        _links.Items.Add(new TrainingLink
        {
            Id = linkId,
            CompanyId = TestFixtures.CompanyId,
            Token = linkId,
            LessonId = "lesson-1",
            LessonSlug = lessonSlug,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreateDate = DateTime.UtcNow,
        });
        _learningSessions.Items.Add(new LearningSession
        {
            Id = sessionId,
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = linkId,
            LearnerKey = "learner-1",
            RecipientName = "ผู้เรียน",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow,
        });
    }

    private SessionQuestion SeedQuestion(string id, string answerStatus, string? reviewResult = null)
    {
        var question = new SessionQuestion
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            SessionId = "session-1",
            AnswerStatus = answerStatus,
            Source = QuestionSource.Voice,
            ReviewResult = reviewResult,
            CreateDate = DateTime.UtcNow,
        };
        _questions.Items.Add(question);
        return question;
    }

    [Fact]
    public void GetQueue_IncludesAnAnswerStatusNotFoundQuestion()
    {
        SeedLink("session-1");
        SeedQuestion("q-1", AnswerStatus.NotFound);

        var queue = _service.GetQueue();

        Assert.Single(queue);
        Assert.Equal("q-1", queue[0].Id);
        Assert.True(queue[0].FromNotFound);
        Assert.False(queue[0].FromIncorrect);
    }

    [Fact]
    public void GetQueue_IncludesAReviewResultIncorrectQuestion()
    {
        SeedLink("session-1");
        SeedQuestion("q-1", AnswerStatus.Answered, ReviewResult.Incorrect);

        var queue = _service.GetQueue();

        Assert.Single(queue);
        Assert.Equal("q-1", queue[0].Id);
        Assert.False(queue[0].FromNotFound);
        Assert.True(queue[0].FromIncorrect);
    }

    [Fact]
    public void GetQueue_TagsBothWhenAQuestionIsNotFoundAndSeparatelyMarkedIncorrect()
    {
        // AI answered not_found AND CS separately marked the SAME row incorrect - QQ-3 says both
        // tags must show, not one or the other.
        SeedLink("session-1");
        SeedQuestion("q-1", AnswerStatus.NotFound, ReviewResult.Incorrect);

        var queue = _service.GetQueue();

        Assert.Single(queue);
        Assert.True(queue[0].FromNotFound);
        Assert.True(queue[0].FromIncorrect);
    }

    [Theory]
    [InlineData(AnswerStatus.OutOfScope)]
    [InlineData(AnswerStatus.NoSpeech)]
    [InlineData(AnswerStatus.TranscriptionFailed)]
    public void GetQueue_NeverIncludesStatusesThatAreNotAKnowledgeGap(string answerStatus)
    {
        // Q6's resolution: out-of-scope/no-speech/transcription-failed are filtered where the
        // question was answered, never re-filtered here.
        SeedLink("session-1");
        SeedQuestion("q-1", answerStatus);

        var queue = _service.GetQueue();

        Assert.Empty(queue);
    }

    [Fact]
    public void GetQueue_ExcludesAQuestionThatAlreadyHasAKnowledgeQnASource()
    {
        SeedLink("session-1");
        SeedQuestion("q-1", AnswerStatus.NotFound);
        _sources.Items.Add(new KnowledgeQnASource
        {
            Id = "qnasrc-1",
            CompanyId = TestFixtures.CompanyId,
            QnAId = "qna-1",
            SessionQuestionId = "q-1",
            CreateDate = DateTime.UtcNow,
        });

        var queue = _service.GetQueue();

        Assert.Empty(queue);
    }

    [Fact]
    public void GetQueue_ReportsWhichLessonEachQuestionCameFrom()
    {
        // QQ-4 - joined through LearningSession -> TrainingLink, never a denormalized LessonId on
        // SessionQuestion itself.
        SeedLink("session-1", lessonSlug: "how-to-refund");
        SeedQuestion("q-1", AnswerStatus.NotFound);

        var queue = _service.GetQueue();

        Assert.Equal("how-to-refund", queue[0].LessonSlug);
        Assert.Equal("lesson-1", queue[0].LessonId);
    }

    [Fact]
    public void GetQueue_SpansMultipleLearningSessionsAndLessons()
    {
        SeedLink("session-1", linkId: "link-1", lessonSlug: "lesson-a");
        SeedLink("session-2", linkId: "link-2", lessonSlug: "lesson-b");
        var q1 = SeedQuestion("q-1", AnswerStatus.NotFound);
        var q2 = SeedQuestion("q-2", AnswerStatus.NotFound);
        // Rewrite SessionId to point each question at a different session/link.
        _questions.Items.Remove(q1);
        _questions.Items.Add(new SessionQuestion { Id = q1.Id, CompanyId = q1.CompanyId, SessionId = "session-1", AnswerStatus = q1.AnswerStatus, Source = QuestionSource.Voice, CreateDate = q1.CreateDate });
        _questions.Items.Remove(q2);
        _questions.Items.Add(new SessionQuestion { Id = q2.Id, CompanyId = q2.CompanyId, SessionId = "session-2", AnswerStatus = q2.AnswerStatus, Source = QuestionSource.Voice, CreateDate = q2.CreateDate });

        var queue = _service.GetQueue();

        Assert.Equal(2, queue.Count);
        Assert.Contains(queue, x => x.LessonSlug == "lesson-a");
        Assert.Contains(queue, x => x.LessonSlug == "lesson-b");
    }

    private static KnowledgeQnA SeedExistingQnA(string id, string companyId, string question)
        => new()
        {
            Id = id,
            CompanyId = companyId,
            CreateDate = DateTime.UtcNow,
            Question = question,
            Answer = "คำตอบเดิม",
            ScopeType = KnowledgeScopeType.Company,
            VectorId = id,
            IndexingStatus = DocumentIndexingStatus.Pending,
        };

    // KL-23/Q-H2 - pre-save gate: trim + collapse repeated whitespace to one space +
    // case-insensitive, compared only against Question (never Answer), blocks the save with 409.
    [Fact]
    public async Task CreateAsync_Throws409WithDuplicateByQuestion_WhenQuestionMatchesAfterTrimWhitespaceCollapseAndCase()
    {
        SeedQuestion("q-1", AnswerStatus.NotFound);
        _qnas.Items.Add(SeedExistingQnA("qna-existing", TestFixtures.CompanyId, "วิธี   Reset รหัสผ่าน"));

        var error = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.CreateAsync(new CreateKnowledgeQnADto
        {
            Question = "  วิธี reset   รหัสผ่าน  ",
            Answer = "คำตอบใหม่",
            ScopeType = KnowledgeScopeType.Company,
            SessionQuestionIds = ["q-1"],
        }));

        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        var details = Assert.IsType<DuplicateQnAResponse>(error.Details);
        Assert.Single(details.DuplicateByQuestion);
        Assert.Equal("qna-existing", details.DuplicateByQuestion[0].Id);
        // Blocking - nothing new was written.
        Assert.Single(_qnas.Items);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenNoMatchingQuestionExists()
    {
        SeedQuestion("q-1", AnswerStatus.NotFound);

        var result = await _service.CreateAsync(new CreateKnowledgeQnADto
        {
            Question = "คำถามใหม่ไม่ซ้ำใคร",
            Answer = "คำตอบใหม่",
            ScopeType = KnowledgeScopeType.Company,
            SessionQuestionIds = ["q-1"],
        });

        Assert.Equal("คำถามใหม่ไม่ซ้ำใคร", result.Question);
        Assert.Single(_qnas.Items);
    }

    // KL-23/security gate item (4) - a real two-company test, not single-company (R-16 pattern).
    [Fact]
    public async Task CreateAsync_DuplicateCheck_NeverCrossesCompanies()
    {
        SeedQuestion("q-1", AnswerStatus.NotFound);
        _qnas.Items.Add(SeedExistingQnA("qna-other-company", TestFixtures.OtherCompanyId, "วิธี reset รหัสผ่าน"));

        var result = await _service.CreateAsync(new CreateKnowledgeQnADto
        {
            Question = "วิธี reset รหัสผ่าน",
            Answer = "คำตอบใหม่",
            ScopeType = KnowledgeScopeType.Company,
            SessionQuestionIds = ["q-1"],
        });

        Assert.Equal("วิธี reset รหัสผ่าน", result.Question);
        Assert.Equal(2, _qnas.Items.Count);
    }

    // KL-23 - proof the check runs before Commit(): a 409 must leave the database exactly as it
    // was, with no KnowledgeQnA/KnowledgeQnASource row written and no job queued.
    [Fact]
    public async Task CreateAsync_WritesNothingAndEnqueuesNoJob_WhenDuplicateFound()
    {
        SeedQuestion("q-1", AnswerStatus.NotFound);
        _qnas.Items.Add(SeedExistingQnA("qna-existing", TestFixtures.CompanyId, "วิธี reset รหัสผ่าน"));

        await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.CreateAsync(new CreateKnowledgeQnADto
        {
            Question = "วิธี reset รหัสผ่าน",
            Answer = "คำตอบใหม่",
            ScopeType = KnowledgeScopeType.Company,
            SessionQuestionIds = ["q-1"],
        }));

        Assert.Single(_qnas.Items);
        Assert.Empty(_sources.Items);
        Assert.Empty(_jobs.Items);
    }

    // KL-23/Q-H2 - ConfirmDuplicate=true is the override: saves through successfully even when a
    // genuine duplicate exists, no 409, no error.
    [Fact]
    public async Task CreateAsync_ConfirmDuplicateTrue_SavesSuccessfullyDespiteMatchingQuestion()
    {
        SeedQuestion("q-1", AnswerStatus.NotFound);
        _qnas.Items.Add(SeedExistingQnA("qna-existing", TestFixtures.CompanyId, "วิธี reset รหัสผ่าน"));

        var result = await _service.CreateAsync(new CreateKnowledgeQnADto
        {
            Question = "วิธี reset รหัสผ่าน",
            Answer = "คำตอบใหม่",
            ScopeType = KnowledgeScopeType.Company,
            SessionQuestionIds = ["q-1"],
            ConfirmDuplicate = true,
        });

        Assert.Equal("วิธี reset รหัสผ่าน", result.Question);
        Assert.Equal(2, _qnas.Items.Count);
        Assert.Single(_sources.Items);
        Assert.Single(_jobs.Items);
    }
}
