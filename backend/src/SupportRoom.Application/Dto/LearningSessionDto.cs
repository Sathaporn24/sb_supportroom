using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

/// <summary>
/// The join screen posts exactly this: a name, plus the key the browser generated for itself.
/// No email, no phone, no organization - CS already recorded the organization on the link, and
/// none of the rest has a consumer (CORE_FEATURE_SPEC §5.1).
/// </summary>
public sealed class JoinLearningSessionDto
{
    [Required]
    [MaxLength(DtoLimits.RecipientNameMaxLength)]
    public required string RecipientName { get; init; }

    /// <summary>Opaque key the browser generates and persists. Picks which row on this link the
    /// caller owns - see LearningSession.LearnerKey.</summary>
    [Required]
    public required string LearnerKey { get; init; }
}

/// <summary>Sent on every slide change. Updates the existing row - never inserts (§2.3).</summary>
public sealed class UpdateLearningProgressDto
{
    [Required]
    public required string LearnerKey { get; init; }
    public string? LastSlideObjectId { get; init; }
    public int LastSlideIndex { get; init; }
}

public sealed class EndLearningSessionDto
{
    [Required]
    public required string LearnerKey { get; init; }
    [Required]
    public required bool CompletedAllSlides { get; init; }
    public string? LastSlideObjectId { get; init; }
    public int LastSlideIndex { get; init; }
}

/// <summary>
/// CS marking one AI answer. The note is what makes the review actionable: "incorrect" alone
/// cannot tell a missing document apart from a retrieval miss apart from the model inventing an
/// answer, and those are fixed in three different places (CORE_FEATURE_SPEC §2.7).
/// </summary>
public sealed class ReviewSessionQuestionDto
{
    [Required]
    public required string ReviewResult { get; init; }

    [MaxLength(DtoLimits.ReviewNoteMaxLength)]
    public string? ReviewNote { get; init; }
}
