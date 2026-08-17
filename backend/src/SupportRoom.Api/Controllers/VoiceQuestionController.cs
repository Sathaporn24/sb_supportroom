using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Api.Controllers;

public sealed class VoiceQuestionRequest
{
    public IFormFile? Audio { get; init; }

    /// <summary>The session's public join token. Replaced the old lessonSlug+sessionId pair:
    /// those were two independent client-supplied values that nothing checked belonged
    /// together.</summary>
    public string? Token { get; init; }

    public string? CurrentSlideObjectId { get; init; }
    public string? DurationMs { get; init; }
    public string? Expecting { get; init; }
}

[ApiController]
[Route("api/voice-question")]
public sealed class VoiceQuestionController : ControllerBase
{
    private readonly IVoiceQuestionService _service;

    public VoiceQuestionController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IVoiceQuestionService>();
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Ask([FromForm] VoiceQuestionRequest request)
    {
        if (request.Audio is null || string.IsNullOrEmpty(request.Token))
        {
            throw GeneralException.ValidationError("ต้องแนบไฟล์เสียง (audio) และ token");
        }

        var maxBytes = UploadLimits.MaxVoiceUploadMb * 1024 * 1024;
        if (request.Audio.Length > maxBytes)
        {
            throw GeneralException.ValidationError($"ไฟล์เสียงใหญ่เกินกำหนด (สูงสุด {UploadLimits.MaxVoiceUploadMb}MB)");
        }
        if (!request.Audio.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) && request.Audio.ContentType != "video/webm")
        {
            throw GeneralException.ValidationError("ชนิดไฟล์ไม่ใช่เสียงที่รองรับ");
        }

        using var stream = new MemoryStream();
        await request.Audio.CopyToAsync(stream);

        var result = await _service.AskAsync(new AskVoiceQuestionDto
        {
            Audio = stream.ToArray(),
            MimeType = string.IsNullOrEmpty(request.Audio.ContentType) ? "audio/webm" : request.Audio.ContentType,
            Token = request.Token,
            DurationMs = int.TryParse(request.DurationMs, out var ms) ? ms : 0,
            CurrentSlideObjectId = request.CurrentSlideObjectId,
            Expecting = request.Expecting == "readiness" ? "readiness" : "question",
        });

        return Ok(result);
    }
}
