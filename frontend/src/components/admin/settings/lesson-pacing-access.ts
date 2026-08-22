import type { SettingsSectionAccess } from "@/components/admin/settings/section-access";

/** SP-15 ข้อ 2 - เท่ากับ LP-9/SP-4 เดิมทุกประการ ไม่ใช่กฎใหม่: pacing เป็น section ที่ "ไม่อ่อนไหว"
 * เห็นได้ทุก role, แก้ได้เฉพาะ owner/admin (`cs` ถูกปฏิเสธจริงที่ server ตาม LP-9).
 *
 * แยกไว้เป็นไฟล์ `.ts` ของตัวเอง (ไม่รวมไว้ใน LessonPacingSettingsSection.tsx) เพื่อให้
 * section-access.test.ts import ค่านี้ตรงจาก production ได้ - Vitest esbuild transform parse
 * `.tsx` ไม่ได้เมื่อ tsconfig ตั้ง `jsx: "preserve"` (ที่ Next.js ต้องใช้) จึงต้องแยกออกมา
 */
export const LESSON_PACING_SECTION_ACCESS: SettingsSectionAccess = {
  visibleToRoles: ["owner", "admin", "cs"],
  editableByRoles: ["owner", "admin"],
};
