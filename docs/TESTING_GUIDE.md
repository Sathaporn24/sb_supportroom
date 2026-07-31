# Testing Guide

## Automated

```bash
npm run lint        # ESLint
npm run typecheck   # tsc --noEmit
npm run test         # Vitest (unit tests, Node environment)
npm run build        # Next.js production build (รวม type-check ของ Next เอง)
```

Unit Tests อยู่ใน `src/**/*.test.ts` (co-located กับไฟล์ที่ทดสอบ) ครอบคลุมตามที่ Prompt
ข้อ 16 กำหนด:

| หัวข้อจาก Prompt | ไฟล์ Test |
|---|---|
| Parse Google Slides Source URL | `src/utils/google-slides-url.test.ts` |
| Parse/validate Published Embed URL | เดียวกัน |
| Provider Factory เลือก Mock เป็น Default | `src/providers/provider-factories.test.ts` |
| Missing Environment Variable ให้ Error ถูกต้อง | `src/config/env.test.ts` |
| Slide duration = `Math.max(ttsDuration, videoDuration)` | `src/tutor/tutor-reducer.test.ts` |
| Tutor State transition ของ Push-to-Talk | เดียวกัน |
| No-speech/failed transcription กลับไปสอนโดยไม่พูดเพิ่ม | เดียวกัน |
| Video slide restart หลังตอบคำถาม | เดียวกัน |
| Session expiry default 24 ชั่วโมง | `src/config/server-defaults.test.ts` |
| Session summary แยก `completedAllSlides` ถูกต้อง | `src/tutor/tutor-reducer.test.ts` (`END_SESSION`/`FINISH_SESSION` describe block) |

รวม MockVoiceQuestionProvider grounding behavior เพิ่มเติมที่
`src/providers/voice-question/mock-voice-question-provider.test.ts`

**หมายเหตุ**: `vitest.config.mts` Alias แพ็กเกจ `server-only` ไปที่ Stub เปล่า
(`src/test/server-only-shim.ts`) เพราะ Vitest รันบน Node ตรง ๆ ไม่ผ่าน Next.js
Webpack ที่ทำให้ `server-only` เป็น No-op ได้ตามปกติ — ถ้าไม่ Alias ทุก Test ที่ import
ไฟล์ฝั่ง Server (Provider/Repository/Config) จะ throw ทันที

## Manual Flow (Mock Mode)

ทำตามลำดับนี้เพื่อยืนยันว่า Mock Mode รันได้ครบ Flow โดยไม่มี Credential ใด ๆ:

1. `npm run dev` โดยไม่มี `.env.local` (หรือมีแต่ทุก Provider เป็น mock)
2. เปิด `/admin` → กด "จัดการบทเรียน" → เปิดบทเรียน `login-mobile`
3. กด "ตรวจสอบ/Sync Slides" → ควรเห็นรายการ 6 Slide พร้อม Speaker Notes (Mock Deck)
4. ติ๊ก "เปิดใช้งานบทเรียนนี้" → บันทึก
5. กลับ `/admin` → "สร้างลิงก์การสอน" → เลือก "วิธีการ Login (mobile)" (พร้อมใช้งาน)
   → กรอกข้อมูล (ไม่บังคับ) → สร้างลิงก์ → คัดลอก
6. เปิดลิงก์ในแท็บ/เบราว์เซอร์ใหม่ → หน้า Pre-join → อนุญาตกล้อง/ไมค์ (Mock Chromium
   ใช้ `--use-fake-device-for-media-stream` ได้) → กด "เข้าร่วมห้องสอน"
7. ในห้องสอน: ตรวจว่า AI ทักทาย → รอ/กด "พร้อมแล้ว เริ่มเรียนเลย" → Slide เดินอัตโนมัติ
   ทีละสไลด์พร้อมเสียง Mock TTS
8. กดค้างปุ่มไมค์ (Push-to-Talk) พูดอะไรก็ได้ที่ยาวพอ (Mock Mode ใช้ Transcript ตัวอย่าง
   คงที่เสมอ) → ปล่อยปุ่ม → ควรได้ยินคำตอบแล้วสไลด์ปัจจุบัน Restart
9. กดค้างปุ่มไมค์แล้วปล่อยทันที (สั้นกว่า `MIN_VOICE_DURATION_MS`) → ควรกลับไปสอนต่อ
   เงียบ ๆ โดยไม่มีคำพูดเพิ่ม (ไม่มีการเรียก `/api/voice-question` เลย — เช็คได้จาก
   Network Tab)
10. ปล่อยให้เดินจนจบทุก Slide → ฟังคำถามท้ายบทเรียน → ปล่อยให้เงียบจนหมดเวลา → ควรได้ยิน
    คำกล่าวลาแล้วเด้งไปหน้า "ขอบคุณค่ะ"
11. กลับ `/admin` → กด "ดูสรุป" ที่แถว Session นั้น → ตรวจว่า `สอนครบทุก Slide = ครบ`
    และเห็นคำถามที่ถามไว้
12. `/admin` → กด "Reset Demo Data" → ตรวจว่ารายการ Session ว่างและบทเรียนกลับเป็น Seed

หาก Real Provider ยังไม่มี Key ให้ทดสอบเฉพาะ Environment Validation
(`getXxxEnv()` throw `MissingEnvError` ถูกต้อง) และ Mocked API Contract แทนขั้นตอนที่
ต้องมี Credential จริง
