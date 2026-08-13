using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// Questions are never POSTed here - they are written inside POST /api/voice-question, the only
/// place a question is actually asked and answered. What this controller adds on top of reading
/// them back is CS's review verdict.
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

    /// <summary>The learner's own questions - needs the browser key, because the token alone now
    /// covers everyone on the link.</summary>
    [HttpGet]
    public ActionResult GetForLearner([FromQuery] string token, [FromQuery] string learnerKey)
        => Ok(new { questions = _service.GetForLearner(token, learnerKey) });

    /// <summary>CS-facing: one learning session's questions, review fields included.</summary>
    [HttpGet("by-learning-session/{learningSessionId}")]
    public ActionResult GetByLearningSessionId([FromRoute] string learningSessionId)
        => Ok(new { questions = _service.GetByLearningSessionId(learningSessionId) });

    [HttpPatch("{questionId}/review")]
    public ActionResult Review([FromRoute] string questionId, [FromBody] ReviewSessionQuestionDto input)
        => Ok(new { question = _service.Review(questionId, input) });
}
