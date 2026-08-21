# Project Status

## Scaffold
Already scaffolded — this is an existing project (`frontend/` Next.js + `backend/` ASP.NET Core), not a fresh `setup` run. See root `CLAUDE.md` for the real architecture; the pipeline's `setup` agent does not need to run here.

## Modules

| Module | Stage | Next agent |
|---|---|---|
| knowledge-base | **QA FULL (2026-08-20) — Phase 7 verified ✅ (FULL)**, module overall ✅ — 139/140 `plan.md` tasks ✅ ติ๊กแล้วทั้ง 7 phase (เหลือแค่ R-2 latency ที่บล็อกด้วย deployment ไม่ใช่ล้มเหลว), ไม่มีข้อค้าง ❌/⚠️ เหลือเลย. Phase 7 (Document scope assignment, Module G, 🔒 gate) 22/22 ✅ ยืนยันด้วยโค้ดจริง + unit test ใหม่ 14 ตัว (backend 204/204) + ทดสอบจริงกับแอปที่รันอยู่ (curl ยิง 6 กรณีปฏิเสธของ DS-3 ผ่าน JWT จริง, login+`GET /api/companies` จริง) · Phase 1's TX-5 partial ปิดแล้ว (ปิดพร้อม Phase 7 wiring call site) → Phase 1 ตอนนี้ 15/15 ✅ · **บั๊ก 2 (owner บริษัทเดียว login ไม่ได้)** แก้ใน `AdminSessionProvider.tsx` ยืนยันแล้วด้วย code trace + live data (`GET /api/companies` คืนบริษัทเดียวจริงสำหรับ `owner@local.test`) — ไม่มี browser tool ในเซสชันนี้ให้คลิกทดสอบเอง แต่ผู้ใช้ยืนยันด้วยตาตัวเองแล้ว · **บั๊ก 3 (Select ต้องเลือกสองรอบ)** แก้ใน `DocumentUploadList.tsx` + `KnowledgeQnAAnswerDialog.tsx` (ไฟล์ Phase 6 เดิม, regression check ผ่านครบไม่กระทบ flow เดิม) ยืนยันแล้วด้วย diff อ่านโค้ดตรง · Phase 3 ยังเหมือนเดิม (21/21 ✅ แต่รอบล่าสุดเป็น TARGETED ไม่ใช่ FULL ต้องมี FULL ก่อน deploy ได้) · **`security` ยังไม่เคยรันสักครั้งตลอดทั้ง 7 phase** — เป็นตัวบล็อกเดียวที่เหลือสำหรับทุก phase ที่ติด gate (2,3,4,6,7) ก่อนส่ง `devops` · รายละเอียดเต็มใน `review.md` (Phase 1–6 เดิมย้ายไป `review/phase-1-6.md` แล้ว, Phase 3 Round 1 เดิมอยู่ที่ `review/phase-3.md`) | `security` (เมื่อผู้ใช้เรียก), `devops` (Phase 1 พร้อม accept ได้ทันที ไม่ติด gate) |
| company-admin | **Security SECURITY-1 ⚠️ — 3 Important findings open**: stale JWT ยังใช้ได้หลัง deactivate/demotion · `MustChangePassword` bypass ผ่าน API · login ไม่มี rate limiting. QA: Phase 1 15/15 verified ✅ (TARGETED, ต้อง FULL ก่อน devops) · Phase 2 7/7 verified ✅ (FULL) · **Phase 3 (Company Switching — Owner UX) 6/6 verified ✅ (FULL) — implement เสร็จแล้ว, ตรวจโค้ดจริงครบทั้ง 6 task ตรง F4.0–F4.6, `typecheck`/`lint`/`test` (41/41)/`build` ผ่านหมด, auto-accepted (all-✅ FULL round)** · deployed ⬜ ทั้งโมดูล · ทั้งสาม phase ยังติด `🔒 Security gate` ของ Module A — Phase 3 ยังไม่เคยผ่าน `security` เลย (SECURITY-1 คลุมแค่ Phase 1/2) | `backend-engineer` แก้ `SEC-01`–`SEC-03`; จากนั้นรอผู้ใช้เรียก `security` re-audit (Phase 1/2 ที่แก้แล้ว + Phase 3 ที่ยังไม่เคยตรวจ) · `qa-engineer` ต้องรัน FULL รอบใหม่ให้ Phase 1 ก่อน `devops` รับได้ |
| learning-session | **QA FULL-3 + manual-4/5 ✅ ครบ 53/53** — LS-QA-05 ปิดหมดแล้ว (6/6 กรณี LR-3 + R1 isolation ยืนยันด้วย SignalR connection จริง) · Phase 1–2 (A, B) ไม่ติด gate พร้อมให้ `devops` accept ได้เลย · Phase 3–6 ยังรอ `security` audit ก่อน deploy ได้ | `security` (เมื่อผู้ใช้เรียก) สำหรับ Phase 3–6 · `devops` accept Phase 1–2 ได้ทันที |

## 🟡 เรื่องที่ค้างการตัดสินใจ — บันทึกไว้ 2026-08-21 (capture-only ไม่ได้เปลี่ยนสถานะ phase ใด)

> วันที่มาจาก system context **ยังไม่ได้ให้เจ้าของโปรเจกต์ยืนยันเอง** (เซสชันที่บันทึกไม่มี
> เครื่องมือถามผู้ใช้ และเจ้าของโปรเจกต์กำลังย้ายเครื่อง/ย้ายบัญชี) · **ไม่มีโค้ด ไม่มี checkbox
> ไม่มีมติใดถูกเปลี่ยนในรอบนี้** — บันทึกบริบทอย่างเดียวเพื่อให้เซสชันใหม่ที่ไม่มีความจำต่อได้

1. **ค่า pacing ของบทเรียน → ค่าเริ่มต้นระดับบริษัท** — `_docs/module/learning-session/requirement.md`
   §Open Questions (หัวข้อ 2026-08-21) + `_docs/module/company-admin/requirement.md` **OQ-8** ·
   ตรวจโค้ดจริงแล้วว่า `introWaitMs`/`breathPauseMs`/`finalQuestionWaitMs` มีผลจริง ตัดไม่ได้
   ย้ายได้อย่างเดียว · ต้องปิดผ่าน **A5/B3/B4** ซึ่งอยู่ใน **`company-admin/design.md`**
   (ไม่ใช่ `learning-session/design.md` ตามที่เคยเข้าใจกัน) · **ยังไม่เคยสัมภาษณ์ P1–P4**
   → รอ `business-analyst` · แยกต่างหาก: `videoDurationMs` เป็นงานลดความรกของ UI ล้วน
   ส่ง `frontend-engineer` ได้ตรงๆ ไม่ต้องรออะไร
2. **CR-1 ยุบ knowledge scope บทเรียน/บริษัทให้เหลือกองเดียว** —
   `_docs/module/knowledge-base/requirement.md` §Open Change Request · **ขัดกับมติข้อ 5 ใน
   `docs/HANDOFF_MASTER.md` ที่ประกาศห้ามเปลี่ยนเงียบๆ** (ใส่ตัวชี้ไว้ที่นั่นแล้ว โดยไม่แก้ตัวมติ) ·
   ต้องสัมภาษณ์เต็มรูปแบบ + `system-analyst` (Pinecone namespace, ingestion, retrieval quality)
   → **ยังไม่พร้อมส่ง engineer**

## knowledge-base

**ขาเข้าสื่อการสอน + คลังความรู้ที่มีคนดูแลได้** — เกิดจากเจ้าของโปรเจกต์ยกประเด็นว่าระบบ
"ยังไม่ครบวงจร": ครึ่งซ้าย (เตรียมความรู้ → แจกลิงก์ → เรียน → จับคำถาม) ทำงานได้แล้ว
แต่ครึ่งขวา (รีวิว → แก้ความรู้ → รู้ว่าดีขึ้น) ตัน

Docs: requirement ✅ · design ✅ (ยืนยัน default-chain amendment แล้ว — เป็น contract) · plan ✅ (default-chain repository/test contract ตรง design แล้ว) · Phase 1 code ✅ พร้อม QA (invariant แก้แล้ว + verify ผ่านครบ — ดูหัวข้อ "Claude Code handoff" ด้านล่าง)

**เคาะแล้ว 4 ข้อ**: R1 taxonomy 3 ชั้น (category > subcategory > ชื่อเนื้อหา ใช้กับบทเรียนและเอกสาร) ·
R2 แต่ละบริษัทจัดหมวดเอง ไม่ใช่ชุดกลางของ School Bright · R3 คลังความรู้ 3 ระดับ
(บทเรียน/หมวด/ทั้งบริษัท) · R4 บทพูด — Google Slides แก้ที่ต้นทาง, PDF มีช่องแก้ในระบบ
prefill จากข้อความที่ดึงได้ เก็บเฉพาะหน้าที่แก้ อัปไฟล์ใหม่ล้างทิ้ง ไม่ทำ OCR และต้อง re-index

**R5 เคาะแล้ว 2026-08-19** — **คลังความรู้เป็น Q&A ที่โตจากคำถามจริง** (แนวคิดของเจ้าของโปรเจกต์):
CS กด "ตอบผิด" แล้วเขียนคำตอบที่ถูกลงไปเลย ไม่ต้องไปเขียนเอกสารใหม่ · คิว = คำถามที่ยังไม่มี
คำตอบ มาจาก `not_found` อัตโนมัติ + CS กดผิด · ปิดงานเกิดเองเมื่อมีคำตอบ ไม่มีปุ่ม "แก้แล้ว" ·
AI ใช้ Q&A เป็นหลักฐานแล้วเรียบเรียงใหม่ ห้ามคัดลอก · CS เลือก scope ตอนบันทึก ·
**ขัดกับเอกสาร → เอกสารชนะ + ยกธงให้ CS ไปแก้เอกสาร**

**R6 เคาะแล้ว 2026-08-19** — ลบเอกสาร = **soft delete ในฐานข้อมูล + ลบ vector จริงใน Pinecone**
(soft delete ไม่มีความหมายกับ vector store เพราะไม่มี query filter — ถ้าไม่ลบ AI ยังตอบจากมัน) ·
คิวงาน index เก็บลง DB และทำงานค้างต่อเองตอนสตาร์ต ไม่ต้องรอ CS กด · CS เปิดดูข้อความที่แปลงได้
ตลอดทุกไฟล์ ไม่ใช่เฉพาะตอนล้มเหลว (ระบบจับ "ว่างเปล่า" ได้ แต่จับ "ตัวอักษรเสียหาย" ไม่ได้) ·
สถานะ `failed` ต้องบอกสาเหตุแยกกัน

**เพิ่มจากการทบทวนรอบสุดท้าย**: R1.1 บทเรียนอยู่หมวดเดียวและต้องมีหมวดเสมอ (ข้อมูลเดิมต้องจัด
หมวดให้ครบก่อนเปิดใช้) · R3.1 ย้ายหมวด = ความรู้ที่ใช้ตอบเปลี่ยนทันที ต้องเตือนก่อนย้าย ·
R5.7 `cs` เขียนคำตอบแล้วใช้ได้ทันทีไม่ต้องรออนุมัติ

**อัปเดต 2026-08-19 — `design.md` ร่างเสร็จแล้ว (`system-analyst`)**

R1–R6 **ทำได้ทั้งหมดด้วย stack เดิม ไม่ต้องเพิ่ม dependency ใดๆ** (ตรวจถึงระดับโค้ดแล้ว:
Pinecone `/vectors/delete` รองรับ `ids` อยู่แล้ว · คิวถาวร = ตาราง + polling ไม่ต้องมี Redis)
→ Data Model 16 ส่วน (ตารางใหม่ 7 · แก้ของเดิม 3 · constants 4 ชุด · interface 2 ตัว) ·
contract 5 ชุด (Taxonomy · Knowledge Scope & Retrieval · PDF Narration · Q&A Queue ·
Document Intake & Job) · Migration Plan MG-A1..MG-F1 · Module A–F

**ราคาที่ต้องจ่ายจริง 3 จุด**: (1) `LessonConfig.CategoryId` เป็น required = breaking change
ต้อง backfill · (2) R6.1 เป็นครั้งแรกที่ soft delete มีความหมายจริง — วันนี้ `RepositoryBase.Delete`
คือ `_set.Remove()` ลบจริงทุกครั้ง ไม่มี query filter ที่ไหนเลย · (3) R6.2 ไม่ใช่การย้ายที่เก็บคิว
แต่เป็นการเปลี่ยนรูปงาน — คิวเดิมจับ `byte[]` ของไฟล์ไว้ใน closure งานที่ persist ได้ต้องโหลด
ไฟล์ใหม่จาก storage

**งานแฝงที่เลี่ยงไม่ได้**: บทเรียน `ContentSourceType = "pdf"` **ไม่เคยถูก index เข้า namespace
ของตัวเองเลย** (`ILessonConfigService` index เฉพาะเมื่อมี `PresentationId`) → R4.5 บังคับให้ต้อง
เปิดเส้นทางนี้ก่อน ไม่งั้นไม่มีอะไรให้ re-index

**✅ Q1–Q6 เคาะครบเมื่อ 2026-08-19** (รายละเอียดมติแต่ละข้ออยู่ใน `design.md`
§Unresolved Open Questions → "มติที่ปิดแล้ว"): Q1–Q5 ยืนยันตรงตามข้อเสนอเดิมทุกข้อ ·
**Q6 เจ้าของโปรเจกต์ปฏิเสธข้อเสนอเดิม (ปุ่ม "ไม่ต้องตอบ" + `QuestionQueueDismissal`)
และให้เหตุผลที่ถูกกว่า** — คำถามนอกเรื่องระบบต้องถูกกรองตั้งแต่ในห้องเรียนตอนที่ครูถาม
ไม่ใช่มากรองทีหลังในคิว ตรวจโค้ดยืนยันว่าระบบทำแบบนี้อยู่แล้ววันนี้จริง (`AnswerStatus.OutOfScope`
แยกจาก `NotFound`, QQ-1 ดึงคิวจาก `NotFound` เท่านั้น) → **DM-9 (`QuestionQueueDismissal`)
ถูกตัดออกทั้งตาราง** พร้อมทุกจุดที่อ้างถึง ขอบเขตจริงที่เหลือของ Q6 (คำถามที่เกี่ยวกับระบบจริง
แต่ CS ตัดสินว่าไม่มีคำตอบมาตรฐาน) เลือกปล่อยค้างไว้ในคิวเฉยๆ ไม่มีกลไกพิเศษ

**Design contract amendment ยืนยันแล้ว**: 6 phase และ Security gate เดิมไม่เปลี่ยน
แต่ migration แยกตามเจ้าของ phase: Phase 1 `AddKnowledgeTaxonomyAndScope` (breaking) · Phase 3
`AddDurableIndexingJobs` · Phase 4 `AddDocumentChunks` · Phase 5 `AddLessonSlideNarrations` ·
Phase 6 `AddKnowledgeQnA` (สี่ใบหลัง additive) · Phase 1–5 คืน Q&A count ของ TX-6/TX-10 เป็น `0`
โดยคง response shape เดิม แล้ว Phase 6 เชื่อมค่าจริงในรอบเดียวกับตาราง Q&A · final Data Model ไม่เปลี่ยน

local development/rehearsal เดินหน้าต่อได้เมื่อ local DB มี migration baseline ถึง
`20260818155126_AddTotalSlideCount` ซึ่ง local Compose ปัจจุบันมีแล้ว — **ไม่ต้องรอ shared/production
deployment ก่อนพัฒนา** · การ apply กับ shared/production ยังเป็น DevOps hard stop และต้องมี backup

**Claude Code handoff — Phase 1 backend — ✅ พร้อม QA (อัปเดต 2026-08-19)**: implementation หลักมี
อยู่ใน working tree แล้ว — `KnowledgeCategory`, scope fields/enums, EF mapping/repositories/UoW,
taxonomy service/controller, lesson category endpoint/validation, tests และ migration
`20260819082956_AddKnowledgeTaxonomyAndScope` · migration SQL ทำ default chain ถูก shape แล้วและ
**ไม่ต้อง regenerate**: หนึ่งบริษัทมี flagged parent Level 1 + leaf Level 2 ที่เชื่อมกัน และ backfill
`LessonConfig.CategoryId` ไป leaf

**งาน 4 ข้อที่เคยค้างไว้ก่อน QA — ปิดครบแล้ว**:
1. ✅ แก้แล้วทั้ง real repository (`IKnowledgeCategoryRepository.cs`) และ fake ใน tests —
   `GetSystemDefault()` filter `IsSystemDefault && Level == 2` ก่อน `SingleOrDefault()` ตรวจโค้ด
   ก่อนแก้ยืนยันแล้วว่าเป็นบั๊กจริง (migration backfill ติดธง `IsSystemDefault` ทั้ง parent+leaf
   พร้อมกัน `SingleOrDefault()` เปล่าจะได้ 2 แถวแล้ว throw `InvalidOperationException` ทันที)
2. ✅ เพิ่ม 6 tests ใหม่ใน `KnowledgeCategoryServiceTests.cs`: `GetSystemDefault_...ReturnsTheLeaf`,
   `GetSystemDefault_...FailsFastInsteadOfPickingOne` (สอง leaf ติดธงพร้อมกัน = data corruption
   guard) และ `[Theory(1,2)]` สำหรับ Update/Delete บล็อกทั้ง parent (Level 1) และ leaf (Level 2)
3. ✅ verify ครบ: `dotnet build` 0 warning/0 error · focused `KnowledgeCategory*` tests 9/9 ผ่าน ·
   full non-integration suite 149/149 ผ่าน (21 Providers + 127 Application + 1 IntegrationTests) ·
   `dotnet ef migrations has-pending-model-changes` = "No changes have been made to the model" ·
   **isolated PostgreSQL rehearsal รอบใหม่ผ่านจริงพร้อมข้อมูล** — สร้าง DB แยก
   `supportroom_rehearsal` ใน container เดิม, migrate ถึง `AddTotalSlideCount`, seed
   `LessonConfig`/`DocumentResource` คนละบริษัท 2 บริษัท (ครอบคลุมทั้ง lesson-scoped และ
   standalone/company-scoped), รัน `AddKnowledgeTaxonomyAndScope`, ตรวจ query ยืนยัน invariant
   ตรงทุกจุด (2 แถว `IsSystemDefault` ต่อบริษัทพอดี, chain เชื่อมกัน, `LessonConfig.CategoryId`
   ชี้ leaf, ไม่มี `CategoryId` ว่างค้าง, `DocumentResource.ScopeType/ScopeId` ตั้งถูก) แล้ว
   `DROP DATABASE supportroom_rehearsal` ทิ้ง ไม่แตะ `supportroom` (DB ทดสอบ manual เดิม) เลย
4. สถานะนี้คือ awaiting QA แล้ว — **ไม่มีการแก้ checkbox ใน `plan.md`** (ตามกฎ QA เท่านั้นที่ติ๊กได้)

**หมายเหตุระหว่างทำ rehearsal**: รอบแรกที่ลองทำ seed data ผ่าน `docker exec` (ไม่ใส่ `-i`) ทำให้
heredoc ไม่ถูกส่งเข้า container จริง เลย migrate ทับ DB ที่ว่างเปล่าโดยไม่รู้ตัว (ผลลัพธ์ดูผ่าน
เพราะไม่มี error แต่ไม่ได้พิสูจน์อะไรเลย) แก้ด้วยการใส่ `-i` แล้วยืนยันว่า seed เข้าจริงก่อนรัน
migration ทุกครั้ง — บันทึกไว้เผื่อใครทำ rehearsal แบบเดียวกันต่อจะได้ไม่พลาดซ้ำ

**หลักฐานรอบนี้ (แทนที่ของเดิมที่เป็น stale)**: build 0/0 · focused 9/9 · full non-integration
149/149 · EF model ตรง snapshot 100% · isolated rehearsal ผ่านพร้อมข้อมูลจริง (ไม่ใช่ DB ว่าง) ·
ไม่แตะ local shared `supportroom` DB, shared หรือ production

**EF tooling incident/ข้อห้าม**: เคยใช้ `--no-build` กับ stale binary จนได้ empty migration และ
`dotnet ef migrations remove` เลือกลบ baseline `20260818155126_AddTotalSlideCount` ผิดใบ; source,
Designer และ snapshot ถูกกู้กลับแล้ว และไม่มี DB mutation เพราะ connection ล้มก่อน · **ห้ามใช้
`migrations remove` ซ้ำ**; build source ปัจจุบันก่อนใช้ EF CLI และใช้ explicit project/startup +
isolated DB เท่านั้น · ยังไม่ deploy และงาน final UI/visual polish ยังรอทีม UX/UI ของผู้ใช้

**Contract dependency ที่ตรวจแล้ว**:
1. `learning-session/design.md` CA-1 ยืนยัน `SessionQuestion.SessionId` เป็น contract ปัจจุบันแล้ว ·
   `knowledge-base` ใช้ชื่อนี้ต่อได้ ไม่ใช่ unresolved drift
2. `CLAUDE.md` §Known Baseline "ไม่มี auth/rate limiting (TD-002)" **ล้าสมัยแล้ว** —
   `AdminUser` + `AdminRole` (owner/admin/cs) + `IAuthorizationGuard` มีครบ · R5.6/R5.7
   ออกแบบโดยอาศัยข้อเท็จจริงนี้ ถ้า auth ยังไม่ครอบ `/admin/*` จริงต้องตีกลับมาที่ `system-analyst`

**Claude Code handoff — Phase 2 backend — ✅ core เสร็จ + verify เองแล้ว (2026-08-19)**:
`KnowledgeNamespaces.ForCategory` (DM-12) · `IVoiceQuestionProvider.CategoryNamespace` (required,
KS-3) · `RagVoiceQuestionProvider` ยิง 3 namespace พร้อมกันด้วย `Task.WhenAll` (lesson + category +
global) แทนที่ 2 เดิม, `MergeTopK` เปลี่ยน signature รับ `IEnumerable<IReadOnlyList<ScoredChunk>>`
เพื่อรองรับ N namespace โดยไม่ต้องแก้ signature อีกรอบถ้ามีที่สี่ (KS-3 หมายเหตุ Parent) ·
resolver กลาง KS-1 ใหม่ `IKnowledgeNamespaceResolver`/`KnowledgeNamespaceResolver`
(`SupportRoom.Application/Services/`) — `Resolve(companyId, scopeType, scopeId)` แปลง
ScopeType/ScopeId เป็น namespace เดียวสำหรับทุก entity (lesson ค้นชื่อ slug จาก
`ILessonConfigRepository`, category ใช้ id ตรงผ่าน `ForCategory`, company บังคับ `ScopeId == null`)
พร้อม `EnsureValidScope(...)` แยกสำหรับ KS-2 (ตรวจว่าแถวมีจริงในบริษัทนี้ก่อนเซฟ, category ต้อง
`Level == 2`) — ยังไม่มี call site ไหนเรียก `EnsureValidScope` เพราะ Phase 2 ไม่มี endpoint ที่รับ
scope จาก request โดยตรง (`DocumentResource`/`KnowledgeQnA` scope selection เป็นงานของ Phase 3/6);
`VoiceQuestionService.AskAsync` เรียก `Resolve(...)` เพื่อได้ `CategoryNamespace` จาก
`content.Lesson.CategoryId` ก่อนเรียก provider (จุดเดียวที่ resolver ถูกใช้จริงตอนนี้) ·
`metadata.sourceType` เพิ่มครบ 3 จุดตาม KS-6: `IKnowledgeIndexingService.IndexLessonAsync` =
`"slide"`, `IDocumentResourceService`/`IAdminService` (ทั้งจุด upload และจุด reindex-all) =
`"document"` · เพิ่ม `SupportRoom.Domain.Enums.KnowledgeSourceType` (`document`/`slide`/`qna`)
คู่กับ `KnowledgeScopeType` เดิม กันไม่ให้ string เหล่านี้เป็น magic value กระจายในโค้ด — ไม่ได้อยู่ใน
DM-11 ตรงตัวแต่สอดคล้อง convention เดิมของโปรเจกต์ (static class + const string) · ฝั่งอ่าน metadata
เพิ่ม `ResolveSourceType` (private static ใน `RagVoiceQuestionProvider`) treat "ไม่มี sourceType"
เป็น `"document"` เสมอ ไม่ throw — ใช้จริงในบรรทัด log ของการ retrieval (KS-7 การแยกสองบล็อกใน
prompt ตาม sourceType เป็นงานของ Phase 6 ตามที่ `plan.md` ระบุไว้ชัด ไม่ได้ทำที่นี่) · KS-11
ยืนยันแล้วว่าไม่ต้องแก้โค้ด — query 3 namespace อยู่ใน `try/catch` เดิมที่ fallback เป็น full-deck
อยู่แล้ว และ Pinecone `/query` กับ namespace ที่ไม่เคยสร้างคืน list ว่างไม่ throw (พฤติกรรมเดิมตอน
มี 2 namespace ก็อาศัยกลไกเดียวกันนี้อยู่แล้ว)

**Unit test ใหม่**: `KnowledgeNamespaceResolverTests.cs` (7 tests) ครบ 3 กรณีตาม `plan.md`
(lesson ใช้ slug ไม่ใช่ id, category ใช้ id ตรง, company คืน `kb-global`) บวก KS-2 (`company` ที่มี
`ScopeId` ต้องถูกปฏิเสธไม่ใช่เพิกเฉย, lesson ที่หา id ไม่เจอต้อง 404, category level 1 ต้องถูก
ปฏิเสธ) · แก้ `RagVoiceQuestionProviderMergeTests.cs` ให้ตรง `MergeTopK` signature ใหม่ (list ของ list
แทนสอง parameter แยก) ไม่ได้เพิ่ม assertion ใหม่ในไฟล์นั้น · แก้ `VoiceQuestionServiceTests.cs` ให้
inject `KnowledgeNamespaceResolver` (constructor ใหม่ต้องการมัน) โดยใช้ `FakeLessonConfigRepository`
เดิมของ test + `FakeKnowledgeCategoryRepository` เปล่า (ไม่ต้อง seed เพราะ `Resolve` ของ category
ไม่ query repository)

**Verify**: `dotnet build SupportRoom.slnx` = 0 warning/0 error · `dotnet test --filter
"Category!=Integration"` = 156/156 ผ่านทั้งหมด (21 Providers + 134 Application + 1
IntegrationTests; baseline ก่อนหน้า 149/149 = 21 + 127 + 1 → เพิ่ม 7 test ใหม่ของ resolver พอดี
ไม่มี test ไหนถูกลบ) · ไม่แตะ migration ใดๆ ในรอบนี้ (Phase 2 ไม่มี MG-* ของตัวเอง)

**ค้างไว้ ไม่ได้ตัดสินใจเอง**:
1. **R-2 latency ยัง"ไม่ได้วัด"** — `plan.md` ขอให้ "วัด latency จริงหลัง deploy 3-namespace query"
   แต่ระบบยังไม่เคย deploy และไม่มี traffic จริงให้วัด (local dev เท่านั้น) รอ `devops`/สภาพแวดล้อม
   ที่มี traffic จริงก่อนจะมีตัวเลขให้บันทึก — ไม่ใช่ทำหาย เป็นเงื่อนไขเวลาที่ยังไม่ถึง
2. `IKnowledgeNamespaceResolver.EnsureValidScope` (KS-2) เขียนไว้แล้วแต่ยังไม่มี call site เรียกใช้
   จริง เพราะไม่มี endpoint ไหนใน Phase 2 ที่รับ ScopeType/ScopeId จาก request โดยตรง — จะถูกเรียก
   จริงตอน Phase 3 (`DocumentResource` upload รองรับ category scope) และ Phase 6 (`KnowledgeQnA`)
   ตาม `plan.md` ระบุไว้ว่า resolver ตัวนี้ "ให้ทั้ง DocumentResource และ KnowledgeQnA ใช้ร่วมกัน" —
   ไม่ใช่ gap ของ Phase 2 เอง แต่เป็นจุดที่ `qa-engineer`/`system-analyst` ควรตรวจตอน Phase 3/6 ว่า
   call site จริงเรียก `EnsureValidScope` ก่อนเซฟทุกครั้งตามที่ KS-2 ต้องการ
3. **ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA" ไม่ใช่ "เสร็จยืนยันแล้ว"

หมายเหตุ: `docs/KNOWLEDGE_ROADMAP.md` เป็น roadmap เชิงเทคนิคของ retrieval/eval (K0–K4)
คนละชั้นกับ requirement นี้ ไม่ทับกัน — เอกสารนั้นตอบ "ทำให้ retrieval ดีขึ้นอย่างไร"
เอกสารนี้ตอบ "ใครดูแลความรู้และทำงานวันต่อวันยังไง"

**Claude Code handoff — Phase 3 backend — ✅ เสร็จ + verify เองแล้ว (2026-08-19), มี 1 deviation ต้องตรวจ**:

ทำครบทุกงาน `[backend]` ใน `plan.md` §Phase 3 (ข้าม `[frontend]` 3 ข้อท้าย checklist ตามที่สั่ง):
`BackgroundJob` entity (DM-10, ไม่มี `HasQueryFilter` โดยเจตนา พร้อม comment ทั้งใน entity/
`ApplicationDbContext`) · `BackgroundJobType`/`BackgroundJobStatus` (DM-11) · migration
`AddDurableIndexingJobs` (สร้างแค่ `BackgroundJob` ตาม MG-C1 ตรง scope) · `IBackgroundJobRepository`
— `ClaimNext(now)` ด้วย raw SQL `UPDATE...RETURNING` + `FOR UPDATE SKIP LOCKED`,
`RequeueOrphanedRunning()`, ทั้งคู่ `IgnoreQueryFilters()` — **สมัคร SQL ตรงกับ column/table name
จริงแล้วยืนยันด้วยมือผ่าน `psql` กับ Postgres จริง** (claim → running, requeue → pending, ไม่ผ่าน
`dotnet test` เพราะ EF InMemory provider ไม่รองรับ raw SQL/locking) · `IKnowledgeIndexProvider.
DeleteVectorsAsync` + implement ใน `PineconeKnowledgeIndexProvider` (ซอย 1000 id/request, แยก
`DeleteAllRequest`/`DeleteByIdsRequest` คนละ type) · worker ใหม่ `BackgroundJobHostedService`
(Api) + `IBackgroundJobProcessor` (Application, business logic ตามธรรมเนียม layering) แทนที่
`IBackgroundTaskQueue`/`BackgroundTaskQueue.cs`/`QueuedHostedService.cs` ที่ลบทิ้งทั้งหมดแล้ว
(DI-17) · DI-1/DI-2/DI-3/DI-4/DI-9/DI-11/DI-12/DI-14/DI-15/DI-16 ตรงตาม contract ทุกข้อ · DI-5
แยกครบ 4 สาเหตุผ่าน pure function `DocumentIndexingResultMapper.Map` (unit-test ได้โดยไม่ต้องมี DB/
provider ตาม R-12) · DI-10 เพิ่ม `DocumentResourceViewModel.WillRetryAt`, ไม่ map `LastErrorDetail`
ที่ไหนเลย · `GET /api/documents/deleted` + `POST /api/documents/{id}/restore` ใหม่ · unit test ใหม่
`BackgroundJobProcessingTests.cs` (DI-5 ครบ 5 กรณี + DI-9 backoff calculation)

**⚠️ Deviation ที่ต้องให้ `system-analyst`/`qa-engineer` ตรวจ — DI-13 กับลำดับ migration**:
`plan.md`/`design.md` DI-13 สั่งให้ตอนลบเอกสาร "soft delete `DocumentChunk` ทุกแถวของมัน" +
เก็บ `VectorId` ทั้งหมดลง `BackgroundJob.PayloadJson` ของงาน `vector_delete` — แต่ `DocumentChunk`
เป็นตารางของ Phase 4/Module D (`MG-D1`) **ยังไม่ถูกสร้างในโค้ดจนถึงตอนนี้** (`design.md` เอง
ก็ระบุ MG-C1 ของ Phase 3 สร้างแค่ `BackgroundJob` เท่านั้น) เลยไม่มีตารางให้ soft delete และไม่มี
ที่เก็บ `VectorId` ที่ persist ไว้ล่วงหน้าให้ดึงมาใส่ `PayloadJson` ตามตัวอย่างใน DM-10

สิ่งที่ implement แทน (ตัดสินใจทางเทคนิค ไม่ใช่การตีความ business/schema ใหม่ — `PayloadJson`
เป็น free-form JSON อยู่แล้วตาม DM-10 ไม่มี schema บังคับรูปแบบ): `vector_delete` job เก็บแค่
`TargetId` (= documentId), ไม่ใส่ `PayloadJson`; ตอน process worker โหลด entity ที่ soft-delete
แล้วผ่าน `GetDeleted()`, re-download ไฟล์เดิมจาก storage แล้ว **re-extract ด้วย extractor เดิม**
เพื่อ regenerate `{documentId}-{chunkId}` ให้ตรงชุดที่เคย index ไว้ (ใช้ได้เพราะทุก extractor
สร้าง `ChunkId` จากตำแหน่งโครงสร้างไฟล์ล้วนๆ — เลขหน้า/สไลด์/ย่อหน้า — ไม่ใช่ hash เนื้อหา และ
DI-13 เองก็สั่งห้ามลบไฟล์จริงตอนลบเอกสารอยู่แล้ว) แล้วค่อยเรียก `DeleteVectorsAsync` ด้วย id
ชุดนั้น —ดูรายละเอียดที่ comment บนคลาส `BackgroundJobProcessor`
(`SupportRoom.Application/Services/IBackgroundJobProcessor.cs`)

**ทำไมไม่ใช้วิธีอื่น**: `DeleteNamespaceAsync` (ลบทั้ง namespace) ผิดชัดเจน — namespace เดียวกัน
มีเอกสาร/บทเรียนอื่นแชร์อยู่ · การรอสร้าง `DocumentChunk` ก่อนแล้วค่อยทำ Phase 3 เป็นการข้าม
sequencing ที่ `design.md` เขียนไว้เอง (Phase 4 ขึ้นกับ Phase 3 ทำงาน worker เสร็จก่อน ไม่ใช่กลับกัน)

**สิ่งที่ควรเกิดใน Phase 4**: เมื่อ `DocumentChunk` มีจริงแล้ว ควรแก้ `ProcessVectorDeleteAsync`
ให้อ่าน `VectorId` จาก `DocumentChunk` ที่ persist ไว้แทนการ re-extract — แม่นกว่า (ไม่ต้องพึ่ง
สมมติฐานว่า `ChunkId` deterministic ตลอดไป) และไม่ต้องพึ่งไฟล์เดิมยังอ่านได้ ควรบันทึกเป็นงาน
follow-up ใน `plan.md` §Phase 4 หรือ amend `design.md` ให้ `system-analyst` ยืนยันแนวทางนี้ก่อน

**Verify**: `dotnet build SupportRoom.slnx` = 0 warning/0 error · `dotnet test --filter
"Category!=Integration"` = **168/168 ผ่านทั้งหมด** (21 Providers + 146 Application + 1
IntegrationTests; baseline ก่อนหน้า 156/156 → เพิ่ม 12 test ใหม่: 9 DI-5/DI-9 pure-logic +
3 `DocumentResourceServiceTests` ใหม่ครอบ `GetDeleted`/`RestoreAsync`, ไม่มี test ไหนถูกลบ) ·
migration `AddDurableIndexingJobs` apply กับ local Postgres จริงแล้ว (`dotnet ef database update`
ผ่าน) · แก้ `CompanyIsolationTests.EveryEntityIsCompanyScoped` ให้มี allowlist ชัดเจนสำหรับ
`BackgroundJob` (เอนทิตีเดียวที่ `ICompanyScoped` แต่ไม่มี query filter โดยเจตนา — เดิม test
ไม่มีช่องให้ยกเว้น ทำให้ fail ทันทีที่เพิ่ม entity นี้เข้ามา ต้องแก้ให้ test เป็นไปตาม contract
แทนที่จะย้อนไปเพิ่ม filter ที่ design.md ห้ามไว้)

**ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA" ไม่ใช่ "เสร็จยืนยันแล้ว"
· งาน `[frontend]` 3 ข้อท้าย Phase 3 checklist ยังไม่ได้ทำ รอ `frontend-engineer`

**✅ Phase 3 deviation ปิดแล้วใน Phase 4 (2026-08-19)**: `DocumentChunk` มีจริงแล้ว —
`ProcessVectorDeleteAsync` เลิกใช้วิธี re-download + re-extract ไฟล์เดิมทั้งหมด เปลี่ยนไปอ่าน
`VectorId`/`NamespaceKey` จาก `DocumentChunk` ที่ `IDocumentResourceService.DeleteAsync` persist ไว้
ณ ตอนลบจริง (ผ่าน `BackgroundJob.PayloadJson` รูปแบบ `VectorDeleteJobPayload`) — ดูรายละเอียดที่หัวข้อ
"Claude Code handoff — Phase 4 backend" ด้านล่าง ลบ comment เก่าบนคลาส `BackgroundJobProcessor` ที่
อธิบาย deviation นี้ทิ้งแล้วเพราะไม่จริงอีกต่อไป

**Claude Code handoff — Phase 4 backend — ✅ เสร็จ + verify เองแล้ว (2026-08-19)**:

ทำครบทุกงาน `[backend]` ใน `plan.md` §Phase 4 (ข้าม `[frontend]` 1 ข้อท้าย checklist ตามที่สั่ง):
`DocumentChunk` entity ตรง DM-4 คำต่อคำ (รวม class comment ยาวที่อธิบายเหตุผล R6.1/R6.3 — คัดลอกมา
จาก `design.md` ทั้งก้อน) · `ApplicationDbContext` เพิ่ม `DbSet<DocumentChunk>` + index
`(DocumentId, SeqNo)` และ `CompanyId` + `HasQueryFilter` ตาม DM-15 · migration `AddDocumentChunks`
(MG-D1, สร้างแค่ตาราง `DocumentChunk` ตาม scope ที่กำหนด) · `IDocumentChunkRepository` —
`GetByDocumentId(documentId)` (เรียง `SeqNo`), `DeleteByDocumentId(documentId)` soft delete —
ลงทะเบียนใน `UnitOfWork.Register` · `DocumentChunkTextAnalyzer.HasSuspectCharacters` (DI-6) เป็น
pure static function แยกออกมาต่างหาก (NUL/C0 control นอก tab-newline-CR/PUA `U+E000`–`U+F8FF`/
`U+FFFD`) — ใช้เป็นตัวช่วยเรียงลำดับเท่านั้น ไม่เคยใช้บล็อกการ index หรือกำหนด `failed`
· แก้ `BackgroundJobProcessor.ProcessDocumentIndexAsync` ให้เขียน `DocumentChunk` ตาม DI-8 ทุกครั้งที่
index สำเร็จ — soft delete ชุดเดิมของ `DocumentId` ทั้งหมดแล้วเขียนชุดใหม่ทั้งชุด (ไม่ merge ทีละแถว)
ในทรานแซกชันเดียวกับการอัปเดตสถานะเอกสาร (commit เดียวกันใน `ProcessAsync`) — chunk ที่ text ว่าง/
whitespace ล้วนไม่ถูกเขียนเป็นแถว (ไม่เคยถูก embed/upsert จริง จึงไม่มี `VectorId` ให้บันทึก) ·
`GET /api/documents/{id}/chunks` (DI-7) — คืน `DocumentChunk` เรียง `SeqNo` พร้อม `ChunkKey`/
`CharCount`/`HasSuspectCharacters` ผ่าน `DocumentChunkViewModel` ใหม่ — เรียก
`guard.EnsureAuthenticated()` + `guard.EnsureCanAccessCompany(entity.CompanyId)` ก่อนคืนข้อมูลเสมอ
(security gate ของ phase นี้ — endpoint แรกที่คืนเนื้อหาดิบของไฟล์อัปโหลด)

**DI-13 เปลี่ยนจริงตามที่สั่ง**: `IDocumentResourceService.DeleteAsync` อ่าน `DocumentChunk` ที่มีอยู่
ของเอกสารก่อน soft delete, group ตาม `NamespaceKey` (ปกติมีกลุ่มเดียวเพราะ DI-8 แทนที่ทั้งชุดเสมอ),
สร้าง `BackgroundJob` ชนิด `vector_delete` หนึ่งงานต่อกลุ่ม `PayloadJson` เป็น `VectorDeleteJobPayload`
(`{NamespaceKey, VectorIds}` เขียนด้วย `System.Text.Json`) แล้วค่อย soft delete แถว `DocumentChunk`
เอง — เอกสารที่ไม่เคย index สำเร็จเลย (ไม่มี `DocumentChunk`) จะไม่ enqueue งาน `vector_delete` เลย
(ต่างจากพฤติกรรมเดิมที่ enqueue เสมอแล้วให้ worker เช็คว่าง — เปลี่ยนพฤติกรรมนี้โดยตั้งใจ มี test ใหม่
ยืนยันทั้งสองเคส) · `ProcessVectorDeleteAsync` ยังคงเช็ค `documentRepository.GetDeleted()` ก่อนเสมอ
(ถ้าเอกสารถูก restore ไปแล้วก่อนงานนี้รัน ให้ข้ามไปเฉยๆ ไม่แตะ vector ที่เพิ่ง re-index ใหม่ — เหตุผล
เดิมจาก Phase 3 ยังใช้ได้ และสำคัญกว่าเดิมเพราะ id ที่เก็บใน payload อาจชนกับ id ที่เพิ่งสร้างใหม่หลัง
restore ถ้า parser ไม่เปลี่ยน)

**Verify**: `dotnet build SupportRoom.slnx` = 0 warning/0 error · `dotnet test --filter
"Category!=Integration"` = **172/172 ผ่านทั้งหมด** (21 Providers + 150 Application + 1
IntegrationTests; baseline ก่อนหน้า 168/168 → เพิ่ม 4 test ใหม่ใน `DocumentResourceServiceTests`
ครอบ `DeleteAsync` payload/soft-delete ของ `DocumentChunk` และ `GetChunks` authorization/ordering
ไม่มี test ไหนถูกลบ) · migration `AddDocumentChunks` apply กับ local Postgres จริงแล้ว (container
`supportroom-pg` พอร์ต 5432 ตรงกับ `.env` — คนละตัวกับ `supportroom-local-postgres-1` พอร์ต 55432
ที่ไม่ได้ใช้งานจริง) ยืนยัน `\d "DocumentChunk"` ตรง schema ที่คาดไว้ครบทุกคอลัมน์/index

**ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA" ไม่ใช่ "เสร็จยืนยันแล้ว"
· งาน `[frontend]` 1 ข้อท้าย Phase 4 checklist ยังไม่ได้ทำ รอ `frontend-engineer`

**Claude Code handoff — Phase 5 backend — ✅ เสร็จ + verify เองแล้ว (2026-08-19)**:

ทำครบทุกงาน `[backend]` ใน `plan.md` §Phase 5 (ข้าม `[frontend]` 3 ข้อท้าย checklist ตามที่สั่ง):
`LessonSlideNarration` entity ตรง DM-5 คำต่อคำ (รวม class comment ที่อธิบาย R4.1) · `ApplicationDbContext`
เพิ่ม `DbSet<LessonSlideNarration>` + index `(LessonId, SlideObjectId)`/`CompanyId` + query filter
`CompanyId && !IsDelete` ตาม DM-15 · migration `AddLessonSlideNarrations` (MG-E1, สร้างแค่ตารางนี้
ตาม scope) · `ILessonSlideNarrationRepository` — `GetByLessonId`/`GetOne`/`DeleteByLessonId` (soft,
คืนจำนวนที่ลบ) ลงทะเบียนใน `UnitOfWork.Register`

**NR-1 (จุดเดียว ใช้ทั้งสอนจริง+index)**: `ILessonSlideNarrationResolver`/`LessonSlideNarrationResolver`
ใหม่ (`SupportRoom.Application/Services/`) — `ResolveAsync(lessonId, baseSlides)` merge แถว
`LessonSlideNarration` ทับ `SpeakerNotes` เฉพาะหน้าที่มี override เรียกจากสามจุดเท่านั้น ไม่มีจุดที่สี่:
(1) `LessonConfigService.GetPdfContentAsync` (tutor engine + `GetTeachingContentBySlugAsync`/
`GetTeachingContentByLinkAsync` ที่เรียกต่อ) (2) `LessonConfigService.SaveAsync`'s NR-7 branch ตอน
index (3) `BackgroundJobProcessor.ProcessLessonIndexAsync`'s NR-6 partial re-index — ตรวจโค้ด
tutor engine เดิม (`ILessonConfigService.GetPdfContentAsync`) แล้วต่อ resolver เข้าไปแทนที่จุดเดิม
ไม่ได้เพิ่มจุดที่สาม ตามที่สั่งเป็นพิเศษ

**NR-2/NR-9**: `ILessonSlideNarrationService`+impl ใหม่ — `SaveAsync(lessonId, slideObjectId,
narrationText)`: trim แล้วเทียบกับ prefill จาก `PdfSlidesRenderer` (ผ่าน `ILessonConfigService.
PreviewPdfAsync` ที่ cache อยู่แล้ว ไม่ re-parse ซ้ำ) — เท่ากับ prefill (รวมค่าว่าง) → ลบแถวถ้ามี
ไม่สร้างใหม่ (มี test ยืนยันทั้งเคส "พิมพ์กลับค่าเดิมทับ override ที่เคยมี" และเคส "prefill ตรงเป๊ะตั้งแต่ต้น
ไม่เคยมีแถว") · ต่างกัน → upsert · `EnsurePdfSource` ปฏิเสธที่ server ทันทีถ้า
`ContentSourceType = google_slides` (NR-9, ครอบทั้ง `GetAllAsync`/`SaveAsync`)

**NR-3**: `LessonConfigService.SaveAsync` capture `previousPdfDocumentResourceId` ก่อนเขียนทับ —
ถ้าเปลี่ยนค่าจริง (ไม่ใช่แค่เซฟทั่วไป) เรียก `_narrationRepository.DeleteByLessonId(entity.Id)`
**ในทรานแซกชันเดียวกับ** `UnitOfWork.Commit()` ที่เซฟ `PdfDocumentResourceId` ใหม่ · เพิ่ม
`GET /api/lessons/{id}/narrations/count` แยกต่างหากให้ frontend เรียกก่อนยืนยันอัปโหลดทับ (คืน
`{count}` จาก `ILessonSlideNarrationService.CountByLessonId`) — ชื่อ route นี้เป็นการตัดสินใจทางเทคนิค
เอง (`design.md`/`plan.md` ไม่ได้ตั้งชื่อ endpoint ไว้ตายตัว แค่บอกว่าต้องมี "endpoint คืนจำนวนแถว")

**NR-4**: ไม่ implement heuristic จับคู่หน้าใดๆ ตามที่สั่งห้ามไว้ — NR-3 ลบทั้งหมดเสมอ

**NR-5**: `LessonNarrationsViewModel.IsLikelyScanned` คำนวณจาก **base** (unedited) `SpeakerNotes`
ของทุกหน้าก่อน apply resolver (ไม่ใช่ resolved text ซึ่งจะไม่มีทางว่างถ้ามี override แล้ว) — ตรงตาม
เจตนาของ NR-5 ที่ต้องเตือนว่าไฟล์เป็นสแกน ไม่ใช่เตือนว่า CS ยังพิมพ์ไม่ครบ

**NR-6**: `LessonSlideNarrationService.SaveAsync` enqueue `BackgroundJob(lesson_index)` (`TargetId
= LessonId`, `PayloadJson` = `LessonIndexJobPayload{SlideObjectIds}` ผ่าน `JsonSerializer.Serialize`
default — **ตั้งใจไม่ใส่ `PropertyNamingPolicy.CamelCase`** เพื่อให้ตรง convention จริงที่
`VectorDeleteJobPayload` วางไว้แล้วใน Phase 4 (PascalCase บน `PayloadJson`, ต่างจาก property
ตัวอย่างใน comment ของ DM-10 ซึ่งเป็นแค่ตัวอย่างประกอบ ไม่ใช่ contract ตายตัว) เฉพาะตอน "เปลี่ยนจริง"
เท่านั้น (ไม่ enqueue ถ้า trimmed text เท่ากับ prefill และไม่เคยมีแถวมาก่อน — ไม่มี state ให้เปลี่ยน) ·
`BackgroundJobProcessor.ProcessLessonIndexAsync` ใหม่ — อ่านเฉพาะหน้าที่ระบุใน payload, resolve ผ่าน
resolver ตัวเดียวกับ NR-1, upsert เฉพาะ chunk ที่ resolve ได้ข้อความจริง (`EmbedAndUpsertAsync`) ·
เพิ่มเคสที่ design.md ไม่ได้ยกตัวอย่างไว้ตรงๆ แต่จำเป็นเพื่อความถูกต้อง: หน้าที่ resolve ได้ข้อความว่าง
(override ถูกลบ **และ** extracted text ของหน้านั้นก็ว่างพอดี) เรียก `DeleteVectorsAsync` แทนการ
upsert เพื่อไม่ให้ vector เก่าที่มีเนื้อหาจริงค้างอยู่ใน Pinecone อย่างผิดๆ

**NR-7**: `LessonConfigService.SaveAsync` เพิ่ม `else if (ContentSourceType == Pdf && ...)` ต่อจาก
branch google_slides เดิม — build content จาก `BuildPdfContentAsync` แล้ว resolve ผ่าน NR-1 resolver
ก่อน index ด้วย `KnowledgeNamespaces.For(...)` เหมือน google_slides ทุกประการ (`metadata.sourceType
= "slide"` มาจาก `IndexLessonAsync` เดิมอยู่แล้ว ไม่ต้องแก้) — เปิดเส้นทาง index บทเรียน PDF เป็นครั้งแรก
ตามที่ `design.md`/`status.md` เตือนไว้ว่าไม่เคยเกิดขึ้นเลยก่อนหน้านี้

**NR-8**: ตรวจแล้วว่า `sourceType` แยกถูกระหว่าง `slide` (narration, ผ่าน `IndexLessonAsync`/
`ProcessLessonIndexAsync`) กับ `document` (เอกสารแนบ, ผ่าน `ProcessDocumentIndexAsync` เดิม) — ไม่มี
โค้ดจุดไหนรวมสองตัวแปลงเข้าด้วยกัน ตรงตาม O-4/NR-8 (นอก scope เฟสนี้)

**`POST /api/lessons` (P9/Q4)**: ตรวจแล้วว่า endpoint `[HttpPost] Save` เดิม (upsert-by-slug) ครอบ
requirement นี้อยู่แล้วครบ — `LessonConfigDto.CategoryId` เป็น `[Required]` และ `ValidateCategory`
ปฏิเสธ `Level != 2` (TX-4) ส่วน `ValidateSlug` ปฏิเสธ `kbcat-`/`kb-global` (TX-7) มาตั้งแต่ Phase 1
ไม่ต้องเพิ่ม endpoint ใหม่หรือโค้ดใหม่สำหรับข้อนี้

**Unit test ใหม่**: `LessonSlideNarrationServiceTests.cs` (8 tests) — NR-9 reject ทั้ง `GetAllAsync`/
`SaveAsync` บนบทเรียน google_slides · NR-2 ครบ 4 เคส (upsert เมื่อต่างจาก prefill + enqueue job,
ลบ override เมื่อส่งค่าว่าง, ไม่เคยสร้างแถวเมื่อ text ตรง prefill เป๊ะตั้งแต่ต้น + ไม่ enqueue job, ลบ
override เมื่อพิมพ์กลับค่า prefill เดิมทับ) · NR-3 นับเฉพาะแถวที่ยังไม่ถูกลบ · NR-1 resolver ผ่าน
`GetAllAsync` คืนทั้ง resolved text และ flag `IsOverridden` ถูกต้องต่อหน้า ใช้ PDF fixture จริง
(`Fixtures/sample.pdf`, 10 หน้า) ไม่ใช่ stub — documentId ตั้งใจใช้ `doc-narr-1` (ไม่ใช่ `doc-1`)
เพราะ `LocalDocumentStorageProvider` เขียนไฟล์ลง disk จริงใต้ `bin/Debug/.../storage/` ที่ persist
ข้ามรอบ `dotnet test` — ชนกับ `doc-1` ที่ `LessonConfigServiceTests` ใช้อยู่แล้วจะทำให้ test อื่น false-fail
(เจอเองระหว่างทำงานรอบนี้ พบว่าเป็นข้อจำกัดของ storage provider ไม่ใช่บั๊กใหม่ที่สร้างขึ้น — บันทึกไว้
เผื่อใครเพิ่ม test ที่ใช้ PDF fixture ต่อจะได้เลือก id ที่ไม่ชนกัน)

**Verify**: `dotnet build SupportRoom.slnx` = 0 warning/0 error · `dotnet test --filter
"Category!=Integration"` = **180/180 ผ่านทั้งหมด** (21 Providers + 158 Application + 1
IntegrationTests; baseline ก่อนหน้า 172/172 → เพิ่ม 8 test ใหม่ ไม่มี test ไหนถูกลบ) · migration
`AddLessonSlideNarrations` apply กับ local Postgres จริงแล้ว (`supportroom-pg` container พอร์ต 5432)
ยืนยัน `\d "LessonSlideNarration"` ตรง schema ที่คาดไว้ครบทุกคอลัมน์/index · `dotnet ef migrations
has-pending-model-changes` = "No changes have been made to the model"

**ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA" ไม่ใช่ "เสร็จยืนยันแล้ว"
· งาน `[frontend]` 3 ข้อท้าย Phase 5 checklist ยังไม่ได้ทำ รอ `frontend-engineer` (หน้าแก้บทพูดต่อหน้า,
flow ยืนยันก่อนอัปโหลด PDF ใหม่ทับของเดิม เรียก `GET /api/lessons/{id}/narrations/count` ที่เพิ่งเพิ่ม,
หน้า `/admin/lessons/new`)

**⚠️ พบและแก้บั๊กจริงหลัง Phase 6 — build/test ผ่านแต่แอปสตาร์ตไม่ขึ้น (2026-08-19)**: หลัง Phase 6
เสร็จ รีสตาร์ต backend จริงเพื่อยืนยัน (ไม่ใช่แค่เชื่อ build/test) พบว่า **แอปพังตอนสตาร์ตจริง**
ด้วย `InvalidOperationException: Unable to resolve service for type ILessonConfigRepository` —
`KnowledgeNamespaceResolver` (Phase 2) รับ `ILessonConfigRepository`/`IKnowledgeCategoryRepository`
เข้า constructor ตรงๆ แต่โปรเจกต์นี้**ไม่เคยลงทะเบียน repository เป็น DI service แยก** ทุก service
ต้องดึงผ่าน `IUnitOfWork.GetRepository<T>()` เท่านั้น (convention เดียวกันทั้งโปรเจกต์) — unit test
จับบั๊กนี้ไม่ได้เพราะใช้ fake ตรงๆ ไม่ผ่าน ASP.NET Core DI container จริง ต้อง**รันแอปจริงถึงจะเจอ**

แก้แล้ว: เปลี่ยน `KnowledgeNamespaceResolver` ให้รับ `IUnitOfWork` แล้วดึง repository ทั้งสองใน
constructor ตาม pattern เดียวกับทุก service ในระบบ · แก้ 3 จุดในเทส (`KnowledgeNamespaceResolverTests`,
`VoiceQuestionServiceTests`, `KnowledgeQnAServiceTests`) ให้ผ่าน `FakeUnitOfWork` แทนการ `new` ตรงๆ ·
build 0/0, test **189/189** ยังผ่านหมดหลังแก้ · **รีสตาร์ต backend จริงยืนยันแล้วว่าขึ้นสำเร็จ** และยิง
endpoint จริงผ่านทั้ง 6 phase (`knowledge-categories`, `lessons`, `qna-queue`, `knowledge-qna-conflicts`
ทั้งหมด 200) — บทเรียนสำหรับรอบต่อไป: **build+test เขียวไม่พอ ต้องรันแอปจริงก่อนถือว่า phase เสร็จ**

**Claude Code handoff — Phase 6 backend — ✅ เสร็จ + verify เองแล้ว (2026-08-19) — นี่คือ backend
phase สุดท้ายของทั้งโมดูล**:

ทำครบทุกงาน `[backend]` ใน `plan.md` §Phase 6 (ข้าม `[frontend]` 4 ข้อท้าย checklist ตามที่สั่ง):
`KnowledgeQnA`/`KnowledgeQnASource`/`KnowledgeQnAConflict` ตรง DM-6/7/8 คำต่อคำ (รวม class comment
อธิบาย R5/R5.2/R5.5) · `ApplicationDbContext` เพิ่ม 3 `DbSet` + index ตาม DM-15 และเพิ่ม index ใหม่บน
`SessionQuestion` (`(CompanyId, AnswerStatus)`, `(CompanyId, ReviewResult)`) **โดยไม่แก้ฟิลด์ใดๆ ของ
`SessionQuestion`** (R-9 — แจ้ง `learning-session` module ว่ามีการแตะ `OnModelCreating` ของ entity
ข้ามโมดูล ไม่ใช่ตัว entity เอง) · migration `AddKnowledgeQnA` (MG-F1) สร้างแค่ 3 ตารางนี้ + 2 index
บน `SessionQuestion` ตาม scope เป๊ะ ไม่รวมของ phase อื่น · `KnowledgeSourceChunk.EmbedText` (DM-14)
+ `IKnowledgeIndexingService` embed `chunk.EmbedText ?? chunk.Text` (บรรทัดเดียว คงสัญญาเดิมทั้งหมด)

**`IKnowledgeQnARepository`/`IKnowledgeQnASourceRepository`/`IKnowledgeQnAConflictRepository`** ใหม่
ลงทะเบียนใน `UnitOfWork.Register` ครบ · **QQ-1 นิยามคิว implement แบบแยกสองที่โดยตั้งใจ** (บันทึกไว้
เป็น technical decision ไม่ใช่การตีความ business ใหม่): `ISessionQuestionRepository.GetReviewQueue()`
ทำแค่ครึ่งแรก (`AnswerStatus == NotFound || ReviewResult == Incorrect`); ครึ่งหลัง (ไม่มี
`KnowledgeQnASource` ชี้มา) ทำที่ `KnowledgeQnAService.GetQueue()` ผ่าน
`IKnowledgeQnASourceRepository.GetBySessionQuestionIds(...)` เรียก**ครั้งเดียวต่อหน้า**ตามที่ DM-16
ระบุไว้ ("ห้ามยิงต่อแถว") — เลือกแยกแบบนี้เพื่อไม่ให้ `ISessionQuestionRepository` (ของ module นี้เอง
แต่เป็น entity ที่อ้างอิงข้าม concern) ต้องรู้จักตาราง Q&A โดยตรง และเพื่อให้ QQ-1 ทั้งชุด unit-test ได้
ง่ายด้วย fake repos ธรรมดา (ดู `KnowledgeQnAServiceTests.cs`) · QQ-4 (join ข้ามการเรียน/บทเรียน) ก็ทำ
ที่ service เดียวกัน ผ่าน `ILearningSessionRepository`/`ITrainingLinkRepository` แบบ batched 2 คิวรี
(ไม่ N+1) แทนที่จะ join ใน repository โดยตรง — รูปแบบเดียวกับที่ `VoiceQuestionService` orchestrate
ข้าม repository อยู่แล้วในโค้ดเดิม

**`IKnowledgeQnAService`** (`CreateAsync`/`UpdateAsync`/`DeleteAsync`/`GetQueue`) — `CreateAsync`
เรียก `IKnowledgeNamespaceResolver.EnsureValidScope` (KS-2 + TX-5 level==2 ในตัวเดียวกัน — resolver
เดิมจาก Phase 2 ที่ยังไม่เคยถูกเรียกจริงมาก่อน **Phase 6 คือจุดแรกที่ต่อสายใช้งานจริง** ตามที่ทิ้งไว้ใน
Phase 2 handoff) · `VectorId = Id` ของแถวเดียวกัน (DM-6) · validate `SessionQuestionIds` แบบ batched
query เดียว (ไม่ loop `Get()`) · enqueue `qna_index` เสมอตอนสร้าง · `CreateBy` เป็น `AdminUser.Id`
จริงจาก `CurrentUserId` (R5.6 — ระบบมี auth ครบแล้ว) · `UpdateAsync` implement QQ-6 เป๊ะ: เทียบ
`Question`/`Answer` เดิมกับใหม่แยกกัน → `Question` เปลี่ยน enqueue `qna_index` พร้อม `NeedsReEmbed=true`,
`Answer` เปลี่ยนอย่างเดียว enqueue พร้อม `NeedsReEmbed=false` (ข้าม embed call จริง) · `DeleteAsync`
implement QQ-5: soft delete `KnowledgeQnASource` ทุกแถวที่ชี้มาในทรานแซกชันเดียวกับการลบ Q&A (คำถาม
กลับเข้าคิวเองผ่าน QQ-1 ทันทีที่ commit) + enqueue `vector_delete`

**ส่วนขยาย interface ที่ทำเพิ่มนอกจากที่ระบุตรงๆ ใน DM-13 (technical decision ของ engineer เอง ไม่ใช่
business/schema)**: `IKnowledgeIndexProvider.UpdateMetadataAsync(namespaceKey, id, text, metadata)`
ใหม่ ใช้ Pinecone `/vectors/update` แบบ `setMetadata` ไม่ส่ง `values` — จำเป็นเพราะ QQ-6 สั่งให้ "ข้าม
embed call ได้ถ้า Question ไม่เปลี่ยน" แต่ยังต้องอัปเดตข้อความที่เก็บใน Pinecone (Answer เปลี่ยน) โดยไม่มี
vector ใหม่ให้ upsert เพราะระบบไม่ persist ตัวเลข float[] ของ vector ไว้ที่ไหนเลย (เหมือน
`DeleteVectorsAsync` ที่ Phase 3 เพิ่มไว้ก่อนหน้าด้วยเหตุผลคล้ายกัน) · `VectorDeleteJobPayload` เพิ่ม
`Kind` (`document`/`qna`, default `document` เพื่อ backward-compat กับ payload เก่า) เพราะ
`vector_delete` job type เดิมเป็นของ document โดยเฉพาะ (เช็ค `GetDeleted()` ก่อนลบเสมอ ตาม DI-16) แต่
Q&A ไม่มี restore path เลย (การลบถาวรตาม QQ-5) เช็คแบบเดียวกันจึงใช้ไม่ได้ — `PayloadJson` เป็น
free-form JSON อยู่แล้วตาม DM-10 ไม่มี schema บังคับรูปแบบ ไม่ใช่ contract change

**`BackgroundJobProcessor.ProcessQnaIndexAsync`** ใหม่ (`qna_index`, เดิมมีแค่ placeholder throw) —
อ่าน `QnaIndexJobPayload.NeedsReEmbed`: true → `EmbedAndUpsertAsync` ปกติ (มี `EmbedText=Question`,
`Text="ถาม: ...\nตอบ: ..."`, `Metadata={sourceType:"qna", qnaId}` ตาม KS-5) · false →
`UpdateMetadataAsync` ตรงๆ ไม่เรียก embedding provider เลย · ใช้ `DocumentIndexOutcome`/
`DocumentIndexingException`/`DocumentIndexingResultMapper` ชุดเดิมกับ document/lesson index (แค่
`embedding_failed`/`index_failed` เท่านั้นที่เกิดได้ ตรงตาม DM-6 comment) · `ProcessVectorDeleteAsync`
แก้ให้ branch ตาม `payload.Kind` — เฉพาะ `document` เท่านั้นที่เช็ค `GetDeleted()` ก่อนลบ (DI-16), `qna`
ลบตรงเพราะไม่มี restore ให้ชนกัน

**KS-7/KS-8/KS-9 (prompt ทั้ง Gemini และ OpenAI-compatible variant)** — ทั้งสอง variant ใช้
`BuildAnswerPrompt`/`GeminiAnswerJson` ร่วมกันใน `RagVoiceQuestionProvider.cs` (จุดเดียวที่ retrieval
เกิดขึ้นจริง) จึงแก้ที่เดียวครอบทั้งคู่ — **`GeminiVoiceQuestionProvider.cs` (full-deck, ไม่ RAG) ไม่ได้
แก้เลยเพราะไม่เคยยิง query เข้า Pinecone อยู่แล้ว โครงสร้างทำให้ Q&A content ไปไม่ถึง provider ตัวนี้ได้
ไม่ใช่ gap ที่ตกหล่น** · `BuildGroundingContextAsync` คืน `GroundingBlocks(DocumentBlock, QnaBlock)`
แยกตาม `metadata.sourceType` แทนสตริงเดียว, บล็อกเอกสาร/สไลด์มาก่อนเสมอพร้อมคำสั่งชัดว่ายึดบล็อกแรกเมื่อ
ขัดกัน (KS-7 — **บังคับได้แค่ระดับ prompt เท่านั้น ไม่ใช่โค้ด ตามที่ R-3 ยอมรับไว้แล้ว มี comment กำกับ
ข้อจำกัดนี้ในโค้ด**), ห้ามคัดลอกคำตอบ Q&A ตรงๆ + ตัวอย่างคำถามใกล้เคียงแต่คนละเรื่อง (KS-8) ·
structured output เพิ่ม `conflict: {qnaId, sourceLabel, note} | null` (KS-9) ผ่าน `GeminiConflictJson`
ใหม่ · fallback full-deck (ไม่มี retrieval) ไม่มีทาง Q&A ปนเข้าไปได้เพราะ Q&A มาจาก retrieval เท่านั้น

**KS-9/KS-10** — `VoiceQuestionService.AskAsync` เพิ่ม `TryRecordConflict` เรียกหลังบันทึก
`SessionQuestion` ทุกครั้งที่ `result.Conflict != null` — validate `qnaId` ผ่าน
`IKnowledgeQnARepository.Get()` (query filter กรอง cross-company ให้อัตโนมัติ = KS-10) ไม่เจอ →
log warning แล้วทิ้งธง ไม่ throw · บันทึกสำเร็จ → `KnowledgeQnAConflict` หนึ่งแถว `CreateBy=null`
(เกิดจากระบบ) · **ทั้งเมธอดอยู่ใน try/catch แยก — บันทึกธงล้มเหลวไม่มีทางทำให้คำตอบที่ตอบไปแล้วล้มเหลว**
ตรงตาม pattern "integration รองพังได้ ห้ามพัง flow หลัก" ที่ใช้ทั้งโปรเจกต์

**TX-5/TX-6/TX-10 เชื่อมค่าจริงแล้ว** ตามที่ Phase 1 ทิ้ง placeholder ไว้: `KnowledgeCategoryService`
เพิ่ม `IKnowledgeQnARepository` แล้วเปลี่ยน `qnaCount`/`LosingQnAs`/`GainingQnAs` จาก `0` คงที่เป็นค่า
จริงจาก `GetByScope(Category, id)` ในรอบเดียวกับ MG-F1 ตามที่ design.md สั่งไว้

**Endpoint ใหม่ 6 ตัว**: `GET /api/qna-queue` · `POST /api/knowledge-qna` ·
`PUT /api/knowledge-qna/{id}` · `DELETE /api/knowledge-qna/{id}` ·
`GET /api/knowledge-qna-conflicts?resolved=false` · `PUT /api/knowledge-qna-conflicts/{id}/resolve`
— ไม่มี explicit guard เพิ่มในตัว controller/service เพราะ `FallbackPolicy` (บังคับ auth ทุก endpoint
เป็นค่าเริ่มต้นอยู่แล้ว) + query filter ผ่าน `CurrentCompanyId` ครอบเพียงพอ ตรงกับ pattern เดิมของ
`KnowledgeCategoriesController`/`DocumentsController` (QQ-9 default "ทุกคนในบริษัทแก้/ลบของกันได้"
ไม่ต้องเช็ค `CreateBy` เพิ่ม)

**Unit test ใหม่**: `KnowledgeQnAServiceTests.cs` (9 tests) ครอบ QQ-1 ครบตามที่สั่ง — `NotFound` เข้าคิว
· `Incorrect` เข้าคิว · ทั้งสองพร้อมกันติดสองป้าย (QQ-3) · `OutOfScope`/`NoSpeech`/`TranscriptionFailed`
ไม่เข้าคิวเลย (`[Theory]` 3 กรณี) · มี `KnowledgeQnASource` ชี้มาแล้วไม่เข้าคิว (QQ-5 reverse case) ·
QQ-4 บอกบทเรียนต้นทางถูกต้อง + ข้ามหลายการเรียน/บทเรียนพร้อมกันได้

**Verify**: `dotnet build SupportRoom.slnx` = 0 warning/0 error · `dotnet test --filter
"Category!=Integration"` = **189/189 ผ่านทั้งหมด** (21 Providers + 167 Application + 1
IntegrationTests; baseline ก่อนหน้า 180/180 → เพิ่ม 9 test ใหม่ ไม่มี test ไหนถูกลบ) · migration
`AddKnowledgeQnA` apply กับ local Postgres จริงแล้ว (`supportroom-pg` container พอร์ต 5432 ตรงกับ
`.env`) ยืนยัน `\d "KnowledgeQnA"`/`"KnowledgeQnASource"`/`"KnowledgeQnAConflict"`/`"SessionQuestion"`
ตรง schema ที่คาดไว้ครบทุกคอลัมน์/index (index ใหม่บน `SessionQuestion` มีจริง ไม่มีคอลัมน์เพิ่มบน
`SessionQuestion` เอง) · `dotnet ef migrations has-pending-model-changes` = "No changes have been
made to the model"

**ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA" ไม่ใช่ "เสร็จยืนยันแล้ว"
· งาน `[frontend]` 4 ข้อท้าย Phase 6 checklist ยังไม่ได้ทำ รอ `frontend-engineer` (type `KnowledgeQnA`/
`KnowledgeQnAConflict` ใน `domain.ts`, `api-client.ts` methods, หน้าคิวรวม, หน้าเขียนคำตอบ,
หน้ารายการธงขัดแย้ง)

**จุดที่ควรให้ `qa-engineer`/`system-analyst` ตรวจเพิ่ม (บันทึกไว้ ไม่ได้ตัดสินใจเอง)**:
1. QQ-1's ครึ่งหลัง (ไม่มี `KnowledgeQnASource` ชี้มา) implement ที่ service layer ไม่ใช่ที่
   `ISessionQuestionRepository.GetReviewQueue()` โดยตรงตามที่ `plan.md` เขียนไว้ตัวอักษร ("เพิ่ม
   `GetReviewQueue(...)` ... implement QQ-1") — เป็นการตัดสินใจทางเทคนิคเพื่อความ testable และไม่ให้
   repository ของ module นี้ผูกกับตาราง Q&A โดยตรง ผลลัพธ์ทางพฤติกรรมเหมือนกันทุกกรณี (มี test ยืนยัน)
   แต่โครงสร้างโค้ดต่างจากคำบรรยายตรงๆ ใน plan.md
2. `IKnowledgeIndexProvider.UpdateMetadataAsync` เป็น method ใหม่ที่ไม่มีอยู่ใน DM-13 เดิม (DM-13 ปิด
   ไปแล้วตั้งแต่ Phase 3) — เป็นทางเลือกทางเทคนิคที่จำเป็นเพื่อทำ QQ-6 ให้ครบตามที่สั่ง ("ข้าม embed
   call ได้") ไม่มีทางเลือกอื่นที่ไม่เพิ่ม method ใหม่เพราะระบบไม่ persist vector float[] ไว้เลย
3. R-9 index ใหม่บน `SessionQuestion` (`(CompanyId, AnswerStatus)`, `(CompanyId, ReviewResult)`) —
   `learning-session` module ควรรับทราบว่ามีคนแตะ `OnModelCreating` ของ entity ที่ตัวเองเป็นเจ้าของ
   (ไม่ได้แก้ field ใดๆ ของ entity เอง แค่เพิ่ม index)

**Claude Code handoff — Phase 1 frontend — ✅ 4/4 งานเสร็จ พร้อม QA (2026-08-19)**: ทำครบ 4 งาน
`[frontend]` ของ Phase 1 checklist ใน `plan.md`:
1. type `KnowledgeCategory`/`KnowledgeScopeType`/`CreateKnowledgeCategoryInput`/
   `UpdateKnowledgeCategoryInput`/`CategoryMovePreview` ใน `src/types/domain.ts` ตรง
   `KnowledgeCategoryViewModel`/`CategoryMovePreviewViewModel`/`KnowledgeScopeType.cs` ที่อ่านจากโค้ด
   จริง (ไม่เดา field name) — เพิ่ม `LessonConfig.categoryId` (required) ด้วย เพราะ backend
   `LessonConfigViewModel`/`LessonConfigDto` บังคับ field นี้อยู่แล้วตั้งแต่ Phase 1 backend และ
   หน้าแก้บทเรียนเดิม (ที่ยังไม่มี field นี้) จะ POST ไม่ผ่าน validation ทันทีถ้าไม่เพิ่ม
2. เพิ่ม 6 เมธอดใน `src/lib/api-client.ts`: `listKnowledgeCategories`, `createKnowledgeCategory`,
   `updateKnowledgeCategory`, `deleteKnowledgeCategory`, `getCategoryMovePreview`,
   `moveLessonCategory` (endpoint หลังคือ `PUT /api/lessons/{id}/category` แยกจาก `saveLesson`)
3. หน้า `/admin/categories` ใหม่ (`src/app/admin/categories/page.tsx`) + component
   `CategoryTree.tsx`/`CategoryFormDialog.tsx` ใน `src/components/admin/` — list เป็นต้นไม้ 2 ระดับ,
   create หมวดใหญ่/หมวดย่อย, rename, delete พร้อมแสดง error message ภาษาไทยจาก TX-6 (นับแยก
   บทเรียน/เอกสาร/Q&A/หมวดย่อย) ตรงๆ · แถว `isSystemDefault` แสดงเสมอ (ไม่ซ่อน) พร้อม `Tooltip`
   อธิบายเหตุผลและปุ่มแก้/ลบถูกปิด (TX-11) · เพิ่มลิงก์ "จัดการหมวดความรู้" ใน `/admin/page.tsx`
4. dropdown เลือกหมวด (เฉพาะ Level 2) ใน `src/app/admin/lessons/[slug]/page.tsx` ผ่าน
   `CategoryMovePreviewDialog.tsx` ใหม่ — เปลี่ยนหมวดแล้วกด "บันทึก" จะเรียก move-preview ก่อนเสมอ,
   โชว์ตัวเลข 4 ค่า (`losingDocuments`/`losingQnAs`/`gainingDocuments`/`gainingQnAs`), ต้องกดยืนยัน
   ก่อนถึงเรียก `PUT /api/lessons/{id}/category` จริง แล้วค่อยไปต่อ general save ของฟิลด์อื่น —
   ไม่มี auto-save เงียบๆ (R3.1)

เพิ่ม shadcn component ใหม่ 2 ตัวผ่าน CLI: `select.tsx`, `tooltip.tsx` (ทั้งคู่ไม่เคยมีในโปรเจกต์มา
ก่อน) — wrap `TooltipProvider` ที่ `src/app/admin/layout.tsx` เพราะ `AdminGuard`/route ทุกหน้าอยู่ใต้
layout นี้อยู่แล้ว

**Verify ผ่านครบ**: `npm run typecheck` / `npm run lint` / `npm run test` (36/36) / `npm run build`
ผ่านหมด (Node 22) · **ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA"

**ค้างไว้ให้ QA/รอบถัดไปตรวจ (บันทึกไว้ ไม่ได้ตัดสินใจเอง)**:
1. `src/types/domain.ts` `DocumentResource`/`DocumentIndexingStatus` ยังเป็นรูปแบบเก่า
   (`lessonId`/ไม่มี `scopeType`/`scopeId`/`failureReason`/`willRetryAt`) ไม่ตรงกับ
   `DocumentResourceViewModel` จริงที่มีอยู่แล้วตั้งแต่ Phase 1/3 backend — **ไม่ได้แก้ในรอบนี้**
   เพราะอยู่นอกขอบเขต 4 งานของ Phase 1 `[frontend]` (เป็นงาน Phase 3 `[frontend]` ตาม
   `plan.md`/`status.md` เดิม ที่ยังค้างอยู่) แต่หมายความว่าหน้า `/admin/documents` และ
   `DocumentUploadList.tsx` วันนี้เรียก API เอกสารด้วย type ที่ผิด shape จริงอยู่ก่อนแล้ว
2. `LessonConfigInput`/`FormState` ของหน้าแก้บทเรียนตอนนี้บังคับส่ง `categoryId` เสมอ (มาจาก
   `LessonConfig` ที่แก้ไปข้อ 1) — เข้ากันได้กับ backend `LessonConfigDto.CategoryId` (required)
   แต่ยังไม่มีหน้า "สร้างบทเรียนใหม่" (`/admin/lessons/new`) จนกว่า Phase 5 `[frontend]` จะทำ — ตอนนี้
   บทเรียนใหม่สร้างได้เฉพาะผ่าน `POST /api/lessons` เดิม (upsert by slug) ซึ่งก็ต้องมี `categoryId`
   อยู่ดี ไม่ใช่ gap ใหม่ที่รอบนี้สร้างขึ้น

**Claude Code handoff — Phase 3/4/5/6 frontend — ✅ 10/11 งานเสร็จ พร้อม QA, 1 ข้อค้างเพราะ
schema gap (2026-08-19)**: ทำงาน `[frontend]` ที่เหลือทั้งหมดของ Phase 3/4/5/6 ใน `plan.md`
ในรอบเดียว โดยอ่าน backend ViewModel/Controller จริงก่อนเขียน type ทุกจุด (ไม่เดา field name):

1. **แก้ `src/types/domain.ts`** — `DocumentResource` เปลี่ยนจาก `lessonId` (รูปแบบเก่า ไม่มีใครใช้
   จริงแล้ว) เป็น `scopeType`/`scopeId`/`failureReason`/`willRetryAt` ตรง `DocumentResourceViewModel`
   จริง เพิ่ม `DocumentFailureReason` union (5 ค่า) และ `DocumentChunk` (DI-7) · เพิ่ม
   `LessonNarrationSlide`/`LessonNarrations` (NR-1/NR-5) · เพิ่ม `KnowledgeQnA`/
   `CreateKnowledgeQnAInput`/`UpdateKnowledgeQnAInput`/`KnowledgeQnAQueueItem`/`KnowledgeQnAConflict`
   ตรง `KnowledgeQnAViewModel`/`KnowledgeQnAQueueItemViewModel`/`KnowledgeQnAConflictViewModel`
   ที่อ่านจากโค้ดจริง — แก้ข้อค้างที่บันทึกไว้ในหัวข้อ "ค้างไว้ให้ QA" ข้อ 1 ด้านบนไปพร้อมกัน
2. **`src/lib/api-client.ts`** เพิ่ม 11 ฟังก์ชัน: `getLessonNarrations`/`saveLessonNarration`/
   `getLessonNarrationCount` (NR), `listDeletedDocuments`/`restoreDocument`/`getDocumentChunks` (DI),
   `getQnaQueue`/`createKnowledgeQnA`/`updateKnowledgeQnA`/`deleteKnowledgeQnA` (QQ),
   `listQnaConflicts`/`resolveQnaConflict` (QQ-10)
3. **Phase 3**: `DocumentUploadList.tsx` แสดง `willRetryAt` แยกบรรทัดจากข้อความล้มเหลว, แปล
   `failureReason` เป็นไทยตามสาเหตุ (R6.4, ไม่รวมเป็นข้อความเดียว) · หน้า `/admin/documents` เพิ่ม
   `Tabs` "เอกสารทั้งหมด"/"กู้คืนเอกสารที่ถูกลบ" ด้วย `DeletedDocumentsList.tsx` ใหม่ เรียก
   `POST /api/documents/{id}/restore` — **1 ข้อค้าง**: "UI แจ้งเตือนงานลบ vector ค้าง" (R-4/DI-16)
   ทำไม่ได้เพราะ backend ไม่มี field/endpoint ใดเลยที่บอกสถานะ `vector_delete` job ต่อเอกสาร (ไม่มี
   `BackgroundJob` controller, `DocumentResourceViewModel` มีแค่ `willRetryAt` ของ job
   `document_index` เท่านั้น) — บันทึกเป็น schema gap ไม่เดา field
4. **Phase 4**: หน้าใหม่ `/admin/documents/[id]/chunks` แสดงทุก chunk เรียง `seqNo`, ไฮไลต์แถว
   `hasSuspectCharacters` ก่อน (พื้นหลัง `bg-destructive/5` + badge), แสดง "แปลงไม่ได้" (ไม่ใช่หน้าว่าง)
   เมื่อไม่มี chunk เลย · ลิงก์ "ดูข้อความที่แปลงได้" จากแต่ละแถวใน `DocumentUploadList.tsx`
5. **Phase 5**: หน้าใหม่ `/admin/lessons/[slug]/narrations` (ซ่อน/ปฏิเสธ UI ทั้งหน้าถ้า
   `contentSourceType != "pdf"`, NR-9) แก้บทพูดต่อหน้าทีละหน้า พร้อม badge "แก้ไขแล้ว" (`isOverridden`)
   และ Alert เตือนเมื่อ `isLikelyScanned` · หน้าแก้บทเรียนเพิ่ม flow ยืนยันก่อนแทนที่ PDF เดิมด้วย
   `AlertDialog` (เรียก `getLessonNarrationCount` ก่อนอัปโหลดเมื่อบทเรียนมี `pdfDocumentResourceId`
   อยู่แล้วเท่านั้น — อัปโหลดครั้งแรกไม่มีอะไรให้เสียจึงข้าม dialog) · หน้าใหม่ `/admin/lessons/new`
   (P9/Q4 ขั้นต่ำตามที่ design.md สั่ง) ใช้ฟอร์มเดียวกับหน้าแก้ไข ต่างกันที่ PDF อัปโหลดแบบ standalone
   (ไม่มี `lessonSlug` ให้ resolve เพราะบทเรียนยังไม่มีในตอนนั้น) แล้วผูกผ่าน `pdfDocumentResourceId`
   ตอน `POST /api/lessons`
6. **Phase 6**: หน้าใหม่ `/admin/qna-queue` (ตาราง checkbox เลือกได้หลายแถว, badge
   "AI ไม่มีข้อมูล"/"CS ตรวจว่าตอบผิด" ตาม `fromNotFound`/`fromIncorrect`, ปุ่ม "เขียนคำตอบ (n)")
   คู่กับ `KnowledgeQnAAnswerDialog.tsx` (prefill คำถามจาก transcript, scope prefill เป็น `lesson`
   ของคำถามแรกที่เลือกแต่แก้ได้เสมอก่อนกด "บันทึกคำตอบ" — ไม่มี auto-save, QQ-8) · หน้าใหม่
   `/admin/qna-conflicts` แยกจากคิว (ไม่ใช่ badge) พร้อมปุ่ม "ปิดธง" เรียก
   `PUT /api/knowledge-qna-conflicts/{id}/resolve` · เพิ่มลิงก์ทั้งสองหน้าใน `/admin/page.tsx`

เพิ่ม shadcn component ใหม่ 4 ตัวผ่าน CLI: `textarea.tsx`, `tabs.tsx`, `alert.tsx`, `alert-dialog.tsx`
(ไม่เคยมีในโปรเจกต์มาก่อน)

**Verify ผ่านครบ**: `npm run typecheck` / `npm run lint` / `npm run test` (36/36) / `npm run build`
ผ่านหมด (Node 22) · **ไม่มีการติ๊ก checkbox ใดใน `plan.md`** ตามกฎ pipeline — สถานะนี้คือ "รอ QA"

**ค้างไว้ให้ QA/รอบถัดไปตรวจ**:
1. Phase 3 "UI แจ้งเตือนงานลบ vector ค้าง" ยังไม่ทำ — ต้องกลับไปที่ `system-analyst`/
   `backend-engineer` ก่อนเพื่อเพิ่ม field/endpoint ที่บอกได้ว่าเอกสารที่ถูกลบมี `vector_delete`
   job ค้างอยู่หรือไม่ (ปัจจุบันไม่มี `BackgroundJobsController` หรือ field ใดเลยที่ frontend
   อ่านได้) — ไม่ใช่งานที่ frontend ตัดสินใจเองได้ว่าจะแสดงอะไร
2. `/admin/lessons/new` ยังไม่ validate `Slug` ฝั่ง client ว่าห้ามขึ้นต้นด้วย `kbcat-`/เท่ากับ
   `kb-global` (TX-7) — พึ่ง server validation อย่างเดียว (backend ปฏิเสธแน่นอน แต่ error message
   จะโผล่หลังกดสร้างแทนที่จะเตือนตั้งแต่พิมพ์) ยอมรับได้เพราะ pattern เดียวกับฟอร์มอื่นในโปรเจกต์นี้
   ที่พึ่ง server validation เป็นหลัก ไม่ใช่ gap ใหม่

**`system-analyst` amendment — 2026-08-20 — ปิด QA issue ข้อ 2 (R3 เอกสารระดับหมวด)**

`design.md` ถูก amend แล้ว (ไม่ได้เขียนทับ) เพิ่ม:
- หัวข้อ contract ใหม่ **`## Document Scope Assignment Rules (R3 — write path)` DS-1..DS-12** —
  `UploadDocumentDto` ลบ `LessonSlug` ใช้ `ScopeType`/`ScopeId` แทน (`ScopeId` ของ `lesson` คือ
  **`LessonConfig.Id` ไม่ใช่ Slug**) · `EnsureValidScope` ต้องถูกเรียก**ก่อน**แตะ object storage ·
  `GET /api/documents?scopeType=&scopeId=` · `PATCH /api/documents/{id}/scope` (call site แรกของ KS-4)
  ประกอบจาก `vector_delete` + `document_index` เดิม **ไม่มี `BackgroundJobType` ใหม่** · scope picker
  ในหน้า `/admin/documents` เท่านั้น (หน้าบทเรียนคงเป็น `lesson` เสมอ)
- **Module G 🔒 Security gate** → เจ้าของโปรเจกต์เคาะให้เป็น **Phase 7 ใหม่** ไม่ยัดกลับ Phase 1
- R-14/R-15 · O-8/O-9 · 6 มติใหม่ (Q-A..Q-G) ในตารางการตัดสินใจ

**ไม่มี migration · ไม่มีฟิลด์ใหม่ · Data Model ไม่เปลี่ยนแม้ฟิลด์เดียว** — Phase 1–6 ไม่ต้องแก้
ย้อนหลัง · ฝั่ง DB additive ล้วน · **breaking เฉพาะ wire contract** ของ `POST`/`GET /api/documents`
(caller 3 จุด ไม่มี client ภายนอก) ต้องแก้ backend + `api-client.ts` + ทั้ง 3 caller ในเฟสเดียวกัน

**สาเหตุรากที่บันทึกไว้เป็น R-14**: KS-2 (`EnsureValidScope`) และ KS-4 ถูกเขียนถูกต้อง มี test ผ่าน
และผ่าน QA มาหกเฟส **โดยไม่เคยมีใครเรียกใช้จริงฝั่งเอกสาร** — `status.md` ของ Phase 2 บันทึกไว้เองว่า
"ยังไม่มี call site" แต่ไม่มีกลไกไหนพาข้อความนั้นไปเป็น task ใน `plan.md`

**`qa-engineer` — Phase 7 QA FULL round (2026-08-20) — ✅ presented for accept, 139/140 ครบทั้งโมดูล**:

ตรวจ Phase 7 (22 tasks) แบบ FULL จากศูนย์ + ยืนยันครั้งแรกของ 2 บั๊กที่พบระหว่างทดสอบ manual จริงวันนี้
(owner บริษัทเดียว login ไม่ได้ ใน `AdminSessionProvider.tsx`, Select ต้องเลือกสองรอบใน
`DocumentUploadList.tsx`/`KnowledgeQnAAnswerDialog.tsx`) ทุกอย่าง ✅ Verified — ไม่มี ❌/⚠️ เหลือใน
โมดูลนี้เลย รายละเอียดเต็มอยู่ใน `_docs/module/knowledge-base/review.md` (round ปัจจุบัน) และ
`review/phase-1-6.md` (Phase 1–6 เดิมที่ถูก archive ไปพร้อมรอบนี้)

ยืนยันด้วย: backend build 0/0 · test 204/204 (+14 จาก 190) · `dotnet ef migrations
has-pending-model-changes` clean (ไม่มี migration ใหม่ตรงตาม DS-11) · frontend typecheck/lint clean ·
test 36/36 · build clean (21 route) · **ทดสอบจริงกับแอปที่รันอยู่จริง** (ไม่ใช่แค่เชื่อ build/test เขียว
ตามบทเรียนที่บันทึกไว้จาก Phase 6): `curl` login จริง + `GET /api/companies` ยืนยันว่า
`owner@local.test` มีบริษัทเดียวจริง (ตรงเงื่อนไขบั๊ก 2) และยิง 6 กรณีปฏิเสธของ DS-3 ผ่าน backend
ที่รันจริงด้วย JWT จริง ได้ 400/404 ตรงทุกกรณี — **ข้อจำกัดที่บันทึกไว้ตรงๆ**: ไม่มี browser/
computer-use tool ในเซสชันนี้ให้คลิกทดสอบ owner login flow ในเบราว์เซอร์จริงด้วยตัวเอง ใช้ code trace
+ live data แทน (ผู้ใช้เองยืนยันด้วยตาตัวเองแล้วว่า auto-redirect ทำงานจริง)

Phase 1's TX-5 partial ปิดแล้ว (15/15 ✅) · Phase 7 เข้าเกณฑ์ deploy ได้ตาม round mode (FULL) แต่ยังติด
🔒 gate รอ `security` เหมือนทุก phase ที่ gate อยู่ · **`security` ยังไม่เคยรันสักครั้งตลอดทั้ง 7 phase** —
เป็นตัวบล็อกเดียวที่เหลือก่อนส่ง `devops` ได้ (Phase 1/5 ไม่ติด gate พร้อม accept ได้ทันที)

---

## learning-session

**1 ลิงก์ = หลายการเรียน แยกคนละคน** — ลิงก์คือสื่อการสอนที่ส่งลงกลุ่มไลน์ได้ ห้องเรียนเกิดตอนผู้ใช้
กดเข้าและระบุชื่อตัวเอง แต่ละคนเรียนตัวใครตัวมัน 1:1 · ผู้เรียนกรอกชื่อเองก่อนเข้าห้อง,
บันทึกความคืบหน้า/เวลาเคลื่อนไหว, แยก "ครบทุกสไลด์" ออกจาก "จบแล้ว", ปุ่มเรียนอีกครั้ง,
CS รีวิวคำตอบ AI ถูก/ผิด + หมายเหตุ

Docs: requirement ✅ · design ✅ (LS-QA-02 amended) · plan ✅ (50/53 checked) · review ⚠️
(**verified ⚠️ (FULL)**; 50/53 tasks ✅, 3 open pending manual browser test) · deploy.md ⚠️ (local-only
claim not yet independently re-verified by this QA round; not deployed to production regardless)

**QA FULL-3 (2026-08-19)** — full from-scratch re-verification of all 6 phases (53 tasks) run because
prior `review.md`/`deploy.md`/`review/*.md` content in the worktree could not be trusted as authored by
a real pipeline round. Independently re-checked Data Model DM-1..DM-8, all 4 contract sections
(LR/SR/RR/IC), and the 7 drift points `plan.md` flagged — all 7 confirmed as "implementation variant
that matches CA-1..CA-6", no code change needed. `dotnet ef migrations has-pending-model-changes` clean;
backend build 0/0, tests 149/149; frontend typecheck/lint clean, tests 36/36, build clean. Checked off
one previously-open task (Phase 3 request-logging/cache/HTTPS, confirmed in `Program.cs`). **3 tasks
remain unchecked** (Phase 4 two-browser realtime test; Phase 5 six-case LR-3 test and IC-7/Strict-Mode
test) — this session has no browser/computer-use tool, so these were traced through source (no
contradiction found) but not executed. Full detail/manifest in `review.md`; LS-QA-01/09 stay closed from
the prior TARGETED-2 round.

**Local Docker ready (ยังไม่ deploy)** — `docker-compose.yml` เปิด PostgreSQL 16 → one-shot EF
migration → ASP.NET API → standalone Next.js พร้อม persistent DB/log/storage volumes; services ทั้ง
สาม healthy ที่ PostgreSQL `localhost:55432`, API `http://localhost:5138` และ frontend
`http://localhost:3001` · migration ครบถึง `AddTotalSlideCount` และ rerun เป็น idempotent · owner
bootstrap/login, CORS และ anonymous admin redirect ผ่าน · backend non-integration tests 140/140,
frontend lint/typecheck/tests 36/36/build ผ่าน รายละเอียดอยู่ใน `deploy.md`

**UX/UI reminder**: งาน visual polish/final UI ยังรอทีม UX/UI ของผู้ใช้และอยู่นอก scope ของ local
infrastructure รอบนี้ ห้ามตีความ technical readiness ว่า UI sign-off เสร็จแล้ว

**Now**: `design.md` และ `plan.md` ผ่าน Contract Amendment เพื่อปิด `LS-QA-02` —
ยอมรับชื่อจริง `TrainingLink`/`LearningSession`, `RecipientName`, child `SessionId`,
`SessionStatus`/`LinkStatus`; public learner ใช้ `(token, learnerKey)` แล้ว server resolve session id;
คู่ดังกล่าวเป็น composite bearer credential ที่ห้าม log/cache/analytics และต้องผ่าน Security gate ·
migration contract คือ `20260813140603_SplitLinkAndAddAuth` +
`20260818155126_AddTotalSlideCount` ไม่มี schema/migration เพิ่ม ส่วน D2 ถามยืนยันก่อน resume,
F1–F8 และ business behavior เดิมไม่เปลี่ยน

F1–F8 ทำได้ทั้งหมดด้วย stack เดิม (ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR +
Next.js 15) **ไม่ต้องเพิ่ม dependency หรือ external service ใดๆ**

ยืนยันจากไฟล์จริง: **ระบบยังไม่เคย deploy**; ตอนนี้มี Dockerfile/Compose สำหรับ local แล้ว แต่ยัง
ไม่มี CI หรือ production deployment artifact และไม่ได้แตะ shared/production environment → ไม่มี
ข้อมูลลูกค้าจริง ทำให้ต้นทุนของการ rename/ลบตารางต่ำมาก และเป็นเหตุผลหลักที่ Q2/Q4 ถูกเคาะไปทางนี้

**Blocked on**: LS-QA-05 manual browser checks · LS-QA-08 Security gate · LS-QA-10 production
reverse-proxy/TLS/logging evidence · และ FULL round หลังปิด gates ก่อน deploy

**อัปเดต Backend 2026-08-19** — เพิ่ม `learnerCount`/`inProgressCount`/`endedCount` ที่ link
ViewModel (คง `learningSessionCount` ชั่วคราวเพื่อ compatibility) · validate `MaxAttendees >= 1`
เฉพาะตอนสร้างโดยยังไม่ enforce จำนวนผู้เรียน · `reviewResult = null` ล้าง result/note/time ครบและ
ตรวจ note สูงสุด 2000 ตัวอักษร · เพิ่ม tests ของ wrong `(token, learnerKey)` → 404 และ expired-link
progress/end/restart · เปลี่ยน request logging ไม่ให้เก็บ path/query/token/key พร้อม `no-store`,
`no-referrer`, HSTS · audit CS REST/SignalR แล้วพบว่า JWT fallback policy + company query filter +
agent hub guard ครบตาม contract ปัจจุบัน

Verification: targeted backend tests **44/44 ผ่าน** · API build **0 warning / 0 error** · full
Application suite **114 ผ่าน / 8 ล้ม** จาก provider credentials/fixture เดิมที่ environment นี้ไม่มี
(Google/Pinecone/Google Slides) ไม่ใช่สาม service ที่แก้ · `dotnet ef migrations
has-pending-model-changes` ตอบว่า model ตรง snapshot และสร้าง idempotent SQL ของ migration สองใบ
ตามลำดับได้สำเร็จโดยไม่เชื่อม/แก้ DB

LS-QA-09 backend fix ส่งมอบแล้ว: trim/validate ชื่อ 1–80 และ `LearnerKey` 8–128 ทั้ง DTO/service พร้อม
boundary tests · build 0 warning / 0 error และ non-integration tests 140/140 ผ่าน

**Migration/production hold**: ทดสอบ upgrade → rollback → upgrade และ backfill/repoint สำเร็จบน
isolated PostgreSQL 16 พร้อม demo cases แล้ว; local Compose fresh DB apply migrations ครบและ rerun
ตอบ up-to-date แต่ยังไม่เคย apply กับ shared/production environment ·
rollback ของ `SplitLinkAndAddAuth` เป็น lossy เมื่อหนึ่ง link มีหลาย learning rounds จึงต้อง backup
ก่อน apply environment จริง · production reverse proxy/load balancer ต้องยืนยันว่าไม่ access-log
path/full query และ terminate TLS ก่อนผ่าน Security/DevOps gate; protection ใน ASP.NET Core อย่างเดียว
รับประกันระบบภายนอก repo ไม่ได้

**อัปเดต Frontend** — ฟอร์มสร้างลิงก์มี `MaxAttendees` พร้อม validation และข้อความชัดเจนว่า
ยังไม่จำกัดจำนวนผู้เรียน · ตาราง/หน้ารายละเอียดลิงก์แสดง `learnerCount`/`inProgressCount`/
`endedCount` พร้อมสถานะลิงก์ · `LearnerKey` ใช้ key เดียว `supportroom.learnerKey` ข้ามลิงก์
สร้างด้วย `crypto.randomUUID()` เท่านั้นและตัด `Math.random()` fallback · ปุ่ม "เรียนอีกครั้ง"
จากหน้าสรุปกลับเข้า join flow เพื่อ prefill ชื่อเดิมให้แก้ก่อน Restart · review type/API/UI รับ `null`
และมีปุ่มล้างผลรีวิวซึ่งล้าง note/timestamp ตาม response จาก backend

Frontend verification: lint ผ่าน · typecheck ผ่าน · tests **34/34 ผ่าน** บน bundled Node v24
(เพิ่ม learner-key tests 3 เคส) · production build ผ่าน; มีเพียง warning เดิมเรื่อง Next.js พบ
หลาย lockfiles และเลือก workspace root ระดับ home

LS-QA-09 frontend fix ส่งมอบแล้ว: name input ใช้ limit 80, validate หลัง trim และเพิ่ม utility tests ·
lint/typecheck ผ่าน · tests 36/36 ผ่าน · production build ผ่าน

**อัปเดต 2026-08-18 (หลัง merge `Dev-gun/Gun`)** — โค้ดจริง implement F1–F8 ไปแล้วเกือบทั้งหมดด้วย
ชื่อ `TrainingLink`/`LearningSession` (**เจ้าของโปรเจกต์ตัดสิน 2026-08-18: ยึดชื่อตามโค้ด ไม่ rename
เป็น `LessonLink` ตามมติ Q2 เดิม** — โค้ดเขียนเสร็จก่อนที่ `design.md` จะถูกเขียน) · gap analysis
เทียบ design กับโค้ดจริงพบ 6 จุด **ปิดไปแล้วทั้ง 6**:
1. LR-4 progress หลังกดจบ เดิม throw → คืนค่าปัจจุบันเงียบ ๆ ตาม contract
2. LR-4 ตั้ง `CompletedAllSlides` ทันทีที่ถึงสไลด์สุดท้าย (เดิมตั้งตอนกดจบเท่านั้น)
3. LR-5 `CompletedAllSlides` เป็น OR ไม่ใช่ทับ
4. `LastSlideIndex` nullable + เขียนเฉพาะค่าที่ส่งมาจริง (เดิม 0 ทับของจริงได้)
5. `TotalSlideCount` เพิ่มใหม่ → CS เห็น "7/20" ตาม F4 (migration `20260818155126_AddTotalSlideCount`)
6. **LR-3 + LR-3a หน้ายืนยันก่อนเรียนต่อ (มติ D2)** — เพิ่ม `GET /api/learning-sessions/{token}/resume`
   + เขียนหน้า join ใหม่ครบ 6 กรณี + ปิด auto-resume เดิมที่ขัด IC-7 · การเข้าห้องต้องผ่าน
   one-shot grant ใน `sessionStorage` (ไม่ใช่ flag ถาวร ตาม LR-3a ข้อ 5)

QA รันซ้ำแล้ว: backend build 0 error (8 warnings เดิม) · tests 127/127 ผ่าน · frontend
lint/typecheck ผ่าน · tests 31/31 ผ่าน · production build ผ่าน · migration ทั้ง 2 ใบ apply แล้วเฉพาะ
fresh PostgreSQL ใน local Compose (ยังไม่ใช่ shared/production DB) และ manual
two-browser/LR-3/Strict Mode checks ยังไม่ได้รัน

**ค้างถัดไป**: เจ้าของโปรเจกต์ยกประเด็นใหม่ที่ยังไม่มี requirement — **ขาเข้าเอกสารและ
การออกแบบคลังความรู้ (document ingestion + knowledge base)** ต้องคุย req ก่อนออกแบบ
ยังไม่แตะโค้ดส่วนนี้

**Next**: ผู้ใช้เปลี่ยนรหัส owner ครั้งแรกและสร้าง Company/lesson/link test data (external provider
credentials ใน local `.env` ยังว่าง) แล้วเรียก `qa-engineer` ตรวจ manual LS-QA-05 บน stack ที่เปิดอยู่ ·
เรียก `security` แยกต่างหากตามคำขอผู้ใช้ · environment จริงต้องขออนุมัติเฉพาะเจาะจงก่อนเสมอ และ
หลังทุก gate ปิดต้องมี QA FULL ก่อนพิจารณา deploy

**🔒 Security gate ที่ PM ต้องติดใน `plan.md`**: phase ที่ครอบ **Module C** (learning lifecycle) ·
**Module D** (realtime/conversation re-pointing) · **Module E** (หน้า join/ยืนยันตัวตนผู้เรียน —
เพิ่มตามมติ 2026-08-18) · **Module F** (CS review) — เหตุผลตามที่ `design.md` วิเคราะห์ไว้:
- **C** — รับ input จากภายนอกที่ไม่ผ่าน auth (ชื่อผู้เรียน + `learnerKey`) · คู่
  `(TrainingLink.Token, LearnerKey)` เป็น **composite bearer credential** ในระบบที่ยังไม่มี learner auth ·
  การบังคับขอบเขตสิทธิ์ระหว่างผู้เรียน (IC-3) อยู่ที่นี่
- **D** — client ส่ง token/key แล้ว server resolve SignalR group key เป็น learning id +
  `voice-question` ใช้ contract เดียวกัน (CA-2/CA-3) ·
  ถ้าพลาด บทสนทนาของผู้เรียนคนหนึ่งจะ broadcast ไปหาทุกคนบนลิงก์เดียวกันโดยไม่มี error ให้เห็น
- **E** — เป็นจุดเดียวที่บังคับ **LR-3a/IC-7** ได้ (หน้ายืนยันก่อน resume ตามมติ D2) ·
  server แยกไม่ออกว่า resume ผ่านการยืนยันมาหรือยัง เพราะ `(token, learnerKey)` ถูกต้องทั้งสองทาง ·
  ถ้าหน้ายืนยันหายไป คนที่สองบนเครื่องที่ใช้ร่วมกันเห็นความคืบหน้าและคำถามของคนแรกแบบเงียบ
- **F** — หมายเหตุรีวิวเป็นข้อมูลภายในของ CS · โค้ดปัจจุบันมี JWT fallback policy ครอบ admin API
  แล้ว แต่ยังต้องให้ `security` audit authorization/company isolation ของ REST + SignalR จริง

**ไม่ใช่ด้วยเหตุผล PII** — ตาม F2 เก็บชื่ออย่างเดียว ไม่เก็บเบอร์/อีเมล/ตำแหน่ง เหตุผล PII
ถูกตัดออกโดยตั้งใจ ห้ามนำมาอ้างเป็นเหตุผลของ gate

หมายเหตุ: `docs/CORE_FEATURE_SPEC.md` §1 **ตรงกับ requirement ปัจจุบัน** · เอกสารนั้นเป็นบันทึก
การตัดสินใจเดิมของทีม เก็บไว้เป็นประวัติ ไม่แก้ ให้ยึด `_docs/module/learning-session/requirement.md`
เมื่อขัดกัน

หมายเหตุ: โปรเจกต์นี้ใช้ EF Core/PostgreSQL ไม่ใช่ Prisma — กฎ `schema.prisma` ใน conventions §7
ต้องอ่านเทียบเป็น EF migration + entity ของจริง

---

## ✅ คำถามค้างข้อสุดท้าย — ปิดแล้ว 2026-08-18

> **Module E ควรติด `🔒 Security gate` ไหม → ติด** (มติเจ้าของโปรเจกต์)

เหตุผล: หลังมติ D2 (ถามยืนยันก่อน resume เสมอ) Module E เป็นจุดเดียวที่บังคับ LR-3a/IC-7 ได้
พลาดแล้วรั่วเงียบโดยไม่มี error และ server ตรวจแทนไม่ได้ · `design.md` amend แล้ว 3 จุด
(หัวข้อ Module E ติด 🔒 · ช่อง Sensitive เพิ่มเหตุผล gate · บรรทัดส่งต่อ PM เป็น C, D, E, F)
**ไม่มี contract ส่วนใดเปลี่ยน**

**ผลต่อขั้นถัดไป:** `devops` deploy phase ที่ครอบ Module E ไม่ได้จนกว่า `security` จะ audit ·
`project-manager` ต้องติด gate ที่หัวข้อ phase ที่ครอบ Module E ด้วย

**ขั้นถัดไป:** ยึด routing จาก FULL QA ใน `review.md`; ข้อความ project-manager เดิมถูก supersede แล้ว

---

## company-admin

**รับลูกค้าใหม่เข้าระบบ + ปรับระบบให้เข้ากับลูกค้าแต่ละราย** — เกิดจากการทบทวนระบบผ่าน
UX wireframe หน้า admin แล้วเจอ 3 gap ที่ยืนยันด้วยการตรวจโค้ดจริง

Docs: requirement ✅ (2026-08-21) · design ✅ (**Module A เป็น contract แล้ว · F2/Module B–C ยังพัก**) · plan ✅ (2026-08-21, 2 phase, 22/22 tasks checked — checkbox เป็นของ QA) · review ✅ (QA TARGETED-1) · security ⚠️ (SECURITY-1, SEC-01–03 remediation implemented; re-audit pending)

- Phase 1 — implemented ✅ · verified ✅ (TARGETED, 15/15; ต้อง FULL ก่อน `devops`) · security ⚠️ (SEC-01–03 remediation implemented; re-audit pending) · deployed ⬜
- Phase 2 — implemented ✅ · verified ✅ (FULL, 7/7) · security ⚠️ (SEC-01–03 remediation implemented; re-audit pending) · deployed ⬜

> **🟢 design ปิดแล้วสำหรับ Module A / F1 (2026-08-21)** — trigger คือลูกค้าใหม่ **"scb"**
> · **F2 (ตั้งค่าระดับบริษัท) ยังพักไว้** เพราะทุกบริษัทรวม scb ใช้ค่ากลางจาก env ได้อยู่แล้ว
>
> **เคาะครบ 5 ข้อ**: **B1** สร้าง default chain อัตโนมัติให้บริษัทใหม่ + ซ่อมบริษัทเก่าทันที ·
> **A1** owner พิมพ์รหัสเอง + `MustChangePassword = true` · **B2** ลิงก์เดิมเรียนต่อจนหมดอายุ
> (ไม่แตะฝั่งผู้เรียน) · **N1** ปฏิเสธ slug ซ้ำ + แก้ข้อความ error ให้บอกเหตุผล · **N2** เพิ่มช่อง
> `AdminDisplayName` ในฟอร์ม
>
> **สิ่งที่ engineer ต้องอ่านก่อนเขียนโค้ด**: `## Company Provisioning Rules` (CP-1..CP-15) และ
> `## Default Category Chain Rules` (CH-1..CH-8) ใน `design.md` — เป็น contract ไม่ใช่คำแนะนำ
> · กับดักที่เขียนไว้ชัดแล้ว: **single-`Commit()`** (CP-6 ห้ามเรียก `AdminUserService.Create`
> ที่ commit เอง), **ห้าม `IgnoreQueryFilters()`** (CP-12), **`Role` ตายตัวเป็น `admin`** (CP-8),
> **`ON CONFLICT` ใช้แทนการเช็คไม่ได้ใน backfill** (CH-6)

**ทำไมเป็น module แยก ไม่ใช่พ่วง `knowledge-base`/`learning-session`** (มติเจ้าของโปรเจกต์
2026-08-21 ตาม `conventions.md` §1): ผู้ใช้คนละกลุ่ม (owner ของ School Bright / admin ของ
บริษัทลูกค้า ไม่ใช่ CS ที่ดูแลเนื้อหา) · business purpose คนละเรื่อง · ตัดทั้งก้อนได้โดยไม่กระทบ
สองโมดูลเดิม

**3 gap ที่ยืนยันจากโค้ดจริง**:
1. **P1 สร้างบริษัทไม่มี UI** — `POST /api/companies` (`CompanyController.cs:26`) และ
   `createCompany()` (`api-client.ts:559`) มีครบทั้งคู่ แต่ grep ทั้ง `frontend/src` แล้ว
   **ไม่มีไฟล์ใดเรียกเลย** วันนี้ลูกค้าใหม่เข้าระบบได้ทางเดียวคือ insert DB ตรงๆ
2. **P2 ไม่มีตั้งค่าระดับบริษัท** — ไม่มี entity `CompanySettings`/`CompanyConfig` ที่ไหนเลย ·
   `DEFAULT_SESSION_EXPIRY_HOURS` = `ServerDefaults.cs:46`, `EDGE_TTS_VOICE/RATE` =
   `ServerDefaults.cs:271-273` ทุกตัวเป็น env ระดับ deployment เดียวใช้ร่วมทุกบริษัท
3. **P3 (เจอเพิ่มระหว่างสัมภาษณ์) — ปิดแล้วใน Phase 1**:
   `CreateDefaultChain()` สร้าง chain ให้บริษัทใหม่ใน transaction เดียว และ migration เดิมถูก apply
   พร้อม CH-3 invariant ผ่าน · QA FULL-1 พบว่ากรณีเติม leaf ใต้ parent เดิมใช้ `CreateDate` ไม่ตรง
   CH-2/CH-6; corrective data-only migration ซ่อมเฉพาะ leaf กลุ่มนั้น และ QA TARGETED-1 ยืนยัน
   contract/test/EF discovery แล้ว (15/15) · **นี่คือ hard dependency ข้ามโมดูลไปที่ `knowledge-base`**

**เคาะแล้ว 2026-08-21**: F1 ฟอร์มเดียวจบ (Company + AdminUser คนแรก role `admin` + default
category chain สำเร็จพร้อมกันหรือไม่เกิดอะไรเลย, `owner` เท่านั้น) · F2 ตั้งค่า **3 ข้อเท่านั้น**
(ลิงก์หมดอายุ · เสียง+ความเร็ว TTS · ชื่อ/โลโก้/สีแบรนด์ที่ผู้เรียนเห็น) แบบ **null = inherit
จาก env** สิทธิ์แก้ = owner + admin ของบริษัทนั้น · ลำดับ: F1 ก่อน F2

**ตัดออกโดยตั้งใจ (DC-1..DC-5)**: แจ้งเตือนเชิงรุกอีเมล/LINE/สรุปรายวัน (in-app badge มีอยู่แล้ว
จริงที่ `AdminSidebar.tsx:130` → ปิด requirement ด้วยของเดิม) · จังหวะการสอน 3 ค่า ms ·
ขนาดไฟล์อัปโหลด · เกณฑ์หยุดกลางคัน · default จำนวนคนสูงสุดต่อลิงก์

**✅ open questions ของ Module A เคาะครบแล้ว (2026-08-21)** — A1 · B1 · B2 · N1 · N2 ปิดหมด
บันทึกอยู่ใน `design.md` ตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" พร้อมสิ่งที่แต่ละคำตอบตัดออก ·
⏸️ **ยังเหลือ 6 ข้อของ F2 (A2–A6 · B4 · B3)** ที่ **ไม่บล็อกอะไรตอนนี้** · **B4 (รูปร่าง schema
ของค่าตั้งค่า) ไม่ใช่ตัวบล็อกอีกต่อไป** เพราะเป็นเรื่อง F2 ล้วนๆ และ F1 ไม่ต้องแก้ schema เลย ·
**A1 (= OQ-1) ไม่ได้บล็อกหนักอย่างที่ประเมินไว้ตอนแรก**: ตรวจโค้ดแล้วพบว่า
ข้อเสนอเดิม (owner ตั้งรหัสในฟอร์ม + `MustChangePassword`) **เป็นสิ่งที่ระบบทำอยู่แล้วครบทุกขั้น**
(`IAdminUserService.cs:80`, `IAuthService.cs:186`, `/admin/change-password`, test ที่
`AdminUserServiceTests.cs:277`) เลือกทางนี้ = ไม่ต้องสร้างกลไกใหม่เลย

**✅ ผลสำรวจ feasibility (2026-08-21) — ใช้ได้เลยไม่ต้องตรวจซ้ำ**: **ทุกฟีเจอร์ F1.1–F2.4
ทำได้ด้วย stack ปัจจุบัน ไม่ต้องเพิ่ม dependency/provider ใหม่แม้แต่ตัวเดียว** (แม้แต่ทาง
อัปโหลดโลโก้ก็ใช้ `IDocumentStorageProvider` เดิม และค่า TTS ต่อบริษัทไม่ต้องแก้ `ITtsProvider`
เพราะ `TtsInput.Voice`/`Rate` เป็น optional override อยู่แล้ว) — สิ่งที่บล็อกคือคำถามที่ยัง
ไม่ถูกเคาะ ไม่ใช่ความเป็นไปได้ทางเทคนิค

**🔍 findings จากโค้ดจริง 7 ข้อ (F-1..F-7) เก็บครบใน `design.md` §Findings from Feasibility Check**
— อ้างไฟล์+บรรทัดไว้ทุกจุด ไม่ต้อง grep ซ้ำ สรุปหัวข้อ:
1. **F-1** `GET /api/companies` คืนเฉพาะบริษัท active (`ICompanyService.cs:45`) → **เปิดบริษัท
   ที่ปิดไปแล้วกลับมาไม่ได้ผ่าน UI** F1.6 ต้องมี endpoint ใหม่ ไม่ใช่ reuse ของเดิมอย่างที่
   `requirement.md` §Pre-existing assets เขียนไว้
2. **F-2** `IsActive = false` **ไม่ได้บล็อกฝั่งผู้เรียนเลย** — `IAuthService.cs:105-117` บล็อกแค่
   login ของ admin/cs ส่วน join/TTS/voice-question/`GET /api/training-links/{token}` ไม่เคยเช็ค
   → "offboard ลูกค้า" วันนี้แปลว่าลิงก์ที่แจกไปแล้วยังเรียนได้จนหมดอายุ **เป็นกฎธุรกิจที่ยังไม่เคาะ (B2)**
3. **F-3 — ปิดแล้วใน Phase 1**: migration เดิมครอบไม่ครบ แต่
   `BackfillMissingDefaultCategoryChain` ครอบทุกบริษัทและ apply กับ local PostgreSQL แล้ว;
   integration invariant check ยืนยันว่าแต่ละบริษัทมี system-default leaf หนึ่งแถวเป๊ะ
4. **F-4** `POST /api/tts` เชื่อ `Voice`/`Rate` จาก client ตรงๆ (`TtsController.cs:20-34` +
   `ITtsService.cs:23`) → ถ้าจะบังคับค่าต่อบริษัทในอนาคต **ต้อง resolve ที่ server** โดยคงกรณี
   per-utterance override ของ filler ไว้ (`config/response-texts.ts:57-62`)
5. **F-5** `KnowledgeCategory` มี query filter ตาม company context ปัจจุบัน
   (`ApplicationDbContext.cs:136`) → **owner ที่ switch อยู่บริษัทอื่นจะอ่านแถวของบริษัทที่เพิ่ง
   สร้างไม่เจอ** (insert ผ่าน แต่อ่านกลับได้ 0 แถว) — กับดักที่ engineer ต้องรู้ล่วงหน้า และ
   **ห้ามแก้ด้วย `IgnoreQueryFilters()`** ซึ่งเคยทำให้เกิด data leak มาแล้ว (`CompanyIsolationTests.cs:211-214`)
6. **F-6** `CompanyIsolationTests.EveryEntityIsCompanyScoped` (`CompanyIsolationTests.cs:227-251`)
   จะ fail ทันทีถ้าเพิ่ม entity ที่ `ICompanyScoped` โดยไม่มี query filter → กระทบทางเลือก B4 โดยตรง
7. **F-7** `.claude/agents/backend-engineer.md` §Auth ล้าสมัย (เขียนว่ายังไม่มี auth แต่ของจริง
   มี JWT + RBAC + `IAuthorizationGuard` ครบ) — `system-analyst` แก้ไฟล์นั้นไม่ได้ตาม
   `conventions.md` §9 ควรให้ `backend-engineer` แก้ในรอบที่แตะ backend ครั้งถัดไป

**Security note สำหรับ `project-manager`/`security` ในอนาคต**: งานทั้งโมดูลแตะ `Company` และ
`AdminUser` ซึ่งเป็นสองตารางที่ **ไม่มี query filter โดยตั้งใจ** (tenant registry เอง) —
`IAuthorizationGuard` คือสิ่งเดียวที่กั้นข้อมูลข้ามลูกค้า (TD-014) จุดนี้ควรติด `🔒 Security gate`

**Scope ปิดแล้ว 2026-08-21** (ถามรอบปิดท้ายครบ เจ้าของโปรเจกต์ยืนยันว่าเท่านี้): ไม่สร้างหน้า
จัดการผู้ใช้ใหม่ ใช้ `/admin/users` เดิม · **แพ็กเกจ/โควตา/สัญญา/usage ยังไม่คิด ห้ามออกแบบเผื่อ**
(ยอมรับความเสี่ยงว่าอนาคตอาจต้องแตะ `Company` ซ้ำ) · หน้า audit log ยังไม่ต้องมี

**Now**: `backend-engineer` implement remediation ของ **SEC-01–SEC-03** แล้ว: ทุก authenticated
back-office request refresh account/role/company state จาก server และ fail closed เมื่อ account/company
ไม่ active หรือเปลี่ยน assignment · `MustChangePassword` อนุญาตเฉพาะ `/api/auth/me` และ
`/api/auth/change-password` ที่ server boundary · `POST /api/auth/login` ถูกจำกัดด้วย ASP.NET Core
source-IP policy และ normalized-account short-lived policy พร้อม 429 error envelope ที่ไม่ reveal
account. Build 0 warning/0 error · focused security regression 13/13 · non-integration tests 228/228
· frontend typecheck ผ่าน. **Security gate ยัง open** จนกว่า `security` จะ re-audit และเป็นผู้ปิด
finding เอง; QA คงเดิม: Phase 1 15/15 TARGETED (ต้อง FULL ก่อน devops), Phase 2 7/7 FULL.

**Blocked on**: ผู้ใช้เรียก `security` re-audit เพื่อตรวจและปิด/เปิด SEC-01–SEC-03; หลัง security
ผ่าน Phase 1 ยังต้อง QA FULL ก่อนส่ง `devops` เพราะผลล่าสุดเป็น TARGETED

**F2 / Module B–C ยังพักไว้**: คำถาม A2–A6/B3/B4 ยังเปิดอยู่แต่ **ไม่บล็อก Module A** ·
trigger ที่จะปลุก = scb (หรือลูกค้ารายอื่น) ขอแบรนด์/เสียง/อายุลิงก์เป็นของตัวเอง ·
วันปลุกต้อง re-run STATE: ANALYZE ของส่วนนั้นใหม่ตามกฎ deferred module

**F2 / Module B ยังพักไว้**: คำถาม A2–A6/B4 ไม่ต้องตอบรอบนี้ · trigger ที่จะปลุก = scb (หรือ
ลูกค้ารายอื่น) ขอแบรนด์/เสียง/อายุลิงก์เป็นของตัวเอง · วันปลุกต้อง re-run STATE: ANALYZE
ของส่วนนั้นใหม่ตามกฎ deferred module
