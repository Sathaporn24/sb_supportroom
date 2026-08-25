using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;
using SupportRoom.Providers.VoiceQuestion;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

/// <summary>
/// QA-06 (2) - proves AskTextAsync resolves lesson/category/global namespaces from the CALLER'S
/// company (via the token's TrainingLink, exactly like the voice path), not some other company's
/// data. Uses a capturing fake IVoiceQuestionProvider instead of the real Gemini one so the
/// namespace values reaching the provider can be asserted directly, without an Integration-tagged
/// live call.
/// </summary>
public class VoiceQuestionServiceTextNamespaceTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly CapturingVoiceQuestionProvider _provider = new();
    private readonly VoiceQuestionService _service;

    public VoiceQuestionServiceTextNamespaceTests()
    {
        MapsterConfig.Apply();
        var uow = new FakeUnitOfWork()
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<IDocumentResourceRepository>(new FakeDocumentResourceRepository())
            .Register<ILessonSlideNarrationRepository>(new FakeLessonSlideNarrationRepository())
            .Register<ISessionQuestionRepository>(_questions)
            .Register<IKnowledgeCategoryRepository>(new FakeKnowledgeCategoryRepository())
            .Register<ICompanyRepository>(new FakeCompanyRepository());
        var namespaceResolver = new KnowledgeNamespaceResolver(uow);

        var lessonService = new LessonConfigService(
            uow, new FakeServiceProvider(), NullLogger<ILessonConfigService>.Instance,
            new CannedSlidesProvider(), new FakeKnowledgeIndexingService(),
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            new MemoryCache(new MemoryCacheOptions()),
            new LessonSlideNarrationResolver(uow));
        var questionService = new SessionQuestionService(uow, new FakeServiceProvider(), NullLogger<ISessionQuestionService>.Instance);

        var serviceProvider = new FakeServiceProvider()
            .Register<ILessonConfigService>(lessonService)
            .Register<ISessionQuestionService>(questionService);
        serviceProvider.Register<ITrainingLinkService>(
            new TrainingLinkService(uow, serviceProvider, NullLogger<ITrainingLinkService>.Instance));
        serviceProvider.Register<ILearningSessionService>(
            new LearningSessionService(uow, serviceProvider, NullLogger<ILearningSessionService>.Instance));

        // company-test's own lesson/category, so the expected namespaces are unambiguous.
        _lessons.Items.Add(new LessonConfig
        {
            Id = "lesson-1",
            CompanyId = CompanyId,
            CategoryId = "kbcat-child",
            Slug = "lesson-a",
            Title = "บทเรียน",
            SlidesSourceUrl = "",
            PresentationId = GooglePresentationId,
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            SlideConfigs = [],
            IsActive = true,
        });
        _links.Items.Add(new TrainingLink
        {
            Id = "link-1",
            CompanyId = CompanyId,
            Token = "tok-1",
            LessonId = "lesson-1",
            LessonSlug = "lesson-a",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        _learningSessions.Items.Add(new LearningSession
        {
            Id = "learning-1",
            CompanyId = CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = "key-1",
            RecipientName = "ครูเอ",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        _service = new VoiceQuestionService(
            uow, serviceProvider, NullLogger<IVoiceQuestionService>.Instance,
            _provider, new FakeRealtimeNotifier(), namespaceResolver);
    }

    [Fact]
    public async Task AskTextAsync_ResolvesAllThreeNamespaces_FromTheLinksOwnCompany()
    {
        await _service.AskTextAsync(new AskTextQuestionDto
        {
            Token = "tok-1",
            LearnerKey = "key-1",
            Text = "คำถามทดสอบ",
            CurrentSlideObjectId = "slide-1",
        });

        Assert.NotNull(_provider.LastInput);
        // company-test's own lesson slug and category id - never company-other's, and never a
        // bare/unprefixed scope (see KnowledgeNamespaces' isolation invariant).
        Assert.Equal($"{CompanyId}:lesson-a", _provider.LastInput!.LessonNamespace);
        Assert.Equal($"{CompanyId}:kbcat-child", _provider.LastInput.CategoryNamespace);
        Assert.Equal($"{CompanyId}:kb-global", _provider.LastInput.GlobalNamespace);
    }

    /// <summary>Stands in for the real GoogleSlidesProvider so this test never needs a live Google
    /// service account - GetTeachingContentBySlugAsync only needs SOME slide content to build the
    /// grounding context that gets embedded/queried; its exact text is irrelevant to what this
    /// test asserts (the namespace strings passed to the question provider).</summary>
    private sealed class CannedSlidesProvider : ISlidesProvider
    {
        public Task<ResolvedPresentation> ResolvePresentationAsync(ResolvePresentationInput input)
            => throw new NotSupportedException("not exercised by this test");

        public Task<SlidesLessonContent> GetLessonContentAsync(GetLessonContentInput input)
            => Task.FromResult(new SlidesLessonContent
            {
                PresentationId = input.PresentationId,
                Title = "บทเรียนทดสอบ",
                EmbedUrl = "",
                SyncedAt = DateTime.UtcNow.ToString("O"),
                Slides = [new ResolvedSlide { SlideObjectId = "slide-1", Index = 0, SpeakerNotes = "หน้าแรกของบทเรียน" }],
            });
    }

    private sealed class CapturingVoiceQuestionProvider : IVoiceQuestionProvider
    {
        public TextQuestionInput? LastInput { get; private set; }

        public Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input)
            => throw new NotSupportedException("This test only exercises the typed-question path.");

        public Task<VoiceQuestionResult> AnswerTextAsync(TextQuestionInput input)
        {
            LastInput = input;
            return Task.FromResult(new VoiceQuestionResult
            {
                Transcript = input.QuestionText,
                Answer = "คำตอบ",
                AnswerStatus = AnswerStatus.Answered,
            });
        }
    }
}
