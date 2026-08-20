using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Api.Configurations;

public static class AuthenticationConfiguration
{
    /// <summary>
    /// JWT bearer rather than cookies (TD-014): the frontend is served from a different origin than
    /// this API, so cookies would need SameSite=None plus AllowCredentials() on a CORS policy that
    /// deliberately lists explicit origins, and SignalR - which currently connects with
    /// withCredentials:false on purpose - would have to change too. A bearer token in the
    /// Authorization header needs none of that, and SignalR carries it natively via
    /// accessTokenFactory.
    /// </summary>
    public static IServiceCollection AddBackOfficeAuthentication(this IServiceCollection services)
    {
        var settings = AuthEnv.GetJwt();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                    ValidateLifetime = true,
                    // Default is five minutes of grace, which quietly extends every token's life
                    // past the expiry the frontend was told about. Zero keeps the two honest.
                    ClockSkew = TimeSpan.Zero,
                    // The handler otherwise renames "sub" and "role" to long SOAP-era claim URIs,
                    // and CurrentUserMiddleware looks for the short names it actually issued.
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    // SignalR cannot set an Authorization header on the WebSocket handshake, so the
                    // client passes the token as a query string instead. Accept it only for the hub
                    // path - anywhere else a token in the URL would end up in access logs.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        // Fail closed: every endpoint requires a signed-in user unless it opts out with
        // [AllowAnonymous]. The alternative - marking each admin controller [Authorize] - means a
        // controller added later is public until someone remembers, and "someone forgot" is how
        // an unprotected admin endpoint ships. Here forgetting breaks the learner flow loudly in
        // development instead of quietly exposing the back office in production.
        //
        // The endpoints that legitimately opt out are exactly the learner surface (join, progress,
        // voice question, TTS, lesson content, own recap) plus login and health. They carry no
        // token and derive their company from TrainingLink.Token.
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
