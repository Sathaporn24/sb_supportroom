using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/slides")]
public sealed class SlidesController : ControllerBase
{
    private readonly ISlidesService _service;

    public SlidesController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ISlidesService>();
    }

    [HttpPost("resolve")]
    public async Task<ActionResult> Resolve([FromBody] ResolveSlidesDto input) => Ok(await _service.ResolveAsync(input));

    [HttpGet("content")]
    public async Task<ActionResult> GetContent([FromQuery] string? presentationId)
    {
        if (string.IsNullOrEmpty(presentationId))
        {
            throw GeneralException.ValidationError("ต้องระบุ presentationId");
        }
        return Ok(await _service.GetContentAsync(presentationId));
    }
}
