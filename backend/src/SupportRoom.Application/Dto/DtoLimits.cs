namespace SupportRoom.Application.Dto;

/// <summary>Shared [StringLength]/[MaxLength] bounds - kept in one place so a change to one
/// caller's limit doesn't silently drift from another's identical-looking literal.</summary>
internal static class DtoLimits
{
    /// <summary>Chat message text and TTS input text share this ceiling today; split them out
    /// into their own constants if either input's requirements diverge.</summary>
    public const int MaxTextLength = 2000;
}
