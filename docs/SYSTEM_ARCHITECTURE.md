# System Architecture

> สถานะ: ตรงกับโค้ดจริงใน branch นี้ (Mock-first, Backend เตรียมพร้อมเชื่อมจริง)

## ภาพรวม

`sb_supportroom` ใช้ Next.js App Router เป็นทั้ง Frontend และ Backend ในโปรเจกต์เดียว
(Route Handlers ทำหน้าที่ Backend API) ไม่มี Backend แยกต่างหาก

```text
Browser UI (Client Components)
  ↓ fetch()
src/lib/api-client.ts        — Internal API Client ฝั่ง Browser จุดเดียวที่คุยกับ /api/*
  ↓ HTTP
src/app/api/**/route.ts      — Route Handlers (Backend)
  ↓
Provider / Repository Interfaces
  ├─ src/providers/slides      → SlidesContentProvider   (Mock / Google)
  ├─ src/providers/tts         → TextToSpeechProvider     (Mock / Hugging Face)
  ├─ src/providers/voice-question → VoiceQuestionProvider (Mock / Gemini)
  └─ src/providers/data        → LessonConfigRepository / SessionRepository /
                                   SessionQuestionRepository / SessionSummaryRepository
                                   (Mock in-memory / Supabase)
```

## กฎ Architecture ที่บังคับใช้จริงในโค้ด

1. **Component ห้ามเรียก Google/Gemini/Hugging Face/Supabase โดยตรง** — ทุก Client
   Component เรียกผ่าน `src/lib/api-client.ts` เท่านั้น ซึ่งยิง `fetch("/api/...")`
2. **Secret อยู่ฝั่ง Server เท่านั้น** — ไฟล์ที่แตะ Credential ทุกไฟล์ขึ้นต้นด้วย
   `import "server-only"` (เช่น `src/config/env.ts`, ทุกไฟล์ใน `src/providers/*/`,
   `src/providers/data/supabase/*`) ถ้ามีการ import เข้า Client Component โดยไม่ตั้งใจ
   Next.js จะ build fail ทันที
3. **Business Logic ของ Tutor Engine ไม่ผูกกับ SDK ผู้ให้บริการ** — ดู `src/tutor/`
   เป็น Pure Reducer (`tutor-reducer.ts`) รับ Event คืน `{ runtime, effect }` เท่านั้น
   ไม่รู้จัก Google Slides API, Gemini SDK หรือ Hugging Face เลย
4. **Factory จุดเดียวต่อ Provider** — `src/providers/*/index.ts` แต่ละตัวมี
   `createXxxProvider()` ที่ switch ตาม Environment Variable (`SLIDES_PROVIDER`,
   `TTS_PROVIDER`, `VOICE_QUESTION_PROVIDER`, `DATA_PROVIDER`) ไม่มีการกระจาย
   `if (provider === ...)` ไปตาม Component อื่น
5. **Mock เป็นค่าเริ่มต้นเสมอ** — ทุก Factory `default` ไปที่ Mock เมื่อไม่ได้ตั้งค่า
   Environment Variable (ทดสอบใน `src/providers/provider-factories.test.ts`)

## โครงสร้างโฟลเดอร์ (ส่วนที่เกี่ยวกับ Backend/Integration)

```text
src/
├── app/
│   ├── api/                      Route Handlers (Backend)
│   │   ├── health/route.ts
│   │   ├── slides/resolve/route.ts
│   │   ├── slides/content/route.ts
│   │   ├── tts/route.ts
│   │   ├── voice-question/route.ts
│   │   ├── lessons/route.ts
│   │   ├── lessons/[slug]/route.ts
│   │   ├── sessions/route.ts
│   │   ├── sessions/[token]/route.ts
│   │   ├── sessions/[token]/summary/route.ts
│   │   ├── session-questions/route.ts
│   │   └── admin/reset/route.ts
│   ├── admin/                    CS-facing pages (lessons, sessions)
│   ├── join/[token]/             Pre-join (camera/mic preview)
│   └── room/[token]/             Meeting room (Slides embed + Push-to-Talk)
├── config/
│   ├── env.ts                    Server-only env readers + validation (Zod)
│   ├── server-defaults.ts        Server-only defaults for new LessonConfig rows
│   └── tutor-config.ts           Client-safe constants (mirrors the env defaults)
├── lib/
│   ├── api-client.ts             Only place Browser code calls /api/*
│   └── api-response.ts           Shared ApiErrorResponse helper for Route Handlers
├── providers/
│   ├── slides/                   SlidesContentProvider (Mock, Google real skeleton)
│   ├── tts/                      TextToSpeechProvider (Mock, Hugging Face skeleton)
│   ├── voice-question/           VoiceQuestionProvider (Mock, Gemini skeleton)
│   └── data/                     4 repository interfaces, Mock (in-memory) + Supabase
├── tutor/                        Pure state machine (reducer + types + intents)
├── hooks/use-tutor-session.ts    Wires the reducer to React + browser APIs
└── types/                        domain.ts (LessonConfig/TrainingSession/...), api.ts
```

## Data Ownership

- **Google Slides = แหล่งเนื้อหาการสอนหลัก** เสมอ (Speaker Notes ของแต่ละ Slide คือ
  บทพูด) — ระบบไม่ Snapshot หรือ Copy เนื้อหานี้ลงฐานข้อมูล ทุกครั้งที่เข้าห้องจะอ่านจาก
  Slides Provider สด ๆ (`resolvePresentation` → `getLessonContent`)
- **Supabase (หรือ Mock in-memory) เก็บเฉพาะ Config และประวัติการใช้งาน**:
  `LessonConfig` (URL, ค่าจังหวะเวลา, `videoDurationMs` ต่อ Slide), `TrainingSession`,
  `SessionQuestion`, `SessionSummary` — ดู [ER_DIAGRAM.md](./ER_DIAGRAM.md)

## Mock Mode ทำงานอย่างไร

- `DATA_PROVIDER=mock` (ค่าเริ่มต้น): `src/providers/data/mock/store.ts` เป็น In-memory
  Map เก็บบน `globalThis` (กัน Next.js Dev Hot Reload ล้างข้อมูล) — **รีเซ็ตเมื่อรีสตาร์ท
  เซิร์ฟเวอร์** และไม่ persist ข้าม Process/Serverless Invocation ในโปรดักชัน
- `SLIDES_PROVIDER=mock`: มี Deck จำลอง 1 ชุด (`MOCK_PRESENTATION_ID` ใน
  `mock-slides-provider.ts`) ให้เดิน Flow ได้โดยไม่ต้องมี Google Credential
- `TTS_PROVIDER=mock`: สร้างไฟล์ WAV เงียบ (silent) ความยาวคำนวณจากจำนวนตัวอักษร
  ผ่าน `estimateSpeakingDurationMs` — Path เดียวกับที่ Client เล่นเสียงจริง
  (`<audio>` element + `ended` event) ต่างกันแค่เนื้อหาเสียง
- `VOICE_QUESTION_PROVIDER=mock`: ไม่ถอดเสียงจริง แต่ใช้ Transcript ตัวอย่างคงที่
  แล้ว Ground คำตอบกับ Speaker Notes จริงของบทเรียนนั้น ๆ

ดูสถานะ Integration แต่ละตัวแบบละเอียดใน [BACKEND_HANDOFF.md](./BACKEND_HANDOFF.md)
