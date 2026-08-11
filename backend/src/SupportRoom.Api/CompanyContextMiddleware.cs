using SupportRoom.Domain.Common;

namespace SupportRoom.Api;

/// <summary>
/// Gives every request a company before it reaches a controller, so admin-side endpoints (which
/// carry no session token) can query at all.
///
/// ⚠️ TEMPORARY until authentication lands (TD-002). Once there is a real identity provider this
/// must read the company from a verified claim; the X-Company-Id header is a development
/// convenience that any caller can set and is therefore NOT access control. It only works when
/// ALLOW_COMPANY_HEADER=true, so a production deployment that leaves that unset resolves every
/// request to DEFAULT_COMPANY_ID.
///
/// Recipient-side flows (join link, room, voice question, chat) do not rely on this: they
/// overwrite the company from the TrainingSession the token points at, which is the only value
/// that can be trusted today.
/// </summary>
public sealed class CompanyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICompanyContext companyContext)
    {
        var fromHeader = CompanyResolutionEnv.IsHeaderAllowed()
            && context.Request.Headers.TryGetValue(CompanyResolutionEnv.HeaderName, out var header)
            && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : null;

        companyContext.Resolve(fromHeader ?? CompanyResolutionEnv.GetDefaultCompanyId());

        await next(context);
    }
}
