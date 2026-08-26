import { describe, expect, it } from "vitest";
import { getLessonTrashBadge } from "@/components/admin/lesson-trash-display";

describe("getLessonTrashBadge (LT-9)", () => {
  it("purging wins over urgency and disables every action, regardless of remaining days", () => {
    const badge = getLessonTrashBadge("purging", "neutral", 30);
    expect(badge).toMatchObject({ label: "กำลังลบถาวร", variant: "destructive", disableActions: true });
  });

  it("neutral urgency renders the plain outline badge with no color override", () => {
    const badge = getLessonTrashBadge("trash", "neutral", 20);
    expect(badge).toMatchObject({ label: "เหลืออีก 20 วัน", variant: "outline", disableActions: false });
    expect(badge.className).toBeUndefined();
  });

  it("yellow urgency keeps the outline variant but adds the primary-tinted class", () => {
    const badge = getLessonTrashBadge("trash", "yellow", 10);
    expect(badge.variant).toBe("outline");
    expect(badge.className).toContain("text-primary");
    expect(badge.label).toBe("เหลืออีก 10 วัน");
    expect(badge.disableActions).toBe(false);
  });

  it("red urgency switches to the destructive variant but still shows remaining days", () => {
    const badge = getLessonTrashBadge("trash", "red", 3);
    expect(badge).toMatchObject({ label: "เหลืออีก 3 วัน", variant: "destructive", disableActions: false });
  });

  it("red_today shows the special ≤24h copy instead of a day count", () => {
    const badge = getLessonTrashBadge("trash", "red_today", 0);
    expect(badge).toMatchObject({
      label: "จะถูกลบถาวรภายในวันนี้",
      variant: "destructive",
      disableActions: false,
    });
  });

  it("every action-enabling state (everything but purging) leaves disableActions false", () => {
    for (const urgency of ["neutral", "yellow", "red", "red_today"] as const) {
      expect(getLessonTrashBadge("trash", urgency, 1).disableActions).toBe(false);
    }
  });
});
