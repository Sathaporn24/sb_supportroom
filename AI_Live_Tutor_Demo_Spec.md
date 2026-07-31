# SupportRoom AI — Mock-first Demo Specification

> ชื่อโปรเจกต์ชั่วคราว: **SupportRoom AI** (ชื่อ Repository จริง: `sb_supportroom`)
>
> **⚠️ สถานะเอกสาร: Superseded (เอกสาร Phase 1 — เก็บไว้เป็นบริบทประวัติศาสตร์)**
> Product Logic ปัจจุบันเปลี่ยนไปมากจากที่เอกสารนี้อธิบาย (เนื้อหาสอนย้ายจาก Mock Lesson
> Editor แบบ Step/Segment ไปเป็น **Google Slides**, Interruption Logic เปลี่ยนจาก
> Checkpoint/FAQ-keyword ไปเป็น **Push-to-Talk + Grounded Q&A**, และเพิ่ม Backend จริง
> ผ่าน Next.js Route Handlers) **อย่ายึดเอกสารนี้เป็น Source of Truth ของสถาปัตยกรรมหรือ
> Business Logic ปัจจุบันอีกต่อไป** ให้ใช้:
>
> - [`docs/SYSTEM_LOGIC.md`](./docs/SYSTEM_LOGIC.md) — Business Logic ปัจจุบัน
> - [`docs/SYSTEM_ARCHITECTURE.md`](./docs/SYSTEM_ARCHITECTURE.md) — สถาปัตยกรรมปัจจุบัน
> - [`docs/STATE_MACHINE.md`](./docs/STATE_MACHINE.md) — Tutor Engine State Machine ปัจจุบัน
> - [`CLAUDE.md`](./CLAUDE.md) — จุดเริ่มต้นสำหรับนักพัฒนา/Agent
>
> เอกสารนี้ยังคงมีประโยชน์สำหรับ: หลักการ Conversation-first/Meeting-like UX ที่ยังใช้อยู่,
> เหตุผลเบื้องหลังการออกแบบ Mock-first แต่แรก, และ UI/UX Convention ภาษาไทยที่ยังใช้
> (เช่น การเลี่ยงคำว่า "เริ่มบทเรียน", "บทที่", "คะแนน")
>
> Technology หลัก: **Next.js App Router + TypeScript + Tailwind CSS** (ยังใช้จริง —
> เพิ่มเติมคือตอนนี้ Next.js เป็น Backend ด้วยผ่าน Route Handlers)
>
> Phase ที่เอกสารนี้อธิบาย: **UI + Mock Data + Mock Logic** (Phase 1 — เสร็จสมบูรณ์แล้ว
> ก่อนถูก Superseded โดย Phase 2)

---

## Implementation Status (อัปเดตล่าสุด ณ Phase 2)

ตารางนี้บอกสถานะ **ปัจจุบันจริง** ของแต่ละองค์ประกอบ ตรงกับโค้ดใน Branch นี้ ไม่ใช่สถานะ
ตอนเขียนเอกสาร Phase 1 ด้านล่างอีกต่อไป — ใช้ตารางนี้แทนเนื้อหา Phase 1 ที่เหลือของไฟล์นี้
เมื่อต้องการทราบว่า "ตอนนี้ทำอะไรไปแล้วบ้าง"

| องค์ประกอบ | สถานะ | หมายเหตุ |
|---|---|---|
| CS Dashboard | Completed | `/admin`, `/admin/lessons`, `/admin/lessons/[slug]`, `/admin/sessions/new`, `/admin/sessions/[token]` — ไม่มี Auth ตามที่ตั้งใจ |
| Google Slides Config UI | Completed | แทนที่ Step/Segment Editor เดิมทั้งหมด |
| Slides Sync | Mock Only (Real: Prepared) | `MockSlidesContentProvider` มี Deck จำลอง 6 Slide, `GoogleSlidesContentProvider` เขียนแล้วยังไม่ทดสอบกับ Credential จริง |
| Shared Screen Embed | Completed (Mock placeholder เมื่อไม่มี Embed URL จริง) | `SlidesEmbed` component, ใช้ `#slide=id.<objectId>` fragment + Force reload |
| Speaker Notes Parsing | Mock Only (Real: Prepared) | ดูแถว Slides Sync |
| Tutor Engine | Completed | State Machine 14 states ใหม่ทั้งหมด แทน Checkpoint-based เดิม — ดู `docs/STATE_MACHINE.md` |
| Push-to-Talk | Completed | แทน Demo Controls/FAQ-keyword เดิมทั้งหมด — ไม่มี VAD |
| Gemini Voice Question | Mock Only (Real: Prepared — Credentials Required) | Mock กราวด์กับ Speaker Notes จริง, Transcript เป็นค่าคงที่ |
| Hugging Face TTS | Mock Only (Real: Prepared — Credentials Required) | Mock สร้าง Silent WAV ตามความยาวข้อความจริง |
| Supabase | Mock Only (Real: Prepared — Credentials Required, Migration ยังไม่ Apply) | In-memory Mock Repository เป็น Default |
| Session History | Completed (Mock/Supabase-ready) | `training_sessions`, `session_questions` |
| Session Summary | Completed (Mock/Supabase-ready) | `session_results`, ไม่มีคะแนน |
| Responsive UI | Completed | Desktop + Mobile |
| Camera/Microphone | Completed | Camera เป็น Preview เท่านั้น (ไม่วิเคราะห์ภาพ), Microphone ใช้กับ Push-to-Talk |
| Authentication | Not Included | ตามที่ Prompt Phase 2 ระบุว่ายังไม่ต้องทำ |
| Multi-device Sync | Not Included | ตามที่ Prompt Phase 2 ระบุว่ายังไม่ต้องทำ |

ดูรายละเอียดสถานะ Integration ภายนอกทั้ง 4 ตัวแบบเต็มใน [`docs/BACKEND_HANDOFF.md`](./docs/BACKEND_HANDOFF.md)

---

## เนื้อหาด้านล่างนี้คือเอกสาร Phase 1 ฉบับเดิม (เก็บไว้อ้างอิงประวัติศาสตร์)

## 1. แนวคิดของระบบ

SupportRoom AI คือ **ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ** ซึ่งให้ประสบการณ์ใกล้เคียงการเข้าประชุมออนไลน์กับเจ้าหน้าที่ CS

CS สร้างลิงก์เฉพาะ Session แล้วส่งให้คุณครูเหมือนส่งลิงก์ Meeting เมื่อคุณครูเข้าห้อง จะพบผู้สอนในชื่อกลาง เช่น **School Bright Support** ซึ่งสอนตามสคริปต์มาตรฐาน รับคำถาม และแสดงสื่อสาธิตตามจังหวะของเนื้อหา

ระบบนี้ **ไม่ใช่เว็บเปิดวิดีโอสอน** ภาพและคลิปเป็นเพียงสื่อที่ผู้สอนแชร์ประกอบ คุณครูไม่จำเป็นต้องรู้ว่าสิ่งที่กำลังเห็นเป็นภาพนิ่งหรือคลิป

### หลักการสำคัญ

1. **Conversation-first** — การพูดคุยเป็นแกนหลัก สื่อเป็นสิ่งประกอบ
2. **Meeting-like UX** — หน้าตาและคำเรียกใกล้เคียงห้องประชุม ไม่เหมือน LMS, Course หรือ Chatbot
3. **Script-controlled** — ช่วงสอนหลักใช้สคริปต์มาตรฐานที่กำหนดไว้
4. **Grounded answers** — คำตอบต้องอิงสคริปต์และ FAQ ที่เตรียมไว้ ไม่เดา
5. **Mock-first** — ทำ UI และ Flow ให้เดินครบก่อน โดยยังไม่ผูกกับ AI, Voice หรือ Database จริง
6. **Replaceable architecture** — Mock ทุกส่วนต้องเปลี่ยนเป็น Provider หรือ Repository จริงได้โดยไม่รื้อ UI และ Tutor Logic

---

## 2. เป้าหมายของ Phase ปัจจุบัน

เป้าหมายของ Phase นี้คือสร้าง Demo ที่แสดงประสบการณ์และ Logic หลักได้ครบ โดยใช้ข้อมูลและเหตุการณ์จำลอง

```text
CS เปิด Dashboard
→ แก้ไขบทเรียน Login จาก Mock Data
→ สร้าง Mock Session Link
→ เปิดลิงก์ใน Browser เดียวกัน
→ ผ่านหน้าเตรียมเข้าห้อง
→ เข้าห้องที่มี UX คล้าย Video Call
→ ผู้สอนจำลองการพูดตามสคริปต์
→ สื่อเปลี่ยนตามช่วงของสคริปต์
→ ใช้ Demo Controls หรือ Chat จำลองคำถามและการพูดแทรก
→ Tutor Engine ตอบและกลับไปสอนจากจุดเดิม
→ Session จบ
→ CS เปิดดู Mock Summary
```

### เป้าหมายเชิงสถาปัตยกรรม

หลังจบ Phase นี้ ต้องสามารถต่อยอดได้ตามลำดับดังนี้โดยไม่รื้อหน้าจอหลัก:

1. ต่อ Text-to-Speech จริง
2. ต่อ Speech-to-Text จริง
3. ต่อ Gemini สำหรับ Intent และ Grounded Q&A
4. เลือกและต่อ Database ภายหลัง
5. เพิ่ม Backend/API และรองรับการส่งลิงก์ข้ามอุปกรณ์จริง

---

## 3. สิ่งที่ต้องทำใน Mock Demo

### 3.1 UI และ Flow

- หน้า CS Dashboard
- หน้าแก้ไขบทเรียน Login
- หน้าสร้าง Session Link
- รายการ Mock Session แบบเรียบง่าย
- หน้า Pre-join สำหรับคุณครู
- ขอ Permission และ Preview ไมโครโฟน/กล้องจาก Browser
- ห้องสอน Responsive ที่มีบรรยากาศเหมือน Video Call
- Shared Screen เป็นพื้นที่หลัก
- AI Tile และ Teacher Tile
- กรอบสีเขียวหรือ Pulse Animation เมื่อผู้พูดกำลังพูด
- ปุ่มเปิด–ปิดไมค์ กล้อง แชต และออกจากห้อง
- ช่องแชตสำรองที่ซ่อนไว้ก่อน
- หน้าขอบคุณเมื่อ Session จบ
- หน้าลิงก์หมดอายุหรือใช้งานแล้ว

### 3.2 Mock Logic

- Tutor Engine เดินสคริปต์ทีละ Segment
- สื่อเปลี่ยนตาม Segment ที่กำหนดไว้
- จุด Checkpoint เฉพาะ Step สำคัญ
- Auto-continue เมื่อผู้ใช้เงียบ
- จำลองการพูดแทรก
- จำลองเสียงรบกวนหรือคำพูดที่ไม่มีความหมาย
- ตอบคำถามจาก Mock FAQ
- คำถามนอกขอบเขตใช้ข้อความมาตรฐาน
- ตอบคำถามแล้วกลับไปสอนจากจุดเดิม
- ทบทวนขั้นตอนก่อนหน้าแบบชั่วคราว
- Pause และ Resume
- สรุปผล Session แบบไม่มีคะแนน

### 3.3 Mock Persistence

Phase นี้ยังไม่มี Database ให้ใช้:

- Mock Data ใน TypeScript เป็นค่าเริ่มต้น
- `localStorage` สำหรับเก็บการแก้บทเรียน, Session และ Summary เพื่อให้ Refresh แล้วข้อมูลยังอยู่
- `sessionStorage` ใช้ได้เฉพาะ Runtime State ที่ไม่จำเป็นต้องคงถาวร
- มีปุ่ม Reset Demo Data กลับเป็นค่าเริ่มต้น
- UI ห้ามอ่าน `localStorage` โดยตรง ต้องผ่าน Repository Interface

---

## 4. สิ่งที่ยังไม่ทำใน Phase นี้

- Database ทุกชนิด
- Prisma ORM
- SQLite, PostgreSQL, MySQL, Firebase หรือ Supabase
- Backend Persistence จริง
- Authentication สำหรับ CS
- Gemini API หรือ Gemini Live API จริง
- Speech-to-Text จริง
- Voice AI หรือ Text-to-Speech API จริง
- Voice Activity Detection ขั้นสูง
- Barge-in จากเสียงไมโครโฟนจริง
- Avatar ขยับปากหรือ Lip Sync
- วิเคราะห์ใบหน้า สีหน้า หรือภาพจากกล้อง
- การอัปโหลดสื่อไป Cloud Storage
- การส่งลิงก์ผ่าน LINE หรือ Email
- Notification
- คะแนนหรือการประเมินคุณครู
- Multi-device synchronization
- การใช้งาน Session Link ข้าม Browser หรือข้ามอุปกรณ์จริง
- Zoom, Google Meet หรือ Meeting SDK ภายนอก
- ระบบ Versioning เต็มรูปแบบ
- การสร้างบทเรียนหลายประเภท

---

## 5. ข้อจำกัดของ Mock Session Link

ใน Phase นี้ Session ถูกเก็บใน `localStorage` ดังนั้น:

- ลิงก์ทำงานภายใน Browser Profile และ Origin เดียวกัน
- สามารถเปิดอีก Tab หรืออีก Window ใน Browser เดียวกันได้
- การคัดลอกลิงก์ไปเปิดบนอุปกรณ์อื่นยังไม่ทำงาน เพราะยังไม่มี Backend/Database
- ข้อจำกัดนี้ต้องระบุใน README และแสดงข้อความเล็ก ๆ ใน Admin ว่าเป็น Mock Link

เมื่อเชื่อม Database และ Backend แล้ว Route และ UI เดิมต้องสามารถใช้ต่อได้ โดยเปลี่ยนเฉพาะ Repository Implementation

---

## 6. ผู้ใช้งาน

### 6.1 CS / ผู้ดูแล

ไม่ต้อง Login ใน Mock Demo และสามารถ:

- เปิด Dashboard
- แก้ไขบทเรียน Login ที่มีอยู่แล้ว
- แก้สคริปต์การพูด
- จัดลำดับ Step และ Segment ด้วยปุ่มขึ้น/ลง
- เลือกสื่อจาก Demo Media ที่เตรียมไว้
- เปิด/ปิด Checkpoint
- เลือกคำถาม Checkpoint สำเร็จรูป
- แก้ FAQ และคำตอบมาตรฐาน
- สร้าง Mock Session Link
- คัดลอกลิงก์
- ดูรายการ Session
- ดู Summary หลังจบ
- Reset Mock Data

### 6.2 คุณครู

- เปิดลิงก์เฉพาะ Session โดยไม่ใช้ PIN หรือ OTP
- ตรวจสอบหัวข้อก่อนเข้าห้อง
- ทดสอบไมโครโฟน
- เปิดกล้องได้แต่ไม่บังคับ
- เข้าฟังการสาธิต
- เปิด–ปิดไมค์และกล้อง
- เปิดช่องแชตสำรอง
- ใช้ Demo Controls เพื่อจำลองคำพูดในช่วงนำเสนอ
- ขอทบทวน ขอพัก หรือถามคำถามได้
- ออกจากห้องได้

---

## 7. ขอบเขตบทเรียน Demo

มีบทเรียนเดียว:

- Code: `LOGIN_APP`
- ชื่อ: `การเข้าสู่ระบบสำหรับคุณครู`
- ภาษา: ไทย

โครงสร้าง:

```text
Lesson
└── Step
    ├── Segment 1: Script + Media
    ├── Segment 2: Script + Media
    └── Checkpoint: เปิดหรือปิด
```

### Step

- `id`
- `title`
- `order`
- `checkpointEnabled`
- `checkpointPromptId`
- `segments`

### Segment

- `id`
- `order`
- `scriptText`
- `mediaId`
- `mockSpeakDurationMs`

### FAQ

- `id`
- `question`
- `keywords`
- `answer`
- `scope`
- `relatedMediaId` แบบ Optional
- `active`

---

## 8. Seed Content สำหรับบทเรียน Login

### Step 1 — แนะนำหน้าเข้าสู่ระบบ

**Segment 1**

- Script: `ตอนนี้คุณครูอยู่ที่หน้าเข้าสู่ระบบนะคะ ก่อนเริ่มใช้งาน ให้ตรวจสอบว่ากำลังเข้าเว็บไซต์ของระบบถูกต้องค่ะ`
- Media: หน้า Login แบบเต็มหน้า

**Segment 2**

- Script: `ด้านบนของหน้าจอจะเป็นส่วนสำหรับเลือกโรงเรียนของคุณครูค่ะ`
- Media: ไฮไลต์ช่องโรงเรียน

Checkpoint: เปิด

### Step 2 — กรอกชื่อผู้ใช้งาน

**Segment 1**

- Script: `ช่องแรกใช้สำหรับกรอกชื่อผู้ใช้งานที่โรงเรียนกำหนดให้นะคะ`
- Media: ไฮไลต์ช่อง Username

**Segment 2**

- Script: `ข้อมูลในช่องนี้อาจเป็นชื่อผู้ใช้งานหรือเบอร์โทรศัพท์ ขึ้นอยู่กับข้อมูลที่โรงเรียนตั้งค่าไว้ค่ะ`
- Media: ตัวอย่างการกรอก Username

Checkpoint: เปิด

### Step 3 — กรอกรหัสผ่าน

**Segment 1**

- Script: `จากนั้นกรอกรหัสผ่านในช่องถัดไป โดยตรวจสอบตัวพิมพ์เล็ก ตัวพิมพ์ใหญ่ และภาษาของแป้นพิมพ์ให้ถูกต้องค่ะ`
- Media: ไฮไลต์ช่อง Password

Checkpoint: เปิด

### Step 4 — เข้าสู่ระบบและกรณีไม่สำเร็จ

**Segment 1**

- Script: `เมื่อข้อมูลครบแล้ว ให้กดปุ่มเข้าสู่ระบบค่ะ`
- Media: สาธิตการกด Login

**Segment 2**

- Script: `หากเข้าสู่ระบบไม่สำเร็จ ให้ตรวจสอบชื่อผู้ใช้งานและรหัสผ่านอีกครั้ง หากยังใช้งานไม่ได้ ให้ติดต่อผู้ดูแลระบบของโรงเรียนค่ะ`
- Media: ตัวอย่าง Login Error

Checkpoint: เปิด

### Step 5 — สรุป

**Segment 1**

- Script: `สรุปแล้ว คุณครูต้องเลือกโรงเรียน กรอกชื่อผู้ใช้งาน กรอกรหัสผ่าน และกดเข้าสู่ระบบค่ะ`
- Media: ภาพสรุปหน้า Login

Checkpoint: ปิด

---

## 9. กติกาการตอบคำถาม

| กลุ่ม | พฤติกรรม |
|---|---|
| อยู่ในบทเรียน Login | ตอบสั้น กระชับ จาก FAQ หรือสคริปต์ แล้วกลับไปจุดเดิม |
| อยู่ในระบบเดียวกันแต่ไม่ใช่หัวข้อ Session | ตอบข้อมูลพื้นฐาน หากต้องสอนขั้นตอนละเอียดให้แนะนำติดต่อ CS |
| นอกเรื่องระบบ | ปฏิเสธสั้น ๆ แล้วกลับเข้าสู่การสอน |
| ไม่พบข้อมูลหรือไม่มั่นใจ | แจ้งว่าไม่มีข้อมูลยืนยัน และบันทึกไว้ใน Summary |

ข้อความ Mock สำหรับคำถามนอกเรื่อง:

> ขออภัยค่ะ เรื่องนี้อยู่นอกขอบเขตการสอนระบบ ตอนนี้ขออนุญาตกลับไปที่หัวข้อเดิมนะคะ

ข้อความ Mock เมื่อไม่พบข้อมูล:

> ขออภัยค่ะ ยังไม่มีข้อมูลที่ยืนยันได้สำหรับคำถามนี้ ระบบจะบันทึกไว้ให้ทีม CS ตรวจสอบค่ะ

---

## 10. Logic การสนทนา

### 10.1 เริ่มต้น

1. ผู้สอนทักทาย
2. ถ้ามีชื่อคุณครูให้ใช้ชื่อ หากไม่มีใช้ `สวัสดีค่ะคุณครู`
3. ถาม `พร้อมเริ่มหรือยังคะ?`
4. ถ้าตอบว่าพร้อม ให้เริ่มทันที
5. ถ้าเงียบเกินค่าที่กำหนด ให้เริ่มอัตโนมัติ
6. ถ้าบอกให้รอหรือขอพัก ให้เข้าสถานะ Pause

### 10.2 ช่วงสอนหลัก

- ใช้ Script ที่กำหนดไว้ตรงตาม Segment
- ใช้ Mock TTS Timer เพื่อจำลองระยะเวลาพูด
- AI Tile แสดง Speaking Animation ระหว่าง Timer ทำงาน
- Media เปลี่ยนตาม Segment ที่ผูกไว้
- ห้ามให้ Mock AI แก้หรือแต่งสคริปต์ช่วงสอนหลักเอง

### 10.3 พูดแทรก

ใน Phase นี้จำลองผ่าน Demo Controls หรือ Chat:

1. บันทึก Step และ Segment ที่กำลังสอน
2. หยุด Mock Speaking
3. เปลี่ยน Media ชั่วคราวหากคำตอบมี `relatedMediaId`
4. ตอบจาก Mock FAQ
5. กลับไป Media เดิม
6. พูดต่อจาก Segment ที่ถูกขัดจังหวะ

Phase นี้ยังไม่ต้อง Resume ระดับคำภายในประโยค ให้ Resume ที่ต้น Segment เดิมได้ หากการทำระดับประโยคซับซ้อนเกินไป แต่โครงสร้างต้องรองรับตำแหน่ง Resume ภายหลัง

### 10.4 เสียงรบกวนหรือคำพูดไม่มีความหมาย

1. หยุดชั่วคราว
2. ถาม `เมื่อสักครู่ต้องการถามอะไรไหมคะ?`
3. ถ้าเงียบหรือเลือกคำตอบว่าไม่มี
4. พูด `งั้นขออนุญาตไปต่อนะคะ`
5. Resume จากจุดเดิม

### 10.5 คำถามเกี่ยวกับขั้นตอนถัดไป

- ตอบทันทีแบบสั้น
- กลับมาสอนจุดเดิม

### 10.6 ยังไม่เข้าใจ

ครั้งแรก:

- อธิบายใหม่ด้วย Mock Simplified Explanation
- แสดง Media เดิมอีกครั้ง

ถ้ายังไม่เข้าใจอีก:

- ถามว่าติดตรงส่วนใด
- ใช้ Demo Control หรือ Chat รับคำตอบ
- ตอบเฉพาะจุด

### 10.7 Checkpoint

- หยุดเฉพาะ Step ที่เปิด Checkpoint
- ใช้คำถามสำเร็จรูป เช่น `ส่วนนี้เข้าใจไหมคะ?`
- ถ้าเงียบ ให้พูด `ถ้าไม่มีคำถาม ขออนุญาตไปต่อนะคะ`
- ไป Step ถัดไป

### 10.8 ทบทวนขั้นตอนก่อนหน้า

- บันทึกตำแหน่งปัจจุบัน
- แสดงและพูด Step ก่อนหน้าชั่วคราว
- กลับมาสอนตำแหน่งเดิม
- ไม่เปลี่ยน Progress ถาวร

### 10.9 การข้าม

ไม่อนุญาตให้ข้าม Step ใน Mock Demo

### 10.10 การพัก

- เข้าสถานะ Pause เมื่อเลือกหรือพิมพ์ `ขอพักก่อน`
- รอได้ไม่จำกัดเวลา
- Resume เมื่อเลือกหรือพิมพ์ `สอนต่อได้เลย`

### 10.11 การจบ

เมื่อสอนครบ:

1. สรุปสั้น ๆ
2. เปิด Final Q&A
3. ถ้าเงียบเกินเวลาที่กำหนด ให้กล่าวลา
4. จบ Session อัตโนมัติ

ถ้ากดออกจากห้อง:

- Session จบทันที
- บันทึกว่าสอนครบหรือไม่ครบ
- บันทึก Step ล่าสุด

---

## 11. Tutor State Machine

```ts
type TutorState =
  | "PRE_JOIN"
  | "GREETING"
  | "WAITING_READY"
  | "TEACHING"
  | "CHECKPOINT"
  | "INTERRUPTED"
  | "ANSWERING"
  | "REVIEWING"
  | "PAUSED"
  | "FINAL_QA"
  | "ENDED"
  | "EXPIRED";
```

Runtime ขั้นต่ำ:

```ts
type TutorRuntime = {
  state: TutorState;
  sessionId: string;
  currentStepIndex: number;
  currentSegmentIndex: number;
  resumeStepIndex?: number;
  resumeSegmentIndex?: number;
  activeMediaId?: string;
  isAiSpeaking: boolean;
  isUserSpeaking: boolean;
  isMicEnabled: boolean;
  isCameraEnabled: boolean;
};
```

Tutor Logic ต้องอยู่ใน Reducer, Hook หรือ Engine แยกจาก Meeting UI

---

## 12. ค่าที่ต้องปรับได้

เก็บใน `src/config/tutor-config.ts`

```ts
export const tutorConfig = {
  readyAutoContinueMs: 5_000,
  checkpointSilenceMs: 5_000,
  interruptionClarifyMs: 5_000,
  finalQuestionSilenceMs: 10_000,
  reconnectGraceMs: 15 * 60_000,
  defaultLinkExpiryHours: 24,
  mockWordsPerMinute: 125,
};
```

---

## 13. Mock Data Architecture

### 13.1 กฎหลัก

```text
UI / Tutor Engine
        ↓
Repository Interfaces
        ↓
LocalStorage Mock Repositories
        ↓
Mock Data Seed ใน TypeScript
```

ห้าม Import Mock Data โดยตรงใน Page หรือ Component ที่เป็น Business Flow

### 13.2 Repository Interfaces

```ts
export interface LessonRepository {
  getLoginLesson(): Promise<Lesson>;
  saveLoginLesson(lesson: Lesson): Promise<Lesson>;
  resetLoginLesson(): Promise<Lesson>;
}

export interface SessionRepository {
  list(): Promise<TrainingSession[]>;
  create(input: CreateSessionInput): Promise<TrainingSession>;
  getById(id: string): Promise<TrainingSession | null>;
  getByToken(token: string): Promise<TrainingSession | null>;
  update(session: TrainingSession): Promise<TrainingSession>;
}

export interface ReportRepository {
  getBySessionId(sessionId: string): Promise<SessionSummary | null>;
  save(summary: SessionSummary): Promise<SessionSummary>;
}
```

### 13.3 Mock Implementations

- `LocalStorageLessonRepository`
- `LocalStorageSessionRepository`
- `LocalStorageReportRepository`

ต้องมี Storage Key แยกและ Version Number แบบง่าย เช่น:

```ts
const STORAGE_KEYS = {
  lesson: "supportroom.mock.lesson.v1",
  sessions: "supportroom.mock.sessions.v1",
  reports: "supportroom.mock.reports.v1",
};
```

Version นี้มีไว้ป้องกันข้อมูล Mock เก่าชนกับโครงสร้างใหม่ ไม่ใช่ระบบ Lesson Versioning

### 13.4 Future Implementations

สร้าง Interface ให้พร้อม แต่ยังไม่ต้องสร้าง Provider จริง:

- `ApiLessonRepository`
- `ApiSessionRepository`
- `ApiReportRepository`

เมื่อเลือก Database แล้ว ให้ Backend หรือ API Repository implement Interface เดิม

---

## 14. AI และ Voice Provider Architecture

### Interfaces

```ts
export type AnswerContext = {
  lessonSnapshot: Lesson;
  currentStepIndex: number;
  currentSegmentIndex: number;
  question: string;
};

export type AnswerResult = {
  text: string;
  scope: "IN_LESSON" | "SYSTEM_BASIC" | "OUT_OF_SCOPE" | "UNKNOWN";
  relatedMediaId?: string;
  shouldFlagForCs: boolean;
};

export interface AiAnswerProvider {
  answer(context: AnswerContext): Promise<AnswerResult>;
}

export interface TextToSpeechProvider {
  speak(text: string, options?: { signal?: AbortSignal }): Promise<void>;
  stop(): void;
}

export interface SpeechToTextProvider {
  start(onText: (text: string) => void): Promise<void>;
  stop(): Promise<void>;
}
```

### Default Providers ใน Phase นี้

1. `MockAiAnswerProvider`
   - Match Keyword จาก FAQ
   - คืนคำตอบมาตรฐาน
   - ไม่เรียก API

2. `MockTextToSpeechProvider`
   - ใช้ Timer คำนวณจากจำนวนคำ
   - Trigger Speaking Animation
   - รองรับ Abort/Stop
   - ยังไม่สร้างเสียงจริง

3. `MockSpeechToTextProvider`
   - รับข้อความจาก Demo Controls หรือ Chat
   - ยังไม่ฟังไมโครโฟนจริง

### Optional Provider ที่ทำได้หากไม่กระทบ Scope

- `BrowserTextToSpeechProvider` ใช้ Web Speech API
- ต้องปิดเป็นค่าเริ่มต้น
- ถ้า Browser ไม่รองรับต้อง Fallback ไป Mock Timer

### Placeholder สำหรับอนาคต

- `GeminiAnswerProvider`
- `GeminiLiveVoiceProvider`
- `ServerSpeechToTextProvider`
- `ExternalTextToSpeechProvider`

ใช้ Config:

```env
NEXT_PUBLIC_AI_PROVIDER=mock
NEXT_PUBLIC_TTS_PROVIDER=mock
NEXT_PUBLIC_STT_PROVIDER=mock
NEXT_PUBLIC_DATA_PROVIDER=local-storage
```

Phase นี้ห้ามมี API Key จริง

---

## 15. Session Rules ใน Mock Demo

- ลิงก์เฉพาะ Session
- Teacher Name และ School Name ไม่บังคับ
- ไม่ใช้ PIN หรือ OTP
- อายุเริ่มนับเมื่อสร้าง
- ค่าเริ่มต้น 1 วัน
- CS เปลี่ยนเวลาหมดอายุได้
- ไม่มีปุ่มยกเลิกลิงก์
- ถ้าหมดอายุก่อน Join ให้เข้าไม่ได้
- ถ้าหมดอายุระหว่างอยู่ในห้อง ให้สอนต่อได้
- กด Leave แล้ว Session จบ
- Mock Disconnect สามารถ Rejoin ภายใน 15 นาทีจาก Browser เดิมได้
- Demo รองรับครั้งละหนึ่งอุปกรณ์
- Public Token ใช้ `crypto.randomUUID()` หรือ Web Crypto
- ตอนสร้าง Session ให้ Copy Lesson เป็น Snapshot Object ภายใน Session
- เนื้อหาที่แก้ภายหลังไม่กระทบ Session ที่สร้างแล้ว

---

## 16. Route และหน้าจอ

### `/admin`

- ปุ่มจัดการบทเรียน Login
- ปุ่มสร้างลิงก์
- ตาราง Mock Sessions
- วันที่สร้าง
- ชื่อคุณครูหรือ `ไม่ระบุ`
- โรงเรียนหรือ `ไม่ระบุ`
- วันหมดอายุ
- สถานะ `ยังไม่เปิด / กำลังสอน / จบแล้ว / หมดอายุ`
- ปุ่มคัดลอกลิงก์
- ปุ่มดูสรุป
- ปุ่ม Reset Demo Data
- ข้อความกำกับว่า Mock Link ใช้ใน Browser เดียวกัน

### `/admin/lesson`

- แก้ชื่อบทเรียน
- Accordion รายการ Step
- ปุ่มขึ้น/ลงสำหรับ Step และ Segment
- Textarea Script
- เลือก Demo Media
- เปิด/ปิด Checkpoint
- เลือก Checkpoint Prompt สำเร็จรูป
- จัดการ FAQ
- Save ลง Mock Repository

ไม่ต้องสร้างระบบอัปโหลดไฟล์จริงใน Phase นี้

### `/admin/sessions/new`

- บทเรียน Login แบบ Read-only
- Teacher Name Optional
- School Name Optional
- Expiry ค่าเริ่มต้น +1 วัน
- สร้าง Token
- Save Session ลง Mock Repository
- แสดง URL และปุ่ม Copy

### `/admin/sessions/[id]`

- สถานะ Session
- สอนครบหรือไม่ครบ
- Step ล่าสุด
- คำถามที่ถาม
- จุดที่ขออธิบายใหม่
- คำถามที่ตอบไม่ได้
- เวลาเริ่มและจบ
- ไม่มีคะแนน

### `/join/[token]`

- ชื่อหัวข้อ
- ชื่อคุณครูและโรงเรียนถ้ามี
- ชื่อผู้สอน `School Bright Support`
- Preview Camera หรือ Icon
- ปุ่มเปิด–ปิดกล้อง
- ปุ่มเปิด–ปิดไมค์
- Mic Level แบบง่ายจาก Browser ถ้าทำได้
- ปุ่ม `เข้าร่วมห้องสอน`
- Permission Error ที่อ่านง่าย

ไม่ใช้คำว่า `เริ่มบทเรียน`

### `/room/[token]`

Desktop:

```text
┌──────────────────────────────────────────────────────────┐
│ School Bright Support                      สถานะการเชื่อมต่อ │
├─────────────────────────────────────┬────────────────────┤
│                                     │ [AI Icon]          │
│      หน้าจอที่ผู้สอนกำลังแชร์          │ กรอบเขียวเมื่อพูด    │
│                                     ├────────────────────┤
│                                     │ [Teacher/Camera]   │
│                                     │ กรอบเมื่อพูด         │
├─────────────────────────────────────┴────────────────────┤
│        ไมค์ | กล้อง | แชต | ออกจากห้อง                    │
└──────────────────────────────────────────────────────────┘
```

Mobile:

- Shared Screen อยู่ด้านบน
- Tiles อยู่ใต้สื่อหรือ Floating
- Control Bar ติดด้านล่าง
- Chat เป็น Bottom Sheet

ไม่แสดง Progress Bar, Step Number, Next, Previous หรือ Skip

### `/session-ended`

- ข้อความขอบคุณ
- แจ้งว่า Session สิ้นสุดแล้ว

### `/link-expired`

- แจ้งว่าลิงก์หมดอายุหรือใช้งานแล้ว
- แนะนำให้ติดต่อ CS เพื่อขอลิงก์ใหม่

---

## 17. Meeting UI States

### AI Tile

- Idle: กรอบปกติ
- Speaking: กรอบเขียวและ Pulse Animation
- Thinking: Loading dots
- Listening: Pulse เบา ๆ

### Teacher Tile

- Camera Off: Icon หรืออักษรย่อ
- Camera On: Local Camera Preview
- Speaking Mock: กรอบเขียว
- Mic Off: แสดง Mic Off

### Shared Screen

- รองรับ Image และ Video จาก `public/demo-media`
- ใช้กรอบเดียวกัน ไม่ติดป้ายว่าเป็นภาพหรือคลิป
- Video Auto-play แบบ Muted
- มี Loading และ Error State

---

## 18. Demo Controls

Development-only Drawer สำหรับจำลองเหตุการณ์:

- พร้อมแล้ว
- ยังไม่เข้าใจ
- ยังไม่เข้าใจอีกครั้ง
- ขอดูขั้นตอนก่อนหน้า
- ขอพักก่อน
- สอนต่อได้เลย
- ลืมรหัสผ่านต้องทำอย่างไร
- เลือกโรงเรียนไม่เจอ
- คำถามเรื่องระบบอื่น
- คำถามนอกเรื่อง
- เสียงรบกวน/ไม่มีคำพูดที่มีความหมาย
- จำลอง Disconnect

เปิดด้วย Feature Flag:

```env
NEXT_PUBLIC_ENABLE_DEMO_CONTROLS=true
```

Demo Controls ต้องไม่อยู่ใน Production UI เมื่อปิด Flag

---

## 19. Mock Summary

เมื่อ Session จบ ให้สร้าง:

```ts
type SessionSummary = {
  sessionId: string;
  completedAllSteps: boolean;
  lastStepIndex: number;
  lastStepTitle?: string;
  questions: Array<{
    question: string;
    answer?: string;
    scope: QuestionScope;
    resolved: boolean;
  }>;
  repeatedPoints: string[];
  unresolvedItems: string[];
  startedAt?: string;
  endedAt: string;
};
```

ไม่มีคะแนนหรือระดับประเมิน

---

## 20. Project Structure ที่แนะนำ

```text
src/
├── app/
│   ├── admin/
│   │   ├── page.tsx
│   │   ├── lesson/page.tsx
│   │   └── sessions/
│   │       ├── new/page.tsx
│   │       └── [id]/page.tsx
│   ├── join/[token]/page.tsx
│   ├── room/[token]/page.tsx
│   ├── session-ended/page.tsx
│   └── link-expired/page.tsx
├── components/
│   ├── admin/
│   ├── meeting/
│   ├── lesson/
│   └── ui/
├── config/
│   └── tutor-config.ts
├── hooks/
│   ├── use-local-media.ts
│   └── use-tutor-session.ts
├── mocks/
│   ├── lesson.mock.ts
│   ├── sessions.mock.ts
│   ├── faq.mock.ts
│   └── media.mock.ts
├── providers/
│   ├── ai/
│   │   ├── types.ts
│   │   ├── mock-ai-answer-provider.ts
│   │   └── gemini-answer-provider.ts
│   ├── stt/
│   │   ├── types.ts
│   │   ├── mock-stt-provider.ts
│   │   └── server-stt-provider.ts
│   ├── tts/
│   │   ├── types.ts
│   │   ├── mock-tts-provider.ts
│   │   ├── browser-tts-provider.ts
│   │   └── external-tts-provider.ts
│   └── data/
│       ├── repository-types.ts
│       ├── local-storage-lesson-repository.ts
│       ├── local-storage-session-repository.ts
│       └── local-storage-report-repository.ts
├── tutor/
│   ├── tutor-reducer.ts
│   ├── tutor-engine.ts
│   ├── intents.ts
│   └── types.ts
├── types/
└── utils/

public/
└── demo-media/
```

ยังไม่ต้องมี `prisma/`, `database/` หรือ API Routes ใน Phase นี้

---

## 21. Acceptance Criteria

### CS

- [ ] เปิด `/admin` ได้โดยไม่ Login
- [ ] แก้ Script และ FAQ ของบทเรียน Login ได้
- [ ] จัดลำดับ Step และ Segment ด้วยปุ่มขึ้น/ลงได้
- [ ] เลือก Demo Media ให้ Segment ได้
- [ ] Save แล้ว Refresh ข้อมูลยังอยู่ใน Browser เดิม
- [ ] สร้าง Session โดยไม่กรอกชื่อครูและโรงเรียนได้
- [ ] Expiry เริ่มต้น 1 วันและแก้ได้
- [ ] คัดลอก Mock Link ได้
- [ ] เห็น Session ในรายการ
- [ ] เปิดดู Summary หลังจบได้
- [ ] Reset Demo Data ได้

### คุณครู

- [ ] เปิด Mock Link ใน Browser เดียวกันได้
- [ ] เห็นหน้า Pre-join
- [ ] เปิด–ปิด Mic และ Camera ได้
- [ ] เข้าห้องและเห็น Shared Screen, AI Tile และ Teacher Tile
- [ ] AI Tile มี Speaking Animation
- [ ] Script เดินตาม Segment
- [ ] Media เปลี่ยนตรง Segment
- [ ] Checkpoint และ Auto-continue ทำงาน
- [ ] Chat ใช้งานได้
- [ ] Demo Controls จำลอง Interruption ได้
- [ ] ตอบคำถามแล้วกลับจุดเดิมได้
- [ ] ทบทวน Step ก่อนหน้าแบบชั่วคราวได้
- [ ] Pause และ Resume ได้
- [ ] กดออกแล้ว Session จบ
- [ ] UI ใช้ได้ทั้ง Desktop และ Mobile

### ระบบ

- [ ] ไม่มี Prisma หรือ Database Dependency
- [ ] Mock Data แยกจาก UI
- [ ] LocalStorage Access อยู่หลัง Repository Interface
- [ ] AI, STT และ TTS แยกด้วย Provider Interface
- [ ] Session Token สุ่มและไม่ใช้ Running ID
- [ ] Expired Link ถูกปฏิเสธใน Mock Flow
- [ ] มี Mock Summary
- [ ] ไม่มี API Key จริง
- [ ] `npm run lint` ผ่าน
- [ ] `npm run build` ผ่าน

---

## 22. ลำดับการพัฒนา

### Phase 1A — UI Foundation

- Scaffold Next.js
- Tailwind และ Responsive Layout
- Admin Pages
- Pre-join Page
- Meeting Room UI
- Camera/Mic Browser Controls

### Phase 1B — Mock Data และ Mock Session

- TypeScript Seed Data
- Repository Interfaces
- LocalStorage Repositories
- Lesson Editor
- Session Creation/List/Summary
- Mock Link Flow

### Phase 1C — Tutor Engine

- State Machine
- Script Playback Timer
- Media Switching
- Checkpoint
- Auto-continue
- Pause/Resume
- Review Previous Step
- Mock Interruption
- Mock FAQ
- Final Summary

### Phase 2 — Voice Integration ทีละส่วน

1. Browser Text-to-Speech หรือ External TTS
2. Speech-to-Text ภาษาไทย
3. Voice Activity Detection
4. Barge-in จริง
5. Realtime Voice Provider หรือ Gemini Live API

### Phase 3 — AI Integration

1. Gemini Intent Classification
2. Grounded Q&A จาก Script และ FAQ
3. Guardrail ไม่ตอบนอกข้อมูล
4. Unresolved Question Logging

### Phase 4 — Database และ Backend

เลือก Database ภายหลังตาม Hosting และระบบบริษัท แล้ว:

- สร้าง Backend/API
- Implement API Repositories
- ย้าย Mock Data เข้าฐานข้อมูล
- รองรับลิงก์ข้ามอุปกรณ์จริง
- เพิ่ม Authentication
- เพิ่ม Media Storage

---

## 23. Definition of Done สำหรับ Mock Demo

Demo ถือว่าพร้อมเมื่อสามารถ:

1. เปิด Admin และแก้สคริปต์ Login
2. Save ลง LocalStorage ผ่าน Repository
3. สร้าง Mock Session Link
4. เปิดลิงก์ในอีก Tab ของ Browser เดียวกัน
5. ผ่านหน้า Pre-join
6. เข้าห้องที่ดูเหมือน Video Call
7. เห็น Mock Speaking Animation และ Media เปลี่ยนตาม Script
8. ใช้ Demo Controls จำลองคำถาม, Interruption, ไม่เข้าใจ, ทบทวน และพัก
9. จบ Session
10. กลับไปดู Summary ใน Admin
11. Reload หน้าแล้ว Mock Data ที่บันทึกไว้ยังอยู่
12. Build และ Lint ผ่าน

---

## 24. ข้อกำชับสำหรับผู้พัฒนา

- อย่าเพิ่ม Database ใน Phase นี้
- อย่าเพิ่ม Prisma ใน Phase นี้
- อย่าทำ API หรือ Backend เพียงเพราะคิดว่า Production ต้องมี
- อย่าผูก UI กับ `localStorage` โดยตรง
- อย่าผูก Tutor Logic กับ Component Layout
- อย่าผูก Business Logic กับ Gemini หรือ Voice Provider ใด
- อย่าเรียกระบบนี้ว่า Video Lesson ใน UX
- อย่าให้ Shared Media มีบทบาทแทนผู้สอน
- อย่าให้ Mock AI ตอบนอกข้อมูลที่กำหนด
- อย่าทำ Multi-device หรือ Realtime Sync ใน Phase นี้
- ทุก Future Provider ให้มี TODO และ Interface ที่ชัดเจน
- ให้ความสำคัญกับ Demo Flow, UX และความสามารถในการต่อยอดมากกว่าความสมบูรณ์แบบเชิง Production

