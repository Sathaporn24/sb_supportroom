using SupportRoom.Providers.Knowledge;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Application.Services;

public interface IVoiceQuestionService
{
    Task<VoiceAnswerViewModel> AskAsync(AskVoiceQuestionDto input);

    /// <summary>F10 - the typed-question counterpart to AskAsync, sharing the same core:
    /// resolve session/company (IC-1) -> reject if Ended -> resolve lesson content -> answer via the
    /// provider -> record + broadcast (TQ-4). "Equivalent to voice 100%" (T1) means this must never
    /// diverge from AskAsync's orchestration except at the one point (transcription) that a typed
    /// question skips by construction.</summary>
    Task<VoiceAnswerViewModel> AskTextAsync(AskTextQuestionDto input);
}

/// <summary>
/// Orchestrates the record -> upload -> ground -> answer pipeline - mirrors
/// src/app/api/voice-question/route.ts exactly.
/// </summary>
public sealed class VoiceQuestionService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IVoiceQuestionService> logger,
    IVoiceQuestionProvider voiceQuestionProvider,
    IRealtimeNotifier realtimeNotifier,
    IKnowledgeNamespaceResolver knowledgeNamespaceResolver)
    : ServiceBase<IVoiceQuestionService>(unitOfWork, serviceProvider, logger), IVoiceQuestionService
{
    private sealed record ResolvedContext(LearningSession Session, TrainingLink Link, LessonTeachingContentViewModel Content);

    /// <summary>TQ-4 step 1-3, shared by both AskAsync and AskTextAsync: resolving the link from its
    /// token FIRST is what makes this request company-scoped (it sets ICompanyContext, so the lesson
    /// lookup below can only ever see this company's lessons). The learner key then picks WHICH
    /// person on that link is asking - the token stopped identifying a person once one link started
    /// serving a whole department.</summary>
    private async Task<ResolvedContext> ResolveContextAsync(string token, string learnerKey)
    {
        var learningSessionService = ServiceProvider.GetRequiredService<ILearningSessionService>();
        var session = learningSessionService.GetEntityByLearnerKey(token, learnerKey);
        if (session.Status == SessionStatus.Ended)
        {
            throw GeneralException.ValidationError("การเรียนนี้จบแล้ว กรุณากดเรียนอีกครั้งก่อนถามคำถามใหม่");
        }
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(token);

        // Resolve slides through the single content-source-agnostic path so questions work for
        // BOTH Google-Slides and PDF lessons - this used to require lesson.PresentationId and call
        // the Slides provider directly, which 404'd every PDF-sourced lesson.
        var lessonService = ServiceProvider.GetRequiredService<ILessonConfigService>();
        var content = await lessonService.GetTeachingContentBySlugAsync(link.LessonSlug);

        return new ResolvedContext(session, link, content);
    }

    /// <summary>Namespaces are built here, from the company the session token resolved to - never
    /// inside the provider (KS-1). Vectors live outside PostgreSQL, so the query filter cannot
    /// protect them; the namespace key is the only isolation the knowledge store has.</summary>
    private (string LessonNamespace, string CategoryNamespace, string GlobalNamespace) ResolveNamespaces(TrainingLink link, LessonTeachingContentViewModel content)
        => (
            KnowledgeNamespaces.For(CurrentCompanyId, link.LessonSlug),
            knowledgeNamespaceResolver.Resolve(CurrentCompanyId, KnowledgeScopeType.Category, content.Lesson.CategoryId),
            KnowledgeNamespaces.ForGlobal(CurrentCompanyId));

    public async Task<VoiceAnswerViewModel> AskAsync(AskVoiceQuestionDto input)
    {
        var context = await ResolveContextAsync(input.Token, input.LearnerKey);
        var (lessonNamespace, categoryNamespace, globalNamespace) = ResolveNamespaces(context.Link, context.Content);

        try
        {
            var result = await voiceQuestionProvider.TranscribeAndAnswerAsync(new VoiceQuestionInput
            {
                Audio = input.Audio,
                MimeType = input.MimeType,
                DurationMs = input.DurationMs,
                LessonSlides = context.Content.Slides.Select(s => new VoiceQuestionSlideContext { SlideObjectId = s.SlideObjectId, SpeakerNotes = s.SpeakerNotes }).ToList(),
                CurrentSlideObjectId = input.CurrentSlideObjectId,
                LessonNamespace = lessonNamespace,
                CategoryNamespace = categoryNamespace,
                GlobalNamespace = globalNamespace,
            });

            // Never log Transcript/Answer - only the outcome.
            Logger.LogInformation("Voice question answered: session={SessionId} status={AnswerStatus}", context.Session.Id, result.AnswerStatus);

            if (result.AnswerStatus == AnswerStatus.NoSpeech)
            {
                return result.Adapt<VoiceAnswerViewModel>();
            }

            return await RecordAndBroadcastAsync(context.Session, result, input.CurrentSlideObjectId, QuestionSource.Voice);
        }
        catch (HttpStatusCodeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }

    public async Task<VoiceAnswerViewModel> AskTextAsync(AskTextQuestionDto input)
    {
        var context = await ResolveContextAsync(input.Token, input.LearnerKey);
        var (lessonNamespace, categoryNamespace, globalNamespace) = ResolveNamespaces(context.Link, context.Content);

        try
        {
            var result = await voiceQuestionProvider.AnswerTextAsync(new TextQuestionInput
            {
                QuestionText = input.Text,
                LessonSlides = context.Content.Slides.Select(s => new VoiceQuestionSlideContext { SlideObjectId = s.SlideObjectId, SpeakerNotes = s.SpeakerNotes }).ToList(),
                CurrentSlideObjectId = input.CurrentSlideObjectId,
                LessonNamespace = lessonNamespace,
                CategoryNamespace = categoryNamespace,
                GlobalNamespace = globalNamespace,
            });

            // TQ-12 - never log the question text itself, only the outcome.
            Logger.LogInformation("Text question answered: session={SessionId} status={AnswerStatus}", context.Session.Id, result.AnswerStatus);

            return await RecordAndBroadcastAsync(context.Session, result, input.CurrentSlideObjectId, QuestionSource.Text);
        }
        catch (HttpStatusCodeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // TQ-10 - the provider throws rather than returning transcription_failed for a typed
            // question, so this catch-all never runs for that case; anything else that lands here
            // (upstream outage, malformed JSON) must also never write a SessionQuestion row - there
            // is nothing at this point to write yet, unlike the voice path's NoSpeech short-circuit.
            throw GeneralException.UpstreamError(ex.Message);
        }
    }

    /// <summary>TQ-5/TQ-6 - the part that must be "identical in every particular" (T2) once a
    /// question has an answer, regardless of which channel produced it: one SessionQuestion row,
    /// tagged with the channel it came from, broadcast to the same group, with the same
    /// Q&amp;A-conflict handling.</summary>
    private async Task<VoiceAnswerViewModel> RecordAndBroadcastAsync(LearningSession session, VoiceQuestionResult result, string? currentSlideObjectId, string source)
    {
        var questionService = ServiceProvider.GetRequiredService<ISessionQuestionService>();
        var question = questionService.Create(session.Id, new CreateSessionQuestionDto
        {
            SlideObjectId = result.RelatedSlideObjectId ?? currentSlideObjectId,
            Transcript = string.IsNullOrEmpty(result.Transcript) ? null : result.Transcript,
            Answer = string.IsNullOrEmpty(result.Answer) ? null : result.Answer,
            AnswerStatus = result.AnswerStatus,
            Source = source,
        });

        await realtimeNotifier.NotifyNewQuestionAsync(session.Id, question);

        if (result.Conflict is not null)
        {
            TryRecordConflict(question.Id, result.Conflict);
        }

        return result.Adapt<VoiceAnswerViewModel>();
    }

    /// <summary>KS-9/KS-10 - the model can hallucinate a qnaId, so this is validated against
    /// IKnowledgeQnARepository (already company-scoped by CurrentCompanyId's query filter) before
    /// anything is written. Recording the flag must never fail the answer that already succeeded -
    /// same "an integration failure degrades, never blocks the main flow" convention as everywhere
    /// else in this codebase (KS-9, R6.4's log-warning-and-continue pattern).</summary>
    private void TryRecordConflict(string sessionQuestionId, VoiceQuestionConflictResult conflict)
    {
        try
        {
            var qnaRepository = UnitOfWork.GetRepository<IKnowledgeQnARepository>();
            var qna = qnaRepository.Get(conflict.QnAId);
            if (qna is null)
            {
                Logger.LogWarning("Discarded a reported Q&A conflict for an unknown or foreign qnaId {QnAId}", conflict.QnAId);
                return;
            }

            var conflictRepository = UnitOfWork.GetRepository<IKnowledgeQnAConflictRepository>();
            conflictRepository.Add(new KnowledgeQnAConflict
            {
                Id = IdGenerator.GenerateId("qnacf"),
                CompanyId = CurrentCompanyId,
                CreateDate = DateTime.UtcNow,
                QnAId = qna.Id,
                SessionQuestionId = sessionQuestionId,
                ConflictingSourceLabel = Truncate(conflict.SourceLabel, DtoLimits.ConflictSourceLabelMaxLength),
                ModelNote = conflict.Note is null ? null : Truncate(conflict.Note, DtoLimits.ConflictNoteMaxLength),
            });
            UnitOfWork.Commit();

            Logger.LogInformation("Q&A conflict recorded: qnaId={QnAId} sessionQuestion={SessionQuestionId}", qna.Id, sessionQuestionId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record Q&A conflict flag for session question {SessionQuestionId}", sessionQuestionId);
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
