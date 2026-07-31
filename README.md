# SupportRoom AI — Mock-first Demo

ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (ประสบการณ์คล้าย Video Call กับเจ้าหน้าที่ CS ชื่อ **School Bright Support**)
เฟสนี้เป็น **UI + Mock Data + Mock Logic ล้วน ๆ** — ยังไม่มี Database, ไม่เชื่อม Gemini/Voice API จริง
รายละเอียด Scope และ Business Rules ทั้งหมดอยู่ใน [`AI_Live_Tutor_Demo_Spec.md`](./AI_Live_Tutor_Demo_Spec.md)

## เทคโนโลยี

- Next.js 15 (App Router) + TypeScript (strict) + Tailwind CSS
- React Context/useReducer-style Tutor Engine (ไม่ใช้ external state library)
- `localStorage` ผ่าน Repository Layer (ไม่มี Prisma / ไม่มี Database)
- Browser `MediaDevices` สำหรับ Camera/Microphone Preview

## เริ่มต้นใช้งาน

```bash
npm install
cp .env.example .env.local   # ค่าเริ่มต้นเป็น mock ทั้งหมดอยู่แล้ว
npm run dev
```

เปิด <http://localhost:3000> จะ redirect ไปที่ `/admin` โดยอัตโนมัติ

ตรวจสอบก่อนส่งงาน:

```bash
npm run lint
npm run build
```

## Demo Flow

1. เปิด `/admin` → กด **จัดการบทเรียน Login** เพื่อแก้สคริปต์/สื่อ/Checkpoint/FAQ แล้วกด **บันทึก**
   (Refresh หน้าเว็บ ข้อมูลที่บันทึกจะยังอยู่ เพราะเก็บผ่าน `localStorage`)
2. กลับไป `/admin` → กด **สร้างลิงก์การสอน** กรอกชื่อคุณครู/โรงเรียน (ไม่บังคับ) แล้วกด **สร้างลิงก์การสอน**
3. คัดลอกลิงก์ที่ได้ แล้วเปิดใน **แท็บใหม่ของเบราว์เซอร์เดียวกัน** (ดูข้อจำกัดด้านล่าง)
4. หน้า Pre-join (`/join/[token]`) — ทดสอบเปิด/ปิดกล้องและไมค์ แล้วกด **เข้าร่วมห้องสอน**
5. ห้องสอน (`/room/[token]`) — School Bright Support จะทักทายและเดินสคริปต์ตาม Segment โดยอัตโนมัติ
   สื่อสาธิตจะเปลี่ยนตามจังหวะของสคริปต์ และ AI Tile จะมีกรอบเขียว/Pulse เมื่อกำลัง "พูด"
6. เปิด **Demo Controls** (มุมล่างซ้าย, แสดงเมื่อ `NEXT_PUBLIC_ENABLE_DEMO_CONTROLS=true`) เพื่อจำลอง:
   พร้อมแล้ว, ยังไม่เข้าใจ, ขอดูขั้นตอนก่อนหน้า, ขอพัก/สอนต่อ, คำถามจาก FAQ ตัวอย่าง, เสียงรบกวน, Disconnect
   หรือพิมพ์คำถามเองผ่านปุ่ม **แชต** ในแถบควบคุมด้านล่าง
7. กด **ออกจากห้อง** เพื่อจบ Session (หรือปล่อยให้ Final Q&A เงียบจนจบอัตโนมัติ)
8. กลับไปที่ `/admin` → กด **ดูสรุป** ในแถวของ Session นั้น เพื่อดู Mock Summary (ไม่มีคะแนน)
9. ปุ่ม **Reset Demo Data** ใน `/admin` จะล้างข้อมูลทั้งหมดกลับเป็นค่าเริ่มต้นจาก Seed

## Environment Flags (`.env.local`)

```env
NEXT_PUBLIC_AI_PROVIDER=mock
NEXT_PUBLIC_TTS_PROVIDER=mock
NEXT_PUBLIC_STT_PROVIDER=mock
NEXT_PUBLIC_DATA_PROVIDER=local-storage
NEXT_PUBLIC_ENABLE_DEMO_CONTROLS=true
```

ปิด `NEXT_PUBLIC_ENABLE_DEMO_CONTROLS` เพื่อซ่อน Demo Controls Drawer ในโหมดที่ใกล้ Production มากขึ้น

## สถาปัตยกรรมที่ต่อยอดได้

| ชั้น | Interface | Mock Implementation ปัจจุบัน | จุดต่อของจริงในอนาคต |
|---|---|---|---|
| ข้อมูล | `LessonRepository` / `SessionRepository` / `ReportRepository` (`src/providers/data`) | `LocalStorage*Repository` | `Api*Repository` (placeholder พร้อม throw "not implemented") ต่อกับ Backend + Database |
| คำตอบ AI | `AiAnswerProvider` (`src/providers/ai`) | `MockAiAnswerProvider` (Match Keyword จาก FAQ เท่านั้น) | `GeminiAnswerProvider` (placeholder) |
| เสียงพูด | `TextToSpeechProvider` (`src/providers/tts`) | `MockTextToSpeechProvider` (Timer จำลอง) | `BrowserTextToSpeechProvider` (Web Speech API, ปิดโดย default) / `ExternalTextToSpeechProvider` (placeholder) |
| แปลงเสียงเป็นข้อความ | `SpeechToTextProvider` (`src/providers/stt`) | `MockSpeechToTextProvider` (รับข้อความจาก Chat/Demo Controls) | `ServerSpeechToTextProvider` (placeholder) |

Business Logic ของการสนทนาทั้งหมดอยู่ใน `src/tutor/tutor-reducer.ts` (pure reducer คืนค่า runtime + effect ให้
`src/hooks/use-tutor-session.ts` เป็นผู้ดำเนินการ side effect เช่น เรียก Provider หรือ persist ข้อมูล) แยกขาดจาก
Meeting UI (`src/components/meeting`) โดยสิ้นเชิง — เปลี่ยนหน้าตาห้องสอนได้โดยไม่กระทบ Logic และในทางกลับกัน

ค่าที่ปรับได้ทั้งหมด (ระยะเวลาเงียบ, อายุลิงก์เริ่มต้น ฯลฯ) อยู่ใน `src/config/tutor-config.ts`

## Mock/Placeholder ที่ใช้ในเฟสนี้

- **Media**: ใช้ภาพ SVG จำลองหน้าจอ Login ใน `public/demo-media` (ไม่มีคลิปวิดีโอจริงเนื่องจากข้อจำกัดเครื่องมือสร้างสื่อ
  ในสภาพแวดล้อมนี้ — คอมโพเนนต์ `SharedMedia` รองรับทั้ง Image และ Video ตาม Spec พร้อมใช้งานคลิปจริงได้ทันทีที่เพิ่มไฟล์)
- **TTS**: ไม่มีเสียงจริง ใช้ Timer ตามความยาวข้อความ (หรือ `Segment.mockSpeakDurationMs` ที่กำหนดในบทเรียน)
- **STT**: ไม่ฟังไมโครโฟนจริง รับข้อความจาก Demo Controls/แชตแทน
- **AI**: ตอบจาก Keyword Matching กับ FAQ ที่ตั้งค่าไว้เท่านั้น ไม่มีความรู้ทั่วไป

## Known Limitations

- **Mock Link ใช้ได้เฉพาะ Browser Profile และเครื่องเดียวกัน** เพราะ Session เก็บอยู่ใน `localStorage` ของเบราว์เซอร์
  ที่สร้างลิงก์เท่านั้น การเปิดลิงก์บนอุปกรณ์อื่นหรือเบราว์เซอร์อื่นจะไม่พบ Session — ต้องรอ Backend/Database ในเฟสถัดไป
- รองรับการเรียนครั้งละหนึ่งอุปกรณ์ ไม่มี Multi-device/Realtime Sync
- Resume หลังถูกขัดจังหวะ ทำที่ระดับ Segment (ไม่ใช่ระดับคำในประโยค) ตามที่ระบุไว้ใน Spec
- ไม่มีการให้คะแนนหรือประเมินคุณครู
