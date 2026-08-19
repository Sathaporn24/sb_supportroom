# Project Status

## Scaffold
Already scaffolded — this is an existing project (`frontend/` Next.js + `backend/` ASP.NET Core), not a fresh `setup` run. See root `CLAUDE.md` for the real architecture; the pipeline's `setup` agent does not need to run here.

## Modules

| Module | Stage | Next agent |
|---|---|---|
| knowledge-base | **เก็บ requirement (ยังไม่ครบ)** — R1–R4 เคาะแล้ว 2026-08-19 · R5/R6 ยังไม่คุย | **ยังไม่ส่งต่อ** — คุย R5/R6 ให้จบก่อน |
| learning-session | **Design ยืนยันแล้ว** — เจ้าของโปรเจกต์เคาะครบ 6 จุด (Q2/Q3/Q4 + D1/D2/D3) เมื่อ 2026-08-18 · schema confirmed | **project-manager** (เขียน `plan.md`) |

## knowledge-base

**ขาเข้าสื่อการสอน + คลังความรู้ที่มีคนดูแลได้** — เกิดจากเจ้าของโปรเจกต์ยกประเด็นว่าระบบ
"ยังไม่ครบวงจร": ครึ่งซ้าย (เตรียมความรู้ → แจกลิงก์ → เรียน → จับคำถาม) ทำงานได้แล้ว
แต่ครึ่งขวา (รีวิว → แก้ความรู้ → รู้ว่าดีขึ้น) ตัน

Docs: requirement 🔵 (เก็บอยู่) · design ⬜ · plan ⬜

**เคาะแล้ว 4 ข้อ**: R1 taxonomy 3 ชั้น (category > subcategory > ชื่อเนื้อหา ใช้กับบทเรียนและเอกสาร) ·
R2 แต่ละบริษัทจัดหมวดเอง ไม่ใช่ชุดกลางของ School Bright · R3 คลังความรู้ 3 ระดับ
(บทเรียน/หมวด/ทั้งบริษัท) · R4 บทพูด — Google Slides แก้ที่ต้นทาง, PDF มีช่องแก้ในระบบ
prefill จากข้อความที่ดึงได้ เก็บเฉพาะหน้าที่แก้ อัปไฟล์ใหม่ล้างทิ้ง ไม่ทำ OCR และต้อง re-index

**ค้าง 2 ข้อ**: R5 รีวิวต้องเดินต่อได้ (คิวงาน + เหตุผลที่พิสูจน์แล้ว + ภาพรวมข้ามการเรียน) ·
R6 ความน่าเชื่อถือของขาเข้า (เห็นผลการแปลงก่อนใช้ · ลบเอกสารต้องลบ vector · คิวงานไม่หายเมื่อ restart)

**อย่าเริ่ม `system-analyst`** จนกว่า R5/R6 จะเคาะ — R5 กระทบ schema ของ `SessionQuestion`
และ R6 กระทบ `DocumentResource` + คิวงาน ออกแบบก่อนจะต้องรื้อ

หมายเหตุ: `docs/KNOWLEDGE_ROADMAP.md` เป็น roadmap เชิงเทคนิคของ retrieval/eval (K0–K4)
คนละชั้นกับ requirement นี้ ไม่ทับกัน — เอกสารนั้นตอบ "ทำให้ retrieval ดีขึ้นอย่างไร"
เอกสารนี้ตอบ "ใครดูแลความรู้และทำงานวันต่อวันยังไง"

---

## learning-session

**1 ลิงก์ = หลายการเรียน แยกคนละคน** — ลิงก์คือสื่อการสอนที่ส่งลงกลุ่มไลน์ได้ ห้องเรียนเกิดตอนผู้ใช้
กดเข้าและระบุชื่อตัวเอง แต่ละคนเรียนตัวใครตัวมัน 1:1 · ผู้เรียนกรอกชื่อเองก่อนเข้าห้อง,
บันทึกความคืบหน้า/เวลาเคลื่อนไหว, แยก "ครบทุกสไลด์" ออกจาก "จบแล้ว", ปุ่มเรียนอีกครั้ง,
CS รีวิวคำตอบ AI ถูก/ผิด + หมายเหตุ

Docs: requirement ✅ · design ✅ (ยืนยันครบ 2026-08-18) · plan ⬜

**Now**: `design.md` **ผ่านการยืนยัน schema จากเจ้าของโปรเจกต์แล้วเมื่อ 2026-08-18** —
ทั้ง 6 จุดที่เคยค้างเคาะตรงตามข้อเสนอทุกข้อ: **Q2** rename `TrainingSession` → `LessonLink` ·
**Q3** ตารางใหม่ = `LearningSession` · **Q4** ลบ `SessionSummary` ทั้งใบ (13 จุด) ·
**D1** เปลี่ยน route/TS type ตามชื่อใหม่ (`/api/links`, `/api/learning-sessions`) ·
**D2** ถามยืนยันก่อน resume เสมอ ห้าม resume เงียบๆ · **D3** migrate ข้อมูล demo ด้วย backfill SQL
→ Data Model + contract 4 ชุด (Learning Lifecycle · Progress & Stalled · Review ·
Isolation & Credential) + API/SignalR delta + migration เดียวพร้อม backfill **เป็น contract
ที่ implement ได้ทันที ไม่มีส่วนใดรอคำตอบ**

F1–F8 ทำได้ทั้งหมดด้วย stack เดิม (ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR +
Next.js 15) **ไม่ต้องเพิ่ม dependency หรือ external service ใดๆ**

ยืนยันจากไฟล์จริง: **ระบบยังไม่เคย deploy** (ไม่มี Dockerfile/CI, roadmap Phase 1 ยังไม่เริ่ม)
→ ไม่มีข้อมูลลูกค้าจริง ทำให้ต้นทุนของการ rename/ลบตารางต่ำมาก และเป็นเหตุผลหลักที่ Q2/Q4
ถูกเคาะไปทางนี้

**Blocked on**: ไม่มี — คำถามค้างข้อสุดท้าย (gate ของ Module E) เจ้าของโปรเจกต์เคาะแล้วเมื่อ 2026-08-18 → **ติด gate** · `design.md` amend เรียบร้อย

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

ตรวจแล้ว: backend build 0 error · test 127 ผ่าน · frontend typecheck/lint/test 31/build ผ่าน ·
**migration ทั้ง 2 ใบยังไม่เคย apply กับ DB จริง** (เครื่องพัฒนาไม่มี Postgres)

**ค้างถัดไป**: เจ้าของโปรเจกต์ยกประเด็นใหม่ที่ยังไม่มี requirement — **ขาเข้าเอกสารและ
การออกแบบคลังความรู้ (document ingestion + knowledge base)** ต้องคุย req ก่อนออกแบบ
ยังไม่แตะโค้ดส่วนนี้

**Next**: `project-manager` เขียน `plan.md` จาก `design.md` (Module A → B, C → D → E, F ·
A ต้องเสร็จก่อนทุกอย่าง และห้ามแบ่ง Module A ครึ่งเดียว มิฉะนั้น codebase จะมีทั้ง
`TrainingSession` และ `LessonLink` ปนกัน — R3)

**🔒 Security gate ที่ PM ต้องติดใน `plan.md`**: phase ที่ครอบ **Module C** (learning lifecycle) ·
**Module D** (realtime/conversation re-pointing) · **Module E** (หน้า join/ยืนยันตัวตนผู้เรียน —
เพิ่มตามมติ 2026-08-18) · **Module F** (CS review) — เหตุผลตามที่ `design.md` วิเคราะห์ไว้:
- **C** — รับ input จากภายนอกที่ไม่ผ่าน auth (ชื่อผู้เรียน + `learnerKey`) · `LearnerKey` และ
  `LearningSession.Id` เป็น **bearer credential ใหม่ 2 ตัว** ในระบบที่ยังไม่มี auth ·
  การบังคับขอบเขตสิทธิ์ระหว่างผู้เรียน (IC-3) อยู่ที่นี่
- **D** — SignalR group key เปลี่ยนจาก token เป็น learning id (IC-5) + `voice-question` (IC-6) ·
  ถ้าพลาด บทสนทนาของผู้เรียนคนหนึ่งจะ broadcast ไปหาทุกคนบนลิงก์เดียวกันโดยไม่มี error ให้เห็น
- **E** — เป็นจุดเดียวที่บังคับ **LR-3a/IC-7** ได้ (หน้ายืนยันก่อน resume ตามมติ D2) ·
  server แยกไม่ออกว่า resume ผ่านการยืนยันมาหรือยัง เพราะ `X-Learner-Key` ถูกต้องทั้งสองทาง ·
  ถ้าหน้ายืนยันหายไป คนที่สองบนเครื่องที่ใช้ร่วมกันเห็นความคืบหน้าและคำถามของคนแรกแบบเงียบ
- **F** — หมายเหตุรีวิวเป็นข้อมูลภายในของ CS แต่ `/admin/*` และ `/api/*` **ยังเปิดสาธารณะ**
  (TD-002) ใครเดา endpoint ได้ก็อ่าน/เขียนรีวิวได้

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

**ขั้นถัดไป:** `project-manager` เขียน `plan.md` จาก `design.md` (ลำดับ A → B, C → D → E, F ·
A ต้องเสร็จเป็นก้อนเดียว ห้ามแบ่งครึ่ง — R3)
