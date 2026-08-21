using SupportRoom.Domain.Common;
using System.Linq.Expressions;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.Services;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
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
    public IQueryable<LessonConfig> GetByCategoryId(string categoryId) => Items.AsQueryable().Where(x => x.CategoryId == categoryId);
    public int CountByCategoryId(string categoryId) => Items.Count(x => x.CategoryId == categoryId);
}

internal sealed class FakeDocumentResourceRepository : IDocumentResourceRepository
{
    public readonly List<DocumentResource> Items = [];

    // Mirrors the real HasQueryFilter (CompanyId + !IsDelete) - RepositoryBase.Get() uses
    // DbSet.Find(), which still applies global query filters, so a soft-deleted row must not be
    // reachable through Get() here either (DS-7 - MoveScopeAsync relies on this to 404 instead of
    // moving something out of the recovery bin).
    public IQueryable<DocumentResource> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<DocumentResource> FindBy(Expression<Func<DocumentResource, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public DocumentResource? Get(string id) => Items.FirstOrDefault(x => x.Id == id && !x.IsDelete);
    public Task<DocumentResource?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(DocumentResource entity) => Items.Add(entity);
    public void Update(DocumentResource entity) { }
    public void Delete(DocumentResource entity) => Items.Remove(entity);

    // Mirrors the real HasQueryFilter (CompanyId + !IsDelete) - GetByScope must not surface a
    // soft-deleted row, the same as the real DB query would not.
    public IQueryable<DocumentResource> GetByScope(string scopeType, string? scopeId)
        => Items.AsQueryable().Where(x => x.ScopeType == scopeType && x.ScopeId == scopeId && !x.IsDelete);
    public IQueryable<DocumentResource> GetDeleted(string companyId) => Items.AsQueryable().Where(x => x.CompanyId == companyId && x.IsDelete);
}

/// <summary>⚠️ Unscoped for the same reason as FakeCompanyRepository - the real BackgroundJob
/// table has no company query filter either (see ApplicationDbContext).</summary>
internal sealed class FakeBackgroundJobRepository : IBackgroundJobRepository
{
    public readonly List<BackgroundJob> Items = [];

    public IQueryable<BackgroundJob> GetAll() => Items.AsQueryable();
    public IQueryable<BackgroundJob> FindBy(Expression<Func<BackgroundJob, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public BackgroundJob? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<BackgroundJob?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(BackgroundJob entity) => Items.Add(entity);
    public void Update(BackgroundJob entity) { }
    public void Delete(BackgroundJob entity) => Items.Remove(entity);

    public BackgroundJob? ClaimNext(DateTime now) => throw new NotSupportedException("not exercised in unit tests - needs FOR UPDATE SKIP LOCKED against real Postgres");
    public int RequeueOrphanedRunning() => throw new NotSupportedException("not exercised in unit tests - needs real Postgres");
}

internal sealed class FakeDocumentChunkRepository : IDocumentChunkRepository
{
    public readonly List<DocumentChunk> Items = [];

    public IQueryable<DocumentChunk> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<DocumentChunk> FindBy(Expression<Func<DocumentChunk, bool>> predicate) => GetAll().Where(predicate);
    public DocumentChunk? Get(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    public Task<DocumentChunk?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(DocumentChunk entity) => Items.Add(entity);
    public void Update(DocumentChunk entity) { }
    public void Delete(DocumentChunk entity) => Items.Remove(entity);

    public IQueryable<DocumentChunk> GetByDocumentId(string documentId)
        => GetAll().Where(x => x.DocumentId == documentId).OrderBy(x => x.SeqNo);

    public void DeleteByDocumentId(string documentId)
    {
        foreach (var chunk in Items.Where(x => x.DocumentId == documentId && !x.IsDelete))
        {
            chunk.IsDelete = true;
            chunk.DeletedAt = DateTime.UtcNow;
        }
    }
}

internal sealed class FakeLessonSlideNarrationRepository : ILessonSlideNarrationRepository
{
    public readonly List<LessonSlideNarration> Items = [];

    public IQueryable<LessonSlideNarration> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<LessonSlideNarration> FindBy(Expression<Func<LessonSlideNarration, bool>> predicate) => GetAll().Where(predicate);
    public LessonSlideNarration? Get(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    public Task<LessonSlideNarration?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(LessonSlideNarration entity) => Items.Add(entity);
    public void Update(LessonSlideNarration entity) { }
    public void Delete(LessonSlideNarration entity) => Items.Remove(entity);

    public IQueryable<LessonSlideNarration> GetByLessonId(string lessonId)
        => GetAll().Where(x => x.LessonId == lessonId);

    public LessonSlideNarration? GetOne(string lessonId, string slideObjectId)
        => GetAll().FirstOrDefault(x => x.LessonId == lessonId && x.SlideObjectId == slideObjectId);

    public int DeleteByLessonId(string lessonId)
    {
        var rows = Items.Where(x => x.LessonId == lessonId && !x.IsDelete).ToList();
        foreach (var row in rows)
        {
            row.IsDelete = true;
            row.DeletedAt = DateTime.UtcNow;
        }
        return rows.Count;
    }
}

internal sealed class FakeKnowledgeCategoryRepository : IKnowledgeCategoryRepository
{
    public readonly List<KnowledgeCategory> Items = [];
    public int QueryCount { get; private set; }
    public IQueryable<KnowledgeCategory> GetAll()
    {
        QueryCount++;
        return Items.AsQueryable().Where(x => !x.IsDelete);
    }
    public IQueryable<KnowledgeCategory> FindBy(Expression<Func<KnowledgeCategory, bool>> predicate)
    {
        QueryCount++;
        return Items.AsQueryable().Where(x => !x.IsDelete).Where(predicate);
    }
    public KnowledgeCategory? Get(string id)
    {
        QueryCount++;
        return Items.FirstOrDefault(x => x.Id == id && !x.IsDelete);
    }
    public Task<KnowledgeCategory?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(KnowledgeCategory entity) => Items.Add(entity);
    public void Update(KnowledgeCategory entity) { }
    public void Delete(KnowledgeCategory entity) => Items.Remove(entity);
    public IQueryable<KnowledgeCategory> GetByCompanyOrdered() => GetAll().OrderBy(x => x.Level).ThenBy(x => x.SortOrder).ThenBy(x => x.Name);
    public IQueryable<KnowledgeCategory> GetChildren(string parentId) => FindBy(x => x.ParentId == parentId);
    public KnowledgeCategory? GetSystemDefault() => FindBy(x => x.IsSystemDefault && x.Level == 2).SingleOrDefault();
}

internal sealed class FakeKnowledgeQnARepository : IKnowledgeQnARepository
{
    public readonly List<KnowledgeQnA> Items = [];

    public IQueryable<KnowledgeQnA> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<KnowledgeQnA> FindBy(Expression<Func<KnowledgeQnA, bool>> predicate) => GetAll().Where(predicate);
    public KnowledgeQnA? Get(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    public Task<KnowledgeQnA?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(KnowledgeQnA entity) => Items.Add(entity);
    public void Update(KnowledgeQnA entity) { }
    public void Delete(KnowledgeQnA entity) => Items.Remove(entity);

    public IQueryable<KnowledgeQnA> GetByScope(string scopeType, string? scopeId)
        => GetAll().Where(x => x.ScopeType == scopeType && x.ScopeId == scopeId);

    public IQueryable<KnowledgeQnA> Search(string keyword)
        => GetAll().Where(x => x.Question.Contains(keyword) || x.Answer.Contains(keyword));
}

internal sealed class FakeKnowledgeQnASourceRepository : IKnowledgeQnASourceRepository
{
    public readonly List<KnowledgeQnASource> Items = [];

    public IQueryable<KnowledgeQnASource> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<KnowledgeQnASource> FindBy(Expression<Func<KnowledgeQnASource, bool>> predicate) => GetAll().Where(predicate);
    public KnowledgeQnASource? Get(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    public Task<KnowledgeQnASource?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(KnowledgeQnASource entity) => Items.Add(entity);
    public void Update(KnowledgeQnASource entity) { }
    public void Delete(KnowledgeQnASource entity) => Items.Remove(entity);

    public IQueryable<KnowledgeQnASource> GetBySessionQuestionIds(IReadOnlyList<string> sessionQuestionIds)
        => GetAll().Where(x => sessionQuestionIds.Contains(x.SessionQuestionId));

    public IQueryable<KnowledgeQnASource> GetByQnAId(string qnaId)
        => GetAll().Where(x => x.QnAId == qnaId);
}

internal sealed class FakeKnowledgeQnAConflictRepository : IKnowledgeQnAConflictRepository
{
    public readonly List<KnowledgeQnAConflict> Items = [];

    public IQueryable<KnowledgeQnAConflict> GetAll() => Items.AsQueryable().Where(x => !x.IsDelete);
    public IQueryable<KnowledgeQnAConflict> FindBy(Expression<Func<KnowledgeQnAConflict, bool>> predicate) => GetAll().Where(predicate);
    public KnowledgeQnAConflict? Get(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    public Task<KnowledgeQnAConflict?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(KnowledgeQnAConflict entity) => Items.Add(entity);
    public void Update(KnowledgeQnAConflict entity) { }
    public void Delete(KnowledgeQnAConflict entity) => Items.Remove(entity);

    public IQueryable<KnowledgeQnAConflict> GetUnresolved() => GetAll().Where(x => x.ResolvedAt == null);
}

/// <summary>
/// ⚠️ Mirrors the real repository's most important property: NOTHING here is scoped by company,
/// because the real table has no query filter either (Company is the tenant registry). A fake that
/// helpfully filtered would hide exactly the bugs these tests exist to catch.
/// </summary>
internal sealed class FakeCompanyRepository : ICompanyRepository
{
    public readonly List<Company> Items = [];

    public IQueryable<Company> GetAll() => Items.AsQueryable();
    public IQueryable<Company> FindBy(Expression<Func<Company, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public Company? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<Company?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(Company entity) => Items.Add(entity);
    public void Update(Company entity) { }
    public void Delete(Company entity) => Items.Remove(entity);

    public IQueryable<Company> GetAllActive() => Items.AsQueryable().Where(x => x.IsActive).OrderBy(x => x.Name);
    public IQueryable<Company> GetAllIncludingInactive() => Items.AsQueryable().OrderBy(x => x.Name);
    public bool ExistsActive(string id) => Items.Any(x => x.Id == id && x.IsActive);
}

/// <summary>⚠️ Unscoped for the same reason as FakeCompanyRepository - see its note.</summary>
internal sealed class FakeAdminUserRepository : IAdminUserRepository
{
    public readonly List<AdminUser> Items = [];

    public IQueryable<AdminUser> GetAll() => Items.AsQueryable();
    public IQueryable<AdminUser> FindBy(Expression<Func<AdminUser, bool>> predicate) => Items.AsQueryable().Where(predicate);
    public AdminUser? Get(string id) => Items.FirstOrDefault(x => x.Id == id);
    public Task<AdminUser?> GetAsync(string id) => Task.FromResult(Get(id));
    public void Add(AdminUser entity) => Items.Add(entity);
    public void Update(AdminUser entity) { }
    public void Delete(AdminUser entity) => Items.Remove(entity);

    public AdminUser? GetByEmail(string email)
        => Items.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));

    public IQueryable<AdminUser> GetByCompanyId(string companyId)
        => Items.AsQueryable().Where(x => x.CompanyId == companyId).OrderBy(x => x.DisplayName);

    public int CountActiveAdmins(string companyId)
        => Items.Count(x => x.CompanyId == companyId && x.Role == AdminRole.Admin && x.IsActive);

    public int CountActiveOwners() => Items.Count(x => x.Role == AdminRole.Owner && x.IsActive);

    public bool IsEmpty() => Items.Count == 0;
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

    public LearningSession? GetLatestInProgressByLearnerKey(string trainingLinkId, string learnerKey)
        => Items.Where(x => x.TrainingLinkId == trainingLinkId
                && x.LearnerKey == learnerKey
                && x.Status == SessionStatus.InProgress)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefault();

    public LearningSession? GetLatestEndedByLearnerKey(string trainingLinkId, string learnerKey)
        => Items.Where(x => x.TrainingLinkId == trainingLinkId
                && x.LearnerKey == learnerKey
                && x.Status == SessionStatus.Ended)
            .OrderByDescending(x => x.EndedAt)
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

    public IQueryable<SessionQuestion> GetReviewQueue()
        => Items.AsQueryable().Where(x => x.AnswerStatus == AnswerStatus.NotFound || x.ReviewResult == ReviewResult.Incorrect);
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

    public Task<int> EmbedAndUpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeSourceChunk> chunks)
        => Task.FromResult(chunks.Count(c => !string.IsNullOrWhiteSpace(c.Text)));
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
    public Task DeleteVectorsAsync(string namespaceKey, IReadOnlyList<string> ids) => throw new NotSupportedException("not exercised in unit tests");
    public Task UpdateMetadataAsync(string namespaceKey, string id, string text, IReadOnlyDictionary<string, string>? metadata) => throw new NotSupportedException("not exercised in unit tests");
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
