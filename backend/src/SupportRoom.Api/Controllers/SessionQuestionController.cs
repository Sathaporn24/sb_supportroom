using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// Read-only - writes happen inside POST /api/voice-question (the only place a question is
/// actually asked+answered). Used by the Admin summary view.
/// </summary>
[ApiController]
[Route("api/session-questions")]
public sealed class SessionQuestionController : ControllerBase
{
    private readonly ISessionQuestionService _service;

    public SessionQuestionController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ISessionQuestionService>();
    }

    [HttpGet]
    public ActionResult GetByToken([FromQuery] string token) => Ok(new { questions = _service.GetByToken(token) });
}
