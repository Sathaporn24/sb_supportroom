import type { LessonPurgeState, LessonTrashUrgency } from "@/types/domain";

/**
 * LT-9's rendered state for one trash row - purely a display mapping over what the server already
 * computed (`urgency`/`purgeState`/`remainingDays`); the day-boundary arithmetic itself is a
 * backend concern (design.md LT-9's >14d/>7d/>24h thresholds), tested there.
 *
 * `purgeState: "purging"` wins over urgency entirely (LT-9: "ไม่มี action" once the worker has
 * claimed the job) - callers must check `badge.disableActions` before rendering restore/delete.
 */
export type LessonTrashBadge = {
  label: string;
  /** Tailwind classes layered on top of `<Badge variant="outline">` - no new color tokens, reuses
   * the existing `primary` (brand orange) and `destructive` semantic tokens as the yellow/red
   * accents (see CLAUDE.md's fixed-token list - this module has no dedicated "warning" token). */
  className?: string;
  variant: "outline" | "destructive";
  /** LT-9 - once purge has started, the row has no action at all, not just a disabled delete. */
  disableActions: boolean;
};

const YELLOW_BADGE_CLASSNAME = "border-primary/40 bg-primary/10 text-primary";

export function getLessonTrashBadge(
  purgeState: LessonPurgeState,
  urgency: LessonTrashUrgency,
  remainingDays: number,
): LessonTrashBadge {
  if (purgeState === "purging") {
    return { label: "กำลังลบถาวร", variant: "destructive", disableActions: true };
  }

  switch (urgency) {
    case "neutral":
      return { label: `เหลืออีก ${remainingDays} วัน`, variant: "outline", disableActions: false };
    case "yellow":
      return {
        label: `เหลืออีก ${remainingDays} วัน`,
        variant: "outline",
        className: YELLOW_BADGE_CLASSNAME,
        disableActions: false,
      };
    case "red":
      return { label: `เหลืออีก ${remainingDays} วัน`, variant: "destructive", disableActions: false };
    case "red_today":
      return { label: "จะถูกลบถาวรภายในวันนี้", variant: "destructive", disableActions: false };
    default:
      return { label: `เหลืออีก ${remainingDays} วัน`, variant: "outline", disableActions: false };
  }
}
