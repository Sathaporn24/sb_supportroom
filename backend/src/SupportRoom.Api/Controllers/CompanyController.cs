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
}
