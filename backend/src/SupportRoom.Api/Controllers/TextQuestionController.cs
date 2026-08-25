using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Api.Configurations;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Infrastructure.ErrorHandling;

namespace SupportRoom.Api.Controllers;

public sealed class TextQuestionRequest
{
    public string? Token { get; init; }
    public string? LearnerKey { get; init; }
    public string? Text { get; init; }
    public string? CurrentSlideObjectId { get; init; }
}

/// <summary>
/// F10/TQ-1 - the typed-question equivalent of VoiceQuestionController. Deliberately a separate
/// controller/JSON request rather than a nullable "text" field bolted onto VoiceQuestionRequest's
/// multipart/10MB-upload shape: the two channels have almost nothing in common at the transport
/// layer, and the DTO/provider layers underneath are what carries the "equivalent to voice"
/// contract (T1), not this controller.
/// </summary>
[ApiController]
[Route("api/text-question")]
public sealed class TextQuestionController : ControllerBase
{
    private readonly IVoiceQuestionService _service;
    private readonly IQuestionRateLimiter _rateLimiter;

    public TextQuestionController(IServiceProvider serviceProvider, IQuestionRateLimiter rateLimiter)
    {
        _service = serviceProvider.GetRequiredService<IVoiceQuestionService>();
        _rateLimiter = rateLimiter;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> Ask([FromBody] TextQuestionRequest request)
    {
        // TQ-3 - this order is a contract, not an implementation detail: token/learnerKey/text
        // presence first, then trimmed emptiness, then the length ceiling.
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.LearnerKey) || string.IsNullOrEmpty(request.Text))
        {
            throw GeneralException.ValidationError("ต้องระบุ token, learnerKey และข้อความคำถาม");
        }

        // SEC-02 - checked after presence validation (a 400 for a malformed request should never
        // consume rate-limit budget) but before anything expensive (embedding/LLM/TTS) runs.
        if (!_rateLimiter.TryAcquire(request.Token, request.LearnerKey))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiErrorEnvelope.Build(
                SupportRoom.Domain.Enums.ApiErrorCode.RateLimited,
                "ถามคำถามถี่เกินไป กรุณาลองใหม่ภายหลัง"));
        }

        var trimmed = request.Text.Trim();
        if (trimmed.Length < 1)
        {
            throw GeneralException.ValidationError("กรุณาพิมพ์คำถามก่อนส่ง");
        }
        if (trimmed.Length > DtoLimits.QuestionTextMaxLength)
        {
            throw GeneralException.ValidationError($"คำถามต้องมี 1-{DtoLimits.QuestionTextMaxLength} ตัวอักษร");
        }

        var result = await _service.AskTextAsync(new AskTextQuestionDto
        {
            Token = request.Token,
            LearnerKey = request.LearnerKey,
            // Every downstream consumer (SessionQuestion.Transcript included) reads this exact
            // trimmed value - never the raw request.Text.
            Text = trimmed,
            CurrentSlideObjectId = request.CurrentSlideObjectId,
        });

        return Ok(result);
    }
}
