using SupportRoom.Application.Exceptions;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Application.Common;

/// <summary>
/// The single place every back-office permission question is answered.
///
/// This exists as one class on purpose. Company and AdminUser carry no EF query filter (they are
/// consulted before a company is known - see ApplicationDbContext), so for those two this guard is
/// not a second layer of defence, it is the ONLY one. Scattering the same checks across controllers
/// would mean the day someone forgets one, a bank's data becomes readable by another customer and
/// nothing else catches it.
///
/// Every method throws rather than returning a bool: a caller that ignores a returned false is a
/// silent hole, a caller that ignores a throw is impossible.
/// </summary>
public interface IAuthorizationGuard
{
    /// <summary>Throws unless someone is signed in. The learner-side surface never calls this -
    /// those endpoints are anonymous by design.</summary>
    void EnsureAuthenticated();

    /// <summary>Throws unless the signed-in user may act on <paramref name="companyId"/>.
    /// Owner passes for any company; admin/cs only for their own.</summary>
    void EnsureCanAccessCompany(string companyId);

    /// <summary>System-wide operations: API key/provider settings, the company list, data reset,
    /// full reindex. Owner only - these affect every customer at once.</summary>
    void EnsureOwner();

    /// <summary>Adding, editing or deactivating accounts inside a company.</summary>
    void EnsureCanManageUsers(string companyId);

    /// <summary>
    /// Throws unless the signed-in user may hand out <paramref name="targetRole"/>. Separate from
    /// EnsureCanManageUsers because they answer different questions: a customer's admin genuinely
    /// may manage their own people (so that check passes), but must never be able to promote
    /// anyone - themselves included - to owner and walk straight into every other customer's data.
    /// </summary>
    void EnsureCanAssignRole(string targetRole);
}

public sealed class AuthorizationGuard(ICurrentUser currentUser) : IAuthorizationGuard
{
    public void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated)
        {
            throw GeneralException.Unauthorized("กรุณาเข้าสู่ระบบก่อน");
        }
    }

    public void EnsureCanAccessCompany(string companyId)
    {
        EnsureAuthenticated();

        if (currentUser.Role == AdminRole.Owner)
        {
            return;
        }

        // Note this compares against the user's OWN CompanyId, never against whatever the request
        // asked for. A company-scoped user with a null CompanyId is a broken row, not a wildcard -
        // it must fail closed here rather than fall through to "matches everything".
        if (string.IsNullOrEmpty(currentUser.CompanyId) || currentUser.CompanyId != companyId)
        {
            throw GeneralException.Forbidden("ไม่มีสิทธิ์เข้าถึงข้อมูลของบริษัทนี้");
        }
    }

    public void EnsureOwner()
    {
        EnsureAuthenticated();

        if (currentUser.Role != AdminRole.Owner)
        {
            throw GeneralException.Forbidden("เฉพาะผู้ดูแลระบบเท่านั้นที่ทำรายการนี้ได้");
        }
    }

    public void EnsureCanManageUsers(string companyId)
    {
        EnsureAuthenticated();

        if (currentUser.Role == AdminRole.Owner)
        {
            return;
        }

        if (currentUser.Role != AdminRole.Admin)
        {
            throw GeneralException.Forbidden("ไม่มีสิทธิ์จัดการผู้ใช้");
        }

        // An admin manages their own company's people and nobody else's.
        EnsureCanAccessCompany(companyId);
    }

    public void EnsureCanAssignRole(string targetRole)
    {
        EnsureAuthenticated();

        if (!AdminRole.IsValid(targetRole))
        {
            throw GeneralException.ValidationError("สิทธิ์ที่ระบุไม่ถูกต้อง");
        }

        if (!AdminRole.CanAssign(currentUser.Role!, targetRole))
        {
            throw GeneralException.Forbidden("ไม่สามารถกำหนดสิทธิ์ที่สูงกว่าสิทธิ์ของตัวเองได้");
        }
    }
}
