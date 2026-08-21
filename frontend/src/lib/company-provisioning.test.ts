import { describe, expect, it } from "vitest";
import {
  ACTIVE_COMPANY_ID_ERROR,
  ADMIN_EMAIL_ERROR,
  DEACTIVATE_COMPANY_CONFIRMATION,
  INACTIVE_COMPANY_ID_ERROR,
  getCompanyCreateFieldErrors,
} from "@/lib/company-provisioning";

describe("company provisioning UI contract", () => {
  it.each([ACTIVE_COMPANY_ID_ERROR, INACTIVE_COMPANY_ID_ERROR])(
    "maps slug duplicate '%s' to the company id field",
    (message) => {
      expect(getCompanyCreateFieldErrors(message)).toEqual({ id: message });
    },
  );

  it("maps the non-enumerating duplicate email message to the email field", () => {
    expect(getCompanyCreateFieldErrors(ADMIN_EMAIL_ERROR)).toEqual({ adminEmail: ADMIN_EMAIL_ERROR });
  });

  it("leaves unrelated failures for the form-level alert", () => {
    expect(getCompanyCreateFieldErrors("สร้างบริษัทไม่สำเร็จ")).toBeNull();
  });

  it("keeps the accepted learner-link consequence in the deactivate confirmation", () => {
    expect(DEACTIVATE_COMPANY_CONFIRMATION).toBe(
      "พนักงานของบริษัทนี้จะเข้าสู่ระบบไม่ได้ทันที แต่ลิงก์เรียนที่แจกออกไปแล้วยังใช้งานได้จนกว่าจะหมดอายุ",
    );
  });
});
