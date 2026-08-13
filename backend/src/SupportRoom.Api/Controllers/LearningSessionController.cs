using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// One person's run through a lesson. Every recipient-side route is keyed on the link token plus
/// the browser's LearnerKey: the token says which lesson and which company, the key says which of
/// the many people on that link is calling.
/// </summary>
[ApiController]
[Route("api/learning-sessions")]
public sealed class LearningSessionController : ControllerBase
{
    private readonly ILearningSessionService _service;

    public LearningSessionController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ILearningSessionService>();
    }

    /// <summary>Idempotent by design - a browser that already has a session on this link gets it
    /// back instead of a second one, which is what makes reconnecting free.</summary>
    [AllowAnonymous]
    [HttpPost("{token}/join")]
    public ActionResult Join([FromRoute] string token, [FromBody] JoinLearningSessionDto input)
        => Ok(new { learningSession = _service.Join(token, input) });

    /// <summary>"เรียนอีกครั้ง" - explicitly starts a new round rather than reopening the old one.</summary>
    [AllowAnonymous]
    [HttpPost("{token}/restart")]
    public ActionResult Restart([FromRoute] string token, [FromBody] JoinLearningSessionDto input)
        => StatusCode(StatusCodes.Status201Created, new { learningSession = _service.Restart(token, input) });

    [AllowAnonymous]
    [HttpPatch("{token}/progress")]
    public ActionResult UpdateProgress([FromRoute] string token, [FromBody] UpdateLearningProgressDto input)
        => Ok(new { learningSession = _service.UpdateProgress(token, input) });

    [AllowAnonymous]
    [HttpPatch("{token}/end")]
    public ActionResult End([FromRoute] string token, [FromBody] EndLearningSessionDto input)
        => Ok(new { learningSession = _service.End(token, input) });

    /// <summary>
    /// The learner's own recap. Goes through (token, learnerKey) rather than accepting a session
    /// id so nobody can read another learner's recap by holding the same link.
    ///
    /// ⚠️ UnansweredPoints in this payload is internal (CORE_FEATURE_SPEC §2.5) - the learner-facing
    /// screen renders questions only. It is served here because the CS console reuses the shape;
    /// splitting the two payloads is the follow-up if that stops being true.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{token}/summary")]
    public ActionResult GetOwnSummary([FromRoute] string token, [FromQuery] string learnerKey)
    {
        var session = _service.GetEntityByLearnerKey(token, learnerKey);
        return Ok(new { learningSession = _service.GetById(session.Id), summary = _service.GetSummary(session.Id) });
    }

    /// <summary>CS-facing: any learning session by id, with review fields on every question.</summary>
    [HttpGet("{id}/summary/by-id")]
    public ActionResult GetSummaryById([FromRoute] string id) => Ok(new { summary = _service.GetSummary(id) });
}
