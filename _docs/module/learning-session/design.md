# การเรียน 1 ลิงก์ = หลายการเรียน + บันทึกความคืบหน้า/รีวิวคำตอบ AI (learning-session) — Feasibility & Design

> **สถานะเอกสาร (2026-08-18):** **ยืนยันครบแล้ว — พร้อมส่งต่อ `project-manager`**
> ทั้ง 6 จุดที่เคยค้าง (Q2, Q3, Q4 + D1–D3) เจ้าของโปรเจกต์เคาะเมื่อ 2026-08-18
> **ตรงตามข้อเสนอของ `system-analyst` ทุกข้อ** เนื้อหาในเอกสารนี้จึงเป็น contract ที่ implement
> ได้ทันที ไม่มีส่วนใดรอคำตอบอีก · หัวข้อทางเลือก/trade-off ที่ยังอยู่ในเอกสาร (Q2+Q3, Q4, D1, D3)
> **เก็บไว้เป็นบันทึกเหตุผล ไม่ใช่คำถามที่ยังเปิดอยู่** — ห้ามรื้อทางเลือกที่ถูกตัดไปแล้วกลับมาพิจารณาใหม่
> โดยไม่ผ่านการ amend · ดูสรุปมติที่ `## Unresolved Open Questions`
>
> **โปรเจกต์นี้ไม่ใช่ Prisma** — contract ของ data model คือ entity ใน
> `backend/src/SupportRoom.Domain/Entities/` + EF Core migration ใน
> `backend/src/SupportRoom.Providers.Data/Migrations/` (กฎ `schema.prisma` ใน
> `.claude/shared/conventions.md` §7 ต้องอ่านเทียบเป็นสองอย่างนี้)

---

## Feasibility Summary

ทำได้ทั้งหมดด้วย stack ปัจจุบัน (ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR + Next.js 15)
**ไม่ต้องเพิ่ม dependency, external service หรือ provider ใหม่แม้แต่ตัวเดียว** ทุกฟีเจอร์ F1–F8
ใช้ pattern ที่โปรเจกต์มีอยู่แล้ว (layered service + repository + UnitOfWork + query filter +
`ServerDefaults` env + SignalR group)

ความยากไม่ได้อยู่ที่เทคโนโลยี แต่อยู่ที่ **ขอบเขตการเปลี่ยนแปลงพร้อมกันหลายจุด**: ตารางใหม่ 1 ตาราง,
ตารางเดิมเปลี่ยนความหมาย 1 ตาราง (ย้าย 6 ฟิลด์ออก), FK ของ 2 ตารางย้ายเป้าหมาย, ตารางเดิม 1 ตาราง
เสนอให้ลบทิ้ง, endpoint REST เปลี่ยน/เพิ่มรวม 12 เส้น, และ **SignalR group key ต้องเปลี่ยนจาก
`Token` เป็น "การเรียน"** ซึ่งเป็นจุดที่ถ้าพลาดจะกลายเป็นข้อมูลรั่วข้ามผู้เรียนแบบเงียบๆ
(คนที่สองบนลิงก์เดียวกันได้รับ chat/คำถามของคนแรก) — ตรงกับที่ `requirement.md` เตือนไว้ว่า
"นี่คืองานเปลี่ยนโครงสร้างข้อมูล ไม่ใช่งานเติมฟิลด์"

**ข้อเท็จจริงที่เปลี่ยนน้ำหนักของ Q2/Q4 อย่างมีนัยสำคัญ — ระบบยังไม่เคย deploy จริง**
(ยืนยันจากไฟล์จริง 2026-08-18: ไม่มี `Dockerfile*`, ไม่มี `docker-compose*`, `.github/workflows/`
ไม่มีไฟล์เลย, `docs/PRODUCTION_ROADMAP.md` Phase 1 "ทำให้ deploy ได้จริง" ยังไม่ติ๊กสักข้อ
รวมถึงข้อ 1.4 "RDS for PostgreSQL + รัน migration") → **ไม่มีข้อมูลลูกค้าจริงในฐานข้อมูลใดๆ**
มีแต่ข้อมูล demo ในเครื่อง dev เท่านั้น ต้นทุนของการ rename ตาราง/ลบตารางจึงเป็น "ต้นทุนแก้โค้ด"
ล้วนๆ ไม่มีต้นทุน "ข้อมูลลูกค้าเสียหาย" และการทำตอนนี้ถูกกว่าทำทีหลังอย่างชัดเจน

---

## Feature-by-Feature Feasibility

| ฟีเจอร์ | ผลประเมิน | หมายเหตุ |
|---|---|---|
| **F1** แยก "ลิงก์" ออกจาก "การเรียน" | **ทำได้ (straightforward ทางเทคนิค แต่กระทบกว้าง)** | ไม่มี dependency ใหม่ · งานคือ migration + rename + ไล่แก้ทุกจุดที่อ้าง `TrainingSession` · กติกาหมดอายุ = บังคับตอน *สร้างการเรียนใหม่* เท่านั้น (LR-1) ไม่แตะรายการที่ค้างอยู่ (LR-3) · ⚠️ วันนี้ backend **ไม่ได้บังคับ expiry เลย** (บังคับที่ frontend อย่างเดียว — `utils/session-status.ts`) เฟสนี้ทำให้ backend บังคับจริงเป็นครั้งแรก |
| **F1b** ตารางใหม่ "การเรียนของแต่ละคน" | **ทำได้** | ตารางใหม่ 1 ตาราง เข้ากฎ `ICompanyScoped` + query filter ครบ · test `EveryEntityIsCompanyScoped` ใน `CompanyIsolationTests.cs` จะ fail ทันทีถ้าลืม filter (tripwire ที่มีอยู่แล้ว ใช้ได้เลย) |
| **F2** ผู้เรียนกรอกชื่อเองก่อนเข้าห้อง | **ทำได้** | หน้า `join/[token]` เพิ่มฟอร์มชื่อ + endpoint สร้างการเรียน · ไม่เก็บ PII อื่นตามมติ 2026-08-18 |
| **F3** `LearnerKey` ทำสองหน้าที่ | **ทำได้ แต่มีเงื่อนไขความปลอดภัยที่ต้องเขียนเป็นกติกา** | `localStorage` + `crypto.randomUUID()` · **`LearnerKey` และ `LearningSession.Id` กลายเป็น bearer credential เพิ่มอีกสองตัวในระบบที่ยังไม่มี auth (TD-002)** — กติกาบังคับอยู่ที่ `## Isolation & Credential Rules` · **เครื่องที่ใช้ร่วมกัน** (คอมกลางในโรงเรียน) มี `LearnerKey` เดียวกัน → เดิมคนที่สองจะ "เรียนต่อ" ของคนแรก · **ปิดช่องนี้ด้วยมติ D2 (2026-08-18): ต้องถามยืนยันก่อน resume เสมอ** — `requirement.md` F3 แยกเป็นกรณี ก/ข แล้ว กติกาบังคับอยู่ที่ **LR-3 + LR-3a + IC-7** |
| **F4** บันทึกความคืบหน้า | **ทำได้** | ⚠️ **`LastSlideIndex` อย่างเดียวแสดง "7/20" ไม่ได้** — ต้องมีตัวหารด้วย จึงเพิ่ม `TotalSlideCount` ในตารางใหม่ (สเปกเดิม §3.2 ไม่ได้ระบุไว้ แต่เป็นเงื่อนไขบังคับของข้อความ "โดยไม่ต้อง resolve deck ใหม่" ใน F4) |
| **F5** แยก "ครบสไลด์" จาก "จบแล้ว" + หน้าสรุป + เรียนอีกครั้ง | **ทำได้** | สองคอลัมน์แยกกันตามมติ · "เรียนอีกครั้ง" = สร้างแถวใหม่ (LR-6) · หน้าสรุปฝั่งผู้เรียนกับฝั่ง CS ใช้ **ViewModel คนละตัว** เพื่อไม่ให้ข้อมูลภายในรั่ว (RR-5) |
| **F6** "หยุดกลางคัน" คำนวณตอนแสดงผล | **ทำได้** | `INACTIVE_THRESHOLD_MINUTES` เข้า `ServerDefaults.cs` ตาม pattern เดิมเป๊ะ · คำนวณที่ ViewModel ฝั่ง backend (ที่เดียว) ไม่ให้ frontend คำนวณเอง — เหตุผลใน SR-2 |
| **F7** CS รีวิวคำตอบ AI | **ทำได้** | ⚠️ ต้องแก้ `SessionQuestion` ให้ audit field เป็น `set` ได้ (วันนี้เป็น `init` ทั้งชุด → อัปเดตแถวเดิมไม่ได้) ดู DM-3 |
| **F8** ช่อง `MaxAttendees` กรอกได้แต่ไม่บังคับใช้ | **ทำได้** | คอลัมน์ `int?` + ช่องกรอก + ข้อความกำกับใน UI · **ห้ามเขียนโค้ดตรวจ** (LR-2) — เป็นข้อห้ามที่เขียนไว้เป็นกติกาเพราะ engineer มักเผลอ validate ให้ "ครบถ้วน" |

**ไม่มีฟีเจอร์ใดใน F1–F8 ที่ต้องใช้ dependency ใหม่ / บริการภายนอกใหม่ / อยู่นอก stack**

### การตัดสินใจที่ผู้ใช้ยืนยันแล้ว

ตารางนี้คือสิ่งที่ **เคาะไปแล้ว** (แหล่งที่มา: `requirement.md` — เจ้าของโปรเจกต์ตอบเอง)
downstream agent ทุกตัวอ่านตารางนี้เพื่อไม่ไปรื้อของที่ตัดสินไปแล้ว

| คำถาม | คำตอบที่เลือก | สิ่งที่ถูกตัดออกด้วยคำตอบนี้ |
|---|---|---|
| ลิงก์กับการเรียนสัมพันธ์กันแบบไหน | **1 ลิงก์ = หลายการเรียน แยกคนละคน** (ยืนยันซ้ำ 2026-08-18 หลังเคยพลิกเป็น 1:1 แล้วเพิกถอน) | โครงสร้าง 1:1 · การเก็บสถานะ/ความคืบหน้าไว้ที่ลิงก์ |
| ใครกรอกชื่อผู้เรียน | **ผู้เรียนกรอกเองตอนเข้าห้อง** | CS กรอกล่วงหน้า · `RecipientName` อยู่ที่ลิงก์ |
| เก็บข้อมูลอะไรจากผู้เรียนบ้าง | **ชื่ออย่างเดียว** ไม่เอาเบอร์/อีเมล/ตำแหน่ง | เหตุผลเรื่อง PII ในการติด 🔒 Security gate |
| ลิงก์หมดอายุระหว่างมีคนเรียนค้าง | **ให้รายการที่ค้างเรียนต่อจนจบ · ห้ามเริ่มรายการใหม่** | การตัดกลางคัน · การ auto-end ตอนหมดอายุ |
| ล้าง browser storage / เปลี่ยนเครื่อง | **กลายเป็นคนใหม่ ยอมรับได้** รวมถึงที่ CS แยกสองรายการไม่ออก | login / OTP / รหัสยืนยัน — **ห้ามเสนอในเฟสนี้** |
| "หยุดกลางคัน" | **คำนวณตอนแสดงผล ไม่เก็บสถานะลง DB** | คอลัมน์สถานะ "หยุดกลางคัน" · สัญญาณตอนปิดแท็บ |
| "จบแล้ว" | **ครบสไลด์ *หรือ* กดจบเอง → เก็บสองค่าแยกกัน** | การยุบเป็นค่าเดียว |
| หมายเหตุรีวิว | **ข้อความอิสระ** | dropdown / enum ของสาเหตุ |
| "จุดที่ AI ตอบไม่ได้" | **คำนวณจาก `AnswerStatus = not_found`** ไม่เก็บซ้ำ | คอลัมน์/ตารางเก็บ unanswered points |
| จำกัดจำนวนคนต่อลิงก์ | **เก็บค่าได้ แต่ยังไม่บังคับใช้** + ต้องมีข้อความกำกับใน UI | โค้ดตรวจจำนวนคน (Declined 2026-08-11) |

### ✅ มติเชิงโครงสร้าง 6 ข้อ — ยืนยันแล้วเมื่อ 2026-08-18 โดยเจ้าของโปรเจกต์

เดิมเป็นข้อเสนอที่รอเคาะ (Q2/Q3/Q4 + D1–D3) — **เจ้าของโปรเจกต์ตอบครบทั้ง 6 ข้อเมื่อ 2026-08-18
และตรงตามข้อเสนอของ `system-analyst` ทุกข้อ** จึงเป็นมติที่มีผลบังคับแล้ว ไม่ใช่คำถามที่ยังเปิดอยู่

| # | เรื่อง | มติที่ยืนยัน (2026-08-18) | สิ่งที่ถูกตัดออกด้วยมตินี้ |
|---|---|---|---|
| **Q2** | rename `TrainingSession` ไหม | **rename → `LessonLink`** | ตัวเลือก B/C (คงชื่อเดิม) และ D (`TrainingLink`) — ห้ามคงชื่อ `TrainingSession` ไว้ในโค้ดใหม่ |
| **Q3** | ชื่อตารางใหม่ | **`LearningSession`** | `LearnerAttempt` · `TrainingAttendance` |
| **Q4** | `SessionSummary` | **ลบทิ้งทั้งใบ (13 จุด)** | การย้าย summary ไปผูก `LearningSession` · การคงตารางไว้เฉยๆ · snapshot แช่แข็งตอนจบ |
| **D1** | route/TS type ตามชื่อใหม่ด้วยไหม | **ตามด้วย** — `/api/links`, `/api/learning-sessions`, type `LessonLink` | การคง path `/api/sessions` และ TS type ชื่อ `TrainingSession` |
| **D2** | เครื่องใช้ร่วมกัน → resume แบบไหน | **ถามยืนยันก่อน resume เสมอ** พร้อมทางเลือก "เริ่มเรียนใหม่ในชื่ออื่น" | การ resume เงียบๆ · การจำสถานะ "ยืนยันแล้ว" เพื่อข้ามคำถามครั้งถัดไป · login/OTP ทุกรูปแบบ |
| **D3** | migrate ข้อมูล demo เดิมไหม | **migrate ด้วย backfill SQL** ใน migration เดียวกัน | migration แบบทำลาย (drop ทิ้งแล้วสร้าง demo ใหม่ด้วยมือ) |

D2 ถูกส่งกลับไปที่ `business-analyst` แล้ว และ `requirement.md` F3 ฉบับ 2026-08-18 แยกเป็น
**กรณี ก** (ไม่มีกุญแจ = คนใหม่ ไม่ต้องถาม) กับ **กรณี ข** (มีกุญแจ *และเจอการเรียนที่ยังไม่จบ*
= ต้องถามยืนยัน) — กติกาที่ engineer ต้องทำตามอยู่ที่ **LR-3** และ **IC-7**

---

## Q2 + Q3 — ชื่อของ "ลิงก์" และ "การเรียน" (ตัดสินคู่กัน)

**ทำไมต้องตัดสินคู่กัน:** ปัญหาไม่ใช่ "ชื่อเดิมผิดไหม" แต่คือ "อ่านสองชื่อติดกันแล้วแยกออกไหม"
ชื่อที่แย่ที่สุดคือคู่ที่เป็นคำพ้องความหมาย เพราะคนอ่านโค้ดจะแยกไม่ออกว่าตัวไหนคือของกลางที่แจกได้
ตัวไหนคือของรายคน

### ทางเลือก

| ตัวเลือก | ชื่อ "ลิงก์" | ชื่อ "การเรียน" | ต้นทุน | ความเสี่ยงที่เหลือ |
|---|---|---|---|---|
| **A ⭐ (เสนอ)** | `LessonLink` | `LearningSession` | rename ตาราง 1 + ไล่แก้ทุก layer (~25 ไฟล์ backend, ~12 ไฟล์ frontend) ใน migration เดียว | คำว่า "session" ยังอยู่ แต่**ย้ายไปอยู่ฝั่งที่ถูกต้องแล้ว** — `SessionQuestion`/`SessionHub`/`useSessionChat` ทั้งหมดผูกกับ "การเรียน" ซึ่งตรงกับความหมายใหม่พอดี |
| **B** | คงชื่อ `TrainingSession` | `LearningSession` | ต่ำสุด (ไม่ rename) | **สูงมาก** — `TrainingSession` กับ `LearningSession` เป็นคำพ้อง อ่านผ่านๆ แยกไม่ออก และชื่อ `TrainingSession` จะสื่อผิดถาวร (มันคือ "ลิงก์") |
| **C** | คงชื่อ `TrainingSession` | `LearnerAttempt` | ต่ำสุด | ชื่อแยกออกดี แต่ `TrainingSession` ยังสื่อผิดอยู่ดี และคำว่า "attempt" ชวนคิดว่าเป็นการสอบ/ให้คะแนน ซึ่งไม่ใช่โดเมนนี้ |
| **D** | `TrainingLink` | `TrainingAttendance` | เท่ากับ A | "attendance" สื่อไปทาง "การเช็คชื่อ" มากกว่า "การเรียนหนึ่งรอบ" |

### ✅ มติ (ยืนยัน 2026-08-18): ตัวเลือก A — `LessonLink` + `LearningSession`

เหตุผล:

1. **ต้นทุน rename ตอนนี้ ≈ ต้นทุนแก้โค้ดล้วน** เพราะยังไม่ deploy (ยืนยันแล้ว — ไม่มี Dockerfile/CI
   และ roadmap 1.4 ยังไม่ได้ทำ) ไม่มีข้อมูลลูกค้า ไม่มี client ภายนอกที่ผูกกับ API path
   **frontend ในโปรเจกต์นี้คือ consumer เดียวของ API** (`frontend/src/lib/api-client.ts` เป็นจุดเดียว
   ที่ browser คุยกับ backend) จึงเปลี่ยนสัญญาได้แบบ atomic
2. **ทำทีหลังแพงกว่าหลายเท่า** — เมื่อมีข้อมูลลูกค้าจริง การ rename ตารางจะต้องมี migration window
   และการอ่านโค้ดผิดสะสมไปแล้วหลายเดือน
3. **`SessionQuestion` / `ChatMessage` / `SessionHub` / `useSessionChat` ย้ายไปผูกกับ "การเรียน" อยู่แล้ว**
   ตาม F7 — เมื่อ "การเรียน" ชื่อ `LearningSession` คำว่า `SessionId` ที่กลายเป็น `LearningSessionId`
   จะอ่านแล้วถูกต้องทั้งชุด ไม่ต้อง rename `SessionQuestion`/`ChatMessage` เพิ่ม
4. ตัวเลือก B ประหยัดวันนี้ แต่ซื้อความสับสนถาวรที่ไม่มีวันหมดอายุ

**สถานะ: ✅ ยืนยันตัวเลือก A เมื่อ 2026-08-18** — ชื่อ `LessonLink` + `LearningSession` เป็นชื่อจริง
ที่ implement ตามได้เลย ตัวเลือก B/C/D ถูกตัดออกแล้ว เก็บตารางไว้เป็นบันทึกเหตุผลเท่านั้น

### D1 ✅ (ยืนยัน 2026-08-18) — route และ TypeScript type ตามชื่อใหม่ด้วย

- **มติ: ตามด้วย** — `/api/sessions` → `/api/links`, type `TrainingSession` → `LessonLink`
  เหตุผล: ถ้า entity ชื่อ `LessonLink` แต่ route ยังเป็น `/api/sessions` เราจะได้ความสับสนแบบเดียว
  กับที่ Q2 พยายามแก้ แค่ย้ายไปอยู่ที่ชั้น API แทน · ต้นทุนเพิ่มจากตัวเลือก A แทบเป็นศูนย์
  เพราะยังไงก็ต้องแก้ `api-client.ts` ทุกฟังก์ชันอยู่แล้ว
- ~~ทางเลือก: คง path `/api/sessions` ไว้~~ — **ถูกตัดออกด้วยมติ 2026-08-18**

---

## Q4 — `SessionSummary` เก็บหรือลบ

### ข้อเท็จจริงจากโค้ดจริง (ตรวจแล้ว 2026-08-18 ไม่ใช่การอนุมาน)

`SessionSummary` ถูกอ้างถึงใน **13 จุด** แบ่งเป็น:

| จุด | ไฟล์ | บทบาท |
|---|---|---|
| entity | `Domain/Entities/SessionSummary.cs` | เก็บ `CompletedAllSlides` · `LastSlideObjectId` · `UnansweredPoints` (`text[]`) |
| service | `Application/Services/ISessionSummaryService.cs` | `Save()` + `GetBySessionId()` |
| repository | `Providers.Data/Repository/ISessionSummaryRepository.cs` | |
| DI | `Api/Configurations/ServiceConfiguration.cs` · `Providers.Data/.../UnitOfWork.cs` | ลงทะเบียน 2 ที่ |
| DbContext | `Providers.Data/Data/ApplicationDbContext.cs` | `DbSet` + `HasIndex(SessionId).IsUnique()` + query filter |
| **ผู้เรียกเดียวที่เขียน** | `ITrainingSessionService.End()` บรรทัด 128–129 | เรียก `summaryService.Save(...)` **ทันทีหลังเพิ่งเขียนค่าเดียวกันลง `TrainingSession` ไปแล้วในบรรทัด 121–122** (เขียนซ้ำสองที่อยู่แล้ววันนี้) |
| ผู้เรียกที่ลบ | `IAdminService.ResetDemoData()` | ลบ summary ทุกแถว |
| endpoint เดียว | `TrainingSessionController.GetSummary()` → `GET /api/sessions/{token}/summary` | |
| ViewModel | `Application/ViewModel/SessionSummaryViewModel.cs` | |
| frontend client | `lib/api-client.ts` → `getSessionSummary()` | |
| frontend type | `types/domain.ts` → `SessionSummary` | |
| **หน้าจอเดียวที่ใช้** | `app/admin/sessions/[token]/page.tsx` | หน้าสรุปฝั่ง CS |
| tests | `SessionSummaryServiceTests.cs` · `TrainingSessionServiceTests.cs` · `AdminServiceTests.cs` · `Fakes/ServiceTestFakes.cs` | |
| migration | `20260806150540_AddSessionSummary` | |

**หลักฐานสำคัญที่สุด:** `app/admin/sessions/[token]/page.tsx` บรรทัด 64 **มี fallback ที่คำนวณ
`unansweredPoints` จาก `questions.filter(q => q.answerStatus === "not_found")` อยู่แล้ว**
และใช้ path นี้จริงทุกครั้งที่ยังไม่มี summary — แปลว่าเส้นทาง "คำนวณสด" ถูกเขียนไว้แล้วและใช้งานได้จริง
ไม่ใช่ของที่ต้องสร้างใหม่

### ทางเลือก

| ตัวเลือก | ได้อะไร | เสียอะไร |
|---|---|---|
| **A ⭐ (เสนอ) ลบทิ้งทั้งใบ** | ลบโค้ดออก 13 จุด · เลิกเขียนข้อมูลซ้ำสองที่ · ไม่ต้องออกแบบว่าจะทำอย่างไรกับ unique index `SessionId` ที่พังทันทีเมื่อ 1 ลิงก์มีหลายการเรียน | เสีย "snapshot แช่แข็ง ณ เวลาจบ" — ถ้าวันหนึ่งลบ/แก้ `SessionQuestion` ย้อนหลัง ตัวเลข unanswered จะเปลี่ยนตาม |
| **B ย้ายไปผูกกับ `LearningSession`** | เก็บ snapshot ไว้ | ต้องเขียน migration เท่ากัน + ยังคงเขียนซ้ำสองที่ + `CompletedAllSlides`/`LastSlideObjectId` ซ้ำกับตารางใหม่แบบ 100% + ต้องเขียน snapshot ใหม่ทุกครั้งที่ "เรียนอีกครั้ง" |
| **C คงไว้เฉยๆ ไม่แตะ** | ไม่ต้องทำอะไร | **ใช้ไม่ได้จริง** — `IX_SessionSummary_SessionId` เป็น unique ต่อ "ลิงก์" ซึ่งขัดกับ 1 ลิงก์ = หลายการเรียนโดยตรง ยังไงก็ต้องมี migration |

### ✅ มติ (ยืนยัน 2026-08-18): ตัวเลือก A — ลบทิ้ง

เหตุผล:

1. **ทั้งสามคอลัมน์ซ้ำซ้อนจริงหลังแยกโครงสร้าง** — `CompletedAllSlides` และ `LastSlideObjectId`
   ย้ายไปเป็นของ `LearningSession` ตาม F1b · `UnansweredPoints` คำนวณจาก `AnswerStatus = not_found`
   ตามมติที่เคาะแล้ว
2. **ตัวเลือก C ไม่มีอยู่จริง** — unique index บังคับ 1 summary ต่อ 1 ลิงก์ ต้องแก้ไม่ทางใดก็ทางหนึ่ง
   ดังนั้นทุกทางเลือกมีต้นทุน migration เท่ากัน เหลือแค่ว่าจะจ่ายเพื่อเก็บของซ้ำไว้หรือไม่
3. **F7 ต้องการค่าที่ *สด* ไม่ใช่ค่าที่แช่แข็ง** — CS จะรีวิวคำตอบ (`ReviewResult`/`ReviewNote`)
   หลังการเรียนจบไปแล้ว หน้าสรุปต้องแสดงผลรีวิวล่าสุดเสมอ snapshot ที่เขียนครั้งเดียวตอนจบ
   จะไม่มีวันมีข้อมูลรีวิว
4. **ความเสี่ยงที่เสียไปเป็นศูนย์ในทางปฏิบัติ** — ไม่มี flow ใดในระบบที่ลบหรือแก้ `SessionQuestion`
   ย้อนหลัง (มีแต่ `ResetDemoData` ซึ่งลบทุกอย่างพร้อมกันอยู่แล้ว)
5. ระบบยังไม่ deploy → ไม่มีข้อมูล summary จริงให้ต้องรักษา

**สิ่งที่ต้องทำตามมาเมื่อลบ** (รายการนี้คือ contract — ห้ามลบครึ่งเดียว):
ลบทั้ง 13 จุดข้างบน + `DropTable` ใน migration + สร้าง endpoint ทดแทนตาม `## API & SignalR
Contract Delta` (`GET /api/learning-sessions/{id}/summary` ฝั่งผู้เรียน และ
`GET /api/learning-sessions/{id}` ฝั่ง CS) ซึ่งประกอบข้อมูลสดจาก `LearningSession` + `SessionQuestion`

---

## Data Model

> **นี่คือ contract ที่ `backend-engineer` implement ตรงตัว** — ชื่อฟิลด์ ชนิด nullability
> index และ query filter ตามนี้เป๊ะ ห้ามเพิ่ม/ลด/เปลี่ยนชื่อเอง ถ้าขาดอะไรให้ตีกลับมาที่
> `system-analyst` · ชื่อ entity เป็นชื่อที่ **ยืนยันแล้วตาม Q2/Q3 เมื่อ 2026-08-18** ใช้ตามนี้ตรงตัว

### DM-1 · `LessonLink` (เดิมชื่อ `TrainingSession`) — "ลิงก์"

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// สื่อการสอนที่แจกได้ (เดิมชื่อ TrainingSession) - หนึ่งลิงก์ถูกหยิบไปเรียนได้หลายคน หลายรอบ
/// สถานะ/ความคืบหน้าไม่ได้อยู่ที่นี่แล้ว ย้ายไป LearningSession ทั้งหมด (design.md DM-2)
/// </summary>
public sealed class LessonLink : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("link")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string Token { get; init; }           // unique ทั้งระบบ - public join secret
    public required string LessonId { get; init; }
    public required string LessonSlug { get; init; }

    /// <summary>องค์กรของผู้รับลิงก์ (โรงเรียน/สาขา/ฝ่าย) - label ล้วน ไม่ใช่ CompanyId
    /// ไม่เคยถูกใช้ใน query filter</summary>
    public string? RecipientOrgName { get; init; }

    public required DateTime ExpiresAt { get; init; }

    /// <summary>F8 - null = ไม่จำกัด · เก็บค่าไว้เฉยๆ ยังไม่บังคับใช้ในเฟสนี้ (LR-2)</summary>
    public int? MaxAttendees { get; init; }
}
```

**ฟิลด์ที่หายไปจากของเดิม (ย้ายไป `LearningSession` ทั้งหมด):**
`RecipientName` · `Status` · `StartedAt` · `EndedAt` · `CompletedAllSlides` · `LastSlideObjectId`

**ลิงก์ไม่มีคอลัมน์ `Status`** — สถานะของลิงก์คำนวณตอนแสดงผลจาก `ExpiresAt` เท่านั้น
(`ACTIVE` / `EXPIRED`) ห้ามเพิ่มคอลัมน์ ห้ามใช้ `SessionStatus` เดิมกับลิงก์

### DM-2 · `LearningSession` (ตารางใหม่) — "การเรียนหนึ่งรอบของหนึ่งคน"

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// การเรียนหนึ่งรอบของหนึ่งคน - เกิดขึ้นตอนผู้เรียนกดเข้าและระบุชื่อ (ไม่ใช่ตอน CS สร้างลิงก์)
/// หนึ่ง LessonLink มีได้ไม่จำกัดรายการ และคนเดิมเรียนซ้ำได้หลายรอบ (F5 "เรียนอีกครั้ง")
/// </summary>
public sealed class LearningSession : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("learning")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>ลิงก์ที่ใช้เข้ามา - logical FK ไม่มี FK constraint จริง ตามแบบแผนเดิมของโปรเจกต์</summary>
    public required string LessonLinkId { get; init; }

    /// <summary>กุญแจที่ browser ของผู้เรียนเก็บไว้ (F3) - ใช้ทั้งกลับมาเรียนต่อและแยกคนบนลิงก์เดียวกัน
    /// ไม่ใช่การยืนยันตัวตน และไม่เคยใช้ resolve company (ดู IC-1..IC-4)</summary>
    public required string LearnerKey { get; init; }

    /// <summary>ผู้เรียนกรอกเอง (F2) - ป้ายกำกับ ไม่ใช่ identity · trim แล้ว 1-80 ตัวอักษร</summary>
    public required string LearnerName { get; set; }

    /// <summary>LearningStatus.InProgress | LearningStatus.Ended เท่านั้น - ไม่มี NOT_STARTED
    /// เพราะแถวนี้เกิดตอนกดเข้าห้องแล้ว</summary>
    public required string Status { get; set; }

    /// <summary>= เวลาที่แถวถูกสร้าง (กดเข้าห้อง) - ไม่ nullable ต่างจาก TrainingSession เดิม</summary>
    public required DateTime StartedAt { get; init; }

    public DateTime? EndedAt { get; set; }

    /// <summary>F6 ใช้คำนวณ "หยุดกลางคัน" ตอนแสดงผล - ไม่มีคอลัมน์สถานะหยุดกลางคัน (SR-1..SR-3)</summary>
    public required DateTime LastActivityAt { get; set; }

    public string? LastSlideObjectId { get; set; }

    /// <summary>ลำดับสไลด์ล่าสุดแบบ 0-based ตรงกับ runtime.currentSlideIndex ฝั่ง frontend</summary>
    public int? LastSlideIndex { get; set; }

    /// <summary>จำนวนสไลด์ทั้ง deck ณ เวลาที่เรียน - เก็บไว้เพื่อให้ CS เห็น "7/20"
    /// โดยไม่ต้อง resolve deck ใหม่ (F4) · เก็บที่นี่ไม่ใช่ที่ลิงก์ เพราะ deck แก้ได้ระหว่างทาง
    /// และตัวเลขต้องตรงกับสิ่งที่ผู้เรียน *คนนั้น* เห็นจริง</summary>
    public int? TotalSlideCount { get; set; }

    /// <summary>F5 - แยกจาก Status.Ended โดยเด็ดขาด ห้ามยุบรวม
    /// true = ไปถึงสไลด์สุดท้ายจริง · Ended = ปิดรายการแล้วไม่ว่าจะครบหรือไม่</summary>
    public bool CompletedAllSlides { get; set; }
}
```

### DM-3 · `SessionQuestion` (แก้ของเดิม)

```csharp
public sealed class SessionQuestion : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }      // ⚠️ เดิมเป็น init - ต้องเปลี่ยนเป็น set
    public DateTime? UpdateDate { get; set; }  // ⚠️ เดิมเป็น init - ต้องเปลี่ยนเป็น set
    public string? DeleteBy { get; set; }      // ⚠️ เดิมเป็น init
    public bool IsDelete { get; set; }         // ⚠️ เดิมเป็น init
    public DateTime? DeletedAt { get; set; }   // ⚠️ เดิมเป็น init

    /// <summary>เดิมชื่อ SessionId และชี้ไป TrainingSession - ตอนนี้ชี้ไป LearningSession (F7)</summary>
    public required string LearningSessionId { get; init; }

    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }

    /// <summary>F7 - ReviewResult.Correct | ReviewResult.Incorrect | null (ยังไม่รีวิว)</summary>
    public string? ReviewResult { get; set; }

    /// <summary>F7 - ข้อความอิสระ ไม่ใช่ enum (มติ 2026-08-11) · null ได้ · สูงสุด 2000 ตัวอักษร</summary>
    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
```

⚠️ **จุดที่พลาดง่าย:** วันนี้ audit field ของ `SessionQuestion` เป็น `init` ทั้งชุด (ต่างจาก
`TrainingSession` ที่เป็น `set`) เพราะไม่เคยมี flow อัปเดตแถวเดิม — F7 มี จึงต้องเปลี่ยนเป็น `set`
ไม่งั้น `_repository.Update(...)` จะเซ็ต `UpdateDate` ไม่ได้และ compile ไม่ผ่าน

**ไม่มี `ReviewedBy` ในเฟสนี้** — ระบบยังไม่มี auth (TD-002) จึงไม่มีค่าที่เชื่อถือได้จะใส่
ถ้าจะเพิ่มต้องรอ auth · ดู `## Unresolved Open Questions`

### DM-4 · `ChatMessage` (แก้ของเดิม)

เปลี่ยนอย่างเดียว: `SessionId` → `LearningSessionId` (ความหมาย: ชี้ไป `LearningSession`)
ฟิลด์อื่นคงเดิมทั้งหมด (`SenderRole` · `SenderName` · `Text` + audit)

### DM-5 · `SessionSummary` — **ลบทั้ง entity** (✅ Q4 ยืนยัน 2026-08-18)

### DM-6 · status constants (ตาม convention `static class` + `const string` ห้ามใช้ C# enum)

```csharp
// SupportRoom.Domain/Enums/LearningStatus.cs — ใหม่
/// <summary>String constants ไม่ใช่ C# enum - ให้ตรงกับ TS union type ตัวเดียวกันเป๊ะ</summary>
public static class LearningStatus
{
    public const string InProgress = "IN_PROGRESS";
    public const string Ended = "ENDED";
}

// SupportRoom.Domain/Enums/ReviewResult.cs — ใหม่
public static class ReviewResult
{
    public const string Correct = "correct";
    public const string Incorrect = "incorrect";
}

// SupportRoom.Domain/Enums/LessonLinkStatus.cs — ใหม่ (คำนวณตอนแสดงผล ไม่มีคอลัมน์)
public static class LessonLinkStatus
{
    public const string Active = "ACTIVE";
    public const string Expired = "EXPIRED";
}
```

**`SessionStatus.cs` เดิม (`NOT_STARTED`/`IN_PROGRESS`/`ENDED`/`EXPIRED`) ให้ลบทิ้ง** — ค่าของมัน
ถูกแยกไปอยู่สองที่คนละความหมายแล้ว (ลิงก์ = `LessonLinkStatus`, การเรียน = `LearningStatus`)
การคงไว้จะทำให้มีคนหยิบไปใช้ผิดฝั่ง

### DM-7 · `ApplicationDbContext.OnModelCreating` (ส่วนที่เปลี่ยน)

```csharp
public DbSet<LessonLink> LessonLink => Set<LessonLink>();
public DbSet<LearningSession> LearningSession => Set<LearningSession>();
// ลบ: public DbSet<SessionSummary> SessionSummary

builder.Entity<LessonLink>(entity =>
{
    entity.HasKey(x => x.Id);
    // Token ยังต้อง unique ทั้งระบบ - เป็น public join secret ที่ถูก lookup ก่อนรู้ company
    // (GetByToken ข้าม filter) จึงห้ามชนกันข้าม company
    entity.HasIndex(x => x.Token).IsUnique();
    entity.HasIndex(x => x.CompanyId);
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});

builder.Entity<LearningSession>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.CompanyId);
    entity.HasIndex(x => x.LessonLinkId);
    // ใช้โดย resume lookup (LR-3) ซึ่งยิงทุกครั้งที่ผู้เรียนเปิดลิงก์ - ไม่ unique เพราะ
    // คนเดิมเรียนซ้ำได้หลายรอบบนลิงก์เดียวกัน (F5)
    entity.HasIndex(x => new { x.LessonLinkId, x.LearnerKey });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});

builder.Entity<SessionQuestion>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.LearningSessionId);   // เดิม SessionId
    entity.HasIndex(x => x.CompanyId);
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});

builder.Entity<ChatMessage>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.LearningSessionId);   // เดิม SessionId
    entity.HasIndex(x => x.CompanyId);
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});
```

`LessonConfig` และ `DocumentResource` **ไม่เปลี่ยน**

### DM-8 · Repository

| Repository | สถานะ | หมายเหตุ |
|---|---|---|
| `ITrainingSessionRepository` → `ILessonLinkRepository` | rename | คง `GetByToken` ที่ `IgnoreQueryFilters()` ไว้ **พร้อม XML doc เดิมทั้งย่อหน้า** — นั่นคือคำอธิบายว่าทำไมถึงข้าม filter ได้ ห้ามตัดทิ้งตอน rename |
| `ILearningSessionRepository` | **ใหม่** | ต้องมี `GetByIdAcrossCompanies(string id)` ที่ `IgnoreQueryFilters()` + XML doc อธิบายเหตุผลแบบเดียวกับ `GetByToken` (IC-2) · `GetResumable(string lessonLinkId, string learnerKey)` · `GetByLessonLinkId(string lessonLinkId)` |
| `ISessionQuestionRepository` | แก้ | `GetBySessionId` → `GetByLearningSessionId` |
| `IChatMessageRepository` | แก้ | เหมือนกัน |
| `ISessionSummaryRepository` | **ลบ** | ✅ ตาม Q4 (ยืนยัน 2026-08-18) |

ทุกตัวที่เพิ่ม/rename ต้องอัปเดต `UnitOfWork.Register` (ลืม = resolve ไม่ได้ตอน runtime)

---

## Learning Lifecycle Rules (F1 · F2 · F4 · F5 · F8) — contract

> engineer ไม่มีสิทธิ์ตัดสินกติกาเอง หัวข้อนี้ต้องตอบให้ครบ อ่านทั้งหัวข้อก่อนเขียน service

**LR-1 · สร้างการเรียนใหม่** (`POST /api/learning-sessions`, body `{ token, learnerKey, learnerName }`)

ลำดับบังคับ ห้ามสลับ:
1. `_linkRepository.GetByToken(token)` (ข้าม query filter) → ถ้า `null` → `GeneralException.NotFound("ลิงก์")`
2. `CompanyContext.Resolve(link.CompanyId)` **ก่อนแตะอย่างอื่นทุกกรณี** (IC-1)
3. ถ้า `link.ExpiresAt <= DateTime.UtcNow` → `GeneralException` ตัวใหม่
   `LinkExpired()` ข้อความไทย `"ลิงก์นี้หมดอายุแล้ว ไม่สามารถเริ่มเรียนใหม่ได้"` **ห้ามสร้างแถว**
   (นี่คือครึ่งแรกของกติกาหมดอายุ ครึ่งหลังอยู่ที่ LR-3/LR-4/LR-5 ซึ่งไม่เช็ค expiry เลย)
4. `learnerName.Trim()` → ถ้าว่าง หรือยาวเกิน 80 ตัวอักษร → `GeneralException.ValidationError`
   ข้อความไทย `"กรุณากรอกชื่อ (ไม่เกิน 80 ตัวอักษร)"`
5. `learnerKey` ต้องไม่ว่าง และยาว 8–128 ตัวอักษร ไม่งั้น validation error
6. สร้างแถว: `Status = LearningStatus.InProgress` · `StartedAt = LastActivityAt = CreateDate = UtcNow`
   · `CompletedAllSlides = false` · `LastSlideObjectId = null` · `LastSlideIndex = null`
   · `TotalSlideCount = null` · `CompanyId = CurrentCompanyId`
7. `UnitOfWork.Commit()` แล้ว return ViewModel

**LR-2 · `MaxAttendees` ห้ามบังคับใช้** — ไม่มีการนับจำนวนการเรียนใต้ลิงก์ ไม่มีการเทียบกับ
`MaxAttendees` ไม่มี error case ใดที่อ้างถึงมัน ใน service **ห้ามมี `if` ที่แตะฟิลด์นี้เลย**
(มติ Declined 2026-08-11) หน้าที่เดียวของมันในเฟสนี้คือถูกเก็บและถูกแสดงกลับให้ CS

**LR-3 · เปิดลิงก์แล้วเช็คว่าเรียนต่อได้ไหม** (`GET /api/learning-sessions/resume?token=&learnerKey=`)

- lookup link ตาม LR-1 ข้อ 1–2 (**ไม่เช็ค expiry** — คนที่ค้างอยู่ต้องเรียนต่อจนจบได้ตามมติ)
- **ถ้า `learnerKey` ไม่ถูกส่งมาหรือเป็นค่าว่าง** (= `requirement.md` F3 **กรณี ก** — ล้าง storage /
  เปลี่ยนเครื่อง / เบราว์เซอร์ใหม่) → **ห้าม query การเรียนใดๆ** ตอบ
  `{ link, resumable: null, lastEnded: null, linkExpired }` ทันที · **ไม่ใช่ validation error**
  (ต่างจาก LR-1 ข้อ 5 ที่บังคับให้มี `learnerKey` เพราะที่นั่นกำลังจะสร้างแถว)
- `resumable` = แถวของ `(LessonLinkId, LearnerKey)` ที่ `Status = IN_PROGRESS`
  เรียง `CreateDate` มาก→น้อย เอาแถวแรก · ถ้าไม่มี = `null`
  — **นี่คือนิยามปฏิบัติการของ "การเรียนที่ยังไม่จบ" ตาม F3 กรณี ข** · แถวที่ `Status = ENDED`
  ไม่นับเป็น `resumable` ไม่ว่าจะครบสไลด์หรือไม่ และไม่ว่าจะจบไปนานแค่ไหน
- `lastEnded` = แถวของคู่เดียวกันที่ `Status = ENDED` เรียง `EndedAt` มาก→น้อย เอาแถวแรก · ไม่มี = `null`
- ตอบ **200 เสมอ** พร้อม `{ link, resumable, lastEnded, linkExpired }` — **ห้ามตอบ 404 เมื่อไม่มีแถว**
  (ไม่มีการเรียนเก่าไม่ใช่ error)
- `resumable`/`lastEnded` ใช้ `LearningSessionViewModel` ปกติ (มี `learnerName` ให้เอาไปเติมใน
  คำถามยืนยัน + `lastSlideIndex`/`totalSlideCount` ให้บอกว่าค้างอยู่ตรงไหน) ·
  **ไม่มี `learnerKey` ใน response ทุกกรณี**
- หน้าจอตัดสินใจจากผลลัพธ์ตามตารางนี้ (ห้าม frontend คิดกติกาเอง) — **`resumable` มาก่อนเสมอ**
  ถ้ามีทั้ง `resumable` และ `lastEnded` ให้ใช้สองแถวแรกและไม่ต้องสนใจ `lastEnded`:

| `resumable` | `lastEnded` | `linkExpired` | หน้าจอต้องแสดง |
|---|---|---|---|
| **มี** | – | false | **หน้ายืนยันตาม LR-3a (บังคับ)** — "คุณคือ *(`resumable.learnerName`)* ใช่ไหม" + ปุ่ม **ใช่ เรียนต่อ** + ปุ่ม **เริ่มเรียนใหม่ในชื่ออื่น** |
| **มี** | – | true | หน้ายืนยันเดียวกัน แต่ปุ่ม **"เริ่มเรียนใหม่ในชื่ออื่น" ถูกปิด** พร้อมข้อความ "ลิงก์นี้หมดอายุแล้ว เริ่มการเรียนใหม่ไม่ได้ แต่เรียนรอบที่ค้างอยู่ต่อจนจบได้" · ปุ่ม "ใช่ เรียนต่อ" ยังทำงานปกติ (**ห้ามพาไปหน้ากรอกชื่อที่กดส่งแล้วเจอ error จาก LR-1 ข้อ 3**) |
| ไม่มี | มี | false | "คุณเรียนบทเรียนนี้จบแล้ว" + ปุ่ม **ดูสรุป** + ปุ่ม **เรียนอีกครั้ง** (prefill ชื่อเดิม) · **ไม่ต้องถามยืนยัน — รอบเดิมจบแล้ว ไม่มีอะไรให้ resume** (การกด "เรียนอีกครั้ง" คือการสร้างแถวใหม่ตาม LR-6 จึงไม่มีทางเข้าไปในของคนก่อน) |
| ไม่มี | มี | true | เหมือนแถวบน แต่ปุ่ม "เรียนอีกครั้ง" ถูกปิด พร้อมข้อความ "ลิงก์นี้หมดอายุแล้ว" · ปุ่ม "ดูสรุป" ยังใช้ได้ |
| ไม่มี | ไม่มี | false | ฟอร์มกรอกชื่อ (F2) — ครอบทั้งคนใหม่จริงและ **กรณี ก** · ไม่ต้องถามยืนยัน |
| ไม่มี | ไม่มี | true | หน้า `/link-expired` |

**LR-3a · หน้ายืนยันก่อนเรียนต่อ (D2 ✅ ยืนยัน 2026-08-18) — บังคับ ห้ามข้ามด้วยเหตุผลใดๆ**

`requirement.md` F3 **กรณี ข** สั่งไว้ว่า "ห้ามพาเข้าไปเรียนต่อเงียบๆ" เพราะกุญแจอยู่ที่ *เบราว์เซอร์*
ไม่ใช่ที่ *คน* — เครื่องกลางในโรงเรียน/ห้องสมุดใช้กุญแจตัวเดียวกันทั้งวัน กติกาที่ต้องทำตาม:

1. **ถามทุกครั้งที่ `resumable` ไม่เป็น `null`** ไม่ว่าเพิ่งออกไปกี่นาที · ไม่มีเงื่อนไข "เพิ่งหลุดไปเมื่อกี้
   เลยข้ามคำถาม" และ **ห้ามเอา `INACTIVE_THRESHOLD_MINUTES` มาใช้ตรงนี้** (ค่านั้นเป็นของ F6 เท่านั้น)
2. **ถ้า `resumable` เป็น `null` ห้ามถาม** — ไม่มีอะไรให้ resume คำถามจะไม่มีคำตอบที่ถูก ·
   ครอบทั้ง **กรณี ก** (ไม่มีกุญแจ) และกรณีที่รอบเดิม **จบไปแล้ว** (มีแต่ `lastEnded`)
3. **กด "ใช่ เรียนต่อ"** → เข้าห้องด้วย `resumable.id` ตรงๆ · **ไม่มี endpoint สำหรับ "ยืนยัน"
   และไม่เขียน DB ณ จังหวะนี้** (`LastActivityAt` จะถูกอัปเดตเองเมื่อ LR-4 ยิงครั้งแรก)
4. **กด "เริ่มเรียนใหม่ในชื่ออื่น"** → หน้ากรอกชื่อ (F2) แล้วเรียก LR-1 ด้วย **`learnerKey` ตัวเดิม**
   (ไม่สร้างกุญแจใหม่ ไม่ล้าง `localStorage`) ได้แถวใหม่ · **ห้ามแตะแถวเดิมทุกกรณี** — ไม่ปิด ไม่ลบ
   ไม่เปลี่ยนสถานะ ไม่ทับชื่อ · ผลข้างเคียงที่ยอมรับแล้ว: บนเครื่องที่ใช้ร่วมกันจะมีหลายแถวค้างใต้
   กุญแจเดียวกัน และ `resumable` หยิบแถวที่ `CreateDate` ใหม่สุดเสมอ (คนก่อนหน้าจะไม่ถูกถามถึงอีก
   ซึ่งถูกต้อง — รายการของเขายังอยู่ครบให้ CS เห็น)
5. **ห้ามเก็บสถานะ "ยืนยันแล้ว" ลง `localStorage`/cookie/query string เพื่อข้ามคำถามครั้งถัดไป** —
   นั่นคือการ resume เงียบๆ ที่มติห้ามไว้ แค่ย้ายที่เก็บ · ขอบเขตที่ถูกต้อง: คำถามผูกกับ
   **การเปิดหน้า `join/[token]` ใหม่** (page load) ไม่ใช่ทุก render — กดยืนยันแล้วเดินต่อเข้าห้อง
   ในแท็บเดิมไม่ต้องถามซ้ำ
6. **ห้ามเสนอหรือเพิ่ม login / OTP / รหัสยืนยัน / การพิสูจน์ตัวตนใดๆ** เพื่อ "แก้ให้ดีกว่านี้" —
   ข้อห้ามมีผลบังคับตาม `requirement.md` (Declined 2026-08-11 ย้ำอีกครั้ง 2026-08-18) ·
   ชื่อยังเป็นป้ายกำกับ ไม่ใช่ identity การกดยืนยันคือการให้ผู้ใช้ *เลือก* ไม่ใช่การพิสูจน์ตัวตน

**LR-4 · บันทึกความคืบหน้า** (`PATCH /api/learning-sessions/{id}/progress`,
body `{ lastSlideObjectId, lastSlideIndex, totalSlideCount }`)

- ตรวจสิทธิ์ตาม IC-3 (ต้องมี `X-Learner-Key` ตรงกับแถว)
- ถ้าแถว `Status = ENDED` → **ไม่เขียนอะไร ตอบ 200 พร้อม ViewModel ปัจจุบัน**
  (progress ที่มาช้ากว่าการกดจบเป็นเรื่องปกติของ tutor engine ไม่ใช่ error — ห้ามตอบ 409)
- เขียน: `LastSlideObjectId` · `LastSlideIndex` · `LastActivityAt = UtcNow` · `UpdateDate = UtcNow`
- `TotalSlideCount` เขียน **เฉพาะเมื่อค่าที่ส่งมา non-null และ > 0** (กันการเขียนทับด้วย null/0
  ตอน deck ยังโหลดไม่เสร็จ)
- **ตั้ง `CompletedAllSlides = true`** เมื่อ `lastSlideIndex is not null && TotalSlideCount is not null
  && lastSlideIndex >= TotalSlideCount - 1` · **ห้ามตั้งกลับเป็น false ไม่ว่ากรณีใด** (ครั้งเดียวคือครั้งเดียว)
- ไม่เช็ค expiry (มติ: รายการที่ค้างอยู่เรียนต่อจนจบ)
- ความถี่: frontend เรียก **เฉพาะตอนเปลี่ยนสไลด์** เท่านั้น ไม่ต้องมี heartbeat แยก
  (สอดคล้องกับ F4 "ทุกครั้งที่เปลี่ยนสไลด์" และไม่สร้าง write volume เกินจำเป็น)

**LR-5 · กดจบ** (`PATCH /api/learning-sessions/{id}/end`,
body `{ completedAllSlides, lastSlideObjectId?, lastSlideIndex? }`)

- ตรวจสิทธิ์ตาม IC-3
- ถ้าแถว `Status = ENDED` อยู่แล้ว → **ไม่เขียนอะไร ตอบ 200 พร้อมค่าปัจจุบัน** (idempotent —
  ปุ่มถูกกดซ้ำ/`beforeunload` ยิงซ้ำได้จริง)
- ไม่งั้น: `Status = ENDED` · `EndedAt = LastActivityAt = UpdateDate = UtcNow`
  · `CompletedAllSlides = ค่าเดิม || ค่าที่ส่งมา` (OR ไม่ใช่ทับ)
  · เขียน `LastSlideObjectId`/`LastSlideIndex` เฉพาะเมื่อส่งมา non-null
- ไม่เช็ค expiry

**LR-6 · "เรียนอีกครั้ง"** = เรียก LR-1 ซ้ำด้วย `learnerKey` เดิมและ `learnerName` เดิม
(prefill ให้ผู้เรียนแก้ได้) → ได้แถวใหม่ **ห้ามแตะแถวเก่าทุกกรณี** ไม่ลบ ไม่ทับ ไม่เปลี่ยนสถานะ ·
ถ้าลิงก์หมดอายุแล้ว LR-1 ข้อ 3 จะปฏิเสธเอง ซึ่งถูกต้องตามมติ ("จบแล้วไม่ได้แปลว่าจบถาวร
แต่หมดอายุแล้วเริ่มใหม่ไม่ได้")

**LR-7 · การสร้างลิงก์ของ CS** (`POST /api/links`)
- DTO: `{ lessonSlug, recipientOrgName?, expiresAt?, maxAttendees? }`
  — **ตัด `recipientName` ออกจาก DTO** (F1: CS ไม่กรอกชื่อผู้เรียนอีกต่อไป)
- `expiresAt` ว่าง → `UtcNow + ServerDefaults.GetDefaultSessionExpiryHours()` (คงพฤติกรรมเดิม)
- `maxAttendees` ถ้าส่งมาต้อง `>= 1` ไม่งั้น validation error · null = ไม่จำกัด
- ไม่มี `Status` ให้เซ็ตอีกต่อไป

**LR-8 · endpoint เดิมที่ต้องหายไป**
`PATCH /api/sessions/{token}` (`action=start` / `action=end`) **ลบทิ้ง** — งานของมันย้ายไป
LR-1 (start เกิดพร้อมการสร้างแถว) และ LR-5 (end) · `MarkStarted` ใน service ลบทิ้งด้วย

---

## Progress & Stalled Rules (F6) — contract

**SR-1 · env** เพิ่มใน `ServerDefaults.cs` ตาม pattern เดิมเป๊ะ (ค่าว่างต้อง fallback เหมือนไม่ตั้ง):

```csharp
public static int GetInactiveThresholdMinutes() =>
    NumberEnv("INACTIVE_THRESHOLD_MINUTES", TutorConfig.DefaultInactiveThresholdMinutes);
// ใน TutorConfig: public const int DefaultInactiveThresholdMinutes = 30;
```
เพิ่มบรรทัดใน `backend/src/SupportRoom.Api/.env.example` ด้วย

**SR-2 · คำนวณที่ backend ตอน map ViewModel ที่เดียว** — ไม่ให้ frontend คำนวณ

```
IsStalled = Status == LearningStatus.InProgress
            && (DateTime.UtcNow - LastActivityAt).TotalMinutes > GetInactiveThresholdMinutes()
```
เหตุผลที่ไม่ให้ frontend คิด: ค่า threshold อยู่ใน env ฝั่ง server ถ้าให้ frontend คำนวณต้องส่งค่า
config ออกไปด้วย แล้วจะมีสูตรสองชุดที่มีวันไม่ตรงกัน · ViewModel ส่งทั้ง `lastActivityAt` (ให้แสดงเวลา)
และ `isStalled` (ให้แสดง badge)

**SR-3 · แถวที่ `Status = ENDED` ไม่มีวัน stalled** ไม่ว่าเวลาผ่านไปเท่าไร · ไม่มีคอลัมน์ใดๆ
เก็บค่านี้ ไม่มี background job ไปแก้สถานะ

---

## Review Rules (F7) — contract

**RR-1 · endpoint** `PATCH /api/session-questions/{id}/review` body `{ reviewResult, reviewNote }`
— ฝั่ง CS เท่านั้น ไม่ต้องมี `X-Learner-Key`

**RR-2 · ค่าที่รับได้ของ `reviewResult`**: `"correct"` · `"incorrect"` · `null`
อย่างอื่นทั้งหมด (รวม `""`) → `GeneralException.ValidationError("ผลรีวิวไม่ถูกต้อง")`

**RR-3 · การเขียน**
- `reviewResult` non-null → เขียน `ReviewResult` + `ReviewedAt = UtcNow`
- `reviewResult` = null → **ล้างการรีวิวทั้งชุด**: `ReviewResult = null`, `ReviewNote = null`,
  `ReviewedAt = null` (ไม่ใช่เก็บ note ค้างไว้แบบไม่มีผล)
- `reviewNote`: `Trim()` แล้วถ้าว่าง → เก็บเป็น `null` · ยาวเกิน `DtoLimits.MaxTextLength` (2000)
  → validation error · เขียนได้เฉพาะเมื่อ `reviewResult` non-null
- ทุกครั้งเซ็ต `UpdateDate = UtcNow` (ต้องแก้ entity ตาม DM-3 ก่อน)
- แถวไม่มีอยู่ → `GeneralException.NotFound("คำถาม")`

**RR-4 · รีวิวซ้ำได้ไม่จำกัด** — CS เปลี่ยนใจได้ ทับค่าเดิม ไม่เก็บประวัติการรีวิว
(ประวัติการรีวิวไม่อยู่ในขอบเขตเฟสนี้)

**RR-5 · ViewModel สองตัว ห้ามใช้ตัวเดียวกัน** — นี่คือกติกากันข้อมูลภายในรั่วตาม F5

| ViewModel | ใช้ที่ไหน | มีอะไร |
|---|---|---|
| `LearnerQuestionViewModel` | หน้าสรุปฝั่งผู้เรียน | `id` · `slideObjectId` · `transcript` · `answer` · `answerStatus` · `createdAt` |
| `SessionQuestionViewModel` | หน้า CS เท่านั้น | ทุกอย่างข้างบน + `learningSessionId` + `reviewResult` + `reviewNote` + `reviewedAt` |

**ผู้เรียนต้องไม่เห็น `reviewResult`/`reviewNote`/`reviewedAt` และไม่เห็นรายการ "จุดที่ AI ตอบไม่ได้"**
— `unansweredPoints` คำนวณและส่งเฉพาะใน response ฝั่ง CS เท่านั้น

**RR-6 · `unansweredPoints` คำนวณสด** ทุกครั้งที่อ่าน:
`questions.Where(q => q.AnswerStatus == AnswerStatus.NotFound).Select(q => q.Transcript ?? q.Answer ?? "").Where(t => t != "")`
(สูตรเดียวกับที่ `ISessionSummaryService.Save` ใช้อยู่วันนี้ ยกมาทั้งดุ้น ไม่ต้องคิดใหม่)

---

## Isolation & Credential Rules (F3) — contract

> นี่คือหัวใจของ "คนที่สองบนลิงก์เดียวกันต้องไม่เห็นของคนแรก" ถ้าพลาดข้อใดข้อหนึ่ง
> ผลคือข้อมูลรั่วข้ามผู้เรียนแบบเงียบ ไม่มี error ให้เห็น

**IC-1 · company resolve จาก row เท่านั้น ไม่เคยจาก client** — ทุก request ฝั่งผู้เรียนต้อง
lookup `LessonLink` ด้วย token (หรือ `LearningSession` ด้วย id) แบบข้าม query filter **แล้วเรียก
`CompanyContext.Resolve(row.CompanyId)` ทันที ก่อน query อื่นทุกตัว** — เป็น pattern เดียวกับ
`ITrainingSessionService.LoadByTokenAndResolveCompany` ที่มีอยู่แล้ว ให้ทำตามเป๊ะ

**IC-2 · `LearningSession.Id` เป็น credential ระดับเดียวกับ `Token`** — repository ต้องมี
`GetByIdAcrossCompanies` ที่ `IgnoreQueryFilters()` พร้อม XML doc อธิบายว่าทำไมถึงข้ามได้
(id เป็น GUID เดาไม่ได้ + resolve company จากแถวที่เจอทันที) การใส่ comment นี้ไม่ใช่พิธีกรรม —
มันคือสิ่งที่กันคนถัดไปไม่ให้ก๊อป `IgnoreQueryFilters()` ไปใช้ที่อื่นโดยไม่เข้าใจ

**IC-3 · endpoint ฝั่งผู้เรียนต้องพก `X-Learner-Key`** — `/progress`, `/end`, `/summary`,
`GET /api/session-questions?learningSessionId=`, `GET /api/chat-messages?learningSessionId=`
เมื่อเรียกจากฝั่งผู้เรียน ต้องส่ง header `X-Learner-Key` และ service เทียบกับ
`LearningSession.LearnerKey` · **ไม่ตรง → ตอบ `GeneralException.NotFound` (404) ไม่ใช่ 403**
เพื่อไม่ยืนยันว่า id นั้นมีจริง

**IC-4 · `LearnerKey` สร้างที่ browser** ด้วย `crypto.randomUUID()` เก็บใน `localStorage`
คีย์ `supportroom.learnerKey` (คีย์เดียวต่อ browser ใช้ข้ามลิงก์ได้) — ไม่ต้องมี endpoint แจกคีย์
เหตุผลที่ยอมรับได้: การจะสวมรอยต้องเดา UUIDv4 (122 bit) **และ** มีลิงก์ที่ถูกต้องพร้อมกัน
· `LearnerKey` **ไม่เคยถูกใช้ตัดสิน company** (IC-1 ทำหน้าที่นั้น) จึงไม่เพิ่มพื้นผิวการรั่วข้ามบริษัท

**IC-5 · SignalR group key เปลี่ยนจาก `Token` เป็น `LearningSession.Id`** — ⚠️ **จุดรั่วอันดับหนึ่ง
ของเฟสนี้** วันนี้ `SessionHub.JoinSession(token)` จับกลุ่มด้วย token และ
`IRealtimeNotifier.NotifyChatMessageAsync(session.Token, ...)` /
`NotifyNewQuestionAsync(session.Token, ...)` broadcast เข้ากลุ่มนั้น เมื่อลิงก์เดียวมีหลายผู้เรียน
**ทุกคนบนลิงก์เดียวกันจะได้รับ chat และคำถามของกันและกันทันที** ซึ่งขัด F3 ตรงๆ

ต้องเปลี่ยนพร้อมกันทั้งชุด:
- `JoinSession(string token)` → `JoinLearning(string learningSessionId)` — validate ว่าแถวมีจริง
  (ผ่าน `GetByIdAcrossCompanies` + resolve company) ไม่มี → `HubException` ข้อความไทย
- `SendChatMessage(token, ...)` → `SendChatMessage(learningSessionId, ...)`
- `IRealtimeNotifier` ทั้งสอง method รับ `learningSessionId` แทน `token`
- `VoiceQuestionService` broadcast ด้วย `learningSessionId` แทน `session.Token`
- `useSessionChat(token, ...)` → `useSessionChat(learningSessionId, ...)` ทั้งฝั่งห้องเรียนและฝั่ง CS

**IC-6 · `POST /api/voice-question` ต้องรับ `learningSessionId` แทน `token`** — วันนี้รับ `token`
แล้วผูกคำถามกับ session ที่ token ชี้ ถ้าไม่แก้ คำถามของทุกคนบนลิงก์เดียวกันจะกองรวมกันที่เดียว
· ยังคงต้อง resolve company จากแถว (IC-1) และยังคงสร้าง Pinecone namespace จาก
`CurrentCompanyId + LessonSlug` เหมือนเดิม (`LessonSlug` อ่านจาก `LessonLink` ที่การเรียนนั้นผูกอยู่)

**IC-7 · ห้าม auto-resume จาก client state — ทางเข้าห้องมีทางเดียวคือผ่าน LR-3 + LR-3a**
(D2 ✅ ยืนยัน 2026-08-18 · `requirement.md` F3 กรณี ข)

- **ห้ามเก็บ `learningSessionId` ไว้ใน `localStorage`/cookie แล้วพาเข้าห้องเองโดยไม่ถาม** ·
  ค่าเดียวที่ browser เก็บถาวรได้คือ `supportroom.learnerKey` (IC-4) เท่านั้น
- เปิด `/room/[token]` ตรงๆ โดยไม่มี `learningSessionId` ที่ผ่านการยืนยันมาในรอบนั้น →
  **ต้องส่งกลับไป `join/[token]` ให้ผ่าน LR-3 ก่อนเสมอ** ห้ามหยิบแถว `IN_PROGRESS` ล่าสุดมาเข้าห้องเอง
- เหตุผลที่กติกานี้อยู่ในหัวข้อ isolation ไม่ใช่แค่ UX: บนเครื่องที่ใช้ร่วมกัน การ auto-resume
  **คือช่องทางที่ทำให้คนที่สองเห็นความคืบหน้าและคำถาม-คำตอบของคนแรก** ซึ่งเป็นสิ่งเดียวกับที่ IC-5/IC-6
  ป้องกันอยู่ฝั่ง server — ต่างกันแค่ว่าช่องนี้เปิดจากฝั่ง frontend
- ทางฝั่ง server ไม่มีอะไรบังคับข้อนี้ได้ (`X-Learner-Key` ถูกต้องทั้งสองกรณี) **จึงเป็นจุดที่ QA
  ต้องทดสอบด้วยมือ**: เปิดลิงก์ → กรอกชื่อ → ออกกลางคัน → เปิดลิงก์เดิมบนเบราว์เซอร์เดิมอีกครั้ง
  ต้องเจอหน้ายืนยันทุกครั้ง ไม่ใช่ถูกพาเข้าห้องเลย

---

## API & SignalR Contract Delta

> ทุกแถวคือจุดที่ **backend และ frontend ต้องแก้คู่กัน** (wire contract เป็น camelCase,
> TS type ต้องอัปเดตพร้อม ViewModel เสมอ ตาม Architecture Rule 7)
> path ในตารางเป็นชื่อที่ **ยืนยันแล้วตาม D1 เมื่อ 2026-08-18** — ใช้ `/api/links` และ
> `/api/learning-sessions` ตรงตัว

### REST

| เดิม | ใหม่ | หมายเหตุ |
|---|---|---|
| `GET /api/sessions` | `GET /api/links` | ViewModel เพิ่ม aggregate: `learnerCount` · `inProgressCount` · `endedCount` (CS ต้องเห็นภาพรวมต่อลิงก์) · `status` = `ACTIVE`/`EXPIRED` คำนวณจาก `expiresAt` |
| `POST /api/sessions` | `POST /api/links` | DTO ตัด `recipientName` เพิ่ม `maxAttendees` |
| `GET /api/sessions/{token}` | `GET /api/links/{token}` | ตอบ `{ link, lessonTitle }` — ไม่มี `status`/`recipientName`/`startedAt`/`endedAt`/`completedAllSlides`/`lastSlideObjectId` อีกต่อไป |
| `GET /api/sessions/{id}/by-id` | `GET /api/links/{id}/by-id` | ใช้ภายใน admin คงไว้ |
| **`PATCH /api/sessions/{token}`** | **ลบทิ้ง** | ย้ายไป LR-1 / LR-5 |
| **`GET /api/sessions/{token}/summary`** | **ลบทิ้ง** | ✅ ตาม Q4 · แทนด้วยสองเส้นข้างล่าง |
| – | `POST /api/learning-sessions` | LR-1 · body `{ token, learnerKey, learnerName }` |
| – | `GET /api/learning-sessions/resume?token=&learnerKey=` | LR-3 · `learnerKey` ว่างได้ (กรณี ก) → ตอบ `resumable: null` ไม่ใช่ error · ผลลัพธ์ป้อนหน้ายืนยัน LR-3a |
| – | `PATCH /api/learning-sessions/{id}/progress` | LR-4 · ต้องมี `X-Learner-Key` |
| – | `PATCH /api/learning-sessions/{id}/end` | LR-5 · ต้องมี `X-Learner-Key` |
| – | `GET /api/learning-sessions/{id}/summary` | หน้าสรุปฝั่งผู้เรียน · ต้องมี `X-Learner-Key` · ใช้ `LearnerQuestionViewModel` (RR-5) · **ไม่มี** `unansweredPoints` |
| – | `GET /api/links/{linkId}/learning-sessions` | CS: รายการการเรียนทุกรายการใต้ลิงก์ + `isStalled` (SR-2) |
| – | `GET /api/learning-sessions/{id}` | CS: รายละเอียดเต็ม + questions (`SessionQuestionViewModel`) + `unansweredPoints` |
| – | `PATCH /api/session-questions/{id}/review` | RR-1 |
| `GET /api/session-questions?token=` | `GET /api/session-questions?learningSessionId=` | |
| `GET /api/chat-messages?token=` | `GET /api/chat-messages?learningSessionId=` | |
| `POST /api/voice-question` (field `token`) | field `learningSessionId` แทน `token` | IC-6 |

### SignalR (`/hubs/session`)

| เดิม | ใหม่ |
|---|---|
| `JoinSession(token)` | `JoinLearning(learningSessionId)` |
| `SendChatMessage(token, senderRole, senderName, text)` | `SendChatMessage(learningSessionId, senderRole, senderName, text)` |
| group key = `Token` | group key = `LearningSession.Id` (IC-5) |
| event `ReceiveChatMessage` / `ReceiveNewQuestion` | ชื่อ event **คงเดิม** payload เปลี่ยน field `sessionId` → `learningSessionId` |

### TypeScript types (`frontend/src/types/domain.ts`)

```ts
// เปลี่ยนชื่อ + ตัดฟิลด์
export type LessonLink = {
  id: string; token: string; lessonId: string; lessonSlug: string;
  recipientOrgName?: string; expiresAt: string; maxAttendees?: number;
  status: LessonLinkStatus;      // "ACTIVE" | "EXPIRED" — คำนวณที่ backend
  learnerCount: number; inProgressCount: number; endedCount: number;
  createdAt: string;
};

export type LearningStatus = "IN_PROGRESS" | "ENDED";

export type LearningSession = {
  id: string; lessonLinkId: string; learnerName: string;
  status: LearningStatus;
  startedAt: string; endedAt?: string; lastActivityAt: string;
  lastSlideObjectId?: string; lastSlideIndex?: number; totalSlideCount?: number;
  completedAllSlides: boolean;
  isStalled: boolean;            // SR-2 — คำนวณที่ backend
  createdAt: string;
};
// หมายเหตุ: learnerKey ไม่เคยอยู่ใน ViewModel ที่ส่งออก — browser เก็บของตัวเองอยู่แล้ว
// การส่งกลับมาเท่ากับแจกกุญแจของคนอื่นให้ทุกคนที่เปิดหน้า CS

export type ReviewResult = "correct" | "incorrect";

export type SessionQuestion = { /* ...เดิม... */
  learningSessionId: string;     // เดิม sessionId
  reviewResult?: ReviewResult; reviewNote?: string; reviewedAt?: string;
};

export type LearnerQuestion = Omit<SessionQuestion,
  "learningSessionId" | "reviewResult" | "reviewNote" | "reviewedAt">;

export type ChatMessage = { /* ...เดิม... */ learningSessionId: string; };

// ลบ: TrainingSession · SessionStatus · SessionSummary · CreateSessionInput · EndSessionInput
```

### ไฟล์ frontend ที่ต้องแก้ (สำรวจแล้ว ไม่ใช่การเดา)

| ไฟล์ | เปลี่ยนอะไร |
|---|---|
| `lib/api-client.ts` | ทุกฟังก์ชันของ session (7 ตัว) + เพิ่มใหม่ 8 ตัว + header `X-Learner-Key` |
| `types/domain.ts` | ตามบล็อกข้างบน |
| `utils/session-status.ts` | `getSessionStatus`/`isSessionJoinable` เขียนใหม่ให้ทำงานกับลิงก์ + เพิ่ม label ของ `LearningStatus` |
| `hooks/use-session-chat.ts` | รับ `learningSessionId` แทน `token` · `invoke("JoinLearning", ...)` |
| `hooks/use-tutor-session.ts` | `persistEnd` → LR-5 · `askVoiceQuestion` ส่ง `learningSessionId` · **เพิ่มการยิง LR-4 ทุกครั้งที่ `currentSlideIndex` เปลี่ยน** พร้อม `totalSlides` · เลิกเรียก `markSessionStarted` |
| `app/join/[token]/page.tsx` | เพิ่มฟอร์มชื่อ + เรียก LR-3 แล้วแตกหน้าจอตาม **6 กรณีในตาราง LR-3** · **รวมหน้ายืนยัน LR-3a (บังคับ)** |
| `app/room/[token]/page.tsx` | รับ `learningSessionId` ที่ **ผ่านการยืนยันจากหน้า join แล้วเท่านั้น** · ถ้าเปิดตรงๆ โดยไม่มี → redirect กลับไป `join/[token]` ให้ผ่าน LR-3 · **ห้าม resolve จาก localStorage เอง** (IC-7) · ส่งต่อให้ hook ทั้งสอง |
| `app/session-ended/page.tsx` | เปลี่ยนเป็นหน้าสรุปผู้เรียน (Q&A ของตัวเอง + ปุ่มเรียนอีกครั้ง) หรือแยกเป็น route ใหม่ `/summary/[learningSessionId]` |
| `app/admin/page.tsx` | รายการลิงก์ + จำนวนคนเรียน |
| `app/admin/sessions/[token]/page.tsx` | เปลี่ยนจาก "สรุป 1 session" เป็น "รายการการเรียนใต้ลิงก์นี้" |
| **ใหม่** `app/admin/learning/[id]/page.tsx` | หน้ารีวิวของ CS (F7) |
| `components/admin/CreateSessionModal.tsx` | ตัดช่องชื่อผู้เรียน เพิ่ม `maxAttendees` + ข้อความ "ค่านี้ยังไม่มีผลในระบบ" (F8) |
| `components/meeting/ParticipantTile.tsx` | `recipientName` → ชื่อจากการเรียน |

---

## Migration Plan

**สรุปน้ำหนักของการ migrate ข้อมูลเดิม: ต่ำมาก** — ยืนยันแล้วว่าไม่มี production database
(ไม่มี Dockerfile/CI, roadmap 1.4 ยังไม่ทำ) มีแต่ DB ของ dev/demo ในเครื่อง

### D3 ✅ (ยืนยัน 2026-08-18) — migrate ข้อมูล demo เดิม

- **มติ: migrate** ด้วย backfill SQL ในตัว migration เดียวกัน เหตุผล: (ก) `requirement.md`
  ระบุ "migration ย้ายข้อมูลเดิม" ไว้ใน MVP ของ F1b (ข) มันคือ SQL ~15 บรรทัด (ค) ทำให้ข้อมูล demo
  ที่ทีมใช้ทดสอบไม่หายไปกลางทาง — คนทดสอบหน้า CS ใหม่จะมีข้อมูลให้ดูทันที
- ~~ทางเลือก: migration แบบทำลาย (drop คอลัมน์/ตารางทิ้งเลย)~~ — **ถูกตัดออกด้วยมติ 2026-08-18**
  ข้อ 4 และ 6 ของลำดับ `Up()` จึงเป็นส่วนบังคับ ห้ามข้าม

### ลำดับใน migration เดียว `SplitLessonLinkAndLearningSession`

**ต้องเป็น migration เดียว** เพื่อไม่ให้ DB ค้างอยู่ในสถานะแยกครึ่ง และ **ห้ามแก้ migration เดิม
6 ตัวที่มีอยู่** (กฎ root `CLAUDE.md` ข้อ 6)

```
dotnet ef migrations add SplitLessonLinkAndLearningSession \
  --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

⚠️ **EF จะ generate `DropTable("TrainingSession")` + `CreateTable("LessonLink")` ให้โดยอัตโนมัติ
ซึ่งทำให้ข้อมูลหายทั้งตาราง** — ต้องแก้มือเป็น `RenameTable` + `RenameIndex` เสมอ
นี่คือจุดที่พลาดบ่อยที่สุดของ EF rename

ลำดับใน `Up()` หลังแก้มือ (ลำดับนี้เป็น contract — SQL ต้องอ้างชื่อตาม *สถานะ ณ จุดนั้น*):

1. `RenameTable("TrainingSession" → "LessonLink")` + `RenameIndex` ทั้งสองตัว
   (`IX_TrainingSession_Token` → `IX_LessonLink_Token`, `..._CompanyId` เช่นกัน)
2. `AddColumn<int>("MaxAttendees", "LessonLink", nullable: true)`
3. `CreateTable("LearningSession", ...)` + `CreateIndex` 3 ตัว (`CompanyId`, `LessonLinkId`,
   `(LessonLinkId, LearnerKey)`)
4. **backfill** `migrationBuilder.Sql(...)` — หนึ่งการเรียนต่อหนึ่งลิงก์เดิม
   ```sql
   INSERT INTO "LearningSession"
     ("Id","CompanyId","CreateDate","IsDelete","LessonLinkId","LearnerKey","LearnerName",
      "Status","StartedAt","EndedAt","LastActivityAt","LastSlideObjectId","LastSlideIndex",
      "TotalSlideCount","CompletedAllSlides")
   SELECT 'learning-legacy-' || "Id", "CompanyId", "CreateDate", false, "Id",
          'legacy-' || "Id", COALESCE("RecipientName", 'ไม่ระบุชื่อ'),
          CASE WHEN "Status" = 'ENDED' THEN 'ENDED' ELSE 'IN_PROGRESS' END,
          COALESCE("StartedAt", "CreateDate"), "EndedAt",
          COALESCE("EndedAt", "StartedAt", "CreateDate"),
          "LastSlideObjectId", NULL, NULL, "CompletedAllSlides"
   FROM "LessonLink";
   ```
5. `RenameColumn` `SessionQuestion.SessionId` → `LearningSessionId` + `RenameIndex`
   · เหมือนกันกับ `ChatMessage`
6. **repoint** `Sql`:
   `UPDATE "SessionQuestion" SET "LearningSessionId" = 'learning-legacy-' || "LearningSessionId";`
   และแบบเดียวกันกับ `ChatMessage` (ค่าที่ค้างอยู่คือ id ของลิงก์เดิม จึงเติม prefix ให้ตรงกับข้อ 4)
7. `AddColumn` `ReviewResult` (text, null) · `ReviewNote` (text, null) · `ReviewedAt` (timestamptz, null)
   บน `SessionQuestion`
8. `DropColumn` บน `LessonLink` 6 คอลัมน์: `RecipientName` · `Status` · `StartedAt` · `EndedAt`
   · `CompletedAllSlides` · `LastSlideObjectId`
9. `DropTable("SessionSummary")` ✅ ตาม Q4 (ยืนยัน 2026-08-18)

`Down()` เขียนย้อนกลับได้เชิงโครงสร้าง แต่ **ข้อมูลใน `SessionSummary` กู้คืนไม่ได้** —
ยอมรับได้เพราะไม่มีข้อมูลจริง ให้ใส่คอมเมนต์บอกไว้ในไฟล์ migration ตรงๆ

รัน: `dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api`

### เอกสารที่ต้องตามไปแก้ตอน implement (delta ที่รู้แล้ว)

| ไฟล์ | delta |
|---|---|
| `docs/schema.dbml` | `TrainingSession` → `LessonLink` (ตัด 6 คอลัมน์ เพิ่ม `MaxAttendees`) · เพิ่ม `Table LearningSession` · **ลบ `Table SessionSummary`** · แก้ `Ref:` ของ `SessionQuestion`/`ChatMessage` ให้ชี้ `LearningSession` · เพิ่ม `Ref: LearningSession.LessonLinkId > LessonLink.Id` · แก้ `TableGroup session_runtime` · **ไฟล์นี้ยังเขียนว่า `TeacherName`/`SchoolName` และ "ยังไม่มี CompanyId" ซึ่งล้าสมัยไปแล้วตั้งแต่ migration `AddCompanyId` (11 ส.ค.) — อัปเดตให้ตรงของจริงไปพร้อมกัน** |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` · `frontend/docs/ER_DIAGRAM.md` | ตารางใหม่ + ความสัมพันธ์ใหม่ |
| `frontend/docs/API_CONTRACT.md` | ทุกแถวใน `## API & SignalR Contract Delta` |
| `frontend/docs/SYSTEM_LOGIC.md` · `USE_CASE_DIAGRAM.md` · `SEQUENCE_DIAGRAMS.md` | flow ใหม่ (กรอกชื่อ → เรียน → จบ → เรียนอีกครั้ง) |
| `frontend/docs/STATE_MACHINE.md` | ถ้า tutor reducer มี event ใหม่ (progress/end) |
| `docs/PROJECT_CONTEXT.md` | ER + API map + หนี้เทคนิค |
| `backend/src/SupportRoom.Api/.env.example` | `INACTIVE_THRESHOLD_MINUTES=30` |
| `docs/TECH_DECISIONS.md` | บันทึกการตัดสิน Q2/Q3/Q4 เป็น TD ใหม่ (ตาม Solution Design Rule ข้อ 8) |
| `docs/CORE_FEATURE_SPEC.md` | **ห้ามแก้** — เป็นบันทึกประวัติ |

### Test ที่กระทบ

| ไฟล์ | ทำอะไร |
|---|---|
| `CompanyIsolationTests.cs` | เพิ่ม seed `LearningSession` · `EveryEntityIsCompanyScoped` จะ fail เองถ้าลืม query filter (**tripwire ที่ต้องปล่อยให้ทำงาน อย่าแก้ test ให้ผ่าน**) · `LookingUpASessionByToken...` เปลี่ยนชื่อ repository |
| `SessionSummaryServiceTests.cs` | **ลบทั้งไฟล์** ✅ ตาม Q4 |
| `TrainingSessionServiceTests.cs` | rename + ตัดการทดสอบ `MarkStarted`/`End` ที่ย้ายไปการเรียน |
| `AdminServiceTests.cs` | `ResetDemoData` ต้องลบ `LearningSession` ด้วย และเลิกลบ summary |
| `Fakes/ServiceTestFakes.cs` | ตัด fake ของ summary service |
| **ใหม่** `LearningSessionServiceTests.cs` | ครอบ LR-1 ถึง LR-6 อย่างน้อย: หมดอายุแล้วสร้างใหม่ไม่ได้ · หมดอายุแล้วรายการค้างยังจบได้ · resume ได้เฉพาะ key ตัวเอง · `X-Learner-Key` ผิด → 404 · end ซ้ำไม่พัง · `CompletedAllSlides` ไม่ถูกตีกลับเป็น false · **LR-3: `learnerKey` ว่าง → `resumable = null` ไม่ใช่ error · แถวที่ `ENDED` ไม่โผล่เป็น `resumable` · "เริ่มใหม่ในชื่ออื่น" ไม่แตะแถวเดิม** |

---

## Modules

> เป็น "Module" แบบ sub-grouping ภายใน module folder `learning-session` เดียว
> (ตาม `.claude/shared/conventions.md` §1) — ไม่ได้เสนอให้แยก folder ใหม่
> การแบ่งนี้เพื่อให้ `project-manager` แตกเป็นเฟสได้ตรงลำดับพึ่งพา ไม่ใช่การแบ่งงานส่งมอบแยกกัน

### Module A — Data foundation & migration
`LessonLink` (rename + ตัด 6 คอลัมน์ + `MaxAttendees`) · `LearningSession` (ตารางใหม่) ·
`SessionQuestion` (rename FK + 3 คอลัมน์รีวิว + แก้ audit เป็น `set`) · `ChatMessage` (rename FK) ·
ลบ `SessionSummary` ทั้งชุด · status constants 3 ตัว · repository + `UnitOfWork.Register` ·
`ApplicationDbContext` · migration เดียวพร้อม backfill · `INACTIVE_THRESHOLD_MINUTES` ใน `ServerDefaults`
**Dependencies:** ไม่มี — ต้องเสร็จก่อนทุก module
**Sensitive:** query filter ของ entity ใหม่ = ขอบเขต multi-company · หลุด = ข้อมูลข้ามบริษัท

### Module B — Link management (ฝั่ง CS)
`ILessonLinkService` (create/list/get) · aggregate counts · สถานะ ACTIVE/EXPIRED คำนวณ ·
`POST/GET /api/links` · ฟอร์มสร้างลิงก์ (ตัดชื่อผู้เรียน เพิ่ม `MaxAttendees` + ข้อความกำกับ) ·
หน้ารายการลิงก์
**Dependencies:** A

### Module C — Learning lifecycle (ฝั่งผู้เรียน, API) 🔒
LR-1 ถึง LR-8 ทั้งชุด · `ILearningSessionService` · endpoint `/api/learning-sessions/*` ·
การบังคับ expiry ที่ backend เป็นครั้งแรก · SR-1..SR-3
**Dependencies:** A
**Sensitive:** รับ input จากภายนอกที่ไม่ผ่าน auth (ชื่อผู้เรียน + `learnerKey`) · `LearnerKey`
และ `LearningSession.Id` เป็น bearer credential · การบังคับสิทธิ์ระหว่างผู้เรียน (IC-3) อยู่ที่นี่

### Module D — Conversation re-pointing & realtime 🔒
`SessionQuestion`/`ChatMessage` ย้ายไปผูกการเรียน · `POST /api/voice-question` เปลี่ยนเป็น
`learningSessionId` (IC-6) · **SignalR group key เปลี่ยนเป็น learning id (IC-5)** ·
`IRealtimeNotifier` · `useSessionChat`
**Dependencies:** A, C
**Sensitive:** **จุดรั่วข้ามผู้เรียนอันดับหนึ่งของทั้งโมดูล** — group key ที่ยังเป็น token
จะ broadcast บทสนทนาของผู้เรียนคนหนึ่งไปหาทุกคนบนลิงก์เดียวกันโดยไม่มี error ให้เห็น

### Module E — Learner-facing UI 🔒
หน้ากรอกชื่อ + 6 กรณีของ LR-3 · **หน้ายืนยันก่อนเรียนต่อ (LR-3a — D2)** · localStorage
`LearnerKey` (IC-4) · ห้องเรียนส่ง progress · ปุ่มกดจบเอง · หน้าสรุปผู้เรียน
(`LearnerQuestionViewModel` เท่านั้น) · ปุ่ม "เรียนอีกครั้ง"
**Dependencies:** C, D
**Sensitive:** หน้าสรุปฝั่งผู้เรียนต้องไม่แสดงผลรีวิว/จุดที่ AI ตอบไม่ได้ (RR-5) ·
**LR-3a + IC-7 บังคับได้ที่ module นี้ที่เดียว** — server แยกไม่ออกว่า resume ผ่านการยืนยันแล้วหรือไม่
ถ้าหน้ายืนยันหายไป ผลคือคนที่สองบนเครื่องที่ใช้ร่วมกันเห็นความคืบหน้าและคำถามของคนแรก
**🔒 Security gate (มติเจ้าของโปรเจกต์ 2026-08-18):** ติด gate ให้ module นี้ด้วย เพราะเป็น
จุดบังคับ LR-3a/IC-7 เพียงจุดเดียวในระบบ — ถ้าหน้ายืนยันหาย ข้อมูลรั่วข้ามผู้เรียนแบบเงียบ
ไม่มี error และ server ตรวจแทนไม่ได้

### Module F — CS console & review 🔒
รายการการเรียนใต้ลิงก์ + badge "หยุดกลางคัน" + "7/20" · หน้ารายละเอียดการเรียน ·
UI รีวิวถูก/ผิด + หมายเหตุ · `PATCH /api/session-questions/{id}/review`
**Dependencies:** C, D
**Sensitive:** หมายเหตุรีวิวเป็นข้อมูลภายในของ CS แต่ `/admin/*` และ `/api/*` ยังเปิดสาธารณะ
(TD-002) — ใครก็ตามที่เดา endpoint ได้จะอ่าน/เขียนรีวิวได้

---

## Risks & Dependencies

| # | ความเสี่ยง | ผลถ้าเกิด | การรับมือ (เป็นคำสั่ง ไม่ใช่ความเห็น) |
|---|---|---|---|
| R1 | **SignalR group key ยังเป็น token** | ผู้เรียนบนลิงก์เดียวกันเห็น chat/คำถามของกันและกัน — ขัด F3 โดยตรง ไม่มี error ให้เห็น | IC-5 ต้องทำครบทั้ง 6 จุดในคราวเดียว · QA ต้องทดสอบด้วย browser 2 ตัวบนลิงก์เดียวกันจริง ไม่ใช่แค่อ่านโค้ด |
| R2 | **`voice-question` ยังรับ token** | คำถามของทุกคนกองที่การเรียนเดียว หน้าสรุปผู้เรียนแสดงคำถามของคนอื่น | IC-6 · ตรวจคู่กับ R1 เสมอ ทั้งคู่มาจากรากเดียวกัน |
| R3 | **rename ครึ่งเดียว** | codebase มีทั้ง `TrainingSession` และ `LessonLink` ปนกัน อ่านไม่รู้เรื่อง กว่าจะรู้ตัวก็สายแล้ว | Module A ต้องจบเป็นก้อนเดียว ห้ามแบ่งครึ่ง · `dotnet build` + `npm run typecheck` ต้องผ่านตอนจบ module A |
| R4 | EF generate `DropTable`+`CreateTable` แทน rename | ข้อมูล demo หายทั้งตาราง | ระบุไว้ในแผน migration แล้ว — ต้องอ่านไฟล์ migration ที่ generate ออกมาด้วยตาก่อนรัน |
| R5 | ลืม query filter บน `LearningSession` | ข้อมูลอ่านข้ามบริษัทได้ | `CompanyIsolationTests.EveryEntityIsCompanyScoped` ดักให้แล้ว **ห้ามแก้ test ให้ผ่าน** |
| R6 | **backend เพิ่งเริ่มบังคับ expiry** | ลิงก์ที่ทีมใช้ทดสอบอยู่ (สร้างไว้นานแล้ว) จะเริ่มเรียนใหม่ไม่ได้ทันทีที่ deploy โค้ดใหม่ | ตั้งใจให้เป็นแบบนั้นตามมติ · แจ้งทีม CS ก่อน · ค่า default 24 ชม. จาก `DEFAULT_SESSION_EXPIRY_HOURS` ยังเหมือนเดิม |
| R7 | `MaxAttendees` ถูก enforce โดยไม่ตั้งใจ | ผิดมติ Declined 2026-08-11 | LR-2 เขียนเป็นข้อห้ามชัดเจน · UI ต้องมีข้อความกำกับ (F8) |
| R8 | เครื่องใช้ร่วมกัน → `LearnerKey` ชนกัน | คนที่สองบนคอมเครื่องเดียวกัน "เรียนต่อ" ของคนแรก และคำถามไปกองรวมกัน | **แก้แล้วด้วยมติ D2 (2026-08-18)** — LR-3a หน้ายืนยันก่อน resume + IC-7 ห้าม auto-resume จาก client state · **ความเสี่ยงที่เหลือ:** กติกานี้บังคับได้ที่ frontend เท่านั้น server แยกไม่ออก → QA ต้องทดสอบด้วยมือตาม IC-7 |
| R9 | ไม่มี auth (TD-002) | `/admin/*` + `/api/*` เปิดสาธารณะ หมายเหตุรีวิวภายในของ CS อ่านได้โดยคนนอก | **นอกขอบเขตเฟสนี้ตาม `requirement.md`** — แต่เป็นเหตุผลที่ Module C/D/F ต้องติด 🔒 Security gate · roadmap 1.2 |
| R10 | `LastActivityAt` เขียนถี่ | write volume เพิ่มตามจำนวนสไลด์ที่เปลี่ยน | ที่ scale ปัจจุบัน (การสาธิตทีละคน) ไม่มีผล · LR-4 บังคับให้ยิงเฉพาะตอนเปลี่ยนสไลด์ ไม่มี heartbeat |
| R11 | ปัญหาพื้นฐานที่ค้างอยู่แล้ว | Edge TTS ถูกบล็อกบน datacenter IP (TD-001) · in-memory queue (TD-003) · ไม่มี CI (TD-006) | **ไม่ใช่ของโมดูลนี้** อยู่ใน `PRODUCTION_ROADMAP.md` Phase 1 — ระบุไว้เพื่อไม่ให้ถูกนับรวมเป็นความเสี่ยงของเฟสนี้ |

**Dependencies ระหว่าง module:** A → (B, C) → D → (E, F) · A ต้องเสร็จก่อนทุกอย่าง ·
D ต้องเสร็จก่อน E และ F เพราะทั้งคู่ต้องใช้ chat/คำถามที่ผูกกับการเรียนแล้ว

**สิ่งที่ `project-manager` ต้องรับไปทำต่อ:** phase ที่ครอบ Module C, D, **E**, F ต้องมี
`🔒 Security gate` ที่หัวข้อ phase ตามเหตุผลที่ระบุในแต่ละ module (ไม่ใช่ด้วยเหตุผล PII
ซึ่งถูกตัดออกโดยตั้งใจตาม F2) · **E เพิ่มเข้ามาตามมติเจ้าของโปรเจกต์ 2026-08-18**

---

## Unresolved Open Questions

> **ไม่มีคำถามค้างแล้ว — พร้อมส่งต่อ `project-manager`**
> 6 ข้อที่เคยอยู่ในหัวข้อนี้ (Q2/Q3/Q4 + D1–D3) **เจ้าของโปรเจกต์เคาะครบเมื่อ 2026-08-18
> ตรงตามข้อเสนอทุกข้อ** · เก็บตารางไว้เป็นบันทึกมติ ไม่ใช่รายการรอคำตอบ ·
> ส่วน "ที่ตัดออกจากเฟสนี้โดยตั้งใจ" ข้างล่าง **ยังมีผลบังคับเต็มที่**

### มติที่ปิดแล้ว (ยืนยัน 2026-08-18 โดยเจ้าของโปรเจกต์)

| # | คำถามเดิม | ✅ มติ | อยู่ในเอกสารที่ไหน |
|---|---|---|---|
| **Q2** | rename `TrainingSession` ไหม | **rename → `LessonLink`** | DM-1 · DM-7 · DM-8 · Migration Plan ข้อ 1 |
| **Q3** | ชื่อตารางใหม่ | **`LearningSession`** | DM-2 · DM-6 · DM-7 |
| **Q4** | `SessionSummary` | **ลบทิ้งทั้งใบ 13 จุด** | DM-5 · Migration Plan ข้อ 9 · API delta (แทนด้วย 2 endpoint ใหม่) |
| **D1** | route/TS type ตามชื่อใหม่ด้วยไหม | **ตามด้วย** (`/api/links`, `/api/learning-sessions`) | API & SignalR Contract Delta ทั้งหัวข้อ |
| **D2** | เครื่องใช้ร่วมกัน → resume แบบไหน | **ถามยืนยันก่อน resume เสมอ** + ทางเลือก "เริ่มเรียนใหม่ในชื่ออื่น" | **LR-3 + LR-3a** (กติกาหลัก) · **IC-7** (ห้าม auto-resume จาก client) · Module E |
| **D3** | migrate ข้อมูล demo เดิมไหม | **migrate ด้วย backfill SQL** | Migration Plan ข้อ 4 และ 6 |

**การรื้อมติเหล่านี้ต้อง amend เอกสารนี้ก่อน** — engineer ที่เจอทางเลือกอื่นในหัวข้อ Q2+Q3/Q4/D1/D3
ให้อ่านเป็นบันทึกเหตุผล ไม่ใช่ทางเลือกที่ยังหยิบได้

**ที่ตัดออกจากเฟสนี้โดยตั้งใจ — อย่า implement โดยไม่ amend เอกสารนี้ก่อน**

- **`ReviewedBy` (ใครเป็นคนรีวิว)** — ไม่มี auth จึงไม่มีค่าที่เชื่อถือได้จะใส่ · รอ roadmap 1.2
- **ประวัติการรีวิว** — RR-4 ทับค่าเดิม ไม่เก็บว่าเคยรีวิวว่าอะไรมาก่อน
- **การ merge สองการเรียนที่จริงๆ เป็นคนเดียวกัน** — ยอมรับแล้วตามมติ F3 (ห้ามเสนอ login/OTP)
- **การเก็บกวาดแถว `IN_PROGRESS` ที่ค้างสะสม** — ผลพวงที่ยอมรับแล้วของมติ D2: ทุกครั้งที่ผู้ใช้
  เลือก "เริ่มเรียนใหม่ในชื่ออื่น" แถวเดิมจะค้างเป็น `IN_PROGRESS` ตลอดไป (LR-3a ข้อ 4) ·
  **ห้ามเขียน background job / auto-end / TTL มาปิดแถวเหล่านี้ในเฟสนี้** — F6 แสดงเป็น
  "หยุดกลางคัน" ให้ CS เห็นอยู่แล้ว และการปิดอัตโนมัติจะขัดมติ "ให้รายการที่ค้างเรียนต่อจนจบ"
- **`MaxAttendees` enforcement** — Declined 2026-08-11
- **Event log ต่อสไลด์ / dashboard คนหลุดสไลด์ไหน** — Declined 2026-08-11 (`LastSlideIndex`
  เก็บแค่จุดล่าสุด ไม่ใช่ทุกก้าว)
- **สถานะ "จัดการแล้ว" ในรายการรีวิว** — Declined 2026-08-11
- **label "หน่วยงาน" ต่อบริษัท** — Declined 2026-08-11 (รอตาราง `Company` + auth)
- **ห้องกลุ่ม** — Declined 2026-08-11 (ลิงก์คนละชนิด)
- **การลบ/ซ่อนการเรียนโดย CS** — ไม่มีใน requirement · ปัจจุบันมีแค่ `ResetDemoData` ที่ลบทั้งหมด
- **บังคับ session expiry ฝั่ง SignalR hub** — hub ยัง join ได้แม้ลิงก์หมดอายุ ซึ่งถูกต้องตามมติ
  (รายการที่ค้างอยู่ต้องเรียนต่อได้) ไม่ใช่ช่องโหว่ที่ต้องปิดในเฟสนี้

---

## Change Log

- 2026-08-18 — สร้างเอกสารครั้งแรกจาก `requirement.md` (ฉบับหลังเพิกถอนการพลิกเป็น 1:1) ·
  ประเมิน F1–F8 ครบ ทุกข้อทำได้ด้วย stack เดิมโดยไม่เพิ่ม dependency · เสนอ Data Model เต็ม
  (`LessonLink` + `LearningSession` ใหม่ + `SessionQuestion`/`ChatMessage` ย้าย FK + ลบ
  `SessionSummary`) · เขียน contract 4 ชุด (Learning Lifecycle · Progress & Stalled ·
  Review · Isolation & Credential) · ปิด Q2/Q3/Q4 เป็น **ข้อเสนอพร้อม trade-off รอผู้ใช้ยืนยัน**
  (subagent เรียก `AskUserQuestion` ไม่ได้ในสภาพแวดล้อมนี้) · พบและบันทึกเพิ่ม 3 เรื่องที่
  `requirement.md` ยังไม่ครอบ: `TotalSlideCount` จำเป็นต่อการแสดง "7/20" · SignalR group key
  ต้องเปลี่ยนเป็น learning id ไม่งั้นรั่วข้ามผู้เรียน · `LearnerKey` ชนกันบนเครื่องที่ใช้ร่วมกัน (D2) ·
  ยืนยันจากไฟล์จริงว่าระบบยังไม่ deploy (ไม่มี Dockerfile/CI, roadmap Phase 1 ยังไม่เริ่ม)
  ทำให้ต้นทุน rename/ลบตารางต่ำและเป็นเหตุผลหลักของข้อเสนอ Q2/Q4
- 2026-08-18 — **เจ้าของโปรเจกต์ยืนยันครบทั้ง 6 จุด ตรงตามข้อเสนอทุกข้อ** (Q2 `LessonLink` ·
  Q3 `LearningSession` · Q4 ลบ `SessionSummary` ทั้งใบ · D1 เปลี่ยน route/TS type ตาม ·
  D2 ถามยืนยันก่อน resume · D3 migrate ข้อมูล demo ด้วย backfill SQL) → **เอกสารเปลี่ยนสถานะจาก
  "รอเคาะ" เป็น contract ที่ implement ได้ทันที** · **เนื้อหา Data Model, contract 4 ชุด,
  API/SignalR delta และ Migration Plan ไม่เปลี่ยนแม้แต่จุดเดียว** เพราะเขียนบนสมมติฐานเหล่านี้อยู่แล้ว ·
  การปรับในรอบนี้: (1) ลบเครื่องหมาย `⏳ รอยืนยัน` ทุกจุด (banner หัวเอกสาร · Q2+Q3 · D1 · Q4 ·
  D3 · DM-5 · DM-8 · API delta · Migration Plan · Test) แล้วแทนด้วย ✅ พร้อมวันที่ยืนยัน
  (2) เปลี่ยนหัวข้อ `## Unresolved Open Questions` เป็น "ไม่มีคำถามค้าง" + ตารางมติที่ปิดแล้ว
  พร้อมชี้ว่าแต่ละมติไปอยู่ส่วนไหนของเอกสาร · บล็อก "ที่ตัดออกจากเฟสนี้โดยตั้งใจ" คงไว้ทั้งหมด
  ยังมีผลบังคับ
- 2026-08-18 — **ทำ D2 ให้เป็น contract ที่ engineer เดาไม่ได้ หลังอ่าน `requirement.md` F3
  ฉบับ amend (แยกกรณี ก / กรณี ข)** · เดิม LR-3 เขียนแค่ว่า "ตาม D2 ⏳" ซึ่งไม่พอสำหรับ implement ·
  เพิ่ม/แก้: (1) LR-3 ระบุว่า `learnerKey` ว่าง = **กรณี ก** → ตอบ `resumable: null` **ไม่ใช่
  validation error** และห้าม query (2) นิยาม `resumable` = `Status = IN_PROGRESS` เท่านั้น
  ผูกกับถ้อยคำ **"การเรียนที่ยังไม่จบ"** ของ F3 กรณี ข ตรงตัว (3) ตารางหน้าจอแตกจาก 5 เป็น 6 กรณี
  แยกกรณี `resumable` + ลิงก์หมดอายุ (ปิดปุ่ม "เริ่มใหม่ในชื่ออื่น" เพราะ LR-1 ข้อ 3 จะปฏิเสธ —
  เดิมกำกวมจน engineer อาจพาไปหน้ากรอกชื่อที่กดแล้วเจอ error) และระบุว่า `resumable` มาก่อน
  `lastEnded` เสมอ (4) **แถวที่รอบเดิมจบแล้ว (`lastEnded` อย่างเดียว) ระบุชัดว่า "ไม่ต้องถามยืนยัน"**
  ตามที่มติกำหนด — ไม่มีอะไรให้ resume (5) เพิ่ม **LR-3a** 6 ข้อ: ถามทุกครั้งที่มี `resumable`
  ห้ามใช้ threshold เวลามาข้ามคำถาม · ห้ามถามเมื่อไม่มี `resumable` · "ใช่" ไม่เขียน DB และไม่มี
  endpoint ยืนยัน · "เริ่มใหม่ในชื่ออื่น" ใช้ `learnerKey` เดิมและห้ามแตะแถวเดิม · ห้ามเก็บ flag
  "ยืนยันแล้ว" เพื่อข้ามคำถามครั้งหน้า · ห้ามเสนอ login/OTP (6) เพิ่ม **IC-7** ห้าม auto-resume
  จาก client state + แก้แถว `app/room/[token]/page.tsx` ที่เดิมเขียนว่า resolve `learningSessionId`
  จาก `localStorage` ได้ ซึ่ง**ขัดมติ D2 โดยตรง** (เป็นการ resume เงียบๆ ที่ย้ายที่เก็บ) ·
  server แยกไม่ออกว่ายืนยันแล้วหรือยัง จึงระบุให้เป็นจุดที่ QA ต้องทดสอบด้วยมือ
  (7) อัปเดต R8 จาก "ความเสี่ยงที่รอ D2" เป็น "แก้แล้ว + ความเสี่ยงที่เหลือ" · Module E ระบุว่าเป็น
  ที่เดียวที่บังคับ LR-3a/IC-7 ได้ · เพิ่มเคสทดสอบ LR-3 ใน `LearningSessionServiceTests.cs` ·
  เพิ่มข้อห้าม background job ปิดแถว `IN_PROGRESS` ที่ค้าง (ผลพวงที่ยอมรับแล้วของ D2)
  ลงในบล็อก "ที่ตัดออกจากเฟสนี้โดยตั้งใจ"
- 2026-08-18 — **เจ้าของโปรเจกต์เคาะคำถามค้างข้อสุดท้าย: Module E ติด `🔒 Security gate` ด้วย** ·
  เดิม gate อยู่ที่ C/D/F เท่านั้น และ Module E ถูกระบุไว้แค่ในช่อง Sensitive · เหตุผลของมติคือ
  หลัง D2 (ถามยืนยันก่อน resume เสมอ) **Module E เป็นจุดเดียวที่บังคับ LR-3a/IC-7 ได้** —
  `X-Learner-Key` ถูกต้องทั้งกรณี resume ที่ผ่านการยืนยันและกรณีที่ไม่ผ่าน server จึงแยกไม่ออก
  ถ้าหน้ายืนยันหายไปตอน implement ผลคือคนที่สองบนเครื่องที่ใช้ร่วมกันเห็นความคืบหน้าและ
  คำถาม-คำตอบของคนแรกโดยไม่มี error ให้เห็น · การแก้ในรอบนี้เป็น 3 จุด **ไม่แตะ contract ใดๆ**:
  (1) หัวข้อ `### Module E` เติม 🔒 (2) ช่อง Sensitive ของ Module E เพิ่มบรรทัดเหตุผลของ gate
  (3) บรรทัดส่งต่อ `project-manager` เปลี่ยนจาก "C, D, F" เป็น "C, D, E, F" ·
  **ผลต่อขั้นถัดไป:** `devops` จะ deploy phase ที่ครอบ Module E ไม่ได้จนกว่า `security` จะ audit
