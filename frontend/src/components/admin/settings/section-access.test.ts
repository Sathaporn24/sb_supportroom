import { describe, expect, it } from "vitest";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { LESSON_PACING_SECTION_ACCESS } from "@/components/admin/settings/lesson-pacing-access";

describe("resolveSectionAccess (SP-15 ข้อ 10)", () => {
  it("owner sees and can edit the pacing section", () => {
    expect(resolveSectionAccess(LESSON_PACING_SECTION_ACCESS, "owner")).toEqual({
      visible: true,
      canEdit: true,
    });
  });

  it("admin sees and can edit the pacing section", () => {
    expect(resolveSectionAccess(LESSON_PACING_SECTION_ACCESS, "admin")).toEqual({
      visible: true,
      canEdit: true,
    });
  });

  it("cs sees the pacing section read-only", () => {
    expect(resolveSectionAccess(LESSON_PACING_SECTION_ACCESS, "cs")).toEqual({
      visible: true,
      canEdit: false,
    });
  });

  it("a role missing from visibleToRoles can never edit either (SP-15 invariant 3)", () => {
    const hiddenSection = {
      visibleToRoles: ["owner"] as const,
      editableByRoles: ["owner", "admin"] as const,
    };

    expect(resolveSectionAccess(hiddenSection, "admin")).toEqual({
      visible: false,
      canEdit: false,
    });
  });
});
