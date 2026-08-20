using System.ComponentModel.DataAnnotations;

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
}

public sealed class UpdateCompanyDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อบริษัท")]
    [MaxLength(200, ErrorMessage = "ชื่อบริษัทยาวเกินไป")]
    public required string Name { get; init; }

    public required bool IsActive { get; init; }
}
