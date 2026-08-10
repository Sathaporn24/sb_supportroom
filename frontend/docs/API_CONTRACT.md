# API Contract

Base URL มาจาก `NEXT_PUBLIC_API_BASE_URL` และ response JSON ใช้ camelCase

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
| GET | `/api/lessons/{slug}` | lesson + resolved teaching content |
| GET | `/api/lessons/pdf-preview?documentId=` | preview PDF เป็น slides |
| GET | `/api/lessons/pdf-pages/{documentId}/{pageNumber}` | rendered PDF page image |
| POST | `/api/slides/resolve` | resolve Google Slides URL |
| GET | `/api/slides/content?presentationId=` | slide order และ speaker notes |
| GET | `/api/sessions` | รายการ sessions |
| POST | `/api/sessions` | สร้าง session link |
| GET | `/api/sessions/{token}` | session + lesson title |
| GET | `/api/sessions/{id}/by-id` | session ตาม internal ID |
| PATCH | `/api/sessions/{token}` | `action=start` หรือ `action=end` |
| GET | `/api/sessions/{token}/summary` | session summary |
| GET | `/api/session-questions?sessionId=` | Push-to-Talk history |
| GET | `/api/chat-messages?sessionId=` | chat history |
| POST | `/api/tts` | JSON `{ text, rate? }` → audio bytes |
| POST | `/api/voice-question` | multipart audio question/readiness |
| GET | `/api/documents?lessonSlug=` | lesson documents หรือ standalone documents |
| POST | `/api/documents` | upload document, สูงสุดตาม `MAX_DOCUMENT_UPLOAD_MB` |
| DELETE | `/api/documents/{id}` | ลบ metadata/storage object |
| POST | `/api/admin/reset` | ลบ session/question/summary เมื่ออนุญาต |
| POST | `/api/admin/reindex` | rebuild RAG namespaces เมื่ออนุญาต |

## Key Payloads

### `POST /api/voice-question`

`multipart/form-data`:

- `audio`, `lessonSlug`, `sessionId` — required
- `durationMs`, `currentSlideObjectId` — optional
- `expecting=question|readiness`

Response:

```json
{
  "transcript": "...",
  "answer": "...",
  "answerStatus": "answered",
  "relatedSlideObjectId": "...",
  "readiness": "ready"
}
```

`answerStatus`: `answered`, `not_found`, `out_of_scope`, `no_speech`,
`transcription_failed`; `readiness` มีเฉพาะ readiness flow

### `POST /api/lessons`

รับ `LessonConfigDto`: slug/title, Google Slides metadata, `contentSourceType` (`google_slides`
หรือ `pdf`), optional `pdfDocumentResourceId`, timing, slide configs และ `isActive`

### `PATCH /api/sessions/{token}`

```json
{ "action": "start" }
```

หรือ

```json
{ "action": "end", "completedAllSlides": true, "lastSlideObjectId": "..." }
```

ข้อจำกัดปัจจุบัน: controller ยังไม่ได้ reject action ที่ไม่รู้จักและจะตีความเป็น `end`

## SignalR Contract

Hub: `/hubs/session`

Client invokes:

- `JoinSession(token)`
- `SendChatMessage(token, senderRole, senderName, text)`

Server events:

- `ReceiveChatMessage(ChatMessage)`
- `ReceiveNewQuestion(SessionQuestion)`

ปัจจุบันไม่มี authentication; token ใช้เป็น group key และ `senderRole` ยังเชื่อค่าจาก client
