# BACKEND / DATABASE HANDOFF

## Architecture boundary

```text
Next.js browser
  ├─ REST + JWT (back office)
  ├─ REST + link token/learnerKey (learner)
  └─ SignalR (agent JWT; learner anonymous token/key)
        ↓
ASP.NET Controllers/Hub → Application services → Repositories/Providers
        ↓
PostgreSQL | Pinecone | Object Storage | Google/Gemini/OpenAI-compatible | TTS
```

กฎ: browser ห้ามคุย DB/provider ตรง; controller บาง; tenant context ต้องมาจาก verified JWT+
authorization หรือ `TrainingLink.Token`; ห้ามรับ company/lesson/session combination จาก client แล้วเชื่อทันที

## Entity relationships

```text
Company
 ├─ AdminUser*          (*owner มี CompanyId=null)
 ├─ LessonConfig ──< TrainingLink ──< LearningSession ──< SessionQuestion
 │       └─< DocumentResource                     └─────< ChatMessage
 └─ global DocumentResource (LessonId=null)
```

รายละเอียดคอลัมน์/เหตุผล: [`schema.dbml`](./schema.dbml) และ
[`backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`](../backend/docs/ER_DIAGRAM_AND_WORKFLOW.md)

ข้อเท็จจริงที่ต้องรู้:

- เส้นใน DBML เป็น logical relationship; EF schema ปัจจุบันไม่มี database FK/navigation จริง
- ทุก business table ยกเว้น `Company`/`AdminUser` มี `CompanyId` + global query filter
- `Company`/`AdminUser` ป้องกันด้วย `IAuthorizationGuard` เท่านั้น
- Slug unique ต่อ company; link token และ admin email unique ทั้งระบบ
- `SessionQuestion.SessionId`/`ChatMessage.SessionId` ชี้ `LearningSession`
- ไม่มี `SessionSummary` table; คำนวณสด
- vectors ไม่มี DB pointer และไฟล์ bytes อยู่นอก DB
- soft-delete fields มีแต่ไม่มี behavior; delete ปัจจุบันเป็น hard delete

## API/security partition

| Surface | Credential/scope | ตัวอย่าง |
|---|---|---|
| Back office REST | JWT + `?company=` + guard | lessons list/save, documents, links, review, users |
| Learner REST | link token + learnerKey | join/progress/end/summary, voice, TTS, chat history |
| Public link metadata | link token | title/status ก่อน join |
| Agent SignalR | JWT + company query + learningSessionId | join/send as agent |
| Learner SignalR | link token + learnerKey | join/send as recipient |
| Owner system actions | JWT owner + deployment switch where applicable | reset/reindex; future settings |

API wire contract: [`frontend/docs/API_CONTRACT.md`](../frontend/docs/API_CONTRACT.md)

## Migration handoff

ล่าสุด: `20260813140603_SplitLinkAndAddAuth` รวม TrainingLink/LearningSession + Company/AdminUser/auth.
Migration generate แล้ว แต่ยังไม่มีหลักฐานว่า apply บน Postgres จริง

ก่อน merge/deploy:

1. สำรอง DB และบันทึก row counts ของตารางเดิม
2. รัน generated SQL review โดยเฉพาะ rename/backfill legacy session IDs
3. Apply บน staging Postgres version เดียวกับ production
4. ตรวจ unique/index/query filter/data isolation และ legacy questions/chat mapping
5. Smoke login owner/admin/cs + 2 companies + link/join/reconnect/PDF/RAG/chat/review
6. ทดสอบ rollback จาก backup; migration Down อย่างเดียวไม่ใช่ backup strategy

## Backend backlog แบ่งงานได้

### P0 — production safety

- Rate limiting/abuse prevention สำหรับ login, join, voice, TTS และ SignalR
- Secret storage/encryption/rotation/redaction + provider setting activation/rollback
- TTS provider ที่มี SLA
- Privacy/retention/deletion/export policy implementation
- Integration tests กับ Postgres และ HTTP/SignalR auth boundary
- Deployment artifact/health/readiness/config validation

### P1 — data/knowledge reliability

- Durable queue + retry/backoff/dead-letter/recovery for indexing
- Delete/re-index vector consistency; idempotent jobs
- Foreign-key strategy หรือ explicit reason/tests หากคง logical-only
- Provider/prompt/config version snapshot ต่อ question
- Audit event table (ไม่ใช่แค่ CreateBy/UpdateBy) สำหรับ login, role/company/settings, delete/reindex
- Pagination/filter/sort สำหรับ links/sessions/questions/documents/users

### P2 — maintainability/scale

- Pin package versions ที่ยังเป็น floating และ clean legacy dependencies (EF conflict แก้แล้ว)
- Observability: latency, answer status, tokens/cost, queue depth/failures, provider health
- SignalR backplane/multi-instance only when capacity requires
- pgvector decision only after workload/cost evidence

## Known behavioral constraints

- Existing learner may reconnect after link expiry; new join/restart may not
- `MaxAttendees` stored but not enforced
- Inactivity/stalled computed from `LastActivityAt`; not persisted
- Readiness answer is not stored as a question
- Retrieval outage falls back to full-deck; indexed-but-low-score yields `not_found`
- TTS failure does not stop lesson
- PDF page rendering/cache is in-memory and process-local
