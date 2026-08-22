import type { AdminRole } from "@/types/domain";

/**
 * "เห็น" (visibility) กับ "แก้" (edit) เป็นคนละแกน (design.md §Company Settings Page Rules,
 * SP-15) - ไฟล์นี้มีแค่ type + pure function ตัดสินสิทธิ์ ไม่มี logic อื่นปนตาม SP-15 ข้อ 1
 */
export type SettingsSectionAccess = {
  /** role ที่ไม่อยู่ในรายการนี้ = "ไม่ render section นี้เลย" ไม่ใช่ disabled ไม่ใช่กล่อง 403 */
  visibleToRoles: readonly AdminRole[];
  /** อยู่ใน visibleToRoles แต่ไม่อยู่ในนี้ = เห็นค่าจริงแบบอ่านอย่างเดียวตาม SP-4 */
  editableByRoles: readonly AdminRole[];
};

export type SectionAccessResult = {
  visible: boolean;
  canEdit: boolean;
};

/**
 * role ที่มองไม่เห็น section นี้แก้ไม่ได้เสมอ ไม่ว่า editableByRoles จะระบุอะไรไว้ (SP-15
 * invariant ข้อ 3: editableByRoles ⊆ visibleToRoles) - ฟังก์ชันนี้บังคับ invariant นั้นเองที่
 * runtime แทนที่จะเชื่อว่า access ที่ส่งเข้ามาถูกต้องเสมอ
 */
export function resolveSectionAccess(
  access: SettingsSectionAccess,
  role: AdminRole,
): SectionAccessResult {
  const visible = access.visibleToRoles.includes(role);
  const canEdit = visible && access.editableByRoles.includes(role);
  return { visible, canEdit };
}
