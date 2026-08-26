using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

/// <summary>
/// Input for POST /api/lessons/{id}/permanent-delete (LT-2/LT-10). ConfirmationTitle is compared
/// server-side, trimmed, ordinal-exact against the trashed lesson's real title - not a boolean
/// confirm flag like the confirm-gates elsewhere in this module (KL-21/KL-23). A typed confirmation
/// is the whole point: it forces the caller to see and copy the real title, not just click through.
/// </summary>
public sealed class PermanentDeleteLessonDto
{
    [Required]
    public required string ConfirmationTitle { get; init; }
}
