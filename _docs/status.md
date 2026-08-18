# Project Status

## Scaffold
Already scaffolded — this is an existing project (`frontend/` Next.js + `backend/` ASP.NET Core), not a fresh `setup` run. See root `CLAUDE.md` for the real architecture; the pipeline's `setup` agent does not need to run here.

## Modules

| Module | Stage | Next agent |
|---|---|---|
| learning-session | **Design ยืนยันแล้ว** — เจ้าของโปรเจกต์เคาะครบ 6 จุด (Q2/Q3/Q4 + D1/D2/D3) เมื่อ 2026-08-18 · schema confirmed | **project-manager** (เขียน `plan.md`) |

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

**Blocked on**: 1 ข้อ — มติ `🔒 Security gate` ของ Module E (ดูหัวข้อ ⏸ ท้ายไฟล์) · ไม่บล็อกการเริ่ม `project-manager` แต่ต้องตอบก่อนปิด `plan.md`

**Next**: `project-manager` เขียน `plan.md` จาก `design.md` (Module A → B, C → D → E, F ·
A ต้องเสร็จก่อนทุกอย่าง และห้ามแบ่ง Module A ครึ่งเดียว มิฉะนั้น codebase จะมีทั้ง
`TrainingSession` และ `LessonLink` ปนกัน — R3)

**🔒 Security gate ที่ PM ต้องติดใน `plan.md`**: phase ที่ครอบ **Module C** (learning lifecycle) ·
**Module D** (realtime/conversation re-pointing) · **Module F** (CS review) — เหตุผลตามที่
`design.md` วิเคราะห์ไว้:
- **C** — รับ input จากภายนอกที่ไม่ผ่าน auth (ชื่อผู้เรียน + `learnerKey`) · `LearnerKey` และ
  `LearningSession.Id` เป็น **bearer credential ใหม่ 2 ตัว** ในระบบที่ยังไม่มี auth ·
  การบังคับขอบเขตสิทธิ์ระหว่างผู้เรียน (IC-3) อยู่ที่นี่
- **D** — SignalR group key เปลี่ยนจาก token เป็น learning id (IC-5) + `voice-question` (IC-6) ·
  ถ้าพลาด บทสนทนาของผู้เรียนคนหนึ่งจะ broadcast ไปหาทุกคนบนลิงก์เดียวกันโดยไม่มี error ให้เห็น
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

## ⏸ ค้างรอเจ้าของโปรเจกต์ตัดสิน (บันทึกไว้ 2026-08-18 ก่อนย้ายเครื่อง)

**คำถามเดียวที่ค้าง — ตอบแล้วเดินต่อได้ทันที:**

> **Module E (หน้า join / หน้ายืนยันตัวตน) ควรติด `🔒 Security gate` ด้วยไหม?**
> ตอนนี้ `design.md` ตั้ง gate ไว้ที่ Module C / D / F เท่านั้น

**บริบทที่ต้องรู้เพื่อตอบ** — `system-analyst` ชี้ว่าหลังมติ D2 (ถามยืนยันก่อน resume เสมอ)
**Module E กลายเป็นจุดเดียวที่บังคับกฎ LR-3a/IC-7 ได้** เพราะฝั่ง server แยกไม่ออกว่า resume นั้น
ผ่านหน้ายืนยันมาหรือยัง — `X-Learner-Key` ถูกต้องทั้งสองทาง ถ้าหน้ายืนยันหายไปตอน implement
ผลคือคนที่สองบนเครื่องที่ใช้ร่วมกัน (คอมกลางในโรงเรียน) จะเห็นความคืบหน้าและคำถาม-คำตอบ
ของคนแรก **โดยไม่มี error ให้เห็น** · ปัจจุบัน SA เขียนไว้ในช่อง Sensitive ของ Module E แล้ว
แต่ยังไม่ได้ติด 🔒 เพราะรอมติ

| ทางเลือก | ผลที่ตามมา |
|---|---|
| **ติด gate ให้ Module E ด้วย** | `devops` จะ deploy phase นั้นไม่ได้จนกว่า `security` จะ audit — กันจุดที่พลาดแล้วรั่วเงียบ |
| **ไม่ติด คงไว้แค่ C/D/F** | พึ่ง QA ทดสอบด้วยมือตามที่ SA เขียนไว้แทน (IC-7 ระบุไว้แล้วว่าเป็นเคสที่ต้องทดสอบมือ) |

**ถ้าตอบว่า "ติด"** → เรียก `system-analyst` amend `design.md` เพิ่มบรรทัดเดียวที่ Module E
แล้วบอก `project-manager` ให้ติด gate กับ phase ที่ครอบ Module E ด้วย
**ถ้าตอบว่า "ไม่ติด"** → ไม่ต้องแก้เอกสาร เดินต่อไป `project-manager` ได้เลย

**ขั้นถัดไปหลังตอบ:** `project-manager` เขียน `plan.md` (ดูหัวข้อ Next + Security gate ด้านบน)
