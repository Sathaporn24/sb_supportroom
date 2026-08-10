# Environment Setup

## ไฟล์ที่ใช้

- Backend: copy `backend/src/SupportRoom.Api/.env.example` เป็น `.env`
- Frontend: copy `frontend/.env.example` เป็น `.env.local`
- ทั้ง `.env` และ `.env.local` ถูก gitignore และห้าม commit credentials

Development backend โหลด `.env` ผ่าน `DotEnv.Load`; environment variables จาก shell/hosting
มีลำดับความสำคัญสูงกว่า

## Frontend

Frontend อ่านตัวแปรเดียว:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5138
```

ตัวแปร provider ที่ยังอยู่ใน `frontend/.env.example` เป็น legacy และไม่มีโค้ด frontend อ่านค่า
credentials ต้องกำหนดฝั่ง backend เท่านั้น

## Backend: Required Selection

| ตัวแปร | ค่าที่รองรับ |
|---|---|
| `SLIDES_PROVIDER` | `google` |
| `TTS_PROVIDER` | `edge` |
| `VOICE_QUESTION_PROVIDER` | `gemini`, `gemini-rag`, `openai-rag` |
| `KNOWLEDGE_PROVIDER` | `pinecone`, `pinecone-openai` |
| `DOCUMENT_STORAGE_PROVIDER` | `local`, `huawei-obs` |

ทุกตัวต้องมีค่าที่ถูกต้องจึงจะ startup ได้ ไม่มี Mock/default provider

## Database และ Server

| ตัวแปร | Default/หมายเหตุ |
|---|---|
| `POSTGRES_CONNECTION_STRING` | Override `ConnectionStrings:Postgres` |
| `ALLOWED_ORIGINS` | comma-separated; localhost:3000 ถูกเพิ่มอัตโนมัติใน Development |
| `ALLOW_DATA_RESET` | ต้องเป็น `true` จึงใช้ reset/reindex admin endpoints ได้ |
| `MAX_VOICE_UPLOAD_MB` | 5 |
| `MIN_VOICE_DURATION_MS` | 300 |
| `MAX_DOCUMENT_UPLOAD_MB` | 20 |
| `DEFAULT_INTRO_WAIT_MS` | 5000 |
| `DEFAULT_BREATH_PAUSE_MS` | 500 |
| `DEFAULT_FINAL_QUESTION_WAIT_MS` | 5000 |
| `DEFAULT_SESSION_EXPIRY_HOURS` | 24 |

## External Providers

### Google Slides

`GOOGLE_SERVICE_ACCOUNT_PROJECT_ID`, `GOOGLE_SERVICE_ACCOUNT_EMAIL`,
`GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY` ต้องครบ และต้อง share deck ให้ service account เป็น Viewer

### Gemini

`GEMINI_API_KEY` บังคับสำหรับ transcription และ Gemini flows;
`GEMINI_MODEL` default เป็น `gemini-flash-latest`

### OpenAI-compatible

ใช้เมื่อเลือก `openai-rag` หรือ `pinecone-openai`:

- `OPENAI_API_KEY`
- `OPENAI_BASE_URL` — default `https://api.openai.com/v1`
- `OPENAI_MODEL` — default `gpt-4o-mini`
- `OPENAI_EMBEDDING_MODEL` — default `text-embedding-3-small`
- `OPENAI_EMBEDDING_DIMENSIONS` — default 768 และต้องตรงกับ Pinecone index
- `OPENAI_DISABLE_REASONING=true` ใช้เฉพาะ gateway/model ที่รองรับ field `thinking`

### Edge TTS

ไม่ต้องมี API key; `EDGE_TTS_VOICE` default `th-TH-PremwadeeNeural` และ
`EDGE_TTS_RATE` default `-10%`

### Pinecone

ต้องมี `PINECONE_API_KEY` และ `PINECONE_INDEX_HOST` (ไม่รวม `https://`)
ปรับ retrieval ได้ด้วย `RAG_TOP_K` (default 3) และ `RAG_MIN_SCORE` (default 0.4)

### Document Storage

- `local`: `LOCAL_STORAGE_PATH` เป็น optional; default คือ `storage` ใต้ working directory
- `huawei-obs`: ต้องมี endpoint, access key, secret key, bucket และ region ตาม `.env.example`

## Database Migration

```powershell
cd backend
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

Application ไม่เรียก `Database.Migrate()` ตอน startup จึงต้อง apply migration แยกก่อนรัน
