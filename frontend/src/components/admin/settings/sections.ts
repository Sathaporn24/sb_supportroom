import type { ComponentType } from "react";
import type { SettingsSectionAccess } from "@/components/admin/settings/section-access";
import { LESSON_PACING_SECTION_ACCESS } from "@/components/admin/settings/lesson-pacing-access";
import { LessonPacingSettingsSection } from "@/components/admin/settings/LessonPacingSettingsSection";

export type SettingsSectionDescriptor = {
  id: string;
  access: SettingsSectionAccess;
  Component: ComponentType<{ companyId: string }>;
};

/**
 * Single registry every reader (the settings page, the sidebar menu gate) reads from - SP-15 ข้อ
 * 4/7 ห้าม hardcode รายชื่อ role หรือรายชื่อ section ที่จุดอื่น. Add a new section here only, once
 * its two access arrays are confirmed by the project owner (SP-15 ข้อ 8).
 */
export const SETTINGS_SECTIONS: readonly SettingsSectionDescriptor[] = [
  { id: "pacing", access: LESSON_PACING_SECTION_ACCESS, Component: LessonPacingSettingsSection },
];
