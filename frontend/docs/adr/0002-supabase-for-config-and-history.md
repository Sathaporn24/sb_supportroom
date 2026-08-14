# ADR 0002: Supabase for Config and History

## Status

**Superseded (2026-08-10)** — เดิม Accepted แต่ปัจจุบัน **เลิกใช้ Supabase แล้ว** ระบบย้ายที่เก็บ
Config/ประวัติไปไว้ที่ Backend .NET ซึ่งต่อ Postgres ผ่าน **EF Core** โดยตรง (ไม่ใช่ Supabase JS
Client) — ดู `backend/src/SupportRoom.Providers.Data/Migrations/` สำหรับ Schema จริง เอกสารนี้เก็บ
ไว้เป็นบันทึกประวัติการตัดสินใจเท่านั้น เนื้อหาด้านล่างสะท้อนบริบทตอนตัดสินใจ ไม่ใช่สถาปัตยกรรมปัจจุบัน

## Context

เฟส Mock-first เดิมเก็บทุกอย่างใน `localStorage` ของ Browser ทำให้ Session Link ใช้ได้
เฉพาะเบราว์เซอร์เดียวกัน และไม่มีที่เก็บ Config/ประวัติแบบถาวรข้ามเครื่อง ระบบต้องการ
ฐานข้อมูลจริงสำหรับ `LessonConfig`, `TrainingSession`, `SessionQuestion`, และ
`SessionSummary` — แต่**ไม่ใช่**สำหรับเนื้อหาการสอน (ดู ADR 0001)

## Decision

ใช้ Supabase (Postgres + Supabase JS Client โดยตรง ไม่ใช้ ORM) เป็นที่เก็บ Config และ
ประวัติการใช้งาน โดย:

- เข้าถึงผ่าน Service Role Key ฝั่ง Server เท่านั้น (ไม่มี Client-side Supabase Call)
- RLS เปิดทุกตาราง ไม่มี Policy ให้ Anon/Authenticated (Deny-by-default)
- Mock In-memory Repository เป็นค่าเริ่มต้น ไม่บังคับให้ต้องมี Supabase Project จึงจะ
  รันได้

## Consequences

**ข้อดี**: Postgres เป็นมาตรฐานที่ทีมส่วนใหญ่คุ้นเคย, Supabase ให้ Dashboard/Migration
Tool พร้อมใช้ทันทีโดยไม่ต้องดูแล Infra เอง, RLS ให้ Defense-in-depth เพิ่มจาก
Application-level Auth (ปัจจุบัน back office ใช้ JWT/RBAC แล้ว)

**ข้อเสีย/ความเสี่ยงที่ยอมรับ**:

- ยังไม่ได้ทดสอบกับ Project จริง (ดู [BACKEND_HANDOFF.md](../BACKEND_HANDOFF.md))
- Service Role Key เป็น Secret ที่ทรงพลังมาก (Bypass RLS ทั้งหมด) ต้องระวังเป็นพิเศษไม่
  ให้หลุดไป Client หรือ Log — ทุกไฟล์ที่แตะ Key นี้มี `import "server-only"` กัน
- ยังไม่ได้ออกแบบ Migration Strategy สำหรับการเปลี่ยน Schema ในอนาคต (Migration แรกยัง
  ไม่ถูก Apply ด้วยซ้ำ)
