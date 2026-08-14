using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Common;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// The invite link CS creates and hands out. One link, many learners - each learner's own run
/// lives behind /api/learning-sessions, not here.
/// </summary>
[ApiController]
[Route("api/training-links")]
public sealed class TrainingLinkController : ControllerBase
{
    private readonly ITrainingLinkService _service;
    private readonly ILessonConfigService _lessonService;

    public TrainingLinkController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ITrainingLinkService>();
        _lessonService = serviceProvider.GetRequiredService<ILessonConfigService>();
    }

    [HttpGet]
    public ActionResult GetAll() => Ok(new { links = _service.GetAll() });

    [HttpPost]
    public ActionResult Create([FromBody] CreateTrainingLinkDto input)
        => StatusCode(StatusCodes.Status201Created, new { link = _service.Create(input) });

    /// <summary>Authenticated back-office lookup used by the link detail screen.</summary>
    [HttpGet("by-token/{token}")]
    public ActionResult GetAdminByToken(
        [FromRoute] string token,
        [FromServices] IAuthorizationGuard guard,
        [FromServices] ICompanyContext companyContext)
    {
        // Token lookup deliberately bypasses the tenant filter for learner entry. On an admin
        // route that bypass must be followed by an explicit authorization check, or staff from
        // company A who learn company B's public token could inspect B's link metadata.
        _ = companyContext.CompanyId
            ?? throw GeneralException.ConfigError("กรุณาเลือกบริษัทก่อนเปิดรายละเอียดลิงก์");
        var entity = _service.GetEntityByToken(token);
        guard.EnsureCanAccessCompany(entity.CompanyId);
        var link = _service.GetById(entity.Id);
        return Ok(new { link, lessonTitle = GetLessonTitle(link.LessonSlug) });
    }

    /// <summary>What the join screen loads before anyone has typed a name - the lesson title is
    /// all it needs to render.</summary>
    [AllowAnonymous]
    [HttpGet("{token}")]
    public ActionResult GetByToken([FromRoute] string token)
    {
        var entity = _service.GetEntityByToken(token);
        var link = _service.GetPublicByToken(token);
        return Ok(new { link, lessonTitle = GetLessonTitle(entity.LessonSlug) });
    }

    [HttpGet("{id}/by-id")]
    public ActionResult GetById([FromRoute] string id) => Ok(new { link = _service.GetById(id) });

    /// <summary>Everyone who has opened this link. The CS console's drill-down.</summary>
    [HttpGet("{id}/learning-sessions")]
    public ActionResult GetLearningSessions(
        [FromRoute] string id,
        [FromServices] ILearningSessionService learningSessionService)
        => Ok(new { learningSessions = learningSessionService.GetByTrainingLinkId(id) });

    private string GetLessonTitle(string lessonSlug)
    {
        try
        {
            return _lessonService.GetBySlug(lessonSlug).Title;
        }
        catch
        {
            return lessonSlug;
        }
    }
}
