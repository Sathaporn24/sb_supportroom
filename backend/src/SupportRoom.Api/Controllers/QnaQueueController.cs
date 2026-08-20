using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>P8/R5.1 - the review queue, read-only. A row leaves this list only by saving a Q&A
/// against it (POST /api/knowledge-qna) - see QQ-2, there is no "dismiss" endpoint.</summary>
[ApiController]
[Route("api/qna-queue")]
public sealed class QnaQueueController : ControllerBase
{
    private readonly IKnowledgeQnAService _service;

    public QnaQueueController(IServiceProvider serviceProvider)
        => _service = serviceProvider.GetRequiredService<IKnowledgeQnAService>();

    [HttpGet]
    public ActionResult GetAll() => Ok(new { queue = _service.GetQueue() });
}
