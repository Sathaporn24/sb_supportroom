# API Integration Guide (Quick Reference)

สรุปสั้น ๆ วิธีเปิดใช้งานแต่ละ Integration จริง — รายละเอียดเต็มอยู่ในเอกสารเฉพาะที่ลิงก์ไว้

| ต้องการเปิด | ตั้งค่า Env | เอกสารละเอียด |
|---|---|---|
| Google Slides จริง | `SLIDES_PROVIDER=google` + `GOOGLE_SERVICE_ACCOUNT_*` | [GOOGLE_SLIDES_SETUP.md](./GOOGLE_SLIDES_SETUP.md) |
| เสียงพูดจริง (Hugging Face) | `TTS_PROVIDER=huggingface` + `HUGGINGFACE_*` | [HUGGINGFACE_TTS_SETUP.md](./HUGGINGFACE_TTS_SETUP.md) |
| ถอดเสียง + ตอบคำถามจริง (Gemini) | `VOICE_QUESTION_PROVIDER=gemini` + `GEMINI_*` | [GEMINI_INTEGRATION.md](./GEMINI_INTEGRATION.md) |
| ฐานข้อมูลจริง (Supabase) | `DATA_PROVIDER=supabase` + `SUPABASE_*` + รัน Migration | [SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md) |

## ทั้ง 4 Integration เป็นอิสระต่อกัน

เปิดทีละตัวได้ตามลำดับความพร้อม ไม่ต้องเปิดพร้อมกันทั้งหมด เช่น:

- เปิดแค่ Supabase อย่างเดียว → Config/Session ถาวรข้ามเครื่อง แต่เนื้อหายังเป็น Mock
  Deck, เสียงยังเป็น Mock TTS
- เปิดแค่ Google Slides → เนื้อหาสอนเป็นสไลด์จริง แต่เสียงยังเป็น Mock (WAV เงียบ) และ
  Session ยัง In-memory

## จุดตัดสินใจ Provider (Composition Root)

แก้ค่าเดียวใน `.env.local` แล้ว Restart Dev Server — ไม่ต้องแก้โค้ด UI หรือ Route
Handler เลยแม้แต่บรรทัดเดียว เพราะทุก Route Handler เรียกผ่าน Factory:

```ts
// src/providers/slides/index.ts
export function createSlidesContentProvider(): SlidesContentProvider {
  switch (getProviderSelection().SLIDES_PROVIDER) {
    case "google": return new GoogleSlidesContentProvider();
    default: return new MockSlidesContentProvider();
  }
}
```

รูปแบบเดียวกันซ้ำใน `src/providers/tts/index.ts`, `src/providers/voice-question/index.ts`,
`src/providers/data/index.ts`

## ลำดับแนะนำสำหรับทีม Backend

ดู [INTEGRATION_ROADMAP.md](./INTEGRATION_ROADMAP.md) สำหรับลำดับความสำคัญและ Checklist
ก่อน Production
