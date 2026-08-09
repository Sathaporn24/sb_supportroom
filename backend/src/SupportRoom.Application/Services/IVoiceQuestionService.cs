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
    private readonly ITrainingSessionRepository _sessionRepository = unitOfWork.GetRepository<ITrainingSessionRepository>();

    public async Task<VoiceAnswerViewModel> AskAsync(AskVoiceQuestionDto input)
    {
        // Resolve slides through the single content-source-agnostic path so voice questions work
        // for BOTH Google-Slides and PDF lessons - this used to require lesson.PresentationId and
        // call the Slides provider directly, which 404'd every PDF-sourced lesson.
        var lessonService = ServiceProvider.GetRequiredService<ILessonConfigService>();
        LessonTeachingContentViewModel content;
        try
        {
            content = await lessonService.GetTeachingContentBySlugAsync(input.LessonSlug);
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
                LessonSlug = input.LessonSlug,
            });

            // A yes/no about starting isn't a question the CS team needs to review.
            if (result.Readiness is not null)
            {
                Logger.LogInformation("Readiness check: session={SessionId} readiness={Readiness}", input.SessionId, result.Readiness);
                return result.Adapt<VoiceAnswerViewModel>();
            }

            // Never log Transcript/Answer - only the outcome.
            Logger.LogInformation("Voice question answered: session={SessionId} status={AnswerStatus}", input.SessionId, result.AnswerStatus);

            if (result.AnswerStatus != AnswerStatus.NoSpeech)
            {
                var questionService = ServiceProvider.GetRequiredService<ISessionQuestionService>();
                var question = questionService.Create(input.SessionId, new CreateSessionQuestionDto
                {
                    SlideObjectId = result.RelatedSlideObjectId ?? input.CurrentSlideObjectId,
                    Transcript = string.IsNullOrEmpty(result.Transcript) ? null : result.Transcript,
                    Answer = string.IsNullOrEmpty(result.Answer) ? null : result.Answer,
                    AnswerStatus = result.AnswerStatus,
                });

                var session = _sessionRepository.Get(input.SessionId);
                if (session is not null)
                {
                    await realtimeNotifier.NotifyNewQuestionAsync(session.Token, question);
                }
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
