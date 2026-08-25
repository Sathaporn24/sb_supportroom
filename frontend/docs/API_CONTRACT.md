# API Contract

Base URL มาจาก `NEXT_PUBLIC_API_BASE_URL` และ response JSON ใช้ camelCase

Frontend แยก request ชัดเจน: learner endpoints ไม่แนบ admin JWT และไม่แนบ `?company=` แม้เปิด
ใน browser ที่ล็อกอินหลังบ้านอยู่; back-office endpoints แนบทั้ง JWT และ company context

Error กลาง:

```json
{ "error": { "code": "VALIDATION_ERROR", "message": "...", "requestId": "..." } }
```

## REST Endpoints

| Method | Path | หน้าที่ |
|---|---|---|
| GET | `/api/health` | สถานะ API และ provider selection |
| GET | `/api/lessons` | รายการบทเรียน |
| POST | `/api/lessons` | create/update lesson ตาม slug |
| GET | `/api/lessons/{slug}` | lesson + resolved teaching content (back office, JWT) |
| GET | `/api/lessons/by-link/{token}` | learner-safe lesson timing + resolved teaching content; ไม่มี source/provider/document IDs |
| GET | `/api/lessons/pdf-preview?documentId=` | preview PDF เป็น slides |
| GET | `/api/lessons/pdf-pages/{token}/{documentId}/{pageNumber}` | learner PDF page; token ต้องชี้มาที่บทเรียนที่ใช้ document นี้ |
| POST | `/api/slides/resolve` | resolve Google Slides URL |
| GET | `/api/slides/content?presentationId=` | slide order และ speaker notes |
| GET | `/api/training-links` | รายการลิงก์ + จำนวนผู้เข้าเรียนต่อลิงก์ |
| POST | `/api/training-links` | สร้างลิงก์ (ไม่มีชื่อผู้รับแล้ว) |
| GET | `/api/training-links/{token}` | public link metadata ขั้นต่ำ + lesson title (หน้า join/room) |
| GET | `/api/training-links/by-token/{token}` | full link metadata สำหรับ back office (JWT + company scope) |
| GET | `/api/training-links/{id}/by-id` | ลิงก์ตาม internal ID |
| GET | `/api/training-links/{id}/learning-sessions` | ทุกคนที่เปิดลิงก์นี้ (CS) |
| POST | `/api/learning-sessions/{token}/join` | เข้าเรียน — idempotent ต่อ learnerKey |
| POST | `/api/learning-sessions/{token}/restart` | เรียนอีกครั้ง (รอบใหม่) |
| PATCH | `/api/learning-sessions/{token}/progress` | อัปเดตสไลด์ล่าสุด + LastActivityAt |
| PATCH | `/api/learning-sessions/{token}/end` | จบการเรียน |
| GET | `/api/learning-sessions/{token}/summary?learnerKey=` | สรุปของผู้เรียนเอง; ไม่มี review fields/UnansweredPoints |
| GET | `/api/learning-sessions/{id}/summary/by-id` | สรุปของการเรียนใดก็ได้ (CS) |
| GET | `/api/session-questions?token=&learnerKey=` | คำถามของผู้เรียนเอง |
| GET | `/api/session-questions/by-learning-session/{id}` | คำถามในการเรียนหนึ่ง (CS) |
| PATCH | `/api/session-questions/{id}/review` | CS ทำเครื่องหมายถูก/ผิด + หมายเหตุ |
| POST | `/api/tts` | JSON `{ text, token, learnerKey, rate? }` → audio bytes |
| POST | `/api/voice-question` | multipart audio question |
| POST | `/api/text-question` | JSON `{ token, learnerKey, text, currentSlideObjectId? }` - typed question, same result shape as voice (F10/TQ-2) |
| GET | `/api/documents?scopeType=&scopeId=` | documents ตาม scope (`lesson`/`category`/`company`); ไม่ส่ง query เลย = `company` |
| POST | `/api/documents` | upload document (multipart `file` + `scopeType` + `scopeId?`), สูงสุดตาม `MAX_DOCUMENT_UPLOAD_MB` |
| PATCH | `/api/documents/{id}/scope` | JSON `{ scopeType, scopeId? }` — ย้ายเอกสารไป scope ใหม่ (DS-5/DS-6) |
| DELETE | `/api/documents/{id}` | ลบ metadata/storage object |
| POST | `/api/admin/reset` | ลบ link/learning session/question เมื่ออนุญาต |
| POST | `/api/admin/reindex` | rebuild RAG namespaces เมื่ออนุญาต |

## Key Payloads

### `POST /api/voice-question`

`multipart/form-data`:

- `audio`, `token`, `learnerKey` — required
- `durationMs`, `currentSlideObjectId` — optional

`token` บอกว่าบทเรียนไหน/บริษัทไหน · `learnerKey` บอกว่าเป็นคนไหนบนลิงก์นั้น
ถ้าไม่มี learnerKey คำถามจะถูกบันทึกและ broadcast ผิดคน

Response (`VoiceAnswerViewModel` - ใช้ร่วมกับ `/api/text-question` ตัวเดียวกัน):

```json
{
  "transcript": "...",
  "answer": "...",
  "answerStatus": "answered",
  "relatedSlideObjectId": "..."
}
```

`answerStatus`: `answered`, `not_found`, `out_of_scope`, `no_speech`, `transcription_failed`

⚠️ **มติ U1 (2026-08-23)**: ไม่มี `expecting`/`readiness` อีกต่อไปทั้งฝั่ง request และ response —
readiness ("พร้อมหรือยัง") ตอบได้ทางเดียวคือปุ่ม "พร้อมแล้ว"/"ยังไม่พร้อม" ในหน้าห้อง ไม่ผ่าน
voice-question/text-question เลย

### `POST /api/text-question`

```json
{ "token": "...", "learnerKey": "...", "text": "...", "currentSlideObjectId": "..." }
```

Response: `VoiceAnswerViewModel` เดียวกับ `/api/voice-question` ทุกฟิลด์ (TQ-2) — ไม่มี `durationMs`
เพราะไม่มีเสียงให้วัดความยาว

### `POST /api/lessons`

รับ `LessonConfigDto`: slug/title, Google Slides metadata, `contentSourceType` (`google_slides`
หรือ `pdf`), optional `pdfDocumentResourceId`, timing, slide configs และ `isActive`

### `POST /api/learning-sessions/{token}/join`

```json
{ "recipientName": "ครูสมศรี", "learnerKey": "<uuid ที่ browser สร้างเอง>" }
```

**Idempotent**: browser ที่มี session บนลิงก์นี้อยู่แล้วจะได้ตัวเดิมกลับมา ไม่ใช่ตัวใหม่ —
นี่คือสิ่งที่ทำให้ reconnect/เปิดแท็บใหม่ไม่เสียความคืบหน้า และเป็นทางที่ `lastSlideIndex`
เดินทางกลับมาให้ frontend เรียนต่อจากจุดเดิม

ลิงก์หมดอายุจะถูกปฏิเสธที่ join เท่านั้น — คนที่เรียนค้างอยู่ยังจบและดูสรุปได้

### `PATCH /api/session-questions/{id}/review`

```json
{ "reviewResult": "incorrect", "reviewNote": "AI เดาเอง ไม่มีเอกสารเรื่องนี้" }
```

`reviewResult` รับแค่ `correct` / `incorrect` · `reviewNote` เป็น free text ไม่บังคับ
(เหตุผลที่ไม่ทำเป็น enum อยู่ใน CORE_FEATURE_SPEC §2.7)

## SignalR Contract

Hub: `/hubs/session`

**Group key = LearningSession id** ไม่ใช่ token

เดิมใช้ token ซึ่งถูกต้องตอน 1 ลิงก์ = 1 คน แต่พอลิงก์เดียวเปิดกันทั้งหน่วยงาน กลุ่มที่ผูกกับ
token จะยัดทุกคนไว้ห้องเดียวกัน แล้วส่งคำถามของแต่ละคนไปให้ทุกคนที่ถือลิงก์เดียวกัน

ผู้เรียนไม่มี client invoke ใดเหลือแล้ว (F10-a ตัดฟีเจอร์แชตออกทั้งฟีเจอร์ทั้งสองฝั่ง — คำถามของ
ผู้เรียนไปทาง REST `/api/voice-question` ไม่ใช่ hub) — ผู้เรียนไม่ต้อง join group ใดเพื่อให้ CS
ได้ยิน เพราะ `NotifyNewQuestionAsync` broadcast ไปที่ group ตรงอยู่แล้ว

Client invokes (ฝั่ง CS — ไม่มี learnerKey จึงอ้างด้วย id ตรง ๆ):

- `JoinSessionAsAgent(learningSessionId)`

Server events:

- `ReceiveNewQuestion(SessionQuestion)`

Agent connection ส่ง JWT ผ่าน `accessTokenFactory` และ `?company=`; agent methods ตรวจ auth
ใน service guard ส่วน learner hub คง anonymous เพราะผู้เรียนไม่มีบัญชี
