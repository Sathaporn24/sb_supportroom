using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

/// <summary>
/// What CS fills in. RecipientName is gone on purpose: the link goes to a whole department and
/// each person types their own name on the join screen (CORE_FEATURE_SPEC §2.1/§2.2).
/// </summary>
public sealed class CreateTrainingLinkDto
{
    [Required]
    public required string LessonSlug { get; init; }

    /// <summary>The receiving organization - a school, a bank branch, a department. Optional, and
    /// a display label only (see TrainingLink.RecipientOrgName).</summary>
    public string? RecipientOrgName { get; init; }

    /// <summary>
    /// ISO-8601 string, optional - defaults to now + ServerDefaults.GetDefaultSessionExpiryHours()
    /// when omitted.
    /// </summary>
    public string? ExpiresAt { get; init; }

    /// <summary>null = unlimited. Values must be positive, but the stored limit is not enforced
    /// against attendance yet (CORE_FEATURE_SPEC §6).</summary>
    [Range(1, int.MaxValue)]
    public int? MaxAttendees { get; init; }
}
