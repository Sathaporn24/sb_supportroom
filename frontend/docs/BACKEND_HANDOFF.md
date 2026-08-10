# Backend Handoff

เอกสารนี้สำหรับทีมที่รับงานต่อโดยตรง — บอกว่าอะไรอยู่ที่ไหน, ต้องทำอะไรต่อ, และวิธีทดสอบ

## สถานะ Integration

| Integration | Status | Entry Point | Credentials Needed | Remaining Work |
|---|---|---|---|---|
| Google Slides | Prepared — Credentials Required | `src/providers/slides/google-slides-provider.ts` | Service Account (Project ID, Email, Private Key) | สร้าง Service Account จริง, แชร์ไฟล์ทดสอบ, ยืนยัน Parse URL/Speaker Notes กับไฟล์จริง (ดู [GOOGLE_SLIDES_SETUP.md](./GOOGLE_SLIDES_SETUP.md)) |
| Hugging Face TTS | Prepared — Credentials Required | `src/providers/tts/huggingface-tts-provider.ts` | API Token + Model ID | ยังไม่ได้เลือก/ประเมิน Model ภาษาไทย, ยังไม่มี Retry/Timeout สำหรับ Cold Start (ดู [HUGGINGFACE_TTS_SETUP.md](./HUGGINGFACE_TTS_SETUP.md)) |
| Gemini | Prepared — Credentials Required | `src/providers/voice-question/gemini-voice-question-provider.ts` | API Key | ยังไม่ทดสอบกับเสียงจริง 4 สถานการณ์ (answered/not_found/out_of_scope/transcription_failed) (ดู [GEMINI_INTEGRATION.md](./GEMINI_INTEGRATION.md)) |
| Supabase | Prepared — Credentials Required, Migration ยังไม่ Apply | `src/providers/data/supabase/*` | URL + Anon Key + Service Role Key | สร้าง Project จริง, รัน Migration, ทดสอบ CRUD ครบ (ดู [SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md)) |

**ไม่มี Integration ใดถูกทดสอบกับบริการจริงในรอบงานนี้** — ทั้งหมด Implement ตาม
Contract ของแต่ละบริการอย่างละเอียด (อ่านจาก Documentation สาธารณะ) แต่ยังไม่เคยรันจริง
เพราะไม่มี Credential ให้ในงานรอบนี้

## Interface ที่ต้อง Implement (ถ้าจะเพิ่ม Provider ใหม่ทดแทน)

| Interface | ไฟล์ | Method |
|---|---|---|
| `SlidesContentProvider` | `src/providers/slides/types.ts` | `resolvePresentation`, `getLessonContent` |
| `TextToSpeechProvider` | `src/providers/tts/types.ts` | `synthesize` |
| `VoiceQuestionProvider` | `src/providers/voice-question/types.ts` | `transcribeAndAnswer` |
| `LessonConfigRepository` | `src/providers/data/repository-types.ts` | `list`, `getBySlug`, `save` |
| `SessionRepository` | เดียวกัน | `create`, `getByToken`, `getById`, `list`, `markStarted`, `end` |
| `SessionQuestionRepository` | เดียวกัน | `add`, `listBySession` |
| `SessionSummaryRepository` | เดียวกัน | `getBySessionId`, `save` |

## Route Handler ที่เกี่ยวข้อง

ดูรายละเอียดเต็มใน [API_CONTRACT.md](./API_CONTRACT.md) — สรุปไฟล์:

```text
src/app/api/health/route.ts
src/app/api/slides/resolve/route.ts
src/app/api/slides/content/route.ts
src/app/api/tts/route.ts
src/app/api/voice-question/route.ts
src/app/api/lessons/route.ts
src/app/api/lessons/[slug]/route.ts
src/app/api/sessions/route.ts
src/app/api/sessions/[token]/route.ts
src/app/api/sessions/[token]/summary/route.ts
src/app/api/session-questions/route.ts
src/app/api/admin/reset/route.ts
```

## Environment Variables

ดูตารางเต็มใน [ENVIRONMENT_SETUP.md](./ENVIRONMENT_SETUP.md)

## Database Table ที่เกี่ยวข้อง

`lessons`, `lesson_slide_configs`, `training_sessions`, `session_questions`,
`session_results` — Schema เต็มใน `supabase/migrations/0001_initial_schema.sql`,
ER Diagram ใน [ER_DIAGRAM.md](./ER_DIAGRAM.md)

## วิธีเปลี่ยนจาก Mock เป็น Real Provider

ไม่ต้องแก้โค้ดเลย แก้แค่ Environment Variable แล้ว Restart:

```env
SLIDES_PROVIDER=google              # แทน mock
TTS_PROVIDER=huggingface            # แทน mock
VOICE_QUESTION_PROVIDER=gemini      # แทน mock
DATA_PROVIDER=supabase              # แทน mock
```

พร้อม Credential ที่จำเป็นของแต่ละตัว (ดู [ENVIRONMENT_SETUP.md](./ENVIRONMENT_SETUP.md))
ทุก Factory (`src/providers/*/index.ts`) จะเลือก Implementation ใหม่ให้อัตโนมัติ

## วิธีทดสอบ

ดู [TESTING_GUIDE.md](./TESTING_GUIDE.md) — มีทั้ง Automated (`npm run
lint/typecheck/test/build`) และ Manual Flow แบบ Step-by-step

## Acceptance Criteria (จาก Prompt ต้นทาง ข้อ 21)

- [x] Mock Demo ยังเปิดและเดิน Flow ได้ (ทดสอบผ่าน Playwright ในรอบงานนี้ ดูหัวข้อ
      Manual Verification ในรายงานผลลัพธ์)
- [x] Google Slides ถูกกำหนดเป็น Content Source หลักใน Architecture และ UI
- [x] 1 Slide = 1 ช่วงการสอน, Speaker Notes = บทพูด
- [x] Push-to-Talk Logic ถูกเตรียมและไม่มี VAD
- [x] ทั้ง 4 External Service มี Interface + Real Provider Skeleton + Route Handler +
      Env Validation + Setup Guide
- [x] Supabase มี SQL Migration พร้อมใช้ แต่ยังไม่บังคับเชื่อม
- [x] Mock เป็น Default Provider ทุกตัว
- [x] Secret อยู่ฝั่ง Server เท่านั้น (`server-only` gate)
- [ ] **ยังไม่ได้ทดสอบกับ Credential จริงของทั้ง 4 บริการ** — เป็นงานถัดไปที่ทีม Backend
      ต้องทำตามลำดับใน [INTEGRATION_ROADMAP.md](./INTEGRATION_ROADMAP.md)

## Known Risks และข้อจำกัด

1. **Mock Data เป็น In-memory** (`src/providers/data/mock/store.ts`) — หายเมื่อ
   Restart Server และไม่แชร์ข้าม Process ถ้า Deploy แบบ Serverless หลาย Instance
   วิธีแก้: เปิด Supabase ก่อน Deploy จริง
2. **Google Slides iframe ควบคุม Cross-origin ไม่ได้เต็มที่** — ใช้ URL Fragment
   `#slide=id.<objectId>` + Force Reload iframe (`key` prop) เป็น Workaround ที่ยังไม่
   เคยทดสอบกับ Google Slides จริง (ทดสอบแล้วเฉพาะกับ Placeholder ที่ไม่มี Embed URL)
3. **Hugging Face Cold Start** อาจทำให้ Slide แรกของ Session หน่วงนานผิดปกติ ยังไม่มี
   Retry/Loading Indicator พิเศษสำหรับกรณีนี้
4. **Resume หลัง Push-to-Talk เป็นแบบ Restart-slide เสมอ** ไม่ใช่ Resume ตำแหน่งกลาง
   ประโยค (ตามที่ Prompt อนุญาตให้ทำได้เมื่อซับซ้อนเกินไป) — ถ้าต้องการ Resume แม่นยำกว่านี้
   ต้องเปลี่ยนสถาปัตยกรรม TTS ให้รองรับการเล่นต่อจากตำแหน่งที่ระบุได้ (Real TTS Provider
   บางเจ้ารองรับ SSML/Timestamp ที่อาจนำมาใช้ได้ในอนาคต)
5. **ไม่มี Authentication** ทั้ง `/admin/**` และ Route Handler ทุกตัว — เปิดสาธารณะทั้งหมด
   ตามที่ Prompt ระบุว่ายังไม่ต้องทำในเฟสนี้ แต่ต้องเพิ่มก่อนใช้งานจริงกับ CS หลายคน
