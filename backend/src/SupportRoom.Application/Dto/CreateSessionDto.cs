using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class CreateSessionDto
{
    [Required]
    public required string LessonSlug { get; init; }
    public string? TeacherName { get; init; }
    public string? SchoolName { get; init; }

    /// <summary>
    /// ISO-8601 string, optional - defaults to now + ServerDefaults.GetDefaultSessionExpiryHours()
    /// when omitted (mirrors sessions/route.ts's POST handler).
    /// </summary>
    public string? ExpiresAt { get; init; }
}
