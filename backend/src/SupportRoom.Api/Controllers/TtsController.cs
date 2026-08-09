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

    [HttpPost]
    public async Task<IActionResult> Synthesize([FromBody] SynthesizeSpeechDto input)
    {
        var result = await _service.SynthesizeAsync(input);
        Response.Headers.CacheControl = "no-store";
        return File(result.Audio, result.MimeType);
    }
}
