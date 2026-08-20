using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/lessons")]
public sealed class LessonController : ControllerBase
{
    private readonly ILessonConfigService _service;
    private readonly ILessonSlideNarrationService _narrationService;

    public LessonController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ILessonConfigService>();
        _narrationService = serviceProvider.GetRequiredService<ILessonSlideNarrationService>();
    }

    [HttpGet]
    public ActionResult GetAll() => Ok(new { lessons = _service.GetAll() });

    /// <summary>Admin-side direct lookup. Learners must use by-link below: a slug is unique only
    /// inside one company and an anonymous request has no company context to scope this query.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult> GetBySlug([FromRoute] string slug)
    {
        var content = await _service.GetTeachingContentBySlugAsync(slug);
        return Ok(new { lesson = content.Lesson, embedUrl = content.EmbedUrl, slides = content.Slides });
    }

    /// <summary>Learner-side lookup. Resolving the link first derives both the company and the
    /// lesson from one unguessable token, so the caller cannot combine one company's slug with
    /// another company's context.</summary>
    [AllowAnonymous]
    [HttpGet("by-link/{token}")]
    public async Task<ActionResult> GetByLink(
        [FromRoute] string token)
    {
        var content = await _service.GetTeachingContentByLinkAsync(token);
        return Ok(new { lesson = content.Lesson, embedUrl = content.EmbedUrl, slides = content.Slides });
    }

    [HttpPost]
    public async Task<ActionResult> Save([FromBody] LessonConfigDto input)
        => Ok(new { lesson = await _service.SaveAsync(input) });

    [HttpPut("{id}/category")]
    public async Task<ActionResult> MoveCategory([FromRoute] string id, [FromBody] MoveLessonCategoryRequest input)
        => Ok(new { lesson = await _service.MoveCategoryAsync(id, input.CategoryId) });

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

    [AllowAnonymous]
    [HttpGet("pdf-pages/{token}/{documentId}/{pageNumber:int}")]
    public async Task<ActionResult> GetPdfPage(
        [FromRoute] string token,
        [FromRoute] string documentId,
        [FromRoute] int pageNumber,
        [FromServices] ITrainingLinkService trainingLinkService)
    {
        // Resolves ICompanyContext before RenderPdfPageAsync looks up the document. Without this,
        // the query filter sees no company on an anonymous request and every PDF page is a 404.
        var link = trainingLinkService.GetEntityByToken(token);
        var lesson = _service.GetBySlug(link.LessonSlug);
        if (!lesson.IsActive
            || lesson.ContentSourceType != LessonContentSourceType.Pdf
            || !string.Equals(lesson.PdfDocumentResourceId, documentId, StringComparison.Ordinal))
        {
            throw GeneralException.NotFound("เอกสารของบทเรียน");
        }
        var png = await _service.RenderPdfPageAsync(documentId, pageNumber);
        return File(png, "image/png");
    }

    /// <summary>NR-1/NR-5 - every page's resolved narration text plus the "likely scanned" flag.
    /// PDF-sourced lessons only - see ILessonSlideNarrationService.EnsurePdfSource (NR-9).</summary>
    [HttpGet("{id}/narrations")]
    public async Task<ActionResult> GetNarrations([FromRoute] string id)
        => Ok(await _narrationService.GetAllAsync(id));

    /// <summary>NR-2/NR-6/NR-9. An empty/omitted narrationText deletes the override row (reverts
    /// to the extracted text).</summary>
    [HttpPut("{id}/narrations/{slideObjectId}")]
    public async Task<ActionResult> SaveNarration(
        [FromRoute] string id,
        [FromRoute] string slideObjectId,
        [FromBody] LessonSlideNarrationDto input)
    {
        await _narrationService.SaveAsync(id, slideObjectId, input.NarrationText);
        return Ok();
    }

    /// <summary>NR-3 - the admin UI calls this before letting CS confirm uploading a new PDF over
    /// an existing one, to show "บทพูดที่แก้ไว้ N หน้าจะถูกลบทั้งหมด".</summary>
    [HttpGet("{id}/narrations/count")]
    public ActionResult GetNarrationCount([FromRoute] string id)
        => Ok(new { count = _narrationService.CountByLessonId(id) });
}

public sealed class MoveLessonCategoryRequest
{
    public required string CategoryId { get; init; }
}
