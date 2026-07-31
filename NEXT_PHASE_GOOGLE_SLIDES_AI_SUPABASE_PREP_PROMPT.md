# PROMPT: Audit Existing Mock Demo and Prepare Google Slides, AI Voice, Gemini, and Supabase Integration

> โปรเจกต์: `sb_supportroom`
>
> ใช้ Prompt นี้กับ Claude Code ภายในโฟลเดอร์โปรเจกต์เดิม หลังจาก Mock Demo รอบแรกสร้างเสร็จแล้ว

---

## บทบาทของคุณ

คุณคือ **Senior Full-stack Engineer, Solution Architect, Integration Engineer และ Technical Documentation Owner**

หน้าที่ของคุณคือเข้าตรวจสอบโปรเจกต์ Next.js ที่มีอยู่จริง ปรับปรุง Mock Demo ให้ตรงกับ Product Logic ล่าสุด และเตรียมโครงสร้าง Frontend, Backend, API, Provider, Repository, Environment และเอกสารส่งมอบงานให้พร้อมเชื่อมต่อบริการจริงในเฟสถัดไป

ต้องทำงานบนโปรเจกต์เดิมเท่านั้น ห้ามสร้างโปรเจกต์ใหม่โดยไม่จำเป็น

---

# 1. เป้าหมายหลักของงานรอบนี้

1. Audit โค้ดและเอกสารของ Mock Demo ปัจจุบันก่อนแก้ไข
2. เปลี่ยนแนวคิดการจัดเก็บเนื้อหาการสอนจาก Mock Lesson Editor เดิม ให้รองรับ **Google Slides เป็นแหล่งเนื้อหาหลัก**
3. ใช้ **Speaker Notes ของแต่ละ Slide เป็นบทพูดของ AI Voice**
4. ใช้ภาพและวิดีโอที่อยู่ใน Google Slides เป็นสื่อที่แสดงในพื้นที่ Shared Screen ของห้องสอน
5. เตรียม **Hugging Face Inference API** สำหรับ Text-to-Speech ภาษาไทย
6. เตรียม **Gemini API** สำหรับรับเสียงจาก Push-to-Talk, ถอดเสียงภาษาไทย และตอบคำถามโดยอิง Speaker Notes ของทุก Slide
7. เตรียม **Supabase** สำหรับ Lesson Config และประวัติการใช้งาน แต่ยังให้ Mock Mode เป็นค่าเริ่มต้น
8. ใช้ **Next.js App Router เป็นทั้ง Frontend และ Backend** ผ่าน Route Handlers
9. เตรียมระบบให้เหลืองานเชื่อมต่อจริงให้น้อยที่สุด เช่น ใส่ Key, สร้าง Supabase Project, รัน SQL และสลับ Provider
10. ปรับปรุง `.md` เดิม และสร้างเอกสาร Architecture, Logic, Diagram, API Contract, Setup Guide และ Backend Handoff ให้ทีมอื่นรับงานต่อได้
11. รักษา Mock Demo ให้ยังรันได้ แม้ไม่มี API Key หรือ Supabase
12. ห้ามกล่าวว่า Integration ใดเชื่อมสำเร็จ หากยังไม่มี Credentials หรือยังไม่ได้ทดสอบกับบริการจริง

---

# 2. คำสั่งสำคัญก่อนเริ่มแก้โค้ด

ก่อนเขียนหรือแก้โค้ด ต้องทำตามลำดับนี้:

1. อ่านไฟล์เอกสารที่มีอยู่ทั้งหมด โดยเฉพาะ:
   - `AI_Live_Tutor_Demo_Spec.md`
   - `BUILD_AI_LIVE_TUTOR_PROMPT.md`
   - `README.md`
   - `CLAUDE.md` ถ้ามี
   - `AGENTS.md` ถ้ามี
   - เอกสารทั้งหมดใน `docs/`
2. อ่าน `package.json`, config files และโครงสร้างใน `src/` หรือ `app/`
3. ตรวจสอบ Route, Component, Mock Data, Repository, Provider, State Management และ Tutor Engine ที่สร้างไว้จริง
4. รันโปรเจกต์และตรวจ Flow ปัจจุบันก่อนแก้ไข หาก Environment เอื้ออำนวย
5. ห้ามสมมติว่าโค้ดตรงกับเอกสาร ต้องยึดโค้ดจริงและ Product Logic ล่าสุดร่วมกัน
6. หากของเดิมมี Interface หรือ Service ที่ทำหน้าที่ใกล้เคียงอยู่แล้ว ให้ปรับของเดิม ห้ามสร้างของซ้ำโดยไม่จำเป็น
7. วางแผนการแก้ไขสั้น ๆ ก่อนลงมือ และรักษา Scope ให้เหมาะกับ Demo

---

# 3. Product Logic ล่าสุดที่ถือเป็น Source of Truth

## 3.1 คอนเซปของระบบ

`sb_supportroom` คือห้องสอนการใช้งานระบบที่ให้ประสบการณ์ใกล้เคียง Video Call หรือ Meeting กับเจ้าหน้าที่ CS

CS สร้าง Session Link เฉพาะ แล้วส่งให้คุณครู คุณครูเข้าผ่านลิงก์ของ `sb_supportroom` ไม่ได้เข้าผ่านลิงก์ Google Slides โดยตรง

ในห้องสอน:

- พื้นที่หลักขนาดใหญ่เป็น Shared Screen สำหรับแสดง Google Slides
- ด้านข้างมี Tile ของ `School Bright Support` และ Tile ของคุณครู
- เมื่อ AI Voice กำลังพูด ให้กรอบของผู้สอนมี Pulse หรือ Active Speaker Indicator
- คุณครูเปิดหรือปิดไมค์และกล้องได้
- ค่าเริ่มต้นของกล้องคือปิดและใช้ Icon
- กล้องแสดงภาพเท่านั้น AI ห้ามวิเคราะห์ภาพ สีหน้า หรือพฤติกรรม
- มีช่องแชตสำรอง แต่ซ่อนไว้และเปิดด้วยปุ่ม Chat
- Demo รองรับ Desktop และ Mobile แบบ Responsive

## 3.2 Google Slides เป็นแหล่งเนื้อหาการสอน

กำหนดกติกาดังนี้:

- **1 Slide = 1 ช่วงการสอน**
- **Speaker Notes = บทพูดมาตรฐานของ Slide นั้น**
- Speaker Notes มีเฉพาะข้อความบทพูด ห้ามบังคับให้ CS เขียน Syntax หรือ Command เช่น `[WAIT]`, `[CHECKPOINT]`
- ภาพและวิดีโออยู่ใน Slide และแสดงตาม Layout ที่ CS ออกแบบไว้
- วิดีโอใน Slide ต้องไม่มีเสียง หรือระบบต้องเปิดแบบ Mute
- Google Slides ทำหน้าที่เหมือนหน้าจอที่ CS กำลัง Share
- เนื้อหาที่ใช้คือเวอร์ชันล่าสุดของ Google Slides ตอนผู้ใช้เข้าห้อง ไม่ทำ Snapshot หรือ Versioning ใน Phase นี้
- CS ต้องสามารถวาง Google Slides URL จากหน้า Admin ได้
- ระบบต้องเตรียมรองรับทั้ง:
  - Google Slides Source URL ที่มี `presentationId` สำหรับ Google Slides API
  - Published/Embed URL สำหรับแสดงใน Shared Screen
- เนื่องจาก Published URL และ Source Presentation URL อาจใช้ Identifier คนละแบบ ให้ UI, Type และเอกสารรองรับการเก็บทั้งสองค่า หากไม่สามารถ derive ค่าอย่างปลอดภัยได้
- หากมีเพียง Source URL ให้เตรียม Fallback Embed URL สำหรับไฟล์ที่เปิดสิทธิ์ดูได้
- ต้องมี Validation และข้อความอธิบายให้ CS เข้าใจว่าต้องวางลิงก์ชนิดใด

## 3.3 การอ่าน Speaker Notes

ใช้ **Google Service Account** ที่ Backend ของ Next.js เพื่อเรียก Google Slides API

กติกา:

- คุณครูไม่ต้อง Login Google
- Credential ต้องอยู่ฝั่ง Server เท่านั้น
- CS ต้องแชร์ไฟล์ต้นฉบับ Google Slides ให้ Service Account อย่างน้อยสิทธิ์ Viewer
- Backend อ่าน Slide order, Slide object ID และ Speaker Notes
- ห้ามส่ง Google Service Account Credential ไป Browser
- ต้องมี Mock Google Slides Provider สำหรับรันโดยไม่มี Credential

## 3.4 การดำเนินบทเรียน

- เมื่อเข้าห้อง AI ทักทายและถามว่าพร้อมหรือไม่
- หากผู้ใช้ไม่ตอบภายในค่าที่กำหนด เช่น 5 วินาที ให้เริ่มอัตโนมัติ
- ค่ารอทั้งหมดต้องเก็บใน Config ไม่ Hardcode กระจายตาม Component
- หลังเริ่มสอน ให้เล่น Slide ต่อเนื่องตามลำดับ
- ไม่มี Checkpoint ถามว่า “เข้าใจไหม” ระหว่างแต่ละ Slide
- ไม่มี Progress Bar เพื่อคงบรรยากาศเหมือน Meeting
- เมื่อ Speaker Notes ของ Slide จบ ให้เว้นจังหวะคล้ายการหายใจ เช่น 800–1,200 ms แล้วไป Slide ถัดไป
- Breath Pause ต้องแก้ได้จาก Config
- เมื่อสอนครบทุก Slide ให้สรุปสั้น ๆ และเปิดโอกาสถามเพิ่มเติม
- หากไม่มีคำถามภายในเวลาที่กำหนด ให้กล่าวลาและจบ Session อัตโนมัติ

## 3.5 Logic ของ Slide ที่มีวิดีโอ

เนื่องจากการแสดง Google Slides ผ่าน iframe ไม่สามารถอ่าน Event ว่าวิดีโอจบได้อย่างน่าเชื่อถือ ให้ใช้ `videoDurationMs` ที่กำหนดใน Lesson Config ต่อ Slide

กติกา:

- AI Voice และสื่อใน Slide เริ่มในช่วงเดียวกัน
- วิดีโอไม่มีเสียง
- ระบบเปลี่ยน Slide เมื่อทั้งสองเงื่อนไขจบแล้ว:
  - เสียง TTS จบ
  - เวลาวิดีโอที่กำหนดจบ
- ใช้เวลารอโดยประมาณ:

```ts
slideDurationMs = Math.max(ttsAudioDurationMs, videoDurationMs ?? 0)
```

จากนั้นเพิ่ม `breathPauseMs` ก่อนเปลี่ยน Slide

- Slide ที่ไม่มีวิดีโอให้ `videoDurationMs` เป็น `0` หรือ `null`
- Admin ต้องสามารถตั้ง `videoDurationMs` ต่อ Slide ได้
- ต้อง Validate ว่าเป็นเลขตั้งแต่ 0 ขึ้นไป

## 3.6 Push-to-Talk แทนการตรวจเสียงอัตโนมัติ

ตัด Voice Activity Detection และการพูดแทรกจากเสียงรบกวนออกจาก Demo

ใช้ **Push-to-Talk**:

- ผู้ใช้กดค้างปุ่มเพื่อถาม
- รองรับ Mouse, Touch และ Keyboard Accessibility
- ตอนเริ่มกด ให้หยุดเสียง AI ทันที
- รับเสียงเฉพาะช่วงที่กดค้างด้วย `MediaRecorder`
- เมื่อปล่อยปุ่ม ให้หยุดบันทึกและส่งเสียงไป Backend
- ถ้าไม่มีเสียง, เสียงสั้นเกินเกณฑ์, ถอดเสียงไม่ได้ หรือเกิด Error ให้กลับไปสอนต่อทันทีโดยไม่พูดข้อความเพิ่มเติม
- ถ้ามีคำถาม ให้ Gemini ถอดเสียงและตอบ จากนั้น Hugging Face อ่านคำตอบออกเสียง
- หลังตอบเสร็จ:
  - หาก Slide ปัจจุบันมีวิดีโอ ให้ Restart Slide และ Speaker Notes จากต้น Slide
  - หากเป็น Slide ภาพนิ่ง ให้ Resume จากตำแหน่งเดิมได้เมื่อ Architecture เดิมรองรับอย่างเสถียร มิฉะนั้นให้ Restart Slide ปัจจุบันเพื่อความสอดคล้อง และอธิบายข้อจำกัดไว้ในเอกสาร
- UI ต้องแสดงสถานะ `กดค้างเพื่อพูด`, `กำลังฟัง`, `กำลังประมวลผล` และ `กำลังตอบ`
- ปุ่ม Push-to-Talk ต้องเด่นพอสำหรับ Mobile

## 3.7 Gemini

Gemini มีหน้าที่:

1. รับเสียงจาก Push-to-Talk
2. ถอดเสียงภาษาไทย
3. วิเคราะห์คำถาม
4. ตอบคำถามโดยใช้ Speaker Notes ของ **ทุก Slide ในบทเรียนนั้น** เป็นฐานความรู้
5. ตอบสั้น กระชับ และกลับเข้าสู่การสอน

Grounding Rules:

- ห้ามตอบจากความรู้ทั่วไปเมื่อไม่มีข้อมูลใน Speaker Notes
- ห้ามเดาคำตอบ
- หากคำถามเกี่ยวกับระบบแต่ไม่มีข้อมูลรองรับ ให้ตอบข้อความมาตรฐานและบันทึกให้ CS ดูภายหลัง
- หากถามนอกเรื่อง ให้ปฏิเสธสั้น ๆ และกลับเข้าบทเรียน
- หากถามหัวข้อที่อยู่ใน Slide ถัดไป ให้ตอบสั้น ๆ ได้ทันที แล้วกลับมาสอน Slide เดิม
- คำตอบต้องมี Result Type เช่น:
  - `answered`
  - `not_found`
  - `out_of_scope`
  - `no_speech`
  - `transcription_failed`

ต้องมี Mock Gemini Provider เป็นค่าเริ่มต้น

## 3.8 Hugging Face Text-to-Speech

ใช้ **Hugging Face Inference API** สำหรับ Text-to-Speech ภาษาไทย

กติกา:

- ใช้แปลง Speaker Notes เป็นเสียง
- ใช้แปลงคำตอบของ Gemini เป็นเสียง
- Model ต้องกำหนดผ่าน Environment Variable ห้าม Hardcode ให้เปลี่ยนยาก
- Token ต้องอยู่ฝั่ง Server เท่านั้น
- Provider ต้องคืน Audio Blob/Buffer พร้อม MIME Type ที่ชัดเจน
- Frontend ต้องควบคุม Play, Stop, Current Time และ Ended Event ได้
- ต้องมี Mock TTS Provider เป็นค่าเริ่มต้น
- หากยังไม่ได้กำหนด Model หรือ Token ให้ Error แบบอ่านเข้าใจง่าย และ Mock Mode ต้องไม่พัง

## 3.9 Supabase

Supabase ไม่ใช่ที่เก็บเนื้อหาบทเรียนหลัก เนื้อหาจริงอยู่ใน Google Slides

Supabase เตรียมไว้เก็บ:

### Lesson Config

- ชื่อบทเรียน
- Slug
- Google Slides Source URL
- `presentationId`
- Published/Embed URL
- Breath Pause
- Intro Wait
- Final Question Wait
- Slide Config ต่อ Slide
- `slideObjectId`
- Slide index/order
- `videoDurationMs`
- Active status

### Session History

- Session token/link
- Lesson ที่ใช้
- ชื่อคุณครูและโรงเรียนแบบ Optional
- Created time
- Expiry time
- Started time
- Ended time
- Status
- จบครบทุก Slide หรือไม่
- Slide ล่าสุด
- คำถามที่ถาม
- Transcript
- คำตอบ
- Answer status
- จุดที่ตอบไม่ได้
- Session summary

Phase นี้:

- Mock Data Provider เป็นค่าเริ่มต้น
- เตรียม Supabase Client, Repository, SQL Migration, Types และ Environment ให้พร้อม
- ห้ามบังคับให้ Mock Demo ต้องมี Supabase Key
- หากมี Key ในอนาคต ให้สลับ Provider ได้จาก Environment โดยไม่แก้ UI
- ห้ามใช้ Prisma หรือ ORM
- ใช้ Supabase JavaScript Client โดยตรง

## 3.10 Session Rules ที่ต้องรักษา

- CS สร้างลิงก์เฉพาะ Session
- ชื่อคุณครูและโรงเรียนไม่บังคับ
- ค่าเริ่มต้นลิงก์หมดอายุ 1 วันนับจากเวลาสร้าง และ CS ปรับได้
- ยังไม่ต้องมี Authentication สำหรับ CS
- Demo รองรับหนึ่งอุปกรณ์ต่อ Session ก่อน
- หากลิงก์หมดอายุระหว่างอยู่ในห้อง ให้เรียนต่อจนจบได้
- เมื่อผู้ใช้อุปกรณ์สุดท้ายกดออก ให้จบ Session ทันที
- หลังจบ แสดงหน้าขอบคุณ
- CS ดูรายการ Session แบบเรียบง่ายและเปิดดูสรุปได้
- ไม่มีคะแนนประเมิน
- สรุปเฉพาะสอนครบหรือไม่, คำถาม, จุดที่ตอบไม่ได้ และ Slide ที่จบ

---

# 4. Architecture ที่ต้องเตรียม

ใช้ Next.js App Router เป็น Frontend และ Backend ใน Repository เดียว

Architecture เป้าหมาย:

```text
Browser UI
  ↓
Application Services / Hooks
  ↓
Internal API Client
  ↓
Next.js Route Handlers
  ↓
Provider / Repository Interfaces
  ├─ Mock Implementations
  ├─ Google Slides Provider
  ├─ Gemini Provider
  ├─ Hugging Face TTS Provider
  └─ Supabase Repositories
```

กฎ:

- Component ห้ามเรียก Google, Gemini, Hugging Face หรือ Supabase โดยตรง
- Secret ห้ามอยู่ใน Client Bundle
- External Integration ต้องเรียกผ่าน Server Route Handler
- Business Logic ของ Tutor Engine ต้องไม่ผูกกับ SDK ของผู้ให้บริการ
- ห้ามกระจาย `if provider === ...` ไปตาม Component
- ใช้ Factory หรือ Dependency Composition จุดเดียว
- หาก Project เดิมมี Architecture ที่เหมาะสมกว่า ให้ปรับโดยรักษาหลัก Separation of Concerns

---

# 5. Provider และ Repository Interfaces

ตรวจของเดิมก่อน หากไม่มีให้สร้าง Interface ที่ใกล้เคียงดังนี้ โดยปรับชื่อให้เข้ากับ Convention ของโปรเจกต์ได้

## 5.1 Slides Provider

```ts
export interface SlidesContentProvider {
  resolvePresentation(input: ResolvePresentationInput): Promise<ResolvedPresentation>;
  getLessonContent(input: GetLessonContentInput): Promise<SlidesLessonContent>;
}
```

ต้องคืนข้อมูลอย่างน้อย:

```ts
interface SlidesLessonContent {
  presentationId: string;
  title: string;
  embedUrl: string;
  slides: Array<{
    slideObjectId: string;
    index: number;
    speakerNotes: string;
    slideUrl?: string;
  }>;
  syncedAt: string;
}
```

Implementations:

- `MockSlidesContentProvider`
- `GoogleSlidesContentProvider`

## 5.2 TTS Provider

```ts
export interface TextToSpeechProvider {
  synthesize(input: TtsInput): Promise<TtsResult>;
}
```

Implementations:

- `MockTextToSpeechProvider`
- `HuggingFaceTextToSpeechProvider`

## 5.3 Voice Question Provider

แยกความรับผิดชอบภายในได้ แต่ API ภายนอกต้องรองรับ Flow เสียงหนึ่งครั้งอย่างชัดเจน

```ts
export interface VoiceQuestionProvider {
  transcribeAndAnswer(input: VoiceQuestionInput): Promise<VoiceQuestionResult>;
}
```

หรือแยกเป็น:

- `SpeechToTextProvider`
- `GroundedAnswerProvider`

Implementations:

- Mock Provider
- Gemini Provider

ต้องไม่สร้าง Interface ซ้ำหากของเดิมรองรับอยู่แล้ว

## 5.4 Repositories

```ts
export interface LessonConfigRepository {
  getBySlug(slug: string): Promise<LessonConfig | null>;
  save(config: LessonConfig): Promise<LessonConfig>;
}

export interface SessionRepository {
  create(input: CreateSessionInput): Promise<TrainingSession>;
  getByToken(token: string): Promise<TrainingSession | null>;
  list(): Promise<TrainingSession[]>;
  markStarted(sessionId: string): Promise<void>;
  end(sessionId: string, result: EndSessionInput): Promise<void>;
}

export interface SessionQuestionRepository {
  add(input: CreateSessionQuestionInput): Promise<SessionQuestion>;
  listBySession(sessionId: string): Promise<SessionQuestion[]>;
}
```

Implementations:

- Mock/Local Storage หรือ In-memory ตามของเดิม
- Supabase Repositories

---

# 6. Next.js Route Handlers ที่ต้องเตรียม

ชื่อ Route ปรับได้ตาม Convention แต่ต้องมี API Contract ชัดเจน

เสนอ:

```text
src/app/api/
├─ health/route.ts
├─ slides/
│  ├─ resolve/route.ts
│  └─ content/route.ts
├─ tts/route.ts
├─ voice-question/route.ts
├─ lessons/
│  └─ [slug]/route.ts
├─ sessions/
│  ├─ route.ts
│  └─ [token]/route.ts
└─ session-questions/route.ts
```

## 6.1 Slides Resolve

รับ Source URL และ Optional Embed URL

หน้าที่:

- Validate URL
- Extract `presentationId` เมื่อทำได้
- Normalize Embed URL
- ส่งข้อความแจ้งเมื่อ Published URL ไม่สามารถใช้แทน Source URL สำหรับ API ได้
- ใน Real Mode เรียก Google Slides API เพื่อยืนยันสิทธิ์และอ่าน Metadata

## 6.2 Slides Content

- รับ Lesson หรือ Presentation ID
- อ่าน Slide order และ Speaker Notes
- Normalize เป็น Domain Type
- ห้ามส่ง Credential กลับ Client
- รองรับ Cache แบบเรียบง่าย แต่ต้องมีวิธี Refresh เพื่อใช้เนื้อหาเวอร์ชันล่าสุด

## 6.3 TTS

- รับข้อความและ Voice Options ที่อนุญาต
- Validate ขนาดข้อความ
- เรียก TTS Provider
- Return Audio Response พร้อม `Content-Type`
- ห้าม Log Speaker Notes เต็มโดยไม่จำเป็น

## 6.4 Voice Question

รับ `multipart/form-data` อย่างน้อย:

- Audio file/blob
- Lesson identifier
- Current slide object ID
- Session ID

Backend:

1. Validate MIME type และขนาดไฟล์
2. โหลด Speaker Notes ทุก Slide ของ Lesson
3. ส่ง Audio และ Grounding Context ให้ Gemini
4. คืน Transcript, Answer, Answer Status และอ้างอิง Slide ที่เกี่ยวข้องถ้ามี
5. บันทึก Session Question ผ่าน Repository เมื่อเปิด Data Persistence
6. ห้ามส่ง Secret ไป Client

## 6.5 Session API

เตรียม CRUD ขั้นต่ำสำหรับ:

- Create Session
- List Sessions
- Get Session by Token
- Mark Started
- End Session พร้อม Summary

Mock Mode ต้องยังทำงานได้

---

# 7. Environment Variables

สร้างหรือปรับ `.env.example` โดยแยก Public และ Server-only ให้ชัดเจน

ตัวอย่างชื่อที่แนะนำ:

```env
# Application
NEXT_PUBLIC_APP_URL=http://localhost:3000

# Provider switches — default mock
DATA_PROVIDER=mock
SLIDES_PROVIDER=mock
TTS_PROVIDER=mock
VOICE_QUESTION_PROVIDER=mock

# Google Slides — server only
GOOGLE_SERVICE_ACCOUNT_PROJECT_ID=
GOOGLE_SERVICE_ACCOUNT_EMAIL=
GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY=

# Hugging Face — server only
HUGGINGFACE_API_TOKEN=
HUGGINGFACE_TTS_MODEL=
HUGGINGFACE_TTS_ENDPOINT=

# Gemini — server only
GEMINI_API_KEY=
GEMINI_MODEL=

# Supabase
NEXT_PUBLIC_SUPABASE_URL=
NEXT_PUBLIC_SUPABASE_ANON_KEY=
SUPABASE_SERVICE_ROLE_KEY=

# Upload / request limits
MAX_VOICE_UPLOAD_MB=5
MIN_VOICE_DURATION_MS=300

# Tutor defaults
DEFAULT_INTRO_WAIT_MS=5000
DEFAULT_BREATH_PAUSE_MS=1000
DEFAULT_FINAL_QUESTION_WAIT_MS=5000
DEFAULT_SESSION_EXPIRY_HOURS=24
```

ข้อกำหนด:

- Mock ต้องเป็นค่าเริ่มต้น
- `.env.local` ต้องถูก Ignore
- Service Role, Google Private Key, Hugging Face Token และ Gemini Key ห้ามใช้ Prefix `NEXT_PUBLIC_`
- ทำ Environment Validation ฝั่ง Server
- แก้ปัญหา Private Key newline เช่นแปลง `\\n` เป็น `\n`
- Error ต้องบอกว่าตัวแปรใดขาด แต่ห้ามแสดงค่าของ Secret
- สร้างเอกสารว่า Key แต่ละตัวได้มาจากที่ใดและใส่ที่ไหน

---

# 8. Supabase Schema และ Migration

ห้ามใช้ Prisma

สร้าง SQL Migration จริงใน Repository เช่น:

```text
supabase/migrations/0001_initial_schema.sql
```

ตารางขั้นต่ำ:

## `lessons`

- `id`
- `slug` unique
- `title`
- `description` nullable
- `slides_source_url`
- `presentation_id`
- `slides_embed_url`
- `intro_wait_ms`
- `breath_pause_ms`
- `final_question_wait_ms`
- `is_active`
- `created_at`
- `updated_at`

## `lesson_slide_configs`

- `id`
- `lesson_id` FK
- `slide_object_id`
- `slide_index`
- `video_duration_ms`
- `created_at`
- `updated_at`

Constraints:

- Unique `(lesson_id, slide_object_id)`
- Index `(lesson_id, slide_index)`

## `training_sessions`

- `id`
- `token` unique
- `lesson_id` FK
- `teacher_name` nullable
- `school_name` nullable
- `status`
- `expires_at`
- `started_at` nullable
- `ended_at` nullable
- `completed_all_slides`
- `last_slide_object_id` nullable
- `created_at`
- `updated_at`

## `session_questions`

- `id`
- `session_id` FK
- `slide_object_id` nullable
- `transcript` nullable
- `answer` nullable
- `answer_status`
- `created_at`

## `session_results`

- `id`
- `session_id` unique FK
- `completed_all_slides`
- `last_slide_object_id` nullable
- `questions_summary` nullable
- `unanswered_points` nullable
- `created_at`

ต้องมี:

- Updated timestamp strategy
- Useful indexes
- Check constraints สำหรับ duration และ status เมื่อเหมาะสม
- Seed SQL หรือ Script สำหรับบทเรียน Login แบบตัวอย่าง
- Type Mapping ระหว่าง SQL กับ TypeScript
- RLS Plan และคำอธิบายว่า Phase Demo ใช้ Server Route Handler เป็นผู้เข้าถึงข้อมูล
- ห้ามเปิด Service Role ให้ Client

หากยังไม่สามารถทดสอบกับ Supabase จริง ให้ระบุว่า Migration ยังไม่ถูก Apply

---

# 9. Tutor Engine และ State Machine

ตรวจของเดิมและปรับให้รองรับ State ขั้นต่ำ:

```text
idle
preparing
ready
intro-speaking
slide-loading
slide-speaking
waiting-slide-duration
push-to-talk-recording
processing-question
answer-speaking
restarting-slide
paused
final-question-window
completed
error
```

Event ขั้นต่ำ:

```text
JOIN
START
INTRO_TIMEOUT
SLIDE_READY
TTS_STARTED
TTS_ENDED
SLIDE_DURATION_ENDED
PUSH_TO_TALK_START
PUSH_TO_TALK_END
NO_SPEECH
QUESTION_ANSWERED
QUESTION_FAILED
RESTART_CURRENT_SLIDE
NEXT_SLIDE
PAUSE
RESUME
END_SESSION
FAIL
```

กฎ:

- State Transition ต้องอยู่ใน Tutor Engine หรือ Reducer ไม่กระจายใน UI
- Media/TTS Timer ต้อง Cleanup เมื่อเปลี่ยน State หรือ Unmount
- ป้องกัน Double Submit และการปล่อยปุ่ม Push-to-Talk ซ้ำ
- Abort Request ได้เมื่อออกจากห้อง
- Restart iframe/slide โดยเปลี่ยน `key` หรือวิธีที่โปรเจกต์รองรับอย่างปลอดภัย
- Session Summary ต้องสร้างจาก Runtime State และ Persist ผ่าน Repository

---

# 10. Admin UI ที่ต้องปรับ

เปลี่ยนหน้า Lesson Editor เดิมให้เป็น **Google Slides Lesson Configuration** แบบเรียบง่ายสำหรับ CS ที่ไม่ใช่ Developer

ฟิลด์และ Action ขั้นต่ำ:

- Lesson title
- Google Slides Source URL
- Published/Embed URL
- ปุ่ม Validate/Sync Slides
- แสดงสถานะ Sync ล่าสุด
- แสดงรายการ Slide ตามลำดับ
- แสดง Slide index และ Speaker Notes preview แบบอ่านอย่างเดียว
- ช่อง `videoDurationMs` ต่อ Slide
- Global intro wait
- Global breath pause
- Global final question wait
- Save Config

กฎ UX:

- CS ไม่ต้องเขียน Command ใน Speaker Notes
- อธิบายสั้น ๆ ว่า `1 Slide = 1 ช่วงการสอน`
- แจ้งว่าวิดีโอต้องไม่มีเสียง
- แจ้งว่าหากแก้ Google Slides ระบบจะใช้เนื้อหาล่าสุดเมื่อ Sync หรือเข้าห้อง ตาม Implementation ที่เลือก
- แสดง Error ที่เข้าใจได้ เช่น ไม่มีสิทธิ์, URL ไม่ถูกต้อง, ไม่มี Speaker Notes
- Mock Mode ต้องมี Deck จำลองให้ทดลอง Flow ได้

ห้ามทำ CMS หรือ Slide Editor ใหม่ในระบบ

---

# 11. Shared Screen และ Google Slides Display

- พื้นที่ Shared Screen ต้องเป็นส่วนหลักของ Tutor Room
- ใช้ iframe/embed ที่ Sandbox และ Permission เหมาะสม
- ไม่แสดง Google Slides Toolbar เกินจำเป็นหาก Embed Mode รองรับ
- Auto advance ของ Google Slides ต้องปิด เพราะ Tutor Engine เป็นผู้ควบคุมเวลา
- เปลี่ยน Slide ตาม Slide index/object ID ตามวิธีที่ Implement ได้จริง
- หากไม่สามารถควบคุม Slide ภายใน iframe เนื่องจาก Cross-origin ให้ใช้การสร้าง Per-slide URL หรือ Reload iframe อย่างมีระบบ
- บันทึกข้อจำกัดทางเทคนิคไว้ในเอกสาร ห้ามซ่อนปัญหา
- ห้ามดึงภาพหรือวิดีโอออกมาจัด Layout ใหม่ เพราะ Product ตัดสินใจให้แสดงตามหน้าตา Google Slides โดยตรง

---

# 12. Documentation ที่ต้องสร้างหรือปรับปรุง

เอกสารทั้งหมดต้องตรงกับโค้ดจริงและใช้ภาษาไทยเป็นหลัก โดยใช้ชื่อ Technical Term ภาษาอังกฤษได้

## 12.1 เอกสารเดิม

ปรับ:

- `AI_Live_Tutor_Demo_Spec.md`
- `README.md`
- `CLAUDE.md` ถ้ามี ให้ Merge อย่างระมัดระวัง
- `AGENTS.md` ถ้ามี ห้ามลบกฎเดิมที่สำคัญ

เปลี่ยนชื่อโปรเจกต์ในเอกสารให้เป็น `sb_supportroom`

## 12.2 เอกสารใหม่ขั้นต่ำ

สร้าง:

```text
docs/
├─ SYSTEM_ARCHITECTURE.md
├─ SYSTEM_LOGIC.md
├─ USE_CASE_DIAGRAM.md
├─ ER_DIAGRAM.md
├─ SEQUENCE_DIAGRAMS.md
├─ STATE_MACHINE.md
├─ DATA_FLOW_DIAGRAM.md
├─ API_CONTRACT.md
├─ BACKEND_HANDOFF.md
├─ API_INTEGRATION_GUIDE.md
├─ ENVIRONMENT_SETUP.md
├─ GOOGLE_SLIDES_SETUP.md
├─ GOOGLE_SLIDES_CONTENT_GUIDE.md
├─ HUGGINGFACE_TTS_SETUP.md
├─ GEMINI_INTEGRATION.md
├─ SUPABASE_SETUP_AND_SCHEMA.md
├─ INTEGRATION_ROADMAP.md
├─ TESTING_GUIDE.md
├─ DEVELOPMENT_CHECKLIST.md
└─ adr/
   ├─ 0001-google-slides-as-content-source.md
   ├─ 0002-supabase-for-config-and-history.md
   ├─ 0003-push-to-talk-instead-of-vad.md
   ├─ 0004-nextjs-fullstack-for-demo.md
   └─ 0005-mock-first-provider-architecture.md
```

หากชื่อเอกสารของเดิมซ้ำ ให้ปรับปรุงของเดิม ไม่สร้างไฟล์ซ้ำซ้อน

## 12.3 `CLAUDE.md`

หากยังไม่มี ให้สร้าง Root `CLAUDE.md` สำหรับ Claude Code และทีมพัฒนา โดยมี:

- Project overview
- Current phase
- Commands
- Architecture rules
- Provider switches
- Folder map
- Integration entry points
- Environment variable map
- Files to read before modifying AI/Backend/Data code
- What is Mock / Prepared / Connected
- Rules ห้ามเรียก External API จาก Client
- Rules ห้ามเปิด Secret
- Definition of Done
- Document update checklist

## 12.4 Diagram

ใช้ Mermaid ใน Markdown และต้อง Render ได้

### Use Case Diagram

Actors อย่างน้อย:

- CS/Admin
- Teacher/User
- Google Slides
- Gemini
- Hugging Face
- Supabase

Use Cases อย่างน้อย:

- Configure lesson
- Sync slide metadata and notes
- Set video duration
- Create session link
- Join room
- Play teaching slide
- Ask through Push-to-Talk
- Answer grounded question
- End session
- View session summary

### ER Diagram

ต้องตรงกับ Supabase Schema จริง

### Sequence Diagrams

สร้างอย่างน้อย 6 Flow:

1. CS บันทึก Google Slides Lesson Config
2. Sync Google Slides และ Speaker Notes
3. CS สร้าง Session Link
4. คุณครูเข้าห้องและเริ่มสอน
5. Push-to-Talk → Gemini → Hugging Face → Resume/Restart Slide
6. จบ Session และบันทึก Summary

### State Machine

ต้องตรงกับ Tutor Engine State/Event จริง

### Data Flow Diagram

แสดง:

- Browser
- Next.js UI
- Route Handlers
- Google Slides API
- Gemini API
- Hugging Face API
- Supabase
- Mock Providers

## 12.5 Backend Handoff

`docs/BACKEND_HANDOFF.md` ต้องเป็นเอกสารที่ทีมรับงานต่อได้จริง โดยระบุ:

- Integration แต่ละตัวอยู่ที่ Folder/File ใด
- Interface ที่ต้อง Implement
- Route Handler ที่เกี่ยวข้อง
- Environment Variables
- Request/Response Contract
- Database Table ที่เกี่ยวข้อง
- TODO ที่เหลือ
- วิธีเปลี่ยนจาก Mock เป็น Real Provider
- วิธีทดสอบ
- Acceptance Criteria
- Known risks และข้อจำกัด

ให้มีตารางสถานะ:

| Integration | Status | Entry Point | Credentials Needed | Remaining Work |
|---|---|---|---|---|
| Google Slides | Prepared/Mock | ระบุไฟล์จริง | Service Account | ระบุจากโค้ดจริง |
| Hugging Face TTS | Prepared/Mock | ระบุไฟล์จริง | Token + Model | ระบุจากโค้ดจริง |
| Gemini | Prepared/Mock | ระบุไฟล์จริง | API Key + Model | ระบุจากโค้ดจริง |
| Supabase | Prepared/Mock | ระบุไฟล์จริง | URL + Keys + Applied Migration | ระบุจากโค้ดจริง |

ห้ามใส่สถานะ `Connected` หากยังไม่ได้ทดสอบจริง

---

# 13. Setup Guide ที่ต้องละเอียดพอทำตามได้

## 13.1 Google Slides Service Account

`docs/GOOGLE_SLIDES_SETUP.md` ต้องอธิบายทีละขั้น:

1. สร้าง Google Cloud Project
2. เปิด Google Slides API
3. สร้าง Service Account
4. สร้าง Key หรือเตรียม Credential ตามวิธีที่ปลอดภัย
5. นำ Email ของ Service Account ไป Share ไฟล์ Google Slides เป็น Viewer
6. หา Source URL และ `presentationId`
7. Publish หรือเตรียม Embed URL สำหรับแสดงในห้อง
8. ใส่ Environment Variables
9. สลับ `SLIDES_PROVIDER=google`
10. ทดสอบ Route Health/Resolve/Content
11. Troubleshooting เช่น 403, 404, Notes ว่าง, Private key newline

ต้องอธิบายข้อจำกัดว่า Published URL อาจไม่สามารถใช้แทน Source Presentation ID สำหรับ API ได้เสมอ จึงอาจต้องเก็บสอง URL

## 13.2 Google Slides Content Guide สำหรับ CS

อธิบาย:

- 1 Slide = 1 ช่วงสอน
- Speaker Notes เขียนเฉพาะบทพูด
- ไม่ใส่ Command
- วิดีโอต้องไม่มีเสียง
- วิธีตั้งชื่อและจัด Slide
- วิธีประมาณ `videoDurationMs`
- แนวทางออกแบบ Slide ให้เหมาะกับ Desktop และ Mobile
- หลีกเลี่ยงข้อความเล็กเกินไป
- ทดสอบ Published/Embed ก่อนส่งลิงก์ Session

## 13.3 Hugging Face

อธิบาย:

- สร้าง Token
- เลือก Model TTS ภาษาไทย
- ตั้ง Model ผ่าน Env
- วิธีทดสอบ `/api/tts`
- MIME type ที่คาดหวัง
- Rate limit/Cold start handling ในเชิง Architecture
- Fallback เป็น Mock

อย่าฟันธง Model หากยังไม่ได้ทดลองจริง ให้ใส่ Checklist การประเมินเสียงภาษาไทย

## 13.4 Gemini

อธิบาย:

- ใส่ API Key และ Model
- Flow ส่ง Audio
- Grounding ด้วย Speaker Notes ทุก Slide
- Prompt/Response Schema
- Safety Rule และ Fallback
- วิธีทดสอบ Transcript และ Answer Status
- ห้าม Log Audio หรือ Transcript เกินจำเป็น

## 13.5 Supabase

อธิบาย:

- สร้าง Project
- คัดลอก URL/Anon/Service Role
- รัน Migration
- ตั้งค่า Env
- ตรวจ Table
- สลับ `DATA_PROVIDER=supabase`
- ทดสอบ Create/List/Get/End Session
- Security และ RLS Plan

---

# 14. API Contract และ Validation

ใช้ Schema Validation เช่น Zod หากเหมาะกับ Stack ปัจจุบัน

`docs/API_CONTRACT.md` ต้องระบุทุก Route:

- Method
- Path
- Purpose
- Authentication ปัจจุบัน
- Request shape
- Response shape
- Error shape
- Status codes
- Example
- Provider ที่ Route เรียก

Error shape กลาง เช่น:

```ts
interface ApiErrorResponse {
  error: {
    code: string;
    message: string;
    details?: unknown;
    requestId?: string;
  };
}
```

ห้ามคืน Stack Trace หรือ Secret ให้ Client

เพิ่ม Request ID หรือ Correlation ID แบบเรียบง่าย หากไม่ซับซ้อนเกินไป

---

# 15. Security และ Privacy

- Secret ทั้งหมด Server-only
- ห้ามเก็บ Service Role ใน Client
- จำกัด MIME type และขนาด Audio Upload
- Validate URL ของ Google Slides
- ป้องกัน SSRF โดยอนุญาตเฉพาะ Domain Google ที่จำเป็นในการ Parse URL
- Sanitize Log
- ไม่เก็บไฟล์เสียงถาวรใน Demo เว้นแต่มี Requirement ใหม่
- เก็บ Transcript และคำตอบเฉพาะที่จำเป็นต่อ Summary
- ระบุว่า Google Slides ที่เปิด Public/Published ไม่เหมาะกับเนื้อหาลับ
- Session Token ต้องสุ่มยากและ Unique
- ห้ามใช้ข้อมูลจริงที่อ่อนไหวใน Seed Data

---

# 16. Tests ที่ควรเพิ่ม

ตรวจ Test Setup เดิมก่อน

หากไม่มี Test Framework และเพิ่มได้โดยไม่กระทบโปรเจกต์ ให้ใช้ Vitest แบบ Minimal

Unit Tests ขั้นต่ำ:

1. Parse Google Slides Source URL
2. Parse/validate Published Embed URL
3. Provider Factory เลือก Mock เป็น Default
4. Missing Environment Variable ให้ Error ถูกต้องใน Real Mode
5. Slide duration ใช้ `Math.max(ttsDuration, videoDuration)`
6. Tutor State transition ของ Push-to-Talk
7. No-speech/failed transcription กลับไปสอนโดยไม่พูดเพิ่ม
8. Video slide restart หลังตอบคำถาม
9. Session expiry default 24 ชั่วโมง
10. Session summary แยก `completed_all_slides` ถูกต้อง

หาก Route Handler ทดสอบได้ง่าย ให้เพิ่ม Integration Test แบบไม่เรียก External API จริง โดย Mock Provider

---

# 17. Implementation Status

ปรับ `AI_Live_Tutor_Demo_Spec.md` และเอกสารที่เกี่ยวข้องให้มีสถานะ:

- `Completed`
- `Partially Completed`
- `Mock Only`
- `Prepared — Credentials Required`
- `Planned`
- `Not Included`

อย่างน้อยสำหรับ:

- CS Dashboard
- Google Slides Config UI
- Slides Sync
- Shared Screen Embed
- Speaker Notes Parsing
- Tutor Engine
- Push-to-Talk
- Gemini Voice Question
- Hugging Face TTS
- Supabase
- Session History
- Session Summary
- Responsive UI
- Camera/Microphone
- Authentication
- Multi-device Sync

สถานะต้องมาจากโค้ดจริง ห้ามเดา

---

# 18. สิ่งที่ห้ามทำ

- ห้ามสร้างโปรเจกต์ใหม่แทนของเดิม
- ห้ามลบ Mock Mode
- ห้ามทำให้โปรเจกต์ต้องมี Key จึงรันได้
- ห้ามเชื่อม External API จาก React Component โดยตรง
- ห้ามเปิด Secret ใน Browser
- ห้ามใช้ Prisma หรือ ORM
- ห้ามสร้าง Slide Editor หรือ Media CMS ใหม่
- ห้ามย้ายภาพและวิดีโอออกจาก Google Slides มาจัดหน้าใหม่
- ห้ามใช้ Voice Activity Detection ใน Demo นี้
- ห้ามทำ Avatar Lip Sync
- ห้ามเพิ่ม Authentication, Role Management หรือ Multi-device Sync ในรอบนี้
- ห้ามสร้างเอกสารหรือ Diagram ที่ไม่ตรงกับ Implementation
- ห้ามปิด TypeScript Strict หรือใช้ `any` จำนวนมากเพื่อผ่าน Build
- ห้ามปิด ESLint Rule แบบกว้างโดยไม่จำเป็น
- ห้ามใส่ API Key จริงลง Git
- ห้ามกล่าวว่า Real Integration ทำงานแล้วหากยังไม่ได้ทดสอบด้วย Credential จริง

---

# 19. Quality Checks

หลังแก้ไข ให้รันตามที่โปรเจกต์รองรับ:

```bash
npm install
npm run lint
npm run typecheck
npm run test
npm run build
```

หาก Script ใดไม่มี ให้พิจารณาเพิ่ม `typecheck` และ `test` เมื่อเหมาะสม

ตรวจ Manual Flow อย่างน้อย:

1. Mock Mode รันโดยไม่มี `.env.local`
2. CS เปิดหน้า Lesson Config
3. Mock Slides Sync แสดง Slide และ Notes ได้
4. สร้าง Session Link
5. เปิด Pre-join
6. เข้าห้อง
7. Slide และ Mock TTS เดินต่อเนื่อง
8. Push-to-Talk Mock Flow ทำงาน
9. No-speech กลับไปสอนเงียบ ๆ
10. Video Slide Restart หลังตอบคำถาม
11. จบ Session
12. CS ดู Summary
13. Mobile layout ไม่พัง

หาก Real Provider ยังไม่มี Key ให้ทดสอบเฉพาะ Environment Validation และ Mocked API Contract

---

# 20. ผลลัพธ์ที่ต้องรายงานเมื่อทำเสร็จ

สรุปเป็นภาษาไทย โดยระบุ:

1. สภาพโปรเจกต์ก่อนแก้ไข
2. สิ่งที่ตรวจพบว่าไม่ตรงกับ Product Logic ล่าสุด
3. Code และ UI ที่แก้
4. Provider/Repository/Route Handler ที่สร้างหรือปรับ
5. Supabase Migration ที่เตรียม
6. Environment Variables ที่เพิ่ม
7. เอกสารและ Diagram ที่สร้าง
8. ไฟล์ `CLAUDE.md` หรือ Handoff ที่ปรับ
9. Integration ใดเป็น Mock, Prepared หรือ Connected
10. Credentials ที่ทีมต้องใส่ภายหลัง
11. ขั้นตอนเปิด Google Slides Integration
12. ขั้นตอนเปิด Hugging Face TTS
13. ขั้นตอนเปิด Gemini
14. ขั้นตอนเปิด Supabase
15. Known limitations
16. ผล `lint`
17. ผล `typecheck`
18. ผล `test`
19. ผล `build`
20. งานถัดไปที่ทีม Backend ควรทำตามลำดับ

ให้แนบรายการไฟล์ที่เปลี่ยนแบบจัดกลุ่ม และแจ้ง Error ที่ยังแก้ไม่ได้อย่างตรงไปตรงมา

---

# 21. Definition of Done สำหรับงานรอบนี้

งานรอบนี้ถือว่าเสร็จเมื่อ:

- Mock Demo ยังเปิดและเดิน Flow ได้
- Project ใช้ชื่อ `sb_supportroom` ในเอกสารใหม่
- Google Slides ถูกกำหนดเป็น Content Source หลักใน Architecture และ UI
- 1 Slide = 1 ช่วงการสอน
- Speaker Notes = บทพูด
- Shared Screen แสดง Slides ตาม Design เดิม
- Push-to-Talk Logic ถูกเตรียมและไม่มี VAD
- Google Slides, Gemini, Hugging Face และ Supabase มี Interface, Real Provider Skeleton/Implementation, Route Handler, Env Validation และ Setup Guide
- Supabase มี SQL Migration พร้อมใช้ แต่ยังไม่บังคับเชื่อม
- Mock เป็น Default Provider
- Secret อยู่ฝั่ง Server
- `CLAUDE.md` และ `BACKEND_HANDOFF.md` ทำให้ทีมใหม่รู้ว่าจะเชื่อมอะไรที่ไหน
- Use Case, ER, Sequence, State Machine และ Data Flow Diagram ตรงกับโค้ด
- เอกสารบอกสถานะจริง ไม่อ้างเกิน Implementation
- Lint, Typecheck และ Build ผ่าน หรือรายงาน Blocker ที่ตรวจสอบได้

---

## เริ่มทำงาน

เริ่มจาก Audit Repository และรายงานแผนแก้ไขแบบสั้น ๆ จากนั้นดำเนินการแก้โค้ด เอกสาร และ Diagram ให้ครบตาม Prompt นี้โดยไม่ต้องหยุดถามรายละเอียดปลีกย่อย หากพบการตัดสินใจที่กระทบ Architecture หรือทำให้ Product Logic ขัดกัน ให้หยุดและถามเฉพาะประเด็นนั้นเท่านั้น
