# Gemini Integration

> สถานะ: **Prepared — Credentials Required** ยังไม่เคยทดสอบกับ Gemini API จริง
> Entry point: `src/providers/voice-question/gemini-voice-question-provider.ts`

## ขั้นตอน

1. สร้าง API Key ที่ [Google AI Studio](https://aistudio.google.com/) หรือผ่าน Google
   Cloud Console (Vertex AI) แล้วแต่ว่าจะใช้ Endpoint ไหน (Provider นี้เขียนไว้สำหรับ
   Generative Language API REST endpoint แบบ AI Studio Key)
2. ตั้งค่า Environment:
   ```env
   GEMINI_API_KEY=xxxxxxxxxxxx
   GEMINI_MODEL=gemini-1.5-flash
   ```
3. สลับ Provider: `VOICE_QUESTION_PROVIDER=gemini`

## Flow การส่งเสียง

`POST /api/voice-question` → โหลด Speaker Notes ทุก Slide → เรียก
`GeminiVoiceQuestionProvider.transcribeAndAnswer` → ส่ง Request ไปที่
`https://generativelanguage.googleapis.com/v1beta/models/<model>:generateContent`
พร้อม 2 Parts ใน Content เดียว:

1. Prompt ข้อความ (บังคับ Persona + Grounding Rule + Speaker Notes ทั้งหมด + Schema)
2. `inline_data` เสียงคำถาม (`mime_type` ตาม MediaRecorder ของ Browser, เนื้อไฟล์เป็น
   Base64)

## Prompt / Response Schema

Prompt เต็มอยู่ใน `buildPrompt()` ของไฟล์ Provider สรุปกติกา:

- ถอดเสียงเป็นข้อความภาษาไทย
- ตอบจาก Speaker Notes ที่แนบมาเท่านั้น ห้ามใช้ความรู้ทั่วไป ห้ามเดา
- บังคับตอบกลับเป็น JSON (`generationConfig.responseMimeType = "application/json"`)
  ตาม Schema:
  ```json
  {
    "transcript": "string",
    "answer": "string",
    "answerStatus": "answered | not_found | out_of_scope | transcription_failed",
    "relatedSlideObjectId": "string | null"
  }
  ```

Provider ฝั่งเราตรวจสอบว่า `answerStatus` เป็นหนึ่งใน 4 ค่าที่กำหนด (บวก `no_speech`
ที่ตัดสินใจฝั่ง Server ก่อนเรียก Gemini ด้วยความยาวการอัดเสียง) ถ้า Parse JSON ไม่ได้หรือ
`answerStatus` ไม่ตรง Schema จะถือเป็น `transcription_failed` โดยอัตโนมัติ (Fail-safe)

## Safety / Fallback

- ถ้าไม่ได้ตั้ง `GEMINI_API_KEY` แล้วเลือก `VOICE_QUESTION_PROVIDER=gemini` จะได้
  `MissingEnvError` ทันทีตอนเรียก `/api/voice-question` (ไม่ Fallback เงียบไปที่ Mock)
- Request ที่ Gemini ตอบ Error (`!response.ok`) → Provider throw Error พร้อมข้อความสั้น ๆ
  → Route Handler คืน `502 UPSTREAM_ERROR` ให้ Client (ไม่ใช่ 500 เพราะไม่ใช่ Bug ของเรา)
- **ห้าม Log เสียง Audio หรือ Transcript เกินความจำเป็น** — โค้ดปัจจุบันไม่มีการ
  `console.log` เนื้อหา Transcript/Answer เต็มที่จุดใดเลย มีแค่ Error Message สั้น ๆ เวลา
  Fail

## วิธีทดสอบ Transcript และ Answer Status

1. ตั้งค่า Credential ตามด้านบน
2. เข้าห้องสอนจริง (`/room/[token]`) กดค้างปุ่มไมค์แล้วถามคำถามที่ตรงกับเนื้อหาใน
   Speaker Notes ของบทเรียนนั้น → ควรได้ `answerStatus: "answered"` และคำตอบตรงเนื้อหา
3. ถามคำถามที่ไม่มีในเนื้อหาเลย → ควรได้ `not_found`
4. ถามคำถามนอกเรื่องระบบทั้งหมด (เช่นถามเรื่องดินฟ้าอากาศ) → ควรได้ `out_of_scope`
5. ปล่อยให้เงียบ/พึมพำสั้น ๆ ต่ำกว่า `MIN_VOICE_DURATION_MS` → ควรได้ `no_speech`
   (กรณีนี้ไม่เรียก Gemini เลย ตัดสินใจได้จาก Duration ฝั่ง Server ก่อน)
