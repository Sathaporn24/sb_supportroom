# Environment Setup

## ไฟล์ Environment

- `.env.example` — Template มีทุกตัวแปร ค่าว่างหมด ปลอดภัยที่จะ commit
- `.env.local` — ค่าจริงของเครื่องคุณ **ต้องไม่ commit** (`.gitignore` กันไว้แล้ว)
  ปัจจุบัน (Mock Mode) มีแค่ค่า Provider switch + ค่า Default ไม่มี Secret ใด ๆ

## รายการตัวแปรทั้งหมด

| ตัวแปร | Public/Server | ค่าเริ่มต้น | ใช้ทำอะไร |
|---|---|---|---|
| `NEXT_PUBLIC_APP_URL` | Public | `http://localhost:3000` | อ้างอิงใน Docs/สคริปต์ (ไม่ได้ใช้ Runtime ในโค้ดปัจจุบัน) |
| `DATA_PROVIDER` | Server | `mock` | `mock` \| `supabase` |
| `SLIDES_PROVIDER` | Server | `mock` | `mock` \| `google` |
| `TTS_PROVIDER` | Server | `mock` | `mock` \| `huggingface` |
| `VOICE_QUESTION_PROVIDER` | Server | `mock` | `mock` \| `gemini` |
| `GOOGLE_SERVICE_ACCOUNT_PROJECT_ID` | **Server only** | - | ดู [GOOGLE_SLIDES_SETUP.md](./GOOGLE_SLIDES_SETUP.md) |
| `GOOGLE_SERVICE_ACCOUNT_EMAIL` | **Server only** | - | เดียวกัน |
| `GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY` | **Server only** | - | เดียวกัน (ใส่ด้วย `\n` แทน newline จริง) |
| `HUGGINGFACE_API_TOKEN` | **Server only** | - | ดู [HUGGINGFACE_TTS_SETUP.md](./HUGGINGFACE_TTS_SETUP.md) |
| `HUGGINGFACE_TTS_MODEL` | **Server only** | - | เดียวกัน |
| `HUGGINGFACE_TTS_ENDPOINT` | **Server only** | Derive จาก Model ID | เดียวกัน (Override ได้ถ้า Self-host) |
| `GEMINI_API_KEY` | **Server only** | - | ดู [GEMINI_INTEGRATION.md](./GEMINI_INTEGRATION.md) |
| `GEMINI_MODEL` | **Server only** | `gemini-1.5-flash` | เดียวกัน |
| `NEXT_PUBLIC_SUPABASE_URL` | Public* | - | ดู [SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md) |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | Public* | - | เดียวกัน (ปัจจุบันยังไม่ใช้ฝั่ง Client เลย ทุกอย่างผ่าน Service Role ฝั่ง Server) |
| `SUPABASE_SERVICE_ROLE_KEY` | **Server only** | - | เดียวกัน — **ห้ามเปิดเผยเด็ดขาด** |
| `MAX_VOICE_UPLOAD_MB` | Server | `5` | จำกัดขนาดไฟล์เสียงที่ `/api/voice-question` |
| `MIN_VOICE_DURATION_MS` | Server | `300` | เกณฑ์ No-speech |
| `DEFAULT_INTRO_WAIT_MS` | Server | `5000` | ค่าเริ่มต้นตอนสร้าง LessonConfig ใหม่ |
| `DEFAULT_BREATH_PAUSE_MS` | Server | `1000` | เดียวกัน |
| `DEFAULT_FINAL_QUESTION_WAIT_MS` | Server | `5000` | เดียวกัน |
| `DEFAULT_SESSION_EXPIRY_HOURS` | Server | `24` | ค่าเริ่มต้นตอนสร้าง Session ใหม่ |

\* `NEXT_PUBLIC_SUPABASE_URL`/`ANON_KEY` ใช้ Prefix `NEXT_PUBLIC_` ตามธรรมเนียม Supabase
แต่โค้ดปัจจุบัน (`src/providers/data/supabase/client.ts`) เรียกใช้จาก **Server เท่านั้น**
ผ่าน Service Role Key — ยังไม่มี Client-side Supabase Call ใด ๆ ในเฟสนี้

## Validation Behavior

- `src/config/env.ts` — `getProviderSelection()` ใช้ Zod `.enum(...).default("mock")`
  ค่าว่าง (`X=""`) ก็ตกไปที่ Default เหมือนไม่ได้ตั้งค่าเลย (มี Unit Test คุ้มครองไว้ที่
  `src/config/env.test.ts` และ `server-defaults.test.ts`)
- `getGoogleServiceAccountEnv()` / `getHuggingFaceEnv()` / `getGeminiEnv()` /
  `getSupabaseEnv()` — เรียก **เฉพาะตอน Provider นั้นถูกเลือกจริง** (Lazy) ถ้าขาดตัวแปร
  จะ throw `MissingEnvError` พร้อมรายชื่อตัวแปรที่ขาด **ไม่แสดงค่า Secret ใด ๆ ใน
  Error Message**
- Private Key ของ Google แปลง `\\n` เป็น newline จริงให้อัตโนมัติ

## Mock Mode ไม่ต้องตั้งค่าอะไรเลย

`npm run dev` รันได้ทันทีโดยไม่มี `.env.local` — ทุก Provider Default เป็น Mock
