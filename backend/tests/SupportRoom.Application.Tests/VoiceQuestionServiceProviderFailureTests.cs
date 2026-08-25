using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;
using SupportRoom.Providers.VoiceQuestion;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

/// <summary>
/// QA-06 residual - the case QA flagged as missing was never "session already Ended" (that was
/// already covered), it was "the voice/text provider itself throws". Uses a fake IVoiceQuestionProvider
/// that always throws so this never needs a live Gemini/OpenAI call (no Integration trait needed),
/// matching the pattern VoiceQuestionServiceTextNamespaceTests already established for provider
/// substitution.
/// </summary>
public class VoiceQuestionServiceProviderFailureTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeRealtimeNotifier _notifier = new();
    private readonly VoiceQuestionService _service;

    public VoiceQuestionServiceProviderFailureTests()
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
            new AlwaysThrowingVoiceQuestionProvider(), _notifier, namespaceResolver);
    }

    [Fact]
    public async Task AskAsync_WhenProviderThrows_ThrowsUpstreamError_AndRecordsNothing()
    {
        var input = new AskVoiceQuestionDto
        {
            Audio = [1, 2, 3],
            MimeType = "audio/mpeg",
            Token = "tok-1",
            LearnerKey = "key-1",
            DurationMs = 4000,
            CurrentSlideObjectId = "slide-1",
        };

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.AskAsync(input));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Empty(_questions.Items);
        Assert.Equal(0, _notifier.NewQuestionCount);
    }

    [Fact]
    public async Task AskTextAsync_WhenProviderThrows_ThrowsUpstreamError_AndRecordsNothing()
    {
        var input = new AskTextQuestionDto
        {
            Token = "tok-1",
            LearnerKey = "key-1",
            Text = "คำถามทดสอบ",
            CurrentSlideObjectId = "slide-1",
        };

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.AskTextAsync(input));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Empty(_questions.Items);
        Assert.Equal(0, _notifier.NewQuestionCount);
    }

    /// <summary>Stands in for the real GoogleSlidesProvider so this test never needs a live Google
    /// service account - see VoiceQuestionServiceTextNamespaceTests, which uses the same fake.</summary>
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

    /// <summary>Simulates an upstream provider outage (network failure, 5xx, malformed response) on
    /// both the voice and typed-question paths - the exact case QA-06 flagged as unverified: neither
    /// path may write a SessionQuestion row or broadcast when the provider itself fails.</summary>
    private sealed class AlwaysThrowingVoiceQuestionProvider : IVoiceQuestionProvider
    {
        public Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input)
            => throw new InvalidOperationException("Simulated provider outage");

        public Task<VoiceQuestionResult> AnswerTextAsync(TextQuestionInput input)
            => throw new InvalidOperationException("Simulated provider outage");
    }
}
