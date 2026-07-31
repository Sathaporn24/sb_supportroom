# Use Case Diagram

```mermaid
flowchart LR
    CS([CS / Admin])
    Teacher([Teacher / User])
    Slides[(Google Slides)]
    Gemini[(Gemini)]
    HF[(Hugging Face)]
    Supabase[(Supabase)]

    CS --> UC1[Configure lesson]
    CS --> UC2[Sync slide metadata and notes]
    CS --> UC3[Set video duration per slide]
    CS --> UC4[Create session link]
    CS --> UC10[View session summary]
    CS --> UC11[Reset demo data - Mock only]

    Teacher --> UC5[Join room]
    Teacher --> UC6[Play teaching slide]
    Teacher --> UC7[Ask through Push-to-Talk]
    Teacher --> UC9[End session]

    UC2 -.reads via Route Handler.-> Slides
    UC6 -.embeds directly.-> Slides
    UC7 --> UC8[Answer grounded question]
    UC8 -.transcribe+answer via Route Handler.-> Gemini
    UC8 -.synthesize answer via Route Handler.-> HF
    UC6 -.synthesize speaker notes via Route Handler.-> HF

    UC1 -.persist config.-> Supabase
    UC4 -.persist session.-> Supabase
    UC8 -.persist question.-> Supabase
    UC9 -.persist summary.-> Supabase
    UC10 -.read.-> Supabase
```

## รายละเอียด Use Case

| Use Case | Actor | Route Handler ที่เกี่ยวข้อง | สถานะ |
|---|---|---|---|
| Configure lesson | CS | `POST /api/lessons` | ทำงานจริงกับ Mock/Supabase Repository |
| Sync slide metadata and notes | CS | `POST /api/slides/resolve`, `GET /api/slides/content` | ทำงานจริงกับ Mock Provider, Google Provider Prepared |
| Set video duration per slide | CS | ส่วนหนึ่งของ `POST /api/lessons` (`slideConfigs[].videoDurationMs`) | ทำงานจริง |
| Create session link | CS | `POST /api/sessions` | ทำงานจริง |
| Join room | Teacher | `GET /api/sessions/[token]` | ทำงานจริง |
| Play teaching slide | Teacher (ระบบขับเคลื่อนอัตโนมัติ) | `GET /api/lessons/[slug]`, `POST /api/tts` | ทำงานจริงกับ Mock, Real Provider Prepared |
| Ask through Push-to-Talk | Teacher | `POST /api/voice-question` | ทำงานจริงกับ Mock, Gemini Prepared |
| Answer grounded question | ระบบ (Gemini/Mock) | ส่วนหนึ่งของ `POST /api/voice-question` | Mock กราวด์กับ Speaker Notes จริง, Gemini Prepared |
| End session | Teacher | `PATCH /api/sessions/[token]` (action `end`) | ทำงานจริง |
| View session summary | CS | `GET /api/sessions/[token]/summary` | ทำงานจริง |
| Reset demo data | CS | `POST /api/admin/reset` | ทำงานเฉพาะ Mock Mode |
