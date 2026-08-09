using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Api.Controllers;

public sealed class UploadDocumentRequest
{
    public IFormFile? File { get; init; }

    /// <summary>Omit to store as a standalone/global knowledge-base document.</summary>
    public string? LessonSlug { get; init; }
}

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentResourceService _service;

    public DocumentsController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IDocumentResourceService>();
    }

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult> Upload([FromForm] UploadDocumentRequest request)
    {
        if (request.File is null)
        {
            throw GeneralException.ValidationError("ต้องแนบไฟล์ (file)");
        }

        var maxBytes = UploadLimits.MaxDocumentUploadMb * 1024 * 1024;
        if (request.File.Length > maxBytes)
        {
            throw GeneralException.ValidationError($"ไฟล์ใหญ่เกินกำหนด (สูงสุด {UploadLimits.MaxDocumentUploadMb}MB)");
        }

        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream);

        var result = await _service.UploadAsync(new UploadDocumentDto
        {
            Content = stream.ToArray(),
            FileName = request.File.FileName,
            ContentType = string.IsNullOrEmpty(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType,
            LessonSlug = request.LessonSlug,
        });

        return Ok(new { document = result });
    }

    [HttpGet]
    public ActionResult GetAll([FromQuery] string? lessonSlug)
    {
        var documents = string.IsNullOrEmpty(lessonSlug)
            ? _service.GetStandalone()
            : _service.GetByLessonSlug(lessonSlug);
        return Ok(new { documents });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }
}
