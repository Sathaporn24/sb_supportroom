using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/knowledge-categories")]
public sealed class KnowledgeCategoriesController : ControllerBase
{
    private readonly IKnowledgeCategoryService _service;

    public KnowledgeCategoriesController(IServiceProvider serviceProvider)
        => _service = serviceProvider.GetRequiredService<IKnowledgeCategoryService>();

    [HttpGet]
    public ActionResult GetAll() => Ok(new { categories = _service.GetAll() });

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateKnowledgeCategoryDto input)
        => Ok(new { category = await _service.CreateAsync(input) });

    [HttpPut("{id}")]
    public async Task<ActionResult> Update([FromRoute] string id, [FromBody] UpdateKnowledgeCategoryDto input)
        => Ok(new { category = await _service.UpdateAsync(id, input) });

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete([FromRoute] string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }

    [HttpGet("{id}/move-preview")]
    public ActionResult GetMovePreview([FromRoute] string id, [FromQuery] string? targetCategoryId)
    {
        if (string.IsNullOrWhiteSpace(targetCategoryId))
        {
            throw GeneralException.ValidationError("ต้องระบุ targetCategoryId");
        }
        return Ok(_service.GetMovePreview(id, targetCategoryId));
    }
}
