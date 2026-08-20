using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/knowledge-qna")]
public sealed class KnowledgeQnAController : ControllerBase
{
    private readonly IKnowledgeQnAService _service;

    public KnowledgeQnAController(IServiceProvider serviceProvider)
        => _service = serviceProvider.GetRequiredService<IKnowledgeQnAService>();

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateKnowledgeQnADto input)
        => Ok(new { qna = await _service.CreateAsync(input) });

    [HttpPut("{id}")]
    public async Task<ActionResult> Update([FromRoute] string id, [FromBody] UpdateKnowledgeQnADto input)
        => Ok(new { qna = await _service.UpdateAsync(id, input) });

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete([FromRoute] string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }
}
