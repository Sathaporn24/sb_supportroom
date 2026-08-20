using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>QQ-10 - its own page, not a badge on the queue: the follow-up action here is fixing
/// the source document, not writing an answer.</summary>
[ApiController]
[Route("api/knowledge-qna-conflicts")]
public sealed class KnowledgeQnAConflictsController : ControllerBase
{
    private readonly IKnowledgeQnAConflictService _service;

    public KnowledgeQnAConflictsController(IServiceProvider serviceProvider)
        => _service = serviceProvider.GetRequiredService<IKnowledgeQnAConflictService>();

    /// <summary>`resolved` is accepted for forward compatibility but every value today returns the
    /// same unresolved-only list (QQ-10) - there is no screen for already-closed flags.</summary>
    [HttpGet]
    public ActionResult GetAll([FromQuery] bool? resolved) => Ok(new { conflicts = _service.GetUnresolved() });

    [HttpPut("{id}/resolve")]
    public async Task<ActionResult> Resolve([FromRoute] string id)
        => Ok(new { conflict = await _service.ResolveAsync(id) });
}
