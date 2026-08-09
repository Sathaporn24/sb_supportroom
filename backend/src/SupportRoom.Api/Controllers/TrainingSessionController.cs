using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

public sealed class PatchSessionDto
{
    public required string Action { get; init; } // "start" | "end"
    public bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
}

[ApiController]
[Route("api/sessions")]
public sealed class TrainingSessionController : ControllerBase
{
    private readonly ITrainingSessionService _service;
    private readonly ILessonConfigService _lessonService;

    public TrainingSessionController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ITrainingSessionService>();
        _lessonService = serviceProvider.GetRequiredService<ILessonConfigService>();
    }

    [HttpGet]
    public ActionResult GetAll() => Ok(new { sessions = _service.GetAll() });

    [HttpPost]
    public ActionResult Create([FromBody] CreateSessionDto input)
        => StatusCode(StatusCodes.Status201Created, new { session = _service.Create(input) });

    [HttpGet("{token}")]
    public ActionResult GetByToken([FromRoute] string token)
    {
        var session = _service.GetByToken(token);
        string lessonTitle;
        try
        {
            lessonTitle = _lessonService.GetBySlug(session.LessonSlug).Title;
        }
        catch
        {
            lessonTitle = session.LessonSlug;
        }
        return Ok(new { session, lessonTitle });
    }

    [HttpGet("{id}/by-id")]
    public ActionResult GetById([FromRoute] string id) => Ok(new { session = _service.GetById(id) });

    [HttpPatch("{token}")]
    public ActionResult Patch([FromRoute] string token, [FromBody] PatchSessionDto input)
    {
        var session = input.Action == "start"
            ? _service.MarkStarted(token)
            : _service.End(token, new EndSessionDto { CompletedAllSlides = input.CompletedAllSlides, LastSlideObjectId = input.LastSlideObjectId });
        return Ok(new { session });
    }

    [HttpGet("{token}/summary")]
    public ActionResult GetSummary([FromRoute] string token, [FromServices] ISessionSummaryService summaryService)
    {
        var session = _service.GetByToken(token);
        var summary = summaryService.GetBySessionId(session.Id);
        return Ok(new { session, summary });
    }
}
