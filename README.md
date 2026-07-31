# sb_supportroom (SupportRoom AI)

ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (ประสบการณ์คล้าย Video Call กับเจ้าหน้าที่ CS ชื่อ
**School Bright Support**) เนื้อหาการสอนดึงจาก **Google Slides** โดยตรง (1 Slide = 1
ช่วงการสอน, Speaker Notes = บทพูดของ AI) คุณครูถามคำถามด้วยเสียงผ่าน **Push-to-Talk**

**Phase ปัจจุบัน**: Backend จริง (Next.js Route Handlers) + Provider/Repository
Interface ครบทั้ง 4 บริการภายนอก (Google Slides, Gemini, Hugging Face, Supabase) —
**Mock เป็นค่าเริ่มต้นเสมอ รันได้ทันทีโดยไม่มี Credential ใด ๆ**
ยังไม่มี Integration ใดถูกทดสอบกับบริการจริง (ดู [docs/BACKEND_HANDOFF.md](./docs/BACKEND_HANDOFF.md))

Spec เดิม: [`AI_Live_Tutor_Demo_Spec.md`](./AI_Live_Tutor_Demo_Spec.md) (Phase 1, มี
Annotation สถานะกำกับแต่ละหัวข้อ) — Business Logic ล่าสุดอยู่ใน
[docs/SYSTEM_LOGIC.md](./docs/SYSTEM_LOGIC.md)

## เทคโนโลยี

- Next.js 15 (App Router) เป็นทั้ง Frontend และ Backend + TypeScript (strict) + Tailwind CSS
- Zod สำหรับ Request/Env Validation
- Vitest สำหรับ Unit Test
- Provider Interfaces สำหรับ Google Slides / Gemini / Hugging Face TTS / Supabase
  (Mock เป็น Default, Real Implementation "Prepared")

## เริ่มต้นใช้งาน

```bash
npm install
cp .env.example .env.local   # ไม่บังคับ - ไม่มีไฟล์นี้ก็รันได้ (ทุกอย่าง default เป็น mock)
npm run dev
```

เปิด <http://localhost:3000> จะ redirect ไปที่ `/admin` โดยอัตโนมัติ

ตรวจสอบก่อนส่งงาน:

```bash
npm run lint
npm run typecheck
npm run test
npm run build
```

## Demo Flow (Mock Mode)

1. เปิด `/admin` → **จัดการบทเรียน** → เปิดบทเรียน "วิธีการ Login (mobile)" → กด
   **ตรวจสอบ/Sync Slides** (ดึง Mock Deck 6 Slide) → ติ๊ก **เปิดใช้งานบทเรียนนี้** → บันทึก
2. `/admin` → **สร้างลิงก์การสอน** → เลือกบทเรียนที่ "พร้อมใช้งาน" → กรอกชื่อครู/โรงเรียน
   (ไม่บังคับ) → สร้างลิงก์ → คัดลอก
3. เปิดลิงก์ในแท็บ/เบราว์เซอร์ใหม่ (ใน Mock Mode ข้อมูลอยู่ใน In-memory Store ฝั่งเซิร์ฟเวอร์
   ใช้ข้ามเบราว์เซอร์ได้ตราบใดที่ยังชี้ไปเซิร์ฟเวอร์เดียวกัน) → หน้า Pre-join → อนุญาต
   กล้อง/ไมค์ → **เข้าร่วมห้องสอน**
4. ห้องสอน — AI ทักทาย → รอ/กด **พร้อมแล้ว เริ่มเรียนเลย** → Slide เดินอัตโนมัติทีละ
   Slide พร้อมเสียง Mock TTS (ไฟล์ WAV เงียบ ความยาวตามจำนวนตัวอักษร)
5. กดค้างปุ่มไมค์ (**Push-to-Talk**) เพื่อถามคำถาม (Mock ใช้ Transcript ตัวอย่างคงที่
   แล้ว Ground คำตอบกับ Speaker Notes จริงของบทเรียน) ปล่อยเร็วเกินไปจะได้ยิน "ไม่มีคำพูด"
   แล้วกลับไปสอนต่อเงียบ ๆ
6. เดินจนจบทุก Slide → ฟังคำถามท้ายบทเรียน → เงียบจนหมดเวลา → กล่าวลา → จบ Session อัตโนมัติ
   (หรือกด **ออกจากห้อง** เพื่อจบก่อนกำหนด)
7. กลับ `/admin` → **ดูสรุป** ที่แถว Session นั้น
8. ปุ่ม **Reset Demo Data** ล้างข้อมูล Mock กลับเป็น Seed (ใช้ได้เฉพาะ Mock Mode)

ขั้นตอนละเอียดกว่านี้ + Checklist ทดสอบ: [docs/TESTING_GUIDE.md](./docs/TESTING_GUIDE.md)

## Environment Variables

```env
DATA_PROVIDER=mock              # mock | supabase
SLIDES_PROVIDER=mock            # mock | google
TTS_PROVIDER=mock               # mock | huggingface
VOICE_QUESTION_PROVIDER=mock    # mock | gemini
```

รายการตัวแปรทั้งหมด (รวม Credential ของแต่ละบริการ): [docs/ENVIRONMENT_SETUP.md](./docs/ENVIRONMENT_SETUP.md)
วิธีเปิดใช้งานแต่ละ Integration จริง: [docs/API_INTEGRATION_GUIDE.md](./docs/API_INTEGRATION_GUIDE.md)

## สถาปัตยกรรม

```text
Browser UI → src/lib/api-client.ts → Next.js Route Handlers (src/app/api/**)
           → Provider/Repository Interfaces → Mock (default) หรือ Real (Google Slides /
             Gemini / Hugging Face / Supabase)
```

- Tutor Engine (`src/tutor/tutor-reducer.ts`) เป็น Pure Reducer แยกขาดจาก UI และจาก
  SDK ผู้ให้บริการทุกตัว — ดู State Diagram เต็มใน [docs/STATE_MACHINE.md](./docs/STATE_MACHINE.md)
- Secret ทั้งหมดอยู่ฝั่ง Server เท่านั้น (`import "server-only"` กันไว้ทุกไฟล์ที่แตะ Credential)
- รายละเอียดสถาปัตยกรรมเต็ม: [docs/SYSTEM_ARCHITECTURE.md](./docs/SYSTEM_ARCHITECTURE.md)

## เอกสารทั้งหมด

ดูสารบัญและจุดเริ่มต้นที่แนะนำใน [docs/SYSTEM_ARCHITECTURE.md](./docs/SYSTEM_ARCHITECTURE.md)
และรายการ Diagram/Setup Guide/ADR ทั้งหมดในโฟลเดอร์ [`docs/`](./docs/)

## Known Limitations

- **ยังไม่มี Integration ใดทดสอบกับบริการจริง** (Google Slides/Gemini/Hugging
  Face/Supabase) — ทั้งหมดอยู่ในสถานะ "Prepared" ดู [docs/BACKEND_HANDOFF.md](./docs/BACKEND_HANDOFF.md)
- Mock Data เป็น In-memory ฝั่งเซิร์ฟเวอร์ — หายเมื่อรีสตาร์ทเซิร์ฟเวอร์
- Resume หลัง Push-to-Talk เป็นแบบ Restart ทั้ง Slide เสมอ ไม่ใช่ Resume ตำแหน่งกลาง
  ประโยค (ตามที่ Spec อนุญาตให้ทำได้เมื่อ Resume แม่นยำกว่านี้ซับซ้อนเกินไป)
- ไม่มี Authentication สำหรับ CS, ไม่มี Multi-device Sync, ไม่มีคะแนนหรือประเมินคุณครู
- รองรับการเรียนครั้งละหนึ่งอุปกรณ์ต่อ Session
