using System.Globalization;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface IKnowledgeQnAService
{
    /// <summary>P8/R5.1/QQ-1/QQ-4 - the whole review queue in one call, across every learning
    /// session and lesson.</summary>
    IReadOnlyList<KnowledgeQnAQueueItemViewModel> GetQueue();

    /// <summary>KL-8/KL-9 - the library view's Q&A table, same shape and semantics as
    /// IDocumentResourceService.GetByScope (KL-2..KL-5, KL-11..KL-13). Explicitly authenticated
    /// inside the implementation (KL-10) - not left to the query filter alone, same reasoning as
    /// GetChunks (DI-7).</summary>
    IReadOnlyList<KnowledgeQnAViewModel> GetAll(KnowledgeQnAFilter filter);

    /// <summary>KL-23/Q-H2 - pre-save gate: throws GeneralException.Conflict (409, details =
    /// DuplicateQnAResponse) before any write when an existing, non-deleted Q&A of this company
    /// already has the same Question (trim + whitespace-collapse + case-insensitive). Unconditional
    /// on every create unless input.ConfirmDuplicate is true.</summary>
    Task<KnowledgeQnAViewModel> CreateAsync(CreateKnowledgeQnADto input);
    Task<KnowledgeQnAViewModel> UpdateAsync(string id, UpdateKnowledgeQnADto input);
    Task DeleteAsync(string id);
}

/// <summary>
/// R5's core: CS writes a question-answer pair directly at the point they hit a gap, instead of
/// authoring a document and waiting for it to be indexed. Saving one enqueues qna_index (KS-5:
/// EmbedText = Question only, Text = the full "ถาม: ...\nตอบ: ..." pair) and closes every selected
/// queue question by writing a KnowledgeQnASource row per question (QQ-7) - cs writes are usable
/// immediately, no approval step (R5.7).
/// </summary>
public sealed class KnowledgeQnAService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IKnowledgeQnAService> logger,
    IAuthorizationGuard guard,
    IKnowledgeNamespaceResolver namespaceResolver)
    : ServiceBase<IKnowledgeQnAService>(unitOfWork, serviceProvider, logger), IKnowledgeQnAService
{
    private readonly IKnowledgeQnARepository _repository = unitOfWork.GetRepository<IKnowledgeQnARepository>();
    private readonly IKnowledgeQnASourceRepository _sourceRepository = unitOfWork.GetRepository<IKnowledgeQnASourceRepository>();
    private readonly ISessionQuestionRepository _sessionQuestionRepository = unitOfWork.GetRepository<ISessionQuestionRepository>();
    private readonly ILearningSessionRepository _learningSessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();
    private readonly ITrainingLinkRepository _trainingLinkRepository = unitOfWork.GetRepository<ITrainingLinkRepository>();
    private readonly ILessonConfigRepository _lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();

    public IReadOnlyList<KnowledgeQnAQueueItemViewModel> GetQueue()
    {
        // QQ-1: AnswerStatus/ReviewResult filter happens in the repository; the "no Q&A already
        // closes it" half happens here, as one batched lookup (GetBySessionQuestionIds) rather
        // than a query per candidate.
        var candidates = _sessionQuestionRepository.GetReviewQueue().ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var candidateIds = candidates.Select(c => c.Id).ToList();
        var alreadyAnswered = _sourceRepository.GetBySessionQuestionIds(candidateIds)
            .Select(s => s.SessionQuestionId)
            .ToHashSet();
        var open = candidates.Where(c => !alreadyAnswered.Contains(c.Id)).ToList();
        if (open.Count == 0)
        {
            return [];
        }

        // QQ-4: join to LessonSlug through LearningSession -> TrainingLink, two batched queries
        // rather than N+1 - SessionQuestion itself never carries a lesson id (see design.md R-9).
        var sessionIds = open.Select(q => q.SessionId).Distinct().ToList();
        var sessions = _learningSessionRepository.FindBy(s => sessionIds.Contains(s.Id)).ToList()
            .ToDictionary(s => s.Id);

        var linkIds = sessions.Values.Select(s => s.TrainingLinkId).Distinct().ToList();
        var links = _trainingLinkRepository.FindBy(l => linkIds.Contains(l.Id)).ToList()
            .ToDictionary(l => l.Id);

        return open
            .OrderBy(q => q.CreateDate)
            .Select(q =>
            {
                sessions.TryGetValue(q.SessionId, out var session);
                TrainingLink? link = session is not null ? links.GetValueOrDefault(session.TrainingLinkId) : null;

                return new KnowledgeQnAQueueItemViewModel
                {
                    Id = q.Id,
                    SessionId = q.SessionId,
                    SlideObjectId = q.SlideObjectId,
                    Transcript = q.Transcript,
                    Answer = q.Answer,
                    AnswerStatus = q.AnswerStatus,
                    ReviewResult = q.ReviewResult,
                    ReviewNote = q.ReviewNote,
                    CreatedAt = q.CreateDate.ToString("O", CultureInfo.InvariantCulture),
                    LessonId = link?.LessonId,
                    LessonSlug = link?.LessonSlug,
                    // QQ-3 - independent flags, not one source enum: a question can be both.
                    FromNotFound = q.AnswerStatus == AnswerStatus.NotFound,
                    FromIncorrect = q.ReviewResult == ReviewResult.Incorrect,
                };
            })
            .ToList();
    }

    public IReadOnlyList<KnowledgeQnAViewModel> GetAll(KnowledgeQnAFilter filter)
    {
        // KL-10 - explicit, not left to the query filter alone: this is the second endpoint in the
        // system (after DI-7's chunks) that returns the knowledge base's actual text content.
        guard.EnsureAuthenticated();

        var query = ResolveScopeQuery(filter.ScopeType, filter.ScopeId);

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(x => x.IndexingStatus == filter.Status);
        }

        var normalizedQuery = KnowledgeLibrarySearch.Normalize(filter.Q);
        if (normalizedQuery is not null)
        {
            // KL-11 - Question or Answer matches; EF.Functions.ILike keeps this parameterized.
            var pattern = $"%{normalizedQuery}%";
            query = query.Where(x => EF.Functions.ILike(x.Question, pattern) || EF.Functions.ILike(x.Answer, pattern));
        }

        return query.OrderByDescending(x => x.CreateDate).ToList().Select(x => x.Adapt<KnowledgeQnAViewModel>()).ToList();
    }

    private IQueryable<KnowledgeQnA> ResolveScopeQuery(string? scopeType, string? scopeId)
    {
        // KL-2 (via KL-9's "same shape as documents") - no scopeType sent = every scope.
        if (string.IsNullOrEmpty(scopeType))
        {
            return _repository.GetAllInCompany();
        }

        // KL-5 - a category filter also surfaces the Q&A of every lesson under it.
        if (scopeType == KnowledgeScopeType.Category && !string.IsNullOrEmpty(scopeId))
        {
            var lessonIdsInCategory = _lessonConfigRepository.GetByCategoryId(scopeId).Select(l => l.Id);
            return _repository.FindBy(x =>
                (x.ScopeType == KnowledgeScopeType.Category && x.ScopeId == scopeId)
                || (x.ScopeType == KnowledgeScopeType.Lesson && lessonIdsInCategory.Contains(x.ScopeId)));
        }

        return _repository.GetByScope(scopeType, scopeId);
    }

    public Task<KnowledgeQnAViewModel> CreateAsync(CreateKnowledgeQnADto input)
    {
        var question = NormalizeQuestion(input.Question);
        var answer = NormalizeAnswer(input.Answer);

        // KS-2/TX-5 - the single resolver validates the scope pair (and, for "category", that it
        // is a Level-2 leaf) before anything is written. 400/404 must win over 409, so this and
        // the SessionQuestionIds checks below run before the duplicate gate (Q-H2 fixed order).
        namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId);

        if (input.SessionQuestionIds.Count == 0)
        {
            throw GeneralException.ValidationError("ต้องเลือกอย่างน้อยหนึ่งคำถามจากคิว");
        }

        // Batched existence check - one query, not one Get() per selected id (QQ-7 can select
        // several).
        var ids = input.SessionQuestionIds.Distinct().ToList();
        var found = _sessionQuestionRepository.FindBy(q => ids.Contains(q.Id)).Select(q => q.Id).ToHashSet();
        var missing = ids.Where(id => !found.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            throw GeneralException.NotFound($"คำถาม ({string.Join(", ", missing)})");
        }

        // KL-23/Q-H2 - pre-save gate, unconditional unless ConfirmDuplicate: computed before any
        // write (no row, no KnowledgeQnASource, no queue close, no queued job, no Commit). A 409
        // here must leave the database exactly as it was.
        if (!input.ConfirmDuplicate)
        {
            var duplicates = FindDuplicateQuestions(question);
            if (duplicates.Count > 0)
            {
                throw GeneralException.Conflict(
                    "พบคำถามที่ซ้ำกับที่มีอยู่แล้วในคลัง",
                    new DuplicateQnAResponse
                    {
                        DuplicateByQuestion = duplicates
                            .OrderByDescending(x => x.CreateDate)
                            .Select(x => x.Adapt<KnowledgeQnAViewModel>())
                            .ToList(),
                    });
            }
        }

        // DM-6: "id ใน Pinecone = Id ของแถวนี้ตรงๆ" - VectorId and Id must be identical, generated
        // once, not two independent ids.
        var id = IdGenerator.GenerateId("qna");
        var entity = new KnowledgeQnA
        {
            Id = id,
            CompanyId = CurrentCompanyId,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
            Question = question,
            Answer = answer,
            ScopeType = input.ScopeType,
            ScopeId = input.ScopeId,
            VectorId = id,
            IndexingStatus = DocumentIndexingStatus.Pending,
        };
        _repository.Add(entity);

        foreach (var sessionQuestionId in ids)
        {
            _sourceRepository.Add(new KnowledgeQnASource
            {
                Id = IdGenerator.GenerateId("qnasrc"),
                CompanyId = CurrentCompanyId,
                CreateBy = CurrentUserId,
                CreateDate = DateTime.UtcNow,
                QnAId = entity.Id,
                SessionQuestionId = sessionQuestionId,
            });
        }

        EnqueueJob(BackgroundJobType.QnaIndex, entity.Id, JsonSerializer.Serialize(new QnaIndexJobPayload { NeedsReEmbed = true }));
        UnitOfWork.Commit();

        Logger.LogInformation("Q&A created: {QnAId} scope={ScopeType} closes={Count} questions", entity.Id, entity.ScopeType, ids.Count);

        return Task.FromResult(entity.Adapt<KnowledgeQnAViewModel>());
    }

    /// <summary>KL-23 - scoped to CurrentCompanyId explicitly in the predicate (same
    /// defence-in-depth reasoning as DocumentResourceService's KL-19/KL-20 checks), Question only,
    /// scope-agnostic. Whitespace-collapse happens in memory rather than via SQL regex, for the
    /// same fakes-testability reason documented on DocumentResourceService.FindDuplicates.</summary>
    private List<KnowledgeQnA> FindDuplicateQuestions(string normalizedQuestion)
    {
        var collapsed = CollapseWhitespace(normalizedQuestion);
        return _repository.FindBy(x => x.CompanyId == CurrentCompanyId)
            .ToList()
            .Where(x => string.Equals(CollapseWhitespace(x.Question), collapsed, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string CollapseWhitespace(string value)
        => System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ");

    public Task<KnowledgeQnAViewModel> UpdateAsync(string id, UpdateKnowledgeQnADto input)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("Q&A");

        var question = NormalizeQuestion(input.Question);
        var answer = NormalizeAnswer(input.Answer);

        // QQ-6 - editing never puts the closed question(s) back in the queue; only whether the
        // Question text changed decides if re-embedding is worth paying for.
        var questionChanged = question != entity.Question;
        var answerChanged = answer != entity.Answer;

        entity.Question = question;
        entity.Answer = answer;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateDate = DateTime.UtcNow;

        if (questionChanged || answerChanged)
        {
            entity.IndexingStatus = DocumentIndexingStatus.Pending;
            _repository.Update(entity);
            EnqueueJob(BackgroundJobType.QnaIndex, entity.Id, JsonSerializer.Serialize(new QnaIndexJobPayload { NeedsReEmbed = questionChanged }));
        }
        else
        {
            _repository.Update(entity);
        }

        UnitOfWork.Commit();

        Logger.LogInformation("Q&A updated: {QnAId} questionChanged={QuestionChanged} answerChanged={AnswerChanged}", id, questionChanged, answerChanged);

        return Task.FromResult(entity.Adapt<KnowledgeQnAViewModel>());
    }

    public Task DeleteAsync(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("Q&A");

        // QQ-5 - every KnowledgeQnASource pointing at this Q&A is soft-deleted in the same
        // transaction, so the question(s) it used to close fall back into the review queue via
        // QQ-1 the moment this commits. That is the intended behaviour, not a side effect.
        foreach (var source in _sourceRepository.GetByQnAId(id))
        {
            source.IsDelete = true;
            source.DeletedAt = DateTime.UtcNow;
            _sourceRepository.Update(source);
        }

        if (entity.IndexedNamespaceKey is not null)
        {
            EnqueueJob(BackgroundJobType.VectorDelete, id, JsonSerializer.Serialize(new VectorDeleteJobPayload
            {
                Kind = VectorDeleteTargetKind.Qna,
                NamespaceKey = entity.IndexedNamespaceKey,
                VectorIds = [entity.VectorId],
            }));
        }

        entity.IsDelete = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeleteBy = CurrentUserId;
        _repository.Update(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Q&A soft-deleted: {QnAId}", id);
        return Task.CompletedTask;
    }

    private void EnqueueJob(string jobType, string targetId, string? payloadJson = null)
    {
        var jobRepository = UnitOfWork.GetRepository<IBackgroundJobRepository>();
        jobRepository.Add(new BackgroundJob
        {
            Id = IdGenerator.GenerateId("job"),
            CompanyId = CurrentCompanyId,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
            JobType = jobType,
            TargetId = targetId,
            PayloadJson = payloadJson,
            Status = BackgroundJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = DateTime.UtcNow,
        });
    }

    private static string NormalizeQuestion(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length is < 1 or > DtoLimits.QnAQuestionMaxLength)
        {
            throw GeneralException.ValidationError($"คำถามต้องมีความยาว 1-{DtoLimits.QnAQuestionMaxLength} ตัวอักษร");
        }
        return trimmed;
    }

    private static string NormalizeAnswer(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length is < 1 or > DtoLimits.QnAAnswerMaxLength)
        {
            throw GeneralException.ValidationError($"คำตอบต้องมีความยาว 1-{DtoLimits.QnAAnswerMaxLength} ตัวอักษร");
        }
        return trimmed;
    }
}
