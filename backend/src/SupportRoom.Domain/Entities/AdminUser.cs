using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// Someone who signs in to the back office. NOT the person who receives a training link - that
/// person has no account at all and never will: their token is both their key and their scope
/// (see CORE_FEATURE_SPEC §2.2). Adding a login to the learner side would be a design regression,
/// not a feature.
///
/// ⚠️ Like Company, this entity is deliberately NOT ICompanyScoped and has NO query filter, for
/// two independent reasons:
///   1. Sign-in looks a user up by email BEFORE any company is known - a filter would match zero
///      rows and nobody could ever log in.
///   2. An `owner` has CompanyId = null, which a `CompanyId == context` filter can never match.
/// So every query here must scope itself, and IAuthorizationGuard is the only thing standing
/// between one customer's staff and another customer's data (see TD-014).
/// </summary>
public sealed class AdminUser : IEntityMaster<string>
{
    public required string Id { get; init; }

    /// <summary>
    /// Null ONLY for AdminRole.Owner, which is School Bright and spans every company. Required
    /// for admin/cs. Never infer "sees everything" from this being null - read Role instead, so
    /// a bug that blanks this column fails closed rather than handing out full access silently.
    /// </summary>
    public string? CompanyId { get; set; }

    /// <summary>AdminRole.Owner | AdminRole.Admin | AdminRole.Cs.</summary>
    public required string Role { get; set; }

    /// <summary>Unique across the whole system, not per company: sign-in supplies only an email,
    /// so a duplicate across two companies would make it impossible to tell which account is
    /// being logged into.</summary>
    public required string Email { get; set; }

    /// <summary>
    /// Null is legal: an account created for SSO-only sign-in has no password. Nullable from the
    /// start because making it required now would cost a migration the day external login lands,
    /// and costs nothing today.
    /// </summary>
    public string? PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Deactivation, not deletion - the audit trail on rows this person created still
    /// points at their id and must stay resolvable.</summary>
    public required bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Forces a password change on next sign-in. Set on the seeded first owner, whose
    /// password came from an environment variable and is therefore known to whoever deployed.</summary>
    public required bool MustChangePassword { get; set; }

    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }
}
