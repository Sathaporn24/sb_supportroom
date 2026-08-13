# CLAUDE.md

คำแนะนำสำหรับผู้ช่วยเขียนโค้ดและทีมพัฒนาที่ทำงานใน `sb_supportroom`

## System Purpose

ห้องเรียนสาธิตแบบโต้ตอบด้วยเสียง สำหรับทีม CS ของ School Bright ใช้สอนคุณครูให้ใช้งานระบบ
โดยไม่ต้องมีคนนั่งประกบ: CS ตั้งค่าบทเรียน (Google Slides หรือ PDF) → สร้างลิงก์ session →
คุณครูเข้าห้องที่หน้าตาเหมือน video call → AI บรรยายทีละสไลด์เป็นภาษาไทย → คุณครูกด
Push-to-Talk ถามได้ตลอด → AI ตอบโดยอ้างอิงเฉพาะเนื้อหาบทเรียน แล้วกลับไปสอนต่อจากจุดเดิม

หัวใจคือ **grounded tutor** ไม่ใช่ chatbot ทั่วไป — ต้องรายงาน `not_found` เมื่อไม่มีข้อมูล
ไม่ใช่เดาคำตอบ

## Read First

| ต้องการอะไร | อ่านที่ |
|---|---|
| ภาพรวมระบบตามโค้ดจริง, flow, ER, API map, หนี้เทคนิค | [`docs/PROJECT_CONTEXT.md`](./docs/PROJECT_CONTEXT.md) |
| ทางเลือกเทคโนโลยี, build vs buy, MVP/Production/Scale | [`docs/SOLUTION_ARCHITECTURE.md`](./docs/SOLUTION_ARCHITECTURE.md) |
| การตัดสินใจเชิงเทคนิคและเหตุผล | [`docs/TECH_DECISIONS.md`](./docs/TECH_DECISIONS.md) |
| ลำดับงานเพื่อขึ้น production | [`docs/PRODUCTION_ROADMAP.md`](./docs/PRODUCTION_ROADMAP.md) |
| สเปกฟีเจอร์หลัก: ลิงก์ vs การเรียน, รีวิวคำตอบ | [`docs/CORE_FEATURE_SPEC.md`](./docs/CORE_FEATURE_SPEC.md) |

## Current Architecture

ระบบเป็น monorepo แยกส่วนชัดเจน:

- `frontend/` — Next.js 15 + React 19 + TypeScript + Tailwind
- `backend/` — ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR
- Frontend ติดต่อ backend ผ่าน `frontend/src/lib/api-client.ts` และ
  `frontend/src/hooks/use-session-chat.ts`
- ไม่มี Next.js Route Handlers, Supabase repository หรือ Mock providers ในระบบปัจจุบัน
- เนื้อหาหลักต่อบทเรียนเลือกได้ระหว่าง Google Slides กับ PDF
- RAG ใช้ Pinecone ร่วมกับ Gemini หรือ OpenAI-compatible embeddings/answering

## Architecture Rules

1. Browser ห้ามเรียก database หรือ external AI provider โดยตรง ทุก request ผ่าน .NET API
2. Credentials อยู่ใน `backend/src/SupportRoom.Api/.env` หรือ deployment secrets เท่านั้น
3. Tutor engine ใน `frontend/src/tutor/` ต้องเป็น pure reducer; browser APIs อยู่ใน hooks
4. Business orchestration อยู่ใน `SupportRoom.Application`; controllers ต้องบาง
5. Entities/contracts อยู่ใน `SupportRoom.Domain`; provider implementation อยู่ใน
   `SupportRoom.Providers.*`
6. Database schema เปลี่ยนผ่าน EF Core migration ใหม่ ห้ามแก้ migration ที่ deploy แล้ว
7. Frontend/backend wire contract ใช้ camelCase และต้องอัปเดต TypeScript types กับ ViewModels คู่กัน
8. ห้าม persist สำเนา Google Slides/PDF teaching content ลง LessonConfig; เก็บเฉพาะ metadata

## Folder Map

```text
frontend/src/app/                    Next.js pages
frontend/src/components/             UI, admin, meeting room
frontend/src/hooks/                  media, tutor orchestration, SignalR
frontend/src/lib/api-client.ts       REST client จุดเดียว
frontend/src/tutor/                  pure state machine
frontend/src/types/                  frontend wire/domain types

backend/src/SupportRoom.Api/         controllers, hub, startup, hosted queue
backend/src/SupportRoom.Application/ services, DTOs, ViewModels
backend/src/SupportRoom.Domain/      entities, constants, environment readers
backend/src/SupportRoom.Infrastructure/ CORS, error handling
backend/src/SupportRoom.Providers.Data/ EF Core, repositories, migrations
backend/src/SupportRoom.Providers.Slides/ Google Slides + PDF rendering
backend/src/SupportRoom.Providers.Tts/ Edge TTS
backend/src/SupportRoom.Providers.VoiceQuestion/ Gemini/OpenAI RAG flows
backend/src/SupportRoom.Providers.Knowledge/ embeddings + Pinecone
backend/src/SupportRoom.Providers.DocumentParsing/ PDF/PPTX/DOCX/XLSX extraction
backend/src/SupportRoom.Providers.Storage/ local/Huawei OBS
backend/tests/                        xUnit test projects
```

## Provider Configuration

ทุกหมวดต้องกำหนดค่า ไม่มี Mock fallback:

| หมวด | ค่าที่รองรับ |
|---|---|
| `SLIDES_PROVIDER` | `google` |
| `TTS_PROVIDER` | `edge` |
| `VOICE_QUESTION_PROVIDER` | `gemini`, `gemini-rag`, `openai-rag` |
| `KNOWLEDGE_PROVIDER` | `pinecone`, `pinecone-openai` |
| `DOCUMENT_STORAGE_PROVIDER` | `local`, `huawei-obs` |

ดูค่าทั้งหมดใน `backend/src/SupportRoom.Api/.env.example`

## Commands

```powershell
# Frontend
cd frontend
npm install
npm run lint
npm run typecheck
npm run test
npm run build

# Backend
cd ../backend
dotnet restore SupportRoom.slnx
dotnet build SupportRoom.slnx
dotnet test SupportRoom.slnx
dotnet run --project src/SupportRoom.Api
```

Apply database migration:

```powershell
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

## Files to Read Before Changes

| งาน | อ่านก่อน |
|---|---|
| Tutor state machine | `frontend/docs/STATE_MACHINE.md`, reducer tests |
| REST/SignalR contract | `frontend/docs/API_CONTRACT.md`, controllers/hub |
| Database schema | `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`, EF migrations |
| Provider/environment | `frontend/docs/ENVIRONMENT_SETUP.md`, `.env.example`, provider factory |
| RAG/document flow | `frontend/docs/SYSTEM_LOGIC.md`, Knowledge/VoiceQuestion services |
| Backend layering convention | `backend/.claude/skills/dotnet-layered-backend/SKILL.md` |

## Existing Integrations

| Integration | ใช้ทำอะไร | ถ้าล้มเหลว |
|---|---|---|
| Google Slides API | ดึง speaker notes + slide URL (service account) | save: log warning ไม่ block; เปิดห้อง: 502 |
| Gemini | ถอดเสียงทุก provider + embeddings + ตอบ (`gemini`, `gemini-rag`) | `transcription_failed` / fallback full-deck |
| OpenAI-compatible | ตอบ (`openai-rag`) + embeddings (`pinecone-openai`) รองรับ gateway อื่นผ่าน `OPENAI_BASE_URL` | `transcription_failed` |
| Pinecone | vector index, namespace = lessonSlug หรือ `kb-global` | log warning → fallback full-deck |
| Edge TTS | สังเคราะห์เสียงไทย (WebSocket ไม่ต้องมี key) | ข้าม chunk ที่พัง; frontend เดินหน้าต่อแบบไม่มีเสียง |
| local disk / Huawei OBS | เก็บไฟล์เอกสาร (ผ่าน AWS S3 SDK) | 500 ตอน upload / 404 ตอน download |

⚠️ เสียงคำถามของคุณครูถูกส่งไป Gemini และ transcript อาจถูกส่งไป OpenAI/gateway เมื่อใช้ `openai-rag`

## Feature Development Pattern

```
Entity (Domain) → Migration (Providers.Data) → Repository + ลงทะเบียนใน UnitOfWork.Register
  → Service interface+impl ในไฟล์เดียว (Application) → DTO + ViewModel
  → ลงทะเบียนใน ServiceConfiguration → Controller (บาง)
  → types/domain.ts → api-client.ts → hook → component
```

ต่อ external service ใหม่: สร้าง interface + factory ใน `SupportRoom.Providers.*`,
เพิ่มค่าที่รับได้ใน `ProviderSelection.cs`, เพิ่ม env ใน `.env.example`

Convention ที่ไม่ปกติแต่จงใจ — ทำตาม อย่าเปลี่ยนเอง:
- ไฟล์ service ชื่อ `IXxxService.cs` บรรจุทั้ง interface และ implementation
- status ใช้ `static class` + `const string` ไม่ใช่ C# enum (ให้ตรงกับ TS union type)
- ข้อความที่ผู้ใช้เห็นเป็นภาษาไทย รวมถึง exception message
- service ห้ามพัง flow หลักเพราะ integration รอง — log warning + degrade แทน throw
- คอมเมนต์อธิบาย "ทำไม" ไม่ใช่ "ทำอะไร" — codebase นี้บันทึกเหตุการณ์ที่เคยพังจริงไว้ รักษาระดับนี้

## Solution Design Rule

ก่อนลงมือฟีเจอร์ที่ไม่ trivial:

1. เข้าใจความต้องการจริง (ปัญหาของผู้ใช้ ไม่ใช่วิธีแก้ที่เขาเสนอมา)
2. ไล่ flow เดิมที่เกี่ยวข้องให้จบ
3. หาโค้ด/service/endpoint ที่ใช้ซ้ำได้ — provider abstraction ที่นี่ดีอยู่แล้ว
4. ถามว่ามี library/API/บริการที่แก้ปัญหานี้แล้วหรือยัง
5. เทียบทางเลือก 2–4 แบบพร้อม trade-off
6. เลือกสถาปัตยกรรมที่ง่ายที่สุดที่ตอบความต้องการ *ปัจจุบัน*
7. อย่าสร้าง infrastructure ล่วงหน้า
8. บันทึกการตัดสินใจสำคัญลง `docs/TECH_DECISIONS.md`
9. ลงมือเมื่อทิศทางชัดแล้ว
10. ตรวจด้วย lint/typecheck/test/build ทั้งสองฝั่ง แล้วอ่าน `git diff` ก่อนส่ง

## Definition of Done

- Frontend lint, typecheck, tests และ build ผ่าน
- Backend build/test ผ่านโดยแยก unit tests จาก tests ที่ต้องใช้ provider จริง
- ไม่มี secret หรือ transcript/answer เต็มใน source/log
- API DTO/ViewModel และ TypeScript types ตรงกัน
- Schema change มี migration ใหม่
- เอกสารที่เกี่ยวข้องอัปเดตตามโค้ดจริง

## Known Baseline Issues

เรียงตามความเร่งด่วน — รายละเอียดและทางเลือกอยู่ใน `docs/PROJECT_CONTEXT.md` §19 และ
`docs/TECH_DECISIONS.md`

- **Edge TTS ไม่เหมาะกับ production** — Microsoft เริ่มบล็อกการเรียก Read-Aloud แบบไม่เป็นทางการ
  (ธ.ค. 2025) และกรอง IP ของ datacenter; deploy บน cloud มีโอกาสสูงที่เสียงจะเงียบทั้งระบบ (TD-001)
- ไม่มี auth/rate limiting — `/admin/*` เปิดสาธารณะเมื่อ deploy (TD-002)
- ไม่มี CI (`.github/workflows/` ว่างเปล่า) และไม่มี Dockerfile/deployment artifact (TD-006)
- Background indexing queue เป็น in-memory — restart แล้วงานค้างที่ `pending` ตลอดไป (TD-003)
- Document deletion ยังทิ้ง vectors ไว้ใน Pinecone (TD-004 — chunk id เป็น `{documentId}-{chunkId}`
  อยู่แล้ว ลบด้วย ID prefix ได้; serverless ไม่รองรับ delete by metadata filter)
- **migration สำหรับการแยก TrainingLink/LearningSession ยังไม่ได้สร้าง** — โค้ดทุกชั้นทำแล้ว
  แต่ต้องรัน `dotnet ef migrations add SplitTrainingLinkAndLearningSession` บนเครื่องที่มี .NET 10 SDK
  (เครื่องที่ทำงานล่าสุดมีแค่ SDK 8 จึง build/test backend ไม่ได้เลย)
- backend ยังไม่เคยถูก build/test หลังการแยก TrainingLink/LearningSession ด้วยเหตุผลข้างบน
- API integration test project ยังไม่มี test ที่ยืนยัน endpoint จริง (`UnitTest1.cs` ยังเป็น template)
- test บางชุดใน Application/Providers ยิงไปยัง provider จริง — ต้องแยกด้วย xUnit trait ก่อนตั้ง CI
- EF Core version conflict (MSB3277 ×5): Npgsql 10.0.3 ดึง EF Relational 10.0.4 vs ที่อ้าง 10.0.10
- `IsDelete`/`DeletedAt` มีในทุก entity แต่โค้ดลบจริงทุกครั้ง ไม่มี global query filter
- Frontend มี dependency ตกค้างที่ไม่มีโค้ดเรียกใช้: `googleapis`, `msedge-tts`, `zod`,
  `client-only`, `bufferutil`, `utf-8-validate`
- repo root มีไฟล์ตกค้างจากตอนย้าย monorepo: `node_modules/`, `.next/`, `next-env.d.ts`,
  `tsconfig.tsbuildinfo`, `public/`
- `PackageReference` แบบ floating (`3.*`, `0.*`, `5.*`) ทำให้ build ไม่ deterministic
