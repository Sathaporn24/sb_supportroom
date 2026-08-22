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

public class TrainingLinkServiceTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TrainingLinkService _service;

    public TrainingLinkServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<ISessionQuestionRepository>(_questions);

        _service = new TrainingLinkService(_unitOfWork, new FakeServiceProvider(), NullLogger<ITrainingLinkService>.Instance);
    }

    private LessonConfig SeedLesson(string slug = "lesson-a")
    {
        var lesson = new LessonConfig
        {
            Id = $"lesson-{slug}",
            CompanyId = TestFixtures.CompanyId,
            CategoryId = "kbcat-child",
            Slug = slug,
            Title = "บทเรียน",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    [Fact]
    public void Create_MintsAnActiveLink_LinkedToTheLesson()
    {
        SeedLesson("lesson-a");

        var vm = _service.Create(new CreateTrainingLinkDto { LessonSlug = "lesson-a", RecipientOrgName = "ฝ่ายบุคคล" });

        Assert.Equal(LinkStatus.Active, vm.Status);
        Assert.False(string.IsNullOrEmpty(vm.Token));
        Assert.Equal("lesson-a", vm.LessonSlug);
        Assert.Equal("ฝ่ายบุคคล", vm.RecipientOrgName);
        Assert.Equal(0, vm.LearningSessionCount);
        Assert.Equal(0, vm.LearnerCount);
        Assert.Equal(0, vm.InProgressCount);
        Assert.Equal(0, vm.EndedCount);
        Assert.Equal(1, _unitOfWork.CommitCount);
        Assert.Single(_links.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsANonPositiveMaxAttendees(int maxAttendees)
    {
        SeedLesson("lesson-a");

        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.Create(new CreateTrainingLinkDto
        {
            LessonSlug = "lesson-a",
            MaxAttendees = maxAttendees,
        }));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_links.Items);
    }

    [Fact]
    public void Create_ThrowsNotFound_ForAnUnknownLesson()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Create(new CreateTrainingLinkDto { LessonSlug = "ghost" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void Create_RefusesAnInactiveLesson()
    {
        var lesson = SeedLesson("lesson-a");
        lesson.IsActive = false;

        var ex = Assert.Throws<HttpStatusCodeException>(
            () => _service.Create(new CreateTrainingLinkDto { LessonSlug = "lesson-a" }));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_links.Items);
    }

    [Fact]
    public void GetById_And_GetByToken_ThrowNotFound_WhenMissing()
    {
        Assert.Throws<HttpStatusCodeException>(() => _service.GetById("nope"));
        Assert.Throws<HttpStatusCodeException>(() => _service.GetByToken("nope"));
    }

    [Fact]
    public void GetByToken_RefusesToReplaceAnAlreadySelectedCompanyContext()
    {
        _links.Items.Add(new TrainingLink
        {
            Id = "link-other",
            CompanyId = TestFixtures.OtherCompanyId,
            Token = "token-other",
            LessonId = "lesson-other",
            LessonSlug = "lesson-a",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });

        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.GetByToken("token-other"));

        Assert.Equal(403, (int)ex.StatusCode);
    }

    [Fact]
    public void Status_IsComputedFromExpiry_NotStored()
    {
        SeedLesson();
        var created = _service.Create(new CreateTrainingLinkDto
        {
            LessonSlug = "lesson-a",
            ExpiresAt = DateTime.UtcNow.AddHours(-1).ToString("O"),
        });

        Assert.Equal(LinkStatus.Expired, created.Status);
        // Nothing on the entity says "expired" - the column does not exist.
        Assert.Single(_links.Items);
    }

    [Fact]
    public void GetAll_ReturnsDistinctLearnersAndRoundStatusAggregates()
    {
        SeedLesson();
        var first = _service.Create(new CreateTrainingLinkDto { LessonSlug = "lesson-a" });
        var second = _service.Create(new CreateTrainingLinkDto { LessonSlug = "lesson-a" });

        // Two people on the first link. key-1 has learned twice, so there are three rounds but
        // only two distinct learners.
        _learningSessions.Items.Add(SeedLearningSession(first.Id, "key-1"));
        var endedForKey1 = SeedLearningSession(first.Id, "key-1");
        endedForKey1.Status = SessionStatus.Ended;
        _learningSessions.Items.Add(endedForKey1);
        var endedForKey2 = SeedLearningSession(first.Id, "key-2");
        endedForKey2.Status = SessionStatus.Ended;
        _learningSessions.Items.Add(endedForKey2);

        var all = _service.GetAll();

        var populated = all.Single(x => x.Id == first.Id);
        Assert.Equal(3, populated.LearningSessionCount);
        Assert.Equal(2, populated.LearnerCount);
        Assert.Equal(1, populated.InProgressCount);
        Assert.Equal(2, populated.EndedCount);

        var empty = all.Single(x => x.Id == second.Id);
        Assert.Equal(0, empty.LearningSessionCount);
        Assert.Equal(0, empty.LearnerCount);
        Assert.Equal(0, empty.InProgressCount);
        Assert.Equal(0, empty.EndedCount);
    }

    private static LearningSession SeedLearningSession(string linkId, string learnerKey) => new()
    {
        Id = $"learning-{Guid.NewGuid():N}",
        CompanyId = TestFixtures.CompanyId,
        TrainingLinkId = linkId,
        LearnerKey = learnerKey,
        RecipientName = "ผู้เรียน",
        Status = SessionStatus.InProgress,
        StartedAt = DateTime.UtcNow,
        LastActivityAt = DateTime.UtcNow,
    };
}
