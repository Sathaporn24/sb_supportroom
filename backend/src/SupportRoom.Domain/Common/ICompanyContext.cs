namespace SupportRoom.Domain.Common;

/// <summary>
/// The company this request belongs to. Scoped: resolved once per request, then read by
/// ApplicationDbContext's query filters on every query.
///
/// Deliberately starts out unresolved (null). The query filter compares CompanyId against it,
/// so an unresolved context matches zero rows rather than every row - a forgotten resolution
/// step shows up as "no data" instead of "everyone's data".
/// </summary>
public interface ICompanyContext
{
    /// <summary>Null until resolved. Never assume non-null in a filter expression.</summary>
    string? CompanyId { get; }

    /// <summary>
    /// Set once per request. Callers: the middleware (admin side) and any service that has just
    /// loaded a TrainingLink by its token (recipient side - see ITrainingLinkRepository.
    /// GetByToken, which bypasses the filter precisely so this can be resolved).
    /// </summary>
    void Resolve(string companyId);
}

public sealed class CompanyContext : ICompanyContext
{
    public string? CompanyId { get; private set; }

    public void Resolve(string companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
        {
            throw new ArgumentException("companyId ต้องไม่เป็นค่าว่าง", nameof(companyId));
        }
        CompanyId = companyId;
    }
}

// CompanyResolutionEnv used to live here: an X-Company-Id header (gated by ALLOW_COMPANY_HEADER)
// falling back to DEFAULT_COMPANY_ID. It was a development scaffold for exercising multiple
// companies before authentication existed, and was tolerable only while every user was School
// Bright staff.
//
// It is deleted rather than switched off. Once a customer's own people sign in (TD-014), a header
// any caller can set is a complete bypass of company isolation - and a disabled-by-default switch
// is one careless environment variable away from being on in production. A request's company now
// comes from a verified JWT plus an authorization check on every request (CurrentUserMiddleware),
// or - on the learner side - from TrainingLink.Token, which was always the only trustworthy source
// there.
