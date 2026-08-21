using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using SupportRoom.Api;
using SupportRoom.Api.Configurations;
using SupportRoom.Api.Controllers;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Api.IntegrationTests;

public sealed class BackOfficeSecurityBoundaryTests
{
    [Theory]
    [InlineData("/api/auth/me", true)]
    [InlineData("/api/auth/change-password", true)]
    [InlineData("/api/companies/all", false)]
    [InlineData("/hubs/session", false)]
    public void MustChangePassword_OnlyPermitsIdentityAndPasswordRoutes(string path, bool allowed)
        => Assert.Equal(allowed, CurrentUserMiddleware.IsAllowedWhilePasswordChangeIsRequired(path));

    [Fact]
    public void LoginEndpoint_UsesTheDedicatedRateLimitPolicy()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(LoginRateLimitConfiguration.LoginPolicyName, attribute.PolicyName);
    }

    [Fact]
    public async Task LoginRateLimitRejection_ReturnsTheStable429ErrorEnvelope()
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await LoginRateLimitConfiguration.WriteRejectedResponseAsync(context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.Ordinal);
        var json = Encoding.UTF8.GetString(body.ToArray());
        Assert.Contains(ApiErrorCode.RateLimited, json, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizedAccountLimiter_RejectsRepeatedAttemptsWithoutLeakingAccountExistence()
    {
        var limiter = new LoginAccountRateLimiter();

        for (var attempt = 0; attempt < LoginAccountRateLimiter.PermitLimit; attempt++)
        {
            Assert.True(limiter.TryAcquire("Admin@Example.com"));
        }

        Assert.False(limiter.TryAcquire("admin@example.com"));
    }

    [Fact]
    public async Task MustChangePassword_BlocksDirectBearerBusinessRequest()
    {
        var context = AuthenticatedContext("/api/companies/all");
        var currentUser = new CurrentUser();
        var nextWasCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        var exception = await Assert.ThrowsAsync<HttpStatusCodeException>(() => middleware.InvokeAsync(
            context,
            currentUser,
            new CompanyContext(),
            new AuthorizationGuard(currentUser),
            new MustChangeAuthService()));

        Assert.Equal(ApiErrorCode.Forbidden, exception.Code);
        Assert.False(nextWasCalled);
    }

    [Fact]
    public async Task MustChangePassword_AllowsTheServerChangePasswordRoute()
    {
        var context = AuthenticatedContext("/api/auth/change-password");
        var currentUser = new CurrentUser();
        var nextWasCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            context,
            currentUser,
            new CompanyContext(),
            new AuthorizationGuard(currentUser),
            new MustChangeAuthService());

        Assert.True(nextWasCalled);
    }

    private static DefaultHttpContext AuthenticatedContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaims.UserId, "user-1"),
            new Claim(AuthClaims.Role, AdminRole.Admin),
            new Claim(AuthClaims.CompanyId, "company-a"),
        ], "test"));
        return context;
    }

    private sealed class MustChangeAuthService : IAuthService
    {
        public LoginResultViewModel Login(LoginDto input) => throw new NotSupportedException();
        public SignedInUserViewModel RefreshCurrentUser() => User;
        public SignedInUserViewModel GetSignedInUser() => User;
        public void ChangePassword(ChangePasswordDto input) => throw new NotSupportedException();
        public void SeedFirstOwnerIfEmpty() => throw new NotSupportedException();

        private static SignedInUserViewModel User => new()
        {
            Id = "user-1",
            Email = "user-1@example.com",
            DisplayName = "User 1",
            Role = AdminRole.Admin,
            CompanyId = "company-a",
            MustChangePassword = true,
        };
    }
}
