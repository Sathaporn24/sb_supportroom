# CLAUDE.md

คำแนะนำสำหรับผู้ช่วยเขียนโค้ดและทีมพัฒนาที่ทำงานใน `sb_supportroom`

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

## Definition of Done

- Frontend lint, typecheck, tests และ build ผ่าน
- Backend build/test ผ่านโดยแยก unit tests จาก tests ที่ต้องใช้ provider จริง
- ไม่มี secret หรือ transcript/answer เต็มใน source/log
- API DTO/ViewModel และ TypeScript types ตรงกัน
- Schema change มี migration ใหม่
- เอกสารที่เกี่ยวข้องอัปเดตตามโค้ดจริง

## Known Baseline Issues

- ไม่มี auth/rate limiting
- Session expiry ยังบังคับเฉพาะ frontend
- Background indexing queue เป็น in-memory/unbounded
- Document deletion ยังทิ้ง vectors ไว้ใน Pinecone
- EF Core dependency versions ยังมี conflict warning
- API integration test project ยังไม่มี test ที่ยืนยัน endpoint จริง
