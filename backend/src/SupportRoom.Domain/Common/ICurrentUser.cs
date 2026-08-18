namespace SupportRoom.Domain.Common;

/// <summary>
/// The signed-in back-office user for this request, resolved once from a verified JWT and read
/// by IAuthorizationGuard on every protected action.
///
/// Starts out unresolved, exactly like ICompanyContext: an anonymous request (the whole
/// learner-side surface) simply never resolves it, and any guard that needs a user throws rather
/// than assuming one. Absence must read as "nobody", never as "somebody trusted".
/// </summary>
public interface ICurrentUser
{
    /// <summary>Null on anonymous (learner-side) requests.</summary>
    string? UserId { get; }

    /// <summary>AdminRole value. Null on anonymous requests.</summary>
    string? Role { get; }

    /// <summary>Null for AdminRole.Owner (spans every company) and on anonymous requests. These
    /// two cases are NOT interchangeable - always check Role, never treat null as "sees all".</summary>
    string? CompanyId { get; }

    bool IsAuthenticated { get; }

    void Resolve(string userId, string role, string? companyId);
}

public sealed class CurrentUser : ICurrentUser
{
    public string? UserId { get; private set; }
    public string? Role { get; private set; }
    public string? CompanyId { get; private set; }

    public bool IsAuthenticated => UserId is not null;

    public void Resolve(string userId, string role, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("userId ต้องไม่เป็นค่าว่าง", nameof(userId));
        }
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("role ต้องไม่เป็นค่าว่าง", nameof(role));
        }

        UserId = userId;
        Role = role;
        CompanyId = companyId;
    }
}
