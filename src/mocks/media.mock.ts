import type { DemoMedia } from "@/types/domain";

export const mediaSeed: DemoMedia[] = [
  { id: "login-full-page", kind: "image", src: "/demo-media/login-full-page.svg", label: "หน้าเข้าสู่ระบบแบบเต็มหน้า" },
  { id: "login-school-highlight", kind: "image", src: "/demo-media/login-school-highlight.svg", label: "ไฮไลต์ช่องเลือกโรงเรียน" },
  { id: "login-username-highlight", kind: "image", src: "/demo-media/login-username-highlight.svg", label: "ไฮไลต์ช่องชื่อผู้ใช้งาน" },
  { id: "login-username-example", kind: "image", src: "/demo-media/login-username-example.svg", label: "ตัวอย่างการกรอกชื่อผู้ใช้งาน" },
  { id: "login-password-highlight", kind: "image", src: "/demo-media/login-password-highlight.svg", label: "ไฮไลต์ช่องรหัสผ่าน" },
  { id: "login-button-demo", kind: "image", src: "/demo-media/login-button-demo.svg", label: "สาธิตการกดปุ่มเข้าสู่ระบบ" },
  { id: "login-error-example", kind: "image", src: "/demo-media/login-error-example.svg", label: "ตัวอย่างข้อความเข้าสู่ระบบไม่สำเร็จ" },
  { id: "login-summary", kind: "image", src: "/demo-media/login-summary.svg", label: "ภาพสรุปขั้นตอนหน้าเข้าสู่ระบบ" },
];

export function getMediaById(id: string): DemoMedia | undefined {
  return mediaSeed.find((m) => m.id === id);
}
