namespace SupportRoom.Domain.Enums;

/// <summary>
/// R9/Module L - computed at read time from LessonConfig.PurgeStartedAt, never stored (same
/// reasoning as LinkStatus): "trash" vs "purging" is fully derivable from the existing DM-2
/// columns, so a stored copy would just be a second source of truth that can drift.
/// </summary>
public static class LessonPurgeState
{
    public const string Trash = "trash";
    public const string Purging = "purging";
}

/// <summary>
/// R9/LT-9 - the trash tab's countdown color band, computed from remainingDays at read time.
/// Thresholds (UTC, against the fixed 60-day retention - design.md O-18):
///   neutral: more than 14 days left
///   yellow:  7 to 14 days left
///   red:      24 hours to 7 days left
///   redToday: 24 hours or less left ("will be purged today")
/// </summary>
public static class LessonTrashUrgency
{
    public const string Neutral = "neutral";
    public const string Yellow = "yellow";
    public const string Red = "red";
    public const string RedToday = "red_today";
}
