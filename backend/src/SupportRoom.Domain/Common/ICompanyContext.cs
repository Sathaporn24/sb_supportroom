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

/// <summary>
/// Where a request's company comes from while there is still no authentication (see TD-002).
///
/// Order: X-Company-Id header (only when ALLOW_COMPANY_HEADER=true) -> DEFAULT_COMPANY_ID.
///
/// ⚠️ The header path is a development scaffold, NOT access control - anyone can set a header.
/// It exists so multiple companies can be exercised locally before auth lands. Leave
/// ALLOW_COMPANY_HEADER unset in production: every request then resolves to DEFAULT_COMPANY_ID,
/// which is exactly the single-company behaviour the system had before, and the recipient-side
/// flows still resolve their real company from the session token regardless.
/// </summary>
public static class CompanyResolutionEnv
{
    public const string HeaderName = "X-Company-Id";

    /// <summary>Company assigned to existing rows by the CompanyId migration, and to any request
    /// that resolves no other way.</summary>
    public static string GetDefaultCompanyId() =>
        Environment.GetEnvironmentVariable("DEFAULT_COMPANY_ID") is { Length: > 0 } id ? id : "default";

    public static bool IsHeaderAllowed() =>
        string.Equals(Environment.GetEnvironmentVariable("ALLOW_COMPANY_HEADER"), "true", StringComparison.OrdinalIgnoreCase);
}
