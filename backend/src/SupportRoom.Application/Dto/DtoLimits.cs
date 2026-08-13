namespace SupportRoom.Application.Dto;

/// <summary>Shared [StringLength]/[MaxLength] bounds - kept in one place so a change to one
/// caller's limit doesn't silently drift from another's identical-looking literal.</summary>
internal static class DtoLimits
{
    /// <summary>Chat message text and TTS input text share this ceiling today; split them out
    /// into their own constants if either input's requirements diverge.</summary>
    public const int MaxTextLength = 2000;

    /// <summary>A display label typed on the join screen, not an identity - long enough for any
    /// real name written any way someone likes, short enough that the field cannot be used as
    /// free storage.</summary>
    public const int RecipientNameMaxLength = 100;

    /// <summary>CS's free-text reason on a reviewed answer. Roomy on purpose: the whole point of
    /// the field is that the cause doesn't fit a fixed set of options yet.</summary>
    public const int ReviewNoteMaxLength = 2000;
}
