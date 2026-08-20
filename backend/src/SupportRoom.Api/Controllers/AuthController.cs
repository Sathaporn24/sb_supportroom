using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IAuthService>();
    }

    /// <summary>The only anonymous endpoint on the back-office surface - you cannot require a
    /// token to obtain one.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult Login([FromBody] LoginDto input)
        => Ok(new { result = _service.Login(input) });

    [HttpGet("me")]
    [Authorize]
    public ActionResult Me() => Ok(new { user = _service.GetSignedInUser() });

    [HttpPost("change-password")]
    [Authorize]
    public ActionResult ChangePassword([FromBody] ChangePasswordDto input)
    {
        _service.ChangePassword(input);
        return NoContent();
    }
}
