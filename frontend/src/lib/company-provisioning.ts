export const ACTIVE_COMPANY_ID_ERROR = "รหัสบริษัทนี้ถูกใช้งานแล้ว";

export const INACTIVE_COMPANY_ID_ERROR =
  "มีบริษัทรหัสนี้อยู่แล้วแต่ถูกปิดใช้งาน หากต้องการใช้งานอีกครั้ง ให้เปิดกลับจากหน้ารายการบริษัท ไม่ใช่สร้างใหม่";

export const ADMIN_EMAIL_ERROR = "อีเมลนี้ถูกใช้งานแล้ว";

export const DEACTIVATE_COMPANY_CONFIRMATION =
  "พนักงานของบริษัทนี้จะเข้าสู่ระบบไม่ได้ทันที แต่ลิงก์เรียนที่แจกออกไปแล้วยังใช้งานได้จนกว่าจะหมดอายุ";

export type CompanyCreateFieldErrors = {
  id?: string;
  adminEmail?: string;
};

/** CP-4/CP-5 are field errors; other server failures stay in the form-level Alert. */
export function getCompanyCreateFieldErrors(message: string): CompanyCreateFieldErrors | null {
  if (message === ACTIVE_COMPANY_ID_ERROR || message === INACTIVE_COMPANY_ID_ERROR) {
    return { id: message };
  }
  if (message === ADMIN_EMAIL_ERROR) {
    return { adminEmail: message };
  }
  return null;
}
