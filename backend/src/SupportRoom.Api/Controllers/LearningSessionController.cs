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

    /// <summary>
    /// Read-only lookup the join screen makes before showing anything: it decides which of the six
    /// join screens to render, and it is what stops a shared computer from dropping the next
    /// person straight into the previous person's lesson. Always 200 - "nothing to resume" is a
    /// normal answer, not a 404.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{token}/resume")]
    public ActionResult GetResumeState(
        [FromRoute] string token,
        [FromQuery] string? learnerKey,
        [FromServices] ILessonConfigService lessonConfigService,
        [FromServices] ITrainingLinkService trainingLinkService)
    {
        // LT-5/LT-6: resolve through the strict gate before returning any link/lesson metadata.
        // A revoked token without the bound IN_PROGRESS learner session must be indistinguishable
        // from an unknown token, rather than exposing the trashed lesson title.
        var link = trainingLinkService.GetEntityByTokenForContentAccess(token, learnerKey);
        var state = _service.GetResumeState(token, learnerKey);
        return Ok(new
        {
            link = state.Link,
            // Bundled so the join screen makes one request instead of two - it cannot render
            // anything without the lesson title anyway.
            lessonTitle = GetLessonTitle(lessonConfigService, link.LessonId),
            resumable = state.Resumable,
            lastEnded = state.LastEnded,
            linkExpired = state.LinkExpired,
        });
    }

    /// <summary>R9/LT-19 - looks the lesson up by id including a trashed one (GetBySlug's normal
    /// filter would 404 that too, indistinguishable from a permanently purged lesson). Only a
    /// lesson that genuinely no longer exists (hard-deleted by purge) falls back to the fixed
    /// "บทเรียนที่ถูกลบ" label. Mirrors TrainingLinkController.GetLessonTitle.</summary>
    private static string GetLessonTitle(ILessonConfigService lessonConfigService, string lessonId)
    {
        try
        {
            return lessonConfigService.GetByIdIncludingDeleted(lessonId).Title;
        }
        catch
        {
            return "บทเรียนที่ถูกลบ";
        }
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
    /// This response deliberately uses a learner-only shape: internal unanswered-point follow-up
    /// and CS review fields are available only from the authenticated by-id endpoint below.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{token}/summary")]
    public ActionResult GetOwnSummary([FromRoute] string token, [FromQuery] string learnerKey)
    {
        var session = _service.GetEntityByLearnerKey(token, learnerKey);
        return Ok(new { learningSession = _service.GetById(session.Id), summary = _service.GetLearnerSummary(session.Id) });
    }

    /// <summary>CS-facing: any learning session by id, with review fields on every question.</summary>
    [HttpGet("{id}/summary/by-id")]
    public ActionResult GetSummaryById([FromRoute] string id) => Ok(new { summary = _service.GetSummary(id) });
}
