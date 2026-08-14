using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/tts")]
public sealed class TtsController : ControllerBase
{
    private readonly ITtsService _service;

    public TtsController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ITtsService>();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Synthesize([FromBody] SynthesizeSpeechDto input)
    {
        // TTS costs real provider quota. Binding every call to an existing learner session keeps
        // the anonymous endpoint usable by recipients without turning it into a public speech API.
        var session = HttpContext.RequestServices.GetRequiredService<ILearningSessionService>()
            .GetEntityByLearnerKey(input.Token!, input.LearnerKey!);
        if (session.Status == SupportRoom.Domain.Enums.SessionStatus.Ended)
        {
            throw SupportRoom.Application.Exceptions.GeneralException.ValidationError(
                "การเรียนนี้จบแล้ว กรุณากดเรียนอีกครั้งก่อนสร้างเสียงใหม่");
        }

        var result = await _service.SynthesizeAsync(input);
        Response.Headers.CacheControl = "no-store";
        return File(result.Audio, result.MimeType);
    }
}
