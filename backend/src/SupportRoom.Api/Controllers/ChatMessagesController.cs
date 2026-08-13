using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// Read-only history hydration - writes only happen through SessionHub.SendChatMessage
/// (same reasoning as SessionQuestionController: no public POST for a write path that's
/// inherently a live/interactive action).
/// </summary>
[ApiController]
[Route("api/chat-messages")]
public sealed class ChatMessagesController : ControllerBase
{
    private readonly IChatMessageService _service;

    public ChatMessagesController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IChatMessageService>();
    }

    /// <summary>The learner's own chat history.</summary>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetForLearner([FromQuery] string token, [FromQuery] string learnerKey)
        => Ok(new { messages = _service.GetForLearner(token, learnerKey) });

    /// <summary>CS-facing: one learning session's chat history.</summary>
    [HttpGet("by-learning-session/{learningSessionId}")]
    public ActionResult GetByLearningSessionId([FromRoute] string learningSessionId)
        => Ok(new { messages = _service.GetByLearningSessionId(learningSessionId) });
}
