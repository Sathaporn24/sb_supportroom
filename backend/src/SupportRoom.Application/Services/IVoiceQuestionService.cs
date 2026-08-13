using SupportRoom.Providers.Knowledge;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Application.Services;

public interface IVoiceQuestionService
{
    Task<VoiceAnswerViewModel> AskAsync(AskVoiceQuestionDto input);
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
    IRealtimeNotifier realtimeNotifier)
    : ServiceBase<IVoiceQuestionService>(unitOfWork, serviceProvider, logger), IVoiceQuestionService
{
    public async Task<VoiceAnswerViewModel> AskAsync(AskVoiceQuestionDto input)
    {
        // Resolving the link from its token FIRST is what makes this request company-scoped:
        // it sets ICompanyContext, so the lesson lookup below can only ever see this company's
        // lessons. The lesson slug used to come straight from the caller alongside a separate
        // sessionId, with nothing checking the two belonged together - harmless while every slug
        // was globally unique, but a cross-company read the moment slugs are per-company.
        //
        // The learner key then picks WHICH person on that link is asking - the token stopped
        // identifying a person once one link started serving a whole department.
        var learningSessionService = ServiceProvider.GetRequiredService<ILearningSessionService>();
        var session = learningSessionService.GetEntityByLearnerKey(input.Token, input.LearnerKey);
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(input.Token);

        // Resolve slides through the single content-source-agnostic path so voice questions work
        // for BOTH Google-Slides and PDF lessons - this used to require lesson.PresentationId and
        // call the Slides provider directly, which 404'd every PDF-sourced lesson.
        var lessonService = ServiceProvider.GetRequiredService<ILessonConfigService>();
        LessonTeachingContentViewModel content;
        try
        {
            content = await lessonService.GetTeachingContentBySlugAsync(link.LessonSlug);
        }
        catch (HttpStatusCodeException)
        {
            throw;
        }

        try
        {
            var result = await voiceQuestionProvider.TranscribeAndAnswerAsync(new VoiceQuestionInput
            {
                Audio = input.Audio,
                MimeType = input.MimeType,
                DurationMs = input.DurationMs,
                LessonSlides = content.Slides.Select(s => new VoiceQuestionSlideContext { SlideObjectId = s.SlideObjectId, SpeakerNotes = s.SpeakerNotes }).ToList(),
                CurrentSlideObjectId = input.CurrentSlideObjectId,
                Expecting = input.Expecting,
                // Built here, from the company the session token resolved to - never inside the
                // provider. Vectors live outside PostgreSQL, so the query filter cannot protect
                // them; the namespace key is the only isolation the knowledge store has.
                LessonNamespace = KnowledgeNamespaces.For(CurrentCompanyId, link.LessonSlug),
                GlobalNamespace = KnowledgeNamespaces.ForGlobal(CurrentCompanyId),
            });

            // A yes/no about starting isn't a question the CS team needs to review.
            if (result.Readiness is not null)
            {
                Logger.LogInformation("Readiness check: session={SessionId} readiness={Readiness}", session.Id, result.Readiness);
                return result.Adapt<VoiceAnswerViewModel>();
            }

            // Never log Transcript/Answer - only the outcome.
            Logger.LogInformation("Voice question answered: session={SessionId} status={AnswerStatus}", session.Id, result.AnswerStatus);

            if (result.AnswerStatus != AnswerStatus.NoSpeech)
            {
                var questionService = ServiceProvider.GetRequiredService<ISessionQuestionService>();
                var question = questionService.Create(session.Id, new CreateSessionQuestionDto
                {
                    SlideObjectId = result.RelatedSlideObjectId ?? input.CurrentSlideObjectId,
                    Transcript = string.IsNullOrEmpty(result.Transcript) ? null : result.Transcript,
                    Answer = string.IsNullOrEmpty(result.Answer) ? null : result.Answer,
                    AnswerStatus = result.AnswerStatus,
                });

                await realtimeNotifier.NotifyNewQuestionAsync(session.Id, question);
            }

            return result.Adapt<VoiceAnswerViewModel>();
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
}
