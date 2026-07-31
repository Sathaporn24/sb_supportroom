# Supabase Setup and Schema

> สถานะ: **Prepared — Credentials Required, Migration ยังไม่ถูก Apply**
> Entry points: `src/providers/data/supabase/*`, `supabase/migrations/0001_initial_schema.sql`

Supabase **ไม่ใช่**ที่เก็บเนื้อหาบทเรียน (Google Slides ทำหน้าที่นั้น) — เก็บเฉพาะ
Config (`lessons`, `lesson_slide_configs`) และประวัติการใช้งาน (`training_sessions`,
`session_questions`, `session_results`) ดู Schema เต็มที่ [ER_DIAGRAM.md](./ER_DIAGRAM.md)

## ขั้นตอน

1. **สร้าง Project** ที่ [supabase.com](https://supabase.com/dashboard)
2. **คัดลอก Credentials** — Project Settings → API:
   - `Project URL` → `NEXT_PUBLIC_SUPABASE_URL`
   - `anon public` key → `NEXT_PUBLIC_SUPABASE_ANON_KEY` (ยังไม่ถูกใช้จริงในโค้ดตอนนี้
     แต่เก็บไว้เผื่ออนาคตทำ Client-side Realtime/Auth)
   - `service_role` key → `SUPABASE_SERVICE_ROLE_KEY` — **Secret ที่สุด ห้ามหลุดไป
     Client เด็ดขาด** ใช้เฉพาะใน `src/providers/data/supabase/client.ts` (มี
     `import "server-only"` กันไว้)
3. **รัน Migration** — ใช้ Supabase CLI หรือ SQL Editor ใน Dashboard รันไฟล์
   `supabase/migrations/0001_initial_schema.sql` ทั้งไฟล์ (สร้างตาราง + Trigger +
   RLS + Seed ข้อมูลบทเรียนตัวอย่าง 3 รายการ)
   ```bash
   supabase db push
   # หรือ
   psql "$SUPABASE_DB_URL" -f supabase/migrations/0001_initial_schema.sql
   ```
4. **ตั้งค่า Environment** ใน `.env.local` ตามข้อ 2
5. **สลับ Provider**: `DATA_PROVIDER=supabase`
6. **ตรวจ Table** — ใน Dashboard → Table Editor ควรเห็น 5 ตารางพร้อมข้อมูล Seed 3
   บทเรียน (`login-mobile`, `login-web`, `forgot-password` ทั้งหมด `is_active = false`
   จนกว่าจะตั้งค่า Slides URL แล้ว Activate ผ่าน Admin UI)
7. **ทดสอบ Create/List/Get/End Session**:
   - Admin UI → สร้างบทเรียน (ใส่ Slides URL จริง, Sync, เปิดใช้งาน) → บันทึก →
     ตรวจว่าแถวใน `lessons`/`lesson_slide_configs` อัปเดต
   - สร้าง Session Link → ตรวจแถวใหม่ใน `training_sessions`
   - เข้าห้องสอนจนจบ → ตรวจ `training_sessions.status = 'ENDED'` และมีแถวใหม่ใน
     `session_results`
   - ถามคำถามผ่าน Push-to-Talk → ตรวจแถวใหม่ใน `session_questions`

## Security และ RLS Plan

- ทุกตารางเปิด `ROW LEVEL SECURITY` แต่**ไม่มี Policy ให้ `anon`/`authenticated`**
  หมายความว่า Client ที่ถือ Anon Key เข้าถึงตารางเหล่านี้โดยตรงไม่ได้เลย (Deny by
  default)
- การเข้าถึงข้อมูลทั้งหมดผ่าน **Service Role Key ฝั่ง Server เท่านั้น** (Bypass RLS
  โดยธรรมชาติของ Service Role) — ตรงกับสถาปัตยกรรมที่ Route Handler เป็นประตูเดียว
- เมื่อเพิ่ม Authentication ให้ CS ในอนาคต (นอก Scope เฟสนี้) ค่อยพิจารณาเปิด Policy
  ให้ Authenticated User อ่าน/เขียนเฉพาะ Session ของตัวเอง แทนการใช้ Service Role
  ทุกจุด

## ห้ามใช้ Prisma/ORM

Repository ทุกตัว (`supabase-*-repository.ts`) เรียก `@supabase/supabase-js` Query
Builder โดยตรง ไม่มี ORM/Query Generator อื่นแทรกกลาง ตาม Prompt ข้อ 18

## Type Mapping

แต่ละ Supabase Repository มี `toXxx()` Mapper แปลง Row (`snake_case`) เป็น Domain
Type (`camelCase`) เขียนมือทั้งหมด — ดูตัวอย่างที่
`src/providers/data/supabase/supabase-session-repository.ts`
