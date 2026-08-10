# System Logic

## Lesson Lifecycle

1. Admin สร้าง lesson โดยเลือก `google_slides` หรือ `pdf`
2. Backend เก็บ metadata/timing ใน PostgreSQL และ resolve content สดจากแหล่งจริง
3. Admin สร้าง session link ที่มี UUID token
4. ครูเข้า join/room; frontend โหลด session และ teaching content จาก .NET API
5. Tutor reducer ขับ intro → readiness → slides → final question → closing
6. เมื่อจบ backend บันทึก `SessionSummary` จากคำถามของ session

Frontend ตรวจ expiry/status ก่อนเข้าห้อง แต่ backend ยังไม่ enforce expiry เองในปัจจุบัน

## Voice Question

- Push-to-Talk ใช้ MediaRecorder; คลิปสั้นกว่า minimum กลับไปสอนแบบเงียบ
- ระหว่างรอคำตอบ hook เล่น processing fillers หลายระดับที่ prefetch ไว้
- `gemini`: ส่งเสียงพร้อม full lesson context ใน request เดียว
- `gemini-rag`: Gemini transcribe → embedding/Pinecone query → Gemini answer
- `openai-rag`: Gemini transcribe → OpenAI embedding/Pinecone → OpenAI-compatible answer
- RAG query lesson namespace และ `kb-global`, merge ตาม score และใช้ threshold
- Retrieval ล้มเหลว fallback เป็น full-deck context
- คำถามปกติถูก persist และ broadcast ผ่าน SignalR; readiness ไม่ถูกเก็บเป็นคำถาม

## Teaching Content

- Google Slides: speaker notes คือ narration และ object ID ใช้อ้าง slide
- PDF: แต่ละหน้าเป็น slide, backend extract narration และ render page image ตาม request
- `videoDurationMs` รวมกับความยาว TTS โดยรอเฉพาะเวลาที่เหลือ
- หลังตอบคำถามจะกลับมาเริ่ม narration ของ slide ที่ถูกขัดจังหวะใหม่

## Documents and RAG Indexing

- รองรับ PDF, PPTX, DOCX และ XLSX สำหรับ knowledge documents
- Upload เขียน storage + `DocumentResource(pending)` ก่อน แล้ว enqueue parse/embed/upsert
- เอกสารผูก lesson ใช้ namespace ของ lesson; standalone ใช้ `kb-global`
- Save Google Slides lesson พยายาม re-index แบบ best effort
- `/api/admin/reindex` rebuild ทุก namespace เมื่อ `ALLOW_DATA_RESET=true`

ข้อจำกัด: queue ไม่ durable และ deletion ยังไม่ลบ vectors รายเอกสารออกจาก Pinecone

## Realtime Chat

- Teacher/Admin join SignalR group ด้วย session token
- `SendChatMessage` persist PostgreSQL แล้ว broadcast `ReceiveChatMessage`
- Push-to-Talk question broadcast `ReceiveNewQuestion`
- REST endpoints ใช้ hydrate history เมื่อเปิดหน้าช้าหรือ reconnect
- ปัจจุบันยังไม่มี identity proof และ client สามารถระบุ `senderRole` เอง

## TTS and Volume

- Backend ใช้ Edge TTS และ chunk ข้อความยาวก่อนรวม audio เพื่อลด timeout
- Frontend เก็บ AI volume ใน browser localStorage และใช้กับ audio ทุกประเภท
- TTS/provider failure เป็น upstream error; tutor พูดข้อความแจ้งใน question failure path
