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

6 phase ตรงกับ Module A–F ของ `design.md` เรียงตาม dependency ที่ `design.md` ระบุไว้:
**A → (B, C) → D → (E, F)** — A ต้องเสร็จก่อนทุกอย่างเพราะเป็น data foundation, D ต้องเสร็จก่อน E
และ F เพราะทั้งคู่ต้องใช้ chat/คำถามที่ผูกกับการเรียนแล้ว โปรเจกต์นี้ scaffold ไว้แล้ว (ASP.NET Core +
Next.js อยู่ก่อนแล้ว) จึงไม่มี Phase 0 ของ `setup`

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

## Unresolved Open Questions

ไม่มีคำถามธุรกิจหรือ data/wire contract ค้าง — CA-1..CA-6 ปิด `LS-QA-02` แล้ว งานที่เหลือเป็น
implementation, environment/manual verification และ Security gate ตาม `review.md`

## Change Log

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
