using SupportRoom.Domain.Common;
using System.Linq.Expressions;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.Services;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Tests.Fakes;

/// <summary>
/// Hand-rolled in-memory test doubles - the codebase deliberately uses no mocking library (the
/// Providers.Tests exercise real providers + pure logic), so service tests follow the same
/// style with tiny list-backed repositories instead of a Moq/NSubstitute dependency. These are
/// repository/UnitOfWork fakes, a different concept from the external-service Mock *providers*
/// that used to exist (Slides/TTS/VoiceQuestion/Storage/Knowledge) - those have been removed
/// entirely; every provider category now requires a Real implementation.
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    public int CommitCount { get; private set; }

    public FakeUnitOfWork Register<TRepository>(TRepository repository) where TRepository : notnull
    {
        _repositories[typeof(TRepository)] = repository;
        return this;
    }

    public TRepository GetRepository<TRepository>() => (TRepository)_repositories[typeof(TRepository)];

    public void Commit() => CommitCount++;
}

internal sealed class FakeLessonConfigRepository : ILessonConfigRepository
{
    public readonly List<LessonConfig> Items = [];

    public IQueryable<LessonConfig> GetAll() => Items.AsQueryable();
    public IQueryable<LessonConfig> FindBy(Expression<Func<LessonConfig, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public LessonConfig? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<LessonConfig?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(LessonConfig entity) => Items.Add(entity);
    public void Update(LessonConfig entity) { /* list already holds the tracked reference */ }
    public void Delete(LessonConfig entity) => Items.Remove(entity);

    public LessonConfig? GetBySlug(string slug) => Items.FirstOrDefault(x => x.Slug == slug);
    public IQueryable<LessonConfig> GetActive() => Items.AsQueryable().Where(x => x.IsActive);
}

internal sealed class FakeDocumentResourceRepository : IDocumentResourceRepository
{
    public readonly List<DocumentResource> Items = [];

    public IQueryable<DocumentResource> GetAll() => Items.AsQueryable();
    public IQueryable<DocumentResource> FindBy(Expression<Func<DocumentResource, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public DocumentResource? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<DocumentResource?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(DocumentResource entity) => Items.Add(entity);
    public void Update(DocumentResource entity) { }
    public void Delete(DocumentResource entity) => Items.Remove(entity);

    public IQueryable<DocumentResource> GetByLessonId(string lessonId) => Items.AsQueryable().Where(x => x.LessonId == lessonId);
    public IQueryable<DocumentResource> GetStandalone() => Items.AsQueryable().Where(x => x.LessonId == null);
}

internal sealed class FakeTrainingLinkRepository : ITrainingLinkRepository
{
    public readonly List<TrainingLink> Items = [];

    public IQueryable<TrainingLink> GetAll() => Items.AsQueryable();
    public IQueryable<TrainingLink> FindBy(Expression<Func<TrainingLink, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public TrainingLink? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<TrainingLink?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(TrainingLink entity) => Items.Add(entity);
    public void Update(TrainingLink entity) { }
    public void Delete(TrainingLink entity) => Items.Remove(entity);

    public TrainingLink? GetByToken(string token) => Items.FirstOrDefault(x => x.Token == token);
}

internal sealed class FakeLearningSessionRepository : ILearningSessionRepository
{
    public readonly List<LearningSession> Items = [];

    public IQueryable<LearningSession> GetAll() => Items.AsQueryable();
    public IQueryable<LearningSession> FindBy(Expression<Func<LearningSession, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public LearningSession? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<LearningSession?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(LearningSession entity) => Items.Add(entity);
    public void Update(LearningSession entity) { }
    public void Delete(LearningSession entity) => Items.Remove(entity);

    public LearningSession? GetActiveByLearnerKey(string trainingLinkId, string learnerKey)
        => Items.Where(x => x.TrainingLinkId == trainingLinkId && x.LearnerKey == learnerKey)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault();

    public IQueryable<LearningSession> GetByTrainingLinkId(string trainingLinkId)
        => Items.AsQueryable().Where(x => x.TrainingLinkId == trainingLinkId);
}

internal sealed class FakeSessionQuestionRepository : ISessionQuestionRepository
{
    public readonly List<SessionQuestion> Items = [];

    public IQueryable<SessionQuestion> GetAll() => Items.AsQueryable();
    public IQueryable<SessionQuestion> FindBy(Expression<Func<SessionQuestion, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public SessionQuestion? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<SessionQuestion?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(SessionQuestion entity) => Items.Add(entity);
    public void Update(SessionQuestion entity) { }
    public void Delete(SessionQuestion entity) => Items.Remove(entity);

    public IQueryable<SessionQuestion> GetBySessionId(string sessionId) => Items.AsQueryable().Where(x => x.SessionId == sessionId);
}

internal sealed class FakeChatMessageRepository : IChatMessageRepository
{
    public readonly List<ChatMessage> Items = [];

    public IQueryable<ChatMessage> GetAll() => Items.AsQueryable();
    public IQueryable<ChatMessage> FindBy(Expression<Func<ChatMessage, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public ChatMessage? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<ChatMessage?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(ChatMessage entity) => Items.Add(entity);
    public void Update(ChatMessage entity) { }
    public void Delete(ChatMessage entity) => Items.Remove(entity);

    public IQueryable<ChatMessage> GetBySessionId(string sessionId) => Items.AsQueryable().Where(x => x.SessionId == sessionId);
}

/// <summary>Records what would have been indexed without touching Gemini/Pinecone - the real
/// service is best-effort and never throws, so the fake mirrors that.</summary>
internal sealed class FakeKnowledgeIndexingService : IKnowledgeIndexingService
{
    public int IndexLessonCallCount { get; private set; }

    public Task IndexLessonAsync(string lessonSlug, IReadOnlyList<ResolvedSlide> slides)
    {
        IndexLessonCallCount++;
        return Task.CompletedTask;
    }

    public Task<int> IndexChunksAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks)
        => Task.FromResult(chunks.Count);
}

// The three fakes below only exist to satisfy AdminService's constructor for the ResetDemoData
// unit tests - reset never touches them. ReindexAllAsync (which does) is verified end-to-end
// against the live endpoint with real providers, so throwing here makes an accidental call in a
// unit test loud rather than silently passing on fake behavior.
internal sealed class FakeDocumentStorageProvider : IDocumentStorageProvider
{
    public string BucketName => "fake-bucket";
    public Task UploadAsync(string key, Stream content, string contentType) => throw new NotSupportedException("not exercised in unit tests");
    public Task<Stream> DownloadAsync(string key) => throw new NotSupportedException("not exercised in unit tests");
    public Task DeleteAsync(string key) => throw new NotSupportedException("not exercised in unit tests");
    public Task<string> GetPresignedUrlAsync(string key) => throw new NotSupportedException("not exercised in unit tests");
}

internal sealed class FakeKnowledgeIndexProvider : IKnowledgeIndexProvider
{
    public Task UpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeChunk> chunks) => throw new NotSupportedException("not exercised in unit tests");
    public Task<IReadOnlyList<ScoredChunk>> QueryAsync(string namespaceKey, float[] queryVector, int topK) => throw new NotSupportedException("not exercised in unit tests");
    public Task DeleteNamespaceAsync(string namespaceKey) => throw new NotSupportedException("not exercised in unit tests");
}

internal sealed class FakeSlidesProvider : ISlidesProvider
{
    public Task<ResolvedPresentation> ResolvePresentationAsync(ResolvePresentationInput input) => throw new NotSupportedException("not exercised in unit tests");
    public Task<SlidesLessonContent> GetLessonContentAsync(GetLessonContentInput input) => throw new NotSupportedException("not exercised in unit tests");
}

internal sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public int NewQuestionCount { get; private set; }
    public int ChatMessageCount { get; private set; }
    /// <summary>The group key broadcasts go to. Now a LEARNING SESSION id, not a link token -
    /// tests assert on this because a token-keyed group would fan one learner's questions out to
    /// everyone else holding the same link.</summary>
    public string? LastQuestionTarget { get; private set; }
    public string? LastChatTarget { get; private set; }

    public Task NotifyNewQuestionAsync(string learningSessionId, SessionQuestionViewModel question)
    {
        NewQuestionCount++;
        LastQuestionTarget = learningSessionId;
        return Task.CompletedTask;
    }

    public Task NotifyChatMessageAsync(string learningSessionId, ChatMessageViewModel message)
    {
        ChatMessageCount++;
        LastChatTarget = learningSessionId;
        return Task.CompletedTask;
    }
}

/// <summary>Minimal registry so services that resolve collaborators via ServiceProvider
/// (ServiceBase.ServiceProvider) get a real instance in tests. Unregistered types return null,
/// which is all LessonConfigService needs.</summary>
internal sealed class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public FakeServiceProvider()
    {
        // ServiceBase resolves ICompanyContext in its constructor, so every service under test
        // needs one present - pre-resolved to TestFixtures.CompanyId so entities created during
        // a test carry the same company the fixtures seed.
        var companyContext = new CompanyContext();
        companyContext.Resolve(TestFixtures.CompanyId);
        _services[typeof(ICompanyContext)] = companyContext;
    }

    public FakeServiceProvider Register<T>(T implementation) where T : notnull
    {
        _services[typeof(T)] = implementation;
        return this;
    }

    public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
}
