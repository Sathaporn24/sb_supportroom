using SupportRoom.Application.Exceptions;
using SupportRoom.Domain.Enums;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

/// <summary>
/// The rules here are the ONLY thing standing between one customer's staff and another customer's
/// data on the two tables that carry no EF query filter (Company and AdminUser - they are read
/// before a company is known, so a filter would match zero rows and nothing would work).
///
/// Everywhere else in this codebase a forgotten check still leaves the query filter as a backstop.
/// Not here. That is why these are tested directly rather than only through the services that call
/// them.
/// </summary>
public class AuthorizationGuardTests
{
    private const string CompanyA = "company-a";
    private const string CompanyB = "company-b";

    // ── the boundary: one customer must never reach another's data ────────────────────────────

    [Fact]
    public void Cs_RequestingAnotherCompany_IsRefused()
    {
        var guard = GuardFor(AdminRole.Cs, CompanyA);

        var ex = Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAccessCompany(CompanyB));

        // 403, not 401: they are signed in and signing in again would change nothing. A 401 would
        // send the frontend to a login screen that cannot help.
        Assert.Equal(ApiErrorCode.Forbidden, ex.Code);
    }

    [Fact]
    public void Admin_RequestingAnotherCompany_IsRefused()
    {
        var guard = GuardFor(AdminRole.Admin, CompanyA);
        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAccessCompany(CompanyB));
    }

    [Fact]
    public void CompanyScopedUser_RequestingOwnCompany_IsAllowed()
    {
        GuardFor(AdminRole.Cs, CompanyA).EnsureCanAccessCompany(CompanyA);
        GuardFor(AdminRole.Admin, CompanyA).EnsureCanAccessCompany(CompanyA);
    }

    [Fact]
    public void Owner_ReachesEveryCompany()
    {
        var guard = OwnerGuard();

        guard.EnsureCanAccessCompany(CompanyA);
        guard.EnsureCanAccessCompany(CompanyB);
    }

    /// <summary>
    /// A company-scoped account with no company is a corrupt row, and the tempting reading of
    /// "no company set" is "not restricted to one". It must fail closed instead - otherwise a
    /// single bad row silently becomes a master key.
    /// </summary>
    [Fact]
    public void CompanyScopedUser_WithNoCompany_IsRefusedRatherThanTreatedAsUnrestricted()
    {
        var guard = GuardFor(AdminRole.Cs, companyId: null);

        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAccessCompany(CompanyA));
    }

    // ── privilege escalation: the hole a user-management feature naturally opens ───────────────

    /// <summary>
    /// The escalation this whole rule exists for. A customer's admin genuinely may manage their own
    /// people, so every "can you manage users" check passes for them - and without a separate rank
    /// check they could hand themselves the owner role and read every other customer's data while
    /// only ever using the feature as intended.
    /// </summary>
    [Fact]
    public void Admin_CannotAssignOwner()
    {
        var guard = GuardFor(AdminRole.Admin, CompanyA);

        // Allowed to manage their own company's users...
        guard.EnsureCanManageUsers(CompanyA);

        // ...but not to hand out a role above their own.
        var ex = Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAssignRole(AdminRole.Owner));
        Assert.Equal(ApiErrorCode.Forbidden, ex.Code);
    }

    [Fact]
    public void Admin_CanAssignAdminAndCs()
    {
        var guard = GuardFor(AdminRole.Admin, CompanyA);

        guard.EnsureCanAssignRole(AdminRole.Admin);
        guard.EnsureCanAssignRole(AdminRole.Cs);
    }

    [Fact]
    public void Owner_CanAssignEveryRole()
    {
        var guard = OwnerGuard();

        guard.EnsureCanAssignRole(AdminRole.Owner);
        guard.EnsureCanAssignRole(AdminRole.Admin);
        guard.EnsureCanAssignRole(AdminRole.Cs);
    }

    [Fact]
    public void Cs_CannotManageUsersAtAll()
    {
        var guard = GuardFor(AdminRole.Cs, CompanyA);

        // Refused even for their own company - managing people is not their job.
        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanManageUsers(CompanyA));
    }

    [Fact]
    public void Admin_CannotManageUsersOfAnotherCompany()
    {
        var guard = GuardFor(AdminRole.Admin, CompanyA);

        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanManageUsers(CompanyB));
    }

    /// <summary>An unknown role ranks below everything and can hand out nothing - a garbled or
    /// future role value must not accidentally outrank a real one.</summary>
    [Fact]
    public void UnknownRole_CanAssignNothing()
    {
        var guard = GuardFor("superuser", CompanyA);

        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAssignRole(AdminRole.Cs));
    }

    [Fact]
    public void UnknownTargetRole_IsRejectedAsInvalid()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(() => OwnerGuard().EnsureCanAssignRole("superuser"));

        Assert.Equal(ApiErrorCode.ValidationError, ex.Code);
    }

    // ── owner-only operations ─────────────────────────────────────────────────────────────────

    [Fact]
    public void OwnerOnlyOperations_AreRefusedForAdminAndCs()
    {
        Assert.Throws<HttpStatusCodeException>(() => GuardFor(AdminRole.Admin, CompanyA).EnsureOwner());
        Assert.Throws<HttpStatusCodeException>(() => GuardFor(AdminRole.Cs, CompanyA).EnsureOwner());
    }

    // ── anonymous requests ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The learner surface is anonymous and never calls this guard. But if a back-office endpoint
    /// is ever reached without a token, every check must refuse rather than read the absence of a
    /// user as permission.
    /// </summary>
    [Fact]
    public void AnonymousRequest_IsRefusedByEveryCheck()
    {
        var guard = AnonymousGuard();

        Assert.Throws<HttpStatusCodeException>(guard.EnsureAuthenticated);
        Assert.Throws<HttpStatusCodeException>(guard.EnsureOwner);
        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAccessCompany(CompanyA));
        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanManageUsers(CompanyA));
        Assert.Throws<HttpStatusCodeException>(() => guard.EnsureCanAssignRole(AdminRole.Cs));
    }

    [Fact]
    public void AnonymousRequest_IsUnauthorizedNotForbidden()
    {
        // 401 here is right: the frontend should send them to sign in, which will actually help.
        var ex = Assert.Throws<HttpStatusCodeException>(() => AnonymousGuard().EnsureCanAccessCompany(CompanyA));

        Assert.Equal(ApiErrorCode.Unauthorized, ex.Code);
    }
}
