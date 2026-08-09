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

    [HttpGet]
    public ActionResult GetBySessionId([FromQuery] string sessionId) => Ok(new { messages = _service.GetBySessionId(sessionId) });
}
