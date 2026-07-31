import type { Lesson } from "@/types/domain";
import { faqSeed } from "@/mocks/faq.mock";

export const loginLessonSeed: Lesson = {
  id: "lesson-login-app",
  code: "LOGIN_APP",
  title: "การเข้าสู่ระบบสำหรับคุณครู",
  language: "th",
  faqs: faqSeed,
  steps: [
    {
      id: "step-1",
      title: "แนะนำหน้าเข้าสู่ระบบ",
      order: 0,
      checkpointEnabled: true,
      checkpointPromptId: "understand-check",
      segments: [
        {
          id: "step-1-seg-1",
          order: 0,
          scriptText:
            "ตอนนี้คุณครูอยู่ที่หน้าเข้าสู่ระบบนะคะ ก่อนเริ่มใช้งาน ให้ตรวจสอบว่ากำลังเข้าเว็บไซต์ของระบบถูกต้องค่ะ",
          mediaId: "login-full-page",
          mockSpeakDurationMs: 4200,
        },
        {
          id: "step-1-seg-2",
          order: 1,
          scriptText: "ด้านบนของหน้าจอจะเป็นส่วนสำหรับเลือกโรงเรียนของคุณครูค่ะ",
          mediaId: "login-school-highlight",
          mockSpeakDurationMs: 3200,
        },
      ],
    },
    {
      id: "step-2",
      title: "กรอกชื่อผู้ใช้งาน",
      order: 1,
      checkpointEnabled: true,
      checkpointPromptId: "understand-check",
      segments: [
        {
          id: "step-2-seg-1",
          order: 0,
          scriptText: "ช่องแรกใช้สำหรับกรอกชื่อผู้ใช้งานที่โรงเรียนกำหนดให้นะคะ",
          mediaId: "login-username-highlight",
          mockSpeakDurationMs: 3000,
        },
        {
          id: "step-2-seg-2",
          order: 1,
          scriptText:
            "ข้อมูลในช่องนี้อาจเป็นชื่อผู้ใช้งานหรือเบอร์โทรศัพท์ ขึ้นอยู่กับข้อมูลที่โรงเรียนตั้งค่าไว้ค่ะ",
          mediaId: "login-username-example",
          mockSpeakDurationMs: 4000,
        },
      ],
    },
    {
      id: "step-3",
      title: "กรอกรหัสผ่าน",
      order: 2,
      checkpointEnabled: true,
      checkpointPromptId: "have-question",
      segments: [
        {
          id: "step-3-seg-1",
          order: 0,
          scriptText:
            "จากนั้นกรอกรหัสผ่านในช่องถัดไป โดยตรวจสอบตัวพิมพ์เล็ก ตัวพิมพ์ใหญ่ และภาษาของแป้นพิมพ์ให้ถูกต้องค่ะ",
          mediaId: "login-password-highlight",
          mockSpeakDurationMs: 4500,
        },
      ],
    },
    {
      id: "step-4",
      title: "เข้าสู่ระบบและกรณีไม่สำเร็จ",
      order: 3,
      checkpointEnabled: true,
      checkpointPromptId: "need-repeat",
      segments: [
        {
          id: "step-4-seg-1",
          order: 0,
          scriptText: "เมื่อข้อมูลครบแล้ว ให้กดปุ่มเข้าสู่ระบบค่ะ",
          mediaId: "login-button-demo",
          mockSpeakDurationMs: 2500,
        },
        {
          id: "step-4-seg-2",
          order: 1,
          scriptText:
            "หากเข้าสู่ระบบไม่สำเร็จ ให้ตรวจสอบชื่อผู้ใช้งานและรหัสผ่านอีกครั้ง หากยังใช้งานไม่ได้ ให้ติดต่อผู้ดูแลระบบของโรงเรียนค่ะ",
          mediaId: "login-error-example",
          mockSpeakDurationMs: 5200,
        },
      ],
    },
    {
      id: "step-5",
      title: "สรุป",
      order: 4,
      checkpointEnabled: false,
      checkpointPromptId: "understand-check",
      segments: [
        {
          id: "step-5-seg-1",
          order: 0,
          scriptText:
            "สรุปแล้ว คุณครูต้องเลือกโรงเรียน กรอกชื่อผู้ใช้งาน กรอกรหัสผ่าน และกดเข้าสู่ระบบค่ะ",
          mediaId: "login-summary",
          mockSpeakDurationMs: 4000,
        },
      ],
    },
  ],
};
