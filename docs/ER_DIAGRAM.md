# ER Diagram

> ตรงกับ `supabase/migrations/0001_initial_schema.sql` และ `src/types/domain.ts` จริง
> **หมายเหตุ: Migration นี้ยังไม่ถูก Apply กับ Supabase Project จริงในเฟสนี้** — ใช้เป็น
> พิมพ์เขียวสำหรับตอนเปิด `DATA_PROVIDER=supabase`

```mermaid
erDiagram
    lessons ||--o{ lesson_slide_configs : "has"
    lessons ||--o{ training_sessions : "used by"
    training_sessions ||--o{ session_questions : "has"
    training_sessions ||--o| session_results : "summarized by"

    lessons {
        uuid id PK
        text slug UK
        text title
        text description
        text slides_source_url
        text presentation_id
        text slides_embed_url
        int intro_wait_ms
        int breath_pause_ms
        int final_question_wait_ms
        bool is_active
        timestamptz created_at
        timestamptz updated_at
    }

    lesson_slide_configs {
        uuid id PK
        uuid lesson_id FK
        text slide_object_id
        int slide_index
        int video_duration_ms "nullable"
        timestamptz created_at
        timestamptz updated_at
    }

    training_sessions {
        uuid id PK
        uuid token UK
        uuid lesson_id FK
        text lesson_slug
        text teacher_name "nullable"
        text school_name "nullable"
        text status "NOT_STARTED|IN_PROGRESS|ENDED|EXPIRED"
        timestamptz expires_at
        timestamptz started_at "nullable"
        timestamptz ended_at "nullable"
        bool completed_all_slides
        text last_slide_object_id "nullable"
        timestamptz created_at
        timestamptz updated_at
    }

    session_questions {
        uuid id PK
        uuid session_id FK
        text slide_object_id "nullable"
        text transcript "nullable"
        text answer "nullable"
        text answer_status "answered|not_found|out_of_scope|no_speech|transcription_failed"
        timestamptz created_at
    }

    session_results {
        uuid id PK
        uuid session_id FK_UK
        bool completed_all_slides
        text last_slide_object_id "nullable"
        jsonb questions_summary "nullable"
        jsonb unanswered_points "nullable"
        timestamptz created_at
    }
```

## หมายเหตุการออกแบบ

- **`lessons` ไม่เก็บเนื้อหาสไลด์** (ไม่มีคอลัมน์ speaker notes/รูป/วิดีโอ) — เก็บแค่ URL
  และค่าตั้งค่า เนื้อหาจริงอ่านสดจาก Google Slides API ทุกครั้ง
- **`lesson_slide_configs`** มีแค่ `video_duration_ms` ต่อ Slide เพราะเป็นค่าเดียวที่
  Admin ต้องกำหนดเอง (Google Slides API บอกความยาววิดีโอที่แม่นยำไม่ได้ผ่าน iframe)
- **`training_sessions.lesson_slug`** เป็นข้อมูลซ้ำ (denormalized) จาก `lessons.slug`
  เพื่อให้ query หา Session ด้วย Slug ได้เร็วโดยไม่ต้อง join ทุกครั้ง — ซิงก์ตอนสร้าง
  Session เท่านั้น (ไม่มีการแก้ lesson slug ภายหลังในเฟสนี้)
- **`session_results`** เป็น Snapshot ที่เขียนครั้งเดียวตอนจบ Session (ไม่ใช่ Live View)
  ตรงกับ `SessionSummaryRepository.save()` ที่เรียกจาก
  `PATCH /api/sessions/[token]` (action `end`)
- **RLS เปิดทุกตาราง ไม่มี Policy ให้ anon/authenticated** — เข้าถึงได้เฉพาะผ่าน
  Service Role Key ที่ฝั่ง Server เท่านั้น (ดู [SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md))
