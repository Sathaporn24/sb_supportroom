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
    private readonly ILessonExcludedSlideService _excludedSlideService;

    public LessonController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ILessonConfigService>();
        _narrationService = serviceProvider.GetRequiredService<ILessonSlideNarrationService>();
        _excludedSlideService = serviceProvider.GetRequiredService<ILessonExcludedSlideService>();
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
        [FromRoute] string token,
        [FromQuery] string? learnerKey)
    {
        var content = await _service.GetTeachingContentByLinkAsync(token, learnerKey);
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

    /// <summary>NR-10 - the create-lesson flow's counterpart to PreviewPdf above, for a file that
    /// has not been uploaded via POST /api/documents yet. Same 30MB limit as DocumentsController.Upload
    /// on purpose - this endpoint feeds the exact same PdfSlidesRenderer.BuildContent path.</summary>
    [HttpPost("pdf-preview/session")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult> CreatePdfPreviewSession(IFormFile? file)
    {
        if (file is null)
        {
            throw GeneralException.ValidationError("ต้องแนบไฟล์ (file)");
        }

        using var stream = file.OpenReadStream();
        return Ok(await _service.CreatePdfPreviewSessionAsync(stream, file.FileName));
    }

    /// <summary>NR-10/NR-11 - image for a page of a not-yet-persisted PDF, scoped to the preview
    /// session's own CompanyId (checked inside the service, not here).</summary>
    [HttpGet("pdf-preview/{previewId}/pages/{pageNumber:int}")]
    public async Task<ActionResult> GetPdfPreviewPage([FromRoute] string previewId, [FromRoute] int pageNumber)
    {
        var png = await _service.RenderPdfPreviewPageAsync(previewId, pageNumber);
        return File(png, "image/png");
    }

    [AllowAnonymous]
    [HttpGet("pdf-pages/{token}/{documentId}/{pageNumber:int}")]
    public async Task<ActionResult> GetPdfPage(
        [FromRoute] string token,
        [FromRoute] string documentId,
        [FromRoute] int pageNumber,
        [FromQuery] string? learnerKey,
        [FromServices] ITrainingLinkService trainingLinkService)
    {
        // Resolves ICompanyContext before RenderPdfPageAsync looks up the document. Without this,
        // the query filter sees no company on an anonymous request and every PDF page is a 404.
        // R9/LT-5/LT-6 - the content-access gate, then a trash-aware lesson lookup (GetBySlug
        // would 404 a trashed lesson even for a legitimately still-IN_PROGRESS learner).
        var link = trainingLinkService.GetEntityByTokenForContentAccess(token, learnerKey);
        var lesson = _service.GetByIdIncludingDeleted(link.LessonId);
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
        return NoContent();
    }

    /// <summary>NR-3/EX-10 - the admin UI calls this before letting CS confirm uploading a new PDF
    /// over an existing one, to show "บทพูดที่แก้ไว้ N หน้า และหน้าที่ตัดออกไว้ M หน้า จะถูกล้างทั้งหมด".</summary>
    [HttpGet("{id}/narrations/count")]
    public ActionResult GetNarrationCount([FromRoute] string id)
    {
        var (count, excludedCount) = _narrationService.CountByLessonId(id);
        return Ok(new { count, excludedCount });
    }

    /// <summary>EX-4 - toggles one PDF page's exclusion. excluded=true cuts the page out of the
    /// lesson (teaching content, RAG index, and the document's own copy vector); excluded=false
    /// brings it back. Idempotent both ways - see ILessonExcludedSlideService.ToggleAsync.</summary>
    [HttpPut("{id}/slides/{slideObjectId}/excluded")]
    public async Task<ActionResult> ToggleSlideExcluded(
        [FromRoute] string id,
        [FromRoute] string slideObjectId,
        [FromBody] ToggleSlideExcludedDto input)
    {
        await _excludedSlideService.ToggleAsync(id, slideObjectId, input.Excluded);
        return Ok();
    }

    /// <summary>R9/LT-7/LT-9 - the trash tab's data source. GetAll() above never includes these
    /// rows (the query filter hides them) - this is the one endpoint that can see them.</summary>
    [HttpGet("trash")]
    public ActionResult GetTrash() => Ok(new { lessons = _service.GetTrash() });

    /// <summary>R9/LT-1..LT-3 - moves an active lesson to the trash. owner/admin only; cs is
    /// rejected server-side inside the service (LT-2), not just hidden in the UI.</summary>
    [HttpPost("{id}/trash")]
    public async Task<ActionResult> Trash([FromRoute] string id)
        => Ok(new { lesson = await _service.ArchiveAsync(id) });

    /// <summary>R9/LT-1/LT-4 - restores a trashed lesson, provided the worker has not already
    /// started purging it (409 otherwise).</summary>
    [HttpPost("{id}/restore")]
    public async Task<ActionResult> Restore([FromRoute] string id)
        => Ok(new { lesson = await _service.RestoreAsync(id) });

    /// <summary>R9/LT-2/LT-10 - owner-only manual permanent delete. 202: the lesson is queued for
    /// immediate purge, not deleted inline - the response body intentionally carries nothing to
    /// adapt, since there is no updated resource to return.</summary>
    [HttpPost("{id}/permanent-delete")]
    public async Task<ActionResult> RequestPermanentDelete([FromRoute] string id, [FromBody] PermanentDeleteLessonDto input)
    {
        await _service.RequestPermanentDeleteAsync(id, input.ConfirmationTitle);
        return StatusCode(StatusCodes.Status202Accepted);
    }
}

public sealed class MoveLessonCategoryRequest
{
    public required string CategoryId { get; init; }
}
