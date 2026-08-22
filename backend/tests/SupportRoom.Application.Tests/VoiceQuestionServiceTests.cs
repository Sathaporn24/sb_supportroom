using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
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
/// Exercises VoiceQuestionService against the real LessonConfigService + SessionQuestionService
/// wired through the fake ServiceProvider, with the real Gemini voice provider. The key
/// regression: the service resolves slides via GetTeachingContentBySlugAsync (works for any
/// content source) instead of demanding a Google PresentationId.
/// </summary>
[Trait(TestCategories.Category, TestCategories.Integration)]
public class VoiceQuestionServiceTests
{
    // GeminiVoiceQuestionProvider checks DurationMs itself before ever calling Gemini (see
    // UploadLimits.MinVoiceDurationMs in the real provider) - real, deterministic provider
    // behavior, not a Mock-only canned response - so garbage audio bytes are fine for this one
    // early-return path only. Every other test needs genuinely valid audio: Gemini rejects
    // structurally-invalid bytes outright (400 INVALID_ARGUMENT) rather than treating them as
    // unclear speech, so there's no such thing as "garbage audio that still parses."
    private static readonly byte[] GarbageAudio = [1, 2, 3];

    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeRealtimeNotifier _notifier = new();
    private readonly VoiceQuestionService _service;

    public VoiceQuestionServiceTests()
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
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance), new FakeKnowledgeIndexingService(),
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            new MemoryCache(new MemoryCacheOptions()),
            new LessonSlideNarrationResolver(uow));
        var questionService = new SessionQuestionService(uow, new FakeServiceProvider(), NullLogger<ISessionQuestionService>.Instance);

        var serviceProvider = new FakeServiceProvider()
            .Register<ILessonConfigService>(lessonService)
            .Register<ISessionQuestionService>(questionService);
        // AskAsync starts by resolving the link from its token - that lookup is what scopes the
        // request to a company and supplies the lesson slug, instead of trusting a slug the caller
        // sent alongside an unrelated session id. The learner key then picks WHICH person on that
        // link is asking, so the question is filed under, and broadcast to, the right session.
        serviceProvider.Register<ITrainingLinkService>(
            new TrainingLinkService(uow, serviceProvider, NullLogger<ITrainingLinkService>.Instance));
        serviceProvider.Register<ILearningSessionService>(
            new LearningSessionService(uow, serviceProvider, NullLogger<ILearningSessionService>.Instance));

        // Active Google-Slides lesson so GetTeachingContentBySlugAsync returns the real deck.
        _lessons.Items.Add(new LessonConfig
        {
            Id = "lesson-1",
            CompanyId = TestFixtures.CompanyId,
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
            CompanyId = TestFixtures.CompanyId,
            Token = "tok-1",
            LessonId = "lesson-1",
            LessonSlug = "lesson-a",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        _learningSessions.Items.Add(new LearningSession
        {
            Id = "learning-1",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = "key-1",
            RecipientName = "ครูเอ",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });

        _service = new VoiceQuestionService(
            uow, serviceProvider, NullLogger<IVoiceQuestionService>.Instance,
            new GeminiVoiceQuestionProvider(new RealHttpClientFactory(), NullLogger<GeminiVoiceQuestionProvider>.Instance),
            _notifier, namespaceResolver);
    }

    private static AskVoiceQuestionDto Ask(byte[] audio, int durationMs, string expecting = "question") => new()
    {
        Audio = audio,
        MimeType = "audio/mpeg",
        Token = "tok-1",
        LearnerKey = "key-1",
        DurationMs = durationMs,
        CurrentSlideObjectId = "slide-1",
        Expecting = expecting,
    };

    [Fact]
    public async Task AskAsync_AskedQuestion_RecordsItAndBroadcasts()
    {
        // Real spoken audio (see Fixtures/spoken-question.mp3) so Gemini has something genuine to
        // transcribe - not pinned to AnswerStatus.Answered specifically, since the fixture asks
        // about a public demo deck ("Baby album") whose speaker notes are just photo-source URLs
        // with nothing substantive to answer from, so Gemini legitimately may say not_found. What
        // this test actually guards is the orchestration: any non-NoSpeech result gets persisted
        // and broadcast, regardless of which particular status Gemini lands on.
        var audio = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "spoken-question.mp3"));

        var result = await _service.AskAsync(Ask(audio, durationMs: 4000));

        Assert.NotEqual(AnswerStatus.NoSpeech, result.AnswerStatus);
        Assert.Single(_questions.Items);            // a reviewable question was persisted
        Assert.Equal(1, _notifier.NewQuestionCount); // and pushed live to the CS dashboard
        // Filed under, and broadcast to, this learner's own session - not the link token, which
        // would fan it out to everyone else who happens to hold the same link.
        Assert.Equal("learning-1", _notifier.LastQuestionTarget);
        Assert.Equal("learning-1", _questions.Items.Single().SessionId);
    }

    [Fact]
    public async Task AskAsync_TooShortRecording_IsNoSpeech_AndRecordsNothing()
    {
        var result = await _service.AskAsync(Ask(GarbageAudio, durationMs: 100));

        Assert.Equal(AnswerStatus.NoSpeech, result.AnswerStatus);
        Assert.Empty(_questions.Items);             // no_speech isn't a question CS should review
        Assert.Equal(0, _notifier.NewQuestionCount);
    }

    [Fact]
    public async Task AskAsync_UnclearReadinessReply_DefaultsToNotReady()
    {
        // Needs genuinely valid audio - Gemini rejects structurally-invalid bytes outright
        // (400 INVALID_ARGUMENT) rather than treating them as "unclear speech." Reusing the
        // spoken-question fixture works precisely because its content ("what is this picture?")
        // is real, valid speech that still isn't a yes/no reply, so the readiness prompt's own
        // explicit fallback ("if unclear, treat as not_ready") is what's actually being exercised.
        var audio = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "spoken-question.mp3"));

        var result = await _service.AskAsync(Ask(audio, durationMs: 4000, expecting: "readiness"));

        Assert.Equal("not_ready", result.Readiness);
        Assert.Empty(_questions.Items);             // a yes/no start reply isn't a lesson question
    }
}
