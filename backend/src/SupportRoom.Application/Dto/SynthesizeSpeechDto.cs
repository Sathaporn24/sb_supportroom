using System.ComponentModel.DataAnnotations;

namespace SupportRoom.Application.Dto;

public sealed class SynthesizeSpeechDto
{
    [Required]
    [StringLength(DtoLimits.MaxTextLength, MinimumLength = 1)]
    public required string Text { get; init; }
    public string? Voice { get; init; }

    /// <summary>Pinned to an SSML percentage so a caller can't smuggle arbitrary prosody markup in.</summary>
    [RegularExpression(@"^[+-]\d{1,2}%$")]
    public string? Rate { get; init; }
}
