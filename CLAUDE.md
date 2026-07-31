# CLAUDE.md

คำแนะนำนี้สำหรับ Claude Code และทีมพัฒนาที่ทำงานในโปรเจกต์ `sb_supportroom`

## Project Overview

ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (คล้าย Video Call กับ AI ชื่อ "School Bright
Support") CS สร้าง Session Link ให้คุณครู เนื้อหาสอนดึงจาก **Google Slides** โดยตรง
(Speaker Notes = บทพูด) ผู้เรียนถามคำถามด้วยเสียงผ่าน **Push-to-Talk** (ไม่มี VAD)

## Current Phase

**Phase 2 — Backend + Real Provider Skeletons (Mock-first ยังเป็น Default)**

- Next.js App Router เป็นทั้ง Frontend และ Backend (Route Handlers ที่ `src/app/api/**`)
- Google Slides / Gemini / Hugging Face / Supabase มี Interface + Mock + Real
  Skeleton ครบ แต่**ยังไม่มี Integration ใดทดสอบกับบริการจริง**
- Mock เป็น Provider เริ่มต้นเสมอ รันได้โดยไม่มี `.env.local`

## Commands

```bash
npm install
npm run dev         # http://localhost:3000
npm run lint
npm run typecheck
npm run test         # Vitest unit tests
npm run build
```

## Architecture Rules (บังคับใช้จริง ไม่ใช่แค่แนวทาง)

1. Client Component **ห้าม** import โค้ดที่มี `import "server-only"` โดยตรง — คุยกับ
   Backend ผ่าน `src/lib/api-client.ts` เท่านั้น (`fetch("/api/...")`)
2. Secret ทุกตัว (Google/Gemini/Hugging Face/Supabase) อยู่ในไฟล์ที่มี
   `import "server-only"` เท่านั้น — ห้ามเพิ่ม Secret ใหม่นอกกติกานี้
3. Tutor Engine (`src/tutor/`) เป็น Pure Reducer ห้ามผูกกับ SDK ผู้ให้บริการหรือ Browser
   API ใด ๆ (`fetch`, `MediaRecorder`, `<audio>`) — สิ่งเหล่านั้นอยู่ใน
   `src/hooks/use-tutor-session.ts` เท่านั้น
4. Provider/Repository ใหม่ทุกตัวต้องมี Mock คู่กับ Real และเพิ่ม Case ใน Factory
   (`src/providers/*/index.ts`) — ห้ามกระจาย `if (provider === ...)` ไปที่อื่น
5. ห้ามใช้ Prisma/ORM — Supabase Repository เรียก `@supabase/supabase-js` ตรง ๆ
6. ห้ามสร้าง Slide Editor/Media CMS ใหม่ — เนื้อหาสอนแก้ผ่าน Google Slides เท่านั้น
   Admin UI แก้ได้แค่ Metadata (URL, videoDurationMs, ค่าจังหวะเวลา)

## Folder Map

```text
src/app/api/**              Route Handlers (Backend)
src/app/admin/**            CS Dashboard (ไม่มี Auth)
src/app/join|room/[token]   Public Teacher Flow
src/config/                 env.ts (server-only), server-defaults.ts (server-only),
                             tutor-config.ts (client-safe constants)
src/lib/                    api-client.ts (Browser→Backend), api-response.ts
src/providers/slides|tts|voice-question/   Interface + mock-*.ts + real skeleton + index.ts (factory)
src/providers/data/         repository-types.ts, mock/ (in-memory), supabase/ (real skeleton), index.ts (factory)
src/tutor/                  types.ts, intents.ts, scripts.ts, tutor-reducer.ts (pure)
src/hooks/use-tutor-session.ts   ต่อ Reducer เข้ากับ React + Browser API จริง
src/types/domain.ts         Domain Types ทั้งหมด (LessonConfig, TrainingSession, ...)
supabase/migrations/        SQL Schema (ยังไม่ Apply)
docs/                        เอกสารเต็ม ดู docs/SYSTEM_ARCHITECTURE.md เป็นจุดเริ่ม
```

## Integration Entry Points

| Service | Real Provider | Env ที่ต้องตั้ง |
|---|---|---|
| Google Slides | `src/providers/slides/google-slides-provider.ts` | `SLIDES_PROVIDER=google` + `GOOGLE_SERVICE_ACCOUNT_*` |
| Hugging Face TTS | `src/providers/tts/huggingface-tts-provider.ts` | `TTS_PROVIDER=huggingface` + `HUGGINGFACE_*` |
| Gemini | `src/providers/voice-question/gemini-voice-question-provider.ts` | `VOICE_QUESTION_PROVIDER=gemini` + `GEMINI_*` |
| Supabase | `src/providers/data/supabase/*` | `DATA_PROVIDER=supabase` + `SUPABASE_*` + รัน Migration |

รายละเอียดครบใน [docs/API_INTEGRATION_GUIDE.md](./docs/API_INTEGRATION_GUIDE.md)

## Environment Variable Map

ดูตารางเต็มที่ [docs/ENVIRONMENT_SETUP.md](./docs/ENVIRONMENT_SETUP.md) — สรุปสั้น:
Provider Switch (`DATA_PROVIDER`/`SLIDES_PROVIDER`/`TTS_PROVIDER`/
`VOICE_QUESTION_PROVIDER`) Default เป็น `mock` เสมอ ตัวแปร Credential ทั้งหมดเป็น
Server-only (ไม่มี `NEXT_PUBLIC_` ยกเว้น Supabase URL/Anon Key ที่เก็บไว้เผื่ออนาคตแต่
ยังไม่ถูกใช้ฝั่ง Client จริง)

## ไฟล์ที่ต้องอ่านก่อนแก้โค้ดส่วนนี้

| จะแก้... | อ่านก่อน |
|---|---|
| Tutor State Machine | `docs/STATE_MACHINE.md`, `src/tutor/tutor-reducer.test.ts` |
| Route Handler / API Contract | `docs/API_CONTRACT.md` |
| Provider ใหม่ | `docs/BACKEND_HANDOFF.md`, Interface ที่เกี่ยวข้องใน `src/providers/*/types.ts` |
| Database Schema | `docs/ER_DIAGRAM.md`, `supabase/migrations/0001_initial_schema.sql` |

## What is Mock / Prepared / Connected

- **Mock** = ทำงานได้จริง ไม่ต้องมี Credential (Default เสมอ)
- **Prepared** = เขียน Real Implementation ตาม Contract ของบริการแล้ว แต่ยังไม่เคย
  ทดสอบกับบริการจริง (สถานะปัจจุบันของทั้ง 4 Integration — ดู
  [docs/BACKEND_HANDOFF.md](./docs/BACKEND_HANDOFF.md))
- **Connected** = ทดสอบกับบริการจริงสำเร็จแล้ว — **ยังไม่มี Integration ใดถึงสถานะนี้**
  ห้ามรายงานว่า "เชื่อมสำเร็จแล้ว" จนกว่าจะทดสอบกับ Credential จริงจริง ๆ

## Definition of Done (ต่อการเปลี่ยนแปลง)

ดู [docs/DEVELOPMENT_CHECKLIST.md](./docs/DEVELOPMENT_CHECKLIST.md) — สรุปสั้น: Lint +
Typecheck + Test + Build ผ่านทั้งหมด, ไม่มี Secret หลุด Client, เอกสารที่เกี่ยวข้อง
อัปเดตตรงกับโค้ดจริง

## Document Update Checklist

แก้ Tutor Engine → อัปเดต `docs/STATE_MACHINE.md` + `docs/SEQUENCE_DIAGRAMS.md`
แก้ Route Handler → อัปเดต `docs/API_CONTRACT.md`
แก้ Schema → Migration ใหม่ (ห้ามแก้ของเดิม) + อัปเดต `docs/ER_DIAGRAM.md`
เพิ่ม Provider ใหม่ → อัปเดต `docs/BACKEND_HANDOFF.md` (ตาราง Status) + Setup Guide ใหม่
