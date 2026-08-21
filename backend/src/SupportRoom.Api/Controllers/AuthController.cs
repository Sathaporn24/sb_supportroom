using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupportRoom.Api.Configurations;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Services;
using SupportRoom.Infrastructure.ErrorHandling;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly ILoginAccountRateLimiter _accountRateLimiter;

    public AuthController(IServiceProvider serviceProvider, ILoginAccountRateLimiter accountRateLimiter)
    {
        _service = serviceProvider.GetRequiredService<IAuthService>();
        _accountRateLimiter = accountRateLimiter;
    }

    /// <summary>The only anonymous endpoint on the back-office surface - you cannot require a
    /// token to obtain one.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitConfiguration.LoginPolicyName)]
    public ActionResult Login([FromBody] LoginDto input)
    {
        if (!_accountRateLimiter.TryAcquire(input.Email))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiErrorEnvelope.Build(
                SupportRoom.Domain.Enums.ApiErrorCode.RateLimited,
                "มีความพยายามเข้าสู่ระบบมากเกินไป กรุณาลองใหม่ภายหลัง"));
        }

        return Ok(new { result = _service.Login(input) });
    }

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
