namespace SupportRoom.Application.ViewModel;

/// <summary>
/// One row on the user-management screen. Never carries PasswordHash - and note there is no
/// mapping registered from AdminUser to this type in MapsterConfig on purpose: it is built by
/// hand in the service so adding a field to the entity can never silently start returning it
/// over the wire.
/// </summary>
public sealed class AdminUserViewModel
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string Role { get; init; }
    public string? CompanyId { get; init; }
    public required bool IsActive { get; init; }
    public string? LastLoginAt { get; init; }
    public required string CreatedAt { get; init; }
}
