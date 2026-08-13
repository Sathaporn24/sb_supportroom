using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;
using SupportRoom.Application.ViewModel;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthService _service;

    public HealthController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IHealthService>();
    }

    [AllowAnonymous]
    [HttpGet]
    public ActionResult<HealthViewModel> Get() => Ok(_service.Get());
}
