# Use Case Diagram

```mermaid
flowchart LR
    CS[ทีม CS/Admin]
    Teacher[คุณครู]

    Lesson[จัดการ Google Slides/PDF Lesson]
    Docs[อัปโหลด Knowledge Documents]
    Session[สร้างและติดตาม Session]
    Room[เรียนผ่าน Tutor Room]
    Voice[ถามด้วย Push-to-Talk]
    Chat[แชตสดผ่าน SignalR]
    Summary[ดู Questions/Summary]

    CS --> Lesson
    CS --> Docs
    CS --> Session
    CS --> Chat
    CS --> Summary
    Teacher --> Room
    Teacher --> Voice
    Teacher --> Chat
```

| Use case | Backend |
|---|---|
| จัดการบทเรียน | LessonController, Google Slides/PDF providers, PostgreSQL |
| สร้างลิงก์ | TrainingSessionController |
| สอนและเล่นเสียง | LessonController + TtsController |
| ถามเสียง | VoiceQuestionController + Gemini/RAG/Pinecone |
| Knowledge documents | DocumentsController + background queue |
| แชตสด | SessionHub + ChatMessageService |
| สรุปผล | SessionSummaryService |

ปัจจุบันทุก use case ไม่มี authentication; session token เป็นเพียง public identifier
