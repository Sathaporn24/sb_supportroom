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

/// <summary>
/// The behaviours the link/session split exists for: one link serving many people who never see
/// each other, and a browser that can come back to its own row.
/// </summary>
public class LearningSessionServiceTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly LearningSessionService _service;
    private readonly TrainingLinkService _linkService;

    public LearningSessionServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<ISessionQuestionRepository>(_questions);

        _linkService = new TrainingLinkService(_unitOfWork, new FakeServiceProvider(), NullLogger<ITrainingLinkService>.Instance);
        var serviceProvider = new FakeServiceProvider().Register<ITrainingLinkService>(_linkService);
        _service = new LearningSessionService(_unitOfWork, serviceProvider, NullLogger<ILearningSessionService>.Instance);
    }

    private string SeedLink(DateTime? expiresAt = null)
    {
        _lessons.Items.Add(new LessonConfig
        {
            Id = "lesson-a",
            CompanyId = TestFixtures.CompanyId,
            CategoryId = "kbcat-child",
            Slug = "lesson-a",
            Title = "บทเรียน",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
        });
        var created = _linkService.Create(new CreateTrainingLinkDto
        {
            LessonSlug = "lesson-a",
            ExpiresAt = (expiresAt ?? DateTime.UtcNow.AddHours(24)).ToString("O"),
        });
        return created.Token;
    }

    /// <summary>Ages a link past its expiry after runs already exist under it. ExpiresAt is init-only
    /// on the entity, so the row is swapped rather than mutated - the same thing the clock does in
    /// production, just without waiting for it.</summary>
    private void ExpireLink(string token)
    {
        var link = _links.Items.Single(x => x.Token == token);
        _links.Items.Remove(link);
        _links.Items.Add(new TrainingLink
        {
            Id = link.Id,
            CompanyId = link.CompanyId,
            Token = link.Token,
            LessonId = link.LessonId,
            LessonSlug = link.LessonSlug,
            RecipientOrgName = link.RecipientOrgName,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            MaxAttendees = link.MaxAttendees,
            CreateDate = link.CreateDate,
        });
    }

    [Fact]
    public void Join_IsIdempotent_SoAReconnectLandsBackOnTheSameRow()
    {
        var token = SeedLink();

        var first = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        var second = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_learningSessions.Items);
    }

    [Fact]
    public void TwoPeopleOnTheSameLink_GetSeparateSessions()
    {
        var token = SeedLink();

        var a = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-a" });
        var b = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูบี", LearnerKey = "learner-b" });

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal("ครูเอ", a.RecipientName);
        Assert.Equal("ครูบี", b.RecipientName);
        Assert.Equal(2, _learningSessions.Items.Count);
    }

    [Fact]
    public void DuplicateNamesAreFine_TheKeyIsWhatSeparatesPeople()
    {
        var token = SeedLink();

        var a = _service.Join(token, new JoinLearningSessionDto { RecipientName = "สมชาย", LearnerKey = "learner-a" });
        var b = _service.Join(token, new JoinLearningSessionDto { RecipientName = "สมชาย", LearnerKey = "learner-b" });

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Join_ReturnsAnEndedSessionAsIs_SoTheRecapCanBeShown()
    {
        var token = SeedLink();
        var joined = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        var reopened = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        Assert.Equal(joined.Id, reopened.Id);
        Assert.Equal(SessionStatus.Ended, reopened.Status);
        Assert.Single(_learningSessions.Items);
    }

    [Fact]
    public void Restart_StartsAFreshRound_LeavingTheFinishedOneAlone()
    {
        var token = SeedLink();
        var first = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        var second = _service.Restart(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(SessionStatus.InProgress, second.Status);
        Assert.Equal(2, _learningSessions.Items.Count);
        // The finished round keeps its status - restarting must not rewrite history.
        Assert.Equal(SessionStatus.Ended, _learningSessions.Items.Single(x => x.Id == first.Id).Status);
    }

    [Fact]
    public void Join_RefusesAnExpiredLink()
    {
        var token = SeedLink(DateTime.UtcNow.AddHours(-1));

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" }));
        Assert.Equal(400, (int)ex.StatusCode);
    }

    [Fact]
    public void Join_ReturnsAnExistingSessionAfterLinkExpiry_SoAReconnectCanFinish()
    {
        var token = SeedLink();
        var first = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        var link = _links.Items.Single();
        _links.Items[0] = new TrainingLink
        {
            Id = link.Id,
            CompanyId = link.CompanyId,
            Token = link.Token,
            LessonId = link.LessonId,
            LessonSlug = link.LessonSlug,
            RecipientOrgName = link.RecipientOrgName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            MaxAttendees = link.MaxAttendees,
            CreateBy = link.CreateBy,
            CreateDate = link.CreateDate,
        };

        var reopened = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        Assert.Equal(first.Id, reopened.Id);
        Assert.Single(_learningSessions.Items);
    }

    [Fact]
    public void Restart_RefusesAnExpiredLink_EvenWhenAnOlderRoundExists()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });
        ExpireLink(token);

        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.Restart(token,
            new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" }));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Single(_learningSessions.Items);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Join_RejectsANameThatIsBlankOrLongerThan80Characters(string recipientName)
    {
        var token = SeedLink();

        Assert.Throws<HttpStatusCodeException>(
            () => _service.Join(token, new JoinLearningSessionDto { RecipientName = recipientName, LearnerKey = "learner-1" }));
    }

    [Fact]
    public void Join_TrimsAndAcceptsAn80CharacterName()
    {
        var token = SeedLink();
        var name = new string('a', 80);

        var joined = _service.Join(token, new JoinLearningSessionDto
        {
            RecipientName = $"  {name}  ",
            LearnerKey = "learner-1",
        });

        Assert.Equal(name, joined.RecipientName);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(129)]
    public void Join_RejectsALearnerKeyOutsideTheContractBounds(int keyLength)
    {
        var token = SeedLink();

        Assert.Throws<HttpStatusCodeException>(() => _service.Join(token, new JoinLearningSessionDto
        {
            RecipientName = "ครูเอ",
            LearnerKey = new string('k', keyLength),
        }));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Join_AcceptsLearnerKeyAtEachContractBoundary(int keyLength)
    {
        var token = SeedLink();

        var joined = _service.Join(token, new JoinLearningSessionDto
        {
            RecipientName = "ครูเอ",
            LearnerKey = new string('k', keyLength),
        });

        Assert.NotNull(joined.Id);
    }

    [Fact]
    public void UpdateProgress_MovesTheSameRow_AndBumpsLastActivity()
    {
        var token = SeedLink();
        var joined = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        var moved = _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideObjectId = "slide-7",
            LastSlideIndex = 6,
        });

        Assert.Equal(joined.Id, moved.Id);
        Assert.Equal("slide-7", moved.LastSlideObjectId);
        Assert.Equal(6, moved.LastSlideIndex);
        Assert.Single(_learningSessions.Items);
    }

    [Fact]
    public void ProgressAndEnd_WithTheWrongLearnerKey_ReturnNotFound()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        var progressError = Assert.Throws<HttpStatusCodeException>(() => _service.UpdateProgress(token,
            new UpdateLearningProgressDto { LearnerKey = "learner-other", LastSlideIndex = 1 }));
        var endError = Assert.Throws<HttpStatusCodeException>(() => _service.End(token,
            new EndLearningSessionDto { LearnerKey = "learner-other", CompletedAllSlides = false }));

        Assert.Equal(404, (int)progressError.StatusCode);
        Assert.Equal(404, (int)endError.StatusCode);
    }

    [Fact]
    public void ExpiredLink_AllowsAnExistingRoundToSaveProgressAndEnd()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        ExpireLink(token);

        var progressed = _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideIndex = 4,
            TotalSlideCount = 10,
        });
        var ended = _service.End(token,
            new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = false });

        Assert.Equal(4, progressed.LastSlideIndex);
        Assert.Equal(SessionStatus.Ended, ended.Status);
        Assert.NotNull(ended.EndedAt);
    }

    [Fact]
    public void GetResumeState_WithNoLearnerKey_AnswersEmptyInsteadOfFailing()
    {
        var token = SeedLink();

        // A browser that has never been here (cleared storage, another device) is the normal first
        // case, not a validation error - the join screen just shows the name form.
        var state = _service.GetResumeState(token, null);

        Assert.Null(state.Resumable);
        Assert.Null(state.LastEnded);
        Assert.False(state.LinkExpired);
    }

    [Fact]
    public void GetResumeState_WithAnUnfinishedRun_ReportsItSoTheScreenCanAsk()
    {
        var token = SeedLink();
        var joined = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        var state = _service.GetResumeState(token, "learner-1");

        Assert.Equal(joined.Id, state.Resumable?.Id);
        // The name is what the confirmation question is built from ("คุณคือครูเอ ใช่ไหม").
        Assert.Equal("ครูเอ", state.Resumable?.RecipientName);
        Assert.Null(state.LastEnded);
    }

    [Fact]
    public void GetResumeState_AfterFinishing_ReportsLastEndedAndNothingToResume()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        var ended = _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        var state = _service.GetResumeState(token, "learner-1");

        // Nothing to confirm: the round is over, so the screen offers the recap and a fresh round
        // instead of asking whether they are the same person.
        Assert.Null(state.Resumable);
        Assert.Equal(ended.Id, state.LastEnded?.Id);
    }

    [Fact]
    public void GetResumeState_WithBoth_PrefersTheUnfinishedRun()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });
        var second = _service.Restart(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        var state = _service.GetResumeState(token, "learner-1");

        Assert.Equal(second.Id, state.Resumable?.Id);
        Assert.Null(state.LastEnded);
    }

    [Fact]
    public void GetResumeState_OnAnExpiredLink_StillReportsTheRunWaitingToBeFinished()
    {
        var token = SeedLink(DateTime.UtcNow.AddHours(2));
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        ExpireLink(token);

        // Expiry blocks STARTING something new, never finishing what was started in time. Throwing
        // here would lock a learner out mid-lesson and look like their progress was lost.
        var state = _service.GetResumeState(token, "learner-1");

        Assert.True(state.LinkExpired);
        Assert.NotNull(state.Resumable);
    }

    [Fact]
    public void GetResumeState_ForSomeoneElsesKeyOnTheSameLink_SeesNothing()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        // The second person on a shared computer generates their own key. Nothing of the first
        // person's may show up for them.
        var state = _service.GetResumeState(token, "learner-2");

        Assert.Null(state.Resumable);
        Assert.Null(state.LastEnded);
    }

    [Fact]
    public void UpdateProgress_AfterTheSessionEnded_ChangesNothingAndDoesNotThrow()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideObjectId = "slide-3",
            LastSlideIndex = 2,
        });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        // The tutor engine fires progress asynchronously, so a ping sent just before the learner
        // pressed "จบ" routinely arrives just after it. Rejecting it showed a failure for
        // something that worked; the finished row simply stands.
        var afterEnd = _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideObjectId = "slide-after-end",
            LastSlideIndex = 99,
        });

        Assert.Equal(SessionStatus.Ended, afterEnd.Status);
        Assert.Equal("slide-3", afterEnd.LastSlideObjectId);
        Assert.Equal(2, afterEnd.LastSlideIndex);
    }

    [Fact]
    public void UpdateProgress_OmittedFields_LeaveTheStoredProgressAlone()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideObjectId = "slide-7",
            LastSlideIndex = 6,
            TotalSlideCount = 20,
        });

        // A ping fired before the deck resolved carries neither an index nor a count. Writing
        // those through would drag a learner on slide 7 back to slide 1.
        var stale = _service.UpdateProgress(token, new UpdateLearningProgressDto { LearnerKey = "learner-1" });

        Assert.Equal(6, stale.LastSlideIndex);
        Assert.Equal(20, stale.TotalSlideCount);
        Assert.Equal("slide-7", stale.LastSlideObjectId);
        Assert.False(stale.CompletedAllSlides);
    }

    [Fact]
    public void UpdateProgress_ReachingTheLastSlide_MarksTheRunComplete()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        // Someone who watches to the end and closes the tab never calls End - without this the
        // CS list showed them as incomplete even though they saw every slide.
        var atLastSlide = _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideIndex = 19,
            TotalSlideCount = 20,
        });

        Assert.True(atLastSlide.CompletedAllSlides);
    }

    [Fact]
    public void End_NeverUnsetsCompletedAllSlides()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.UpdateProgress(token, new UpdateLearningProgressDto
        {
            LearnerKey = "learner-1",
            LastSlideIndex = 19,
            TotalSlideCount = 20,
        });

        // An end fired from a stale runtime can still report false. Reaching the last slide
        // already happened, so it stays true.
        var ended = _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = false });

        Assert.True(ended.CompletedAllSlides);
    }

    [Fact]
    public void End_IsIdempotent_AndDoesNotRewriteTheOriginalEndTime()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        var first = _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        var second = _service.End(token, new EndLearningSessionDto
        {
            LearnerKey = "learner-1",
            CompletedAllSlides = false,
            LastSlideIndex = 99,
        });

        Assert.Equal(first.EndedAt, second.EndedAt);
        Assert.True(second.CompletedAllSlides);
        Assert.NotEqual(99, second.LastSlideIndex);
    }

    [Fact]
    public void IsStalled_IsDerivedFromTheClock_AndNeverStored()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        var row = _learningSessions.Items.Single();
        row.LastActivityAt = DateTime.UtcNow.AddMinutes(-(ServerDefaults.GetInactiveThresholdMinutes() + 1));

        Assert.True(_service.GetById(row.Id).IsStalled);
        Assert.Equal(SessionStatus.InProgress, row.Status);
    }

    [Fact]
    public void AFinishedSessionIsNeverStalled_HoweverLongAgoItEnded()
    {
        var token = SeedLink();
        _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });
        _service.End(token, new EndLearningSessionDto { LearnerKey = "learner-1", CompletedAllSlides = true });

        var row = _learningSessions.Items.Single();
        row.LastActivityAt = DateTime.UtcNow.AddDays(-30);

        Assert.False(_service.GetById(row.Id).IsStalled);
    }

    [Fact]
    public void GetSummary_ComputesUnansweredPointsFromQuestions_WithNoSummaryTable()
    {
        var token = SeedLink();
        var joined = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-1" });

        _questions.Items.Add(SeedQuestion(joined.Id, AnswerStatus.Answered, "ถามเรื่องที่ตอบได้"));
        _questions.Items.Add(SeedQuestion(joined.Id, AnswerStatus.NotFound, "ถามเรื่องที่ไม่มีข้อมูล"));

        var summary = _service.GetSummary(joined.Id);

        Assert.Equal(2, summary.Questions.Count);
        Assert.Equal("ถามเรื่องที่ไม่มีข้อมูล", Assert.Single(summary.UnansweredPoints));
    }

    [Fact]
    public void GetSummary_OnlySeesItsOwnLearnersQuestions()
    {
        var token = SeedLink();
        var a = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูเอ", LearnerKey = "learner-a" });
        var b = _service.Join(token, new JoinLearningSessionDto { RecipientName = "ครูบี", LearnerKey = "learner-b" });

        _questions.Items.Add(SeedQuestion(a.Id, AnswerStatus.NotFound, "คำถามของเอ"));
        _questions.Items.Add(SeedQuestion(b.Id, AnswerStatus.NotFound, "คำถามของบี"));

        Assert.Equal("คำถามของเอ", Assert.Single(_service.GetSummary(a.Id).UnansweredPoints));
        Assert.Equal("คำถามของบี", Assert.Single(_service.GetSummary(b.Id).UnansweredPoints));
    }

    private static SessionQuestion SeedQuestion(string learningSessionId, string answerStatus, string transcript) => new()
    {
        Id = $"question-{Guid.NewGuid()}",
        CompanyId = TestFixtures.CompanyId,
        SessionId = learningSessionId,
        Transcript = transcript,
        AnswerStatus = answerStatus,
        Source = QuestionSource.Voice,
        CreateDate = DateTime.UtcNow,
    };
}
