import type { Faq } from "@/types/domain";
import { OUT_OF_SCOPE_TEXT } from "@/config/response-texts";

export const faqSeed: Faq[] = [
  {
    id: "faq-username-field",
    question: "ช่องชื่อผู้ใช้งานกรอกอะไร",
    keywords: ["ชื่อผู้ใช้งาน", "username", "ช่องแรก", "กรอกอะไร"],
    answer: "ช่องนี้กรอกชื่อผู้ใช้งานหรือเบอร์โทรศัพท์ตามที่โรงเรียนกำหนดไว้ค่ะ",
    scope: "IN_LESSON",
    relatedMediaId: "login-username-highlight",
    active: true,
  },
  {
    id: "faq-forgot-password",
    question: "ลืมรหัสผ่านต้องทำอย่างไร",
    keywords: ["ลืมรหัสผ่าน", "รหัสผ่านไม่ได้", "จำรหัสผ่านไม่ได้"],
    answer: "หากลืมรหัสผ่าน แนะนำให้ติดต่อผู้ดูแลระบบของโรงเรียน หรือทีม CS เพื่อขอรีเซ็ตรหัสผ่านค่ะ",
    scope: "IN_LESSON",
    relatedMediaId: "login-error-example",
    active: true,
  },
  {
    id: "faq-school-not-found",
    question: "เลือกโรงเรียนไม่เจอ",
    keywords: ["หาโรงเรียนไม่เจอ", "เลือกโรงเรียนไม่ได้", "ไม่มีชื่อโรงเรียน", "โรงเรียนไม่เจอ"],
    answer: "ลองตรวจสอบการพิมพ์ชื่อโรงเรียนอีกครั้งค่ะ หากยังไม่พบ แนะนำให้ติดต่อผู้ดูแลระบบของโรงเรียนหรือทีม CS ค่ะ",
    scope: "IN_LESSON",
    relatedMediaId: "login-school-highlight",
    active: true,
  },
  {
    id: "faq-other-system",
    question: "คำถามเรื่องระบบอื่น",
    keywords: ["ระบบอื่น", "หน้าอื่น", "เมนูอื่น", "ฟังก์ชันอื่น", "รายงานผลการเรียน"],
    answer: "เรื่องนี้เป็นข้อมูลพื้นฐานของระบบค่ะ หากต้องการขั้นตอนละเอียด แนะนำให้ติดต่อทีม CS เพิ่มเติมนะคะ",
    scope: "SYSTEM_BASIC",
    active: true,
  },
  {
    id: "faq-out-of-scope-sample",
    question: "คำถามนอกเรื่อง",
    keywords: ["อากาศ", "ข่าว", "อาหารเที่ยง", "ดวง", "ฟุตบอล"],
    answer: OUT_OF_SCOPE_TEXT,
    scope: "OUT_OF_SCOPE",
    active: true,
  },
];
