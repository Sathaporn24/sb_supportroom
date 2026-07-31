# ADR 0004: Next.js Full-stack (App Router) for the Demo

## Status

Accepted

## Context

เฟสนี้ต้องเพิ่ม Backend จริง (เรียก Google Slides/Gemini/Hugging Face/Supabase ซึ่งต้อง
มี Secret ฝั่ง Server) จากเดิมที่เป็น Static Frontend ล้วนบน `localStorage` มีตัวเลือกคือ
แยก Backend เป็น Service ต่างหาก (เช่น Express/Fastify แยก Repo) หรือใช้ Next.js Route
Handlers เป็น Backend ในโปรเจกต์เดียว

## Decision

ใช้ **Next.js App Router เป็นทั้ง Frontend และ Backend** ผ่าน Route Handlers
(`src/app/api/**/route.ts`) ไม่แยก Backend Service ต่างหากในเฟสนี้

## Consequences

**ข้อดี**:

- Repository เดียว, Deploy เดียว — เหมาะกับขนาด Demo/MVP ที่ทีมเล็ก
- Type Sharing ระหว่าง Client/Server ฟรี (Type เดียวกันใน `src/types/domain.ts` ใช้ทั้ง
  API Client และ Route Handler)
- Next.js จัดการ Bundling แยก Client/Server ให้อัตโนมัติ — `import "server-only"`
  ทำให้ Build Fail ทันทีถ้ามีการ Import โค้ด Server เข้า Client Bundle โดยไม่ตั้งใจ
  (ป้องกัน Secret หลุดตั้งแต่ Compile Time)

**ข้อเสีย/ความเสี่ยงที่ยอมรับ**:

- Scale แยก Frontend/Backend อิสระกันไม่ได้ถ้าโหลดสูงขึ้นมากในอนาคต (ต้อง Scale ทั้ง
  Next.js App ไปด้วยกัน)
- Route Handler บน Serverless Platform บางเจ้ามี Timeout สั้นกว่า Backend Service ทั่วไป
  ต้องระวังเรื่อง Latency ของ Audio Upload + Gemini/Hugging Face Round-trip โดยเฉพาะ
  ถ้า Cold Start ของ Hugging Face ช้า (ดู [INTEGRATION_ROADMAP.md](../INTEGRATION_ROADMAP.md))
- ถ้าต้องการ Background Job/Queue ในอนาคต (เช่น Batch Sync Slides หลายบทเรียนพร้อมกัน)
  Next.js Route Handler ไม่เหมาะกับงาน Long-running โดยตรง ต้องพิจารณาเพิ่ม Worker แยก
