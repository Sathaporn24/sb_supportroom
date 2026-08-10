# SupportRoom Backend — ER Diagram and Workflow

> Source of truth: `ApplicationDbContext`, entities และ EF Core migrations ใน
> `backend/src/SupportRoom.Providers.Data/`

## ER Diagram

```mermaid
erDiagram
    LESSON_CONFIG ||--o{ TRAINING_SESSION : creates
    LESSON_CONFIG ||--o{ DOCUMENT_RESOURCE : attaches
    TRAINING_SESSION ||--o{ SESSION_QUESTION : records
    TRAINING_SESSION ||--o{ CHAT_MESSAGE : contains
    TRAINING_SESSION ||--o| SESSION_SUMMARY : summarizes

    LESSON_CONFIG {
        string Id PK
        string Slug UK
        string Title
        string ContentSourceType "google_slides|pdf"
        string PresentationId "nullable"
        string PdfDocumentResourceId "nullable"
        json SlideConfigs
        bool IsActive
    }

    TRAINING_SESSION {
        string Id PK
        string Token UK
        string LessonId
        string LessonSlug
        string Status "NOT_STARTED|IN_PROGRESS|ENDED|EXPIRED"
        datetime ExpiresAt
        bool CompletedAllSlides
    }

    SESSION_QUESTION {
        string Id PK
        string SessionId
        string SlideObjectId "nullable"
        string Transcript "nullable"
        string Answer "nullable"
        string AnswerStatus
    }

    CHAT_MESSAGE {
        string Id PK
        string SessionId
        string SenderRole
        string SenderName "nullable"
        string Text
    }

    SESSION_SUMMARY {
        string Id PK
        string SessionId UK
        bool CompletedAllSlides
        string LastSlideObjectId "nullable"
        text_array UnansweredPoints
    }

    DOCUMENT_RESOURCE {
        string Id PK
        string LessonId "nullable"
        string FileName
        string ContentType
        long SizeBytes
        string ObsBucket
        string ObsKey
        string IndexingStatus "pending|indexed|failed"
        int IndexedChunkCount
    }
```

ความสัมพันธ์บางส่วนใช้ string IDs โดยไม่มี database FK navigation กำกับใน `OnModelCreating`;
diagram แสดงความสัมพันธ์เชิง domain ที่ services/repositories ใช้

## Data Ownership

- `LessonConfig.SlideConfigs` เป็น EF owned collection เก็บเป็น JSON
- Google Slides/PDF content ไม่ถูก snapshot ลงตาราง lesson
- PDF/knowledge file bytes อยู่ใน storage; `DocumentResource` เก็บ metadata/pointer
- Pinecone อยู่นอก PostgreSQL และ partition ด้วย lesson slug หรือ `kb-global`
- Summary เก็บ unanswered points; question records อ่านแยกตาม session ID

## Main Workflow

```mermaid
flowchart TD
    Admin[Admin configures lesson] --> Source{Content source}
    Source -->|Google Slides| Google[Resolve live deck]
    Source -->|PDF| Pdf[Resolve uploaded PDF]
    Google --> Lesson[(LessonConfig)]
    Pdf --> Lesson
    Lesson --> Index[Best-effort RAG indexing]
    Lesson --> Session[(TrainingSession)]
    Session --> Room[Teacher room]
    Room --> Voice[Voice question]
    Voice --> Question[(SessionQuestion)]
    Voice --> Live[SignalR broadcast]
    Room --> Chat[(ChatMessage)]
    Chat --> Live
    Room --> End[End session]
    End --> Summary[(SessionSummary)]
```

## Document Indexing Workflow

```mermaid
sequenceDiagram
    participant API
    participant Storage
    participant DB as PostgreSQL
    participant Queue as In-memory Queue
    participant Index as Parser/Embedding/Pinecone

    API->>Storage: upload bytes
    API->>DB: insert DocumentResource(pending)
    API->>Queue: enqueue work item
    API-->>API: return response
    Queue->>Index: extract and index chunks
    Queue->>DB: set indexed/failed + chunk count
```

Queue ปัจจุบันไม่ durable; restart ก่อนประมวลผลเสร็จอาจทิ้ง row เป็น `pending`

## Voice RAG Workflow

1. Gemini transcribes audio
2. Embedding provider สร้าง query vector
3. Pinecone query ทั้ง lesson namespace และ `kb-global`
4. Merge top-K และกรองด้วย minimum score
5. Gemini หรือ OpenAI-compatible model สร้าง grounded answer
6. Persist question และ broadcast `ReceiveNewQuestion`

Full-context `VOICE_QUESTION_PROVIDER=gemini` ข้ามขั้น embedding/query และส่ง lesson context
ให้ Gemini โดยตรง

## Schema Changes

สร้าง migration ใหม่เสมอ:

```powershell
dotnet ef migrations add <Name> --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```
