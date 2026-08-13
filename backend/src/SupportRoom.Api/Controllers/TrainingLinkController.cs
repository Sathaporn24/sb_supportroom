using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// The invite link CS creates and hands out. One link, many learners - each learner's own run
/// lives behind /api/learning-sessions, not here.
/// </summary>
[ApiController]
[Route("api/training-links")]
public sealed class TrainingLinkController : ControllerBase
{
    private readonly ITrainingLinkService _service;
    private readonly ILessonConfigService _lessonService;

    public TrainingLinkController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ITrainingLinkService>();
        _lessonService = serviceProvider.GetRequiredService<ILessonConfigService>();
    }

    [HttpGet]
    public ActionResult GetAll() => Ok(new { links = _service.GetAll() });

    [HttpPost]
    public ActionResult Create([FromBody] CreateTrainingLinkDto input)
        => StatusCode(StatusCodes.Status201Created, new { link = _service.Create(input) });

    /// <summary>What the join screen loads before anyone has typed a name - the lesson title is
    /// all it needs to render.</summary>
    [AllowAnonymous]
    [HttpGet("{token}")]
    public ActionResult GetByToken([FromRoute] string token)
    {
        var link = _service.GetByToken(token);
        string lessonTitle;
        try
        {
            lessonTitle = _lessonService.GetBySlug(link.LessonSlug).Title;
        }
        catch
        {
            lessonTitle = link.LessonSlug;
        }
        return Ok(new { link, lessonTitle });
    }

    [HttpGet("{id}/by-id")]
    public ActionResult GetById([FromRoute] string id) => Ok(new { link = _service.GetById(id) });

    /// <summary>Everyone who has opened this link. The CS console's drill-down.</summary>
    [HttpGet("{id}/learning-sessions")]
    public ActionResult GetLearningSessions(
        [FromRoute] string id,
        [FromServices] ILearningSessionService learningSessionService)
        => Ok(new { learningSessions = learningSessionService.GetByTrainingLinkId(id) });
}
