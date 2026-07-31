# Data Flow Diagram

```mermaid
flowchart TB
    Browser[Browser<br/>Admin UI / Join / Room]
    APIClient[src/lib/api-client.ts]
    Routes[Next.js Route Handlers<br/>src/app/api/**]

    subgraph Providers
        SlidesProvider[SlidesContentProvider]
        TTSProvider[TextToSpeechProvider]
        VQProvider[VoiceQuestionProvider]
    end

    subgraph Repositories
        LessonRepo[LessonConfigRepository]
        SessionRepo[SessionRepository]
        QuestionRepo[SessionQuestionRepository]
        SummaryRepo[SessionSummaryRepository]
    end

    GoogleSlides[(Google Slides API)]
    Gemini[(Gemini API)]
    HuggingFace[(Hugging Face Inference API)]
    Supabase[(Supabase Postgres)]
    MockStore[(In-memory Mock Store<br/>globalThis, dev-process only)]

    Browser -->|fetch JSON / multipart| APIClient
    APIClient -->|HTTP| Routes

    Routes --> SlidesProvider
    Routes --> TTSProvider
    Routes --> VQProvider
    Routes --> LessonRepo
    Routes --> SessionRepo
    Routes --> QuestionRepo
    Routes --> SummaryRepo

    SlidesProvider -.Real.-> GoogleSlides
    VQProvider -.Real.-> Gemini
    TTSProvider -.Real.-> HuggingFace

    LessonRepo -.Real.-> Supabase
    SessionRepo -.Real.-> Supabase
    QuestionRepo -.Real.-> Supabase
    SummaryRepo -.Real.-> Supabase

    SlidesProvider -.Mock default.-> MockStore
    TTSProvider -.Mock default: generates silent WAV.-> Browser
    VQProvider -.Mock default: grounds against real slide notes.-> MockStore
    LessonRepo -.Mock default.-> MockStore
    SessionRepo -.Mock default.-> MockStore
    QuestionRepo -.Mock default.-> MockStore
    SummaryRepo -.Mock default.-> MockStore

    Routes -->|JSON / audio bytes| APIClient
    APIClient -->|state updates| Browser
```

## หลักการ

- ลูกศร **Real** (เส้นประ) คือ Path ที่ทำงานเมื่อสลับ Environment Variable ไปใช้ Provider
  จริง (`SLIDES_PROVIDER=google`, `TTS_PROVIDER=huggingface`,
  `VOICE_QUESTION_PROVIDER=gemini`, `DATA_PROVIDER=supabase`) — Route Handler และ UI
  ไม่ต้องแก้เลยเมื่อสลับ เพราะ Factory (`src/providers/*/index.ts`) เป็นจุดเดียวที่
  ตัดสินใจ
- **Browser ไม่เคยคุยกับ Google/Gemini/Hugging Face/Supabase โดยตรง** — ทุกอย่างผ่าน
  Route Handler เสมอ (Secret จึงไม่มีทางหลุดไปที่ Client Bundle)
- **Mock Store เป็น In-memory** อยู่ในโปรเซส Next.js dev server เท่านั้น — ไม่ persist
  ข้าม Restart และไม่แชร์กับ Serverless Instance อื่นถ้า Deploy แบบ Production
  Serverless (ดู [BACKEND_HANDOFF.md](./BACKEND_HANDOFF.md) หัวข้อ Known Risks)
