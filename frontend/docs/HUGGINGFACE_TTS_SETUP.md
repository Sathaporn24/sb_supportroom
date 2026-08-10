# Hugging Face Text-to-Speech Setup

> สถานะ: **Prepared — Credentials Required** ยังไม่เคยทดสอบกับ Model จริง
> Entry point: `src/providers/tts/huggingface-tts-provider.ts`

## ขั้นตอน

1. สร้างบัญชีที่ [huggingface.co](https://huggingface.co) แล้วสร้าง Access Token
   (Settings → Access Tokens → New Token, สิทธิ์ Read พอสำหรับ Inference API)
2. เลือก Model Text-to-Speech ภาษาไทย — **ยังไม่ได้ทดลองจริงในโปรเจกต์นี้** ให้ประเมิน
   ตาม Checklist ด้านล่างก่อนตัดสินใจ ตัวอย่าง Model ที่พบได้บน Hugging Face Hub ที่ควร
   ลองประเมิน (ตรวจสอบ License และคุณภาพเสียงไทยเองก่อนใช้จริง):
   - Model กลุ่ม MMS-TTS (Meta) ที่รองรับภาษาไทย (`tha`)
   - Model กลุ่ม VITS/Coqui ที่มีคน Fine-tune ภาษาไทยเผยแพร่ไว้
3. ตั้งค่า Environment:
   ```env
   HUGGINGFACE_API_TOKEN=hf_xxxxxxxxxxxx
   HUGGINGFACE_TTS_MODEL=<model id เช่น facebook/mms-tts-tha>
   # ไม่บังคับ - ถ้าไม่ใส่ระบบสร้าง endpoint ให้จาก Model ID อัตโนมัติ
   HUGGINGFACE_TTS_ENDPOINT=
   ```
4. สลับ Provider: `TTS_PROVIDER=huggingface`
5. ทดสอบ `POST /api/tts` ด้วยข้อความสั้น ๆ ภาษาไทย ตรวจสอบ:
   - Status 200 และ `Content-Type` เป็น Audio (เช่น `audio/flac`, `audio/wav`)
   - เล่นไฟล์ที่ได้จริงและฟังว่าออกเสียงภาษาไทยถูกต้อง อ่านเลข/คำทับศัพท์เข้าใจได้
6. **Cold Start / Rate Limit**: Hugging Face Inference API (Serverless) มักมี Cold
   Start หน่วงหลายวินาทีในการเรียกครั้งแรกหลัง Model ไม่ได้ถูกใช้งานพักหนึ่ง และมี Rate
   Limit ตาม Tier บัญชี — ในเชิง Architecture ควรพิจารณา (ยังไม่ Implement ในเฟสนี้):
   - Timeout + Retry ที่ Route Handler (`/api/tts`)
   - แจ้ง Client ว่ากำลังโหลด Model ครั้งแรก (Loading State นานกว่าปกติ)
   - พิจารณา Dedicated Inference Endpoint (จ่ายเงิน) ถ้าต้องการ Latency สม่ำเสมอสำหรับ
     Production จริง
7. **Fallback**: ถ้ายังไม่ได้ตั้ง `HUGGINGFACE_API_TOKEN`/`HUGGINGFACE_TTS_MODEL` และมี
   คนตั้ง `TTS_PROVIDER=huggingface` โดยไม่ได้ตั้ง Credential ระบบจะ throw
   `MissingEnvError` ทันทีตอนเรียก `/api/tts` (ไม่ Fallback เงียบ ๆ ไปที่ Mock —
   ตั้งใจให้ Error ชัดเจนแทนที่จะทำให้ดูเหมือนใช้งานได้ทั้งที่ยังไม่ได้ตั้งค่า)

## Checklist ประเมินคุณภาพเสียงภาษาไทย (ยังไม่ได้ทำ ต้องทำก่อนใช้จริง)

- [ ] อ่านคำทับศัพท์/ภาษาอังกฤษปนไทยได้เข้าใจ (เช่น "Login", "Username")
- [ ] อ่านตัวเลขและวันที่ถูกต้อง
- [ ] จังหวะ/วรรคตอนฟังเป็นธรรมชาติพอสำหรับบทเรียน ไม่เร็ว/ช้าเกินไป
- [ ] Latency จาก Request ถึงได้ไฟล์เสียงกลับ อยู่ในระดับที่ยอมรับได้สำหรับ Demo สด
- [ ] License ของ Model อนุญาตให้ใช้เชิงพาณิชย์ (ถ้าจะใช้ใน Production)
- [ ] ทดสอบกับประโยคจริงจากบทเรียน Login ที่มีอยู่ใน Mock Deck

## Frontend Playback Contract

`src/hooks/use-tutor-session.ts` ควบคุมการเล่นเสียงผ่าน `<audio>` element มาตรฐาน —
ไม่ว่าจะเป็น Mock (WAV เงียบ) หรือ Hugging Face (เสียงจริง) Client ใช้ Code เดียวกันทั้งหมด:
`play()`, `pause()`, อ่าน `duration`/`currentTime`, ฟัง Event `ended` — สลับ Provider
ไม่ต้องแก้ Client เลย
