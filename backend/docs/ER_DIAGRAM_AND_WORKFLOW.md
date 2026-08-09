# SB_Ai_Supportroom — ER Diagram & Workflow

โครงสร้างข้อมูลและลำดับการทำงานของระบบ (.NET backend) — รวม Real-time Chat (SignalR, verify แล้ว)
และ RAG ด้วย Pinecone (implement + verify แล้วกับเสียงจริง/Gemini จริง/Pinecone จริง — ทุก Provider
เป็น Real ทั้งหมดแล้ว ไม่มี Mock อีกต่อไป)

## ER Diagram

5 ตารางใน Postgres: `LessonConfig` เป็นต้นทาง สร้าง `TrainingSession` ได้หลายอัน แต่ละ Session มี
`SessionQuestion` (Push-to-Talk log) และ `ChatMessage` (พิมพ์คุยสด) หลายแถว และปิดท้ายด้วย
`SessionSummary` หนึ่งแถว (เขียนตอน end เท่านั้น)

```mermaid
erDiagram
    LESSON_CONFIG ||--o{ TRAINING_SESSION : "spawns"
    TRAINING_SESSION ||--o{ SESSION_QUESTION : "logs"
    TRAINING_SESSION ||--o{ CHAT_MESSAGE : "carries"
    TRAINING_SESSION ||--o| SESSION_SUMMARY : "closes with"

    LESSON_CONFIG {
        string Id PK
        string Slug UK
        string Title
        string SlidesSourceUrl
        string PresentationId "nullable, resolved from URL"
        string SlidesEmbedUrl "nullable"
        int IntroWaitMs
        int BreathPauseMs
        int FinalQuestionWaitMs
        json SlideConfigs "embedded array, not its own table"
        bool IsActive
    }

    TRAINING_SESSION {
        string Id PK
        string Token UK "join link + SignalR group key"
        string LessonId FK
        string LessonSlug
        string TeacherName "nullable"
        string SchoolName "nullable"
        string Status "created / active / ended"
        datetime ExpiresAt
        datetime StartedAt "nullable"
        datetime EndedAt "nullable"
        bool CompletedAllSlides
        string LastSlideObjectId "nullable"
    }

    SESSION_QUESTION {
        string Id PK
        string SessionId FK
        string SlideObjectId "nullable"
        string Transcript "nullable"
        string Answer "nullable"
        string AnswerStatus "answered / not_found / no_speech"
    }

    CHAT_MESSAGE {
        string Id PK
        string SessionId FK
        string SenderRole "teacher / cs / system"
        string SenderName "nullable"
        string Text
    }

    SESSION_SUMMARY {
        string Id PK
        string SessionId FK "unique, 1 session : 0..1 summary"
        bool CompletedAllSlides
        string LastSlideObjectId "nullable"
        list UnansweredPoints "text[]; questions where AnswerStatus=not_found"
    }
```

**หมายเหตุ**
- `SlideConfigs` ไม่ใช่ตารางแยก — เก็บเป็น `text[]`/JSON ฝังอยู่ใน `LessonConfig` เอง (ตาม EF Core native `List<T>` → Postgres array mapping)
- เนื้อหาสไลด์จริง (speaker notes, รูป/วิดีโอ) **ไม่ persist ซ้ำ** — ดึงสดจาก Google Slides ทุกครั้งผ่าน `ISlidesProvider`, `LessonConfig` เก็บแค่ metadata/timing
- `SessionSummary.UnansweredPoints` ไม่ได้ join คำถามทั้งหมดมาเก็บซ้ำ — list คำถามจริงถูก re-join จาก `SessionQuestion` ด้วย `SessionId` ตอนอ่าน (`GetBySessionId`)
- **`ChatMessage` เขียนได้ทาง SignalR Hub เท่านั้น** ไม่มี public POST (เหมือน `SessionQuestion`) — REST มีแค่ `GET /api/chat-messages?sessionId=` สำหรับโหลดประวัติ
- **Pinecone** ไม่ใช่ตารางใน Postgres — เป็น Vector Store แยกภายนอก เก็บ embedding ของ Speaker Notes ต่อ Slide โดย namespace = `LessonConfig.Slug` เชื่อมกันทาง Slug ไม่ใช่ FK ในฐานข้อมูลเดียวกัน (`SupportRoom.Providers.Knowledge`)

## Workflow — ภาพรวมทั้งระบบ

จาก CS สร้างบทเรียน ไปจนจบ Session และสรุปผล — มี Real-time Chat วิ่งขนานอยู่ตลอดตั้งแต่เข้าห้องจนจบ

```mermaid
flowchart TD
    subgraph Setup["1. Admin/CS ตั้งค่า"]
        A1[CS สร้าง Lesson<br/>slug, Google Slides URL, timing] --> A2[POST /api/lessons<br/>resolve PresentationId]
        A2 --> A2b[[Index สไลด์ลง Knowledge Store<br/>Pinecone]]
        A2b --> A3[CS สร้าง Session Link<br/>POST /api/sessions]
    end

    subgraph Join["2. ครูเข้าห้อง"]
        B1[เปิด /room/token] --> B2[GET /api/sessions/token<br/>+ lessonTitle]
        B2 --> B3[PATCH action=start<br/>Status → active]
        B3 --> BH[[เชื่อม SignalR Hub<br/>JoinSession/token/]]
    end

    subgraph Live["3. สอนสด (วนตามสไลด์)"]
        C1[เล่นคำบรรยายสไลด์<br/>Google Slides embed + TTS] --> C2{ครูกด<br/>Push-to-Talk?}
        C2 -- ใช่ --> C3[POST /api/voice-question<br/>audio + slug + sessionId]
        C3 --> C4[Gemini ถอดเสียง + ตอบ<br/>เต็มเดคหรือ RAG]
        C4 --> C4b[[Broadcast ReceiveNewQuestion<br/>ไปหน้า Admin แบบสด]]
        C4b --> C5[POST /api/tts<br/>พูดคำตอบ]
        C5 --> C1
        C2 -- ไม่, จบสไลด์ --> C6{มีสไลด์ถัดไป?}
        C6 -- มี --> C1
        C6 -- หมด --> D1
        CH[[พิมพ์แชทสำรอง<br/>SendChatMessage ↔ ครู/CS]] -.ขนานตลอดเวลา.- C1
    end

    subgraph Close["4. จบ Session"]
        D1[ครูกดจบ] --> D2[PATCH action=end]
        D2 --> D3[บันทึก SessionSummary<br/>รวม unanswered points]
        D3 --> D4[GET /api/sessions/token/summary]
    end

    A3 --> B1
    BH --> C1
```

## Workflow — Voice Q&A Pipeline (Full Context — `VOICE_QUESTION_PROVIDER=gemini`)

รายละเอียดทางเทคนิคของ 1 คำถามเสียง — ส่ง Speaker Notes ทั้งเดคเข้า Gemini ทุกครั้ง (verify แล้ว)

```mermaid
sequenceDiagram
    participant FE as Next.js Frontend
    participant VC as VoiceQuestionController
    participant VS as IVoiceQuestionService
    participant LR as LessonConfig Repo
    participant SP as ISlidesProvider
    participant VP as IVoiceQuestionProvider
    participant Gemini as Gemini API
    participant SQ as SessionQuestion Repo
    participant Hub as SessionHub (SignalR)

    FE->>VC: POST /api/voice-question (multipart: audio)
    VC->>VS: AskAsync(input)
    VS->>LR: GetBySlug(lessonSlug)
    LR-->>VS: LessonConfig (PresentationId)
    VS->>SP: GetLessonContentAsync(presentationId)
    SP-->>VS: Slides + speaker notes (สด จาก Google Slides)
    VS->>VP: TranscribeAndAnswerAsync(audio, ทุก Slide)
    VP->>Gemini: generateContent (audio + Speaker Notes ทั้งเดค)
    Gemini-->>VP: transcript + answer + status (strict JSON)
    VP-->>VS: VoiceQuestionResult
    alt ไม่ใช่ readiness check และไม่ใช่ no_speech
        VS->>SQ: Create(sessionId, transcript, answer, status)
        VS->>Hub: NotifyNewQuestionAsync(token, question)
        Hub-->>FE: ReceiveNewQuestion (ไปหน้า Admin ที่เปิดดูอยู่)
    end
    VS-->>VC: VoiceAnswerViewModel
    VC-->>FE: 200 { transcript, answer, answerStatus, relatedSlideObjectId }
    FE->>FE: POST /api/tts(answer) → เล่นเสียงคำตอบ
```

## Workflow — Voice Q&A Pipeline (RAG — `VOICE_QUESTION_PROVIDER=gemini-rag`)

Implement และทดสอบกับเสียงจริง + Gemini จริง + Pinecone จริงแล้ว (ผลถูกต้อง 100% ในทุกเคสที่ทดสอบ)
เลือกใช้ผ่าน env var เดียว contract หน้าบ้านเหมือนเดิมทุกอย่าง ไม่ต้องแก้ frontend แยกเป็น 3 ขั้น
เพราะต้องรู้คำถามก่อนถึงจะค้นหาส่วนที่เกี่ยวข้องได้

```mermaid
sequenceDiagram
    participant VS as IVoiceQuestionService
    participant VP as RagVoiceQuestionProvider
    participant Gemini as Gemini API
    participant Embed as IEmbeddingProvider
    participant Pine as IKnowledgeIndexProvider (Pinecone)

    VS->>VP: TranscribeAndAnswerAsync(audio, ทุก Slide)
    VP->>Gemini: ①ถอดเสียงอย่างเดียว (prompt สั้น)
    Gemini-->>VP: transcript
    VP->>Embed: ②EmbedAsync(transcript)
    Embed-->>VP: query vector
    VP->>Pine: QueryAsync(namespace=lessonSlug, vector, topK=3)
    Pine-->>VP: Slide ที่เกี่ยวข้องที่สุด 3 อัน
    alt ค้นหาไม่สำเร็จ (Pinecone ล่ม/timeout)
        VP->>VP: fallback → ส่งทั้งเดคแบบเดิม (เสถียรไว้ก่อน)
    end
    VP->>Gemini: ③ตอบคำถาม (transcript + เฉพาะ 3 Slide ที่เกี่ยวข้อง)
    Gemini-->>VP: answer + status + relatedSlideObjectId
    VP-->>VS: VoiceQuestionResult (โครงสร้างเหมือนเดิมทุกอย่าง)
```

## Workflow — RAG Indexing (implement แล้ว — ตอน CS บันทึก Lesson)

Index สไลด์ใหม่ทุกครั้งที่ CS กดบันทึก Lesson — ไม่ต้องมีปุ่ม Sync แยก ไม่บล็อกการบันทึกถ้า index พัง

```mermaid
flowchart LR
    A[CS กดบันทึก Lesson] --> B[resolve PresentationId<br/>เหมือนเดิม]
    B --> C[ดึง Slides เต็ม<br/>GetLessonContentAsync]
    C --> D[Embed Speaker Notes<br/>ทีละ Slide]
    D --> E[Upsert เข้า Pinecone<br/>namespace = lessonSlug]
    E -.ล้มเหลวได้ ไม่บล็อก Save.-> F[บันทึก Lesson สำเร็จ]
    D -.ล้มเหลวได้ ไม่บล็อก Save.-> F
```

## Workflow — Real-time Chat (SignalR, verify แล้ว)

แชทสำรองสองทางระหว่างครูกับทีม CS ผ่าน `SessionHub` — group key คือ `TrainingSession.Token`

```mermaid
sequenceDiagram
    participant Teacher as ครู (/room/token)
    participant Hub as SessionHub
    participant Svc as IChatMessageService
    participant DB as ChatMessage (Postgres)
    participant CS as ทีม CS (/admin/sessions/token)

    Teacher->>Hub: JoinSession(token)
    CS->>Hub: JoinSession(token)
    Teacher->>Hub: SendChatMessage(token, "teacher", text)
    Hub->>Svc: SendAsync(dto)
    Svc->>DB: Add + Commit
    Svc-->>Hub: broadcast ReceiveChatMessage
    Hub-->>Teacher: ReceiveChatMessage
    Hub-->>CS: ReceiveChatMessage (เห็นสดทันที)
    CS->>Hub: SendChatMessage(token, "cs", text)
    Hub-->>Teacher: ReceiveChatMessage (เห็นสดทันที)
    Note over CS: เข้าห้องช้ากว่า? โหลดประวัติผ่าน<br/>GET /api/chat-messages?sessionId= ก่อนได้
```
