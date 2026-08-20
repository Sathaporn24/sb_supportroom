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

    /// <summary>DS-1 - one of SupportRoom.Domain.Enums.KnowledgeScopeType. When "lesson",
    /// ScopeId must be LessonConfig.Id, not Slug.</summary>
    public string? ScopeType { get; init; }

    public string? ScopeId { get; init; }
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
            // Passed through as-is (empty string, not null) - EnsureValidScope's default branch
            // rejects an unrecognized/empty ScopeType with a 400, same DS-3 case as an explicit
            // typo, so the controller does not need a second check of its own.
            ScopeType = request.ScopeType ?? string.Empty,
            ScopeId = request.ScopeId,
        });

        return Ok(new { document = result });
    }

    [HttpGet]
    public ActionResult GetAll([FromQuery] string? scopeType, [FromQuery] string? scopeId)
    {
        var documents = _service.GetByScope(scopeType, scopeId);
        return Ok(new { documents });
    }

    [HttpGet("deleted")]
    public ActionResult GetDeleted()
    {
        var documents = _service.GetDeleted();
        return Ok(new { documents });
    }

    /// <summary>DI-7 - the extracted-text-visibility screen's data source. This is the first
    /// endpoint in the system that returns raw uploaded-file content, so authorization is checked
    /// explicitly inside the service (IAuthorizationGuard), not left to the query filter alone.</summary>
    [HttpGet("{id}/chunks")]
    public ActionResult GetChunks(string id)
    {
        var chunks = _service.GetChunks(id);
        return Ok(new { chunks });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> Restore(string id)
    {
        await _service.RestoreAsync(id);
        return Ok(new { status = "restored" });
    }

    /// <summary>DS-5 - first call site of KS-4 ("changing scope moves the document"). id comes
    /// from the path, scope from the body - both feed IKnowledgeNamespaceResolver.EnsureValidScope
    /// inside MoveScopeAsync, so an id/scope pair that spans two companies is rejected there, not
    /// trusted here.</summary>
    [HttpPatch("{id}/scope")]
    public async Task<ActionResult> MoveScope(string id, [FromBody] MoveDocumentScopeDto request)
    {
        var result = await _service.MoveScopeAsync(id, request);
        return Ok(new { document = result });
    }
}
