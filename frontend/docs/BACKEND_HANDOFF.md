# Backend Handoff

## Current Backend

Backend หลักคือ ASP.NET Core .NET 10 ใน `backend/`; Next.js Route Handlers และ Supabase layer
ถูกถอดออกแล้ว

### Implemented

- REST controllers สำหรับ lessons, slides, sessions, questions, TTS, documents และ admin
- SignalR `SessionHub` สำหรับ live chat/questions
- PostgreSQL repositories + EF Core migrations
- Google Slides + PDF teaching sources
- Edge TTS พร้อม chunking ข้อความยาว
- Gemini full-context, Gemini RAG และ OpenAI-compatible RAG
- Gemini/OpenAI embeddings + Pinecone
- PDF/PPTX/DOCX/XLSX parsing
- Local storage และ Huawei OBS provider
- Background document indexing
- Serilog, correlation ID, error envelope และ Development OpenAPI

## Startup Requirements

1. PostgreSQL connection พร้อมและ apply EF migrations แล้ว
2. `backend/src/SupportRoom.Api/.env` มี provider selections ครบ
3. Credentials ของ providers ที่เลือกพร้อม
4. Frontend ตั้ง `NEXT_PUBLIC_API_BASE_URL`
5. Production ตั้ง `ALLOWED_ORIGINS`

## Operational Notes

- `ALLOW_DATA_RESET=true` เปิดทั้ง reset และ full reindex; ห้ามเปิดโดยไม่มี access control ใน production
- Logs อยู่ `backend/src/SupportRoom.Api/logs/` ตาม working directory ของ process
- Local storage default อยู่ใต้ `storage/`; production ควรกำหนด path ถาวรหรือใช้ OBS
- Application ไม่ auto-migrate database

## Outstanding Risks

- ไม่มี authentication/authorization/rate limiting
- Session expiry ยังไม่ enforce ฝั่ง backend
- `PATCH /api/sessions/{token}` ยังตีความ unknown action เป็น end
- Background queue เป็น in-memory/unbounded
- Document deletion ยังทิ้ง Pinecone vectors
- EF Core package versions conflict
- Tests ผสม unit และ live-provider integration tests
- API integration test project ยังเป็น placeholder
- ไม่มี CI workflow

## Handoff Verification

- [ ] ติดตั้ง frontend dependencies และผ่าน lint/typecheck/test/build
- [ ] backend build ไม่มี dependency warnings
- [ ] unit tests ไม่ต้องใช้ credentials/network
- [ ] live provider tests แยกด้วย category/CI job
- [ ] apply migrations กับ database เป้าหมาย
- [ ] smoke test REST + SignalR + Google/PDF + RAG + TTS
- [ ] ตรวจ auth/rate limiting ก่อนเปิด API ภายนอก
