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

/// <summary>
/// Input for PUT /api/companies/{companyId}/lesson-pacing (LP-9) - separate from
/// UpdateCompanyDto/PUT /api/companies/{id} on purpose, because that endpoint is owner-only
/// (CP-14) and this one must let a company's own admin edit its own pacing too. All three fields
/// are required NOT NULL ints - Company is the last layer of the pacing resolve chain (LP-1),
/// so there is no "unset" state to represent here, and a partial/null payload is rejected rather
/// than treated as "leave unchanged".
/// </summary>
public sealed class UpdateCompanyLessonPacingDto
{
    [Required, Range(0, 60_000, ErrorMessage = "introWaitMs ต้องอยู่ระหว่าง 0-60000 มิลลิวินาที")]
    public required int IntroWaitMs { get; init; }

    [Required, Range(0, 10_000, ErrorMessage = "breathPauseMs ต้องอยู่ระหว่าง 0-10000 มิลลิวินาที")]
    public required int BreathPauseMs { get; init; }

    [Required, Range(0, 120_000, ErrorMessage = "finalQuestionWaitMs ต้องอยู่ระหว่าง 0-120000 มิลลิวินาที")]
    public required int FinalQuestionWaitMs { get; init; }
}
