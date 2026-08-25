using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Api;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Common;

namespace SupportRoom.Api.Hubs;

/// <summary>
/// One SignalR group per LEARNING SESSION id.
///
/// It used to be one group per link token, which was correct only while a link meant a single
/// learner. Now that one link is opened by a whole department, a token-keyed group would put
/// every learner in the same room and fan each person's questions out to all of them - the exact
/// leak CORE_FEATURE_SPEC §2.4 exists to prevent.
///
/// Learners never join a group at all (F10-a removed the only reason they did - typed chat with
/// CS). This hub's only remaining entry point is the CS-side one below: a support agent joins a
/// learning session's group to receive that learner's questions live
/// ("ReceiveNewQuestion", broadcast from Application services via IRealtimeNotifier, not from this
/// Hub directly).
/// </summary>
[AllowAnonymous]
public sealed class SessionHub(IServiceProvider serviceProvider) : Hub
{
    /// <summary>
    /// CS-side entry point. A support agent has no learnerKey - that key belongs to the learner's
    /// browser - so they address a learning session by id instead.
    ///
    /// The hub itself stays anonymous because learners have no account. Agent entry points must
    /// therefore opt into the same authorization guard as REST; a GUID is not access control.
    /// </summary>
    /// <summary>
    /// QA-02 residual: this check runs only on invocation, not continuously for the life of the
    /// connection. A connection that has already joined a group and then goes idle - no further
    /// hub method calls - keeps receiving broadcasts until the token's own transport-level
    /// disconnect or the browser closes it, even after the JWT it authenticated with expires. A
    /// periodic re-check via a server-side timer per connection would close that gap but adds
    /// per-connection background work for every open tab; not worth it until there is evidence an
    /// idle CS connection actually gets left open past token expiry in practice.
    /// </summary>
    public async Task JoinSessionAsAgent(string learningSessionId)
    {
        EnsureAgentAuthenticated();
        EnsureLearningSessionExists(learningSessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, learningSessionId);
    }

    /// <summary>
    /// SignalR opens a new DI scope on every hub method invocation, not once per connection - so
    /// ICurrentUser/ICompanyContext (populated by CurrentUserMiddleware, an HTTP-only middleware
    /// that only ever runs during the handshake request) are never resolved in the scope a hub
    /// method actually runs in. Context.User, unlike ICurrentUser, IS the same authenticated
    /// ClaimsPrincipal for the whole connection lifetime (the JWT bearer handler authenticates it
    /// once via OnMessageReceived reading ?access_token=, per AuthenticationConfiguration.cs), and
    /// Context.GetHttpContext() keeps returning the original handshake request's query string
    /// (including ?company=) for the same reason - so this mirrors
    /// CurrentUserMiddleware.InvokeAsync's claim-reading and company resolution, just sourced from
    /// the Hub's Context instead of an HttpContext, and run before the guard on every invocation.
    /// Without this, EnsureLearningSessionExists' repository lookup runs behind the company query
    /// filter with ICompanyContext.CompanyId still null and returns "not found" for every session,
    /// even once EnsureAuthenticated itself passes.
    ///
    /// QA-02: Context.User is the ClaimsPrincipal the JWT bearer handler produced once, at
    /// handshake time. Unlike an HTTP request - which re-runs JwtBearerMiddleware's lifetime check
    /// on every call - a hub method invocation never re-validates the token, so a connection opened
    /// with a still-valid token keeps calling hub methods after that token expires. Two checks close
    /// that: the "exp" claim is read and compared to now (the same check JwtBearerMiddleware would
    /// have failed with on a fresh request), and RefreshCurrentUser() re-reads the account/company
    /// from the database (the same call CurrentUserMiddleware.InvokeAsync makes on every HTTP
    /// request) so a deactivated account or company is caught even if the token itself has not
    /// expired yet.
    /// </summary>
    private void EnsureAgentAuthenticated()
    {
        var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        var userId = Context.User?.FindFirst(AuthClaims.UserId)?.Value;
        var role = Context.User?.FindFirst(AuthClaims.Role)?.Value;

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(role))
        {
            var companyId = Context.User?.FindFirst(AuthClaims.CompanyId)?.Value;
            currentUser.Resolve(userId, role, string.IsNullOrEmpty(companyId) ? null : companyId);
        }

        var guard = serviceProvider.GetRequiredService<IAuthorizationGuard>();
        try
        {
            guard.EnsureAuthenticated();
            EnsureTokenNotExpired();
            serviceProvider.GetRequiredService<IAuthService>().RefreshCurrentUser();

            var query = Context.GetHttpContext()?.Request.Query ?? new QueryCollection();
            var requestedCompany = CurrentUserMiddleware.ResolveRequestedCompany(query, currentUser);
            if (requestedCompany is not null)
            {
                guard.EnsureCanAccessCompany(requestedCompany);
                serviceProvider.GetRequiredService<ICompanyContext>().Resolve(requestedCompany);
            }
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// ValidateLifetime in AuthenticationConfiguration only runs at handshake time for a SignalR
    /// connection (see the class-level remarks above), so a long-lived connection must re-check the
    /// standard JWT "exp" claim itself on every hub method call rather than trust the handshake's
    /// one-time validation.
    /// </summary>
    private void EnsureTokenNotExpired()
    {
        var expClaim = Context.User?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnixSeconds))
        {
            throw new HubException("เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่");
        }

        if (DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds) <= DateTimeOffset.UtcNow)
        {
            throw new HubException("เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่");
        }
    }

    private void EnsureLearningSessionExists(string learningSessionId)
    {
        var learningSessionService = serviceProvider.GetRequiredService<ILearningSessionService>();
        try
        {
            learningSessionService.GetById(learningSessionId);
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
