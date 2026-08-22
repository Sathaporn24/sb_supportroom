using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public sealed class CompanyController : ControllerBase
{
    private readonly ICompanyService _service;

    public CompanyController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<ICompanyService>();
    }

    /// <summary>Feeds the company switcher: every active company for an owner, exactly one for
    /// anyone else. Scoping happens in the service - Company has no query filter.</summary>
    [HttpGet]
    public ActionResult GetSwitchable() => Ok(new { companies = _service.GetSwitchableCompanies() });

    [HttpGet("all")]
    public ActionResult GetAllIncludingInactive()
        => Ok(new { companies = _service.GetAllIncludingInactive() });

    [HttpPost]
    public ActionResult Create([FromBody] CreateCompanyDto input)
        => StatusCode(StatusCodes.Status201Created, new { company = _service.Create(input) });

    [HttpPut("{id}")]
    public ActionResult Update([FromRoute] string id, [FromBody] UpdateCompanyDto input)
        => Ok(new { company = _service.Update(id, input) });

    /// <summary>LP-9 - separate from PUT /api/companies/{id}, which is owner-only (CP-14).
    /// Owner reads any company; admin/cs read only their own (cs included on purpose - the
    /// pacing section on /admin/settings declares visibleToRoles including cs, see SP-4/SP-15).</summary>
    [HttpGet("{companyId}/lesson-pacing")]
    public ActionResult GetLessonPacing([FromRoute] string companyId)
        => Ok(_service.GetLessonPacing(companyId));

    /// <summary>LP-9 - owner or that company's own admin only; cs is rejected inside the service.</summary>
    [HttpPut("{companyId}/lesson-pacing")]
    public ActionResult UpdateLessonPacing([FromRoute] string companyId, [FromBody] UpdateCompanyLessonPacingDto input)
        => Ok(_service.UpdateLessonPacing(companyId, input));
}
