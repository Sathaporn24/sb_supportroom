# System Architecture

> สถานะ: อ้างอิงโค้ดใน monorepo ปัจจุบัน ณ branch นี้

## ภาพรวม

```text
Next.js Browser UI
  ├─ REST: frontend/src/lib/api-client.ts
  └─ SignalR: frontend/src/hooks/use-session-chat.ts
                ↓
ASP.NET Core API (.NET 10)
  ├─ Controllers / SessionHub
  ├─ Application Services
  ├─ Domain contracts/entities
  └─ Provider implementations
       ├─ PostgreSQL ผ่าน EF Core/Npgsql
       ├─ Google Slides หรือ PDF renderer
       ├─ Edge TTS
       ├─ Gemini / OpenAI-compatible API
       ├─ Pinecone
       └─ Local storage / Huawei OBS
```

Next.js ทำหน้าที่ UI เท่านั้น ไม่มี `src/app/api/**` และไม่ถือ credentials ของ provider

## Backend Layers

| Layer | หน้าที่ |
|---|---|
| `SupportRoom.Api` | HTTP controllers, SignalR hub, DI, CORS, logging, exception pipeline, background worker |
| `SupportRoom.Application` | Use cases, validation/orchestration, DTOs, ViewModels, realtime abstraction |
| `SupportRoom.Domain` | Entities, status constants, provider selection และ environment readers |
| `SupportRoom.Infrastructure` | Error envelope, exception handlers และ CORS |
| `SupportRoom.Providers.Data` | EF Core DbContext, repositories, unit of work และ migrations |
| `SupportRoom.Providers.Slides` | Google Slides และ PDF rendering |
| `SupportRoom.Providers.Tts` | Edge TTS |
| `SupportRoom.Providers.VoiceQuestion` | Gemini full-context และ RAG answer pipelines |
| `SupportRoom.Providers.Knowledge` | Gemini/OpenAI embeddings และ Pinecone vector index |
| `SupportRoom.Providers.DocumentParsing` | PDF, PPTX, DOCX, XLSX text extraction |
| `SupportRoom.Providers.Storage` | Local filesystem และ Huawei OBS |

## Frontend Boundaries

- REST calls รวมที่ `frontend/src/lib/api-client.ts`
- SignalR connection อยู่ใน `frontend/src/hooks/use-session-chat.ts`
- Browser media/TTS orchestration อยู่ใน hooks
- `frontend/src/tutor/` เป็น pure reducer ไม่รู้จัก fetch, SignalR, MediaRecorder หรือ provider SDK
- Wire types อยู่ใน `frontend/src/types/` และต้องตรงกับ backend ViewModels

## Provider Selection

ระบบไม่มี Mock fallback และอ่าน selection ตอน backend startup:

```text
SLIDES_PROVIDER=google
TTS_PROVIDER=edge
VOICE_QUESTION_PROVIDER=gemini | gemini-rag | openai-rag
KNOWLEDGE_PROVIDER=pinecone | pinecone-openai
DOCUMENT_STORAGE_PROVIDER=local | huawei-obs
```

PostgreSQL เป็น dependency บังคับและไม่มี `DATA_PROVIDER` switch

## Data Ownership

- PostgreSQL เก็บ metadata/config/history: company, admin user, lesson, link, learning session,
  question, chat และ document metadata; summary คำนวณสด ไม่มีตาราง summary
- Google Slides หรือ PDF storage เป็น source of truth ของ teaching content
- Pinecone เก็บ embeddings/chunks แยกจาก PostgreSQL โดยใช้ `{companyId}:{lessonSlug}` หรือ
  `{companyId}:kb-global` เป็น namespace
- Frontend ไม่เข้าถึงฐานข้อมูลหรือ provider ภายนอกโดยตรง

## Background Work

Document upload จะบันทึกไฟล์และแถว `DocumentResource` ก่อน แล้ว enqueue งาน parse/embed/index
เข้า in-memory channel ซึ่ง `QueuedHostedService` ประมวลผลใน DI scope ใหม่

ข้อจำกัดปัจจุบัน: queue เป็น unbounded และไม่ durable; process restart อาจทิ้งเอกสารไว้สถานะ
`pending`

## Cross-cutting Concerns

- JSON ใช้ camelCase
- Error ใช้ envelope `{ error: { code, message, requestId } }`
- Serilog ใส่ correlation ID และเขียน console/rolling files
- Development CORS อนุญาต `http://localhost:3000`; production อ่าน `ALLOWED_ORIGINS`
- Swagger/OpenAPI เปิดเฉพาะ Development
- Back office มี JWT/RBAC และ company authorization แล้ว; learner ใช้ link token + learnerKey
- ยังไม่มี rate limiting/abuse protection และ provider settings ยังเป็น env ตอน startup
