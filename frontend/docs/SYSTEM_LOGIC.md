# System Logic

## Lesson Lifecycle

1. Admin สร้าง lesson โดยเลือก `google_slides` หรือ `pdf`
2. Backend เก็บ metadata/timing ใน PostgreSQL และ resolve content สดจากแหล่งจริง
3. Admin สร้าง `TrainingLink` ที่มี UUID token — ส่งให้กี่คนก็ได้จนกว่าจะหมดอายุ
4. ผู้เรียนเปิดลิงก์ กรอกชื่อตัวเอง แล้ว backend สร้าง `LearningSession` ของคนนั้น
   browser เก็บ `learnerKey` ไว้ ทำสองหน้าที่: กลับมาเรียนต่อได้ และแยกคนบนลิงก์เดียวกัน
5. Tutor reducer ขับ intro → readiness → slides → final question → closing
   เริ่มที่ `lastSlideIndex` ที่ join ส่งกลับมา จึงเรียนต่อจากจุดเดิมได้หลังเน็ตหลุด/ปิดแท็บ
6. ทุกครั้งที่เปลี่ยนสไลด์ frontend ยิง progress อัปเดตแถวเดิม (ไม่สร้างแถวใหม่)
7. เมื่อจบ สรุปถูก**คำนวณสดตอนอ่าน** จาก `LearningSession` + `SessionQuestion` — ไม่มีตาราง summary
8. CS ตรวจคำตอบ AI ถูก/ผิด พร้อมหมายเหตุอิสระ (ผู้เรียนไม่เห็นส่วนนี้)

Backend enforce expiry ตอน join แล้ว — แต่จงใจไม่บล็อกคนที่เรียนค้างอยู่ไม่ให้จบหรือดูสรุป
"หยุดกลางคัน" ไม่ใช่สถานะใน DB แต่คำนวณจาก `LastActivityAt` เทียบ `INACTIVE_THRESHOLD_MINUTES`

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

- Teacher/Agent join SignalR group ที่ derive เป็น LearningSession id ฝั่ง server
- `SendChatMessage(token, learnerKey, text)` persist PostgreSQL แล้ว broadcast `ReceiveChatMessage`; server derive role/name
- Push-to-Talk question broadcast `ReceiveNewQuestion`
- REST endpoints ใช้ hydrate history เมื่อเปิดหน้าช้าหรือ reconnect
- Learner derive ชื่อจาก LearningSession; agent derive ชื่อจาก JWT และ client ระบุ role เองไม่ได้

## TTS and Volume

- Backend ใช้ Edge TTS และ chunk ข้อความยาวก่อนรวม audio เพื่อลด timeout
- Frontend เก็บ AI volume ใน browser localStorage และใช้กับ audio ทุกประเภท
- TTS/provider failure เป็น upstream error; tutor พูดข้อความแจ้งใน question failure path
