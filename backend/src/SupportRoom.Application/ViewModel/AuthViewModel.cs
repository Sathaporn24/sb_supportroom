namespace SupportRoom.Application.ViewModel;

/// <summary>What the frontend stores after a successful sign-in. Mirrors LoginResult in
/// domain.ts.</summary>
public sealed class LoginResultViewModel
{
    public required string Token { get; init; }

    /// <summary>ISO-8601. The frontend uses this to sign the user out before a request fails with
    /// a 401 rather than after.</summary>
    public required string ExpiresAt { get; init; }

    public required SignedInUserViewModel User { get; init; }
}

/// <summary>
/// The signed-in user's own profile. Never includes PasswordHash - and note this is a distinct
/// type from AdminUserViewModel (the management-list shape) on purpose: they answer different
/// questions and will drift apart, and collapsing them invites accidentally exposing a management
/// field to whoever can call /me.
/// </summary>
public sealed class SignedInUserViewModel
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string Role { get; init; }

    /// <summary>Null for an owner, who spans every company.</summary>
    public string? CompanyId { get; init; }

    /// <summary>When true the frontend must route to the change-password screen and keep the rest
    /// of the back office out of reach - the seeded first owner's password came from an
    /// environment variable that whoever deployed can read.</summary>
    public required bool MustChangePassword { get; init; }
}
