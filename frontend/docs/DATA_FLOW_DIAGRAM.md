# Data Flow Diagram

```mermaid
flowchart LR
    Browser[Next.js Browser UI]
    Rest[api-client.ts]
    Realtime[use-agent-session-questions.ts]
    Api[ASP.NET Core Controllers]
    Hub[SignalR SessionHub]
    App[Application Services]
    Db[(PostgreSQL / EF Core)]
    Slides[Google Slides]
    Files[Local Storage / Huawei OBS]
    Parser[PDF/PPTX/DOCX/XLSX Parser]
    Embed[Gemini/OpenAI Embeddings]
    Pine[(Pinecone)]
    Answer[Gemini/OpenAI Answer]
    Tts[Edge TTS]

    Browser --> Rest --> Api --> App
    Browser --> Realtime --> Hub --> App
    App --> Db
    %% Realtime is CS-side only (use-agent-session-questions.ts) - learners have no live
    %% connection left since chat was removed (F10-a); questions go via Rest above.
    App --> Slides
    App --> Files --> Parser --> Embed --> Pine
    App --> Pine --> Answer
    App --> Tts
    App --> Hub
```

## หลักการ

- Browser ส่ง REST/SignalR ไป .NET backend เท่านั้น
- Google Slides/PDF เป็น source of truth ของ teaching content
- PostgreSQL เก็บ config และ history; Pinecone เก็บ retrieval vectors
- Voice RAG ทำ transcription → embedding/query → grounded answer
- Document upload ตอบกลับหลัง storage/DB commit แล้ว index ต่อใน background
- Credentials ไม่ผ่าน frontend
