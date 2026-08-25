# การเรียน 1 ลิงก์ = หลายการเรียน + บันทึกความคืบหน้า/รีวิวคำตอบ AI (learning-session) — Feasibility & Design

> **สถานะเอกสาร (2026-08-19):** **ยืนยัน Contract Amendment แล้ว — พร้อมส่งต่อ `project-manager`**
> ทั้ง 6 จุดที่เคยค้าง (Q2, Q3, Q4 + D1–D3) เคาะครั้งแรกเมื่อ 2026-08-18 และ Q2/D1 รวมถึง
> data/wire details ถูก amend เมื่อ 2026-08-19 ตาม implementation ที่ยืนยันแล้ว เนื้อหาในเอกสารนี้
> จึงเป็น contract ที่ implement ได้โดยอ่าน CA-1..CA-6 เป็น authority · หัวข้อทางเลือก/trade-off เดิม
> **เก็บไว้เป็นบันทึกเหตุผล ไม่ใช่คำถามที่ยังเปิดอยู่** — ห้ามรื้อทางเลือกที่ถูกตัดไปแล้วกลับมาพิจารณาใหม่
> โดยไม่ผ่านการ amend · ดูสรุปมติที่ `## Unresolved Open Questions`
>
> **โปรเจกต์นี้ไม่ใช่ Prisma** — contract ของ data model คือ entity ใน
> `backend/src/SupportRoom.Domain/Entities/` + EF Core migration ใน
> `backend/src/SupportRoom.Providers.Data/Migrations/` (กฎ `schema.prisma` ใน
> `.claude/shared/conventions.md` §7 ต้องอ่านเทียบเป็นสองอย่างนี้)
>
> **Amendment 2026-08-19 มี authority เหนือข้อความเดิมเมื่อขัดกัน** โดยคง business behavior
> จาก `requirement.md` ทุกข้อ แต่ยอมรับ naming, public learner API และ migration shape ที่ implement
> อยู่จริงตามหัวข้อ `## Contract Amendment — 2026-08-19` ด้านล่าง ไม่มี schema เพิ่มเติม
>
> ---
>
> **🆕 สถานะ amendment 2026-08-23 — F9 / F10 / F10-a · ✅ U1–U4 เคาะครบแล้ว ไม่มีคำถามค้าง**
>
> `requirement.md` ปิดการสัมภาษณ์ F9 (responsive ฝั่งผู้เรียน) · F10 (พิมพ์ถามแทนพูด) ·
> F10-a (ตัดฟีเจอร์แชต CS ออกทั้งฟีเจอร์) เมื่อ 2026-08-22 · รอบนี้แปลงเป็น contract แล้วที่
> **`## Responsive Interaction Rules (F9)`**, **`## Text Question Rules (F10)`**,
> **`## Chat Removal Rules (F10-a)`**, **DM-3/DM-4/DM-6/DM-7/DM-8 ที่แก้**,
> **`## Migration Plan → MG-R1`** และ **Module G / H / I**
>
> ✅ **เจ้าของโปรเจกต์ตอบครบทั้ง 4 ข้อในแชทเมื่อ 2026-08-23 — U1–U4 เป็นมติแล้ว ไม่ใช่ข้อเสนอ**
> (amend รอบเดียวกัน) · **`project-manager` วาง phase ได้เต็มรูปแบบ**:
> **U1 = ตัดการตอบ readiness ด้วยเสียงทิ้งด้วย เหลือปุ่มกดอย่างเดียว** *(ไม่ใช่ทางที่
> `system-analyst` เสนอ — เจ้าของโปรเจกต์เลือกทางที่งานหนักกว่าโดยเห็น trade-off ครบแล้ว)* ·
> **U2 = เพิ่ม `SessionQuestion.Source`** · **U3 = `DropTable` `ChatMessage` ทิ้งทั้งใบพร้อมข้อมูล** ·
> **U4 = F9 รวม `/session-ended/[token]` + `/link-expired` ด้วย** ·
> มติเต็มพร้อมสิ่งที่ถูกตัดออกอยู่ที่ `## Unresolved Open Questions → ✅ มติ U1–U4 (2026-08-23)`
>
> ⚠️ **ผลของ U1 ที่ทุกคนต้องรู้ก่อนอ่านต่อ:** งานรอบนี้ **ใหญ่กว่าที่ประเมินไว้ตอนสัมภาษณ์**
> เพราะการถอด voice readiness เป็นการรื้อโค้ดที่ **ผ่าน QA FULL-3 ไปแล้ว** ทั้ง backend
> (`/api/voice-question` — Module C/D) และ frontend (reducer + ห้องเรียน — Module E) ·
> รายการที่ต้องรื้อครบทุกจุดอยู่ที่ **`## Text Question Rules (F10) → TQ-22..TQ-27`**

---

## Contract Amendment — 2026-08-19

> เจ้าของโปรเจกต์ยืนยัน amendment นี้เมื่อ 2026-08-19 เพื่อ resolve `LS-QA-02` จาก FULL QA รอบแรก
> ข้อความในหัวข้อนี้ **แทนที่เฉพาะชื่อ data/wire contract, public learner route/credential flow,
> audit mutability และ migration shape** ที่ขัดกันใน proposal วันที่ 2026-08-18 ส่วน business rules
> F1–F8, LR-2, LR-3/LR-3a, progress/end behavior, review behavior และ isolation outcome คงเดิม

### CA-1 · Naming contract ที่ใช้จริง

| ความหมาย | Contract ที่ยืนยัน | หมายเหตุ |
|---|---|---|
| ลิงก์ที่ CS แจก | `TrainingLink` / `ITrainingLinkRepository` / `/api/training-links` / TS `TrainingLink` | แทน proposal `LessonLink`; ไม่กลับไปใช้ `TrainingSession` |
| FK จากการเรียนไปลิงก์ | `LearningSession.TrainingLinkId` | logical FK ตาม pattern เดิม |
| ชื่อที่ผู้เรียนกรอก | `LearningSession.RecipientName` / wire `recipientName` | เป็น display label ที่ผู้เรียนกรอกเอง ไม่ใช่ชื่อที่ CS กรอกล่วงหน้า |
| FK ของคำถามและแชต | `SessionQuestion.SessionId`, `ChatMessage.SessionId` / wire `sessionId` | ทุกจุดชี้ `LearningSession.Id`; คำว่า session หลัง split หมายถึงการเรียนเท่านั้น |
| สถานะการเรียน | `SessionStatus.InProgress` / `SessionStatus.Ended`; TS `LearningSessionStatus` | ค่า wire ยังเป็น `IN_PROGRESS` / `ENDED` |
| สถานะลิงก์ | `LinkStatus.Active` / `LinkStatus.Expired`; TS `LinkStatus` | คำนวณจาก `ExpiresAt`, ไม่เก็บคอลัมน์ status |

ชื่อเหล่านี้เปลี่ยนเฉพาะ identifier ไม่เปลี่ยน business semantics และไม่ต้องออก EF migration เพิ่ม

### CA-2 · Public learner identity และ API contract

Public learner flow ใช้คู่ **`(training-link token, learnerKey)`** ทุกครั้ง แล้ว server resolve
`CompanyId`, `TrainingLink` และ `LearningSession.Id` เอง ผู้เรียน **ห้ามส่ง `learningSessionId`
เพื่อเลือกแถวโดยตรง** และไม่ใช้ `X-Learner-Key` ใน contract นี้ การ resolve server-side ลด IDOR surface
เพราะ client เลือก session id อื่นบนลิงก์เดียวกันไม่ได้

| Operation | Contract ที่ยืนยัน |
|---|---|
| resume state | `GET /api/learning-sessions/{token}/resume?learnerKey=` (`learnerKey` ว่างได้ตาม LR-3 กรณี ก) |
| join | `POST /api/learning-sessions/{token}/join`, body `{ recipientName, learnerKey }` |
| restart | `POST /api/learning-sessions/{token}/restart`, body `{ recipientName, learnerKey }` |
| progress | `PATCH /api/learning-sessions/{token}/progress`, body `{ learnerKey, lastSlideObjectId?, lastSlideIndex, totalSlideCount? }` |
| end | `PATCH /api/learning-sessions/{token}/end`, body `{ learnerKey, completedAllSlides, lastSlideObjectId?, lastSlideIndex }` |
| learner summary | `GET /api/learning-sessions/{token}/summary?learnerKey=` |
| learner questions/chat | `GET /api/session-questions?token=&learnerKey=` และ `GET /api/chat-messages?token=&learnerKey=` |
| voice question | `POST /api/voice-question`, multipart fields `token`, `learnerKey`, `audio`, fields อื่นตามเดิม |
| learner SignalR | `JoinSession(token, learnerKey)` และ `SendChatMessage(token, learnerKey, text)` |
| CS REST/SignalR | ใช้ `learningSessionId` บน authenticated endpoints/methods; server ต้องบังคับ company authorization |

SignalR group key และ persistence key ภายใน server ยังคงเป็น `LearningSession.Id`; client ส่ง token/key
แล้ว hub resolve id ก่อน join/broadcast จึงรักษา isolation outcome เดิมของ IC-5

### CA-3 · Credential handling และ isolation

แม้ `RecipientName` ไม่ใช่ identity แต่คู่ `(token, learnerKey)` ทำหน้าที่เป็น **composite bearer
credential** สำหรับข้อมูลผู้เรียนในระบบที่ยังไม่มี learner login จึงต้องปฏิบัติดังนี้:

1. ใช้ผ่าน HTTPS เท่านั้น และต้องไม่ log ค่า token/`learnerKey` หรือ full query string
2. ห้ามส่งค่าเหล่านี้เข้า analytics, telemetry attributes, cache key ที่เปิดเผยภายนอก หรือ referrer
3. lookup token แบบข้าม query filter ได้เฉพาะ entry point ที่มีเหตุผลกำกับ แล้วต้อง
   `CompanyContext.Resolve(link.CompanyId)` ก่อน query ข้อมูล company-scoped อื่น
4. resolve session ด้วย `(TrainingLinkId, LearnerKey)` และคืน `404` เมื่อคู่ไม่ตรงกัน เพื่อไม่เปิดเผย
   ว่า learner/session อื่นมีอยู่จริง
5. `LearnerKey` ต้องไม่ออกใน ViewModel/response; browser สร้างด้วย `crypto.randomUUID()` และเก็บตาม IC-4
6. Security gate ต้อง audit transport/logging, anonymous REST, SignalR group isolation และ CS authorization

ข้อกำหนด no-log/no-cache ครอบ query-string transport ที่ยอมรับใน amendment นี้โดยเฉพาะ หาก runtime,
reverse proxy หรือ monitoring platform ใดรับประกันไม่ได้ ต้องย้าย `learnerKey` ไป secure header/body
ผ่าน design amendment ใหม่ก่อน production

### CA-4 · Repository และ audit mutability

- Public flow ใช้ `ITrainingLinkRepository.GetByToken()` และ
  `ILearningSessionRepository.GetActiveByLearnerKey/GetLatestInProgressByLearnerKey/
  GetLatestEndedByLearnerKey`; ไม่ต้องมี `GetByIdAcrossCompanies()` สำหรับ learner flow
- CS by-id ใช้ authenticated company-scoped repository query และ authorization guard
- `SessionQuestion.UpdateBy`/`UpdateDate` เป็น `set` เพื่อรองรับ review; delete audit fields ของ
  `SessionQuestion` และ audit update/delete fields ของ `ChatMessage` คง `init` ได้ เพราะโมดูลนี้ไม่มี
  update/delete flow สำหรับ chat และไม่มี soft-delete flow สำหรับสอง entity นี้
- หากเพิ่มการลบ/ซ่อนคำถามหรือแชตภายหลัง ต้อง amend contract และทำ audit setters/authorization
  พร้อม feature นั้น ห้ามเปลี่ยน mutability เผื่อไว้โดยไม่มี use case

### CA-5 · Migration contract ที่เกิดขึ้นจริง

ยอมรับ migrations ที่ generate และตรวจแล้วเป็น contract ของ implementation ปัจจุบัน:

1. `20260813140603_SplitLinkAndAddAuth` — rename `TrainingSession` → `TrainingLink`, สร้าง
   `LearningSession`, backfill demo data, repoint ค่า `SessionQuestion.SessionId` และ
   `ChatMessage.SessionId` ให้เป็น `LearningSession.Id`, เพิ่ม review fields และลบ `SessionSummary`
2. `20260818155126_AddTotalSlideCount` — เพิ่ม `LearningSession.TotalSlideCount`

ไม่มี migration ชื่อ `SplitLessonLinkAndLearningSession` และไม่มีการ rename `SessionId` เป็น
`LearningSessionId` migrations ทั้งสองใบยัง **รอ apply กับ deployment database** ตาม `LS-QA-01`;
amendment นี้ไม่อนุญาตให้แก้ migration ที่ generate แล้วและไม่สร้าง migration เพิ่ม

### CA-6 · Future expansion boundary

contract นี้ตั้งใจรองรับ anonymous learner บน browser เดียวตาม requirement ปัจจุบัน หากอนาคตต้อง
รองรับ login, verified identity หรือ cross-device resume จะต้องออกแบบ identity/authentication contract
ใหม่ ไม่เพิ่ม `learningSessionId` เป็น public bearer credential แบบเฉพาะกิจใน flow ปัจจุบัน

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
| **F3** `LearnerKey` ทำสองหน้าที่ | **ทำได้ แต่มีเงื่อนไขความปลอดภัยที่ต้องเขียนเป็นกติกา** | `localStorage` + `crypto.randomUUID()` · หลัง amendment 2026-08-19 คู่ **`(token, LearnerKey)` เป็น composite bearer credential** และ server resolve `LearningSession.Id` เอง — กติกาบังคับอยู่ที่ CA-2/CA-3 และ `## Isolation & Credential Rules` · **เครื่องที่ใช้ร่วมกัน** (คอมกลางในโรงเรียน) มี `LearnerKey` เดียวกัน → เดิมคนที่สองจะ "เรียนต่อ" ของคนแรก · **ปิดช่องนี้ด้วยมติ D2 (2026-08-18): ต้องถามยืนยันก่อน resume เสมอ** — `requirement.md` F3 แยกเป็นกรณี ก/ข แล้ว กติกาบังคับอยู่ที่ **LR-3 + LR-3a + IC-7** |
| **F4** บันทึกความคืบหน้า | **ทำได้** | ⚠️ **`LastSlideIndex` อย่างเดียวแสดง "7/20" ไม่ได้** — ต้องมีตัวหารด้วย จึงเพิ่ม `TotalSlideCount` ในตารางใหม่ (สเปกเดิม §3.2 ไม่ได้ระบุไว้ แต่เป็นเงื่อนไขบังคับของข้อความ "โดยไม่ต้อง resolve deck ใหม่" ใน F4) |
| **F5** แยก "ครบสไลด์" จาก "จบแล้ว" + หน้าสรุป + เรียนอีกครั้ง | **ทำได้** | สองคอลัมน์แยกกันตามมติ · "เรียนอีกครั้ง" = สร้างแถวใหม่ (LR-6) · หน้าสรุปฝั่งผู้เรียนกับฝั่ง CS ใช้ **ViewModel คนละตัว** เพื่อไม่ให้ข้อมูลภายในรั่ว (RR-5) |
| **F6** "หยุดกลางคัน" คำนวณตอนแสดงผล | **ทำได้** | `INACTIVE_THRESHOLD_MINUTES` เข้า `ServerDefaults.cs` ตาม pattern เดิมเป๊ะ · คำนวณที่ ViewModel ฝั่ง backend (ที่เดียว) ไม่ให้ frontend คำนวณเอง — เหตุผลใน SR-2 |
| **F7** CS รีวิวคำตอบ AI | **ทำได้** | `SessionQuestion.UpdateBy`/`UpdateDate` ต้องเป็น `set` เพื่ออัปเดตรีวิว ส่วน delete audit fields คง `init` ได้เพราะโมดูลนี้ไม่มี delete flow (CA-4) |
| **F8** ช่อง `MaxAttendees` กรอกได้แต่ไม่บังคับใช้ | **ทำได้** | คอลัมน์ `int?` + ช่องกรอก + ข้อความกำกับใน UI · **ห้ามเขียนโค้ดตรวจ** (LR-2) — เป็นข้อห้ามที่เขียนไว้เป็นกติกาเพราะ engineer มักเผลอ validate ให้ "ครบถ้วน" |
| **F9** responsive ฝั่งผู้เรียนครบทุก interaction (เพิ่ม 2026-08-23) | **ทำได้ ไม่ต้องเพิ่ม dependency — แต่ "มี breakpoint อยู่แล้ว" ≠ "ทำงานได้จริง"** | ไม่แตะ schema เลย · แต่ **3 จุดที่มีอยู่วันนี้ไม่ทำงานจริงบนมือถือ ยืนยันกับโค้ดแล้ว ไม่ใช่การอนุมาน**: (1) `PushToTalkButton` เรียก `e.preventDefault()` ใน `onTouchStart` ซึ่ง React ผูกเป็น **passive listener** ที่ root ตั้งแต่ v17 → กัน scroll ไม่ได้จริง (R3a ยังไม่เคยถูกแก้) (2) `room/[token]/page.tsx` ใช้ `h-screen` = `100vh` → บนมือถือ URL bar ดันให้ `ControlBar` (ปุ่มจบ) หลุดใต้ขอบจอ ขัด R3c ตรงๆ (3) ไม่มี `export const viewport` ที่ layout ใดเลย → แป้นพิมพ์เด้งแล้วบังช่องพิมพ์ แก้ด้วย CSS อย่างเดียวไม่พอ · กติกาเต็มอยู่ที่ `## Responsive Interaction Rules (F9)` |
| **F10** พิมพ์ถามแทนพูด เทียบเท่า 100% (เพิ่ม 2026-08-23) | **ทำได้ ไม่ต้องเพิ่ม provider/env/dependency ใหม่** | ตรวจโค้ดแล้ว: `GeminiRest.CallAsync(..., audio: null)` **รองรับ text-only อยู่แล้ว** (RAG answer step ใช้อยู่ทุกวัน) และ `RagVoiceQuestionProvider` แยก step ชัด (1 transcribe → 2 retrieve → 3 answer) — คำถามที่พิมพ์คือ **การข้าม step 1 แล้วเอาข้อความที่พิมพ์ไปเป็น transcript** ไม่ใช่ pipeline ใหม่ · ⚠️ **แตะ schema จริง** — U2 ยืนยันแล้วให้เพิ่ม `SessionQuestion.Source` · ⚠️ **และหลังมติ U1 (2026-08-23) F10 ไม่ใช่แค่ "เพิ่มทางเข้าใหม่" อีกต่อไป แต่รวมการ *ถอด* การตอบ readiness ด้วยเสียงออกจากระบบด้วย** ซึ่งเป็นการรื้อโค้ดที่ผ่าน QA แล้วทั้งสองฝั่ง (TQ-22..TQ-27) · กติกาเต็มอยู่ที่ `## Text Question Rules (F10)` |
| **F10-a** ตัดฟีเจอร์แชต CS ออกทั้งฟีเจอร์ (เพิ่ม 2026-08-23) | **ทำได้ — แต่ขอบเขตจริงกว้างกว่าที่ `requirement.md` ตรวจไว้ประมาณเท่าตัว** | `requirement.md` ไล่ไว้ 5 รายการ (ฝั่งผู้เรียน) · ตรวจโค้ดจริงแล้วพบ **ฝั่ง CS เต็มรูปแบบที่เอกสารนั้นระบุเองว่ายังไม่ได้ตรวจ**: `SessionHub.JoinSessionAsAgent`/`SendChatMessageAsAgent`, `use-agent-session-chat.ts`, ปุ่ม "แชท" + `ChatDrawer` ใน `admin/learning-sessions/[id]/page.tsx`, `GET /api/chat-messages/by-learning-session/{id}` · รวมของที่ต้องรื้อ **27 จุด** + migration ลบตาราง · ⚠️ **กับดักที่ทำให้พังเงียบ**: `use-agent-session-chat.ts` ทำสองหน้าที่ (chat **และ** `ReceiveNewQuestion` = คำถามสดของ CS) ลบทั้งไฟล์ = Phase 6 เสียฟีเจอร์ที่ไม่ได้สั่งให้ลบ · รายการเต็มอยู่ที่ `## Chat Removal Rules (F10-a)` |

**ไม่มีฟีเจอร์ใดใน F1–F10a ที่ต้องใช้ dependency ใหม่ / บริการภายนอกใหม่ / อยู่นอก stack**

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
| *(เพิ่ม 2026-08-22)* มือถือ/แท็บเล็ตต้องรองรับแค่ไหน | **desktop เป็นหลัก แต่มือถือ/แท็บเล็ตต้องครบทุก interaction ไม่มีข้อยกเว้น** (R1/R4) | mobile-first · การยก interaction ใดไปเป็น "desktop only" |
| *(เพิ่ม 2026-08-22)* จอแนวตั้ง | **ใช้งานได้เต็มรูปแบบ portrait ห้ามบังคับหมุนจอ** (R5) | หน้าจอ "กรุณาหมุนเป็นแนวนอน" ทุกรูปแบบ |
| *(เพิ่ม 2026-08-22)* ขอบเขต responsive | **`/room/*` + `/join/*` เท่านั้น ไม่รวม `/admin/*`** (R2) | การจัด responsive ให้หลังบ้าน CS ในรอบนี้ |
| *(เพิ่ม 2026-08-22)* push-to-talk บนจอสัมผัส | **กดค้างเหมือนเดิม + ต้องกัน scroll/context menu** (R3a) | การเปลี่ยนเป็นกดครั้งเดียวสลับเปิด/ปิด |
| *(เพิ่ม 2026-08-22)* พิมพ์ถาม vs พูด | **เทียบเท่า 100%** · บันทึกลง `SessionQuestion` + เข้าคิวรีวิว F7 **เหมือนกันทุกประการ** (T1/T2) | การทำเป็นทางสำรองที่จำกัดกว่า · การแยกตารางเก็บคำถามที่พิมพ์ |
| *(เพิ่ม 2026-08-22)* เสียงอ่านคำตอบเมื่อพิมพ์ถาม | **TTS อ่านทุกครั้ง ไม่มีโหมดเงียบ** (T3) | โหมดเงียบ · การให้ผู้ใช้เลือกเปิด/ปิดเสียงคำตอบ |
| *(เพิ่ม 2026-08-22)* จังหวะหยุดบรรยายเมื่อพิมพ์ | **หยุดตอน "กดส่ง" ไม่ใช่ตอนโฟกัสช่องพิมพ์** (T5) | การ interrupt ทันทีที่โฟกัส/เริ่มพิมพ์ (ต่างจาก push-to-talk โดยตั้งใจ) |
| *(เพิ่ม 2026-08-22)* ตอบ readiness ด้วยการพิมพ์ | **ไม่รองรับ — ใช้ปุ่มกด "พร้อม/ยังไม่พร้อม"** (T6) · **ขยายผลด้วยมติ U1 เมื่อ 2026-08-23: การพูดตอบก็ไม่รองรับเช่นกัน** | ช่องพิมพ์ที่ active ตอน `state = "ready"` · การ parse ข้อความว่าเป็นคำตอบพร้อม/ไม่พร้อม |
| *(เพิ่ม 2026-08-22)* ช่องพิมพ์ในห้องเรียน | **ช่องเดียว ปลายทางเดียวคือ AI** (T4) | ช่องพิมพ์ 2 ช่อง · ช่องเดียวที่แยกปลายทางเอง |
| *(เพิ่ม 2026-08-22)* แชตคุยกับ CS ระหว่างเรียน | **ตัดออกทั้งฟีเจอร์ ไม่ใช่ซ่อน UI · ไม่มีทางสำรองให้คุยกับคนจริง** (T4-a ยืนยัน 2 รอบ) | การซ่อน UI ไว้เผื่อเปิดทีหลัง · การย้ายไปช่องทางอื่นในระบบ · ⛔ **ห้ามยกมาถามซ้ำ** ว่าควรมี fallback ให้คุยกับคนไหม (`requirement.md` §Constraints สั่งไว้ตรงๆ) |
| *(เพิ่ม 2026-08-22)* ลำดับงาน F9 กับ F10 | **ชุดเดียวกัน ทำพร้อมกัน** (T7/R6) | การแยก F10 ไปเฟสถัดไป |
| **U1** *(เคาะ 2026-08-23)* ตอบ readiness ด้วย **เสียง** ยังได้ไหม | **❌ ไม่ได้ — ตัดทิ้งด้วย เหลือ "กดปุ่ม" ทางเดียวเท่านั้น** · ทั้งพิมพ์และพูดใช้ตอบจุดนี้ไม่ได้เลย · เจ้าของโปรเจกต์เลือก **ทางที่ `system-analyst` เตือนว่างานหนักกว่า** โดยเห็น trade-off ครบแล้ว รวมถึงผลกระทบต่อ Module C/D/E ที่ผ่าน QA ไปแล้ว | การคงเส้นทางเสียงไว้แล้วเพิ่มปุ่ม (ข้อเสนอเดิมของ `system-analyst`) · `expecting: "readiness"` ทุกชั้น · `READINESS_ANSWERED` · `BuildReadinessPrompt` ทั้งสอง provider · `VoiceAnswerViewModel.Readiness` · ⛔ **ห้ามยกกลับมาถามว่า "เก็บเสียงไว้เผื่อ" อีก** |
| **U2** *(เคาะ 2026-08-23)* เพิ่ม `SessionQuestion.Source` (`voice`/`text`) ไหม | **✅ เพิ่ม** ตามข้อเสนอ — คอลัมน์ `NOT NULL` backfill `"voice"` (DM-3a) | การปล่อยให้แยกไม่ออกว่าคำถามมาทางไหน · การเพิ่มคอลัมน์ทีหลังแล้ว backfill เป็น `"unknown"` |
| **U3** *(เคาะ 2026-08-23)* ตาราง `ChatMessage` + ข้อมูลเดิม | **✅ `DropTable` ทิ้งทั้งใบพร้อมข้อมูล** — breaking + data loss ที่ตั้งใจ (MG-R1) | การเก็บตารางไว้อ่านย้อนหลัง · การ archive/ย้ายข้อความไปตารางอื่น |
| **U4** *(เคาะ 2026-08-23)* ขอบเขต F9 รวม `/session-ended/[token]` + `/link-expired` ไหม | **✅ รวม แบบ "ตรวจตามกฎเดียวกัน ไม่ redesign"** (RS-1) | การยึดตัวอักษร R2 แล้วเลื่อนสองหน้านี้ไปรอบหน้า · การ redesign หน้าจบ/หน้าหมดอายุ |

### ✅ มติเชิงโครงสร้าง 6 ข้อ — ยืนยัน 2026-08-18 และ amend 2026-08-19

เดิมเป็นข้อเสนอที่รอเคาะ (Q2/Q3/Q4 + D1–D3) — **เจ้าของโปรเจกต์ตอบครบทั้ง 6 ข้อเมื่อ 2026-08-18
และตรงตามข้อเสนอของ `system-analyst` ทุกข้อ** จึงเป็นมติที่มีผลบังคับแล้ว ไม่ใช่คำถามที่ยังเปิดอยู่

| # | เรื่อง | มติที่ยืนยัน (2026-08-18) | สิ่งที่ถูกตัดออกด้วยมตินี้ |
|---|---|---|---|
| **Q2** | rename `TrainingSession` ไหม | **amend → `TrainingLink`** (2026-08-19) | คงหลักการว่าลิงก์ต้องไม่ชื่อ `TrainingSession`; ยอมรับชื่อที่ implement ครบทุก layer แล้วตาม CA-1 |
| **Q3** | ชื่อตารางใหม่ | **`LearningSession`** | `LearnerAttempt` · `TrainingAttendance` |
| **Q4** | `SessionSummary` | **ลบทิ้งทั้งใบ (13 จุด)** | การย้าย summary ไปผูก `LearningSession` · การคงตารางไว้เฉยๆ · snapshot แช่แข็งตอนจบ |
| **D1** | route/TS type ตามชื่อใหม่ด้วยไหม | **ตามด้วยชื่อที่ amend** — `/api/training-links`, `/api/learning-sessions`, type `TrainingLink` | การคง path `/api/sessions` และ TS type ชื่อ `TrainingSession` |
| **D2** | เครื่องใช้ร่วมกัน → resume แบบไหน | **ถามยืนยันก่อน resume เสมอ** พร้อมทางเลือก "เริ่มเรียนใหม่ในชื่ออื่น" | การ resume เงียบๆ · การจำสถานะ "ยืนยันแล้ว" เพื่อข้ามคำถามครั้งถัดไป · login/OTP ทุกรูปแบบ |
| **D3** | migrate ข้อมูล demo เดิมไหม | **migrate ด้วย backfill SQL** ใน migration เดียวกัน | migration แบบทำลาย (drop ทิ้งแล้วสร้าง demo ใหม่ด้วยมือ) |

D2 ถูกส่งกลับไปที่ `business-analyst` แล้ว และ `requirement.md` F3 ฉบับ 2026-08-18 แยกเป็น
**กรณี ก** (ไม่มีกุญแจ = คนใหม่ ไม่ต้องถาม) กับ **กรณี ข** (มีกุญแจ *และเจอการเรียนที่ยังไม่จบ*
= ต้องถามยืนยัน) — กติกาที่ engineer ต้องทำตามอยู่ที่ **LR-3** และ **IC-7**

---

## Q2 + Q3 — ชื่อของ "ลิงก์" และ "การเรียน" (บันทึกเหตุผลเดิม; Q2 ถูก amend โดย CA-1)

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

### ✅ มติเดิม (2026-08-18) และ amendment (2026-08-19)

มติเดิมเลือก `LessonLink` + `LearningSession`; วันที่ 2026-08-19 เจ้าของโปรเจกต์ยืนยัน amend Q2
เป็น **`TrainingLink` + `LearningSession`** ตาม CA-1 เพราะ implementation ปัจจุบันแยกความหมาย
ครบทุก layer แล้วและไม่เหลือ entity `TrainingSession` ข้อความเหตุผลเดิมด้านล่างเก็บเป็น decision history

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

**สถานะปัจจุบัน: ✅ amend เมื่อ 2026-08-19** — ชื่อจริงคือ `TrainingLink` + `LearningSession`;
ตารางทางเลือกข้างบนเป็นบันทึกเหตุผลเดิม ไม่ใช่ implementation contract

### D1 ✅ (ยืนยัน 2026-08-18; amend 2026-08-19) — route และ TypeScript type ตามชื่อใหม่ด้วย

- **มติปัจจุบัน: ตามด้วยชื่อที่ amend** — `/api/sessions` → `/api/training-links`,
  type `TrainingSession` → `TrainingLink`
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

> **Baseline section นี้ต้องอ่านร่วมกับ CA-1, CA-4 และ CA-5** — เมื่อชื่อ field/type/repository
> ขัดกัน ให้ใช้ Contract Amendment 2026-08-19 เป็น authority; shape, nullability, index และ query
> filter ส่วนที่ amendment ไม่ได้เปลี่ยนยังมีผลตาม section นี้

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

#### DM-3a · `SessionQuestion.Source` — ฟิลด์ใหม่ของ F10 (✅ **U2 ยืนยัน 2026-08-23**, เพิ่ม 2026-08-23)

> ✅ **U2 เคาะแล้ว = เพิ่มคอลัมน์นี้จริง** (เจ้าของโปรเจกต์ตอบตรงตามข้อเสนอ) — หัวข้อย่อยนี้
> เป็น contract ที่ implement ได้ทันที ไม่ใช่ทางเลือกอีกต่อไป

ชื่อไฟล์จริงที่ต้องแก้: `backend/src/SupportRoom.Domain/Entities/SessionQuestion.cs`
(ชื่อ field ในโค้ดจริงคือ `SessionId` ตาม CA-1 ไม่ใช่ `LearningSessionId` ของ proposal ด้านบน)

```csharp
    /// <summary>QuestionSource.Voice | QuestionSource.Text - ผู้เรียนพูดถามหรือพิมพ์ถาม (F10/TQ-5)
    /// NOT NULL เสมอ: "ไม่รู้ที่มา" ไม่ใช่สถานะที่ถูกต้องสำหรับแถวที่เกิดหลัง MG-R1 และแถวเดิม
    /// ทุกแถวมาจากเสียงล้วนโดยข้อเท็จจริง (วันนี้ยังพิมพ์ถามไม่ได้เลย) จึง backfill เป็น voice ได้ตรงๆ
    /// ไม่ต้องเดา · เก็บที่นี่เพราะย้อนหลังสร้างใหม่ไม่ได้: transcript ของคำถามที่พูดกับที่พิมพ์
    /// หน้าตาเหมือนกันทุกประการ</summary>
    public required string Source { get; init; }
```

**ทำไมถึงคุ้มที่จะเพิ่ม (เหตุผลเชิงธุรกิจ ไม่ใช่ความอยากเก็บข้อมูล):** F7 มีอยู่เพื่อให้ CS แยก
**สาเหตุของ "ตอบผิด" 3 แบบ** (คลังไม่มีข้อมูล / มีแต่ค้นไม่เจอ / AI เดาเอง) ที่แก้คนละทาง —
คำถามที่ *พูด* มีสาเหตุที่ 4 ที่คำถามที่ *พิมพ์* ไม่มีเลยคือ **ถอดเสียงผิด** ถ้าไม่รู้ที่มา
CS จะแยกไม่ออกว่ากำลังดูปัญหาความรู้หรือปัญหาการถอดเสียง · **ไม่มีอะไรใน DB สร้างค่านี้ย้อนหลังได้**

**ผลที่ตามมา (บังคับแล้วตามมติ U2):** `CreateSessionQuestionDto` เพิ่ม `Source` (required) · `SessionQuestionViewModel`
เพิ่ม `Source` · `LearnerSessionQuestionViewModel` **ไม่เพิ่ม** (ผู้เรียนรู้อยู่แล้วว่าตัวเองพิมพ์หรือพูด
และ RR-5 ให้ ViewModel ฝั่งผู้เรียนบางที่สุดเท่าที่จำเป็น) · TS `SessionQuestion` เพิ่ม
`source: QuestionSource` · UI รีวิวของ CS แสดงป้าย "พิมพ์"/"เสียง" ต่อคำถาม

### DM-4 · `ChatMessage` — **ลบทั้ง entity** (F10-a, 2026-08-23 · ✅ **U3 ยืนยัน 2026-08-23: ลบพร้อมข้อมูล**)

> **ข้อความเดิมของ DM-4 ("เปลี่ยน `SessionId` → `LearningSessionId` ฟิลด์อื่นคงเดิม") เป็นโมฆะแล้ว**
> เก็บบรรทัดนี้ไว้เพื่อให้ประวัติอ่านย้อนได้ · คำสั่งเดิมของ F7 ที่ให้ย้าย `ChatMessage`
> ไปผูกกับ "การเรียน" ถูกเพิกถอนโดย T4-a (`requirement.md` 2026-08-22) — ของชิ้นนี้ไม่ใช่
> "ของที่ต้องย้าย" อีกต่อไป แต่เป็น **"ของที่ต้องรื้อ"**

`backend/src/SupportRoom.Domain/Entities/ChatMessage.cs` **ลบทั้งไฟล์** พร้อมตารางใน DB (MG-R1)

**นี่คือ breaking change + data loss ที่ตั้งใจ** — pattern เดียวกับ
`RemoveLessonConfigPacingOverrides` ของ `company-admin` (มติ N2/N3): **ห้าม migrate ค่าเดิมไปไหน
ห้าม archive ลบตรงๆ พร้อมคอมเมนต์อธิบายเจตนาในไฟล์ migration** · `Down()` สร้างตารางคืนได้
แค่ *รูปร่าง* ไม่ใช่ข้อมูล และต้องเขียนคอมเมนต์บอกไว้ตรงๆ เหมือนกัน

ของที่ห้อยอยู่กับ entity นี้และต้องหายไปพร้อมกันครบทุกจุด: ดู `## Chat Removal Rules (F10-a)`

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

#### DM-6a · constants ที่เปลี่ยนในรอบ 2026-08-23

| Constant | สิ่งที่ต้องเกิดขึ้น |
|---|---|
| `SupportRoom.Domain/Enums/ChatSenderRole.cs` (`recipient`/`agent`/`system`) | **ลบทั้งไฟล์** — ไม่มีผู้ส่งข้อความอื่นนอกจาก AI แล้ว (F10-a) · TS `ChatSenderRole` ใน `types/domain.ts` ลบคู่กัน |
| `SupportRoom.Domain/Enums/QuestionSource.cs` — **ใหม่** (✅ U2 ยืนยัน 2026-08-23 — สร้างจริง) | ```csharp\npublic static class QuestionSource\n{\n    public const string Voice = "voice";\n    public const string Text = "text";\n}\n``` — `static class` + `const string` ตาม convention ห้ามใช้ C# enum · TS union `QuestionSource = "voice" \| "text"` ต้องตรงกันเป๊ะ |
| `AnswerStatus` (เดิม) | **ไม่เพิ่มค่าใหม่** — `no_speech`/`transcription_failed` เป็นของเส้นทางเสียงล้วน คำถามที่พิมพ์ไม่มีทางได้สองค่านี้ (TQ-9 อธิบายว่าทำไมถึงไม่เพิ่ม `answer_failed`) |

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

#### DM-7a · สิ่งที่เปลี่ยนใน `ApplicationDbContext` รอบ 2026-08-23 (F10-a)

**บล็อก `builder.Entity<ChatMessage>(...)` ด้านบนเป็นโมฆะ — ลบทั้งบล็อก** พร้อม
`public DbSet<ChatMessage> ChatMessage => Set<ChatMessage>();` (ไฟล์จริง:
`backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` บรรทัด ~34 และ ~116)

`SessionQuestion` **ไม่เปลี่ยน mapping เลย** แม้จะเพิ่มคอลัมน์ `Source` (U2 ✅) — เป็น
`string` NOT NULL ธรรมดา ไม่ต้องมี index ใหม่: ไม่มี query ไหนกรองด้วย `Source`
(CS ใช้เพื่อ *แสดง* ป้าย ไม่ใช่เพื่อ *ค้น*) — index ที่ไม่มีใคร query คือหนี้ที่ไม่มีคนจ่ายคืน

### DM-8 · Repository

| Repository | สถานะ | หมายเหตุ |
|---|---|---|
| `ITrainingSessionRepository` → `ILessonLinkRepository` | rename | คง `GetByToken` ที่ `IgnoreQueryFilters()` ไว้ **พร้อม XML doc เดิมทั้งย่อหน้า** — นั่นคือคำอธิบายว่าทำไมถึงข้าม filter ได้ ห้ามตัดทิ้งตอน rename |
| `ILearningSessionRepository` | **ใหม่** | ต้องมี `GetByIdAcrossCompanies(string id)` ที่ `IgnoreQueryFilters()` + XML doc อธิบายเหตุผลแบบเดียวกับ `GetByToken` (IC-2) · `GetResumable(string lessonLinkId, string learnerKey)` · `GetByLessonLinkId(string lessonLinkId)` |
| `ISessionQuestionRepository` | แก้ | `GetBySessionId` → `GetByLearningSessionId` |
| `IChatMessageRepository` | แก้ | เหมือนกัน |
| `ISessionSummaryRepository` | **ลบ** | ✅ ตาม Q4 (ยืนยัน 2026-08-18) |
| `IChatMessageRepository` | **ลบ** *(แก้ 2026-08-23)* | แถวเดิมข้างบนเขียนว่า "แก้" — เป็นโมฆะตาม F10-a · ลบทั้งไฟล์ `Providers.Data/Repository/IChatMessageRepository.cs` + ถอดออกจาก `UnitOfWork.Register` (บรรทัด ~21) + ลบ `FakeChatMessageRepository` ใน `tests/.../Fakes/ServiceTestFakes.cs` |

ทุกตัวที่เพิ่ม/rename ต้องอัปเดต `UnitOfWork.Register` (ลืม = resolve ไม่ได้ตอน runtime)
**และการ *ลบ* ก็เช่นกัน** — repository ที่ถูกลบแต่ยังค้างใน `Register` ทำให้ DI พังตอน resolve
ไม่ใช่ตอน compile จึงไม่มี build error ให้เห็น

---

## Learning Lifecycle Rules (F1 · F2 · F4 · F5 · F8) — contract

> engineer ไม่มีสิทธิ์ตัดสินกติกาเอง หัวข้อนี้ต้องตอบให้ครบ อ่านทั้งหัวข้อก่อนเขียน service
> **Route/body/name ที่ปรากฏใน LR-1, LR-3, LR-4, LR-5, LR-6 และ LR-7 ถูก amend โดย CA-1/CA-2**;
> ลำดับ validation, state transition และ UX behavior เดิมไม่เปลี่ยน

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
>
> **IC-1..IC-6 ด้านล่างเป็น baseline proposal วันที่ 2026-08-18 และถูก amend โดย CA-2/CA-3:**
> public learner ส่ง `(token, learnerKey)` แล้ว server resolve session id; ไม่รับ public
> `learningSessionId` และไม่ใช้ `X-Learner-Key` ส่วน isolation outcome และ IC-7 ยังมีผลเต็มที่

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

## Responsive Interaction Rules (F9) — contract

> **เขียนเมื่อ 2026-08-23 จากมติ R1–R6 ที่ปิดแล้ว** · หัวข้อนี้เป็นสัญญาสำหรับ **สองกลุ่มผู้อ่าน**:
> `frontend-engineer` ใช้เป็นข้อบังคับตอน implement และ **ทีม UX/UI ใช้เป็นข้อจำกัดตอนออกแบบ**
> (R6 สั่งให้เข้าชุดเดียวกับรอบที่กำลังส่งอยู่) · ทุกข้อในนี้เป็นกติกา ไม่ใช่คำแนะนำ
>
> ⚠️ **สิ่งที่หัวข้อนี้ *ไม่ใช่*:** ไม่ใช่การออกแบบหน้าตาใหม่ · ไม่มีข้อไหนอนุญาตให้เพิ่ม
> component/ฟีเจอร์ที่ `requirement.md` ไม่ได้สั่ง · งานนี้คือทำให้ *ของที่มีอยู่* ใช้ได้จริงบนจอสัมผัส

**RS-1 · ขอบเขตที่บังคับใช้** — `/room/*` · `/join/*` · **`/session-ended/[token]` และ
`/link-expired`** (✅ **U4 ยืนยัน 2026-08-23**) · **ห้ามแตะ `/admin/*`
ในรอบนี้แม้จะเห็นว่าพัง** — ถ้าเจอปัญหา responsive ในหลังบ้าน ให้บันทึกส่งกลับ `business-analyst`
ไม่ใช่แก้เลย

**ขอบเขตของสองหน้าที่ U4 เพิ่มเข้ามาเป็นแบบ "ตรวจตามกฎเดียวกัน ไม่ redesign"** — เหตุผลคือผู้เรียน
ที่กดจบบนมือถือเด้งไป `/session-ended/[token]` เป็นหน้าสุดท้ายของ flow เสมอ · **สิ่งที่ทำได้มีเท่านี้**:
บังคับ RS-4 (`min-h-screen` → `min-h-[100dvh]`) · ปุ่มที่มีอยู่บนสองหน้านี้ต้องมี hit target
ขั้นต่ำ 44×44px (มาตรฐานเดียวกับ RS-9) · RS-11 (ห้ามบล็อกแนวตั้ง) · **ห้ามเพิ่ม component ใหม่ ห้ามจัด layout ใหม่ ห้ามเปลี่ยนข้อความ
บนสองหน้านี้** — ถ้าเจอว่าต้องออกแบบใหม่จริงจึงจะใช้ได้บนมือถือ ให้หยุดแล้วส่งกลับ `system-analyst`
ไม่ใช่ redesign เอง

**RS-2 · breakpoint ที่ใช้ — ห้ามประกาศ breakpoint เอง**

โปรเจกต์นี้เป็น **Tailwind v4 ที่กำหนดธีมผ่าน CSS ล้วนใน `frontend/src/app/globals.css`**
(ไม่มี `tailwind.config.*` แล้ว) และ **`@theme inline` ปัจจุบันไม่ได้ override `--breakpoint-*`
แม้แต่ตัวเดียว** (ตรวจไฟล์จริงแล้ว) → ค่าที่มีผลคือค่า default ของ Tailwind v4:
`sm 40rem/640px · md 48rem/768px · lg 64rem/1024px · xl 80rem/1280px · 2xl 96rem/1536px`

**ห้ามเพิ่ม `--breakpoint-*` ใหม่ใน `@theme` และห้ามใช้ arbitrary breakpoint (`min-[823px]:`)**
— ระบบนี้ต้องมีจุดสลับ layout จุดเดียวที่ทุกคนอ่านออก กติกาคือ:

| ชื่อในสัญญา | เงื่อนไข | ความหมาย |
|---|---|---|
| **compact** | `< lg` (ต่ำกว่า 1024px) | มือถือทุกแนว + แท็บเล็ตแนวตั้งส่วนใหญ่ → layout คอลัมน์เดียว ทุกกฎ RS-5..RS-10 บังคับใช้ |
| **regular** | `lg` ขึ้นไป (≥ 1024px) | เดสก์ท็อป + แท็บเล็ตแนวนอน → layout สองคอลัมน์แบบที่มีอยู่วันนี้ |

⚠️ **นี่คือการ *เลื่อน* จุดสลับที่มีอยู่ ไม่ใช่การเพิ่มของใหม่**: `room/[token]/page.tsx` วันนี้สลับที่
`md:` (768px) ซึ่งแปลว่า **iPad แนวตั้ง (768px) ได้ layout สองคอลัมน์** — สไลด์เหลือกว้างจริง
~440px ในจอที่สูง 1024px ซึ่งขัดเจตนา R5 (portrait ต้องใช้ได้เต็มรูปแบบ) · เปลี่ยน `md:flex-row`
→ `lg:flex-row` และ `md:w-72 md:flex-col` → `lg:w-72 lg:flex-col`

**RS-3 · viewport meta — ต้องมี ไม่งั้น RS-8 ทำไม่ได้เลย**

วันนี้ **ไม่มี `export const viewport` ที่ layout ใดในโปรเจกต์** (ตรวจแล้ว: `app/layout.tsx`
มีแต่ `metadata`) → Next.js ใส่ค่า default `width=device-width, initial-scale=1` ให้ ซึ่ง
**ไม่มี `interactive-widget`** ผลคือบน Chrome Android แป้นพิมพ์ใช้พฤติกรรม default
`resizes-visual` = viewport ไม่หด ช่องพิมพ์/ปุ่มส่งถูกแป้นพิมพ์ทับ **โดยที่ CSS ฝั่งเราแก้ไม่ได้เลย**

- สร้าง **`frontend/src/app/room/layout.tsx`** และ **`frontend/src/app/join/layout.tsx`**
  เป็น **server component บางๆ** (`export default function Layout({children}) { return children }`)
  ที่ `export const viewport: Viewport = { width: "device-width", initialScale: 1,
  interactiveWidget: "resizes-content" }`
- **เหตุผลที่ต้องเป็น layout ใหม่ ไม่ใช่แก้ `app/layout.tsx`**: (ก) `room/[token]/page.tsx` และ
  `join/[token]/page.tsx` เป็น `"use client"` ซึ่ง **export `viewport` ไม่ได้** (ข) แก้ที่ root
  จะเปลี่ยนพฤติกรรมแป้นพิมพ์ของ `/admin/*` ด้วย ซึ่ง RS-1 ห้ามแตะ
- **ห้ามใส่ `maximumScale` หรือ `userScalable: false`** — การปิด pinch-zoom เป็นการลดความสามารถ
  ของผู้ใช้ (และผิด WCAG 1.4.4) · R5 บอกว่าต้องใช้ได้เต็มรูปแบบ ไม่ได้บอกว่าให้ล็อกการซูม

**RS-4 · หน่วยความสูง — `100vh` เป็นข้อห้ามในหน้าเหล่านี้**

`room/[token]/page.tsx` บรรทัด ~173 ใช้ `h-screen` (= `100vh`) คู่กับ `overflow-hidden`
บนมือถือ `100vh` **นับรวมพื้นที่ที่ URL bar ครองอยู่** → เนื้อหาสูงเกินจอจริง และเพราะ
`overflow-hidden` เลื่อนตามไม่ได้ ผลคือ **`ControlBar` ซึ่งมีปุ่มจบ/ออกหลุดใต้ขอบจอและกดไม่ได้**
— ขัด R3c ("ปุ่มจบ/ออกต้องเข้าถึงได้ตลอด ไม่ต้องเลื่อนหา") แบบตรงตัวที่สุด

- เปลี่ยนเป็น **`h-[100dvh]`** (dynamic viewport height) ที่ container ของห้อง
- หน้าอื่นในขอบเขตที่ใช้ `min-h-screen` (`join`, และ **`session-ended`/`link-expired` ที่ U4 ✅
  ยืนยันว่าอยู่ในขอบเขต**) เปลี่ยนเป็น **`min-h-[100dvh]`** ด้วยเหตุผลเดียวกัน
- **`ControlBar` ต้องอยู่นอก element ที่ scroll เสมอ** (วันนี้ถูกอยู่แล้ว — เป็น sibling ของ
  `div.overflow-y-auto` ไม่ใช่ลูก) · **ห้ามย้ายเข้าไปในพื้นที่ scroll ตอนจัด layout ใหม่**
- แถบล่างต้องเผื่อ safe area: `pb-[env(safe-area-inset-bottom)]` (iPhone ที่มี home indicator)

**RS-5 · Push-to-talk บนจอสัมผัส (R3a) — โค้ดกัน scroll ที่มีอยู่วันนี้ไม่ทำงานจริง**

**ข้อเท็จจริงที่ต้องรู้ก่อนแก้:** `PushToTalkButton.tsx` เรียก `e.preventDefault()` ใน
`onTouchStart` อยู่แล้ว **แต่ไม่มีผล** — React ผูก `touchstart`/`touchmove`/`wheel` เป็น
**passive listener** ที่ root container ตั้งแต่ v17 การ `preventDefault()` ใน handler แบบ React
จึงถูกเบราว์เซอร์เพิกเฉย (และขึ้น warning ใน console) · **อย่าอ่านโค้ดเดิมแล้วสรุปว่า R3a
ทำเสร็จแล้ว — มันไม่เคยทำงานเลย**

กติกาที่ต้อง implement:

1. **เปลี่ยนไปใช้ Pointer Events**: `onPointerDown` / `onPointerUp` / `onPointerCancel`
   แทน `onMouseDown`/`onMouseUp`/`onMouseLeave`/`onTouchStart`/`onTouchEnd` — ชุดเดียวครอบทั้ง
   เมาส์ ปากกา และนิ้ว และตัดปัญหา phantom mouse event ที่เบราว์เซอร์ยิงตามหลัง touch
2. **กัน scroll ด้วย CSS ไม่ใช่ด้วย `preventDefault`**: ปุ่มต้องมี **`touch-action: none`**
   (Tailwind: `touch-none`) — นี่คือกลไกเดียวที่ได้ผลจริงกับ passive listener
3. **จับ pointer ไว้กับปุ่ม**: `e.currentTarget.setPointerCapture(e.pointerId)` ตอน `pointerdown`
   เพื่อให้ "กดค้างแล้วนิ้วเลื่อนออกนอกปุ่ม" ยังได้ `pointerup` ที่ปุ่มเดิม — ไม่งั้นการอัดค้างไว้
   ตลอดกาลเกิดได้จริงเมื่อนิ้วขยับ
4. **กัน context menu / การเลือกข้อความ / callout ของ iOS**: `onContextMenu={(e) => e.preventDefault()}`
   + `select-none` + `[-webkit-touch-callout:none]`
5. **`pointercancel` ต้องปล่อยการอัดเสมอ** (สายเข้า สลับแอป ระบบแย่ง pointer) โดยเดินเส้นทาง
   เดียวกับการปล่อยปุ่มปกติ ไม่ใช่ปล่อยค้างไว้
6. **ห้ามเปลี่ยนเป็น toggle** (กดครั้งเดียวเปิด/ปิด) — R3a ปฏิเสธไว้ชัด
7. **ห้ามแตะ `MIN_RECORDING_MS` / `MinVoiceDurationMs`** เพื่อ "ชดเชย" การกดบนจอสัมผัส —
   เกณฑ์ความยาวเสียงเป็นคนละเรื่องกับการรับ input
8. ปุ่มต้องสูงอย่างน้อย **44px** บน compact (วันนี้ `h-11` = 44px พอดี **ห้ามลดลง**)

**RS-6 · แตะสไลด์ขยายเต็มจอ (R3b)**

- **ใช้ overlay ในแอปเอง (`fixed inset-0 z-50` + พื้นหลังทึบ + ปุ่มปิดที่มุม) ห้ามใช้
  Fullscreen API** — เหตุผล: **Safari บน iPhone ไม่รองรับ `requestFullscreen()` กับ element
  ที่ไม่ใช่ `<video>`** ถ้าใช้ API จะต้องเขียนสองทางแยกตาม browser แล้วทางหนึ่งจะไม่เคยถูกทดสอบ ·
  overlay ทำงานเหมือนกันทุกเบราว์เซอร์และปิดด้วย state เดียว
- **จุดที่ผูก handler ต่างกันตามชนิดเนื้อหา และห้ามสลับกัน**:
  - **Google Slides (iframe)**: iframe เป็น cross-origin และมี **overlay โปร่งใสที่กินคลิกอยู่แล้ว**
    (`<div className="absolute inset-0" aria-hidden="true" />` ใน `SlidesEmbed.tsx` ~89)
    — **ผูกการแตะที่ overlay ตัวนี้** โดยเปลี่ยนเป็น `<button>` จริงที่มี `aria-label`
    ("ขยายสไลด์เต็มจอ") แทนการเป็น `div aria-hidden` · **ห้ามผูก handler บน `<iframe>`**
    (cross-origin จับ event ไม่ได้)
  - **PDF (`<img>`)**: ผูกที่ container ของรูป
- **ขยายเต็มจอต้องไม่หยุด/ไม่รีโหลดบทเรียน**: ห้าม unmount `SlidesEmbed` หรือเปลี่ยน `key`
  ของ iframe ตอนเข้า/ออก fullscreen — iframe ที่ remount จะโหลดใหม่และเสียตำแหน่งสไลด์ · ให้ใช้
  การสลับ class ของ container ไม่ใช่ render โครงคนละชุด
- **ปุ่มจบ/ออก และปุ่มพูดต้องยังกดได้ระหว่างเปิด fullscreen หรือปิด fullscreen อัตโนมัติเมื่อ AI
  เริ่มพูดตอบ** — เลือกอย่างใดอย่างหนึ่งแล้วเขียนคอมเมนต์บอกเหตุผล **ห้ามปล่อยให้มีสถานะที่ผู้เรียน
  ติดอยู่ในจอสไลด์โดยกดจบไม่ได้** (ขัด R3c)
- ปิดได้ด้วย: ปุ่มปิด · แตะพื้นหลัง · (ถ้าทำได้) ปุ่ม Back ของ Android — สองทางแรกเป็นขั้นต่ำ

**RS-7 · ช่องสนทนาเป็น drawer เต็มจอบน compact (R3c)**

R3c ยังมีผลบังคับ **เปลี่ยนแค่ปลายทางของข้อความจาก CS เป็น AI** (`requirement.md` เขียนกำกับไว้เอง
ว่านี่ไม่ใช่การเพิกถอน R3c) · component ที่รับกติกานี้คือตัวใหม่ตาม **CX-6** ไม่ใช่ `ChatDrawer` เดิม

- **compact**: เต็มจอจริง — `fixed inset-0` + `h-[100dvh]` **ไม่ใช่** bottom sheet
  (วันนี้ `ChatDrawer` เป็น `fixed inset-x-0 bottom-0 max-h-[70vh]` ซึ่งทั้ง *ไม่เต็มจอ* และใช้
  `vh` ที่ RS-4 ห้าม)
- **regular**: panel ลอยด้านขวาแบบเดิมได้ (`lg:absolute lg:right-4 lg:bottom-20 lg:w-80`) ·
  ⚠️ ของเดิมใช้ `sm:absolute` โดยที่ **parent ไม่มี `relative`** → มันไปอิงกับ ancestor ที่ positioned
  ตัวอื่น/viewport แทน · ตอนย้ายต้องใส่ `relative` ให้ container ของห้องให้ถูกต้อง
- เปิดอยู่บน compact = **ไม่แย่งพื้นที่สไลด์** (มันทับไปเลย) และต้องปิดได้ด้วยปุ่มที่แตะถึงด้วยนิ้วโป้ง
- **การเปิด/ปิด drawer ห้ามส่ง event ใดๆ เข้า tutor reducer** — มันเป็น UI state ล้วน (ดู TQ-15)

**RS-8 · แป้นพิมพ์ต้องไม่บังช่องพิมพ์/ปุ่มส่ง (R3c)**

1. RS-3 (`interactiveWidget: "resizes-content"`) เป็นเงื่อนไขจำเป็น — ทำข้ออื่นก่อนโดยไม่ทำข้อนี้
   จะได้ผลที่ดูเหมือนแก้แล้วบน desktop devtools แต่ยังพังบนเครื่องจริง
2. แถว input ของ drawer ต้อง `sticky bottom-0` (หรืออยู่เป็นแถวล่างสุดของ flex column ที่สูง
   `100dvh`) + `pb-[env(safe-area-inset-bottom)]`
3. เมื่อ input ได้โฟกัส ให้เลื่อนรายการข้อความไปท้ายสุด เพื่อไม่ให้ข้อความล่าสุดถูกแป้นพิมพ์บัง
4. **`font-size` ของ input ต้องไม่ต่ำกว่า 16px บน compact** — iOS Safari ซูมหน้าเข้าอัตโนมัติ
   เมื่อโฟกัส input ที่เล็กกว่า 16px แล้วผู้ใช้ต้องซูมออกเอง (`text-base` ขึ้นไป ห้าม `text-sm`)
5. Enter = ส่ง (พฤติกรรมเดิม) แต่บน compact **ปุ่ม "ส่ง" ต้องมองเห็นและกดได้เสมอ** ห้ามพึ่ง Enter
   อย่างเดียว — แป้นพิมพ์มือถือหลายตัวไม่มีปุ่ม Enter ที่ชัดเจน

**RS-9 · ปุ่มจบ/ออกเข้าถึงได้ตลอด (R3c)** — `ControlBar` ต้องติดขอบล่างและไม่เลื่อนหาย
(RS-4 ครอบเหตุผลหลักไว้แล้ว) · บน compact ถ้าปุ่มใน `ControlBar` ล้นบรรทัด **ห้ามให้ปุ่มจบ
เป็นตัวที่ตกบรรทัดหรือถูกซ่อน** — ลำดับความสำคัญเมื่อพื้นที่ไม่พอคือ
`ปุ่มพูด > ปุ่มจบ > ปุ่มเปิด drawer > ปุ่มปรับเสียง` · ทุกปุ่มใน `ControlBar` ขั้นต่ำ 44×44px

**RS-10 · "รายการสไลด์/ความคืบหน้า" (R3c) — ระวัง: ของชิ้นนี้ยังไม่มีในโค้ด**

ตรวจแล้ว **ห้องเรียนวันนี้ไม่มีรายการสไลด์ และไม่มีตัวบอกความคืบหน้าถาวรให้ผู้เรียนเห็น** —
`SlidesEmbed` แสดง "กำลังแสดงสไลด์ X จาก Y" เฉพาะใน Mock Mode เท่านั้น สิ่งที่กินพื้นที่จริง
บนจอเล็กคือคอลัมน์ขวา (`AiTile` + `ParticipantTile`)

- **ห้ามสร้าง "รายการสไลด์" ขึ้นมาใหม่ในรอบนี้** — `requirement.md` ไม่ได้สั่ง และ event log
  ต่อสไลด์ถูก Declined ไว้ตั้งแต่ 2026-08-11
- กติกา "ย่อเป็นแถบบางหรือซ่อนใต้ปุ่ม" บังคับกับ **คอลัมน์ `AiTile`+`ParticipantTile`** บน compact:
  ย่อเป็นแถวเตี้ยแนวนอนเหนือ `ControlBar` หรือย่อเหลือ `AiTile` อย่างเดียว
  (ผู้เรียนต้องเห็นสถานะว่า AI กำลังพูด/กำลังคิด ซึ่งเป็น feedback เดียวที่บอกว่าระบบยังทำงานอยู่)
- ถ้ารอบ UX/UI เพิ่มตัวบอกความคืบหน้าเข้ามาจริง มันต้องเป็น **แถบบาง/badge** ที่ไม่กินพื้นที่สไลด์
  บน compact และต้องเป็นการตัดสินใจของเจ้าของโปรเจกต์ ไม่ใช่ของ engineer

**RS-11 · Portrait (R5)** — **ห้ามมี code path ใดที่ตรวจ `orientation` แล้วบล็อกหน้าจอ**
ไม่ว่าจะเป็นหน้า "กรุณาหมุนจอ" · CSS `@media (orientation: portrait)` ที่ซ่อนเนื้อหาหลัก ·
หรือ `screen.orientation.lock()` · จัด layout ต่างกันตามแนวจอทำได้ แต่ **ทุก interaction
ต้องทำได้ครบในแนวตั้ง**

**RS-12 · ข้อห้ามรวม (อ่านก่อนเริ่ม)**

1. ห้ามเพิ่ม dependency ใหม่เพื่อทำ responsive (ไม่ต้องมี library ตรวจ device/breakpoint —
   `frontend/` มี dependency ตกค้างที่ไม่มีโค้ดเรียกใช้อยู่แล้ว 6 ตัว อย่าเพิ่มตัวที่ 7)
2. ห้าม user-agent sniffing เพื่อแยกมือถือ — ใช้ breakpoint / `pointer: coarse` เท่านั้น
3. ห้ามใช้ `100vh` ในหน้าที่อยู่ในขอบเขต (RS-4)
4. ห้ามเพิ่ม breakpoint ใหม่ใน `@theme` (RS-2)
5. ห้ามย้าย `@theme inline` ออกจาก top level ของ `globals.css` (กฎเดิมใน root `CLAUDE.md`)
6. ห้ามเขียน primitive UI เองถ้า shadcn มีให้แล้ว และห้ามใส่ business logic ใน `components/ui/`
   (กฎเดิม) · ไฟล์ใน `ui/` ต้องเป็นตัวพิมพ์เล็กเสมอ
7. **ห้ามเปลี่ยนพฤติกรรมของ state machine เพื่อให้ responsive ทำงาน** — ถ้าเจอว่าต้องเปลี่ยน
   ให้หยุดและส่งกลับ `system-analyst`

**RS-13 · ไฟล์ที่ต้องแก้ (สำรวจจากโค้ดจริง ไม่ใช่การเดา)**

| ไฟล์ | เปลี่ยนอะไร | กฎที่บังคับ |
|---|---|---|
| `frontend/src/app/room/layout.tsx` **(ใหม่)** | server component + `export const viewport` | RS-3 |
| `frontend/src/app/join/layout.tsx` **(ใหม่)** | เหมือนกัน | RS-3 |
| `frontend/src/app/room/[token]/page.tsx` | `h-screen` → `h-[100dvh]` · `md:` → `lg:` ทั้งสองจุด · `relative` ให้ container · ถือ state ของ drawer/fullscreen | RS-2, RS-4, RS-7 |
| `frontend/src/components/meeting/PushToTalkButton.tsx` | เขียน handler ใหม่เป็น Pointer Events + `touch-none` + pointer capture + กัน context menu | RS-5 |
| `frontend/src/components/meeting/SlidesEmbed.tsx` | overlay กินคลิก → `<button>` ขยายเต็มจอ · `min-h-[280px]` ต้องไม่ดันให้สไลด์ล้นจอบน compact | RS-6 |
| `frontend/src/components/meeting/ControlBar.tsx` | ลำดับความสำคัญปุ่ม + hit target + safe area · **และตัดปุ่ม/prop ของแชตเดิมออกตาม CX-5** | RS-9, CX-5 |
| `frontend/src/components/meeting/AiTile.tsx` · `ParticipantTile.tsx` | ย่อบน compact | RS-10 |
| `frontend/src/components/meeting/VolumeControl.tsx` | `PopoverContent w-52` ต้องไม่ล้นจอแคบ · trigger ≥ 44px | RS-9 |
| `frontend/src/app/join/[token]/page.tsx` | `min-h-screen` → `min-h-[100dvh]` · ปุ่มในหน้ายืนยัน (LR-3a) ต้อง ≥ 44px | RS-4 |
| `frontend/src/app/session-ended/[token]/page.tsx` · `app/link-expired/page.tsx` | เหมือนกัน — **✅ อยู่ในขอบเขตแล้วตามมติ U4 (2026-08-23)** แต่ทำได้เฉพาะ `min-h-[100dvh]` + hit target ≥ 44px **ห้าม redesign** | RS-1, RS-4 |
| `frontend/src/app/room/[token]/page.tsx` *(เพิ่มจากมติ U1)* | ลบ `"ready"` ออกจาก `PUSH_TO_TALK_ENABLED_STATES` (บรรทัด ~111-117) + ลบบรรทัดชวนให้พูดตอบใต้ปุ่มเริ่ม (บรรทัด ~195) + เพิ่มปุ่ม "ยังไม่พร้อม" | TQ-18, TQ-24 |
| component ใหม่ของ drawer (CX-6) | เต็มจอบน compact + กฎแป้นพิมพ์ | RS-7, RS-8 |

**RS-14 · เกณฑ์ยอมรับ — `qa-engineer` ตรวจอะไร**

`typecheck`/`lint`/`build` ผ่าน **ไม่ได้พิสูจน์อะไรเลย**สำหรับหัวข้อนี้ (ไม่มี test suite ที่รัน
พฤติกรรมสัมผัสได้) → รอบที่ verify F9 **ต้องรายงานทุกข้อด้านล่างไว้ใต้
`## Unverified Behaviour` ถ้าไม่ได้ทดสอบด้วยอุปกรณ์/emulator จริง** ห้ามติ๊กผ่านด้วยการอ่านโค้ด:

1. กดค้างปุ่มพูดบนจอสัมผัสแล้ว **หน้าไม่เลื่อน** และ **ไม่มี context menu เด้ง**
2. กดค้างแล้วลากนิ้วออกนอกปุ่มแล้วปล่อย → การอัดหยุดจริง ไม่ค้าง
3. เปิดห้องบนมือถือแนวตั้ง → **เห็นปุ่มจบโดยไม่ต้องเลื่อน** ทั้งตอน URL bar กางและหุบ
4. แตะสไลด์ → เต็มจอ · ปิดได้ · บทเรียนไม่รีโหลดและไม่หยุดเล่น
5. โฟกัสช่องพิมพ์บนมือถือ → เห็นทั้งช่องพิมพ์และปุ่มส่ง และหน้าไม่ซูมเข้าเอง (iOS)
6. หมุนจอไปมา → ไม่มีหน้าจอบังคับหมุน และ layout ไม่พัง
7. ทั้ง 6 ข้อต้องผ่านทั้ง **บทเรียน Google Slides** และ **บทเรียน PDF** (สองเส้นทาง render คนละอัน)

---

## Text Question Rules (F10) — contract

> **เขียนเมื่อ 2026-08-23 จากมติ T1–T7 ที่ปิดแล้ว** · engineer ไม่มีสิทธิ์ตัดสินกติกาเอง
> อ่านทั้งหัวข้อก่อนเขียนโค้ด · ต้องอ่านคู่กับ **`## Chat Removal Rules (F10-a)`** เสมอ —
> ช่องพิมพ์ที่ TQ พูดถึงคือช่องเดียวกับที่ CX สั่งให้เปลี่ยนปลายทาง

### ฝั่ง backend

**TQ-1 · endpoint ใหม่ ไม่ใช่การยัดเพิ่มใน `/api/voice-question` — แต่ใช้ service เดียวกัน**

| ชั้น | ตัดสิน | ทำไม |
|---|---|---|
| **Controller** | **แยก**: `POST /api/text-question` (ไฟล์ใหม่ `TextQuestionController.cs`) | transport คนละแบบคนละกฎ — ของเดิมเป็น `multipart/form-data` + `[RequestSizeLimit(10MB)]` + ตรวจว่า `ContentType` ขึ้นต้นด้วย `audio/` · การทำ `IFormFile?` ให้ nullable ในแอ็กชันเดียวแปลว่ากฎ "ต้องมีเสียง **หรือ** ข้อความ อย่างใดอย่างหนึ่ง ห้ามทั้งคู่ ห้ามไม่มีเลย" ไปอยู่ใน if-chain ของ controller ที่ควรบาง และทำให้ request JSON เล็กๆ แบกเพดาน 10MB ไปด้วย |
| **Service** | **ไม่แยก**: `IVoiceQuestionService` เพิ่ม `AskTextAsync(AskTextQuestionDto)` และ **แชร์ core เดียวกับ `AskAsync`** | orchestration หลังได้ข้อความคำถามแล้วเหมือนกัน 100% — resolve session/company (IC-1) · ตรวจ `Ended` · resolve lesson content · ประกอบ 3 namespace ผ่าน `KnowledgeNamespaces`/`IKnowledgeNamespaceResolver` (KS-1) · เขียน `SessionQuestion` · broadcast · `TryRecordConflict` (KS-9/KS-10) · **service ที่สองแปลว่ามีสองที่ให้ลืมแก้ และ KS-1 คือกฎที่ลืมแล้วข้อมูลรั่วข้ามบริษัทโดยไม่มี error ให้เห็น** |
| **Provider** | **ไม่แยก**: `IVoiceQuestionProvider` เพิ่ม `AnswerTextAsync(TextQuestionInput)` | ทั้งสอง implementation ต้องรองรับ — ถ้าแยก interface จะเกิดสถานะที่ `VOICE_QUESTION_PROVIDER=gemini` แล้วพิมพ์ถามไม่ได้ ซึ่งขัด T1 (เทียบเท่า 100%) |

**ห้ามเพิ่ม env ใหม่ · ห้ามเพิ่มค่าใน `ProviderSelection.cs` · ห้ามสร้าง project `Providers.*` ใหม่**

**TQ-2 · สัญญาของ endpoint**

```
POST /api/text-question          [AllowAnonymous]   Content-Type: application/json
body: { token, learnerKey, text, currentSlideObjectId? }
200 : VoiceAnswerViewModel (โครงเดิม ลบฟิลด์ readiness ออกแล้วตาม TQ-22)
```

- **ไม่มีฟิลด์ `expecting`** และ **ไม่มี `durationMs`** — สองค่านี้เป็นของเส้นทางเสียงล้วน (TQ-11)
- ⚠️ **แก้ตามมติ U1 (2026-08-23)**: ข้อความเดิมของบรรทัด `200 :` เขียนว่า "`readiness` = null เสมอ"
  ซึ่งเป็นโมฆะแล้ว — หลัง TQ-22 **`VoiceAnswerViewModel` ไม่มีฟิลด์ `readiness` เหลืออยู่เลย**
  ทั้งเส้นทางพิมพ์และเส้นทางเสียง
- ใช้ ViewModel ตัวเดิม (`VoiceAnswerViewModel`) โดยเจตนา: frontend มี handler เดียวสำหรับผลลัพธ์
  ของคำถาม ไม่ว่าจะเข้ามาทางไหน — ตรงกับ T1
- **ต้องเพิ่ม `/api/text-question` ใน `IsSensitiveLearnerPath()` ที่
  `backend/src/SupportRoom.Api/Program.cs` (~บรรทัด 187)** ไม่งั้น response ของ endpoint ที่รับ
  `learnerKey` จะไม่ได้ `no-store`/`no-referrer` ซึ่งผิด CA-3 ข้อ 1–2 · **ในคอมมิตเดียวกันต้อง
  ลบ `/api/chat-messages` ออกจากรายการนั้นด้วย** (CX-4)

**TQ-3 · validation (ลำดับนี้เป็นสัญญา ห้ามสลับ)**

1. `token`, `learnerKey`, `text` ว่าง/ไม่มี → `400` `"ต้องระบุ token, learnerKey และข้อความคำถาม"`
2. `text.Trim()` แล้วยาว `< 1` → `400` `"กรุณาพิมพ์คำถามก่อนส่ง"`
3. ยาว `> DtoLimits.QuestionTextMaxLength` → `400` `"คำถามต้องมี 1-{QuestionTextMaxLength} ตัวอักษร"`
4. **ค่าที่ใช้ต่อจากนี้คือค่าที่ trim แล้วเสมอ** (รวมค่าที่บันทึกลง `Transcript`)

`DtoLimits.QuestionTextMaxLength = 2000` **เป็นค่าคงที่ใหม่ ไม่ใช่การ reuse `MaxTextLength`** —
`MaxTextLength` วันนี้ใช้ร่วมกันระหว่างข้อความแชต (กำลังถูกลบ) กับข้อความ TTS และ XML doc ของมัน
บอกไว้เองว่าให้แยกออกเมื่อความต้องการต่างกัน · หลัง CX ลบแชตแล้ว ให้แก้ XML doc ของ
`MaxTextLength` ให้เหลือเฉพาะ TTS

**TQ-4 · ลำดับใน service (เหมือน `AskAsync` เป๊ะ ห้ามสลับ — นี่คือสิ่งที่ทำให้ request เป็น company-scoped)**

1. `ILearningSessionService.GetEntityByLearnerKey(token, learnerKey)` — resolve คน **ก่อนแตะอะไรทั้งสิ้น** (IC-1/IC-3)
2. `session.Status == SessionStatus.Ended` → `ValidationError("การเรียนนี้จบแล้ว กรุณากดเรียนอีกครั้งก่อนถามคำถามใหม่")` (ข้อความเดียวกับเส้นทางเสียง)
3. `ITrainingLinkService.GetEntityByToken(token)` → `ILessonConfigService.GetTeachingContentBySlugAsync(link.LessonSlug)`
4. เรียก `AnswerTextAsync` พร้อม 3 namespace ที่ประกอบโดย **caller เท่านั้น** (KS-1) — ค่าเดียวกับเส้นทางเสียงทุกตัว
5. บันทึก + broadcast ตาม TQ-5/TQ-6

**TQ-5 · การบันทึก — จุดที่ T2 บังคับว่าต้อง "เหมือนกันทุกประการ"**

- เขียน `SessionQuestion` **หนึ่งแถวต่อหนึ่งคำถามที่ได้คำตอบ** ผ่าน
  `ISessionQuestionService.Create(session.Id, ...)` **ตัวเดิม** — ห้ามเขียน entity เองในบริการนี้
- `Transcript` = **ข้อความที่ผู้เรียนพิมพ์ (trim แล้ว) ตรงตัว** ไม่ดัดแปลง ไม่สรุป ไม่ต่อท้ายอะไร
- `SlideObjectId` = `result.RelatedSlideObjectId ?? input.CurrentSlideObjectId` (สูตรเดียวกับเสียง)
- `Answer` / `AnswerStatus` = ค่าจาก provider ตรงๆ
- ✅ **U2 (ยืนยัน 2026-08-23)**: `Source = QuestionSource.Text` (เส้นทางเสียงส่ง `QuestionSource.Voice`)
  — **บังคับทั้งสองเส้นทาง ไม่มี default ให้ลืมส่ง**
- **ผลที่ต้องเกิดเองโดยไม่ต้องเขียนโค้ดเพิ่ม และ QA ต้องตรวจว่าเกิดจริง**: คำถามที่พิมพ์
  โผล่ในหน้าสรุปผู้เรียน (RR-5) · โผล่ในหน้ารีวิวของ CS พร้อมช่องถูก/ผิด (RR-1..RR-4) ·
  ถ้าเป็น `not_found` ก็เข้าคิว Q&A ของโมดูล `knowledge-base` (QQ-1) เหมือนคำถามที่พูด
- **ห้ามสร้างตาราง/คอลัมน์/endpoint แยกสำหรับคำถามที่พิมพ์** (T2 ปฏิเสธไว้ชัด)

**TQ-6 · realtime** — `IRealtimeNotifier.NotifyNewQuestionAsync(session.Id, question)` เหมือนเสียงทุกประการ
(group key = `LearningSession.Id` ตาม IC-5) · **ห้ามเพิ่ม event ใหม่** — `ReceiveNewQuestion`
ตัวเดิมครอบคลุมแล้ว และ CS ไม่ควรต้องรู้ว่าคำถามเข้ามาทางไหนเพื่อจะแสดงผลได้

**TQ-7 · สัญญาของ provider**

```csharp
public sealed class TextQuestionInput
{
    /// <summary>ข้อความที่ผู้เรียนพิมพ์ (trim แล้ว) - ทำหน้าที่แทน transcript ของเส้นทางเสียง
    /// ทุกประการ ทั้งใน retrieval query และใน prompt</summary>
    public required string QuestionText { get; init; }
    public required IReadOnlyList<VoiceQuestionSlideContext> LessonSlides { get; init; }
    public string? CurrentSlideObjectId { get; init; }
    public required string LessonNamespace { get; init; }
    public required string CategoryNamespace { get; init; }
    public required string GlobalNamespace { get; init; }
}

// เพิ่มใน IVoiceQuestionProvider - ทั้งสอง implementation ต้องมี
Task<VoiceQuestionResult> AnswerTextAsync(TextQuestionInput input);
```

- **ไม่มี `Expecting`** (TQ-11) · **ไม่มี `Audio`/`MimeType`/`DurationMs`**
- คืน `VoiceQuestionResult` ตัวเดิม โดย `Transcript` = `QuestionText` ที่รับเข้ามา
  (ไม่ใช่ค่าว่าง — `SessionQuestion.Transcript` อ่านจากฟิลด์นี้)

**TQ-8 · `RagVoiceQuestionProvider.AnswerTextAsync` — ข้าม step เดียว ห้ามเขียน pipeline ใหม่**

`TranscribeAndAnswerAsync` วันนี้มี 3 step (1 ถอดเสียง → 2 embed+retrieve → 3 ตอบ) ·
`AnswerTextAsync` คือ **step 2–3 เดิมทุกบรรทัด** โดยใช้ `input.QuestionText` แทน `transcript`
ที่ได้จาก step 1:

- ใช้ `BuildGroundingContextAsync` **ตัวเดิม** (fallback full-deck, threshold, `MergeTopK`,
  logging ต้องได้ประโยชน์เท่ากัน) — ห้าม copy-paste เป็นเมธอดที่สอง
- ใช้ `BuildAnswerPrompt(...)` **ตัวเดิม** และเส้นทาง `useOpenAiAnswer` เดิม
  (`openai-rag` ต้องทำงานกับคำถามที่พิมพ์ได้เหมือนกัน)
- ใช้ conflict flow เดิมผ่าน `VoiceQuestionResult.Conflict` (KS-9/KS-10 ยังบังคับใช้เต็มที่
  รวมทั้งการที่ provider **ห้าม** validate `qnaId` เอง)
- **ห้ามเรียก Gemini เพื่อถอดเสียง** และห้ามส่ง `audio = null` เข้า prompt ถอดเสียงเดิม

**TQ-9 · `GeminiVoiceQuestionProvider.AnswerTextAsync` (โหมด full-deck)**

- prompt ใหม่ `BuildTextPrompt(groundingContext, questionText)` — เนื้อความเหมือน `BuildPrompt`
  เดิมทุกกฎ (**อ้างอิงเฉพาะ Speaker Notes · ห้ามตอบจากความรู้ทั่วไป · ห้ามเดา · ตอบสั้นกระชับ ·
  `relatedSlideObjectId` ต้องเป็น objectId ที่ปรากฏจริงเท่านั้น**) ต่างกันแค่ **ไม่มีการถอดเสียง**
  และคำถามมาเป็นข้อความในตัว prompt
- JSON schema ที่ขอกลับมา **ตัด `transcript` ออก** และค่าที่รับได้เหลือ
  `"answered" | "not_found" | "out_of_scope"` (`transcription_failed` เป็นไปไม่ได้แล้วโดยนิยาม)
- เรียกผ่าน `GeminiRest.CallAsync(httpClientFactory, creds, logger, prompt)` — **ไม่ส่ง audio**
  (overload นี้มีอยู่แล้วและ RAG answer step ใช้อยู่ทุกวัน **ไม่ต้องแก้ `GeminiRest`**)
- **ข้อความคำถามของผู้เรียนคือ untrusted input ที่ถูกวางลงใน prompt** — ต้องวางไว้ในบล็อกที่ระบุ
  ชัดว่าเป็น "คำถามของคุณครู" และ **ห้ามวางก่อนหรือปนกับบรรทัดกติกา** เพื่อลด prompt injection
  (ความเสี่ยงนี้ไม่ใช่ของใหม่ — ข้อความที่ถอดจากเสียงก็ inject ได้ — แต่การพิมพ์ทำได้แม่นยำกว่ามาก ดู R14)

**TQ-10 · กรณีล้มเหลว — คำถามที่พิมพ์ต้องไม่เงียบหาย**

| สถานการณ์ | เส้นทางเสียงทำอะไรวันนี้ | เส้นทางพิมพ์ต้องทำอะไร |
|---|---|---|
| provider เรียกไม่สำเร็จ / parse JSON ไม่ได้ | คืน `transcription_failed` → **ถูกบันทึกเป็นแถว** → frontend เงียบแล้วเล่นต่อ | **`throw GeneralException.UpstreamError(...)` — ไม่เขียนแถวลง DB** และ frontend พูดข้อความแจ้งพลาด (TQ-16) |
| `no_speech` | เกิดได้ (กดค้างสั้นเกินไป) | **เกิดไม่ได้** — ไม่มีเสียง และข้อความว่างถูกปัดตั้งแต่ TQ-3 |
| AI ตอบว่าไม่พบข้อมูล | `not_found` → บันทึก → เข้าคิว Q&A | **เหมือนกันทุกประการ** (นี่คือ "คำตอบ" ไม่ใช่ "ความล้มเหลว") |

เหตุผลที่ต่างกันแค่แถวแรก: `transcription_failed` แปลว่า "ถอดเสียงไม่ได้" ซึ่ง **ไม่มีความหมาย**
กับคำถามที่พิมพ์ — ถ้าปล่อยให้บันทึกด้วยสถานะนี้ CS จะเห็นแถวที่โกหกตัวเองในคิวรีวิว ·
และการ "เงียบแล้วเล่นต่อ" ซึ่งสมเหตุผลกับเสียงที่ฟังไม่ชัด **ดูเหมือนปุ่มส่งพัง** เมื่อผู้เรียน
เพิ่งพิมพ์ประโยคเต็มๆ ส่งไป · **ห้ามเพิ่มค่าใหม่ใน `AnswerStatus`** เพื่อแก้เรื่องนี้ — ค่านั้นจะ
ไหลไปถึง TS union, `answerStatusLabels`, หน้าสรุปผู้เรียน และคิวรีวิว โดยไม่มีใครต้องการมันจริง

**TQ-11 · readiness ตอบได้ทางเดียวคือ "กดปุ่ม" (T6 + ✅ มติ U1 2026-08-23)**

- เส้นทาง **พิมพ์** ไม่มี concept ของ readiness เลย ทั้งใน DTO, provider และ reducer ·
  **ห้ามพยายามเดาว่าข้อความที่พิมพ์เป็นคำตอบ "พร้อม/ไม่พร้อม"**
- เส้นทาง **เสียง** ก็ไม่มีเช่นกันอีกต่อไป — **มติ U1 สั่งถอด readiness-by-voice ออกจากระบบทั้งชุด**
  (TQ-22..TQ-27) · หลังงานรอบนี้ **ไม่มีทางใดในระบบที่ตอบ readiness ได้นอกจากปุ่มสองปุ่มใน TQ-18**
- การบังคับอยู่ที่ TQ-20 (ช่องพิมพ์ถูกปิดตอน `state = "ready"`) + TQ-24 (ปุ่มพูดถูกปิดตอน
  `state = "ready"`) และ **ไม่มีการบังคับฝั่ง server** เพราะ server แยกไม่ออกว่าห้องอยู่สถานะไหน
  — **เป็นการบังคับที่ frontend เท่านั้น เขียนไว้ให้ `qa-engineer` รู้ว่าต้องทดสอบด้วยมือ**
  (เหมือน LR-3a/IC-7)

**TQ-12 · log** — ห้าม log ข้อความคำถาม เหมือนที่ห้าม log `Transcript`/`Answer` วันนี้ ·
log ได้เฉพาะผลลัพธ์ เช่น `"Text question answered: session={SessionId} status={AnswerStatus}"` ·
`token`/`learnerKey`/query string ยังห้าม log ตาม CA-3

### ฝั่ง frontend

**TQ-13 · api-client** — เพิ่ม `askTextQuestion({ token, learnerKey, text, currentSlideObjectId? })`
ใน `frontend/src/lib/api-client.ts` เรียกผ่าน `publicRequest` (JSON) · **`askVoiceQuestion` เดิม
ไม่เปลี่ยน signature แม้แต่ตัวเดียว** — ยังบังคับ `audioBlob` ต่อไป เพราะเส้นทางเสียงยังอยู่ครบ

**TQ-14 · tutor state machine — event ใหม่ 1 ตัว, effect ใหม่ 1 ตัว, state ใหม่ 0 ตัว**

```ts
// intents.ts - TutorUserEvent
| { type: "SUBMIT_TEXT_QUESTION"; text: string }

// types.ts - TutorEffect
| { kind: "SEND_TEXT_QUESTION"; text: string }
```

กฎใน `tutorReducer`:

- รับเฉพาะเมื่อ `runtime.state` อยู่ใน **`PUSH_TO_TALK_STATES` ยกเว้น `"ready"`** →
  `["slide-speaking", "waiting-slide-duration", "final-question-window"]` ·
  สถานะอื่น → `noEffect(runtime)` (ต้องมี guard แม้ UI จะ disable ปุ่มแล้ว)
- ผลลัพธ์: `{ ...runtime, interruptedFrom: runtime.state, state: "processing-question",
  isAiSpeaking: false, afterSpeech: null, micNotice: null }` + effect `SEND_TEXT_QUESTION`
- **ไม่ผ่าน `"push-to-talk-recording"`** — ไม่มีอะไรให้อัด · **ห้ามสร้าง state ใหม่**
  ชื่อทำนอง `"typing"` เพราะการพิมพ์ไม่ใช่สถานะของบทเรียน (T5: บรรยายเดินต่อระหว่างพิมพ์)
- หลังได้คำตอบใช้ event เดิมทุกตัว: `QUESTION_ANSWERED` / `QUESTION_FAILED` ·
  `resumeAfterInterruption` ทำงานถูกอยู่แล้วเพราะ `interruptedFrom` ถูกตั้งไว้
- reducer ต้องยัง **pure** ตาม Architecture Rule 3 — การเรียก API อยู่ใน effect runner ของ hook

**TQ-15 · จังหวะหยุดการบรรยาย (T5) — จุดที่พลาดง่ายที่สุดของงานนี้**

- **การหยุดเกิดที่ "กดส่ง" เท่านั้น** · `dispatch()` ใน `use-tutor-session.ts` เรียก
  `clearPending()` ซึ่งหยุดเสียงและ timer อยู่แล้ว → การส่ง `SUBMIT_TEXT_QUESTION` ทำให้เสียงหยุด
  ทันทีโดยไม่ต้องเขียนโค้ดหยุดเสียงเพิ่ม **ห้ามเขียนโค้ดหยุดเสียงซ้ำอีกที่**
- **ข้อห้ามที่ต้องอ่านให้ขึ้นใจ**: ช่องพิมพ์ **ห้ามมี** `onFocus`, `onChange`, `onKeyDown`
  หรือ handler ใดๆ ที่ `sendEvent(...)` เข้า reducer · การเปิด drawer ก็ห้าม (RS-7) ·
  ผู้เรียนพิมพ์ยาวแค่ไหน ลบทิ้งกี่รอบ AI ก็ยังบรรยายต่อจนกว่าจะกดส่ง — **นี่คือความต่างที่
  เจ้าของโปรเจกต์ระบุเองว่าตั้งใจให้ต่างจาก push-to-talk**
- ผลข้างเคียงที่ยอมรับแล้วและ **ห้าม "แก้"**: ผู้เรียนอาจพิมพ์เสร็จพอดีตอน AI พูดจบสไลด์ แล้ว
  สไลด์เลื่อนไปข้างหน้า ทำให้ `currentSlideObjectId` ที่ส่งไปเป็นสไลด์ใหม่ — ยอมรับได้ (เส้นทางเสียง
  มีปัญหาเดียวกันเมื่อกดค้างพอดีจังหวะเปลี่ยนสไลด์) · **ห้ามแช่ค่าสไลด์ไว้ตั้งแต่ตอนเริ่มพิมพ์**
  เพราะนั่นคือการทำให้ "เริ่มพิมพ์" มีความหมายกับ state machine ซึ่ง T5 ปฏิเสธ

**TQ-16 · effect runner ใน `use-tutor-session.ts`**

```
case "SEND_TEXT_QUESTION": void sendTextQuestion(effect.text); break;
```

`sendTextQuestion` ทำเหมือน `stopRecordingAndSend()` ตั้งแต่จุดที่ได้ข้อความแล้ว:

1. `playProcessingFiller()` — **เรียกเหมือนกัน** (เสียง "รอสักครู่" ต้องมีเหมือนกัน T1/T3)
2. `api.askTextQuestion({ token, learnerKey, text, currentSlideObjectId: currentSlide?.slideObjectId })`
3. `answerStatus` ∈ `answered|not_found|out_of_scope` → `QUESTION_ANSWERED` (payload เดิมทุกฟิลด์)
4. **`catch` → `QUESTION_FAILED`** (reducer เดิมพูดข้อความแจ้งพลาด) — คู่กับ TQ-10 ที่ทำให้
   backend โยน error แทนการคืน `transcription_failed`
5. **ไม่มีการเช็ค `result.readiness`** และ **ไม่มีการ map เป็น `NO_SPEECH`** ในเส้นทางนี้

**TQ-17 · TTS (T3)** — ไม่มีอะไรต้องทำเพิ่ม และ **นั่นคือกติกา**: `QUESTION_ANSWERED` เดิม
สั่ง `SPEAK` อยู่แล้ว → คำตอบถูกอ่านออกเสียงเสมอ · **ห้ามเพิ่ม flag/prop/setting ใดๆ ที่ทำให้
คำตอบของคำถามที่พิมพ์ไม่ถูกอ่าน** และห้ามเพิ่มปุ่มปิดเสียงคำตอบ (`VolumeControl` เดิมที่ปรับ
*ระดับ* เสียงเป็นคนละเรื่อง)

**TQ-18 · ปุ่ม readiness (T6) — ✅ หลังมติ U1 นี่คือ *ทางเดียว* ที่ตอบ readiness ได้**

- ตอน `runtime.state === "ready"` หน้าห้องแสดงปุ่ม **"พร้อมแล้ว เริ่มเรียนเลย"** (มีอยู่แล้ว
  ส่ง `START`) และเพิ่มปุ่ม **"ยังไม่พร้อม"**
- "ยังไม่พร้อม" ต้อง **ยกเลิก timer auto-start** (`WAIT_READY_TIMEOUT`) ไม่งั้นกดแล้วบทเรียน
  ก็ยังเริ่มเองในอีกไม่กี่วินาที = ปุ่มไม่ได้ทำอะไร → ต้องมี event ใหม่ **`{ type: "NOT_READY" }`**
  ใน `TutorUserEvent` ที่รับเฉพาะ `state === "ready"` และให้ผลเท่ากับ
  `READINESS_ANSWERED { ready: false }` วันนี้ทุกประการ (พูด `notReadyScript` แล้ว
  `afterSpeech: "AWAIT_READINESS"` — **ไม่มี** timer auto-start)
- ⚠️ **แก้ตามมติ U1 (2026-08-23)**: ข้อความเดิมของข้อนี้เขียนว่า "ห้ามแก้ guard ของ
  `READINESS_ANSWERED` ให้รับ state `"ready"` เพิ่ม" — **เป็นโมฆะแล้วเพราะ `READINESS_ANSWERED`
  ถูกลบทิ้งทั้ง event** (TQ-25) · `NOT_READY` ไม่ใช่การผ่อน guard ของ event เดิม แต่เป็น event ใหม่
  ที่รับเฉพาะ `state === "ready"` และเป็น **ผู้ผลิตเพียงรายเดียวที่เหลือของ `AfterSpeechAction`
  `"AWAIT_READINESS"`** — ห้ามให้ state อื่นส่ง `NOT_READY` ได้
- ช่องพิมพ์ต้อง **disabled + มี placeholder อธิบาย** ตอน `state === "ready"`
  (เช่น `"เลือกพร้อม/ยังไม่พร้อมด้านบนก่อนนะคะ"`) — ห้ามซ่อนหายไปเฉยๆ เพราะผู้เรียนจะเข้าใจว่า
  ระบบนี้พิมพ์ไม่ได้เลย
- ✅ **U1 เคาะแล้ว (2026-08-23): ตัดการตอบ readiness ด้วยเสียงทิ้ง** — เจ้าของโปรเจกต์เลือก
  **ตรงข้ามกับข้อเสนอของ `system-analyst`** (ที่เสนอให้คงเสียงไว้) โดยเห็น trade-off ครบแล้ว
  รวมถึงว่ามันแตะโค้ดที่ผ่าน QA ไปแล้ว · **หลังรอบนี้ readiness ตอบได้ทางเดียวคือกดปุ่ม —
  ทั้งพิมพ์และพูดใช้ตอบจุดนี้ไม่ได้เลย** · รายการที่ต้องรื้อครบทุกจุดเป็นสัญญาอยู่ที่
  **TQ-22..TQ-27 ด้านล่าง** (⛔ ห้ามอ่านข้ามแล้วรื้อครึ่งเดียว — นี่คืองานรื้ออีกก้อน
  ไม่ใช่การลบบรรทัดเดียว)

**TQ-19 · ตำแหน่งของช่องพิมพ์** — อยู่ใน drawer ตัวใหม่ (CX-6) ตาม R3c · **ห้ามมีช่องพิมพ์
ที่สองในห้องเรียนไม่ว่าที่ไหน** (T4) · ปุ่มเปิด drawer ใน `ControlBar` ยังอยู่ แต่ label/aria
ต้องเลิกใช้คำว่า "แชต" (CX-5)

**TQ-20 · matrix สถานะของช่องพิมพ์และปุ่มส่ง (ตารางนี้คือสัญญา — engineer ห้ามเดา)**

| `runtime.state` | ช่องพิมพ์ | ปุ่มส่ง | หมายเหตุ |
|---|---|---|---|
| `idle` · `preparing` · `slide-loading` · `restarting-slide` | disabled | disabled | บทเรียนยังไม่พร้อม |
| `ready` | **disabled** | disabled | T6 — ใช้ปุ่มพร้อม/ยังไม่พร้อมแทน (TQ-18) · **ปุ่มพูดต้อง disabled ที่ state นี้ด้วยตามมติ U1** (TQ-24) — เดิมเปิดอยู่ |
| `slide-speaking` · `waiting-slide-duration` · `final-question-window` | **enabled** | enabled เมื่อ `text.trim()` ไม่ว่าง | เส้นทางหลักของ F10 |
| `push-to-talk-recording` | disabled | disabled | กำลังถามด้วยเสียงอยู่ |
| `processing-question` · `answer-speaking` | **enabled (พิมพ์ร่างได้)** | **disabled** | พิมพ์รอได้ แต่ส่งซ้อนไม่ได้ — ตรงกับ push-to-talk ที่กดซ้อนไม่ได้ |
| `paused` · `completed` · `error` | disabled | disabled | |

**TQ-21 · test ที่ต้องมี**

| ไฟล์ | ต้องครอบ |
|---|---|
| `backend/tests/.../VoiceQuestionServiceTests.cs` | `AskTextAsync`: บันทึก `SessionQuestion` ด้วย `Transcript` = ข้อความที่พิมพ์ตรงตัว · session ที่ `Ended` แล้วถูกปฏิเสธด้วยข้อความเดียวกับเส้นทางเสียง · namespace ทั้ง 3 ถูกประกอบจาก `CurrentCompanyId` (KS-1) · provider ล้มเหลว → โยน error และ **ไม่มีแถวถูกเขียน** (TQ-10) · ✅U2: `Source = "text"` และเส้นทางเสียงยังได้ `"voice"` · **✅U1: ลบ `AskAsync_UnclearReadinessReply_DefaultsToNotReady` (บรรทัด ~162-173) ทั้งเคส และตัดพารามิเตอร์ `expecting` ออกจาก helper `Ask(...)` (บรรทัด ~118/126)** — ดู TQ-26 |
| frontend tutor reducer tests | `SUBMIT_TEXT_QUESTION` จาก 3 state ที่อนุญาต → `processing-question` + `interruptedFrom` ถูกตั้ง · จาก `"ready"` → **ไม่เกิดอะไรเลย** · จาก `"processing-question"` → ไม่เกิดอะไรเลย · `NOT_READY` จาก `"ready"` → ไม่มี `WAIT_READY_TIMEOUT` ตามมา · **✅U1: `PUSH_TO_TALK_START` จาก `"ready"` → ไม่เกิดอะไรเลย** (เคสใหม่ที่ต้องมี ไม่ใช่แค่ลบของเก่า) และ describe block `"answering the readiness prompt by voice"` (`tutor-reducer.test.ts` ~บรรทัด 141-190) ถูกลบ/เขียนใหม่เป็นเคสของ `NOT_READY` — ดู TQ-26 |

### 🆕 การถอด readiness-by-voice ออกจากระบบ (มติ **U1** · 2026-08-23) — TQ-22..TQ-27

> ⚠️ **อ่านก่อนลงมือ: หัวข้อนี้ไม่ใช่การ "เพิ่มฟีเจอร์" แต่เป็นการ *ถอด* พฤติกรรมที่ ship แล้วและ
> ผ่าน `qa-engineer` FULL-3 ไปแล้ว** — เจ้าของโปรเจกต์ตัดสินใจเต็มที่หลังเห็น trade-off
> (ไม่ใช่ความเข้าใจผิด **ห้ามถามซ้ำ ห้ามเสนอให้คงเสียงไว้เผื่อ**) · ผลคือ **ขนาดงานของรอบนี้
> ใหญ่กว่าที่ประเมินไว้ตอนสัมภาษณ์ F9/F10** และมี **regression surface พาดเข้า Module C/D/E**
> ที่ปิดไปแล้ว (ดู R19)
>
> **เกณฑ์ปิดงานของ TQ-22..TQ-27 คือ "ไม่เหลือ readiness/expecting ที่เป็นโค้ดจริง" ไม่ใช่ "build ผ่าน"**
> — ของพวกนี้ส่วนใหญ่เป็น `string` และ optional field การรื้อครึ่งเดียวจึงคอมไพล์ผ่านได้สบาย

**TQ-22 · backend — contract/wire ที่ต้องหายไป (นี่คือการเปลี่ยน response shape ที่ผ่าน QA แล้ว)**

| ที่อยู่จริง (ตรวจไฟล์แล้ว) | ทำอะไร |
|---|---|
| `Application/Dto/AskVoiceQuestionDto.cs:24-25` `Expecting` | **ลบ** พร้อม XML doc |
| `Api/Controllers/VoiceQuestionController.cs:25` `VoiceQuestionRequest.Expecting` + การ map บรรทัด ~70 (`Expecting = request.Expecting == "readiness" ? "readiness" : "question"`) | **ลบทั้งคู่** — หลังจากนี้ multipart field ชื่อ `expecting` ที่ client เก่ายังส่งมาจะถูก **เมินเงียบๆ ไม่ error** (ASP.NET ไม่ bind field ที่ไม่มีใน model) ซึ่งยอมรับได้เพราะ frontend deploy พร้อมกันเสมอ (TD ของโมดูลนี้: frontend เป็น consumer เดียว) |
| `Providers.VoiceQuestion/IVoiceQuestionProvider.cs:38-42` `VoiceQuestionInput.Expecting` | **ลบ** พร้อม XML doc |
| `Providers.VoiceQuestion/IVoiceQuestionProvider.cs:63-64` `VoiceQuestionResult.Readiness` | **ลบ** |
| `Application/ViewModel/VoiceAnswerViewModel.cs:9` `Readiness` | **ลบ** — ⚠️ **เปลี่ยน response shape ของ `POST /api/voice-question`** ต้องแก้ TS ในคอมมิตเดียวกันตามกฎ root `CLAUDE.md` ข้อ 7 |
| `Providers.VoiceQuestion/GeminiRest.cs:64` `GeminiAnswerJson.Readiness` | **ลบ** |
| `Application/Services/IVoiceQuestionService.cs:78` (`Expecting = input.Expecting`) และบล็อก `if (result.Readiness is not null) { Logger...; return ...; }` บรรทัด ~90-95 | **ลบทั้งสองจุด** — บล็อกนี้คือ early-return ที่ทำให้ readiness ไม่ถูกบันทึกเป็นคำถาม เมื่อไม่มี readiness แล้วมันกลายเป็น dead branch ที่ทำให้คนอ่านเข้าใจผิดว่ายังมีเส้นทางที่สอง |

**ห้ามคง property ไว้แบบ `[Obsolete]` หรือ "รับค่าแล้วเมิน"** — ค่าที่ยังรับได้แต่ไม่มีผลคือสิ่งที่
ทำให้รอบหน้ามีคนคิดว่าฟีเจอร์ยังอยู่แล้วต่อยอดจากมัน

**TQ-23 · backend — provider ทั้งสองตัว (ห้ามแตะเส้นทางคำถามจริงที่ใช้ร่วมกัน)**

- `RagVoiceQuestionProvider.cs`: ลบ `BuildReadinessPrompt()` (~บรรทัด 51-58) และลบบล็อก
  `if (input.Expecting == "readiness") { ... }` (~บรรทัด 102-118) **ทั้งก้อน** → เหลือ pipeline
  3 step เดิมเส้นเดียว
- `GeminiVoiceQuestionProvider.cs`: ลบ `BuildReadinessPrompt()` (~บรรทัด 36-44) · ลบตัวแปร
  `isReadiness` (~บรรทัด 54) และ **ทุก branch ที่ใช้มัน** — `groundingContext` (~55-57) กลับไป
  join speaker notes เสมอ · `GeminiRest.CallAsync(...)` (~59-60) เรียก `BuildPrompt(groundingContext)`
  เสมอ · ลบบล็อก `if (isReadiness) { return new VoiceQuestionResult { ... Readiness = ... }; }`
  (~75-85)
- ⛔ **ห้ามลบหรือแก้** `BuildPrompt`, `GeminiRest.CallAsync`, `GeminiRest.IsAnswerStatus`,
  guard `input.DurationMs < UploadLimits.MinVoiceDurationMs` → `no_speech` — ทั้งหมดเป็นของ
  **เส้นทางคำถามด้วยเสียงที่ยังอยู่ครบ** (T1 ไม่ได้ตัดการ *ถาม* ด้วยเสียง ตัดแค่การ *ตอบ readiness*)

**TQ-24 · frontend — state machine + ห้องเรียน (มีลิสต์ซ้ำสองใบ ลบใบเดียวแล้วพังเงียบ)**

1. `tutor/tutor-reducer.ts:59-66` — `PUSH_TO_TALK_STATES` **ลบ `"ready"`** และลบคอมเมนต์บรรทัด
   59-60 ที่อธิบายว่าทำไม `"ready"` ถึงอยู่ในนั้น (คอมเมนต์ที่อธิบายพฤติกรรมที่ไม่มีแล้ว = คอมเมนต์
   ที่โกหก) · **`PAUSABLE_STATES` บรรทัด 67 ห้ามแตะ** — การ *พัก* ตอน `ready` เป็นคนละเรื่องกับ
   การ *พูดถาม*
2. `app/room/[token]/page.tsx:111-117` — `PUSH_TO_TALK_ENABLED_STATES` **ลบ `"ready"`** + คอมเมนต์
   บรรทัด 111 · ⚠️ **นี่คือรายการใบที่สอง**: UI ตัดสินสถานะปุ่มจากลิสต์ของตัวเอง ถ้าลบแต่ในreducer
   ปุ่มจะยัง **กดได้** แต่ reducer เมิน = ปุ่มที่กดแล้วไม่เกิดอะไรและไม่มี error ให้เห็น
3. `app/room/[token]/page.tsx:195` — ลบบรรทัด
   `หรือกดปุ่ม "กดค้างเพื่อพูด" แล้วบอกว่าพร้อมแล้วก็ได้ค่ะ` (ข้อความที่สั่งให้ผู้เรียนทำสิ่งที่
   เพิ่งถูกถอดออก) และวางปุ่ม **"ยังไม่พร้อม"** ตาม TQ-18 ไว้ที่ overlay เดียวกันกับปุ่ม
   "พร้อมแล้ว เริ่มเรียนเลย" (บรรทัด 192-197)
4. `tutor/tutor-reducer.ts:120-131` `resumeAfterInterruption` — branch
   `if (runtime.interruptedFrom === "ready")` **กลายเป็น dead code** เพราะ `interruptedFrom`
   ถูกตั้งจาก `PUSH_TO_TALK_START` (บรรทัด 241) และ `SUBMIT_TEXT_QUESTION` (TQ-14) เท่านั้น
   ซึ่งทั้งคู่ไม่รับ state `"ready"` แล้ว → **ลบ branch ทิ้ง** · type ของ `interruptedFrom`
   ยังเป็น `TutorState | null` เหมือนเดิม **ห้ามทำให้แคบลง** (state อื่นยังใช้อยู่)

**TQ-25 · frontend — ของที่หมดผู้ผลิต: อันไหนลบ อันไหนห้ามลบ (ตารางนี้คือสัญญา ห้ามเดา)**

| สิ่งของ | ที่อยู่ | ทำอะไร | เหตุผล |
|---|---|---|---|
| event `READINESS_ANSWERED` | `tutor/intents.ts:36` + `tutor/tutor-reducer.ts:282-292` | **ลบ** | ผู้ผลิตเดียวคือ `result.readiness` จาก provider ซึ่งหายไปหมดตาม TQ-22 |
| `AfterSpeechAction` `"START_FIRST_SLIDE"` | `tutor/types.ts:32` + case ใน `TTS_ENDED` `tutor-reducer.ts:180-181` | **ลบทั้งค่าและ case** | ถูกตั้งที่เดียวคือ `READINESS_ANSWERED { ready: true }` |
| ฟังก์ชัน `startFirstSlide()` | `tutor-reducer.ts:92-95` | ⛔ **ห้ามลบ** | `START` (ปุ่มพร้อม) และ `INTRO_TIMEOUT` ยังเรียกอยู่ — ลบแล้วบทเรียนเริ่มไม่ได้เลย |
| `AfterSpeechAction` `"AWAIT_READINESS"` | `tutor/types.ts:34` + case `tutor-reducer.ts:182-186` | ⛔ **ห้ามลบ** | `NOT_READY` (TQ-18) กลายเป็นผู้ผลิตรายเดียวที่เหลือ |
| `readyConfirmScript` | `tutor/scripts.ts:11` | **ลบ** | ผู้ใช้เดียวคือ `READINESS_ANSWERED { ready: true }` · ปุ่ม "พร้อมแล้ว" ใช้ `START` ซึ่งไม่พูดตอบอยู่แล้ววันนี้ — **ห้ามเพิ่มเสียงตอบรับใหม่ให้ปุ่มเพื่อ "ชดเชย"** (เปลี่ยนพฤติกรรมที่ไม่มีใครสั่ง) |
| `notReadyScript` | `tutor/scripts.ts:12` | ⛔ **ห้ามลบ แต่ *ต้อง* แก้ข้อความ** | `NOT_READY` ยังใช้ · ข้อความวันนี้คือ *"…พร้อมเมื่อไหร่**กดปุ่มพูด**แล้วบอกได้เลยค่ะ"* ซึ่งสั่งให้ทำสิ่งที่เพิ่งถูกถอด → แก้ให้ชี้ปุ่ม เช่น `"ได้ค่ะ ไม่ต้องรีบนะคะ พร้อมเมื่อไหร่กดปุ่มพร้อมแล้วได้เลยค่ะ"` **และถ้อยคำต้องตรงกับ label ปุ่มจริงในหน้าห้อง** · คอมเมนต์บรรทัด 9-10 ที่อธิบายว่า "ตอบด้วยเสียงแทนการคลิก" ต้องแก้ตาม |
| `introScript()` | `tutor/scripts.ts:5-7` | **ไม่แตะ** | "พร้อมเริ่มหรือยังคะ?" ยังถูกต้อง — คำถามเดิม เปลี่ยนแค่ช่องทางตอบ |

**TQ-26 · frontend — api-client / hook / types / test**

- `lib/api-client.ts:457-458` ลบ `expecting?: "question" | "readiness"` ออกจาก input type
  และ `:465-466` ลบ `if (input.expecting) formData.append("expecting", ...)`
- `types/domain.ts:265-266` ลบ `readiness?: "ready" | "not_ready"` + คอมเมนต์เหนือมัน
- `hooks/use-tutor-session.ts:346` ลบ
  `const expecting = runtimeRef.current.interruptedFrom === "ready" ? "readiness" : "question"` ·
  `:358` เลิกส่ง `expecting` เข้า `askVoiceQuestion` · `:363-364` ลบบล็อก
  `if (result.readiness) { dispatch({ type: "READINESS_ANSWERED", ... }); }` ทั้งก้อน
  **พร้อมตรวจว่าเส้นทางที่เหลือ (`QUESTION_ANSWERED`/`QUESTION_FAILED`) ยังครอบทุก path ของ
  `stopRecordingAndSend()`** — ลบ early-return แล้วทำให้ผลลัพธ์ readiness เก่าไหลไปเข้า
  `QUESTION_ANSWERED` ไม่ได้ เพราะ backend ไม่ผลิตมันแล้ว
- test ตาม TQ-21 (ทั้งฝั่ง .NET และ vitest)

**TQ-27 · เอกสารที่ต้องตามไปแก้ + เกณฑ์ปิดงาน**

เอกสารที่ `grep` แล้วพบว่าอธิบาย readiness-by-voice ไว้ (ไม่ใช่การเดา): `frontend/docs/STATE_MACHINE.md`
(แผนภาพมี `READINESS_ANSWERED` ตรงๆ) · `frontend/docs/API_CONTRACT.md` ·
`frontend/docs/SYSTEM_LOGIC.md` · `frontend/docs/SEQUENCE_DIAGRAMS.md` ·
`frontend/docs/GEMINI_INTEGRATION.md` · `frontend/docs/TESTING_GUIDE.md` · `docs/PROJECT_CONTEXT.md` ·
`docs/UX_UI_WORKFLOWS.md` · `docs/SOLUTION_ARCHITECTURE.md` · `docs/BACKEND_DB_HANDOFF.md` ·
`docs/PROVIDER_SETTINGS_SPEC.md`

**เกณฑ์ปิดงาน:** `grep -ri "readiness\|expecting" backend/src frontend/src` ต้องไม่เหลือผลลัพธ์
ที่เป็นโค้ดจริง · **ข้อยกเว้นเดียวที่รู้แล้ว**: `Domain/Entities/KnowledgeQnAConflict.cs:29`
คอมเมนต์ *"Null when that question was never recorded (e.g. a readiness check)"* —
**แก้ถ้อยคำอย่างเดียว ห้ามแตะ logic**: หลังรอบนี้กรณีที่ยังทำให้ค่าเป็น null คือ
`AnswerStatus.NoSpeech` (คำถามที่ไม่มีเสียงพูดจริงไม่ถูกบันทึกเป็นแถว) ไม่ใช่ readiness check

---

## Chat Removal Rules (F10-a) — contract

> **เขียนเมื่อ 2026-08-23 จากมติ T4-a ที่เจ้าของโปรเจกต์ยืนยันแล้ว 2 รอบ** · นี่คือ **งานรื้อของเดิม
> ไม่ใช่งานเพิ่มของใหม่** — ต่างจากทุกหัวข้อ contract อื่นในเอกสารนี้ · เกณฑ์ว่า "เสร็จ" คือ
> `grep -ri "chatmessage\|sendchatmessage\|chat-messages\|ChatDrawer\|use-session-chat\|use-agent-session-chat"`
> บน `backend/src`, `backend/tests`, `frontend/src` **ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง**
>
> ⛔ `requirement.md` §Constraints สั่งไว้ตรงๆ ว่า **ห้าม `system-analyst`/`qa-engineer` ยกขึ้นมา
> ถามซ้ำ** ว่าควรมีทางสำรองให้คุยกับคนไหม — ถามและตอบไปแล้ว 2 รอบ

**CX-1 · ขอบเขตจริงกว้างกว่าที่ `requirement.md` ไล่ไว้**

`requirement.md` (ตาราง T4-a) ระบุ 5 รายการฝั่งผู้เรียน และเขียนกำกับเองว่า *"หน้าฝั่ง CS ที่อ่าน/
ตอบแชต (ถ้ามี) — `system-analyst` ต้องไล่ให้ครบ เอกสารนี้ไม่ได้ตรวจฝั่ง `/admin/*` ไว้"*
· **ตรวจแล้ว: มีฝั่ง CS เต็มรูปแบบจริง** — hub method 2 ตัว, hook 1 ตัว, ปุ่ม + drawer ในหน้า
`admin/learning-sessions/[id]`, endpoint `by-learning-session` · **รวมของที่ต้องรื้อ 26 จุด
+ migration 1 ใบ**

**CX-2 · กับดักที่ทำให้ Phase 6 พังเงียบถ้ารื้อแบบตรงไปตรงมา**

`frontend/src/hooks/use-agent-session-chat.ts` **ทำสองหน้าที่ในไฟล์เดียว**:
(ก) แชต CS — ของที่ต้องลบ (ข) รับ `ReceiveNewQuestion` = **คำถามสดของผู้เรียนที่ไหลเข้าหน้ารีวิว
ของ CS ทันที** ซึ่งเป็นฟีเจอร์ของ Module F ที่ **ไม่มีใครสั่งให้ลบ**

→ **ห้ามลบไฟล์นี้ทิ้ง** · ให้ **เขียนใหม่เป็น `use-agent-session-questions.ts`** ที่เหลือเฉพาะ
`JoinSessionAsAgent` + `ReceiveNewQuestion` + `liveQuestions` (ตัด `chatMessages`,
`sendChatMessage`, `getChatMessagesByLearningSession`) · `admin/learning-sessions/[id]/page.tsx`
ยังต้องได้ `liveQuestions` และ `mergeQuestions` ต่อไปเหมือนเดิม

**CX-3 · ฝั่งผู้เรียนตรงกันข้าม — SignalR ทั้งเส้นไม่มีใครใช้แล้ว**

`use-session-chat.ts` คืน `liveQuestions` ออกมาด้วย **แต่ `room/[token]/page.tsx` ไม่เคยใช้ค่านั้นเลย**
(ห้องใช้ `runtime.questions` ที่ reducer เก็บเอง — ตรวจโค้ดยืนยันแล้ว) · เมื่อแชตหายไป
**ผู้เรียนไม่มีเหตุผลใดเหลือให้ต่อ SignalR**

→ **ลบ `use-session-chat.ts` ทั้งไฟล์** และ **ลบ `SessionHub.JoinSession(token, learnerKey)`** ด้วย
· การถอด `JoinSession` **ไม่กระทบ CS**: `NotifyNewQuestionAsync` broadcast ไปที่ group
`LearningSession.Id` และ CS เข้า group นั้นเองผ่าน `JoinSessionAsAgent` — ผู้เรียนไม่เคยต้องอยู่ใน
group เพื่อให้ CS ได้ยิน · **ผลข้างเคียงที่ดี**: anonymous hub method ที่ resolve session จาก
`(token, learnerKey)` หายไปหนึ่งตัว = ลด surface ที่ `security` (ซึ่งยังไม่เคย audit Phase 3–6 เลย)
ต้องตรวจ · ⚠️ ถ้าอนาคตต้องการ live update ฝั่งผู้เรียน ต้องเปิดใหม่ผ่าน design amendment
**ไม่ใช่แอบเก็บ dead code ไว้เผื่อ**

**CX-4 · รายการรื้อฝั่ง backend (ทุกแถวคือ "ต้องหายไป" เว้นที่ระบุว่าแก้)**

| # | ไฟล์ / จุด | ทำอะไร |
|---|---|---|
| 1 | `Domain/Entities/ChatMessage.cs` | ลบทั้งไฟล์ (DM-4) |
| 2 | `Domain/Enums/ChatSenderRole.cs` | ลบทั้งไฟล์ (DM-6a) |
| 3 | `Application/Services/IChatMessageService.cs` | ลบทั้งไฟล์ (interface + impl อยู่ไฟล์เดียวกันตาม convention) |
| 4 | `Application/Dto/SendChatMessageDto.cs` | ลบทั้งไฟล์ |
| 5 | `Application/ViewModel/ChatMessageViewModel.cs` | ลบทั้งไฟล์ |
| 6 | `Application/Common/MapsterConfig.cs` (~57) | ลบ `TypeAdapterConfig<ChatMessage, ChatMessageViewModel>` |
| 7 | `Application/Realtime/IRealtimeNotifier.cs` (~19) | ลบ `NotifyChatMessageAsync` · **เก็บ `NotifyNewQuestionAsync` ไว้** |
| 8 | `Api/Realtime/SignalRRealtimeNotifier.cs` (~15–16) | ลบ implementation ของเมธอดนั้น |
| 9 | `Api/Controllers/ChatMessagesController.cs` | ลบทั้งไฟล์ (endpoint หายทั้ง 2 เส้น) |
| 10 | `Api/Configurations/ServiceConfiguration.cs` (~44) | ลบ `AddScoped<IChatMessageService, ChatMessageService>()` |
| 11 | `Api/Hubs/SessionHub.cs` | ลบ `SendChatMessage`, `SendChatMessageAsAgent`, **และ `JoinSession` (CX-3)** · เก็บ `JoinSessionAsAgent` + `EnsureAgentAuthenticated` + `EnsureLearningSessionExists` · `ResolveLearningSession`/`ResolveLearningSessionId` จะไม่มีคนเรียกแล้ว ให้ลบด้วย · **แก้ XML doc หัวคลาสให้ตรงความจริงใหม่** (วันนี้อธิบาย `ReceiveChatMessage` ไว้) |
| 12 | `Api/Program.cs` (~191) | ลบ `/api/chat-messages` จาก `IsSensitiveLearnerPath` · **เพิ่ม `/api/text-question`** (TQ-2) |
| 13 | `Providers.Data/Repository/IChatMessageRepository.cs` | ลบทั้งไฟล์ |
| 14 | `Providers.Data/Data/UnitOfWork/UnitOfWork.cs` (~21) | ลบการ register (DI พังตอน runtime ไม่ใช่ตอน compile ถ้าลืม) |
| 15 | `Providers.Data/Data/ApplicationDbContext.cs` (~34, ~116) | ลบ `DbSet` + บล็อก `builder.Entity<ChatMessage>` (DM-7a) |
| 16 | `Application/Services/IAdminService.cs` (~74–79) | `ResetDemoData` เลิกลบ chat (ตารางไม่มีแล้ว) |
| 17 | `Domain/Entities/TrainingLink.cs` (~10) · `LearningSession.cs` (~11) | **comment-only** — XML doc พูดถึง `ChatMessage.SessionId` ต้องแก้ให้ตรงความจริง |
| 18 | migration ใหม่ | `DropTable("ChatMessage")` — ดู MG-R1 |

**CX-5 · รายการรื้อฝั่ง frontend**

| # | ไฟล์ / จุด | ทำอะไร |
|---|---|---|
| 19 | `hooks/use-session-chat.ts` | ลบทั้งไฟล์ (CX-3) |
| 20 | `hooks/use-agent-session-chat.ts` | **เขียนใหม่เป็น `use-agent-session-questions.ts`** (CX-2) ไม่ใช่ลบทิ้ง |
| 21 | `components/meeting/ChatDrawer.tsx` | ลบทั้งไฟล์ · แทนด้วย component ใหม่ตาม CX-6 |
| 22 | `lib/api-client.ts` (~414–426) | ลบ `getOwnChatMessages` + `getChatMessagesByLearningSession` · ลบ import `ChatMessage` |
| 23 | `types/domain.ts` (~294–302) | ลบ type `ChatMessage` + `ChatSenderRole` · แก้คอมเมนต์บรรทัด ~115 ที่พูดถึง `ChatMessage.sessionId` |
| 24 | `app/room/[token]/page.tsx` | ลบ `useSessionChat`, `chat.*`, prop `chatMessages`/`onSendMessage` · เปลี่ยนไปใช้ component ใหม่ + `onSubmitQuestion` |
| 25 | `components/meeting/ControlBar.tsx` | prop `onToggleChat` เปลี่ยนชื่อให้สื่อว่าเปิดช่องถาม AI · `title`/`aria-label` **เลิกใช้คำว่า "แชต"** (เช่น `"ถาม-ตอบกับ AI"`) เพราะคำนี้ทำให้ผู้เรียนคาดหวังว่าจะได้คุยกับคน ซึ่งคือความคาดหวังที่ T4-a ตั้งใจตัดทิ้ง |
| 26 | `app/admin/learning-sessions/[id]/page.tsx` | ลบปุ่ม "แชท" + `chat.chatMessages` + `onSendMessage` · เหลือรายการคำถามอย่างเดียว (คำถามสดยังต้องมา — CX-2) |

**CX-6 · component ที่มาแทน `ChatDrawer` — เขียนใหม่ ไม่ reuse ไฟล์เดิม**

**มติ: ลบ `ChatDrawer.tsx` แล้วสร้าง `frontend/src/components/meeting/AskAiDrawer.tsx`**
(ชื่ออื่นที่สื่อความหมายเดียวกันใช้ได้ แต่ **ห้ามใช้คำว่า Chat ในชื่อไฟล์/component/prop**)

เหตุผลที่ไม่เก็บชื่อเดิมทั้งที่ layout ใช้ซ้ำได้: หลัง T4-a คำว่า "chat" ไม่มีอยู่ในโดเมนนี้อีกแล้ว —
component ชื่อ `ChatDrawer` ที่ไม่มี chat จะทำให้คนอ่านโค้ดรอบหน้าออกตามหาฟีเจอร์ที่ถูกลบไปแล้ว
(เหตุผลเดียวกับที่ Q2 ตัดสินว่าลิงก์ต้องไม่ชื่อ `TrainingSession` ต่อไป)

**ยก layout เดิมมาใช้ได้ และควรใช้** (ไม่ต้องเสียงานที่ทำไว้แล้ว): โครง drawer, timeline ที่เรียงตาม
`createdAt`, การแสดง `transcript` + `answer` + `answerStatusLabels`, Enter = ส่ง,
การคง draft ไว้เมื่อส่งไม่สำเร็จ

**ต้องหายไป**: prop `chatMessages`, `TimelineEntry` แบบ 2 ชนิด, `senderLabel`, `kind: "chat"`
ทุกจุด — timeline เหลือชนิดเดียวคือ `SessionQuestion`

props ใหม่: `{ open, onClose, questions, onSubmitQuestion, inputEnabled, sendEnabled, disabledHint? }`
โดย `inputEnabled`/`sendEnabled` มาจากตาราง **TQ-20** (component ไม่ตัดสินเอง — มันไม่รู้จัก
tutor state และไม่ควรรู้จัก) · กติกาหน้าจอบน compact อยู่ที่ **RS-7/RS-8**

**CX-7 · ข้อความ/ป้ายที่ต้องเปลี่ยนคำ**

- หัว drawer เดิมเขียนว่า **"แชตสำรอง"** → ต้องเปลี่ยน (เช่น `"ถาม-ตอบกับผู้ช่วย AI"`)
- empty state เดิม *"กดค้างปุ่มไมค์เพื่อถามคำถาม หรือพิมพ์ข้อความได้เลยค่ะ"* ยังใช้ได้
  แต่ต้องไม่สื่อว่ามีคนอ่าน
- **ห้ามมีข้อความใดในห้องเรียนที่บอกเป็นนัยว่าจะมีเจ้าหน้าที่มาตอบ** — ผู้เรียนที่นั่งรอคำตอบจากคน
  ที่ไม่มีวันมา คือผลเสียที่แย่กว่าการไม่มีช่องทางเลย

**CX-8 · เอกสารในโปรเจกต์ที่ต้องตามไปแก้**

`docs/schema.dbml` (ลบ `Table ChatMessage` + `Ref`) · `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`
(~156) · `frontend/docs/ER_DIAGRAM.md` · `frontend/docs/API_CONTRACT.md` (2 endpoint หาย +
`/api/text-question` ใหม่ + hub methods) · `frontend/docs/SYSTEM_LOGIC.md` ·
`frontend/docs/SEQUENCE_DIAGRAMS.md` · `frontend/docs/USE_CASE_DIAGRAM.md` ·
`frontend/docs/DATA_FLOW_DIAGRAM.md` · `frontend/docs/SYSTEM_ARCHITECTURE.md` ·
`frontend/docs/STATE_MACHINE.md` (event/effect ใหม่ของ TQ-14) · `backend/docs/WORKFLOW.drawio`
(~73 มีโน้ต "พิมพ์แชทสำรอง") · `docs/PROJECT_CONTEXT.md` · `docs/TECH_DECISIONS.md` (บันทึก
การตัดฟีเจอร์เป็น TD ใหม่ ตาม Solution Design Rule ข้อ 8) · **`docs/CORE_FEATURE_SPEC.md` ห้ามแก้**
(เป็นบันทึกประวัติ) · root `CLAUDE.md` ไม่ต้องแก้ (ไม่ได้พูดถึงแชต)

**CX-9 · test ที่กระทบ**

| ไฟล์ | ทำอะไร |
|---|---|
| `backend/tests/.../ChatMessageServiceTests.cs` | **ลบทั้งไฟล์** |
| `backend/tests/.../CompanyIsolationTests.cs` (~97–99, 139, 152, 184) | ลบ seed + assertion ของ `ChatMessage` · **`EveryEntityIsCompanyScoped` เป็น tripwire ห้ามแก้ให้ผ่าน** — มันจะผ่านเองเมื่อ entity หายไปจริง ถ้ายัง fail แปลว่ารื้อไม่ครบ |
| `backend/tests/.../AdminServiceTests.cs` (~20, 30, 78) | ลบ fake repo + seed |
| `backend/tests/.../Fakes/ServiceTestFakes.cs` (~353–365, ~418, ~432–434) | ลบ `FakeChatMessageRepository` + `ChatMessageCount`/`NotifyChatMessageAsync` ใน fake notifier |
| `backend/tests/.../SessionQuestionServiceTests.cs` (~29) | comment อ้าง `ChatMessageServiceTests` ที่กำลังถูกลบ — แก้ให้ตรง |

---

## API & SignalR Contract Delta

> ทุกแถวคือจุดที่ **backend และ frontend ต้องแก้คู่กัน** (wire contract เป็น camelCase,
> TS type ต้องอัปเดตพร้อม ViewModel เสมอ ตาม Architecture Rule 7)
> **ตาราง proposal วันที่ 2026-08-18 ด้านล่างถูกแทนที่ในส่วน public learner และ naming โดย
> CA-1/CA-2** — ใช้ `/api/training-links`, `/api/learning-sessions/{token}/...`, wire
> `recipientName`/`sessionId` และ learner SignalR methods ตาม amendment; CS by-id ใช้ authenticated flow

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

### 🆕 Delta รอบ 2026-08-23 (F10 / F10-a) — มี authority เหนือสองตารางด้านบนเมื่อขัดกัน

**REST**

| เดิม | ใหม่ | หมายเหตุ |
|---|---|---|
| – | **`POST /api/text-question`** | TQ-2 · `[AllowAnonymous]` · JSON `{ token, learnerKey, text, currentSlideObjectId? }` → `VoiceAnswerViewModel` (ที่ไม่มีฟิลด์ `readiness` แล้ว) |
| `POST /api/voice-question` | **⚠️ เปลี่ยนแล้วตามมติ U1 (2026-08-23)** | ยังบังคับ `audio` + `token` + `learnerKey` เหมือนเดิม **แต่**: (1) **request ตัด field `expecting` ทิ้ง** (ส่งมาก็ถูกเมิน) (2) **response `VoiceAnswerViewModel` ตัดฟิลด์ `readiness` ทิ้ง** — เป็น **breaking wire change ของ endpoint ที่ผ่าน QA ไปแล้ว** ต้อง deploy frontend/backend พร้อมกัน (TQ-22) |
| `GET /api/chat-messages?token=&learnerKey=` | **ลบทิ้ง** | F10-a · CX-4 #9 |
| `GET /api/chat-messages/by-learning-session/{id}` | **ลบทิ้ง** | F10-a · CX-4 #9 |
| `PATCH /api/session-questions/{id}/review` | **ไม่เปลี่ยน สัญญาเดิม** | ✅U2: response `SessionQuestionViewModel` เพิ่มฟิลด์ `source` (additive อ่านอย่างเดียว) |

**SignalR (`/hubs/session`)**

| เดิม | ใหม่ |
|---|---|
| `JoinSession(token, learnerKey)` | **ลบทิ้ง** (CX-3 — ไม่มี client ฝั่งผู้เรียนเหลือแล้ว) |
| `SendChatMessage(token, learnerKey, text)` | **ลบทิ้ง** |
| `SendChatMessageAsAgent(learningSessionId, text)` | **ลบทิ้ง** |
| `JoinSessionAsAgent(learningSessionId)` | **คงไว้ ไม่เปลี่ยน** — เป็นทางเดียวที่ CS เข้า group เพื่อรับคำถามสด |
| event `ReceiveChatMessage` | **ลบทิ้ง** ทั้งฝั่งส่งและฝั่งรับ |
| event `ReceiveNewQuestion` | **คงไว้ ไม่เปลี่ยน payload** — คำถามที่พิมพ์ใช้ event เดียวกัน (TQ-6) |

**TypeScript types (`frontend/src/types/domain.ts`)**

```ts
// ลบ: ChatMessage · ChatSenderRole            (F10-a)
// ลบ: readiness?: "ready" | "not_ready" ใน VoiceAnswer (domain.ts:265-266)   (U1 / TQ-26)
// ลบ: expecting?: "question" | "readiness" ใน input ของ askVoiceQuestion      (U1 / TQ-26)
// เพิ่ม (✅ U2 ยืนยัน 2026-08-23):
export type QuestionSource = "voice" | "text";
export type SessionQuestion = { /* ...เดิม... */ source: QuestionSource; };
// LearnerQuestion ไม่มี source (RR-5 - ViewModel ฝั่งผู้เรียนบางที่สุดเท่าที่จำเป็น)
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

> **ลำดับ proposal เดิมด้านล่างเก็บเป็น design history เท่านั้น** Migration contract ปัจจุบันคือ
> migrations 2 ใบใน CA-5 ห้ามสร้าง `SplitLessonLinkAndLearningSession` เพิ่มและห้ามแก้ migrations เดิม
>
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

### 🆕 MG-R1 · migration ใบใหม่รอบ 2026-08-23 — `RemoveChatMessageAndAddQuestionSource`

> **นี่คือ migration ใบที่ 3 ของโมดูลนี้** ต่อจาก `20260813140603_SplitLinkAndAddAuth` และ
> `20260818155126_AddTotalSlideCount` ที่ CA-5 ยอมรับเป็น contract แล้ว · **ห้ามแก้สองใบเดิม**
> (กฎ root `CLAUDE.md` ข้อ 6) และ **ห้ามยุบงานรอบนี้เข้าไปในสองใบนั้น**

**ใบเดียว ไม่แยกสองใบ** — เพราะทั้งสองการเปลี่ยนแปลงถูก deploy พร้อมโค้ดชุดเดียวกัน (F9/F10/F10-a
เป็นชุดงานเดียวตาม T7) และการแยกจะสร้างสถานะกลางที่ไม่มีใครต้องการ

```
dotnet ef migrations add RemoveChatMessageAndAddQuestionSource \
  --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

**ลำดับใน `Up()` (เป็นสัญญา):**

1. ✅ **U2 ยืนยันแล้ว (2026-08-23) — ทำจริง** — `AddColumn<string>("Source", "SessionQuestion", nullable: false, defaultValue: "voice")`
   · **backfill ด้วย `defaultValue` ไม่ใช่ `UPDATE` แยก**: แถวเดิมทุกแถวมาจากเสียงล้วน
   **โดยข้อเท็จจริง** (วันนี้พิมพ์ถามไม่ได้เลย) จึงไม่ใช่การเดา
   · **ต้องมีคอมเมนต์ในไฟล์ migration อธิบายว่าทำไม `"voice"` ถึงถูกต้องย้อนหลัง 100%**
   · พิจารณาถอด default constraint ออกหลัง backfill ตาม pattern ที่ EF generate — ถ้าคง default
   ไว้ โค้ดที่ลืมส่ง `Source` จะเงียบและได้ `"voice"` ผิดๆ **ให้ถอดออก** และให้ entity เป็น
   `required` บังคับที่ compile time แทน
2. **`DropTable("ChatMessage")`** — พร้อมคอมเมนต์เหนือบรรทัดที่อธิบายเจตนาแบบเดียวกับ
   `RemoveLessonConfigPacingOverrides` ของ `company-admin`:
   > ฟีเจอร์แชตคุยกับ CS ระหว่างเรียนถูกตัดออกทั้งฟีเจอร์ตามมติเจ้าของโปรเจกต์ (T4-a,
   > `requirement.md` 2026-08-22) · **ข้อความเดิมทั้งหมดถูกทิ้งโดยตั้งใจ ห้าม migrate ไปตารางอื่น
   > ห้าม archive** · คำสั่งเดิมของ F7 ที่ให้ย้าย `ChatMessage` ไปผูกกับ `LearningSession` เป็นโมฆะ

**`Down()`:**

- สร้างตาราง `ChatMessage` คืน **เฉพาะรูปร่าง** (คอลัมน์ + index + FK-less pattern เดิม)
  **ไม่มีการกู้ข้อมูล** พร้อมคอมเมนต์บอกตรงๆ ว่า *"กู้ได้แค่โครงสร้าง ข้อความที่เคยมีหายถาวร"*
- ✅U2: `DropColumn("Source", "SessionQuestion")`

**⚠️ ข้อบังคับเรื่องลำดับ deploy (เหมือน R16/R17 ของ `company-admin`):**
migration ใบนี้ **ต้อง deploy พร้อมโค้ดที่เลิกอ่าน/เขียนตาราง `ChatMessage` แล้วเสมอ** ·
ถ้า migration ไปก่อนโค้ด → ทุก request ที่แตะ `IChatMessageRepository` จะ 500 ·
ถ้าโค้ดไปก่อน migration → ตารางค้างอยู่เฉยๆ (ไม่พัง แต่ยังไม่จบงาน)

**สถานะการ apply:** ⚠️ migration สองใบเดิมของโมดูลนี้ **ยังรอ apply กับ deployment database**
ตาม `LS-QA-01` (ยังไม่มี deployment database จริงในโปรเจกต์) · ใบที่ 3 นี้จึงเข้าคิวเดียวกัน
— `devops` ต้อง rehearsal ทั้งสามใบเรียงกันบน staging ก่อน production ไม่ใช่ทีละใบ

**🆕 มติ U1 (ถอด readiness-by-voice) ไม่มี schema change และ *ห้าม* มี migration ใบที่ 4** —
ของที่ถูกถอดทั้งหมด (`Expecting`/`Readiness`/prompt/event/script) เป็น DTO, ViewModel, provider
และ state machine **ไม่มีอะไรลงตาราง** · `SessionQuestion` ที่เคยเกิดจาก readiness check
**ไม่มีอยู่จริงในฐานข้อมูล** อยู่แล้ว เพราะ `IVoiceQuestionService` early-return ก่อนเขียนแถวเสมอ
(`if (result.Readiness is not null)` บรรทัด ~90-95) → **ไม่มีข้อมูลเก่าต้องล้าง ไม่มี backfill**
· MG-R1 ยังคงเป็นใบเดียวของรอบนี้ตามเดิม

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

> **Module A–F ข้างบนคือรอบแรกของโมดูล (implement + QA FULL-3 ผ่านครบ 53/53 แล้ว)** ·
> สาม module ข้างล่างคือรอบ 2026-08-23 (F9/F10/F10-a) — **เป็น Module ใหม่ในโมดูลโฟลเดอร์เดิม
> ไม่ใช่ module folder ใหม่** เพราะเป็นพฤติกรรมของห้องเรียนเดียวกัน ผู้ใช้กลุ่มเดียวกัน
> ใช้ `SessionQuestion`/`LearningSession` ชุดเดียวกัน และจะไม่มีวันถูก ship หรือยกเลิกแยกจากกัน
> (เกณฑ์ `.claude/shared/conventions.md` §1)

### Module G — Typed questions: backend + provider 🔒 (2026-08-23)
`POST /api/text-question` + `TextQuestionController` · `AskTextQuestionDto` ·
`IVoiceQuestionService.AskTextAsync` (แชร์ core กับ `AskAsync`) ·
`IVoiceQuestionProvider.AnswerTextAsync` + implementation ทั้ง 2 ตัว ·
`DtoLimits.QuestionTextMaxLength` · `Program.cs` sensitive path ·
✅U2: `SessionQuestion.Source` + `QuestionSource` + DTO/ViewModel + migration ส่วนที่ 1 ·
**🆕 ✅U1 (2026-08-23): งาน *ถอด* readiness-by-voice ฝั่ง backend ทั้งชุด — TQ-22 + TQ-23**
(`AskVoiceQuestionDto.Expecting` · `VoiceQuestionController` request+map · `VoiceQuestionInput.Expecting` ·
`VoiceQuestionResult.Readiness` · `VoiceAnswerViewModel.Readiness` · `GeminiRest.GeminiAnswerJson.Readiness` ·
early-return ใน `IVoiceQuestionService` · `BuildReadinessPrompt` + branch `isReadiness` ทั้งสอง provider ·
test ของ readiness ใน `VoiceQuestionServiceTests.cs`)
**สัญญาที่ต้องอ่าน:** `## Text Question Rules (F10)` ข้อ TQ-1..TQ-12 + TQ-21 + **TQ-22/TQ-23/TQ-27**
**Dependencies:** A, C, D (มีครบแล้วทั้งสาม — ไม่บล็อกอะไร · U1–U4 เคาะครบแล้ว เริ่มได้ทันที)
**⚠️ Regression surface (เพิ่ม 2026-08-23):** งาน U1 ในโมดูลนี้ **แก้ `POST /api/voice-question`
ซึ่งเป็นของ Module C/D ที่ `qa-engineer` ปิด FULL-3 ไปแล้ว** — request ตัด field `expecting`
และ response ตัดฟิลด์ `readiness` · **รอบที่ verify Module G ต้อง re-verify เส้นทางถามด้วยเสียง
ทั้งเส้น** (อัดเสียงถามจริง → ได้คำตอบ → มีแถวใน `SessionQuestion` → เด้งเข้าหน้ารีวิว CS)
ไม่ใช่ตรวจเฉพาะ endpoint ใหม่ (ดู R19)
**Sensitive — ทำไมต้องติด gate:** เป็น **endpoint anonymous ตัวใหม่ตัวแรกนับตั้งแต่ `security`
ยังไม่เคย audit โมดูลนี้เลย** (LS-QA-08) · รับ untrusted text จากคนที่ไม่มี account แล้วส่งเข้า
prompt ของ LLM (prompt injection) · เป็นทางเข้าที่ถูกที่สุดในการยิงถล่ม LLM quota เพราะไม่ต้อง
อัดเสียงและไม่มี rate limiting (R14/R15) · แตะ `KnowledgeNamespaces` ซึ่งเป็นการกั้นข้อมูล
ข้ามบริษัทเพียงชั้นเดียวของ vector store (KS-1)

### Module H — Chat feature removal (ทั้ง stack + migration) 🔒 (2026-08-23)
26 จุดตาม CX-4/CX-5 · migration MG-R1 ส่วน `DropTable` · เอกสาร 13 ไฟล์ตาม CX-8 ·
test 5 ไฟล์ตาม CX-9
**สัญญาที่ต้องอ่าน:** `## Chat Removal Rules (F10-a)` ทั้งหัวข้อ **ห้ามอ่านแค่ตาราง**
(CX-2/CX-3 คือสองข้อที่ทำให้รื้อผิดแล้วไม่มี error ให้เห็น)
**Dependencies:** ไม่มี — U3 เคาะแล้ว (✅ 2026-08-23 `DropTable` พร้อมข้อมูล) เริ่มได้ทันที ·
**แต่ต้องเสร็จก่อน Module I**
เพราะ I เขียน `room/[token]/page.tsx` และ drawer ทับที่เดิม
**Sensitive — ทำไมต้องติด gate:** (1) **ลบตารางจริงพร้อมข้อมูล** — breaking + data loss ที่ตั้งใจ
(2) แตะ `SessionHub` ซึ่งเป็นจุดกั้นข้อมูลข้ามผู้เรียนอันดับหนึ่งของโมดูล (IC-5/R1) — การรื้อผิด
ที่ group key หรือ `JoinSessionAsAgent` ทำให้บทสนทนาของคนหนึ่งไหลไปหาอีกคนโดยไม่มี error
(3) แตะ `IsSensitiveLearnerPath` ใน `Program.cs` ซึ่งเป็นที่บังคับ CA-3 (no-store/no-referrer)
— ลบผิดบรรทัดเดียวก็ถอด header ป้องกันของ endpoint อื่นออกไปด้วย

### Module I — Learner responsive & single-input room UI 🔒 (2026-08-23)
RS-1..RS-14 ทั้งชุด (**รวม `/session-ended/[token]` + `/link-expired` ตามมติ ✅U4** — เฉพาะ
`min-h-[100dvh]` + hit target ห้าม redesign) · `room/layout.tsx` + `join/layout.tsx` (ใหม่) ·
`AskAiDrawer` ใหม่ (CX-6) · reducer `SUBMIT_TEXT_QUESTION` + `NOT_READY` (TQ-14/TQ-18) ·
effect runner + `askTextQuestion` ใน api-client · ปุ่มพร้อม/ยังไม่พร้อม · matrix TQ-20 ·
**🆕 ✅U1 (2026-08-23): งาน *ถอด* readiness-by-voice ฝั่ง frontend ทั้งชุด — TQ-24 + TQ-25 + TQ-26**
(ลิสต์ push-to-talk **สองใบ** ที่ต้องลบ `"ready"` ออกทั้งคู่ · ข้อความชวนพูดใต้ปุ่มเริ่ม ·
dead branch ใน `resumeAfterInterruption` · `READINESS_ANSWERED` · `START_FIRST_SLIDE` ·
`readyConfirmScript` · แก้ถ้อยคำ `notReadyScript` · `expecting`/`readiness` ใน api-client/types/hook ·
reducer test)
**สัญญาที่ต้องอ่าน:** `## Responsive Interaction Rules (F9)` ทั้งหัวข้อ + TQ-13..TQ-21 +
**TQ-24/TQ-25/TQ-26/TQ-27** (⛔ TQ-25 คือตารางที่บอกว่าอันไหน **ห้าม** ลบ — อ่านข้ามแล้วลบเกิน
= บทเรียนเริ่มไม่ได้ หรือปุ่ม "ยังไม่พร้อม" ไม่มี afterSpeech ให้กลับ)
**Dependencies:** G (ต้องมี endpoint ให้เรียก) และ H (ต้องรื้อของเดิมออกก่อนเขียนทับ)
**⚠️ Regression surface (เพิ่ม 2026-08-23):** งาน U1 ในโมดูลนี้ **เขียนทับพฤติกรรมของ Module E
ที่ผ่าน QA FULL-3 แล้ว** (หน้าจอ `ready` + push-to-talk + การกลับเข้าบทเรียนหลังถูกขัดจังหวะ) ·
**รอบที่ verify Module I ต้อง re-verify ด้วยมือ**: กดปุ่มพร้อม → บทเรียนเริ่มที่สไลด์ resume ถูกจุด ·
กด "ยังไม่พร้อม" → พูด `notReadyScript` แล้วกลับมา `ready` **โดยไม่มี auto-start** ·
กดปุ่มพูดตอน `ready` → **ไม่เกิดอะไรเลย** · กดพูดถามกลางบทเรียน → ยังทำงานครบเหมือนเดิม (R19)
**Sensitive — ทำไมต้องติด gate:** เขียน `app/room/[token]/page.tsx` ใหม่ ซึ่งเป็น **จุดเดียว
ในระบบที่บังคับ IC-7 ได้** (การกันไม่ให้เปิด `/room` ตรงๆ แล้ว resume ของคนอื่น ผ่าน
`consumeRoomEntry`/`peekLearnerKey`) — server แยกไม่ออกว่าผ่านหน้ายืนยันมาหรือยัง ถ้า guard
ตัวนี้หายไประหว่างจัด layout ใหม่ **คนที่สองบนเครื่องที่ใช้ร่วมกันจะเห็นความคืบหน้าและคำถาม
ของคนแรกโดยไม่มี error ให้เห็น** (เหตุผลเดียวกับที่ Module E ติด gate ตามมติ 2026-08-18)

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

| R12 | **รื้อแชตไม่ครบ เหลือครึ่งทาง** (2026-08-23) | codebase มี type/hook/hub method ที่ไม่มีปลายทาง · แย่กว่านั้นคือ **UI ที่ยังเปิดช่องให้พิมพ์หาคนได้ แต่ไม่มีใครอ่าน** ซึ่งขัด T4-a โดยตรงและผู้เรียนจะนั่งรอคำตอบที่ไม่มีวันมา | Module H ต้องจบเป็นก้อนเดียว ห้ามแบ่งครึ่ง (กฎเดียวกับ R3) · เกณฑ์ปิดงานคือ **grep ตามหัว `## Chat Removal Rules` ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง** ไม่ใช่ "build ผ่าน" |
| R13 | **ลบ `use-agent-session-chat.ts` ทั้งไฟล์เพราะชื่อมีคำว่า chat** (2026-08-23) | หน้ารีวิวของ CS (Module F) เลิกได้คำถามสดแบบ realtime — **ฟีเจอร์ที่ไม่มีใครสั่งให้ลบ หายไปเงียบๆ โดย build ยังผ่าน** เพราะไม่มี type error | CX-2 เขียนเป็นข้อห้ามชัด: **เขียนใหม่เป็น `use-agent-session-questions.ts` ห้ามลบ** · QA ต้องเปิดหน้า `admin/learning-sessions/[id]` แล้วยืนยันว่าคำถามใหม่ยังเด้งขึ้นมาเองจริง ไม่ใช่แค่ดูว่าไฟล์หายไปแล้ว |
| R14 | **พิมพ์ถามเปิดทางให้ prompt injection แม่นยำขึ้นมาก** (2026-08-23) | ผู้เรียน (ไม่ต้องมี account) พิมพ์ข้อความที่พยายามสั่งโมเดลให้เลิกยึด grounding · ผลที่แย่ที่สุดคือ AI ตอบเรื่องนอกบทเรียนโดยอ้างเป็นข้อมูลของ School Bright — ขัดหลัก grounded tutor ที่เป็นหัวใจของทั้งระบบ | **ไม่ใช่ความเสี่ยงใหม่ในเชิงชนิด** (transcript จากเสียงก็ inject ได้) แต่แม่นยำขึ้นมาก → TQ-9 บังคับให้วางข้อความคำถามในบล็อกที่ระบุชัด **ห้ามปนกับบรรทัดกติกา** · จำกัดความยาวที่ 2000 (TQ-3) · **เป็นข้อที่ `security` ต้องตรวจตอน gate ของ Module G โดยเฉพาะ** |
| R15 | **ไม่มี rate limiting บน `/api/text-question`** (2026-08-23) | ยิงถล่มได้ถูกกว่าเสียงมาก (ไม่ต้องอัดเสียง ไม่ต้องรอ 300ms ไม่ต้องมีไมค์) → เผา LLM quota / Pinecone quota ของทั้งบริษัทได้จากลิงก์เดียวที่หลุดในกลุ่มไลน์ | **นอกขอบเขตรอบนี้แต่ห้ามลืม**: `/api/voice-question` วันนี้ก็ไม่มีเหมือนกัน (TD-002 ทำเพียงบางส่วน — มี rate limiter แค่ที่ login) · โปรเจกต์**มี** `AddLoginRateLimiting()` + `app.UseRateLimiter()` อยู่แล้ว ดังนั้นต้นทุนการเพิ่ม policy ใหม่ต่ำ · **บันทึกเป็น open question ให้เจ้าของโปรเจกต์ตัดสิน ไม่ใช่ให้ engineer ตัดสินใจเอง** |
| R16 | **`transcription_failed` ถูกเขียนลงคิวรีวิวสำหรับคำถามที่พิมพ์** (2026-08-23) | CS เห็นแถวที่บอกว่า "ถอดเสียงไม่สำเร็จ" ของคำถามที่ไม่เคยมีเสียง — ข้อมูลที่โกหกตัวเองในคิวที่ใช้ตัดสินใจแก้คลังความรู้ | TQ-10 บังคับให้เส้นทางพิมพ์ **โยน error แทนการบันทึกแถว** · test ใน TQ-21 ต้องยืนยันว่า **ไม่มีแถวถูกเขียน** ไม่ใช่แค่ว่ามี error |
| R17 | **จัด responsive แล้วทำ IC-7 guard หลุด** (2026-08-23) | `room/[token]/page.tsx` ถูกเขียนใหม่ทั้งไฟล์ — ถ้า `peekLearnerKey`/`getLearnerName`/`consumeRoomEntry` + การ redirect กลับ `/join` หายไประหว่างทาง **คนที่สองบนเครื่องที่ใช้ร่วมกันเข้าไปเรียนต่อของคนแรกได้เงียบๆ** ซึ่งเป็นช่องที่มติ D2 เปิดขึ้นมาปิดโดยเฉพาะ | Module I ติด 🔒 · **`qa-engineer` ต้องรัน IC-7 manual test ซ้ำทั้งชุดในรอบที่ verify Module I** ไม่ใช่ถือว่าผ่านแล้วจาก FULL-3 · `entryGrantedRef` + `consumeRoomEntry` ต้องยังทำงานเหมือนเดิมทุกกรณีรวม React Strict Mode |
| R18 | **F9 ไม่มีอะไรที่ automated test จับได้เลย** (2026-08-23) | `typecheck`/`lint`/`build` ผ่านทั้งที่ปุ่มพูดยังเลื่อนหน้า ปุ่มจบยังหลุดขอบจอ — และไม่มีใครรู้จนกว่าจะมีคนถือมือถือจริง | RS-14 กำหนดเกณฑ์ยอมรับ 7 ข้อไว้แล้ว · **ถ้า QA ไม่ได้ทดสอบบนอุปกรณ์/emulator จริง ต้องลงทั้ง 7 ข้อใน `## Unverified Behaviour — undeployed phases`** และ `devops` ต้องเอารายการนั้นให้เจ้าของโปรเจกต์ดูก่อน deploy |
| R19 | **มติ U1 ทำให้ขอบเขตรอบนี้โตขึ้นและพาดเข้า regression surface ของ Module C/D/E ที่ปิดไปแล้ว** (2026-08-23) | **ขนาดงานจริงใหญ่กว่าที่ทุกคนคิดตอนสัมภาษณ์ F9/F10** — ตอนนั้นเข้าใจว่า F10 คือ "เพิ่มช่องพิมพ์" แต่หลัง U1 มันรวม *การถอด* readiness-by-voice ออกจาก 20+ จุดใน 15 ไฟล์ ซึ่งเป็นโค้ดที่ `qa-engineer` ปิด FULL-3 ไปแล้ว · ผลที่แย่ที่สุดคือ **รื้อครึ่งเดียวแล้วคอมไพล์ผ่าน** (`expecting`/`readiness` เป็น `string`/optional เกือบทุกชั้น) → ปุ่มพูดที่ยังกดได้ตอน `ready` แต่ไม่มีอะไรเกิดขึ้น หรือ AI พูดบอกให้ "กดปุ่มพูดแล้วบอกว่าพร้อม" ทั้งที่ทำไม่ได้แล้ว | 1) TQ-22..TQ-27 เป็นรายการปิดงาน **ต้องทำครบเป็นก้อนเดียว ห้ามแบ่งครึ่ง** (กฎเดียวกับ R3/R12) · 2) เกณฑ์ปิดงานคือ **`grep -ri "readiness\|expecting" backend/src frontend/src` ไม่เหลือโค้ดจริง** ไม่ใช่ "build ผ่าน" · 3) **`project-manager` ต้องวางงาน U1 ไว้ในเฟสเดียวกับ Module G (ฝั่ง backend) และ Module I (ฝั่ง frontend) ห้ามแยกคนละเฟส** เพราะ wire contract เปลี่ยนสองฝั่งพร้อมกัน · 4) **`qa-engineer` ต้องถือรอบที่ verify G และ I เป็น FULL และ re-verify Module C/D/E ตามรายการในช่อง Regression surface ของทั้งสอง module** ไม่ใช่เชื่อผล FULL-3 เดิม |
| R20 | **ลบของที่ยัง "มีคนใช้" ในชุด readiness** (2026-08-23) | ของ 7 ชิ้นในตาราง TQ-25 หน้าตาเหมือนเป็นชุดเดียวกันหมด แต่ **3 ชิ้นต้องอยู่ต่อ** — ลบ `startFirstSlide()` = บทเรียนเริ่มไม่ได้เลย · ลบ `AWAIT_READINESS` = ปุ่ม "ยังไม่พร้อม" พูดจบแล้วค้าง ไม่กลับสู่ `ready` · ลบ `notReadyScript` = `NOT_READY` ไม่มีบทให้พูด · ทั้งสามอย่างนี้ **TypeScript จับให้บางส่วนเท่านั้น** (ลบ union member แล้ว case ที่เหลือยัง compile ได้) | TQ-25 เขียนเป็นตาราง "ลบ/ห้ามลบ" พร้อมเหตุผลรายบรรทัดแล้ว — **engineer ต้องอ่านตารางนั้นให้จบก่อนลบบรรทัดแรก** · reducer test ที่ TQ-21 กำหนด (`NOT_READY` จาก `"ready"` → ไม่มี `WAIT_READY_TIMEOUT`, `PUSH_TO_TALK_START` จาก `"ready"` → ไม่เกิดอะไร) คือ tripwire ของข้อนี้ |

**Dependencies ระหว่าง module:** A → (B, C) → D → (E, F) · A ต้องเสร็จก่อนทุกอย่าง ·
D ต้องเสร็จก่อน E และ F เพราะทั้งคู่ต้องใช้ chat/คำถามที่ผูกกับการเรียนแล้ว ·
**รอบ 2026-08-23:** (G, H) ขนานกันได้ → **I ต้องรอทั้งคู่** · G/H ไม่พึ่งกันเอง แต่ **ทั้งคู่ต้อง
เสร็จก่อน I** เพราะ I เรียก endpoint ของ G และเขียนทับไฟล์ที่ H รื้อ · **ห้ามส่งมอบ G หรือ H
ครึ่งทางแล้วเริ่ม I** — ระหว่างนั้นห้องเรียนจะอยู่ในสถานะที่แชตหายแล้วแต่ยังพิมพ์ถาม AI ไม่ได้
= ผู้เรียนไม่มีทางถามด้วยการพิมพ์เลย ซึ่งแย่กว่าสถานะก่อนเริ่มงาน

**สิ่งที่ `project-manager` ต้องรับไปทำต่อ:** phase ที่ครอบ Module C, D, **E**, F ต้องมี
`🔒 Security gate` ที่หัวข้อ phase ตามเหตุผลที่ระบุในแต่ละ module (ไม่ใช่ด้วยเหตุผล PII
ซึ่งถูกตัดออกโดยตั้งใจตาม F2) · **E เพิ่มเข้ามาตามมติเจ้าของโปรเจกต์ 2026-08-18** ·
**เพิ่ม 2026-08-23: phase ที่ครอบ Module G, H, I ต้องมี `🔒 Security gate` ทั้งสาม**
ด้วยเหตุผลที่เขียนไว้ในช่อง Sensitive ของแต่ละ module (anonymous endpoint ใหม่ + prompt injection /
DropTable + `SessionHub` + `IsSensitiveLearnerPath` / IC-7 guard ที่บังคับได้ที่เดียว) ·
✅ **U1–U4 เคาะครบแล้วเมื่อ 2026-08-23 — วาง phase ได้เต็มรูปแบบ ไม่มีอะไรบล็อกอีก** ·
**สิ่งที่ต้องระวังตอนวาง phase (มาจากมติ U1 โดยตรง):** งานถอด readiness-by-voice
**คร่อมสองฝั่งของ wire contract** — ฝั่ง backend อยู่ที่ Module G (TQ-22/TQ-23) ฝั่ง frontend
อยู่ที่ Module I (TQ-24..TQ-26) · **ห้ามวางสองก้อนนี้คนละเฟสที่ deploy แยกกัน** เพราะช่วงกลาง
คือ frontend ที่ยังส่ง `expecting` และรอ `readiness` จาก backend ที่เลิกผลิตแล้ว (R19)

---

## Unresolved Open Questions

> ✅ **U1–U4 ที่เปิดไว้เมื่อ 2026-08-23 ถูกเคาะครบในวันเดียวกันแล้ว — ไม่มีคำถามค้างในเอกสารนี้**
> ทั้งรอบแรก (Module A–F) และรอบ F9/F10/F10-a

### ✅ มติ U1–U4 (ยืนยันโดยเจ้าของโปรเจกต์ในแชท 2026-08-23)

> เดิม 4 ข้อนี้เป็น "ข้อเสนอพร้อม trade-off" เพราะ subagent เรียก `AskUserQuestion` ไม่ได้
> ในสภาพแวดล้อมนี้ · เจ้าของโปรเจกต์ตอบครบทั้ง 4 ข้อแล้ว **จึงเป็นมติที่มีผลบังคับ ไม่ใช่ข้อเสนอ** ·
> ✅ `project-manager` วาง phase ได้ · **การรื้อมติเหล่านี้ต้อง amend เอกสารนี้ก่อนเสมอ**

| # | คำถาม | ✅ มติ | ตรงกับข้อเสนอ? | สิ่งที่ถูกตัดออกด้วยมตินี้ | อยู่ในเอกสารที่ไหน |
|---|---|---|---|---|---|
| **U1** | readiness ยังตอบด้วย **เสียง** ได้ไหม | **ไม่ได้ — ตัดทิ้งด้วย เหลือกดปุ่มทางเดียว** ทั้งพิมพ์และพูดใช้ตอบจุดนี้ไม่ได้เลย | ❌ **ไม่ตรง — เจ้าของโปรเจกต์เลือกทางที่ `system-analyst` เตือนว่างานหนักกว่า** โดยเห็น trade-off ครบแล้วรวมถึงผลต่อ Module C/D/E ที่ผ่าน QA · **ไม่ใช่ความเข้าใจผิด ⛔ ห้ามถามซ้ำ** | การคงเส้นทางเสียงไว้ (ข้อเสนอเดิม) · `expecting: "readiness"` ทุกชั้น · `READINESS_ANSWERED` · `BuildReadinessPrompt` × 2 · `VoiceAnswerViewModel.Readiness` · `readyConfirmScript` · `"ready"` ใน push-to-talk list ทั้งสองใบ | **TQ-22..TQ-27** (รายการรื้อเต็ม) · TQ-11 · TQ-18 · TQ-20 · TQ-21 · Module G/I · R19/R20 · API delta |
| **U2** | เพิ่ม `SessionQuestion.Source` (`voice`/`text`) ไหม | **เพิ่ม** — `NOT NULL`, backfill `"voice"` | ✅ ตรง | การปล่อยให้ CS แยกไม่ออกว่าคำถามมาทางไหน · การเพิ่มคอลัมน์ทีหลังแล้ว backfill `"unknown"` ตลอดกาล | DM-3a · DM-6a · DM-7a · TQ-5 · TQ-21 · MG-R1 ข้อ 1 · API delta |
| **U3** | ตาราง `ChatMessage` + ข้อมูลเดิม | **`DropTable` ทิ้งทั้งใบพร้อมข้อมูล** — breaking + data loss ที่ตั้งใจ | ✅ ตรง | การเก็บตารางไว้อ่านย้อนหลัง · การ archive/ย้ายข้อความ · การคง entity+query filter+repository ไว้โดยไม่มีใครใช้ | DM-4 · DM-7a · DM-8 · MG-R1 ข้อ 2 · Module H |
| **U4** | F9 รวม `/session-ended/[token]` + `/link-expired` ไหม | **รวม แบบ "ตรวจตามกฎเดียวกัน ไม่ redesign"** | ✅ ตรง | การยึดตัวอักษร R2 แล้วเลื่อนไปรอบหน้า (ซึ่งจะเสียรอบ CR เต็มอีกรอบเพื่อแก้ 2 คลาส) · **การ redesign สองหน้านี้** — ทำได้แค่ `min-h-[100dvh]` + hit target ≥ 44px | RS-1 · RS-4 · RS-13 · Module I |

**สิ่งที่มติ U1 *ไม่* ครอบ (อ่านให้ชัดก่อนลงมือ):** การถามด้วย **เสียง** กลางบทเรียน
**ยังอยู่ครบทุกประการ** — T1 บอกว่าพิมพ์ต้องเทียบเท่าเสียง ไม่ได้บอกให้เลิกรับเสียง ·
สิ่งที่ถูกตัดคือ **การใช้เสียงตอบคำถาม "พร้อมหรือยังคะ?"** เท่านั้น (`expecting = "readiness"`) ·
⛔ ห้ามถือโอกาสรื้อ `PushToTalkButton`, `MinVoiceDurationMs`, `no_speech` หรือ pipeline ถอดเสียง

**คำถามข้างเคียงที่ *ไม่* บล็อกรอบนี้ แต่ต้องรู้ว่ามีอยู่:** rate limiting ของ
`/api/text-question` และ `/api/voice-question` (R15) — **ข้อเสนอ: ไม่ทำในรอบนี้**
(อยู่ในกลุ่ม TD-002 ที่ `requirement.md` ประกาศว่านอกขอบเขตโมดูล) แต่ `security` จะยกขึ้นมา
แน่นอนตอน audit Module G · เจ้าของโปรเจกต์ตัดสินตอนนั้นได้ ไม่ต้องตัดสินตอนนี้

### สถานะรอบแรก (Module A–F) — ยังจริงทุกข้อ

> **ไม่มีคำถามค้างแล้วสำหรับรอบแรก**
> 6 ข้อที่เคยอยู่ในหัวข้อนี้ (Q2/Q3/Q4 + D1–D3) เคาะครบเมื่อ 2026-08-18 และ Q2/D1
> ถูก amend เมื่อ 2026-08-19 ตาม CA-1/CA-2 · เก็บตารางไว้เป็นบันทึกมติ ไม่ใช่รายการรอคำตอบ ·
> ส่วน "ที่ตัดออกจากเฟสนี้โดยตั้งใจ" ข้างล่าง **ยังมีผลบังคับเต็มที่**

### มติที่ปิดแล้ว (ยืนยัน 2026-08-18; amend 2026-08-19 โดยเจ้าของโปรเจกต์)

| # | คำถามเดิม | ✅ มติ | อยู่ในเอกสารที่ไหน |
|---|---|---|---|
| **Q2** | rename `TrainingSession` ไหม | **amend → `TrainingLink`** | CA-1 · CA-5; DM-1/DM-7/DM-8 เดิมเป็น design history |
| **Q3** | ชื่อตารางใหม่ | **`LearningSession`** | DM-2 · DM-6 · DM-7 |
| **Q4** | `SessionSummary` | **ลบทิ้งทั้งใบ 13 จุด** | DM-5 · Migration Plan ข้อ 9 · API delta (แทนด้วย 2 endpoint ใหม่) |
| **D1** | route/TS type ตามชื่อใหม่ด้วยไหม | **ตามด้วยชื่อที่ amend** (`/api/training-links`, `/api/learning-sessions`) | CA-1 · CA-2 |
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

**เพิ่มเมื่อ 2026-08-23 (รอบ F9/F10/F10-a) — ตัดออกโดยตั้งใจเช่นกัน อย่า implement โดยไม่ amend ก่อน**

- **ช่องทางคุยกับคนจริงระหว่างเรียนทุกรูปแบบ** — T4-a ตัดทิ้งแล้วและ `requirement.md` §Constraints
  **สั่งห้าม `system-analyst`/`qa-engineer` ยกขึ้นมาถามซ้ำ** · ห้ามเสนอ "ปุ่มขอความช่วยเหลือ"
  "ส่งอีเมลหา CS" หรือ fallback ใดๆ แทน · เส้นทางที่เจ้าของโปรเจกต์เลือกคือไลน์/โทรนอกระบบ
  และคิวรีวิว F7 ย้อนหลัง
- **การ rename `IVoiceQuestionService`/`IVoiceQuestionProvider`/project `Providers.VoiceQuestion`/
  env `VOICE_QUESTION_PROVIDER`/`MAX_VOICE_UPLOAD_MB`** ให้สื่อว่าครอบทั้งเสียงและข้อความ —
  **ยอมรับเป็นหนี้ชื่อโดยตั้งใจ** แบบเดียวกับที่ CA-1 ยอมรับ `SessionId`/`RecipientName` ·
  การ rename ลากไปถึงชื่อ project, env, `.env.example`, root `CLAUDE.md` และเอกสารอีกหลายไฟล์
  โดยไม่เปลี่ยนพฤติกรรมแม้แต่นิดเดียว · **เขียนไว้ในสัญญาแทน**: คำว่า "voice question" ในโค้ด
  หลังรอบนี้หมายถึง **"คำถามของผู้เรียน ไม่ว่าจะเข้ามาทางเสียงหรือข้อความ"** — engineer ที่เจอ
  ชื่อนี้แล้วสงสัย ให้อ่านบรรทัดนี้ ไม่ใช่ rename เอง
- **rate limiting ของ endpoint ฝั่งผู้เรียน** — R15 · อยู่ในกลุ่ม TD-002 ที่ `requirement.md`
  ประกาศว่านอกขอบเขตโมดูลนี้ · **ห้าม engineer เพิ่ม policy เองระหว่างทำ Module G**
  (การเพิ่ม throttle ที่ผิดจังหวะทำให้ห้องเรียนสาธิตจริงถูกปฏิเสธกลางคัน) — รอ `security` ยกขึ้น
  แล้วเจ้าของโปรเจกต์ตัดสิน
- **responsive ของ `/admin/*`** — R2 ตัดออกชัดเจน · แม้จะเห็นว่าหลังบ้านพังบนมือถือระหว่างทำงานนี้
  ก็ห้ามแก้ ให้บันทึกส่งกลับ `business-analyst`
- **"รายการสไลด์" ในห้องเรียน** — RS-10 · ของชิ้นนี้ไม่มีอยู่จริงวันนี้ และ event log ต่อสไลด์
  ถูก Declined ไว้ตั้งแต่ 2026-08-11 · ห้ามสร้างขึ้นใหม่เพื่อ "ทำ R3c ให้ครบ"
- **live update ฝั่งผู้เรียนผ่าน SignalR** — CX-3 ถอด `JoinSession` ออกเพราะไม่มีใครใช้แล้ว ·
  ถ้าวันหน้าต้องการ (เช่น CS แทรกข้อความระบบเข้าห้อง) **ต้องเปิดใหม่ผ่าน design amendment**
  ไม่ใช่เก็บ dead code ไว้เผื่อ
- **โหมดเงียบ / ปุ่มปิดเสียงคำตอบ** — T3 ปฏิเสธชัด "TTS อ่านทุกครั้ง ไม่มีโหมดเงียบ" ·
  `VolumeControl` ที่ปรับ *ระดับ* เสียงเป็นคนละเรื่องและยังอยู่เหมือนเดิม
- **การตอบ readiness ด้วยเสียงทุกรูปแบบ** *(เพิ่มตามมติ U1 · 2026-08-23)* — ถูกถอดออกจากระบบ
  ทั้งชุดตาม TQ-22..TQ-27 · **ห้ามคงไว้แบบ `[Obsolete]` ห้ามรับค่าแล้วเมิน ห้ามซ่อน flag ไว้เผื่อ
  เปิดกลับ** · ถ้าวันหน้าอยากได้คืน ต้องเปิดใหม่ผ่าน design amendment เหมือนกรณี `JoinSession`
  ของ CX-3 ไม่ใช่เก็บ dead code ไว้
- **การเพิ่มเสียงตอบรับให้ปุ่ม "พร้อมแล้ว เริ่มเรียนเลย" เพื่อชดเชย `readyConfirmScript` ที่ถูกลบ**
  *(เพิ่ม 2026-08-23)* — ปุ่มนี้วันนี้ไม่พูดตอบอยู่แล้ว (`START` → `startFirstSlide()` ตรงๆ) ·
  การเติมเสียงเข้าไปคือการเปลี่ยนพฤติกรรมที่ไม่มีใครสั่ง (TQ-25)

---

## Change Log

- 2026-08-23 *(วันที่ยืนยันแล้วตาม system context ปัจจุบันของเซสชัน)* —
  **เจ้าของโปรเจกต์เคาะ U1–U4 ครบทั้ง 4 ข้อในแชท → amend เปลี่ยนจาก "ข้อเสนอ" เป็น "มติ"
  และขยาย contract ตามมติ U1** · **ไม่มีโค้ดถูกแตะในรอบนี้ ไม่มี checkbox ถูกติ๊ก
  ไม่แตะ `requirement.md`/`plan.md`** ·
  **มติ:** U1 = **ตัดการตอบ readiness ด้วยเสียงทิ้ง เหลือปุ่มกดทางเดียว** *(ตรงข้ามกับข้อเสนอของ
  `system-analyst` ที่ให้คงเสียงไว้ — เจ้าของโปรเจกต์เลือกทางที่งานหนักกว่าโดยเห็น trade-off
  และผลต่อ Module C/D/E ที่ผ่าน QA แล้วครบถ้วน)* · U2 = **เพิ่ม `SessionQuestion.Source`** (ตรงข้อเสนอ) ·
  U3 = **`DropTable` `ChatMessage` ทิ้งทั้งใบพร้อมข้อมูล** (ตรงข้อเสนอ) · U4 = **F9 รวม
  `/session-ended/[token]` + `/link-expired`** (ตรงข้อเสนอ) ·
  **สิ่งที่แก้ในเอกสารนี้:** (1) banner หัวเอกสารเปลี่ยนจาก ⏳ เป็น ✅ พร้อมคำเตือนว่าขนาดงาน
  ใหญ่กว่าที่ประเมินไว้ (2) ตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" เพิ่ม 4 แถว U1–U4 และเติม
  ผลของ U1 ลงแถว T6 เดิม (3) แถว F10 ใน Feature-by-Feature ระบุว่า F10 ไม่ใช่แค่ "เพิ่มทางเข้าใหม่"
  แต่รวมการ *ถอด* readiness-by-voice (4) ปลดธง ⏳ ที่ DM-3a · DM-4 · DM-6a · DM-7a · MG-R1
  (ทั้ง `Up()` และ `Down()`) · API/SignalR delta · TS types · RS-1 · RS-4 · RS-13 · TQ-5 · TQ-21
  (5) **contract ชุดใหม่ `### การถอด readiness-by-voice` = TQ-22..TQ-27** — รายการรื้อครบทุกจุด
  พร้อมเลขบรรทัดจริงที่ตรวจจากโค้ดในรอบนี้ (backend 7 จุด/2 provider · frontend 2 ลิสต์ push-to-talk
  ที่ซ้ำกัน + event/afterSpeech/script + api-client/types/hook · test 2 ชุด · เอกสาร 11 ไฟล์)
  และ **ตาราง "ลบ/ห้ามลบ" ของ TQ-25** ที่ระบุว่า `startFirstSlide()`, `AWAIT_READINESS`,
  `notReadyScript` ต้องอยู่ต่อ (6) แก้ TQ-2 · TQ-11 · TQ-18 · TQ-20 ให้สะท้อนว่า readiness
  ตอบได้ทางเดียวคือกดปุ่ม และประกาศว่าข้อความเดิมบางบรรทัดเป็นโมฆะ (7) MG-R1 เพิ่มย่อหน้าว่า
  **U1 ไม่มี schema change และห้ามมี migration ใบที่ 4** (readiness ไม่เคยถูกบันทึกลงตารางอยู่แล้ว
  เพราะ early-return ใน `IVoiceQuestionService`) (8) Module G/H/I: ใส่งาน U1 ฝั่ง backend ลง G
  ฝั่ง frontend ลง I พร้อมช่อง **⚠️ Regression surface** ของทั้งสอง (9) `## Risks & Dependencies`
  เพิ่ม **R19** (ขอบเขตโตขึ้น + พาดเข้า Module C/D/E ที่ปิดไปแล้ว + สั่งให้ QA ถือเป็น FULL
  และ re-verify) และ **R20** (ลบเกินในชุด readiness แล้วบทเรียนเริ่มไม่ได้) · เรียงลำดับแถว
  R18 ให้ถูกที่ (10) `## Unresolved Open Questions` เปลี่ยนบล็อก ⏳ เป็น **✅ ตารางมติ U1–U4**
  พร้อมคอลัมน์ "ตรงกับข้อเสนอ?" และเพิ่มข้อห้ามใหม่ 2 ข้อในบล็อก "ตัดออกโดยตั้งใจ" ·
  **ข้อเท็จจริงจากโค้ดที่พบใหม่ในรอบนี้ (ไม่ได้อยู่ในตาราง U1 เดิม จึงเป็นของแถมที่ต้องรู้):**
  **(ก)** ลิสต์ push-to-talk มี **สองใบ** — `tutor-reducer.ts:61` และ
  `room/[token]/page.tsx:112` — ลบใบเดียวได้ปุ่มที่กดแล้วไม่เกิดอะไรและไม่มี error
  **(ข)** `room/[token]/page.tsx:195` มีข้อความชวนให้พูดตอบ และ `notReadyScript`
  (`scripts.ts:12`) บอกให้ "กดปุ่มพูดแล้วบอก" — **สองข้อความนี้จะสั่งให้ผู้เรียนทำสิ่งที่ทำไม่ได้แล้ว
  ถ้าลืมแก้** **(ค)** `resumeAfterInterruption` มี branch `interruptedFrom === "ready"`
  ที่กลายเป็น dead code **(ง)** `AWAIT_READINESS` ต้องอยู่ต่อ (ผู้ผลิตใหม่คือ `NOT_READY`)
  แต่ `START_FIRST_SLIDE` ต้องไป — สองค่านี้อยู่ติดกันใน `types.ts` และหน้าตาเหมือนเป็นชุดเดียวกัน
  **(จ)** คอมเมนต์ `KnowledgeQnAConflict.cs:29` อ้าง readiness check เป็นตัวอย่าง ต้องแก้ถ้อยคำ
  เป็น `no_speech` ✅ **พร้อมส่ง `project-manager` เต็มรูปแบบ**
- 2026-08-23 *(วันที่มาจาก system context ของเซสชัน — **ยังไม่ได้ให้เจ้าของโปรเจกต์ยืนยันเอง**
  เพราะ subagent ไม่มีเครื่องมือถามผู้ใช้ · การสัมภาษณ์ที่เป็นต้นทางของรอบนี้เกิดเมื่อ 2026-08-22
  ถ้าวันที่ผิดให้แก้บรรทัดนี้บรรทัดเดียว)* —
  **amend รับ F9 (responsive ฝั่งผู้เรียน) · F10 (พิมพ์ถามแทนพูด) · F10-a (ตัดฟีเจอร์แชต CS
  ออกทั้งฟีเจอร์) จาก `requirement.md` ที่ปิดสัมภาษณ์ครบ R1–R6 / T1–T7 / T4-a เมื่อ 2026-08-22** ·
  **ไม่มีโค้ดถูกแตะในรอบนี้** ·
  **สิ่งที่เพิ่ม/แก้ในเอกสารนี้:** (1) banner หัวเอกสารเพิ่มสถานะ amendment + ธง ⏳ U1–U4
  (2) `## Feature-by-Feature Feasibility` เพิ่มแถว F9/F10/F10-a พร้อมข้อเท็จจริงจากโค้ดจริง
  (3) ตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" เพิ่ม 11 แถวจากมติ R1–R6/T1–T7/T4-a
  (4) **DM-3a** `SessionQuestion.Source` (⏳U2) · **DM-4** เปลี่ยนจาก "แก้ FK" เป็น **"ลบทั้ง entity"**
  (คำสั่งย้าย `ChatMessage` ของ F7 เดิมเป็นโมฆะ) · **DM-6a** ลบ `ChatSenderRole` + เพิ่ม
  `QuestionSource` · **DM-7a** ลบ `DbSet`/mapping ของ `ChatMessage` · **DM-8** เพิ่มแถว
  `IChatMessageRepository` = ลบ
  (5) contract ชุดใหม่ 3 หัวข้อ: **`## Responsive Interaction Rules (F9)` RS-1..RS-14** ·
  **`## Text Question Rules (F10)` TQ-1..TQ-21** · **`## Chat Removal Rules (F10-a)` CX-1..CX-9**
  (6) `## API & SignalR Contract Delta` เพิ่มหัวข้อย่อย delta 2026-08-23
  (7) `## Migration Plan` เพิ่ม **MG-R1 `RemoveChatMessageAndAddQuestionSource`** —
  **breaking + data loss ที่ตั้งใจ** pattern เดียวกับ `RemoveLessonConfigPacingOverrides`
  (8) `## Modules` เพิ่ม **Module G / H / I ติด 🔒 ทั้งสาม** พร้อมเหตุผลรายตัว
  (9) `## Risks & Dependencies` เพิ่ม **R12–R18**
  (10) `## Unresolved Open Questions` เพิ่มบล็อก ⏳ **U1–U4** + รายการ "ตัดออกโดยตั้งใจ" อีก 7 ข้อ ·
  **ผลการวิเคราะห์ที่สำคัญที่สุด 4 ข้อ (ทั้งหมดยืนยันกับโค้ดจริง ไม่ใช่การอนุมาน):**
  **(ก)** ขอบเขตการรื้อแชตกว้างกว่าที่ `requirement.md` ไล่ไว้เกือบเท่าตัว — มีฝั่ง CS เต็มรูปแบบ
  (`JoinSessionAsAgent`/`SendChatMessageAsAgent`/`use-agent-session-chat.ts`/ปุ่มในหน้า admin)
  ที่เอกสารนั้นระบุเองว่ายังไม่ได้ตรวจ
  **(ข)** `use-agent-session-chat.ts` ทำสองหน้าที่ (chat + คำถามสดของ CS) — **ลบทั้งไฟล์
  = Module F เสียฟีเจอร์เงียบๆ โดย build ยังผ่าน**
  **(ค)** พิมพ์ถาม **ไม่ต้องมี provider/env/dependency ใหม่เลย** — `GeminiRest.CallAsync` รองรับ
  text-only อยู่แล้ว และ RAG provider แยก step ไว้พอดีจนคำถามที่พิมพ์คือ "ข้าม step 1"
  **(ง)** F9 ไม่ใช่งานจัด CSS — **3 จุดที่มีอยู่วันนี้ไม่ทำงานจริงบนมือถือ**: `preventDefault()`
  ใน `onTouchStart` ไร้ผลเพราะ React ผูก passive listener · `h-screen`/`100vh` ทำให้ปุ่มจบ
  หลุดใต้ขอบจอ · ไม่มี `viewport` export ที่ไหนเลยจึงกันแป้นพิมพ์บังไม่ได้ ·
  ⛔ **ยังส่งต่อ `project-manager` ไม่ได้จนกว่า U1–U4 จะถูกเคาะ** (ทั้ง 4 ข้อเปลี่ยนขอบเขตงานจริง:
  U1 เปลี่ยนขนาด Module I · U2 เปลี่ยน schema ของ G · U3 เปลี่ยนว่ามี migration ไหมใน H ·
  U4 เปลี่ยนจำนวนไฟล์ของ I) · ⛔ engineer ห้ามหยิบไปทำ
- 2026-08-19 — **เจ้าของโปรเจกต์ยืนยัน Contract Amendment เพื่อ resolve `LS-QA-02`** · ยอมรับ
  implementation naming `TrainingLink`, `TrainingLinkId`, `RecipientName`, `SessionId`,
  `SessionStatus`/`LinkStatus` และ wire camelCase คู่กัน · public learner contract ใช้
  `(token, learnerKey)` แล้ว server resolve `LearningSession.Id` แทนการรับ id +
  `X-Learner-Key` · ระบุคู่ token/key เป็น composite bearer credential พร้อมข้อห้าม log/cache/analytics,
  HTTPS-only, mismatch คืน 404 และ Security gate · จำกัด audit setter เฉพาะ flow ที่มีจริง ·
  ยอมรับ migrations จริง `20260813140603_SplitLinkAndAddAuth` และ
  `20260818155126_AddTotalSlideCount` โดยไม่สร้าง migration เพิ่ม · ไม่มี table/column/relation ใหม่,
  ไม่เปลี่ยน business requirement และไม่ปิด `LS-QA-01` ซึ่งยังรอ apply deployment DB
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
