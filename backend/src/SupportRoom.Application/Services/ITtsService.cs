using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Tts;

namespace SupportRoom.Application.Services;

public interface ITtsService
{
    Task<TtsResult> SynthesizeAsync(SynthesizeSpeechDto input);
}

public sealed class TtsService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<ITtsService> logger, ITtsProvider ttsProvider)
    : ServiceBase<ITtsService>(unitOfWork, serviceProvider, logger), ITtsService
{
    public async Task<TtsResult> SynthesizeAsync(SynthesizeSpeechDto input)
    {
        try
        {
            // Never log the full speaker-notes/answer text here - message only.
            return await ttsProvider.SynthesizeAsync(new Providers.Tts.TtsInput { Text = input.Text, Voice = input.Voice, Rate = input.Rate });
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }
}
