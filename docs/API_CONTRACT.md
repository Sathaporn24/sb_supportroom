# API Contract

ตรงกับไฟล์ใน `src/app/api/**/route.ts` จริง Validation ทั้งหมดใช้ [Zod](https://zod.dev)
Error Shape กลาง (`src/types/api.ts`):

```ts
interface ApiErrorResponse {
  error: {
    code: "VALIDATION_ERROR" | "NOT_FOUND" | "UNAUTHORIZED" | "UPSTREAM_ERROR" | "CONFIG_ERROR" | "INTERNAL_ERROR";
    message: string;
    details?: unknown;
    requestId?: string;
  };
}
```

ไม่มี Route ใดคืน Stack Trace หรือ Secret ให้ Client — Authentication ปัจจุบัน: **ไม่มี**
(CS ไม่ต้อง Login ในเฟสนี้ ทุก Route เปิดสาธารณะ)

---

## `GET /api/health`

- **Purpose**: ตรวจสถานะเซิร์ฟเวอร์ + Provider ที่กำลังใช้งาน
- **Response 200**: `{ status: "ok", providers: ProviderSelection, timestamp: string }`

## `POST /api/slides/resolve`

- **Purpose**: ตรวจสอบ Google Slides URL และ derive `presentationId`/`embedUrl`
- **Provider**: `SlidesContentProvider.resolvePresentation`
- **Request**: `{ slidesSourceUrl: string; slidesEmbedUrl?: string }`
- **Response 200**: `{ presentationId: string | null; embedUrl: string; isEmbedOnly: boolean; warning?: string }`
- **Errors**: `400 VALIDATION_ERROR`, `502 UPSTREAM_ERROR` (Google API ล้มเหลว/ไม่มีสิทธิ์)

## `GET /api/slides/content?presentationId=xxx`

- **Purpose**: อ่าน Slide order + Speaker Notes สดจาก Google Slides
- **Provider**: `SlidesContentProvider.getLessonContent`
- **Response 200**: `SlidesLessonContent` — `{ presentationId, title, embedUrl, slides: [{ slideObjectId, index, speakerNotes, slideUrl? }], syncedAt }`
- **Errors**: `400 VALIDATION_ERROR` (ไม่มี presentationId), `502 UPSTREAM_ERROR`

## `POST /api/tts`

- **Purpose**: แปลงข้อความเป็นเสียง
- **Provider**: `TextToSpeechProvider.synthesize`
- **Request**: `{ text: string (1-2000 ตัวอักษร); voice?: string }`
- **Response 200**: Audio bytes ดิบ พร้อม `Content-Type` ตาม Provider (Mock = `audio/wav`)
- **Errors**: `400 VALIDATION_ERROR`, `502 UPSTREAM_ERROR` — **ไม่ Log ข้อความเต็มที่ส่งมา**

## `POST /api/voice-question`

- **Purpose**: รับเสียงคำถามจาก Push-to-Talk → ถอดเสียง → ตอบแบบ Grounded → บันทึกคำถาม
- **Provider**: `VoiceQuestionProvider.transcribeAndAnswer` + `SessionQuestionRepository.add`
- **Request**: `multipart/form-data`
  - `audio: File` (บังคับ, ต้องเป็น `audio/*` หรือ `video/webm`, ขนาด ≤ `MAX_VOICE_UPLOAD_MB`)
  - `lessonSlug: string` (บังคับ)
  - `sessionId: string` (บังคับ)
  - `durationMs: string` (ตัวเลขระยะเวลาอัด)
  - `currentSlideObjectId?: string`
- **Response 200**: `VoiceQuestionResult` — `{ transcript, answer, answerStatus, relatedSlideObjectId? }`
  `answerStatus ∈ answered | not_found | out_of_scope | no_speech | transcription_failed`
- **Errors**: `400 VALIDATION_ERROR` (ไฟล์ผิดชนิด/เกินขนาด/ขาดฟิลด์), `404 NOT_FOUND`
  (ไม่พบบทเรียนหรือยังไม่ตั้งค่า Slides), `502 UPSTREAM_ERROR`
- **Side effect**: บันทึก `SessionQuestion` เมื่อ `answerStatus !== "no_speech"`

## `GET /api/lessons`

- **Purpose**: รายการบทเรียนทั้งหมด (Admin ใช้เลือก/แก้ไข)
- **Response 200**: `{ lessons: LessonConfig[] }`

## `POST /api/lessons`

- **Purpose**: สร้าง/แก้ไข LessonConfig (upsert ตาม `slug`)
- **Request**: `LessonConfigInput` (ทุกฟิลด์ของ `LessonConfig` ยกเว้น `id`, `presentationId`,
  `createdAt`, `updatedAt` — `presentationId` ถูก Derive ใหม่จาก `slidesSourceUrl` เสมอฝั่ง Server)
- **Response 200**: `{ lesson: LessonConfig }`
- **Errors**: `400 VALIDATION_ERROR`

## `GET /api/lessons/[slug]`

- **Purpose**: โหลดบทเรียนสำหรับห้องสอน (LessonConfig + Slide เนื้อหาสด รวม `videoDurationMs`)
- **ใช้โดย**: `useTutorSession` (effect `LOAD_LESSON`)
- **Response 200**: `{ lesson: LessonConfig; embedUrl: string; slides: TeachingSlide[] }`
- **Errors**: `404 NOT_FOUND` (ไม่พบ/ยังไม่ Active), `409 CONFIG_ERROR` (ยังไม่ตั้งค่า Slides), `502 UPSTREAM_ERROR`

## `GET /api/sessions`

- **Purpose**: รายการ Session ทั้งหมด (Admin Dashboard)
- **Response 200**: `{ sessions: TrainingSession[] }`

## `POST /api/sessions`

- **Purpose**: สร้าง Session Link ใหม่
- **Request**: `{ lessonSlug: string; teacherName?: string; schoolName?: string; expiresAt?: string (ISO) }`
  — ถ้าไม่ระบุ `expiresAt` จะใช้ `getDefaultSessionExpiryHours()` (ค่าเริ่มต้น 24 ชั่วโมง)
- **Response 201**: `{ session: TrainingSession }`
- **Errors**: `400 VALIDATION_ERROR`, `404 NOT_FOUND` (lessonSlug ไม่มีจริง)

## `GET /api/sessions/[token]`

- **Purpose**: โหลด Session สำหรับหน้า Join/Room (Public, ไม่ต้อง Auth)
- **Response 200**: `{ session: TrainingSession; lessonTitle: string }`
- **Errors**: `404 NOT_FOUND`

## `PATCH /api/sessions/[token]`

- **Purpose**: Mark Session เริ่ม/จบ
- **Request** (discriminated union ตาม `action`):
  - `{ action: "start" }`
  - `{ action: "end"; completedAllSlides: boolean; lastSlideObjectId?: string }`
- **Response 200**: `{ session: TrainingSession }`
- **Side effect** (`action: "end"`): สร้าง `SessionSummary` ผ่าน `SessionSummaryRepository.save`
  โดย `unansweredPoints` มาจากคำถามที่ `answerStatus === "not_found"`
- **Errors**: `400 VALIDATION_ERROR`, `404 NOT_FOUND`

## `GET /api/sessions/[token]/summary`

- **Purpose**: สรุปผลการสอน (Admin ดูหลังจบ Session)
- **Response 200**: `{ session: TrainingSession; summary: SessionSummary | null }`
  (`summary` เป็น `null` ถ้า Session ยังไม่จบ)
- **Errors**: `404 NOT_FOUND`

## `GET /api/session-questions?sessionId=xxx`

- **Purpose**: คำถามที่ถามระหว่าง Session (Live view ก่อน Session จบ, Admin ใช้)
- **Response 200**: `{ questions: SessionQuestion[] }`
- **หมายเหตุ**: Read-only — การเขียนเกิดเฉพาะภายใน `/api/voice-question` เท่านั้น ไม่มี
  POST แยกที่ Route นี้เพื่อไม่ให้มี 2 ทางเขียนข้อมูลเดียวกัน
- **Errors**: `400 VALIDATION_ERROR`

## `POST /api/admin/reset`

- **Purpose**: รีเซ็ตข้อมูล Mock กลับเป็น Seed (เฉพาะ `DATA_PROVIDER=mock`)
- **Response 200**: `{ status: "reset" }`
- **Errors**: `409 CONFIG_ERROR` (ถ้าเรียกตอนใช้ `DATA_PROVIDER=supabase`)
