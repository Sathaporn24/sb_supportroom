# SupportRoom AI

ระบบห้องสอนการใช้งานแบบสนทนาโต้ตอบสำหรับทีม CS และคุณครู เนื้อหาสอนมาจาก Google Slides
หรือไฟล์ PDF, รองรับ Push-to-Talk, คำตอบแบบ grounded/RAG, Edge TTS และแชตสดผ่าน SignalR

## สถาปัตยกรรมปัจจุบัน

โปรเจกต์เป็น monorepo แยก frontend และ backend ชัดเจน:

```text
Browser (Next.js)
  ├─ REST ────────> ASP.NET Core Controllers
  └─ SignalR ─────> SessionHub
                       ↓
                 Application Services
                       ↓
        PostgreSQL / Google Slides / Edge TTS
        Gemini / OpenAI-compatible / Pinecone
        Local storage หรือ Huawei OBS
```

- `frontend/` — Next.js 15, React 19, TypeScript, Tailwind และ SignalR client
- `backend/` — .NET 10, ASP.NET Core, EF Core/PostgreSQL, SignalR และ Serilog
- `backend/src/SupportRoom.Application/` — use cases และ business orchestration
- `backend/src/SupportRoom.Domain/` — entities, status/constants และ configuration contracts
- `backend/src/SupportRoom.Providers.*` — integrations ภายนอกและ persistence
- `backend/tests/` — application/provider/API test projects

Next.js ไม่มี Route Handler ฝั่ง backend แล้ว ทุก `/api/*` และ `/hubs/session` ชี้ไปที่ .NET API
ผ่าน `NEXT_PUBLIC_API_BASE_URL`

## ความสามารถหลัก

- จัดการบทเรียนและสร้างลิงก์ session
- ใช้ Google Slides หรือ PDF เป็นเนื้อหาหลักต่อบทเรียน
- อัปโหลด PDF, PPTX, DOCX และ XLSX เพื่อสร้าง knowledge base
- ถามด้วยเสียงแบบ Push-to-Talk และตอบโดยอ้างอิงเนื้อหาที่กำหนด
- เลือก full-context Gemini, Gemini RAG หรือ OpenAI-compatible RAG
- สังเคราะห์เสียงภาษาไทยด้วย Edge TTS
- แชตสดระหว่างครูกับทีม CS ผ่าน SignalR พร้อมเก็บประวัติ
- เก็บบทเรียน, session, คำถาม, แชต, เอกสาร และ summary ใน PostgreSQL

## เริ่มต้นใช้งาน

สิ่งที่ต้องมี: Node.js/npm, .NET SDK 10, PostgreSQL และ credentials ของ provider ที่เลือก
ระบบปัจจุบันไม่มี Mock provider และ backend ต้องได้รับ provider selection ทุกหมวดอย่างชัดเจน

### Backend

```powershell
cd backend
Copy-Item src/SupportRoom.Api/.env.example src/SupportRoom.Api/.env
# กรอก provider credentials และตั้ง POSTGRES_CONNECTION_STRING หรือ ConnectionStrings:Postgres

dotnet restore SupportRoom.slnx
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
dotnet run --project src/SupportRoom.Api
```

Development API ใช้ `http://localhost:5138` ตาม `launchSettings.json`

### Frontend

```powershell
cd frontend
Copy-Item .env.example .env.local
npm install
npm run dev
```

เปิด <http://localhost:3000>

> `frontend/.env.example` มี legacy server variables เหลืออยู่ แต่ frontend อ่านจริงเฉพาะ
> `NEXT_PUBLIC_API_BASE_URL`; credentials ทั้งหมดต้องอยู่ฝั่ง backend เท่านั้น

## Provider Selection

Backend บังคับให้ตั้งค่าหมวดเหล่านี้:

| หมวด | ค่าที่รองรับ |
|---|---|
| Slides | `SLIDES_PROVIDER=google` |
| TTS | `TTS_PROVIDER=edge` |
| Voice question | `gemini`, `gemini-rag`, `openai-rag` |
| Knowledge | `pinecone`, `pinecone-openai` |
| Document storage | `local`, `huawei-obs` |

รายละเอียด environment ทั้งหมดอยู่ใน
[`backend/src/SupportRoom.Api/.env.example`](./backend/src/SupportRoom.Api/.env.example)

## ตรวจสอบก่อนส่งงาน

```powershell
cd frontend
npm run lint
npm run typecheck
npm run test
npm run build

cd ../backend
dotnet build SupportRoom.slnx
dotnet test SupportRoom.slnx
```

Provider tests บางรายการเรียกบริการจริงและต้องมี credentials/network จึงควรแยกผล unit tests
ออกจาก integration tests เมื่อใช้ใน CI

## สถานะและข้อจำกัดที่ต้องทราบ

- ยังไม่มี authentication/authorization และ rate limiting
- Backend ยังไม่บังคับ session expiry; frontend เป็นผู้ตรวจวันหมดอายุในปัจจุบัน
- Document indexing queue อยู่ใน memory และงาน Pending อาจสูญหายเมื่อ process restart
- การลบเอกสารยังไม่ลบ vector รายเอกสารออกจาก Pinecone
- ไม่มี CI workflow ใน repository
- EF Core/Npgsql dependencies ต้องจัด version ให้ตรงกันเพื่อกำจัด assembly conflict warning

## เอกสาร

- ภาพรวมระบบ: [`frontend/docs/SYSTEM_ARCHITECTURE.md`](./frontend/docs/SYSTEM_ARCHITECTURE.md)
- Environment: [`frontend/docs/ENVIRONMENT_SETUP.md`](./frontend/docs/ENVIRONMENT_SETUP.md)
- API: [`frontend/docs/API_CONTRACT.md`](./frontend/docs/API_CONTRACT.md)
- State machine: [`frontend/docs/STATE_MACHINE.md`](./frontend/docs/STATE_MACHINE.md)
- Data model/workflow: [`backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`](./backend/docs/ER_DIAGRAM_AND_WORKFLOW.md)
- การทดสอบ: [`frontend/docs/TESTING_GUIDE.md`](./frontend/docs/TESTING_GUIDE.md)
