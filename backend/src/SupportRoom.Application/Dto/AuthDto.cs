using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class LoginDto
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    public required string Password { get; init; }
}

public sealed class ChangePasswordDto
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    public required string CurrentPassword { get; init; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.TooShortTh)]
    public required string NewPassword { get; init; }
}

/// <summary>
/// Kept next to the DTOs that enforce it so the rule and its message cannot drift apart.
/// Deliberately just a length floor: complexity rules (a digit, a symbol, mixed case) push people
/// toward predictable substitutions and a sticky note, while length is what actually costs an
/// attacker. Revisit if the organisation has its own policy to comply with.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 10;
    public const string TooShortTh = "รหัสผ่านต้องยาวอย่างน้อย 10 ตัวอักษร";
}
