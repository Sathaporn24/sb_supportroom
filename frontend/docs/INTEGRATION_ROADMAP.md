# Integration Roadmap

ลำดับแนะนำสำหรับทีมที่รับงานต่อ เรียงตามคุณค่าที่ได้และความเสี่ยงจากน้อยไปมาก

## 1. Supabase (แนะนำให้ทำก่อน)

**ทำไมก่อน**: ความเสี่ยงต่ำที่สุด (Schema พร้อมแล้ว, ไม่กระทบ UX ที่เห็น), ทำให้ Session
ไม่หายเมื่อ Restart Server หรือ Deploy ใหม่ — ปลดล็อกการทดสอบ Integration อื่น ๆ ได้นิ่งขึ้น

**งานที่เหลือ**: สร้าง Project จริง → รัน Migration → ตั้ง Env → ทดสอบตาม
[SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md) → พิจารณา Retry/Connection
Pooling ถ้า Deploy แบบ Serverless (Supabase JS Client ควร reuse connection ข้าม Request
ให้ดี ตรวจสอบ Cold Start บน Platform ที่เลือก)

## 2. Google Slides

**ทำไมถัดมา**: กระทบ UX ตรง ๆ (เนื้อหาสอนจริงแทน Mock Deck) แต่ความเสี่ยงทางเทคนิคอยู่ที่
การจัดการสิทธิ์ Service Account และ URL Parsing เท่านั้น ไม่มี Cost ต่อ Request สูง

**งานที่เหลือ**: สร้าง Service Account → แชร์ไฟล์จริง → ทดสอบตาม
[GOOGLE_SLIDES_SETUP.md](./GOOGLE_SLIDES_SETUP.md) → ทดสอบ Cross-origin iframe
Behavior จริงบน Browser ที่จะใช้งาน (Safari/iOS อาจมีข้อจำกัด Autoplay/Sandbox ต่างจาก
Chrome ควรทดสอบก่อน Production)

## 3. Hugging Face TTS

**ทำไมถัดมา**: ยังไม่ได้เลือก Model ภาษาไทยที่ยืนยันคุณภาพแล้ว ต้องใช้เวลาประเมิน
(Checklist ใน [HUGGINGFACE_TTS_SETUP.md](./HUGGINGFACE_TTS_SETUP.md)) ก่อนตัดสินใจ Model
สุดท้าย และต้องออกแบบรับมือ Cold Start/Rate Limit เพิ่มจากโค้ดปัจจุบัน (ยังไม่มี
Retry/Timeout Wrapper)

**งานที่เหลือ**: ประเมิน Model ≥ 2-3 ตัวตาม Checklist → เลือกและตั้งค่า → เพิ่ม
Retry/Timeout ที่ `/api/tts` ถ้าจำเป็น → พิจารณา Cache เสียงที่ Synthesize ซ้ำบ่อย (เช่น
Greeting/Closing ที่เนื้อหาเดิมทุกครั้ง) เพื่อลด Latency/Cost

## 4. Gemini (แนะนำให้ทำหลังสุด)

**ทำไมหลังสุด**: ซับซ้อนที่สุด (Audio Input + Grounded JSON Output + Safety) และพึ่งพา
เนื้อหาจริงจาก Google Slides ให้พร้อมก่อนถึงจะทดสอบ Grounding ได้อย่างมีความหมาย

**งานที่เหลือ**: ตั้งค่า API Key → ทดสอบ 4 สถานการณ์ตาม
[GEMINI_INTEGRATION.md](./GEMINI_INTEGRATION.md) (answered/not_found/out_of_scope/
no_speech) → พิจารณา Timeout ที่เหมาะสมสำหรับ Audio Request (อาจช้ากว่า Text-only) →
ทดสอบกับสำเนียง/ความเร็วพูดหลากหลายของครูจริง ก่อนใช้ในสถานการณ์จริง

## งานข้ามทั้ง 4 Integration (ทำเมื่อพร้อม Production)

- Rate Limiting ที่ Route Handler ระดับ IP/Session (ยังไม่มีในเฟสนี้)
- Authentication สำหรับ CS (`/admin/**`) — นอก Scope ที่ระบุไว้ชัดเจนในเฟสนี้ แต่จำเป็น
  ก่อนเปิดใช้งานจริงกับ CS หลายคน
- Multi-device Sync ถ้าต้องการให้ 1 Session เปิดพร้อมกันได้หลายอุปกรณ์ (นอก Scope ปัจจุบัน)
- Observability: Log/Metric แยกตาม Provider เพื่อ Debug ปัญหา Production (ปัจจุบันมีแค่
  `console`/Error Message สั้น ๆ)
