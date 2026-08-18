# PROJECT_CONTEXT — SupportRoom AI

> เอกสารนี้อธิบายระบบ **ตามที่โค้ดเป็นจริง** อัปเดต audit ล่าสุด 13 สิงหาคม 2026
> (branch `Dev-gun/Gun`) เริ่มอ่านสถานะส่งมอบที่ [`HANDOFF_MASTER.md`](./HANDOFF_MASTER.md)
> เมื่อเอกสารกับโค้ดขัดกัน ให้ถือโค้ดเป็น source of truth และแก้เอกสารตาม
>
> เอกสารพี่น้อง: [`SOLUTION_ARCHITECTURE.md`](./SOLUTION_ARCHITECTURE.md) (solution space / ecosystem)
> และ [`TECH_DECISIONS.md`](./TECH_DECISIONS.md) (decision log)

---

## 1. Project Overview

SupportRoom AI คือ **ห้องเรียนสาธิตแบบโต้ตอบด้วยเสียง** ที่ทีม Customer Success ของ School Bright
ใช้สอนคุณครูให้ใช้งานระบบ โดยไม่ต้องมีพนักงานจริงนั่งประกบทุก session

รูปแบบการใช้งาน: CS สร้าง "บทเรียน" (ผูกกับ Google Slides deck หรือไฟล์ PDF) → สร้างลิงก์ session
→ ส่งลิงก์ให้คุณครู → คุณครูเปิดลิงก์แล้วเข้าห้องที่หน้าตาเหมือน video call
→ AI tutor บรรยายทีละสไลด์ด้วยเสียงภาษาไทย → คุณครูกดปุ่ม Push-to-Talk ถามได้ตลอด
→ AI ตอบโดยอ้างอิงเฉพาะเนื้อหาบทเรียน แล้วกลับไปสอนต่อจากจุดเดิม
→ จบ session ระบบสรุปคำถาม/สิ่งที่ตอบไม่ได้ให้ CS ตามต่อ

จุดต่างที่สำคัญ: นี่ไม่ใช่ chatbot ที่ตอบอะไรก็ได้ — เป็น **grounded tutor** ที่ถูกบังคับให้ตอบ
จากเนื้อหาบทเรียนเท่านั้น และรายงาน `not_found` เมื่อไม่มีข้อมูล

## 2. Business Domain

### ผู้ใช้และบทบาท

| บทบาท | ใช้หน้าไหน | ทำอะไร |
|---|---|---|
| CS / Admin | `/admin/*` | ตั้งค่าบทเรียน, อัปโหลดเอกสาร knowledge base, สร้าง/ติดตาม session, แชตช่วยคุณครูสด |
| คุณครู (Teacher) | `/join/[token]`, `/room/[token]` | เข้าห้องผ่านลิงก์, ฟังบทเรียน, ถามด้วยเสียง, แชต |

Back office มี JWT login และ 3 role (`owner`/`admin`/`cs`) ส่วนผู้เรียนไม่มีบัญชี ใช้ link token
ร่วมกับ browser `learnerKey` ดูข้อ 13

### Entity หลักและกฎธุรกิจ

- **LessonConfig** — เนื้อหาหนึ่งชุด ระบุด้วย `slug` ที่ไม่ซ้ำภายในบริษัท เลือกแหล่งเนื้อหาได้ 2 แบบ
  (`google_slides` หรือ `pdf`) เก็บเฉพาะ *metadata + timing* ห้ามเก็บสำเนาเนื้อหาสอน
  (สถาปัตยกรรมกฎข้อ 8) — เนื้อหาจริงถูก resolve สดทุกครั้ง
- **TrainingLink** — ลิงก์เชิญที่ CS สร้าง มี `token` สาธารณะกับ `expiresAt`
  1 ลิงก์เปิดได้หลายคน สถานะ ACTIVE/EXPIRED คำนวณจาก `expiresAt` ไม่ได้เก็บ
- **LearningSession** — การเรียนของคน *หนึ่ง* คน ผูกกับลิงก์ผ่าน `trainingLinkId`
  แยกคนด้วย `learnerKey` (key ที่ browser เก็บ) — `SessionQuestion.SessionId` และ
  `ChatMessage.SessionId` ชี้มาที่ตารางนี้ ไม่ใช่ที่ลิงก์
- ~~**TrainingSession**~~ — เดิมคือ "1 ลิงก์ = 1 การเรียน" แยกออกเป็นสองตารางข้างบนแล้ว (TD-013)
  ปัจจุบัน `LearningSession` มี `IN_PROGRESS → ENDED`; ส่วน `TrainingLink` มี ACTIVE/EXPIRED แบบคำนวณสด
- **SessionQuestion** — บันทึกคำถาม Push-to-Talk หนึ่งครั้ง พร้อม `answerStatus`
  (`answered` / `not_found` / `out_of_scope` / `no_speech` / `transcription_failed`)
  จงใจไม่ใช้ boolean เพื่อให้ CS รู้ว่า *ทำไม* ถึงตอบไม่ได้
- ~~**SessionSummary**~~ — ลบทิ้งแล้ว (TD-013) สรุปคำนวณสดจาก `LearningSession` + `SessionQuestion`
- **ChatMessage** — ช่องข้อความสำรอง แยกจาก voice Q&A เก็บลง DB เพื่อให้คนเข้ากลางคันเห็นย้อนหลัง
- **DocumentResource** — ไฟล์ที่ CS อัปโหลด (`LessonId` เป็น null ได้ = เอกสาร global)

กฎที่ฝังอยู่ในโค้ดและควรรู้:
- คำถาม readiness ("พร้อมหรือยัง") **ไม่ถูกบันทึก** เป็น SessionQuestion — ไม่ใช่คำถามที่ CS ต้องรีวิว
- เอกสารที่ถูกใช้เป็นเนื้อหาสอนหลัก (PDF ของบทเรียน) **ลบไม่ได้** จนกว่าจะเปลี่ยนแหล่งเนื้อหา
- Re-index RAG เป็น best-effort เสมอ — index พังไม่ควรทำให้ CS บันทึกบทเรียนไม่ได้

## 3. Technology Stack

### Frontend (`frontend/`)

| ด้าน | ที่ใช้จริง |
|---|---|
| Framework | Next.js 15.5.22 (App Router, client components เป็นหลัก) |
| ภาษา / UI | React 19, TypeScript 5.7, Tailwind CSS 3.4 |
| Routing | Next.js file-based (`src/app/`) |
| State | `useReducer`-style pure reducer เขียนเอง (`src/tutor/`) + `useRef` ไม่มี Redux/Zustand |
| API client | `fetch` wrapper จุดเดียวที่ `src/lib/api-client.ts` |
| Realtime | `@microsoft/signalr` 10.x |
| Media | MediaRecorder + getUserMedia ผ่าน hooks |
| Test | Vitest 4.1 (31 tests ผ่านทั้งหมด) |
| Lint / Types | ESLint 9 (`eslint-config-next`), `tsc --noEmit` |

> ⚠️ `package.json` ยังมี dependency ตกค้างจากยุค Next.js fullstack ที่ **ไม่มีโค้ดเรียกใช้แล้ว**:
> `googleapis`, `msedge-tts`, `zod`, `client-only`, `bufferutil`, `utf-8-validate`
> (`server-only` เหลือใช้เฉพาะใน test shim) — ดูข้อ 19

### Backend (`backend/`)

| ด้าน | ที่ใช้จริง |
|---|---|
| Framework / runtime | ASP.NET Core บน .NET 10 (`net10.0`) |
| API | Controller-based REST + SignalR Hub |
| Business layer | Service classes ใน `SupportRoom.Application` |
| Data access | EF Core 10.0.10 + Npgsql 10.0.3, Repository + UnitOfWork เขียนเอง |
| Mapping | Mapster 10 |
| Logging | Serilog (console + rolling file, มี `CorrelationId` ทุกบรรทัด) |
| Background jobs | `IBackgroundTaskQueue` (Channel ใน memory) + `QueuedHostedService` |
| Cache | `IMemoryCache` (PDF bytes / parsed content / rendered pages) |
| API docs | OpenAPI + Swagger UI (Development เท่านั้น) |
| Auth | JWT bearer + fallback authorization policy; 3-role back-office RBAC |
| Test | xUnit 2.9 (3 projects) |

### Infrastructure

| ด้าน | สถานะ |
|---|---|
| Database | PostgreSQL (EF Core migrations) |
| Object storage | local disk หรือ Huawei OBS (ผ่าน `AWSSDK.S3` S3-compatible) |
| Vector store | Pinecone serverless (data-plane REST ตรง ไม่ใช้ SDK) |
| Deployment | **ยังไม่มี** — ไม่มี Dockerfile, ไม่มี IaC, ไม่มี deploy script |
| CI/CD | **ไม่มี** — `.github/workflows/` มีอยู่แต่ว่างเปล่า |
| Observability | Serilog ลงไฟล์เท่านั้น ไม่มี metrics/tracing/error tracking |

## 4. System Architecture

```text
┌─────────────────────────────────────────────────────────┐
│ Browser (Next.js 15)                                    │
│  /admin/*  — CS console                                 │
│  /join/[token], /room/[token] — Teacher                 │
│                                                         │
│  src/tutor/         pure reducer (ไม่มี browser API)     │
│  src/hooks/         effect runner: audio, mic, SignalR   │
│  src/lib/api-client.ts   จุดเดียวที่คุยกับ backend        │
└───────────┬──────────────────────────┬──────────────────┘
            │ REST (camelCase JSON)    │ SignalR /hubs/session
            ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│ ASP.NET Core (.NET 10)                                  │
│  Controllers (บาง) ──► Application Services ──► UnitOfWork│
│  SessionHub                    │                        │
│                                ├─► IRealtimeNotifier ───┘
│                                ├─► IBackgroundTaskQueue │
│                                └─► Provider interfaces  │
└────────────────────────────────┬────────────────────────┘
                                 ▼
    PostgreSQL │ Google Slides API │ Edge TTS │ Gemini
    Pinecone   │ OpenAI-compatible │ local disk / Huawei OBS
```

หลักการที่บังคับอยู่จริงในโค้ด:

1. Browser **ห้าม** เรียก database หรือ AI provider ตรง — ทุกอย่างผ่าน .NET API
2. Credentials อยู่ฝั่ง backend เท่านั้น (`backend/src/SupportRoom.Api/.env`)
3. `frontend/src/tutor/` ต้องเป็น pure reducer — browser API อยู่ใน hooks เท่านั้น
4. Controller บาง / orchestration อยู่ใน Application
5. Provider เปลี่ยนได้ด้วย env var โดยไม่แตะโค้ดเรียกใช้ (factory pattern)

## 5. Directory Structure

```text
frontend/
  src/app/                    Next.js pages (admin, join, room, link-expired, session-ended)
  src/components/admin/       CS console UI
  src/components/meeting/     ห้องสอน: SlidesEmbed, AiTile, TeacherTile, ControlBar, ChatDrawer
  src/components/ui/          primitives (Button, Card, Modal, ...)
  src/config/                 ข้อความตอบกลับ/filler และค่า timing default
  src/hooks/                  use-tutor-session (effect runner), use-session-chat, use-local-media
  src/lib/api-client.ts       REST client จุดเดียว
  src/tutor/                  pure state machine + types + intents + tests
  src/types/                  wire/domain types (ต้องตรงกับ ViewModel ฝั่ง .NET)
  src/utils/                  format, session-status, google-slides-url, storage

backend/src/
  SupportRoom.Api/            Program.cs, Controllers, Hubs, QueuedHostedService, DI config
  SupportRoom.Application/    Services (interface + impl ในไฟล์เดียว), DTO, ViewModel, Exceptions
  SupportRoom.Domain/         Entities, Enums (string constants), Configuration/env readers
  SupportRoom.Infrastructure/ CORS, global error handling, JSON options
  SupportRoom.Providers.Data/           EF Core DbContext, Repository, UnitOfWork, Migrations
  SupportRoom.Providers.Slides/         Google Slides + PdfSlidesRenderer (PDFtoImage/PdfPig)
  SupportRoom.Providers.Tts/            Edge TTS
  SupportRoom.Providers.VoiceQuestion/  Gemini full-context + RAG (Gemini/OpenAI answer step)
  SupportRoom.Providers.Knowledge/      embeddings (Gemini/OpenAI) + Pinecone index
  SupportRoom.Providers.DocumentParsing/ PDF/PPTX/DOCX/XLSX text extraction
  SupportRoom.Providers.Storage/        local disk / Huawei OBS
backend/tests/                Application.Tests, Providers.Tests, Api.IntegrationTests (ยังว่าง)
```

**ข้อสังเกต convention ที่ไม่ปกติแต่จงใจ:** ไฟล์ service ชื่อ `IXxxService.cs` แต่บรรจุ
*ทั้ง interface และ implementation* ไว้ด้วยกัน — ทำตามแบบเดิมทั้ง codebase อย่าแยกไฟล์ใหม่
โดยไม่ได้ตกลงกันก่อน

## 6. Main Modules

| Module | ความรับผิดชอบ | ไฟล์เริ่มอ่าน |
|---|---|---|
| Tutor state machine | ลำดับการสอนทั้งหมด (15 states) เป็น pure function | `frontend/src/tutor/tutor-reducer.ts` |
| Effect runner | แปลง effect เป็น audio/mic/timer/API จริง | `frontend/src/hooks/use-tutor-session.ts` |
| Lesson content | resolve Google Slides หรือ PDF ให้เป็น slide list เดียวกัน | `ILessonConfigService.cs` |
| Voice Q&A | transcribe → retrieve → answer → persist → broadcast | `IVoiceQuestionService.cs`, `RagVoiceQuestionProvider.cs` |
| Knowledge indexing | chunk → embed (bounded parallel) → upsert Pinecone | `IKnowledgeIndexingService.cs` |
| Document pipeline | upload → storage → [respond] → background parse/index | `IDocumentResourceService.cs` |
| Realtime | SignalR group ต่อ LearningSession id | `SessionHub.cs`, `SignalRRealtimeNotifier.cs` |
| Admin ops | reset demo data, re-index ทั้งระบบ | `IAdminService.cs` |

## 7. Main User Flows

### 7.1 CS ตั้งค่าบทเรียน

```
/admin/lessons/[slug] → saveLesson()
  → POST /api/lessons → LessonConfigService.SaveAsync()
     ├─ validate contentSourceType
     ├─ re-resolve presentationId จาก URL (ล้มเหลว = log warning, ไม่ block การ save)
     ├─ upsert LessonConfig + Commit
     └─ best-effort: GetLessonContentAsync() → KnowledgeIndexingService.IndexLessonAsync()
```

### 7.2 คุณครูเข้าห้องและฟังบทเรียน

```
/room/[token]
  → GET /api/training-links/{token}  (หมดอายุ → /link-expired)
  → POST /api/learning-sessions/{token}/join  (ENDED → /session-ended/{token})
  → useTutorSession: dispatch JOIN
     → effect LOAD_LESSON  → GET /api/lessons/by-link/{token} (learner-safe lesson + embedUrl + slides)
     → LESSON_LOADED       → effect SPEAK(intro)  → POST /api/tts → play <audio>
     → TTS_ENDED           → state "ready" + timer introWaitMs
     → START / INTRO_TIMEOUT → LOAD_SLIDE(0) → POST /api/tts (speakerNotes)
     → TTS_ENDED           → WAIT_REMAINING (videoDurationMs - elapsed + breathPause)
     → SLIDE_DURATION_ENDED → สไลด์ถัดไป … จนหมด → final-question-window → closing → PERSIST_END
```

พร้อมกันนั้น `prefetchFillers()` ทยอยสังเคราะห์เสียง "รอสักครู่นะคะ" ล่วงหน้าระหว่าง intro

### 7.3 คำถามด้วยเสียง (flow ที่ซับซ้อนที่สุด)

```
กดค้างปุ่มพูด → PUSH_TO_TALK_START  → START_RECORDING (จำ interruptedFrom ไว้)
ปล่อยปุ่ม      → PUSH_TO_TALK_END    → STOP_RECORDING_AND_SEND
                 ├─ playProcessingFiller() เริ่มเล่นเสียงคั่นเป็นขั้น ๆ ทันที
                 └─ POST /api/voice-question (multipart: audio, lessonSlug, sessionId, expecting)
                     → VoiceQuestionService.AskAsync()
                        ├─ ILessonConfigService.GetTeachingContentBySlugAsync()  ← รองรับทั้ง Slides และ PDF
                        └─ RagVoiceQuestionProvider.TranscribeAndAnswerAsync()
                           (1) Gemini: ถอดเสียงอย่างเดียว
                           (2) embed transcript → Pinecone query 2 namespace พร้อมกัน
                               (lessonSlug + "kb-global") → merge top-K → กรองด้วย RAG_MIN_SCORE
                           (3) Gemini หรือ OpenAI-compatible: ตอบจาก context ที่ retrieve ได้
                        ├─ readiness → return เลย (ไม่บันทึก)
                        ├─ บันทึก SessionQuestion
                        └─ IRealtimeNotifier.NotifyNewQuestionAsync() → SignalR "ReceiveNewQuestion"
                 → QUESTION_ANSWERED → SPEAK(answer, withFoundLead) + แสดงสไลด์ที่อ้างอิง
                 → TTS_ENDED → RESUME_AFTER_ANSWER → กลับไปสไลด์เดิมพร้อมประโยคเชื่อม
```

**สาม fallback ที่ออกแบบไว้แล้ว** และควรรักษาไว้:
- retrieval ล้มเหลว/ยังไม่เคย index → ส่ง deck ทั้งชุดเป็น context (พฤติกรรมเหมือน provider แบบเก่า)
- index แล้วแต่ไม่มี chunk ผ่าน threshold → ส่งข้อความ "ไม่พบข้อมูลอ้างอิง" เพื่อบังคับ `not_found`
- TTS ล้มเหลว → ข้ามเสียงแล้วเดินหน้า state machine ต่อ ไม่พังทั้ง session

### 7.4 อัปโหลดเอกสาร knowledge base

```
/admin/documents → POST /api/documents (multipart)
  → DocumentResourceService.UploadAsync()
     ├─ storage.UploadAsync()  ── ไฟล์ลง local disk / OBS
     ├─ insert DocumentResource(status=pending) + Commit
     ├─ DocumentParserFactory.Create() ← เช็ค content-type ทันที (ไฟล์ไม่รองรับ = 400 เลย)
     ├─ enqueue งานหนัก แล้ว return response
     └─ [background] QueuedHostedService → extract → embed → Pinecone upsert
                     → update status = indexed/failed + chunk count
```

### 7.5 จบ session

```
END_SESSION หรือ FINAL_QUESTION_TIMEOUT → PERSIST_END
  → PATCH /api/learning-sessions/{token}/end {learnerKey, completedAllSlides, ...}
     → LearningSessionService.End() → status=ENDED, endedAt
       (ไม่มี snapshot — สรุปคำนวณสดตอนอ่านที่ GetSummary)
     → Commit ทีเดียว (session + summary อยู่ transaction เดียวกัน)
```

## 8. Frontend Architecture

**แกนหลักคือการแยก pure logic ออกจาก side effect** — เป็นจุดแข็งที่สุดของ codebase นี้

- `tutor-reducer.ts` รับ `(runtime, event, ctx)` แล้วคืน `{ runtime, effect }` ไม่แตะ browser API เลย
  จึงทดสอบได้ครบทุก path (28 unit tests)
- `use-tutor-session.ts` เป็น effect runner ตัวเดียว แปลง `TutorEffect` เป็น audio/timer/recorder/API
- ความ "สุ่ม" ทั้งหมด (เลือกประโยค filler, ประโยคเชื่อม) อยู่ใน hook ไม่ใช่ reducer — reducer จึง deterministic
- state เก็บใน `useRef` + `forceRender()` แทน `useState` เพื่อให้ closure ใน timer/callback อ่านค่าล่าสุดได้เสมอ

Routing: App Router ทั้งหมด, `"use client"` เกือบทุกหน้า, ไม่มี Route Handler เหลืออยู่
Error/Loading: จัดการเองในแต่ละหน้า (`LoadingBlock`, error state ใน runtime) ไม่ได้ใช้
`loading.tsx`/`error.tsx` ของ Next.js
Styling: Tailwind + custom theme tokens (`room-bg`, `room-panel`, `room-text`, `room-muted`)
Forms: controlled component ธรรมดา ไม่มี form library และ**ไม่มี schema validation ฝั่ง client**

## 9. Backend Architecture

Request path จริง:

```
HTTP → [CorrelationId middleware] → [SerilogRequestLogging] → [UseExceptionHandler]
     → [CORS] → [Authorization (no-op)] → Controller
     → Service (ctor injection ผ่าน ServiceBase) → UnitOfWork.GetRepository<T>() → EF Core
                                                 └→ Provider interface → external API
```

- **Controller บางจริง** — แค่ validate input พื้นฐาน แปลง `IFormFile` เป็น byte[] แล้วเรียก service
- **Error handling** สองชั้น: `HttpStatusCodeExceptionHandler` (GeneralException ที่ service โยน)
  แล้วตกไป `GlobalExceptionHandler` (500 ทั่วไป) ทุก error ตอบด้วย envelope เดียวกัน
  `{ error: { code, message, requestId } }` รวมถึง model-validation error ที่ถูก override ไว้ใน `Program.cs`
- **UnitOfWork** เป็นแบบเขียนเอง: repository ต้องลงทะเบียนใน `UnitOfWork.Register` ไม่งั้น throw
  `Commit()` = `SaveChanges()` เท่านั้น ไม่มี explicit transaction/rollback
- **ServiceBase** ให้ `UnitOfWork`, `ServiceProvider`, `Logger` — service เรียก service อื่นผ่าน
  `ServiceProvider.GetRequiredService<T>()` (แก้ปัญหา circular dependency)
- **Provider factory** อ่าน env ครั้งเดียวตอน startup (`ProviderSelectionReader.Read()`)
  ค่าไม่ถูกต้อง = ระบบไม่ start (fail fast โดยตั้งใจ ไม่มี Mock fallback)
- **Logging discipline**: ห้าม log `Transcript`/`Answer` เต็ม — log เฉพาะ outcome/status

## 10. Database Architecture

- PostgreSQL, primary key เป็น `string` ที่ generate เองแบบมี prefix (`lesson_`, `session_`, `doc_`)
- ทุก entity implement `IEntityMaster<string>` มีคอลัมน์ audit (`CreateBy/CreateDate/UpdateBy/…`)
  และ soft-delete flag (`IsDelete`, `DeletedAt`) — **แต่โค้ดยังลบจริง (hard delete)
  และไม่มี global query filter** ฟิลด์เหล่านี้จึงยังไม่มีผลจริง (ดูข้อ 19)
- ความสัมพันธ์เป็น string id ล้วน **ไม่มี FK constraint / navigation property** ใน `OnModelCreating`
  — referential integrity ถูกบังคับด้วย service layer เท่านั้น
- `LessonConfig.SlideConfigs` เป็น EF owned collection แบบ `ToJson()`
- Index ที่มี: `TrainingLink.Token` (unique), `LearningSession.(TrainingLinkId, LearnerKey)`
  (ไม่ unique — กด "เรียนอีกครั้ง" สร้างแถวใหม่ใต้ key เดิม), `LessonConfig.Slug` (unique),
  `SessionQuestion.SessionId`, `ChatMessage.SessionId`, `DocumentResource.LessonId`
- Migration: InitialCreate → AddSessionSummary → AddChatMessage → AddDocumentResource →
  AddLessonPdfSource → AddCompanyId → RenameChatSenderRoles → `SplitLinkAndAddAuth`
  ⚠️ migration ล่าสุดสร้างแล้วแต่ยังไม่ apply/verify กับ PostgreSQL จริง

## 11. ER Diagram

```mermaid
erDiagram
    COMPANY ||--o{ ADMIN_USER : employs
    LESSON_CONFIG ||--o{ TRAINING_LINK : creates
    LESSON_CONFIG ||--o{ DOCUMENT_RESOURCE : "LessonId (nullable)"
    LESSON_CONFIG |o--o| DOCUMENT_RESOURCE : "PdfDocumentResourceId"
    TRAINING_LINK ||--o{ LEARNING_SESSION : opens
    LEARNING_SESSION ||--o{ SESSION_QUESTION : records
    LEARNING_SESSION ||--o{ CHAT_MESSAGE : contains

    LESSON_CONFIG {
        string Id PK
        string Slug UK
        string Title
        string Description "nullable"
        string SlidesSourceUrl
        string PresentationId "nullable"
        string SlidesEmbedUrl "nullable"
        string ContentSourceType "google_slides|pdf"
        string PdfDocumentResourceId "nullable"
        int IntroWaitMs
        int BreathPauseMs
        int FinalQuestionWaitMs
        json SlideConfigs "owned, ToJson()"
        bool IsActive
    }
    COMPANY {
        string Id PK
        string Name
        bool IsActive
    }
    ADMIN_USER {
        string Id PK
        string CompanyId "nullable for owner"
        string Email UK
        string DisplayName
        string Role "owner|admin|cs"
        bool IsActive
        bool MustChangePassword
    }
    TRAINING_LINK {
        string Id PK
        string CompanyId
        string Token UK
        string LessonId
        string LessonSlug
        string RecipientOrgName "nullable"
        datetime ExpiresAt
        int MaxAttendees "nullable, not enforced"
    }
    LEARNING_SESSION {
        string Id PK
        string CompanyId
        string TrainingLinkId
        string LearnerKey
        string RecipientName
        string Status "IN_PROGRESS|ENDED"
        datetime StartedAt
        datetime EndedAt "nullable"
        datetime LastActivityAt
        bool CompletedAllSlides
        string LastSlideObjectId "nullable"
        int LastSlideIndex
    }
    SESSION_QUESTION {
        string Id PK
        string SessionId FK-logical
        string SlideObjectId "nullable"
        string Transcript "nullable"
        string Answer "nullable"
        string AnswerStatus "answered|not_found|out_of_scope|no_speech|transcription_failed"
    }
    CHAT_MESSAGE {
        string Id PK
        string SessionId FK-logical
        string SenderRole
        string SenderName "nullable"
        string Text
    }
    DOCUMENT_RESOURCE {
        string Id PK
        string LessonId "nullable = kb-global"
        string FileName
        string ContentType
        long SizeBytes
        string ObsBucket
        string ObsKey
        string IndexingStatus "pending|indexed|failed"
        int IndexedChunkCount
    }
```

> ความสัมพันธ์ทั้งหมดเป็น *domain-level* — ไม่มี FK จริงในฐานข้อมูล

**ข้อมูลนอก PostgreSQL:** vector อยู่ใน Pinecone แบ่งด้วย namespace =
`{companyId}:{lessonSlug}` หรือ `{companyId}:kb-global`
(ไม่มีอะไรใน DB ที่ชี้ไปยัง vector id — เป็นเหตุผลที่ลบเอกสารแล้ว vector ค้าง ดูข้อ 19)

## 12. API Map

ทุก endpoint ตอบ camelCase JSON; back-office ใช้ JWT ส่วน learner/health opt out เฉพาะรายการที่ระบุ

| Method | Route | Service | ปลายทาง | เรียกจาก |
|---|---|---|---|---|
| GET | `/api/health` | HealthService | — | ops |
| GET | `/api/lessons` | LessonConfigService | DB | `/admin/lessons` |
| GET | `/api/lessons/{slug}` | LessonConfigService | DB + Slides/PDF | admin |
| GET | `/api/lessons/by-link/{token}` | LessonConfigService | Link scope + Slides/PDF | room |
| POST | `/api/lessons` | LessonConfigService | DB + Slides + Pinecone | `/admin/lessons/[slug]` |
| GET | `/api/lessons/pdf-preview` | LessonConfigService | Storage + PdfPig | admin lesson editor |
| GET | `/api/lessons/pdf-pages/{token}/{documentId}/{page}` | LessonConfigService | Link scope + Storage + PDFtoImage | SlidesEmbed |
| POST | `/api/slides/resolve` | SlidesService | Google Slides | admin "Validate/Sync" |
| GET | `/api/slides/content` | SlidesService | Google Slides | admin preview |
| GET | `/api/training-links` | TrainingLinkService | DB | `/admin` |
| POST | `/api/training-links` | TrainingLinkService | DB | CreateTrainingLinkModal |
| GET | `/api/training-links/{token}` | TrainingLinkService | DB | join, room |
| GET | `/api/training-links/by-token/{token}` | TrainingLinkService | DB | `/admin/links/[token]` |
| GET | `/api/training-links/{id}/by-id` | TrainingLinkService | DB | admin |
| GET | `/api/training-links/{id}/learning-sessions` | LearningSessionService | DB | `/admin/links/[token]` |
| POST | `/api/learning-sessions/{token}/join` | LearningSessionService | DB | join, room |
| POST | `/api/learning-sessions/{token}/restart` | LearningSessionService | DB | `/session-ended/[token]` |
| PATCH | `/api/learning-sessions/{token}/progress` | LearningSessionService | DB | room (ทุกครั้งที่เปลี่ยนสไลด์) |
| PATCH | `/api/learning-sessions/{token}/end` | LearningSessionService | DB | room |
| GET | `/api/learning-sessions/{token}/summary` | LearningSessionService | DB | `/session-ended/[token]` |
| GET | `/api/learning-sessions/{id}/summary/by-id` | LearningSessionService | DB | `/admin/learning-sessions/[id]` |
| PATCH | `/api/session-questions/{id}/review` | SessionQuestionService | DB | `/admin/learning-sessions/[id]` |
| GET | `/api/session-questions?token=&learnerKey=` | SessionQuestionService | DB | learner |
| GET | `/api/session-questions/by-learning-session/{id}` | SessionQuestionService | DB | admin |
| GET | `/api/chat-messages?token=&learnerKey=` | ChatMessageService | DB | learner |
| GET | `/api/chat-messages/by-learning-session/{id}` | ChatMessageService | DB | admin |
| POST | `/api/tts` | TtsService | Edge TTS | room (ทุกประโยคที่พูด) |
| **POST** | **`/api/voice-question`** | VoiceQuestionService | Gemini + Pinecone + OpenAI + DB + SignalR | room |
| POST | `/api/documents` | DocumentResourceService | Storage + DB + queue | `/admin/documents` |
| GET | `/api/documents?lessonSlug=` | DocumentResourceService | DB | admin |
| DELETE | `/api/documents/{id}` | DocumentResourceService | Storage + DB | admin |
| POST | `/api/admin/reset` | AdminService | DB | admin (ต้อง `ALLOW_DATA_RESET=true`) |
| POST | `/api/admin/reindex` | AdminService | Slides + Storage + Pinecone | admin |

**SignalR** `/hubs/session` — group = LearningSession id
- Learner → Server: `JoinSession(token, learnerKey)`, `SendChatMessage(token, learnerKey, text)`; ชื่อ derive จาก session
- Agent → Server: `JoinSessionAsAgent(learningSessionId)`, `SendChatMessageAsAgent(learningSessionId, text)`; ชื่อ derive จาก JWT
- Server → Client: `ReceiveChatMessage`, `ReceiveNewQuestion`

Endpoint ที่วิกฤตที่สุดต่อสินค้า: `POST /api/voice-question` (แตะ external service 3 ตัวใน request เดียว)
และ `POST /api/tts` (ถูกเรียกทุกประโยคที่ AI พูด)

## 13. Authentication & Authorization

- Back office ใช้ JWT bearer และ fallback policy: endpoint ใหม่เป็น protected จนกว่าจะ opt-out
- role: owner ทุกบริษัท/ระบบ, admin บริษัทตัวเอง+users, cs บริษัทตัวเอง
- `?company=` เป็น view context ที่ server ตรวจผ่าน `IAuthorizationGuard`; ไม่ใช่ permission
- Learner ไม่มี account: anonymous endpoints resolve company/session จาก token + learnerKey
- SignalR group เป็น `LearningSession.Id`; agent ส่ง JWT, learner ใช้ token/key
- ยังไม่มี rate limiting/abuse controls สำหรับ login/join/voice/TTS/SignalR
- Link expiry บังคับที่ backend แล้วตอน join (`ResolveLinkForJoin`) — เดิมบังคับที่ frontend เท่านั้น
  แต่ผู้ที่เริ่มเรียนก่อนหมดอายุยัง reconnect/เรียนต่อ/ดู recap ได้ตามเจตนา

ดูตัวเลือกและข้อเสนอใน [`SOLUTION_ARCHITECTURE.md`](./SOLUTION_ARCHITECTURE.md) §Authentication

## 14. Existing External Integrations

| Integration | ใช้ทำอะไร | ตั้งค่าที่ | ใช้ในโมดูล | ถ้าล้มเหลว |
|---|---|---|---|---|
| **Google Slides API** | ดึง speaker notes + slide URL (service account, `presentations.readonly`) | `GOOGLE_SERVICE_ACCOUNT_*` | `GoogleSlidesProvider` | save lesson: log warning + เก็บ presentationId เดิม; เปิดห้อง: 502 UPSTREAM_ERROR |
| **Gemini** | ถอดเสียง (ทุก provider) + embeddings (`pinecone`) + ตอบ (`gemini`, `gemini-rag`) | `GEMINI_API_KEY`, `GEMINI_MODEL` | `GeminiRest`, `GeminiEmbeddingProvider` | ถอดเสียงพัง → `transcription_failed`; embed พัง → fallback full-deck |
| **OpenAI-compatible** | ตอบ (`openai-rag`) + embeddings (`pinecone-openai`) รองรับ gateway อื่นผ่าน `OPENAI_BASE_URL` (เช่น GLM บน Huawei ModelArts) | `OPENAI_*` | `OpenAiRest`, `OpenAiEmbeddingProvider` | `transcription_failed` |
| **Pinecone** | vector index (REST ตรง ไม่ใช้ SDK) namespace = lessonSlug / `kb-global` | `PINECONE_API_KEY`, `PINECONE_INDEX_HOST` | `PineconeKnowledgeIndexProvider` | log warning → fallback full-deck context |
| **Edge TTS** | สังเคราะห์เสียงไทย (WebSocket, ไม่ต้องมี key) แบ่ง chunk ≤180 ตัวอักษร, retry 2 ครั้ง, timeout 12s | `EDGE_TTS_VOICE`, `EDGE_TTS_RATE` | `EdgeTtsProvider` | ข้าม chunk ที่พัง; ถ้าพังหมด frontend เดินหน้าต่อแบบไม่มีเสียง |
| **Huawei OBS** | object storage (ผ่าน AWS S3 SDK) | `HUAWEI_OBS_*` | `HuaweiObsDocumentStorageProvider` | upload 500; download → 404 "ไม่พบไฟล์ PDF" |
| **local disk** | object storage สำหรับ dev (มี path-traversal guard) | `LOCAL_STORAGE_PATH` | `LocalDocumentStorageProvider` | เหมือนข้างบน |

> ⚠️ ข้อมูลผู้ใช้ที่ออกนอกระบบ: **เสียงคำถามของคุณครูถูกส่งไปยัง Gemini** และ
> **transcript ถูกส่งไปยัง OpenAI/gateway** เมื่อใช้ `openai-rag` — ต้องระบุใน privacy notice

## 15. Environment Variables

> ค่าจริงอยู่ใน `backend/src/SupportRoom.Api/.env` (gitignored) ดูรายละเอียดครบใน
> `backend/src/SupportRoom.Api/.env.example` — **ห้าม** commit ค่าจริงลง repo

**Frontend** (`frontend/.env.local`)

| ชื่อ | หน้าที่ |
|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | base URL ของ .NET API (ตัวเดียวที่ frontend อ่านจริง) |

**Backend — provider switches (บังคับทุกตัว ไม่มี default):**
`SLIDES_PROVIDER`, `TTS_PROVIDER`, `VOICE_QUESTION_PROVIDER`, `KNOWLEDGE_PROVIDER`,
`DOCUMENT_STORAGE_PROVIDER`

**Backend — credentials:**
`GEMINI_API_KEY`, `GEMINI_MODEL`, `OPENAI_API_KEY`, `OPENAI_BASE_URL`, `OPENAI_MODEL`,
`OPENAI_EMBEDDING_MODEL`, `OPENAI_EMBEDDING_DIMENSIONS`, `OPENAI_DISABLE_REASONING`,
`PINECONE_API_KEY`, `PINECONE_INDEX_HOST`, `GOOGLE_SERVICE_ACCOUNT_PROJECT_ID`,
`GOOGLE_SERVICE_ACCOUNT_EMAIL`, `GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY`,
`HUAWEI_OBS_ENDPOINT|ACCESS_KEY|SECRET_KEY|BUCKET|REGION`, `LOCAL_STORAGE_PATH`

**Backend — tuning / behavior:**
`EDGE_TTS_VOICE`, `EDGE_TTS_RATE`, `RAG_TOP_K`, `RAG_MIN_SCORE`, `ALLOWED_ORIGINS`,
`ALLOW_DATA_RESET`, `DEFAULT_INTRO_WAIT_MS`, `DEFAULT_BREATH_PAUSE_MS`,
`DEFAULT_FINAL_QUESTION_WAIT_MS`, `DEFAULT_SESSION_EXPIRY_HOURS`, `MAX_VOICE_UPLOAD_MB`,
`MIN_VOICE_DURATION_MS`, `MAX_DOCUMENT_UPLOAD_MB`, `ConnectionStrings:Postgres` /
`POSTGRES_CONNECTION_STRING`

## 16. Development Workflow

```powershell
# Backend
cd backend
Copy-Item src/SupportRoom.Api/.env.example src/SupportRoom.Api/.env   # แล้วกรอกค่าจริง
dotnet restore SupportRoom.slnx
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
dotnet run --project src/SupportRoom.Api          # http://localhost:5138

# Frontend
cd frontend
Copy-Item .env.example .env.local
npm install
npm run dev                                        # http://localhost:3000
```

Migration ใหม่:

```powershell
dotnet ef migrations add <Name> --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

**ลำดับเพิ่มฟีเจอร์ตามสถาปัตยกรรมจริง:**

```
Entity (Domain) → Migration (Providers.Data) → Repository + ลงทะเบียนใน UnitOfWork.Register
  → Service interface+impl (Application) → DTO + ViewModel → ลงทะเบียนใน ServiceConfiguration
  → Controller (บาง) → types/domain.ts → api-client.ts → hook → component
```

ถ้าเป็นการต่อ external service ใหม่: สร้าง interface + factory ใน `SupportRoom.Providers.*`
เพิ่มค่าที่รับได้ใน `ProviderSelection.cs` และเพิ่ม env ใน `.env.example`

## 17. Testing Strategy

| ชุด | ครอบคลุม | สถานะที่ verify แล้ว (11 ส.ค. 2026) |
|---|---|---|
| `frontend` vitest | tutor reducer ทุก path, google-slides-url | ✅ 31 tests ผ่าน (13 ส.ค. 2026) |
| `frontend` typecheck / lint | ทั้ง project | ✅ ผ่าน ไม่มี warning |
| `backend` build | ทั้ง solution | ✅ 0 warning / 0 error; pin EF Relational 10.0.10 แล้ว |
| `SupportRoom.Application.Tests` | service logic ด้วย fake providers | ✅ 96 tests ผ่าน (ไม่รวม integration) |
| `SupportRoom.Providers.Tests` | PDF/XLSX extraction, RAG merge | ✅ 21 tests ผ่าน (ไม่รวม integration) |
| `SupportRoom.Api.IntegrationTests` | — | ⚠️ **ว่างเปล่า** (`UnitTest1.cs` ยังเป็น template) |

⚠️ test บาง class ใน Providers/Application ใช้ `RealHttpClientFactory` + `TestEnv` ซึ่ง
**ยิงไปยัง provider จริง** ต้องมี credentials/network จึงจะผ่าน — ตอนตั้ง CI ต้องแยก
unit tests ออกจาก integration tests ด้วย xUnit trait/category

ไม่มี E2E test, ไม่มี visual regression, ไม่มี load test, ไม่มี RAG quality eval

## 18. Coding Conventions

- **Wire format เป็น camelCase เสมอ** — บังคับผ่าน `ApiJsonOptions.Default` ต้องอัปเดต
  ViewModel (.NET) กับ `types/domain.ts` (TS) คู่กันทุกครั้ง
- **Status ใช้ `static class` + `const string` ไม่ใช้ C# enum** — ให้ serialize ตรงกับ TS union type
- **ข้อความที่ผู้ใช้เห็นเป็นภาษาไทย** ทั้ง frontend และ exception message ฝั่ง backend
- **คอมเมนต์อธิบาย "ทำไม" ไม่ใช่ "ทำอะไร"** — codebase นี้มีคอมเมนต์เชิงเหตุผลหนาแน่นผิดปกติ
  และมีคุณค่าสูง (บันทึกเหตุการณ์จริงที่เคยพัง) โปรดรักษาระดับนี้ไว้เมื่อแก้ไข
- ไฟล์ service = interface + implementation ในไฟล์เดียว ชื่อไฟล์ขึ้นต้นด้วย `I`
- Service ห้ามพัง flow หลักเพราะ integration รอง — ใช้ log warning + degrade แทนการ throw
- ห้าม log transcript/answer เต็ม ห้าม log secret
- Frontend: `tutor/` pure เท่านั้น, browser API อยู่ใน `hooks/`, fetch อยู่ใน `api-client.ts` ที่เดียว

## 19. Known Risks / Technical Debt

### Immediate (กระทบตอนนี้)

1. **Edge TTS ไม่เหมาะกับ production** — Microsoft เริ่มบล็อกการใช้ Read-Aloud ที่ไม่เป็นทางการ
   (ธ.ค. 2025) โดยต้องมี anti-abuse token และกรอง IP ของ datacenter ร่องรอยในโค้ด
   (retry, chunking, timeout 12s, comment ถึง 502 จริงที่ 24–46 วินาที) ตรงกับอาการนี้
   → deploy บน cloud มีโอกาสสูงที่เสียงจะเงียบทั้งระบบ **นี่คือความเสี่ยงอันดับ 1**
2. **Auth ทำแล้ว แต่ยังไม่มี rate limiting/abuse controls** — endpoint ค่าใช้จ่ายสูงและ login/join
   ยังต้องมี policy ก่อน production
3. **ไม่มี CI** — `.github/workflows/` ว่างเปล่า ทุกการตรวจต้องรันมือ
4. **ไม่มี deployment artifact** — ไม่มี Dockerfile / compose / IaC

### Near-term (ควรทำก่อนเพิ่มฟีเจอร์ใหญ่)

5. **Background queue ไม่ durable** — restart ระหว่าง indexing ทิ้ง row ค้างที่ `pending` ตลอดกาล
   และไม่มีทางกู้นอกจาก re-upload หรือ `/api/admin/reindex` ทั้งระบบ
6. **ลบเอกสารแล้ว vector ค้างใน Pinecone** — ทำให้ตอบคำถามจากเอกสารที่ถูกลบไปแล้วได้
   (chunk id ใช้รูปแบบ `{documentId}-{chunkId}` อยู่แล้ว จึงลบด้วย ID prefix ได้ทันที
   — Pinecone serverless ไม่รองรับ delete by metadata filter)
7. ~~**Session expiry บังคับเฉพาะ frontend**~~ — แก้แล้ว: บังคับที่ `LearningSessionService` ตอน join
   (จงใจไม่บล็อกการอ่าน/จบของคนที่เรียนค้างอยู่ — ล็อกออกกลางคันดูเหมือนข้อมูลหาย ไม่เหมือนนโยบายหมดอายุ)
8. **API integration tests ว่างเปล่า** — ไม่มีอะไรยืนยันสัญญา endpoint จริง
9. **Soft-delete มีฟิลด์แต่ไม่มีพฤติกรรม** — `IsDelete`/`DeletedAt` ไม่เคยถูกใช้ ลบจริงทุกครั้ง
   คนอ่านโค้ดใหม่จะเข้าใจผิดได้ง่าย
10. **Frontend dependency ตกค้าง** — `googleapis`, `msedge-tts`, `zod`, `client-only`,
    `bufferutil`, `utf-8-validate` ไม่มีโค้ดเรียกใช้ (`googleapis` ก้อนใหญ่มาก)
11. **ไฟล์ตกค้างที่ repo root** — `node_modules/`, `.next/`, `next-env.d.ts`,
    `tsconfig.tsbuildinfo`, `public/` เหลือจากตอนย้ายเป็น monorepo
12. **`PackageReference` แบบ floating** — `3.*`, `0.*`, `5.*` (`AWSSDK.S3`, `PdfPig`,
    `DocumentFormat.OpenXml`, `PDFtoImage`) build วันนี้กับพรุ่งนี้อาจได้คนละเวอร์ชัน

### Future (เมื่อโตขึ้น)

14. SignalR เก็บ group ใน memory — ขยายเป็นหลาย instance ต้องมี backplane
15. `MemoryCache` สำหรับ PDF ก็ต่างคนต่างเก็บเมื่อมีหลาย instance
16. ไม่มี metrics/tracing/error tracking — วินิจฉัยได้แค่จาก log file
17. ไม่มี eval ของคุณภาพคำตอบ RAG — `RAG_MIN_SCORE` ปรับด้วยความรู้สึกจาก log
18. `gemini-flash-latest` เป็น alias ที่เลื่อนตามรุ่นใหม่ — พฤติกรรมเปลี่ยนใต้เท้าได้โดยไม่แก้โค้ด
19. ไม่มี navigation/FK ใน DB — ข้อมูลกำพร้าเกิดได้ถ้า service ทำงานผิดพลาด

## 20. Developer Quick Start

```powershell
git clone <repo> && cd sb_supportroom

# 1) Backend
cd backend
Copy-Item src/SupportRoom.Api/.env.example src/SupportRoom.Api/.env
#    ต้องกรอกอย่างน้อย: POSTGRES connection, GEMINI_API_KEY, PINECONE_*,
#    GOOGLE_SERVICE_ACCOUNT_* (ถ้าใช้ Google Slides)
#    provider switch ทั้ง 5 ตัวต้องมีค่า ไม่งั้น API ไม่ start
dotnet restore SupportRoom.slnx
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
dotnet run --project src/SupportRoom.Api

# 2) Frontend (อีก terminal)
cd frontend
Copy-Item .env.example .env.local     # ตั้ง NEXT_PUBLIC_API_BASE_URL=http://localhost:5138
npm install
npm run dev
```

เปิด <http://localhost:3000/admin> → สร้างบทเรียน → สร้าง session → เปิดลิงก์ที่ได้

**ตรวจก่อนส่งงาน:**

```powershell
cd frontend; npm run lint; npm run typecheck; npm run test; npm run build
cd ../backend; dotnet build SupportRoom.slnx; dotnet test SupportRoom.slnx
```

**อ่านก่อนแก้แต่ละส่วน:**

| งาน | อ่านก่อน |
|---|---|
| Tutor state machine | `frontend/docs/STATE_MACHINE.md`, `frontend/src/tutor/tutor-reducer.test.ts` |
| REST/SignalR contract | `frontend/docs/API_CONTRACT.md`, controllers + `SessionHub.cs` |
| Database schema | `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`, EF migrations |
| Provider / environment | `frontend/docs/ENVIRONMENT_SETUP.md`, `.env.example`, provider factory |
| RAG / document flow | `frontend/docs/SYSTEM_LOGIC.md`, Knowledge + VoiceQuestion services |
| Backend layering | `backend/.claude/skills/dotnet-layered-backend/SKILL.md` |
