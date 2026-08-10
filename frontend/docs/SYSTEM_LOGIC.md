# System Logic

สรุป Business Logic ที่ Implement จริง อ้างอิงไฟล์โค้ดที่บังคับใช้กติกาแต่ละข้อ

## 1. Google Slides เป็นแหล่งเนื้อหา

- **1 Slide = 1 ช่วงการสอน**, **Speaker Notes = บทพูด** — ไม่มี Syntax พิเศษใน Notes
  (`MockSlidesContentProvider`/`GoogleSlidesContentProvider` อ่าน Notes เป็น Plain Text
  ตรง ๆ ไม่ Parse Command ใด ๆ)
- เนื้อหาที่ใช้คือเวอร์ชันล่าสุดตอนเข้าห้อง — `GET /api/lessons/[slug]` เรียก
  `SlidesContentProvider.getLessonContent` สดทุกครั้ง ไม่มี Cache/Snapshot ระยะยาว
- Source URL กับ Embed URL เก็บแยกกัน (`LessonConfig.slidesSourceUrl` /
  `slidesEmbedUrl`) เพราะ Published URL ใช้ Identifier คนละแบบกับ Source URL
  (`src/utils/google-slides-url.ts` แยก `isPublished` ออกจาก `presentationId`)

## 2. การดำเนินบทเรียน (`src/tutor/tutor-reducer.ts`)

- เข้าห้อง → AI ทักทาย+ถามพร้อม (`introScript`) → รอ `introWaitMs` (Config ต่อบทเรียน) →
  เริ่มอัตโนมัติถ้าไม่ตอบ (`INTRO_TIMEOUT`)
- เดิน Slide ต่อเนื่องตามลำดับ **ไม่มี Checkpoint** ถามความเข้าใจระหว่างทาง และ
  **ไม่มี Progress Bar** (ดู [STATE_MACHINE.md](./STATE_MACHINE.md))
- จบ Slide สุดท้าย → สรุปสั้น + เปิดคำถามท้ายบทเรียน → เงียบเกิน `finalQuestionWaitMs`
  → กล่าวลา → จบ Session อัตโนมัติ

## 3. Slide ที่มีวิดีโอ

เนื่องจากอ่าน Event "วิดีโอจบ" จาก iframe ข้าม Origin ไม่ได้ ระบบใช้ `videoDurationMs`
ที่ Admin กำหนดต่อ Slide แทน:

```ts
slideDurationMs = Math.max(ttsAudioDurationMs, videoDurationMs ?? 0);
// แล้วเพิ่ม breathPauseMs ก่อนเปลี่ยน Slide
```

Implement ใน `tutor-reducer.ts` case `TTS_ENDED` → `WAIT_SLIDE_DURATION` (คำนวณ
`remaining = max(0, videoDurationMs - elapsedMs)` แล้วรอ `remaining + breathPauseMs`)
มี Unit Test ครอบคลุมทั้ง 3 กรณี (TTS นานกว่า/สั้นกว่า/เท่ากับวิดีโอ) ใน
`tutor-reducer.test.ts`

## 4. Push-to-Talk (ไม่มี Voice Activity Detection)

- กดค้างเพื่อพูด (`PushToTalkButton` รองรับ Mouse/Touch/Keyboard) — เริ่มกด
  หยุดเสียง AI ทันที (`dispatch()` ใน `use-tutor-session.ts` เรียก `clearPending()`
  ก่อนทุกครั้ง ซึ่ง pause `<audio>` ที่กำลังเล่นอยู่)
- อัดเสียงเฉพาะช่วงกดค้างผ่าน `MediaRecorder` (`startRecording`/`stopRecordingAndSend`)
- ปล่อยปุ่ม → หยุดอัด → ถ้าอัดสั้นกว่า `MIN_RECORDING_MS` (300ms ฝั่ง Client) หรือสั้นกว่า
  `MIN_VOICE_DURATION_MS` (ฝั่ง Server) → `NO_SPEECH` ทันที ไม่เรียก API
- ถอดเสียงไม่ได้/Error ระหว่างอัปโหลด → `QUESTION_FAILED` → พฤติกรรมเดียวกับ
  `NO_SPEECH` คือกลับไปสอนต่อโดยไม่พูดข้อความเพิ่มเติม (ดูตารางใน STATE_MACHINE.md)

## 5. Grounded Q&A (`VoiceQuestionProvider`)

- Backend โหลด Speaker Notes ของ **ทุก Slide** ในบทเรียนมาเป็นฐานความรู้ก่อนส่งให้
  Provider (`/api/voice-question` เรียก `getLessonContent` แล้ว map เป็น
  `lessonSlides` ทั้งหมด ไม่ใช่แค่ Slide ปัจจุบัน)
- `MockVoiceQuestionProvider`: จับคู่ Keyword ธรรมดากับ Notes จริง (ไม่ใช้ความรู้ทั่วไป)
- `GeminiVoiceQuestionProvider` (Prepared): Prompt บังคับให้ตอบจาก Notes เท่านั้น คืน
  JSON ตาม Schema คงที่ (`answerStatus` ต้องเป็นหนึ่งใน 5 ค่าที่กำหนด มิฉะนั้นถือว่า
  `transcription_failed`)
- ผลลัพธ์ `not_found`/`out_of_scope` ยังถือเป็น "ตอบสำเร็จ" ในเชิง UX (AI พูดข้อความ
  มาตรฐานแล้วกลับไปสอนต่อ) ต่างจาก `no_speech`/`transcription_failed` ที่กลับไปสอนต่อ
  แบบเงียบ ๆ ทันที

## 6. Session Rules

| กติกา | Implementation |
|---|---|
| CS สร้างลิงก์เฉพาะ Session | `generatePublicToken()` (`crypto.randomUUID()`) ใน `MockSessionRepository.create` / `token uuid default gen_random_uuid()` ใน Supabase |
| ชื่อครู/โรงเรียนไม่บังคับ | `CreateSessionInput.teacherName`/`schoolName` เป็น optional ทั้ง Type และ Zod schema |
| หมดอายุเริ่มต้น 24 ชม. ปรับได้ | `getDefaultSessionExpiryHours()` อ่านจาก `DEFAULT_SESSION_EXPIRY_HOURS`, ฟอร์มสร้าง Session ให้แก้ `expiresAt` ได้ตรง ๆ |
| ยังไม่มี Auth สำหรับ CS | ไม่มี Middleware/Session Cookie ใด ๆ ใน `/admin/**` |
| รองรับหนึ่งอุปกรณ์ต่อ Session | ไม่มี Presence/Lock Mechanism หลายอุปกรณ์ (ตั้งใจไม่ทำตามข้อห้ามใน Prompt) |
| ลิงก์หมดอายุระหว่างอยู่ในห้อง เรียนต่อได้ | `isSessionJoinable()` เช็คแค่ตอนเข้า `/join`; หน้า `/room` เช็คแค่ `status !== "ENDED"` ไม่เช็ค `expiresAt` ซ้ำ |
| จบ Session ต้องแสดงหน้าขอบคุณ | `router.replace("/session-ended")` เมื่อ `runtime.state === "completed"` |
| ไม่มีคะแนนประเมิน | `SessionSummary` ไม่มีฟิลด์คะแนน/ระดับใด ๆ |
