using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class CreateAdminUserDto
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(100, ErrorMessage = "ชื่อผู้ใช้ยาวเกินไป")]
    public required string DisplayName { get; init; }

    /// <summary>AdminRole value. Rejected when it outranks the caller's own role - see
    /// IAuthorizationGuard.EnsureCanAssignRole.</summary>
    [Required(ErrorMessage = "กรุณาเลือกสิทธิ์")]
    public required string Role { get; init; }

    /// <summary>
    /// Ignored for an owner (who belongs to no single company) and required otherwise. An owner
    /// creating a user must state which company it belongs to; a company admin may only ever
    /// create users in their own, so the value is checked against theirs rather than trusted.
    /// </summary>
    public string? CompanyId { get; init; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านเริ่มต้น")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.TooShortTh)]
    public required string InitialPassword { get; init; }
}

public sealed class UpdateAdminUserDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(100, ErrorMessage = "ชื่อผู้ใช้ยาวเกินไป")]
    public required string DisplayName { get; init; }

    [Required(ErrorMessage = "กรุณาเลือกสิทธิ์")]
    public required string Role { get; init; }

    public required bool IsActive { get; init; }
}
