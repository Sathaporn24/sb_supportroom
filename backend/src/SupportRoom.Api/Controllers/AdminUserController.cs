using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

/// <summary>
/// Thin by design: every permission question is answered inside AdminUserService via
/// IAuthorizationGuard, not here. Putting checks in a controller attribute would leave the service
/// callable without them from anywhere else (SignalR, a background job, another service).
/// </summary>
[ApiController]
[Route("api/admin-users")]
[Authorize]
public sealed class AdminUserController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUserController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IAdminUserService>();
    }

    /// <summary>Users of one company. companyId is explicit rather than taken from ?company= so a
    /// request cannot quietly list a different company than the URL says it is showing.</summary>
    [HttpGet("{companyId}")]
    public ActionResult GetByCompany([FromRoute] string companyId)
        => Ok(new { users = _service.GetByCompany(companyId) });

    [HttpPost]
    public ActionResult Create([FromBody] CreateAdminUserDto input)
        => StatusCode(StatusCodes.Status201Created, new { user = _service.Create(input) });

    [HttpPut("{id}")]
    public ActionResult Update([FromRoute] string id, [FromBody] UpdateAdminUserDto input)
        => Ok(new { user = _service.Update(id, input) });
}
