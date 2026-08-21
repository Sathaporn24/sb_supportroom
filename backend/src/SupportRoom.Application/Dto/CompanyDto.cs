using System.ComponentModel.DataAnnotations;
using SupportRoom.Domain;

namespace SupportRoom.Application.Dto;

public sealed class CreateCompanyDto
{
    /// <summary>Slug that becomes Company.Id and shows up in admin URLs - validated against
    /// CompanySlug, which also explains why it must not carry anything confidential.</summary>
    [Required(ErrorMessage = "กรุณากรอกรหัสบริษัท")]
    public required string Id { get; init; }

    [Required(ErrorMessage = "กรุณากรอกชื่อบริษัท")]
    [MaxLength(200, ErrorMessage = "ชื่อบริษัทยาวเกินไป")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string AdminEmail { get; init; }

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(100, ErrorMessage = "ชื่อผู้ใช้ยาวเกินไป")]
    public required string AdminDisplayName { get; init; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านเริ่มต้น")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.TooShortTh)]
    public required string AdminInitialPassword { get; init; }
}

public sealed class UpdateCompanyDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อบริษัท")]
    [MaxLength(200, ErrorMessage = "ชื่อบริษัทยาวเกินไป")]
    public required string Name { get; init; }

    public required bool IsActive { get; init; }
}
