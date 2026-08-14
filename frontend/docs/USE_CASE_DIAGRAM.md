# Use Case Diagram

```mermaid
flowchart LR
    CS[ทีม CS/Admin]
    Learner[ผู้เรียน]

    Lesson[จัดการ Google Slides/PDF Lesson]
    Docs[อัปโหลด Knowledge Documents]
    Link[สร้างลิงก์ + ดูผู้เข้าเรียนทั้งหมด]
    Join[กรอกชื่อเข้าห้อง]
    Room[เรียนผ่าน Tutor Room]
    Voice[ถามด้วย Push-to-Talk]
    Chat[แชตสดผ่าน SignalR]
    Summary[ดูสรุปการเรียนรายคน]
    Review[ตรวจคำตอบ AI ถูก/ผิด + หมายเหตุ]
    Recap[ดูคำถามของตัวเอง / เรียนอีกครั้ง]

    CS --> Lesson
    CS --> Docs
    CS --> Link
    CS --> Chat
    CS --> Summary
    CS --> Review
    Learner --> Join
    Join --> Room
    Learner --> Voice
    Learner --> Chat
    Learner --> Recap
```

| Use case | Backend |
|---|---|
| จัดการบทเรียน | LessonController, Google Slides/PDF providers, PostgreSQL |
| สร้างลิงก์ | TrainingLinkController |
| เข้าห้อง/เรียนต่อ/เรียนอีกครั้ง | LearningSessionController |
| สอนและเล่นเสียง | LessonController + TtsController |
| ถามเสียง | VoiceQuestionController + Gemini/RAG/Pinecone |
| Knowledge documents | DocumentsController + background queue |
| แชตสด | SessionHub + ChatMessageService |
| สรุปผล | LearningSessionService.GetSummary (คำนวณสด ไม่มีตาราง summary) |
| ตรวจคำตอบ | SessionQuestionService.Review |

Back office มี JWT/RBAC; learner flow ตั้งใจ anonymous โดย link token เป็น public capability
ส่วน `learnerKey` ที่ browser เก็บใช้แยกคนบนลิงก์เดียวกัน ไม่ใช่ credential

ผู้เรียน**ไม่เห็น** "จุดที่ตอบไม่ได้ รอ CS ตรวจสอบ" และไม่เห็นผลรีวิว — เป็นข้อมูลภายใน
