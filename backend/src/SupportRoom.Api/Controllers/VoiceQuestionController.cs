using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Api.Configurations;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Configuration;
using SupportRoom.Infrastructure.ErrorHandling;

namespace SupportRoom.Api.Controllers;

public sealed class VoiceQuestionRequest
{
    public IFormFile? Audio { get; init; }

    /// <summary>The link's public join token. Replaced the old lessonSlug+sessionId pair:
    /// those were two independent client-supplied values that nothing checked belonged
    /// together.</summary>
    public string? Token { get; init; }

    /// <summary>Which learner on that link is asking - see AskVoiceQuestionDto.LearnerKey.</summary>
    public string? LearnerKey { get; init; }

    public string? CurrentSlideObjectId { get; init; }
    public string? DurationMs { get; init; }
}

[ApiController]
[Route("api/voice-question")]
public sealed class VoiceQuestionController : ControllerBase
{
    private readonly IVoiceQuestionService _service;
    private readonly IQuestionRateLimiter _rateLimiter;

    public VoiceQuestionController(IServiceProvider serviceProvider, IQuestionRateLimiter rateLimiter)
    {
        _service = serviceProvider.GetRequiredService<IVoiceQuestionService>();
        _rateLimiter = rateLimiter;
    }

    [AllowAnonymous]
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Ask([FromForm] VoiceQuestionRequest request)
    {
        if (request.Audio is null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.LearnerKey))
        {
            throw GeneralException.ValidationError("ต้องแนบไฟล์เสียง (audio), token และ learnerKey");
        }

        // SEC-02 - checked after presence validation but before the upload is buffered/decoded and
        // before any provider call (embedding/LLM/TTS) runs.
        if (!_rateLimiter.TryAcquire(request.Token, request.LearnerKey))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiErrorEnvelope.Build(
                SupportRoom.Domain.Enums.ApiErrorCode.RateLimited,
                "ถามคำถามถี่เกินไป กรุณาลองใหม่ภายหลัง"));
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
            LearnerKey = request.LearnerKey,
            DurationMs = int.TryParse(request.DurationMs, out var ms) ? ms : 0,
            CurrentSlideObjectId = request.CurrentSlideObjectId,
        });

        return Ok(result);
    }
}
