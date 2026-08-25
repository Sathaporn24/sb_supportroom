# 1 ลิงก์ = หลายการเรียนแยกคนละคน (learning-session) — Implementation Plan

## ⚠️ อ่านก่อนใช้แผนนี้ — โมดูลนี้ไม่ใช่งานเริ่มจากศูนย์

**งานส่วนใหญ่ของโมดูลนี้ทำไปแล้วนอก pipeline ก่อนมี `plan.md`** และผ่าน `qa-engineer` FULL-1
เมื่อ 2026-08-19 แล้ว ผลรวมเป็น **❌ 30/53 tasks verified — sent back for fixes** รายละเอียดอยู่ใน
`review.md` · `system-analyst` ปิด `LS-QA-02` ด้วย Contract Amendment CA-1..CA-6 แล้ว ส่วน
`LS-QA-01` และ `LS-QA-03..08` ยังเปิดอยู่

**ขั้นต่อไปคือส่ง unchecked implementation tasks ที่ชัดเจนให้ `backend-engineer` และ
`frontend-engineer` แก้ตามลำดับ dependency แล้วจึงขอ `qa-engineer` re-verify** · checkbox `[x]`
ทั้งหมดเป็นผลของ FULL-1 และห้ามแก้/เคลื่อนย้าย; checkbox `[ ]` คือสิ่งที่ยังต้องแก้ ตรวจกับ
environment จริง หรือ re-verify ตามข้อความของ task นั้น

### Contract Amendment ที่ใช้ในแผนนี้ — CA-1..CA-5

เจ้าของโปรเจกต์ยืนยัน amendment เมื่อ 2026-08-19 ให้ยึด contract ต่อไปนี้เหนือ baseline proposal
วันที่ 2026-08-18 เมื่อขัดกัน:

- naming: `TrainingLink`, `TrainingLinkId`, `RecipientName`, child `SessionId`,
  `SessionStatus`/`LinkStatus` และ camelCase wire names คู่กัน (CA-1)
- public learner API/SignalR: client ส่ง `(token, learnerKey)` แล้ว server resolve
  `LearningSession.Id`; ไม่รับ public `learningSessionId` และไม่ใช้ `X-Learner-Key` (CA-2)
- `(token, learnerKey)` เป็น composite bearer credential: HTTPS-only, ห้าม log/cache/analytics,
  mismatch คืน 404 และ `LearnerKey` ห้ามออก response (CA-3)
- `SessionQuestion.UpdateBy`/`UpdateDate` เป็น `set`; delete audit fields และ chat audit fields
  คง `init` ได้เพราะไม่มี update/delete flow ใน scope นี้ (CA-4)
- migration contract คือ `20260813140603_SplitLinkAndAddAuth` และ
  `20260818155126_AddTotalSlideCount`; ไม่มี migration เพิ่มจาก amendment (CA-5)

### ผลของ LS-QA-02 หลัง Contract Amendment

ตารางนี้เคยเป็นรายการ drift ที่ FULL-1 ส่งให้ `system-analyst`; หลัง amendment แล้วให้ใช้คอลัมน์
"contract ปัจจุบัน" เป็นคำสั่งสำหรับ engineer/QA ไม่ใช่คำถามที่ต้องตัดสินอีก:

| จุด | Contract ปัจจุบัน | ผลต่อแผน |
|---|---|---|
| link naming/route | `TrainingLink` และ `/api/training-links` | ไม่ต้อง rename เป็น `LessonLink`/`/api/links` |
| learner routes | `/api/learning-sessions/{token}/join`, `/restart`, `/progress`, `/end`, `/summary` ตาม CA-2 | คง server-resolution shape; แก้เฉพาะ behavior/tests ที่ยัง fail |
| entity/wire names | `RecipientName`, `TrainingLinkId`, `SessionId`, `SessionStatus`, `LinkStatus` | ไม่ต้องทำ mechanical rename |
| realtime/voice | client ส่ง token/key; server resolve group/persistence เป็น `LearningSession.Id` | คง signatures ปัจจุบันและตรวจ isolation/no-cache |
| audit mutability | review update fields เป็น `set`; delete/chat audit fields คง `init` | ไม่มี soft-delete setter work ใน module นี้ |
| migration | migrations จริง 2 ใบตาม CA-5 | เหลือ approval/apply/verification ตาม LS-QA-01 |

## Plan Summary

9 phase ตรงกับ Module A–I ของ `design.md` เรียงตาม dependency ที่ `design.md` ระบุไว้:
**A → (B, C) → D → (E, F) → (G, H) → I** — A ต้องเสร็จก่อนทุกอย่างเพราะเป็น data foundation, D ต้องเสร็จก่อน E
และ F เพราะทั้งคู่ต้องใช้ chat/คำถามที่ผูกกับการเรียนแล้ว โปรเจกต์นี้ scaffold ไว้แล้ว (ASP.NET Core +
Next.js อยู่ก่อนแล้ว) จึงไม่มี Phase 0 ของ `setup`

**เพิ่ม 2026-08-23**: Phase 7–9 (Module G/H/I) มาจาก amendment ของ `design.md` วันเดียวกัน ครอบ
F9 (responsive ทั้งห้องเรียน + หน้าจบ/หมดอายุ), F10 (พิมพ์ถาม AI แทนพูด) และ F10-a (รื้อฟีเจอร์แชต
คุยกับ CS ทั้งฝั่งผู้เรียนและฝั่ง CS ทิ้งทั้งหมด) รวมถึงมติ U1 (ถอดการตอบ "พร้อมหรือยัง" ด้วยเสียงทิ้ง
เหลือปุ่มกดอย่างเดียว) ซึ่งพาดเข้า regression surface ของ Module C/D/E ที่ QA ปิดไปแล้ว (ดู R19/R20
ใน `design.md`) · Phase 7 (G) และ 8 (H) เป็นอิสระต่อกันและทำขนานกันได้ แต่ **ทั้งคู่ต้องเสร็จ
สมบูรณ์ก่อน Phase 9 (I) เริ่ม** เพราะ I เรียก endpoint ของ G และเขียนทับไฟล์ที่ H รื้อ — ห้ามส่งมอบ
G หรือ H ครึ่งทางแล้วเริ่ม I · U2 (`SessionQuestion.Source`) และ U3 (`DropTable ChatMessage`) รวมอยู่ใน
migration ใบเดียวกัน (`RemoveChatMessageAndAddQuestionSource`) ที่ G และ H ต่างเขียนคนละส่วน — ดู
Sequencing Notes สำหรับวิธีประสานงานไฟล์เดียวกัน · U1 ไม่มี schema change และไม่มี migration ใบที่ 4

**สถานะพิเศษ**: FULL-1 ยืนยันแล้ว 30/53 tasks; งานค้างคือ migration environment (`LS-QA-01`),
implementation/test gaps (`LS-QA-03/04/06/07`), manual verification (`LS-QA-05`) และ Security gate
(`LS-QA-08`) · `LS-QA-02` ไม่ใช่ implementation work อีกต่อไปเพราะ CA-1..CA-5 ยอมรับ naming และ
server-resolution variant แล้ว

## Phase 1: Module A — Data foundation & migration
`TrainingLink` (rename จาก `TrainingSession` + ตัด 6 คอลัมน์ + เพิ่ม `MaxAttendees`) ·
`LearningSession` (ตารางใหม่) · `SessionQuestion`/`ChatMessage.SessionId` ชี้ `LearningSession` +
คอลัมน์รีวิว 3 ตัว + เฉพาะ `UpdateBy`/`UpdateDate` เป็น `set` · ลบ `SessionSummary` ทั้งชุด ·
status constants · repository + `UnitOfWork.Register` · `ApplicationDbContext` query filter ·
migrations 2 ใบตาม CA-5 · `INACTIVE_THRESHOLD_MINUTES` ใน `ServerDefaults`

**สถานะที่พบตอนตรวจโค้ด**: entity `TrainingLink`/`LearningSession` แยกกันจริงแล้ว · `SessionSummary`
entity ไม่มีอยู่ในโค้ดแล้ว (ลบไปแล้วตาม Q4) · migration `20260813140603_SplitLinkAndAddAuth` และ
`20260818155126_AddTotalSlideCount` เขียนเสร็จแล้วทั้งคู่ (แบบ `RenameTable`/backfill ไม่ใช่
`DropTable`+`CreateTable`) — **แต่ทั้งสองใบยังไม่เคย apply กับ DB จริงของ deployment**

- [x] [backend] Inspect `20260813140603_SplitLinkAndAddAuth`, ทำ migration SQL/dry-run กับฐานทดสอบที่ปลอดภัย และส่งหลักฐานว่า rename/backfill ใช้ได้กับข้อมูล demo; ห้าม apply deployment DB เอง
- [x] [backend] Inspect ลำดับ `20260818155126_AddTotalSlideCount` ต่อจากใบแรกและเตรียมคำสั่ง/rollback note สำหรับ handoff; การ apply environment จริงเป็นงาน `devops` หลังผู้ใช้อนุมัติเฉพาะเจาะจง
- [x] [backend] ยืนยันว่า `TrainingLink` entity ตรง CA-1 และ baseline DM-1 ทุกฟิลด์ — ไม่มีคอลัมน์ `Status` หลงเหลือ
- [x] [backend] ยืนยันว่า `LearningSession` ตรง CA-1 + baseline DM-2 โดยใช้ `TrainingLinkId` และ `RecipientName` ตาม contract ปัจจุบัน; ห้าม rename กลับเป็น proposal เดิม
- [x] [backend] ยืนยันว่า `SessionQuestion` มี `ReviewResult`/`ReviewNote`/`ReviewedAt`, `UpdateBy`/`UpdateDate` เป็น `set` และ delete audit fields คง `init` ตาม CA-4 โดยไม่มี soft-delete flow แฝง
- [x] [backend] ยืนยันว่า `SessionQuestion.SessionId`/`ChatMessage.SessionId` ชี้ `LearningSession.Id` ทุก query/index/mapping ตาม CA-1; ไม่ต้อง rename เป็น `LearningSessionId`
- [x] [backend] ยืนยัน `SessionStatus`/`LinkStatus`/`ReviewResult` ตาม CA-1 — ค่า wire ต้องเป็น `IN_PROGRESS`/`ENDED`, `ACTIVE`/`EXPIRED`, `correct`/`incorrect` และไม่มี stored link status
- [x] [backend] ยืนยัน query filter ของ `TrainingLink`/`LearningSession`/`SessionQuestion`/`ChatMessage` ครบตาม DM-7 (`ICompanyScoped` + `HasQueryFilter`) และ `CompanyIsolationTests.EveryEntityIsCompanyScoped` ยัง pass จริง
- [x] [backend] ยืนยัน repository ตาม CA-4: `ITrainingLinkRepository.GetByToken`, `ILearningSessionRepository.GetActiveByLearnerKey/GetLatestInProgressByLearnerKey/GetLatestEndedByLearnerKey/GetByTrainingLinkId`, question/chat repositories และ `UnitOfWork.Register` ครบ; learner flow ไม่ต้องมี `GetByIdAcrossCompanies`
- [x] [backend] ยืนยันว่า `INACTIVE_THRESHOLD_MINUTES` มีใน `ServerDefaults.cs` + `.env.example` ตาม SR-1
- [x] [backend] รัน `dotnet build` + `dotnet test` หลัง apply migration จริง ยืนยันว่า 127 test เดิมยังผ่าน (ตัวเลขจากบันทึกก่อนหน้า ต้องตรวจซ้ำหลัง apply DB จริง)

## Phase 2: Module B — Link management (ฝั่ง CS)
`ITrainingLinkService` (create/list/get) · aggregate counts (`learnerCount`/`inProgressCount`/`endedCount`) ·
สถานะ ACTIVE/EXPIRED คำนวณจาก `ExpiresAt` · `POST/GET /api/training-links` · ฟอร์มสร้างลิงก์
(ตัดช่องชื่อผู้เรียน เพิ่ม `MaxAttendees` + ข้อความกำกับว่ายังไม่มีผล) · หน้ารายการลิงก์

**สถานะที่พบตอนตรวจโค้ด**: `ITrainingLinkService`, `TrainingLinkController` (`/api/training-links`),
หน้า `admin/links/new`, `admin/links/[token]` มีอยู่แล้ว — ยังไม่ตรวจละเอียดว่า aggregate counts
และข้อความกำกับ `MaxAttendees` ตรง F8 หรือไม่

- [x] [backend] ยืนยันว่า `GET /api/training-links` ส่ง `learnerCount`/`inProgressCount`/`endedCount`/`status` (ACTIVE/EXPIRED) ตามที่ตารางเดิมของ `design.md` ระบุ
- [x] [backend] ยืนยันว่า `POST /api/training-links` (`CreateTrainingLinkDto`) ตัด `recipientName` ออกแล้วจริง และมี `maxAttendees` (validate `>= 1` เมื่อส่งมา) ตาม LR-7
- [x] [backend] ยืนยันว่า `ITrainingLinkService` ไม่มี logic ใดๆ ที่บังคับใช้ `MaxAttendees` (LR-2 — ห้ามมี `if` แตะฟิลด์นี้เพื่อเช็คจำนวนคน)
- [x] [frontend] ยืนยันว่าฟอร์มสร้างลิงก์ (`admin/links/new`) ตัดช่องชื่อผู้เรียนออกแล้ว และมีข้อความกำกับชัดเจนว่า `MaxAttendees` ยังไม่มีผลในระบบ (F8)
- [x] [frontend] ยืนยันว่าหน้ารายการลิงก์ (`app/admin/page.tsx`) แสดง aggregate counts และสถานะ ACTIVE/EXPIRED ต่อลิงก์

## Phase 3: Module C — Learning lifecycle (ฝั่งผู้เรียน, API) 🔒 Security gate
LR-1 ถึง LR-8 ทั้งชุด · `ILearningSessionService` · endpoint `/api/learning-sessions/*` ·
การบังคับ expiry ที่ backend เป็นครั้งแรก · SR-1..SR-3 (คำนวณ `IsStalled`)

**สถานะหลัง amendment**: route/DTO ปัจจุบันที่ใช้ token ใน path และ `learnerKey` ใน body/query
เป็น contract ตาม CA-2 แล้ว ไม่ต้องย้ายเป็น learner-supplied id/header; งานค้างของ phase นี้คือ
contract tests ใน `LS-QA-04`, credential handling/no-log ตาม CA-3 และ Security gate

- [x] [backend] ยืนยัน LR-1 (สร้างการเรียนใหม่) ครบลำดับ 7 ข้อ — โดยเฉพาะ resolve company ก่อนแตะอย่างอื่น (ข้อ 2) และปฏิเสธเมื่อ `ExpiresAt` ผ่านแล้ว (ข้อ 3) ห้ามสร้างแถว
- [x] [backend] ยืนยัน LR-2 — ไม่มี logic ใดตรวจ/บังคับ `MaxAttendees` ใน service การเรียน
- [x] [backend] ยืนยัน LR-3/LR-3a ตามตาราง 6 กรณีใน `design.md` ทุกกรณี (`resumable`+`lastEnded`+`linkExpired` ทุก combination) — endpoint จริงคือ `GET /api/learning-sessions/{token}/resume?learnerKey=` ตรวจว่า response shape ให้ frontend ตัดสินใจได้ครบ 6 กรณีจริง
- [x] [backend] ยืนยัน LR-4 (บันทึกความคืบหน้า) — ไม่เขียนอะไรเมื่อ `Status = ENDED`, เขียน `TotalSlideCount` เฉพาะเมื่อ non-null และ > 0, `CompletedAllSlides` เป็น one-way true, ไม่เช็ค expiry
- [x] [backend] ยืนยัน LR-5 (กดจบ) — idempotent เมื่อ `ENDED` อยู่แล้ว, `CompletedAllSlides` เป็น OR ไม่ใช่ทับ, ไม่เช็ค expiry
- [x] [backend] ยืนยัน LR-6 ("เรียนอีกครั้ง"/Restart) — ไม่แตะแถวเก่าทุกกรณี ปฏิเสธถ้าลิงก์หมดอายุ
- [x] [backend] ยืนยัน LR-8 — ไม่มี endpoint เดิม `PATCH /api/sessions/{token}` (action=start/end) หลงเหลือ, ไม่มี `MarkStarted` เหลือใน service
- [x] [backend] ยืนยัน IC-1 — ทุก endpoint ฝั่งผู้เรียน resolve company จากแถว (lookup token/id แบบข้าม query filter) ก่อน query อื่นทุกตัว
- [x] [backend] ยืนยัน CA-3/IC-1 — ข้าม query filter เฉพาะ `ITrainingLinkRepository.GetByToken()` ที่มีเหตุผลกำกับ แล้ว `CompanyContext.Resolve(link.CompanyId)` ก่อน scoped query; public learner flow ไม่ใช้ `GetByIdAcrossCompanies`
- [x] [backend] ยืนยัน CA-2/CA-3 — `progress`/`end`/`summary`/questions/chat resolve ด้วย `(token, learnerKey)`, wrong pair คืน `NotFound` (404), ไม่คืน `LearnerKey` และไม่รับ public `learningSessionId`
- [x] [backend] ตรวจ request logging/telemetry/cache ของ anonymous learner routes ให้ไม่บันทึก token, `learnerKey` หรือ full query string และระบุ HTTPS-only deployment requirement ตาม CA-3; ถ้า runtime/proxy รับประกันไม่ได้ให้หยุดและส่งกลับ `system-analyst` ก่อน production
- [x] [backend] ยืนยัน SR-1/SR-2/SR-3 — `INACTIVE_THRESHOLD_MINUTES` อ่านจาก env, `IsStalled` คำนวณที่ backend ที่เดียว (ดูโค้ด `ToViewModel` ใน `LearningSessionService`), แถว `ENDED` ไม่มีวัน stalled
- [x] [backend] เขียน/ยืนยัน `LearningSessionServiceTests.cs` ครอบ LR-1..LR-6 ตามรายการที่ `design.md` §Migration Plan → Test ที่กระทบ ระบุไว้

## Phase 4: Module D — Conversation re-pointing & realtime 🔒 Security gate
`SessionQuestion`/`ChatMessage.SessionId` ผูกกับการเรียน (ไม่ใช่ลิงก์) · public
`POST /api/voice-question` และ SignalR รับ `(token, learnerKey)` แล้ว server resolve ·
**group key ต้องเป็น `LearningSession.Id` ไม่ใช่ token/ลิงก์** ·
`IRealtimeNotifier` · `useSessionChat`/`useAgentSessionChat`

**สถานะที่พบตอนตรวจโค้ด**: SignalR group key เปลี่ยนเป็นอิง `LearningSession.Id` แล้วจริง (ยืนยันจาก
`SessionHub.cs` — `JoinSession(token, learnerKey)` resolve เป็น `LearningSession.Id` ฝั่ง server
ก่อนเข้ากลุ่ม) และ `POST /api/voice-question` ใช้ contract เดียวกันตาม CA-2 — **นี่คือจุดเสี่ยงสูงสุด
ของทั้งโมดูล ต้องตรวจอย่างละเอียดด้วยมือ ไม่ใช่แค่อ่านโค้ด**

- [x] [backend] ทดสอบว่าสองผู้เรียนบนลิงก์เดียวกันไม่เห็น chat/คำถามของกันและกันแบบ real-time (R1) — **ปิดแล้ว 2026-08-19 (วิธีเปลี่ยนจาก "two-browser UI" เป็น "two independent SignalR connections")**: เครื่องมือ browser ที่มีทุกตัวไม่ให้สองเบราว์เซอร์แยกโปรไฟล์จริง (`Claude_Browser` หลายแท็บ share `localStorage` origin เดียวกัน; `claude-in-chrome` ต่อกับ Chrome บนเครื่อง Windows คนละเครื่อง เข้า `localhost` ของเครื่องนี้ไม่ได้) จึงทดสอบที่ระดับ SignalR connection โดยตรงแทน: สคริปต์ Node ใช้ `@microsoft/signalr` เปิดสอง `HubConnection` อิสระเชื่อม `/hubs/session` แยกกัน แต่ละอันเรียก `JoinSession(token, learnerKey)` ด้วย `learnerKey` คนละตัว (สอง `LearningSession` แยกกันบน `TrainingLink` เดียวกัน สร้างผ่าน SQL ตรง) แล้วให้ A เรียก `SendChatMessage` — ผลจริง: A ได้รับข้อความของตัวเอง, B ได้รับ `[]` ว่างเปล่า ไม่รั่ว ตรงตาม `Groups.AddToGroupAsync` ใช้ `LearningSession.Id` เป็น group key (`SessionHub.cs` บรรทัด 29-32) วิธีนี้พิสูจน์กลไก group-scoping จริงที่ระดับ transport ซึ่งเป็นสิ่งที่ R1 ต้องการยืนยัน ไม่ใช่การลดมาตรฐานจาก UI test — ทดสอบเสร็จลบสคริปต์และข้อมูลทดสอบทิ้งแล้ว ไม่ทดสอบ `ReceiveNewQuestion`/IC-6 แยกอีกรอบเพราะ `SignalRRealtimeNotifier.cs` ใช้ pattern เดียวกันทุก event (`Clients.Group(learningSessionId).SendAsync(...)`) เป็นโค้ดร่วม ไม่ใช่ logic แยกต่อ event type
- [x] [backend] ยืนยันว่า `POST /api/voice-question` ผูกคำถามกับ `LearningSession` ที่ถูกต้องเสมอ แม้ทั้งสองคนเปิดลิงก์เดียวกันพร้อมกัน (R2) — ตรวจ race condition ที่ resolve จาก `(token, learnerKey)` ทุกครั้ง ไม่ cache ข้าม request
- [x] [backend] ยืนยันว่า `SessionQuestion`/`ChatMessage` ทุกแถวใหม่ใช้ `SessionId` ตาม CA-1 และผูกกับ `LearningSession` ที่ถูกต้อง ไม่มีจุดใดยังผูกกับ `TrainingLink` โดยตรง
- [x] [backend] ยืนยันว่า `IRealtimeNotifier` (`NotifyChatMessageAsync`/`NotifyNewQuestionAsync`) broadcast เข้ากลุ่มที่ผูกกับการเรียน ไม่ใช่กลุ่มที่ผูกกับ token/ลิงก์
- [x] [frontend] ยืนยันว่า `useSessionChat`/`useAgentSessionChat` เรียก `JoinSession`/`SendChatMessage` ด้วยพารามิเตอร์ที่ตรงกับ signature จริงของ `SessionHub.cs` (ไม่ใช่ signature เดิมตาม token อย่างเดียว)
- [x] [frontend] ยืนยันว่าไม่มีจุดใดใน frontend ที่ยัง broadcast/join ด้วย token อย่างเดียวโดยไม่มี `learnerKey`/learning-session context

## Phase 5: Module E — Learner-facing UI 🔒 Security gate
หน้ากรอกชื่อ + 6 กรณีของ LR-3 · หน้ายืนยันก่อนเรียนต่อ (LR-3a — D2) · `localStorage LearnerKey` (IC-4) ·
ห้องเรียนส่ง progress ทุกครั้งที่เปลี่ยนสไลด์ · ปุ่มกดจบเอง · หน้าสรุปผู้เรียน (`LearnerQuestionViewModel`
เท่านั้น ไม่มีผลรีวิว/`unansweredPoints`) · ปุ่ม "เรียนอีกครั้ง"

**สถานะที่พบตอนตรวจโค้ด**: `app/join/[token]/page.tsx` และ `app/room/[token]/page.tsx` มีอยู่แล้ว
รวมถึงบั๊ก React Strict Mode ที่ทำให้ one-shot room-entry grant (`grantRoomEntry`/`consumeRoomEntry`)
ถูก consume สองครั้ง — **แก้แล้วด้วย `useRef` ตามที่แจ้งมา (ยังไม่ผ่าน QA ยืนยัน)** — โมดูลนี้เป็น
จุดบังคับ LR-3a/IC-7 เพียงจุดเดียวในระบบ (server แยกไม่ออกว่าผ่านการยืนยันมาหรือยัง) จึง**ต้องทดสอบ
ด้วยมือทุกกรณี ไม่พอแค่ตรวจโค้ด**

- [x] [frontend] ทดสอบด้วยมือทั้ง 6 กรณีในตาราง LR-3 ของ `design.md` (มี resumable+ไม่หมดอายุ / มี resumable+หมดอายุ / ไม่มี resumable มี lastEnded+ไม่หมดอายุ / ไม่มี resumable มี lastEnded+หมดอายุ / ไม่มีทั้งคู่+ไม่หมดอายุ / ไม่มีทั้งคู่+หมดอายุ) ว่าหน้าจอที่แสดงตรงกับตารางเป๊ะ — **ครบ 6/6 แล้ว 2026-08-19**: 3 กรณีไม่หมดอายุยืนยันไปก่อนหน้านี้แล้ว (resumable, lastEnded เท่านั้น, ไม่มีทั้งคู่) ส่วนอีก 3 กรณี `linkExpired=true` ปิดรอบนี้ด้วยลิงก์ทดสอบที่ตั้ง `ExpiresAt` ผ่านแล้วจริงผ่าน SQL ตรง + สร้าง `LearningSession` คู่กัน ทดสอบในเบราว์เซอร์จริง (ตรวจ `disabled` attribute ผ่าน `document.querySelectorAll('button')` ไม่ใช่แค่ดูภาพ): resumable+หมดอายุ → ปุ่ม "ใช่ เรียนต่อจากเดิม" `disabled:false`, ปุ่ม "ไม่ใช่ เริ่มเรียนใหม่ในชื่ออื่น" `disabled:true` พร้อมข้อความอธิบาย ตรงกับ `join/[token]/page.tsx` (`disabled={linkExpired}` บนปุ่มเริ่มใหม่); lastEnded เท่านั้น+หมดอายุ → ไม่ render ฟอร์ม "เรียนอีกครั้ง" เลย มีแต่ข้อความ "ลิงก์นี้หมดอายุแล้ว เริ่มเรียนรอบใหม่ไม่ได้ค่ะ"; ไม่มีทั้งคู่+หมดอายุ → redirect ไป `/link-expired` ตรงกับโค้ดบรรทัด `if (!resume.resumable && !resume.lastEnded && resume.linkExpired) router.replace("/link-expired")` ลบข้อมูลทดสอบทิ้งหลังตรวจเสร็จ
- [x] [frontend] ทดสอบด้วยมือ IC-7: เปิดลิงก์ → กรอกชื่อ → ออกกลางคัน → เปิดลิงก์เดิมบนเบราว์เซอร์เดิมอีกครั้ง ต้องเจอหน้ายืนยัน (LR-3a) ทุกครั้ง ไม่ใช่ถูกพาเข้าห้องเลย — รวมถึงกรณีเปิด `/room/[token]` ตรงๆ โดยไม่ผ่าน `join/[token]` ต้องถูก redirect กลับ (ยืนยันด้วยเบราว์เซอร์จริง 2026-08-19 — เปิด `/room/[token]` ตรงๆ ไม่มี grant → redirect ไป `/join/[token]` จริง (`href` ยืนยันด้วย JS), และ flow "ใช่ เรียนต่อจากเดิม" → เข้าห้องสำเร็จ ไม่เด้งกลับ — `LS-QA-05` ปิดครบแล้ว 2026-08-19 (MANUAL-5))
- [x] [frontend] ยืนยันว่าปุ่ม "เริ่มเรียนใหม่ในชื่ออื่น" ใช้ `learnerKey` เดิม (ไม่สร้างใหม่ ไม่ล้าง `localStorage`) และไม่แตะแถวเดิมเลย
- [x] [frontend] ยืนยันว่าไม่มีการเก็บสถานะ "ยืนยันแล้ว" ไว้ใน `localStorage`/cookie/query string เพื่อข้ามหน้ายืนยันครั้งถัดไป (ต้องถามทุกครั้งที่เปิดหน้า `join/[token]` ใหม่)
- [x] [frontend] ยืนยันว่า one-shot room-entry grant (`grantRoomEntry`/`consumeRoomEntry`) ทำงานถูกต้องหลังแก้บั๊ก Strict Mode — ทดสอบ dev mode (Strict Mode เปิด) จริง ไม่ใช่แค่ production build (ยืนยันด้วยเบราว์เซอร์จริง 2026-08-19 — `next.config.ts` มี `reactStrictMode: true`, dev server รันด้วย config นี้จริง, กด "ใช่ เรียนต่อจากเดิม" แล้วเข้าห้องสำเร็จ ไม่ถูกเด้งกลับ `/join` ซึ่งคือ regression เดิมที่ `entryGrantedRef` แก้ — ไม่กลับมาเป็นซ้ำ)
- [x] [frontend] ยืนยันว่า `hooks/use-tutor-session.ts` ยิง LR-4 (progress) ทุกครั้งที่ `currentSlideIndex` เปลี่ยน พร้อม `totalSlides` และไม่มี heartbeat แยกต่างหาก
- [x] [frontend] ยืนยันว่าปุ่มกดจบเองเรียก LR-5 ถูกต้อง และ idempotent เมื่อกดซ้ำ/`beforeunload` ยิงซ้ำ
- [x] [frontend] ยืนยันว่าหน้าสรุปผู้เรียนใช้ ViewModel ฝั่งผู้เรียนเท่านั้น (`LearnerSessionQuestionViewModel`/เทียบเท่า) — ไม่มี `reviewResult`/`reviewNote`/`reviewedAt`/`unansweredPoints` รั่วออกมาใน response หรือใน DOM
- [x] [frontend] ยืนยันว่าปุ่ม "เรียนอีกครั้ง" เรียก Restart (LR-6) และ prefill ชื่อเดิมให้แก้ได้
- [x] [frontend] ยืนยันว่า `LearnerKey` สร้างด้วย `crypto.randomUUID()` เก็บใน `localStorage` คีย์เดียวตาม IC-4 ใช้ข้ามลิงก์ได้จริง

## Phase 6: Module F — CS console & review 🔒 Security gate
รายการการเรียนใต้ลิงก์ + badge "หยุดกลางคัน" + "7/20" · หน้ารายละเอียดการเรียน · UI รีวิวถูก/ผิด +
หมายเหตุ · `PATCH /api/session-questions/{id}/review`

**สถานะที่พบตอนตรวจโค้ด**: `app/admin/learning-sessions/[id]/page.tsx` มีอยู่แล้ว รวม UI merge
คำถาม live กับคำถามที่โหลดมา + คง review fields ไว้เมื่อมีข้อความใหม่เข้ามา ·
`PATCH /api/session-questions/{id}/review` (RR-1) path ตรงกับ `design.md` เป๊ะ — **ยังไม่ได้ตรวจ
RR-2..RR-6 ทีละข้อ และไม่เคยมี `qa-engineer`/`security` ตรวจ ไม่ว่า Module นี้เคย merge มาจาก
branch อื่นซึ่งไม่เคยเทียบกับ RR-1..RR-5 อย่างเป็นทางการ**

- [x] [backend] ยืนยัน RR-2 — `reviewResult` รับได้เฉพาะ `"correct"`/`"incorrect"`/`null` ค่าอื่น (รวม `""`) ต้อง validation error
- [x] [backend] ยืนยัน RR-3 — `reviewResult = null` ล้างทั้งชุด (`ReviewResult`/`ReviewNote`/`ReviewedAt` เป็น `null` หมด) ไม่ใช่แค่เคลียร์บางฟิลด์ · `reviewNote` trim แล้วว่าง → เก็บเป็น `null` · เกิน 2000 ตัวอักษร → validation error
- [x] [backend] ยืนยัน RR-4 — รีวิวซ้ำได้ไม่จำกัด ทับค่าเดิม ไม่มีการเก็บประวัติการรีวิว
- [x] [backend] ยืนยัน RR-5 — ตรวจ ViewModel ฝั่งผู้เรียนกับฝั่ง CS เป็นคนละตัวจริง ไม่ใช่ตัวเดียวกันที่ซ่อน field ด้วย conditional
- [x] [backend] ยืนยัน RR-6 — `unansweredPoints` คำนวณสดจาก `AnswerStatus = not_found` ทุกครั้งที่อ่าน ไม่ cache/เก็บซ้ำ
- [x] [backend] ยืนยันว่า endpoint รายการการเรียนใต้ลิงก์ (`GET /api/training-links/{id}/learning-sessions`) ส่ง `isStalled` (SR-2) มาให้ frontend ใช้ ไม่ต้องคำนวณเอง
- [x] [backend] ยืนยันว่า CS by-id REST และ `JoinSessionAsAgent`/`SendChatMessageAsAgent` ใช้ authenticated company-scoped query พร้อม authorization guard ตาม CA-2/CA-3; ห้ามใช้ public token/key flow แทนสิทธิ์ CS
- [x] [frontend] ยืนยันว่าหน้ารายการการเรียนใต้ลิงก์แสดง badge "หยุดกลางคัน" (จาก `isStalled` ที่ backend ส่งมา) และ "7/20" (`lastSlideIndex`/`totalSlideCount`) ถูกต้อง
- [x] [frontend] ยืนยันว่า UI รีวิวถูก/ผิด + ช่องหมายเหตุใช้งานได้ครบ (บันทึก/ล้างรีวิว) และไม่มี dropdown/enum ของสาเหตุ (มติ 2026-08-11 — หมายเหตุต้องเป็นข้อความอิสระ)
- [x] [frontend] ยืนยันว่าหน้า CS เห็น `unansweredPoints`/ผลรีวิวครบ แต่หน้าผู้เรียน (Phase 5) ไม่เห็นเลย — cross-check กับ task ของ Phase 5

## Phase 7: Module G — Typed questions: backend + provider 🔒 Security gate

`POST /api/text-question` + `TextQuestionController` · `AskTextQuestionDto` ·
`IVoiceQuestionService.AskTextAsync` (แชร์ core กับ `AskAsync`) · `IVoiceQuestionProvider.AnswerTextAsync`
+ implementation ทั้ง 2 ตัว · `DtoLimits.QuestionTextMaxLength` · `SessionQuestion.Source` +
`QuestionSource` (U2) · migration `RemoveChatMessageAndAddQuestionSource` ส่วนที่ 1 (`AddColumn Source`)
· **ถอด readiness-by-voice ฝั่ง backend ทั้งชุดตามมติ U1 (TQ-22/TQ-23)**

**Sensitive — เหตุผล gate**: endpoint anonymous ตัวใหม่ตัวแรกนับตั้งแต่ `security` ยังไม่เคย audit
โมดูลนี้เลย (`LS-QA-08`) · รับ untrusted text จากคนไม่มี account เข้า prompt ของ LLM (prompt
injection, R14) · ไม่มี rate limiting (R15 — บันทึกเป็น open question ให้เจ้าของโปรเจกต์ตัดสินแยก
ไม่ใช่งานของ phase นี้) · แตะ `KnowledgeNamespaces` ที่กั้นข้อมูลข้ามบริษัทชั้นเดียวของ vector store (KS-1)

**⚠️ Regression surface**: งาน U1 แก้ `POST /api/voice-question` ซึ่งเป็นของ Module C/D ที่ QA
ปิด FULL-3 ไปแล้ว — request ตัด field `expecting`, response ตัดฟิลด์ `readiness` — **ต้อง re-verify
เส้นทางถามด้วยเสียงทั้งเส้น ไม่ใช่ตรวจเฉพาะ endpoint ใหม่** (R19)

- [ ] [backend] เพิ่ม `DtoLimits.QuestionTextMaxLength = 2000` และแก้ XML doc ของ `MaxTextLength` ให้เหลือเฉพาะ TTS (TQ-3)
- [ ] [backend] สร้าง `Domain/Enums/QuestionSource.cs` (`static class` + `const string Voice`/`Text`) (U2, DM-3a)
- [ ] [backend] เพิ่ม `SessionQuestion.Source` (required `string`, ไม่มี default ที่ entity level) ตาม DM-3a
- [ ] [backend] สร้าง/ต่อ migration `RemoveChatMessageAndAddQuestionSource`: เพิ่ม `AddColumn<string>("Source","SessionQuestion", nullable:false, defaultValue:"voice")` พร้อมคอมเมนต์อธิบายว่าทำไม `"voice"` ถูกต้องย้อนหลัง 100% แล้วถอด default constraint ออกหลัง backfill (MG-R1 ข้อ 1) — **ไฟล์นี้ใช้ร่วมกับ Module H ที่เพิ่ม `DropTable("ChatMessage")` เข้าไฟล์เดียวกัน ดู Sequencing Notes**
- [ ] [backend] สร้าง `Application/Dto/AskTextQuestionDto.cs` (`token`, `learnerKey`, `text`, `currentSlideObjectId?`)
- [ ] [backend] สร้าง `Api/Controllers/TextQuestionController.cs` `POST /api/text-question` `[AllowAnonymous]` `Content-Type: application/json` พร้อม validation ตามลำดับ TQ-3 (ห้ามสลับลำดับ)
- [ ] [backend] เพิ่ม `/api/text-question` ใน `IsSensitiveLearnerPath()` (`Program.cs` ~187) — coordinate กับ Module H ที่ลบ `/api/chat-messages` ออกจากรายการเดียวกันในคอมมิตเดียวกัน (TQ-2, CX-4 #12)
- [ ] [backend] เพิ่ม `TextQuestionInput` และ `Task<VoiceQuestionResult> AnswerTextAsync(TextQuestionInput)` ใน `IVoiceQuestionProvider` — ไม่มี `Expecting`/`Audio`/`MimeType`/`DurationMs` (TQ-7)
- [ ] [backend] เพิ่ม `IVoiceQuestionService.AskTextAsync(AskTextQuestionDto)` แชร์ core กับ `AskAsync` ตามลำดับ TQ-4: resolve `(token, learnerKey)` ก่อนแตะอย่างอื่น → ปฏิเสธถ้า `Ended` → resolve lesson content → เรียก provider พร้อม 3 namespace ที่ caller ประกอบ (KS-1) → บันทึก+broadcast ตาม TQ-5/TQ-6
- [ ] [backend] ยืนยันว่า `Source = QuestionSource.Text` มาจาก `AskTextAsync` และ `Source = QuestionSource.Voice` มาจากเส้นทางเสียง — ไม่มี default ให้ลืมส่ง (TQ-5, U2)
- [ ] [backend] Implement `RagVoiceQuestionProvider.AnswerTextAsync` โดยใช้ `BuildGroundingContextAsync`/`BuildAnswerPrompt`/conflict flow เดิมทุกตัว — ห้าม copy-paste เป็นเมธอดที่สอง (TQ-8)
- [ ] [backend] Implement `GeminiVoiceQuestionProvider.AnswerTextAsync` + `BuildTextPrompt(groundingContext, questionText)` — วางคำถามผู้เรียนในบล็อกที่ระบุชัดว่าเป็น "คำถามของคุณครู" ห้ามปนกับบรรทัดกติกา ห้ามส่ง audio (TQ-9, R14)
- [ ] [backend] ยืนยันว่า provider ล้มเหลว/parse JSON ไม่ได้ → `throw GeneralException.UpstreamError(...)` และ **ไม่เขียนแถวลง DB** (ต่างจากเส้นทางเสียงที่บันทึก `transcription_failed`) (TQ-10, R16)
- [ ] [backend] ยืนยันว่า log ของเส้นทางพิมพ์ไม่มีข้อความคำถาม/`Transcript`/`Answer`/`token`/`learnerKey`/query string (TQ-12, CA-3)
- [ ] [backend] อัปเดต `CreateSessionQuestionDto`/`SessionQuestionViewModel` ให้มีฟิลด์ `Source` — `LearnerQuestion`/ViewModel ฝั่งผู้เรียนไม่มี `source` (RR-5 ยังบังคับ)
- [ ] [backend] ยืนยันว่า `PATCH /api/session-questions/{id}/review` response เพิ่ม `source` แบบ additive อ่านอย่างเดียว ไม่กระทบ path เดิม
- [ ] [backend] ลบ `AskVoiceQuestionDto.Expecting` (`Application/Dto/AskVoiceQuestionDto.cs:24-25`) พร้อม XML doc (TQ-22)
- [ ] [backend] ลบ `VoiceQuestionController.cs:25` `VoiceQuestionRequest.Expecting` และการ map บรรทัด ~70 (TQ-22)
- [ ] [backend] ลบ `IVoiceQuestionProvider.cs:38-42` `VoiceQuestionInput.Expecting` และ `:63-64` `VoiceQuestionResult.Readiness` (TQ-22)
- [ ] [backend] ลบ `Application/ViewModel/VoiceAnswerViewModel.cs:9` `Readiness` — breaking response shape ของ `POST /api/voice-question`, แก้ TS ในคอมมิตเดียวกัน (Architecture Rule 7) (TQ-22)
- [ ] [backend] ลบ `GeminiRest.cs:64` `GeminiAnswerJson.Readiness` (TQ-22)
- [ ] [backend] ลบ `IVoiceQuestionService.cs:78` การ map `Expecting = input.Expecting` และบล็อก early-return `if (result.Readiness is not null) {...}` บรรทัด ~90-95 (TQ-22)
- [ ] [backend] ลบ `RagVoiceQuestionProvider.cs` `BuildReadinessPrompt()` (~51-58) และบล็อก `if (input.Expecting == "readiness") {...}` (~102-118) ทั้งก้อน — ห้ามแตะ pipeline คำถามจริง 3 step (TQ-23)
- [ ] [backend] ลบ `GeminiVoiceQuestionProvider.cs` `BuildReadinessPrompt()` (~36-44), ตัวแปร `isReadiness` (~54) และทุก branch ที่ใช้มัน (~55-60, ~75-85) — ห้ามแตะ/ลบ `BuildPrompt`, `GeminiRest.CallAsync`, `GeminiRest.IsAnswerStatus`, guard `no_speech` (TQ-23)
- [ ] [backend] แก้คอมเมนต์ `Domain/Entities/KnowledgeQnAConflict.cs:29` ให้ตรงว่า null มาจาก `AnswerStatus.NoSpeech` ไม่ใช่ readiness check อีกต่อไป — ห้ามแตะ logic (TQ-27)
- [ ] [backend] `grep -ri "readiness\|expecting" backend/src` ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง (เกณฑ์ปิดงาน TQ-22/TQ-23, ยกเว้น `KnowledgeQnAConflict.cs:29` ที่แก้ถ้อยคำแล้ว)
- [ ] [backend] เขียน/แก้ `VoiceQuestionServiceTests.cs` ตาม TQ-21: `AskTextAsync` บันทึก `Transcript`=ข้อความที่พิมพ์ตรงตัว, `Source="text"`, เส้นทางเสียงยัง `Source="voice"`, session `Ended` ถูกปฏิเสธด้วยข้อความเดียวกับเส้นทางเสียง, namespace ทั้ง 3 จาก `CurrentCompanyId`, provider ล้มเหลว → โยน error และไม่มีแถวถูกเขียน · ลบเคส `AskAsync_UnclearReadinessReply_DefaultsToNotReady` (~162-173) และตัดพารามิเตอร์ `expecting` ออกจาก helper `Ask(...)` (~118/126) (TQ-21, TQ-26)
- [ ] [backend] อัปเดตเอกสาร: `docs/schema.dbml` (เพิ่มคอลัมน์ `Source`), `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`, `docs/PROJECT_CONTEXT.md`, `docs/TECH_DECISIONS.md` (บันทึก U1/U2 เป็น TD ใหม่) ส่วนที่เกี่ยวกับ typed question + readiness removal — **ห้ามแก้ `docs/CORE_FEATURE_SPEC.md`**
- [ ] [backend] re-verify ด้วยมือ: อัดเสียงถามจริง → ได้คำตอบ → มีแถวใหม่ใน `SessionQuestion` (`Source="voice"`) → เด้งเข้าหน้ารีวิว CS — ไม่ใช่แค่ตรวจ endpoint ใหม่ (Regression surface, R19)

## Phase 8: Module H — Chat feature removal (ทั้ง stack + migration) 🔒 Security gate

26 จุดตาม CX-4/CX-5 · migration `RemoveChatMessageAndAddQuestionSource` ส่วน `DropTable("ChatMessage")`
(U3) · เอกสาร 13 ไฟล์ตาม CX-8 · test 5 ไฟล์ตาม CX-9 · **งานรื้อของเดิม ไม่ใช่งานเพิ่มของใหม่**

**Sensitive — เหตุผล gate**: (1) ลบตารางจริงพร้อมข้อมูล — breaking + data loss ที่ตั้งใจ (2) แตะ
`SessionHub` ซึ่งเป็นจุดกั้นข้อมูลข้ามผู้เรียนอันดับหนึ่งของโมดูล (IC-5/R1) — รื้อผิดที่ group key หรือ
`JoinSessionAsAgent` ทำให้บทสนทนาไหลข้ามผู้เรียนโดยไม่มี error (3) แตะ `IsSensitiveLearnerPath` ใน
`Program.cs` ซึ่งบังคับ CA-3 (no-store/no-referrer) — ลบผิดบรรทัดถอด header ป้องกันของ endpoint อื่นไปด้วย

**Dependencies**: ไม่มี (เริ่มได้ทันที ขนานกับ Phase 7 ได้) **แต่ต้องเสร็จก่อน Phase 9 (I)** เพราะ I
เขียน `room/[token]/page.tsx` และ drawer ทับที่เดิม

- [ ] [backend] ลบ `Domain/Entities/ChatMessage.cs` ทั้งไฟล์ (DM-4, CX-4 #1)
- [ ] [backend] ลบ `Domain/Enums/ChatSenderRole.cs` ทั้งไฟล์ (DM-6a, CX-4 #2)
- [ ] [backend] ลบ `Application/Services/IChatMessageService.cs` ทั้งไฟล์ (CX-4 #3)
- [ ] [backend] ลบ `Application/Dto/SendChatMessageDto.cs` ทั้งไฟล์ (CX-4 #4)
- [ ] [backend] ลบ `Application/ViewModel/ChatMessageViewModel.cs` ทั้งไฟล์ (CX-4 #5)
- [ ] [backend] ลบ `TypeAdapterConfig<ChatMessage, ChatMessageViewModel>` ใน `Application/Common/MapsterConfig.cs` (~57) (CX-4 #6)
- [ ] [backend] ลบ `IRealtimeNotifier.NotifyChatMessageAsync` (`Application/Realtime/IRealtimeNotifier.cs` ~19) — เก็บ `NotifyNewQuestionAsync` ไว้ (CX-4 #7)
- [ ] [backend] ลบ implementation ของ `NotifyChatMessageAsync` ใน `Api/Realtime/SignalRRealtimeNotifier.cs` (~15-16) (CX-4 #8)
- [ ] [backend] ลบ `Api/Controllers/ChatMessagesController.cs` ทั้งไฟล์ — endpoint ทั้ง 2 เส้นหาย (CX-4 #9)
- [ ] [backend] ลบ `AddScoped<IChatMessageService, ChatMessageService>()` ใน `Api/Configurations/ServiceConfiguration.cs` (~44) (CX-4 #10)
- [ ] [backend] แก้ `Api/Hubs/SessionHub.cs`: ลบ `SendChatMessage`, `SendChatMessageAsAgent`, `JoinSession(token, learnerKey)` — เก็บ `JoinSessionAsAgent`+`EnsureAgentAuthenticated`+`EnsureLearningSessionExists` · ลบ `ResolveLearningSession`/`ResolveLearningSessionId` ที่ไม่มีคนเรียกแล้ว · แก้ XML doc หัวคลาสให้ตรงความจริงใหม่ (CX-3, CX-4 #11)
- [ ] [backend] ลบ `/api/chat-messages` ออกจาก `IsSensitiveLearnerPath()` (`Api/Program.cs` ~191) — coordinate กับ Module G ที่เพิ่ม `/api/text-question` เข้ารายการเดียวกันในคอมมิตเดียวกัน (CX-4 #12)
- [ ] [backend] ลบ `Providers.Data/Repository/IChatMessageRepository.cs` ทั้งไฟล์ (CX-4 #13)
- [ ] [backend] ลบการ register `IChatMessageRepository` ใน `Providers.Data/Data/UnitOfWork/UnitOfWork.cs` (~21) — DI พังตอน runtime ไม่ใช่ตอน compile ถ้าลืม (CX-4 #14)
- [ ] [backend] ลบ `DbSet<ChatMessage>` + บล็อก `builder.Entity<ChatMessage>` ใน `Providers.Data/Data/ApplicationDbContext.cs` (~34, ~116) (DM-7a, CX-4 #15)
- [ ] [backend] แก้ `Application/Services/IAdminService.cs` (~74-79) `ResetDemoData` ให้เลิกลบ `ChatMessage` (ตารางไม่มีแล้ว) (CX-4 #16)
- [ ] [backend] แก้ XML doc comment ใน `Domain/Entities/TrainingLink.cs` (~10) และ `LearningSession.cs` (~11) ที่อ้าง `ChatMessage.SessionId` ให้ตรงความจริงใหม่ (CX-4 #17)
- [ ] [backend] ต่อ migration `RemoveChatMessageAndAddQuestionSource`: เพิ่ม `DropTable("ChatMessage")` พร้อมคอมเมนต์เจตนา (ข้อความคุยกับ CS ถูกตัดออกทั้งฟีเจอร์ตามมติ T4-a, ทิ้งข้อมูลเดิมโดยตั้งใจ ห้าม migrate/archive) และเขียน `Down()` สร้างตารางคืนเฉพาะรูปร่างไม่มีข้อมูล พร้อมคอมเมนต์บอกตรงๆ ว่ากู้ข้อมูลไม่ได้ (MG-R1, CX-4 #18) — **ไฟล์เดียวกับที่ Module G เพิ่ม `Source` ดู Sequencing Notes**
- [ ] [backend] ลบ `backend/tests/.../ChatMessageServiceTests.cs` ทั้งไฟล์ (CX-9)
- [ ] [backend] แก้ `CompanyIsolationTests.cs` (~97-99, 139, 152, 184) ลบ seed/assertion ของ `ChatMessage` — **ห้ามแก้ `EveryEntityIsCompanyScoped` ให้ผ่านเทียม** (tripwire, CX-9)
- [ ] [backend] แก้ `AdminServiceTests.cs` (~20, 30, 78) ลบ fake repo + seed ของ `ChatMessage` (CX-9)
- [ ] [backend] แก้ `Fakes/ServiceTestFakes.cs` (~353-365, ~418, ~432-434) ลบ `FakeChatMessageRepository` + `ChatMessageCount`/`NotifyChatMessageAsync` ใน fake notifier (CX-9)
- [ ] [backend] แก้คอมเมนต์ใน `SessionQuestionServiceTests.cs` (~29) ที่อ้าง `ChatMessageServiceTests` ที่กำลังถูกลบ (CX-9)
- [ ] [backend] `grep -ri "chatmessage\|sendchatmessage\|chat-messages"` บน `backend/src`, `backend/tests` ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง (เกณฑ์ปิดงาน CX-1)
- [ ] [backend] อัปเดตเอกสาร: `docs/schema.dbml` (ลบ `Table ChatMessage` + `Ref`), `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` (~156), `backend/docs/WORKFLOW.drawio` (~73 มีโน้ต "พิมพ์แชทสำรอง"), `docs/PROJECT_CONTEXT.md`, `docs/TECH_DECISIONS.md` (บันทึกการตัดฟีเจอร์เป็น TD ใหม่) — **ห้ามแก้ `docs/CORE_FEATURE_SPEC.md`** (CX-8)
- [ ] [frontend] ลบ `hooks/use-session-chat.ts` ทั้งไฟล์ — ผู้เรียนไม่มีเหตุผลเหลือให้ต่อ SignalR หลังแชตหายไป (CX-3, CX-5 #19)
- [ ] [frontend] เขียนใหม่ `hooks/use-agent-session-chat.ts` เป็น `hooks/use-agent-session-questions.ts` — เหลือเฉพาะ `JoinSessionAsAgent`+`ReceiveNewQuestion`+`liveQuestions` ตัด `chatMessages`/`sendChatMessage`/`getChatMessagesByLearningSession` — **ห้ามลบไฟล์นี้ทิ้ง**, `admin/learning-sessions/[id]/page.tsx` ยังต้องได้ `liveQuestions`/`mergeQuestions` เหมือนเดิม (CX-2, CX-5 #20)
- [ ] [frontend] ลบ `components/meeting/ChatDrawer.tsx` ทั้งไฟล์ — แทนด้วย component ใหม่ที่ Module I สร้าง (CX-5 #21)
- [ ] [frontend] ลบ `getOwnChatMessages`/`getChatMessagesByLearningSession` (`lib/api-client.ts` ~414-426) และ import `ChatMessage` (CX-5 #22)
- [ ] [frontend] ลบ type `ChatMessage`/`ChatSenderRole` ใน `types/domain.ts` (~294-302) + แก้คอมเมนต์บรรทัด ~115 ที่อ้าง `ChatMessage.sessionId` (CX-5 #23)
- [ ] [frontend] แก้ `app/room/[token]/page.tsx`: ลบ `useSessionChat`, `chat.*`, prop `chatMessages`/`onSendMessage` — การต่อ component ใหม่/`onSubmitQuestion` เป็นงานของ Module I (CX-5 #24)
- [ ] [frontend] แก้ `components/meeting/ControlBar.tsx`: เปลี่ยนชื่อ prop `onToggleChat` และ `title`/`aria-label` เลิกใช้คำว่า "แชต" (เช่น `"ถาม-ตอบกับ AI"`) (CX-5 #25, CX-7)
- [ ] [frontend] แก้ `app/admin/learning-sessions/[id]/page.tsx`: ลบปุ่ม "แชท" + `chat.chatMessages` + `onSendMessage` — เหลือรายการคำถามอย่างเดียว (CX-5 #26)
- [ ] [frontend] ยืนยันด้วยมือว่าหน้า `admin/learning-sessions/[id]` ยังได้คำถามสดแบบ realtime ผ่าน `use-agent-session-questions.ts` ใหม่จริง — ไม่ใช่แค่ดูว่าไฟล์หายไปแล้ว (R13)
- [ ] [frontend] `grep -ri "ChatDrawer\|use-session-chat\|use-agent-session-chat"` บน `frontend/src` ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง (เกณฑ์ปิดงาน CX-1)
- [ ] [frontend] อัปเดตเอกสาร: `frontend/docs/ER_DIAGRAM.md`, `frontend/docs/API_CONTRACT.md` (ลบ 2 endpoint + เพิ่ม `/api/text-question` + hub methods ที่เปลี่ยน), `frontend/docs/SYSTEM_LOGIC.md`, `frontend/docs/SEQUENCE_DIAGRAMS.md`, `frontend/docs/USE_CASE_DIAGRAM.md`, `frontend/docs/DATA_FLOW_DIAGRAM.md`, `frontend/docs/SYSTEM_ARCHITECTURE.md` (CX-8)

## Phase 9: Module I — Learner responsive & single-input room UI 🔒 Security gate

RS-1..RS-14 ทั้งชุด (รวม `/session-ended/[token]` + `/link-expired` ตามมติ U4 — เฉพาะ
`min-h-[100dvh]` + hit target ห้าม redesign) · `room/layout.tsx` + `join/layout.tsx` (ใหม่) ·
`AskAiDrawer` ใหม่ (CX-6) · reducer `SUBMIT_TEXT_QUESTION` + `NOT_READY` (TQ-14/TQ-18) · effect
runner + `askTextQuestion` ใน api-client · ปุ่มพร้อม/ยังไม่พร้อม · matrix TQ-20 · **ถอด
readiness-by-voice ฝั่ง frontend ทั้งชุดตามมติ U1 (TQ-24/TQ-25/TQ-26)**

**Sensitive — เหตุผล gate**: เขียน `app/room/[token]/page.tsx` ใหม่ ซึ่งเป็นจุดเดียวในระบบที่บังคับ
IC-7 ได้ (กันไม่ให้เปิด `/room` ตรงๆ แล้ว resume ของคนอื่น ผ่าน `consumeRoomEntry`/`peekLearnerKey`)
— server แยกไม่ออกว่าผ่านหน้ายืนยันมาหรือยัง ถ้า guard นี้หายไประหว่างจัด layout ใหม่ คนที่สองบน
เครื่องที่ใช้ร่วมกันเห็นความคืบหน้า/คำถามของคนแรกโดยไม่มี error ให้เห็น (เหตุผลเดียวกับ Module E)

**Dependencies**: **Phase 7 (G) และ Phase 8 (H) ต้องเสร็จสมบูรณ์ทั้งคู่ก่อน** — I เรียก endpoint
ของ G และเขียนทับไฟล์ที่ H รื้อ ห้ามเริ่มครึ่งทาง

**⚠️ Regression surface**: งาน U1 เขียนทับพฤติกรรมของ Module E ที่ผ่าน QA FULL-3 แล้ว (หน้าจอ
`ready` + push-to-talk + การกลับเข้าบทเรียนหลังถูกขัดจังหวะ) — ต้อง re-verify ด้วยมือทั้งชุด (R19/R20)

- [ ] [frontend] สร้าง `frontend/src/app/room/layout.tsx` server component พร้อม `export const viewport: Viewport = { width: "device-width", initialScale: 1, interactiveWidget: "resizes-content" }` — ห้ามใส่ `maximumScale`/`userScalable: false` (RS-3)
- [ ] [frontend] สร้าง `frontend/src/app/join/layout.tsx` เหมือนกัน (RS-3)
- [ ] [frontend] `room/[token]/page.tsx`: เปลี่ยน `h-screen`(บรรทัด ~173) → `h-[100dvh]`, `md:flex-row`→`lg:flex-row`, `md:w-72 md:flex-col`→`lg:w-72 lg:flex-col`, เพิ่ม `relative` ให้ container, เพิ่ม `pb-[env(safe-area-inset-bottom)]` แถบล่าง (RS-2, RS-4)
- [ ] [frontend] ยืนยันว่า `ControlBar` ยังเป็น sibling ของ element ที่ scroll ไม่ใช่ลูก — ห้ามย้ายเข้าไปในพื้นที่ scroll ตอนจัด layout ใหม่ (RS-4)
- [ ] [frontend] เขียนใหม่ `PushToTalkButton.tsx` ด้วย Pointer Events (`onPointerDown`/`onPointerUp`/`onPointerCancel` แทน mouse/touch handlers ทั้งหมด), `touch-none`, `e.currentTarget.setPointerCapture(e.pointerId)`, `onContextMenu` preventDefault + `select-none` + `[-webkit-touch-callout:none]`, `pointercancel` ปล่อยการอัดเสมอ — ห้ามเปลี่ยนเป็น toggle, ห้ามแตะ `MIN_RECORDING_MS`/`MinVoiceDurationMs`, คงปุ่มสูง ≥44px (RS-5)
- [ ] [frontend] `SlidesEmbed.tsx`: เปลี่ยน overlay `<div aria-hidden>` (~89) เป็น `<button aria-label="ขยายสไลด์เต็มจอ">` ที่กินคลิกจริง ผูก handler ที่ overlay นี้ (ห้ามผูกบน `<iframe>` — cross-origin จับ event ไม่ได้), ใช้ overlay ในแอป (`fixed inset-0 z-50`) แทน Fullscreen API, ห้าม unmount/เปลี่ยน `key` ของ iframe ตอนสลับ fullscreen, ปิดได้ด้วยปุ่มปิด + แตะพื้นหลัง (RS-6)
- [ ] [frontend] ยืนยัน/เลือกว่าปุ่มจบ/ปุ่มพูดยังกดได้ระหว่างเปิด fullscreen หรือปิด fullscreen อัตโนมัติเมื่อ AI เริ่มพูดตอบ — เลือกแบบใดแบบหนึ่งพร้อมคอมเมนต์อธิบายเหตุผล ห้ามมีสถานะที่ผู้เรียนกดจบไม่ได้ (RS-6, R3c)
- [ ] [frontend] `ControlBar.tsx`: จัดลำดับความสำคัญปุ่มบน compact เมื่อพื้นที่ไม่พอ (`ปุ่มพูด > ปุ่มจบ > ปุ่มเปิด drawer > ปุ่มปรับเสียง`), ทุกปุ่มขั้นต่ำ 44×44px, เผื่อ safe area (RS-9)
- [ ] [frontend] `AiTile.tsx`/`ParticipantTile.tsx`: ย่อเป็นแถวเตี้ยแนวนอนเหนือ `ControlBar` หรือย่อเหลือ `AiTile` อย่างเดียวบน compact — ห้ามสร้าง "รายการสไลด์" ใหม่ (RS-10)
- [ ] [frontend] `VolumeControl.tsx`: `PopoverContent w-52` ต้องไม่ล้นจอแคบ, trigger ≥44px (RS-9)
- [ ] [frontend] `join/[token]/page.tsx`: `min-h-screen`→`min-h-[100dvh]`, ปุ่มในหน้ายืนยัน (LR-3a) ≥44px (RS-4)
- [ ] [frontend] `session-ended/[token]/page.tsx`/`link-expired/page.tsx`: `min-h-screen`→`min-h-[100dvh]` + hit target ≥44px เท่านั้น — **ห้ามเพิ่ม component ใหม่ ห้ามจัด layout ใหม่ ห้ามเปลี่ยนข้อความ** (RS-1, RS-4, U4)
- [ ] [frontend] ยืนยันไม่มี code path ใดตรวจ `orientation` แล้วบล็อกหน้าจอ (หน้า "กรุณาหมุนจอ", CSS `@media (orientation: portrait)` ที่ซ่อนเนื้อหา, `screen.orientation.lock()`) — ทุก interaction ทำได้ครบในแนวตั้ง (RS-11)
- [ ] [frontend] สร้าง `components/meeting/AskAiDrawer.tsx` ใหม่ (ห้ามใช้คำว่า Chat ในชื่อไฟล์/component/prop) — compact: `fixed inset-0 h-[100dvh]` ไม่ใช่ bottom sheet, regular: `lg:absolute lg:right-4 lg:bottom-20 lg:w-80` พร้อมใส่ `relative` ให้ container ของห้อง, props `{ open, onClose, questions, onSubmitQuestion, inputEnabled, sendEnabled, disabledHint? }` (component ไม่ตัดสิน enabled เอง — มาจาก TQ-20), ยกโครง timeline/`createdAt`/`transcript`+`answer`+`answerStatusLabels`/Enter=ส่ง/คง draft จาก `ChatDrawer` เดิม, ตัด `chatMessages`/`TimelineEntry` สองชนิด/`senderLabel`/`kind:"chat"` ทุกจุด (CX-6)
- [ ] [frontend] input ของ drawer: `sticky bottom-0` + `pb-[env(safe-area-inset-bottom)]`, เลื่อนรายการไปท้ายสุดเมื่อ input โฟกัส, `font-size` ≥16px (`text-base` ขึ้นไป ห้าม `text-sm`), ปุ่ม "ส่ง" มองเห็น/กดได้เสมอบน compact ห้ามพึ่ง Enter อย่างเดียว (RS-8)
- [ ] [frontend] หัว drawer เปลี่ยนข้อความ "แชตสำรอง" → เช่น `"ถาม-ตอบกับผู้ช่วย AI"` — ห้ามมีข้อความใดในห้องเรียนสื่อว่าจะมีเจ้าหน้าที่มาตอบ (CX-7)
- [ ] [frontend] เพิ่ม `askTextQuestion({ token, learnerKey, text, currentSlideObjectId? })` ใน `lib/api-client.ts` เรียกผ่าน `publicRequest` (JSON) — `askVoiceQuestion` เดิมไม่เปลี่ยน signature (TQ-13)
- [ ] [frontend] เพิ่ม TS type `QuestionSource = "voice" | "text"` และ `SessionQuestion.source: QuestionSource` ใน `types/domain.ts` — `LearnerQuestion` ไม่มี `source` (U2 wire delta)
- [ ] [frontend] ลบ `expecting?: "question" | "readiness"` ออกจาก input type (`lib/api-client.ts:457-458`) และ `if (input.expecting) formData.append("expecting", ...)` (`:465-466`) (TQ-26)
- [ ] [frontend] ลบ `readiness?: "ready" | "not_ready"` + คอมเมนต์เหนือมัน (`types/domain.ts:265-266`) (TQ-26)
- [ ] [frontend] เพิ่ม event `{ type: "SUBMIT_TEXT_QUESTION"; text: string }` ใน `tutor/intents.ts` และ effect `{ kind: "SEND_TEXT_QUESTION"; text: string }` ใน `tutor/types.ts` (TQ-14)
- [ ] [frontend] `tutorReducer`: รับ `SUBMIT_TEXT_QUESTION` เฉพาะ `runtime.state` ∈ `["slide-speaking","waiting-slide-duration","final-question-window"]` → `{ ...runtime, interruptedFrom: runtime.state, state: "processing-question", isAiSpeaking: false, afterSpeech: null, micNotice: null }` + effect `SEND_TEXT_QUESTION` · state อื่น → `noEffect(runtime)` · ห้ามผ่าน `"push-to-talk-recording"` (TQ-14)
- [ ] [frontend] เพิ่ม event `{ type: "NOT_READY" }` รับเฉพาะ `state === "ready"` ให้ผลเท่ากับ `READINESS_ANSWERED{ready:false}` เดิมทุกประการ (พูด `notReadyScript` แล้ว `afterSpeech: "AWAIT_READINESS"` — ไม่มี timer auto-start) — ห้ามให้ state อื่นส่ง `NOT_READY` ได้ (TQ-18)
- [ ] [frontend] ลบ `"ready"` ออกจาก `PUSH_TO_TALK_STATES` (`tutor-reducer.ts:59-66`) พร้อมคอมเมนต์บรรทัด 59-60 ที่อธิบายพฤติกรรมเดิม — **ห้ามแตะ `PAUSABLE_STATES` (บรรทัด 67)** (TQ-24)
- [ ] [frontend] ลบ `"ready"` ออกจาก `PUSH_TO_TALK_ENABLED_STATES` (`room/[token]/page.tsx:111-117`) พร้อมคอมเมนต์บรรทัด 111 — **ลิสต์ใบที่สอง แยกจากรายการใน reducer อย่าลบใบเดียว** (TQ-24)
- [ ] [frontend] `room/[token]/page.tsx:195`: ลบบรรทัด `หรือกดปุ่ม "กดค้างเพื่อพูด" แล้วบอกว่าพร้อมแล้วก็ได้ค่ะ` และวางปุ่ม **"ยังไม่พร้อม"** ไว้ที่ overlay เดียวกันกับปุ่ม "พร้อมแล้ว เริ่มเรียนเลย" (บรรทัด ~192-197) (TQ-18, TQ-24)
- [ ] [frontend] ลบ branch `if (runtime.interruptedFrom === "ready")` ใน `resumeAfterInterruption` (`tutor-reducer.ts:120-131`) — dead code เพราะไม่มี event ใดตั้ง `interruptedFrom` เป็น `"ready"` แล้ว — **type ของ `interruptedFrom` ยังเป็น `TutorState | null` ห้ามทำให้แคบลง** (TQ-24)
- [ ] [frontend] ลบ event `READINESS_ANSWERED` (`tutor/intents.ts:36` + `tutor-reducer.ts:282-292`) (TQ-25)
- [ ] [frontend] ลบ `AfterSpeechAction "START_FIRST_SLIDE"` (`tutor/types.ts:32`) และ case ใน `TTS_ENDED` (`tutor-reducer.ts:180-181`) — **ห้ามลบ `startFirstSlide()` (~92-95) เพราะ `START`/`INTRO_TIMEOUT` ยังเรียกอยู่** (TQ-25)
- [ ] [frontend] **ห้ามลบ** `AfterSpeechAction "AWAIT_READINESS"` (`tutor/types.ts:34` + case `tutor-reducer.ts:182-186`) — `NOT_READY` เป็นผู้ผลิตรายเดียวที่เหลือ (TQ-25)
- [ ] [frontend] ลบ `readyConfirmScript` (`tutor/scripts.ts:11`) — **ห้ามเพิ่มเสียงตอบรับใหม่ให้ปุ่ม `START` เพื่อ "ชดเชย"** (TQ-25)
- [ ] [frontend] **ห้ามลบ** `notReadyScript` (`tutor/scripts.ts:12`) แต่ต้องแก้ข้อความให้ชี้ปุ่มจริง (เช่น `"ได้ค่ะ ไม่ต้องรีบนะคะ พร้อมเมื่อไหร่กดปุ่มพร้อมแล้วได้เลยค่ะ"`) และแก้คอมเมนต์บรรทัด 9-10 ที่อธิบาย "ตอบด้วยเสียง" ให้ตรงความจริงใหม่ — ถ้อยคำต้องตรงกับ label ปุ่มจริงในหน้าห้อง (TQ-25)
- [ ] [frontend] เพิ่ม matrix ช่องพิมพ์/ปุ่มส่งตาม TQ-20 ครบทุก `runtime.state` รวม `ready` (ช่องพิมพ์ disabled + placeholder อธิบาย เช่น `"เลือกพร้อม/ยังไม่พร้อมด้านบนก่อนนะคะ"` — ห้ามซ่อนหายไปเฉยๆ) (TQ-18, TQ-20)
- [ ] [frontend] effect runner ใน `use-tutor-session.ts` เพิ่ม `case "SEND_TEXT_QUESTION": void sendTextQuestion(effect.text);` — `sendTextQuestion` เรียก `playProcessingFiller()` → `api.askTextQuestion({ token, learnerKey, text, currentSlideObjectId })` → `answerStatus` ∈ `answered|not_found|out_of_scope` → `QUESTION_ANSWERED` · `catch` → `QUESTION_FAILED` — **ไม่มีการเช็ค `result.readiness` และไม่ map เป็น `NO_SPEECH`** (TQ-16)
- [ ] [frontend] ยืนยันว่าช่องพิมพ์ **ไม่มี** `onFocus`/`onChange`/`onKeyDown` หรือ handler ใดที่ `sendEvent(...)` เข้า reducer และการเปิด/ปิด drawer ไม่ส่ง event เข้า reducer — การหยุดบรรยายเกิดที่ "กดส่ง" เท่านั้น (TQ-15, RS-7)
- [ ] [frontend] ลบ `const expecting = runtimeRef.current.interruptedFrom === "ready" ? "readiness" : "question"` (`use-tutor-session.ts:346`), เลิกส่ง `expecting` เข้า `askVoiceQuestion` (`:358`), ลบบล็อก `if (result.readiness) { dispatch({ type: "READINESS_ANSWERED", ... }); }` ทั้งก้อน (`:363-364`) — ตรวจว่า `QUESTION_ANSWERED`/`QUESTION_FAILED` ยังครอบทุก path ของ `stopRecordingAndSend()` (TQ-26)
- [ ] [frontend] เขียน/แก้ tutor reducer tests ตาม TQ-21: `SUBMIT_TEXT_QUESTION` จาก 3 state ที่อนุญาต → `processing-question`+`interruptedFrom` ถูกตั้ง · จาก `"ready"`/`"processing-question"` → ไม่เกิดอะไรเลย · `NOT_READY` จาก `"ready"` → ไม่มี `WAIT_READY_TIMEOUT` ตามมา · **`PUSH_TO_TALK_START` จาก `"ready"` → ไม่เกิดอะไรเลย** (เคสใหม่) · ลบ/เขียนใหม่ describe block `"answering the readiness prompt by voice"` (`tutor-reducer.test.ts:141-190`) เป็นเคสของ `NOT_READY` (TQ-21, TQ-26)
- [ ] [frontend] `grep -ri "readiness\|expecting" frontend/src` ต้องไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง (เกณฑ์ปิดงาน TQ-24..TQ-26)
- [ ] [frontend] อัปเดตเอกสาร: `frontend/docs/STATE_MACHINE.md` (ลบ `READINESS_ANSWERED` ออกจากแผนภาพ เพิ่ม `SUBMIT_TEXT_QUESTION`/`NOT_READY`), `frontend/docs/API_CONTRACT.md`, `frontend/docs/SYSTEM_LOGIC.md`, `frontend/docs/SEQUENCE_DIAGRAMS.md`, `frontend/docs/GEMINI_INTEGRATION.md`, `frontend/docs/TESTING_GUIDE.md`, `docs/PROJECT_CONTEXT.md`, `docs/UX_UI_WORKFLOWS.md`, `docs/SOLUTION_ARCHITECTURE.md`, `docs/BACKEND_DB_HANDOFF.md`, `docs/PROVIDER_SETTINGS_SPEC.md` (TQ-27)
- [ ] [frontend] ทดสอบด้วยมือบนอุปกรณ์/emulator จริงตามเกณฑ์ RS-14 ข้อ 1-7 (กดค้างไม่เลื่อนหน้า/ไม่มี context menu · ลากนิ้วออกนอกปุ่มแล้วปล่อยหยุดจริง · เห็นปุ่มจบไม่ต้องเลื่อนทั้งตอน URL bar กาง/หุบ · แตะสไลด์เต็มจอไม่รีโหลด · โฟกัสช่องพิมพ์เห็นทั้งช่อง+ปุ่มส่งไม่ซูมเข้าเอง · หมุนจอไปมาไม่มีหน้าบังคับหมุน) ทั้งบทเรียน Google Slides และ PDF — ถ้าไม่ได้ทดสอบด้วยอุปกรณ์จริง ต้องลงรายการที่ไม่ได้ทดสอบใน `## Unverified Behaviour — undeployed phases` (RS-14, R18)
- [ ] [frontend] re-verify IC-7 ทั้งชุดด้วยมือหลังเขียน `room/[token]/page.tsx` ใหม่ (`entryGrantedRef`/`consumeRoomEntry`/`peekLearnerKey`/redirect กลับ `/join`) รวม React Strict Mode — ไม่ใช่ถือว่าผ่านแล้วจาก FULL-3 (R17)
- [ ] [frontend] re-verify ด้วยมือ: กดปุ่มพร้อม → บทเรียนเริ่มที่สไลด์ resume ถูกจุด · กด "ยังไม่พร้อม" → พูด `notReadyScript` แล้วกลับสู่ `ready` โดยไม่มี auto-start · กดปุ่มพูดตอน `ready` → ไม่เกิดอะไรเลย · กดพูดถามกลางบทเรียนยังทำงานครบเหมือนเดิม (Regression surface, R19)

## Sequencing Notes

- **ลำดับ dependency**: A → (B, C) → D → (E, F) ตามที่ `design.md` §Modules ระบุ — Phase 1 (A) ต้อง
  เสร็จสมบูรณ์ก่อนทุก phase อื่น · backend inspect/dry-run migrations 2 ใบและส่งหลักฐานก่อน;
  **`devops` เท่านั้นที่ apply กับ environment จริงหลังผู้ใช้อนุมัติเฉพาะเจาะจง** · Phase 4 (D)
  ต้องเสร็จก่อน Phase 5 (E) และ Phase 6 (F) เพราะทั้งคู่ต้องใช้ chat/คำถามที่ผูกกับการเรียน
  ที่ถูกต้องแล้ว (ไม่ใช่ผูกกับลิงก์)
- **Phase 3 (C), 4 (D), 5 (E), 6 (F) ติด 🔒 Security gate** ตามที่ `design.md` วิเคราะห์ไว้:
  - **Phase 3 (C)** — รับ input จากภายนอกที่ไม่ผ่าน auth; คู่ `(TrainingLink.Token, LearnerKey)`
    เป็น composite bearer credential ตาม CA-3 · ต้องตรวจ isolation, 404-on-mismatch,
    no-log/no-cache/no-analytics และ HTTPS-only โดยไม่รับ public `learningSessionId`
  - **Phase 4 (D)** — จุดรั่วข้ามผู้เรียนอันดับหนึ่งของทั้งโมดูลตาม `design.md` (R1/R2) — SignalR
    group key และ voice-question ต้อง**ทดสอบด้วยมือด้วย browser 2 ตัว** ไม่ใช่แค่อ่านโค้ด
  - **Phase 5 (E)** — จุดบังคับ LR-3a/IC-7 เพียงจุดเดียวในระบบ server แยกไม่ออกว่า resume ผ่านการ
    ยืนยันมาหรือยัง ถ้าหน้ายืนยันหาย/บั๊ก คนที่สองบนเครื่องที่ใช้ร่วมกันเห็นความคืบหน้า+คำถามของคนแรก
    แบบเงียบ ไม่มี error ให้เห็น — **ไม่ใช่ด้วยเหตุผล PII** (F2 เก็บแค่ชื่อ ไม่เก็บ PII อื่น ห้ามอ้าง
    PII เป็นเหตุผลของ gate นี้)
  - **Phase 6 (F)** — หมายเหตุรีวิวเป็นข้อมูลภายในของ CS แต่ `/admin/*`/`/api/*` ยังเปิดสาธารณะบางส่วน
    (TD-002 — แม้ `AdminUser`/`AdminRole`/`IAuthorizationGuard` จะมีแล้วตามที่พบระหว่างตรวจโค้ดโมดูล
    `knowledge-base` คู่ขนาน ก็ยังต้องยืนยันว่าครอบ endpoint ของโมดูลนี้ครบก่อนถือว่า gate ปิดได้)
- **Phase 1 (A) และ Phase 2 (B) ไม่ติด 🔒 gate** — A เป็นงาน data layer ล้วน ไม่มี input จากภายนอกที่
  ไม่ผ่าน auth · B เป็นฟอร์มฝั่ง CS หลังบ้านที่ auth ครอบอยู่แล้วตามแบบแผนเดิม
- **Engineer และ QA ต้องใช้ CA-1..CA-5 ที่หัวเอกสารนี้เป็น authority**; baseline DM/API/IC text
  ใน `design.md` ที่ขัดกันเป็น design history ไม่ใช่เหตุให้ mechanical rename หรือเปลี่ยน route กลับ
- **QA รอบถัดไปใช้ FULL-1 manifest ใน `review.md` เพื่อ re-verify fixes**; mode เป็นการตัดสินของ
  `qa-engineer` ตาม workflow ห้าม PM เปลี่ยนผล/checkbox เดิม
- **`security` รอ functional fixes และ QA re-verify ก่อน**; Phase 3–6 ยังติด gate เต็มรูปแบบ

### เพิ่ม 2026-08-23 — Phase 7 (G) / 8 (H) / 9 (I)

- **ลำดับ dependency ของรอบนี้**: Phase 7 (G) และ Phase 8 (H) เป็นอิสระต่อกัน — ทำขนานกันได้ ·
  **Phase 9 (I) ต้องรอทั้ง G และ H เสร็จสมบูรณ์ก่อนเริ่ม** เพราะ I เรียก endpoint ของ G
  (`/api/text-question`) และเขียนทับไฟล์ที่ H รื้อ (`room/[token]/page.tsx`, drawer) — **ห้ามส่งมอบ
  G หรือ H ครึ่งทางแล้วเริ่ม I**: ระหว่างนั้นห้องเรียนจะอยู่ในสถานะที่แชตหายแล้วแต่ยังพิมพ์ถาม AI
  ไม่ได้ ซึ่งแย่กว่าสถานะก่อนเริ่มงาน
- **Phase 7 และ Phase 8 เขียนไฟล์ migration เดียวกัน**: `RemoveChatMessageAndAddQuestionSource`
  ต้องเป็นใบเดียว ห้ามแยกสองใบ (MG-R1) แต่ G เพิ่มส่วน `AddColumn Source` และ H เพิ่มส่วน
  `DropTable ChatMessage` เข้าไฟล์เดียวกัน — **ผู้ที่เริ่มงานก่อนเป็นคน `dotnet ef migrations add`
  สร้างไฟล์ครั้งแรก อีกฝ่ายแก้ไฟล์เดิมเพิ่มส่วนของตัวเองต่อ ห้ามสร้างไฟล์ migration คนละใบ** —
  ต้องคุยกันระหว่างสองงานแม้จะ "ขนานกันได้" ในแง่ dependency ของโค้ด
- **Phase 7 และ Phase 8 แก้ `Program.cs` `IsSensitiveLearnerPath()` รายการเดียวกัน**: G เพิ่ม
  `/api/text-question` เข้ารายการ ส่วน H ลบ `/api/chat-messages` ออกจากรายการเดียวกัน (TQ-2, CX-4 #12)
  — ต้องประสานให้ทั้งสองการแก้ไปอยู่ใน merge เดียวกันไม่ทับกันหาย
- **`POST /api/voice-question` เป็น wire contract ที่คร่อมสองฝั่ง**: งานถอด readiness-by-voice
  (มติ U1) ฝั่ง backend อยู่ที่ Phase 7 (TQ-22/TQ-23) ฝั่ง frontend อยู่ที่ Phase 9 (TQ-24..TQ-26) —
  **ห้าม deploy สองก้อนนี้แยกช่วงเวลากัน** เพราะช่วงกลางคือ frontend ที่ยังส่ง `expecting` และรอ
  `readiness` จาก backend ที่เลิกผลิตแล้ว (R19) — ต้อง deploy Phase 7 กับ Phase 9 พร้อมกันสำหรับส่วน
  ที่แชร์ contract นี้ แม้ Phase 9 จะเริ่มทำทีหลังก็ตาม
- **Phase 7 (G), Phase 8 (H), Phase 9 (I) ทั้งสามติด `🔒 Security gate`** ตามเหตุผลที่ระบุไว้ใน
  หัวข้อ phase ของแต่ละอัน (คัดลอกจาก `design.md` §Modules ตรงๆ): G = endpoint anonymous ใหม่ +
  prompt injection + ไม่มี rate limiting + แตะ `KnowledgeNamespaces` · H = `DropTable` จริงพร้อม
  ข้อมูล + แตะ `SessionHub`/`IsSensitiveLearnerPath` ซึ่งเป็นจุดกั้นข้อมูลข้ามผู้เรียน · I = เขียน
  `room/[token]/page.tsx` ใหม่ซึ่งเป็นจุดเดียวที่บังคับ IC-7 ได้
- **`qa-engineer` ต้องถือรอบที่ verify Phase 7 และ Phase 9 เป็น FULL และ re-verify Module C/D/E
  ตามรายการ Regression surface ของทั้งสอง phase** — ไม่ใช่เชื่อผล FULL-3 เดิม (R19/R20): ทดสอบ
  ถามด้วยเสียงทั้งเส้น (Phase 7), ปุ่มพร้อม/ยังไม่พร้อมทำงานถูก + กดปุ่มพูดตอน `ready` ต้องไม่มี
  อะไรเกิดขึ้น + IC-7 ทั้งชุด (Phase 9)
- **RS-14 ไม่มี automated test จับได้** (R18) — ถ้า QA ไม่ทดสอบด้วยอุปกรณ์/emulator จริง ต้องลง
  ทั้ง 7 ข้อของ RS-14 ใน `## Unverified Behaviour — undeployed phases` ให้ `devops` เอาไปให้
  เจ้าของโปรเจกต์ดูก่อน deploy
- **U1 ไม่มี schema change และห้ามมี migration ใบที่ 4** — ของที่ถูกถอด (`Expecting`/`Readiness`/
  prompt/event/script) ไม่มีอะไรลงตาราง เพราะ `IVoiceQuestionService` early-return ก่อนเขียนแถวเสมอ

## Unresolved Open Questions

ไม่มีคำถามธุรกิจหรือ data/wire contract ค้างที่บล็อก Phase 1–9 — CA-1..CA-6 ปิด `LS-QA-02` แล้ว
และ U1–U4 เคาะครบแล้วเมื่อ 2026-08-23 · R15 (rate limiting บน `/api/text-question`) เป็น open
question ที่บันทึกไว้ให้เจ้าของโปรเจกต์ตัดสินแยก **ไม่บล็อก Phase 7** งานที่เหลือเป็น
implementation, environment/manual verification และ Security gate ตาม `review.md`

## Change Log

- 2026-08-23 — Amend: เพิ่ม Phase 7 (Module G — typed questions backend + provider), Phase 8
  (Module H — chat feature removal ทั้ง stack + migration), Phase 9 (Module I — learner
  responsive + single-input room UI) จาก amendment ของ `design.md` วันเดียวกัน (F9/F10/F10-a +
  มติ U1–U4) · ทุก task ใหม่เป็น `[ ]` ทั้งหมด ไม่แตะ checkbox เดิมของ Phase 1–6 แม้แต่บรรทัดเดียว ·
  Phase 7/8 ทำขนานกันได้ตาม `design.md` §Risks & Dependencies แต่ทั้งคู่ต้องเสร็จก่อน Phase 9 เริ่ม ·
  บันทึกจุดประสานงานที่ไม่ชัดจากการอ่าน `design.md` อย่างเดียว 3 จุดไว้ใน Sequencing Notes: (1) G/H
  เขียน migration `RemoveChatMessageAndAddQuestionSource` ไฟล์เดียวกันคนละส่วน (2) G/H แก้
  `IsSensitiveLearnerPath()` รายการเดียวกันคนละบรรทัด (3) readiness-by-voice removal คร่อม
  wire contract ระหว่าง Phase 7 (backend) และ Phase 9 (frontend) ต้อง deploy พร้อมกัน · ติด
  `🔒 Security gate` ทั้ง Phase 7/8/9 ตามเหตุผลใน `design.md` §Modules G/H/I · เพิ่มงาน re-verify
  Module C/D/E (R19/R20) เป็น task ท้าย Phase 7 และ Phase 9 ตามที่ `design.md` สั่งไว้ตรงๆ ·
  ปรับ Plan Summary และ Unresolved Open Questions ให้ครอบ 9 phase และบันทึกว่า R15 ไม่บล็อก
- 2026-08-19 — Amend หลัง `system-analyst` ปิด `LS-QA-02`: เปลี่ยน naming/API/repository/audit/
  migration tasks ให้ตรง CA-1..CA-5 โดยไม่แก้ checkbox จาก FULL-1 · ยอมรับ `TrainingLink`,
  `RecipientName`, child `SessionId`, `SessionStatus`/`LinkStatus` และ server-side resolution จาก
  `(token, learnerKey)` · เพิ่ม task ตรวจ composite bearer credential logging/cache/404/HTTPS และ
  CS authenticated company authorization · ปรับ handoff จาก FULL QA รอบแรกเป็น
  `backend-engineer`/`frontend-engineer` แก้ LS-QA-03/04/06/07 ก่อน re-verify; migration environment,
  manual checks และ Security gate ยังไม่ปิด
- 2026-08-19 — สร้างแผนครั้งแรก จาก `design.md` (ยืนยัน 2026-08-18) ผสมกับผลตรวจโค้ดจริงที่พบว่า
  งานส่วนใหญ่ของ Module A–F ถูก implement ไปแล้วนอก pipeline ก่อนมี `plan.md` ฉบับนี้ · ยึดชื่อ
  entity `TrainingLink` ตามโค้ดจริง (เพิกถอนมติ Q2 เดิมของ `design.md` ที่จะ rename เป็น `LessonLink`
  ตามที่เจ้าของโปรเจกต์แจ้ง) · ทุก checkbox ปล่อยเป็น `[ ]` ตามกฎ "มีแต่ `qa-engineer` ติ๊กได้" แม้พบ
  ว่าโค้ดมีอยู่แล้วเกือบทุกจุด · บันทึกตาราง drift ระหว่าง `design.md` contract กับโค้ดจริง (route
  shape, `X-Learner-Key` header vs body field, field naming `RecipientName`/`LearnerName`,
  `SessionId`/`LearningSessionId`, SignalR method signature) ไว้ให้ `qa-engineer` ตรวจต่อ · migration
  2 ใบของ Phase 1 ระบุชัดว่ายังไม่เคย apply กับ DB จริงของ deployment — เป็น blocking item เดียวที่
  ยืนยันได้แน่ชัดว่า "ยังไม่เสร็จ" ไม่ใช่แค่ "รอ verify" · ติด 🔒 Security gate ที่ Phase 3, 4, 5, 6
  (Module C, D, E, F) ตรงตามที่ `design.md` §Modules และ §Risks & Dependencies ระบุไว้
