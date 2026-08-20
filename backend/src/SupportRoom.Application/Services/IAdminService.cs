using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.DocumentParsing;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

public sealed class ReindexResult
{
    public required int LessonsIndexed { get; init; }
    public required int DocumentsIndexed { get; init; }
    public required int DocumentsFailed { get; init; }
}

public interface IAdminService
{
    /// <summary>Deletes all TrainingLink/LearningSession/SessionQuestion rows - keeps LessonConfig.</summary>
    void ResetDemoData();

    /// <summary>Rebuilds the entire RAG index from source (stored lesson decks + document files).
    /// Needed after any change to how content is embedded or chunked - e.g. switching Gemini
    /// embeddings to retrieval task types leaves every previously-stored vector in an incompatible
    /// embedding space, so they must be re-embedded and re-upserted. Clears each namespace first so
    /// stale vectors (a removed slide, an old embedding space) don't linger alongside the fresh ones.</summary>
    Task<ReindexResult> ReindexAllAsync();
}

public sealed class AdminService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IAdminService> logger,
    IAuthorizationGuard guard,
    IDocumentStorageProvider storageProvider,
    IKnowledgeIndexingService knowledgeIndexingService,
    IKnowledgeIndexProvider knowledgeIndexProvider,
    ISlidesProvider slidesProvider)
    : ServiceBase<IAdminService>(unitOfWork, serviceProvider, logger), IAdminService
{
    /// <summary>
    /// Both operations here are destructive and act on whatever company the request resolved to,
    /// so they are owner-only (TD-014). A customer's own admin must never be able to wipe data or
    /// trigger a full re-index - the latter also spends real money on embeddings.
    ///
    /// The ALLOW_DATA_RESET switch stays as a second, independent lock: being an owner is about
    /// who you are, that flag is about whether this deployment is a demo at all.
    /// </summary>
    private void EnsureAllowed()
    {
        guard.EnsureOwner();

        if (Environment.GetEnvironmentVariable("ALLOW_DATA_RESET") != "true")
        {
            throw GeneralException.ConfigError("การดำเนินการนี้ถูกปิดใช้งาน - ตั้ง ALLOW_DATA_RESET=true เพื่อเปิดใช้ (ใช้เฉพาะฐานข้อมูล Demo เท่านั้น)");
        }
    }

    public void ResetDemoData()
    {
        EnsureAllowed();

        var questionRepository = UnitOfWork.GetRepository<ISessionQuestionRepository>();
        var questions = questionRepository.GetAll().ToList();
        foreach (var question in questions)
        {
            questionRepository.Delete(question);
        }

        var chatRepository = UnitOfWork.GetRepository<IChatMessageRepository>();
        var chatMessages = chatRepository.GetAll().ToList();
        foreach (var chatMessage in chatMessages)
        {
            chatRepository.Delete(chatMessage);
        }

        var learningSessionRepository = UnitOfWork.GetRepository<ILearningSessionRepository>();
        var learningSessions = learningSessionRepository.GetAll().ToList();
        foreach (var learningSession in learningSessions)
        {
            learningSessionRepository.Delete(learningSession);
        }

        var linkRepository = UnitOfWork.GetRepository<ITrainingLinkRepository>();
        var links = linkRepository.GetAll().ToList();
        foreach (var link in links)
        {
            linkRepository.Delete(link);
        }

        UnitOfWork.Commit();

        Logger.LogWarning(
            "Demo data reset: deleted {LinkCount} links, {LearningSessionCount} learning sessions, {QuestionCount} questions, {ChatCount} chat messages",
            links.Count, learningSessions.Count, questions.Count, chatMessages.Count);
    }

    public async Task<ReindexResult> ReindexAllAsync()
    {
        EnsureAllowed();

        var lessonRepository = UnitOfWork.GetRepository<ILessonConfigRepository>();
        var documentRepository = UnitOfWork.GetRepository<IDocumentResourceRepository>();

        var lessons = lessonRepository.GetAll().ToList();
        var documents = documentRepository.GetAll().ToList();

        // 1. Clear every namespace about to be rebuilt so stale vectors don't survive alongside the
        //    fresh ones - both content that no longer exists (a removed slide) and, the reason this
        //    exists, the entire pre-task-type embedding space. A delete-all on a namespace that was
        //    never created 404s (see Pinecone provider) - harmless here, so it's logged and skipped.
        var namespaces = new HashSet<string>(StringComparer.Ordinal) { KnowledgeNamespaces.ForGlobal(CurrentCompanyId) };
        foreach (var lesson in lessons)
        {
            namespaces.Add(KnowledgeNamespaces.For(CurrentCompanyId, lesson.Slug));
        }
        foreach (var namespaceKey in namespaces)
        {
            try
            {
                await knowledgeIndexProvider.DeleteNamespaceAsync(namespaceKey);
            }
            catch (Exception ex)
            {
                Logger.LogInformation("Reindex: skipped clearing namespace {Namespace} ({Reason})", namespaceKey, ex.Message);
            }
        }

        // 2. Google-Slides lessons re-index from the live deck. PDF-source lessons have no slides of
        //    their own - their teaching content is the attached document, re-indexed in step 3.
        var lessonsIndexed = 0;
        foreach (var lesson in lessons.Where(l => !string.IsNullOrEmpty(l.PresentationId)))
        {
            var content = await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = lesson.PresentationId! });
            await knowledgeIndexingService.IndexLessonAsync(KnowledgeNamespaces.For(CurrentCompanyId, lesson.Slug), content.Slides);
            lessonsIndexed++;
        }

        // 3. Every document re-indexes from its stored bytes (never change for an id) into its
        //    correct namespace: attached -> that lesson's slug, standalone -> kb-global.
        var lessonSlugById = lessons.ToDictionary(l => l.Id, l => l.Slug);
        var documentsIndexed = 0;
        var documentsFailed = 0;
        foreach (var document in documents)
        {
            try
            {
                var namespaceKey = document.ScopeType == KnowledgeScopeType.Lesson
                    && document.ScopeId is not null
                    && lessonSlugById.TryGetValue(document.ScopeId, out var slug)
                    ? KnowledgeNamespaces.For(CurrentCompanyId, slug)
                    : KnowledgeNamespaces.ForGlobal(CurrentCompanyId);

                byte[] bytes;
                await using (var stream = await storageProvider.DownloadAsync(document.ObsKey))
                using (var buffer = new MemoryStream())
                {
                    await stream.CopyToAsync(buffer);
                    bytes = buffer.ToArray();
                }

                var extractor = DocumentParserFactory.Create(document.ContentType, document.FileName);
                using var parseStream = new MemoryStream(bytes);
                var chunks = extractor.Extract(parseStream).Select(c => new KnowledgeSourceChunk
                {
                    Id = $"{document.Id}-{c.ChunkId}",
                    Text = c.Text,
                    Metadata = new Dictionary<string, string>
                    {
                        ["documentId"] = document.Id,
                        ["chunkId"] = c.ChunkId,
                        ["fileName"] = document.FileName,
                        ["sourceType"] = KnowledgeSourceType.Document,
                    },
                }).ToList();

                var indexedCount = await knowledgeIndexingService.IndexChunksAsync(namespaceKey, chunks);
                document.IndexingStatus = indexedCount > 0 ? DocumentIndexingStatus.Indexed : DocumentIndexingStatus.Failed;
                document.IndexedChunkCount = indexedCount;
                documentRepository.Update(document);

                if (indexedCount > 0)
                {
                    documentsIndexed++;
                }
                else
                {
                    documentsFailed++;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Reindex failed for document {DocumentId}", document.Id);
                document.IndexingStatus = DocumentIndexingStatus.Failed;
                documentRepository.Update(document);
                documentsFailed++;
            }
        }

        UnitOfWork.Commit();

        Logger.LogWarning(
            "Reindex complete: {LessonsIndexed} lessons, {DocumentsIndexed} documents indexed, {DocumentsFailed} failed",
            lessonsIndexed, documentsIndexed, documentsFailed);

        return new ReindexResult
        {
            LessonsIndexed = lessonsIndexed,
            DocumentsIndexed = documentsIndexed,
            DocumentsFailed = documentsFailed,
        };
    }
}
