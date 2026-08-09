using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/lessons")]
public sealed class LessonController : ControllerBase
{
    private readonly ILessonConfigService _service;

    public LessonController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ILessonConfigService>();
    }

    [HttpGet]
    public ActionResult GetAll() => Ok(new { lessons = _service.GetAll() });

    [HttpGet("{slug}")]
    public async Task<ActionResult> GetBySlug([FromRoute] string slug)
    {
        var content = await _service.GetTeachingContentBySlugAsync(slug);
        return Ok(new { lesson = content.Lesson, embedUrl = content.EmbedUrl, slides = content.Slides });
    }

    [HttpPost]
    public async Task<ActionResult> Save([FromBody] LessonConfigDto input)
        => Ok(new { lesson = await _service.SaveAsync(input) });

    /// <summary>Preview a PDF already uploaded via POST /api/documents, before saving the
    /// lesson - populates the admin editor's slideConfigs list the same way Google's
    /// resolve+content two-step does.</summary>
    [HttpGet("pdf-preview")]
    public async Task<ActionResult> PreviewPdf([FromQuery] string? documentId)
    {
        if (string.IsNullOrEmpty(documentId))
        {
            throw GeneralException.ValidationError("ต้องระบุ documentId");
        }
        return Ok(await _service.PreviewPdfAsync(documentId));
    }

    [HttpGet("pdf-pages/{documentId}/{pageNumber:int}")]
    public async Task<ActionResult> GetPdfPage([FromRoute] string documentId, [FromRoute] int pageNumber)
    {
        var png = await _service.RenderPdfPageAsync(documentId, pageNumber);
        return File(png, "image/png");
    }
}
