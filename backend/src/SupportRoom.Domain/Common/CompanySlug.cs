using System.Text.RegularExpressions;

namespace SupportRoom.Domain.Common;

/// <summary>
/// Company.Id is typed by a human and then travels in admin URLs as ?company=scb, so it ends up
/// in browser history, server access logs and referrer headers. Restricting it to a plain slug
/// does two things: it keeps those places free of anything confidential, and it keeps the value
/// URL-safe without escaping.
///
/// Enforced at the service boundary rather than by a database constraint because the message a
/// user sees for a bad slug should explain the rule, not surface a constraint violation.
/// </summary>
public static partial class CompanySlug
{
    public const int MaxLength = 40;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? value)
        => value is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(value);

    /// <summary>The rule in Thai, for the validation message - kept next to the rule so the two
    /// cannot drift apart.</summary>
    public const string RuleTh =
        "รหัสบริษัทใช้ได้เฉพาะ a-z, 0-9 และขีดกลาง (เช่น scb, school-bright) ยาวไม่เกิน 40 ตัว "
        + "และห้ามใส่ข้อมูลที่เป็นความลับ เพราะค่านี้ปรากฏใน URL";
}
