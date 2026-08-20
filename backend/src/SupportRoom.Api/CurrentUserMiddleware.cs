using SupportRoom.Application.Common;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Api;

/// <summary>
/// Turns a verified JWT into the two request-scoped values everything downstream reads:
/// ICurrentUser (who is asking) and ICompanyContext (whose data they are asking about).
///
/// Replaces the old CompanyContextMiddleware, which read an X-Company-Id header that any caller
/// could set. That was tolerable while every user was School Bright staff; now that a customer's
/// own people sign in, the same header would be a complete bypass of company isolation - so it,
/// ALLOW_COMPANY_HEADER and DEFAULT_COMPANY_ID are gone rather than merely switched off (TD-014).
///
/// Which company is being viewed comes from ?company= in the URL rather than from the token. That
/// is deliberate: an owner switches customers constantly, and a value baked into the token would
/// mean re-issuing it on every switch, would leak across browser tabs through shared storage, and
/// would keep working for minutes after access was revoked. A URL value is untrusted, checked
/// fresh on every request by IAuthorizationGuard, and gives the switcher a real address - so back,
/// refresh, bookmark and "send this link to a colleague" all behave.
///
/// Anonymous (learner-side) requests fall straight through with nothing resolved: those endpoints
/// resolve their own company from TrainingLink.Token, which is the only value they can trust.
/// </summary>
public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public const string CompanyQueryKey = "company";

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        ICompanyContext companyContext,
        IAuthorizationGuard guard)
    {
        var userId = context.User.FindFirst(AuthClaims.UserId)?.Value;
        var role = context.User.FindFirst(AuthClaims.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            // Not signed in. Leave both unresolved - the query filter then matches zero rows, so a
            // protected endpoint reached without auth returns nothing rather than everything.
            await next(context);
            return;
        }

        var tokenCompanyId = context.User.FindFirst(AuthClaims.CompanyId)?.Value;
        currentUser.Resolve(userId, role, string.IsNullOrEmpty(tokenCompanyId) ? null : tokenCompanyId);

        var requested = ResolveRequestedCompany(context, currentUser);
        if (requested is not null)
        {
            // Throws 403 for a company this user may not touch. Runs here, once, rather than in
            // every controller - Company and AdminUser have no query filter behind this check.
            guard.EnsureCanAccessCompany(requested);
            companyContext.Resolve(requested);
        }

        await next(context);
    }

    /// <summary>
    /// The company this request is about: whatever ?company= asked for, or - for a company-scoped
    /// user who omitted it - their own, since they have exactly one and typing it every time would
    /// be pointless ceremony.
    ///
    /// An owner who omits it gets null on purpose. They belong to no company, so there is no
    /// sensible default; guessing one would silently show them a customer they did not pick.
    /// Endpoints that need a company then fail with a clear "company not known" instead.
    /// </summary>
    private static string? ResolveRequestedCompany(HttpContext context, ICurrentUser currentUser)
    {
        if (context.Request.Query.TryGetValue(CompanyQueryKey, out var fromQuery)
            && !string.IsNullOrWhiteSpace(fromQuery))
        {
            return fromQuery.ToString().Trim();
        }

        return currentUser.Role != AdminRole.Owner ? currentUser.CompanyId : null;
    }
}
