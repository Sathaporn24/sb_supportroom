# Project Status

## 🔄 เซสชันก่อนหน้าหยุดตรงนี้ (2026-08-24) — อ่านหัวข้อนี้ก่อนอย่างอื่นทั้งหมด

**บริบท**: ผู้ใช้กำลังจะเปิดเซสชันใหม่เพราะ context เดิมใกล้เต็ม งานที่ทำค้างอยู่คือ QA รอบที่ 3
ของ `learning-session` Phase 7-9 (ดูรายละเอียดเต็มที่ `## learning-session` ด้านล่าง) — session
ใหม่จะได้รับ **ผลตรวจจาก Codex (เครื่องมือ QA ภายนอก ไม่ใช่ `qa-engineer` ในระบบ)** วางไว้ในแชท
ให้ประมวลผลต่อ

**สิ่งที่ทำไปแล้วก่อนตัดเซสชัน**:
1. Phase 7 (backend: ช่องพิมพ์ถาม AI + ตัด readiness-by-voice), Phase 8 (รื้อฟีเจอร์แชต CS ทิ้ง
   ทั้ง stack), Phase 9 (responsive + หน้าตาช่องพิมพ์ใหม่) — implement ครบทั้งสามแล้ว
2. QA รอบ 1 (Codex): พบ 3 Important + 3 Minor (QA) + 2 Important (Security) → แก้ครบ
3. QA รอบ 2 (Codex, targeted re-check): 4 จาก 8 ข้อยังมี gap จริง (QA-02, QA-03, QA-04, QA-06,
   SEC-01) → แก้ครบอีกรอบ (SEC-01 เปลี่ยนจาก text-fence เป็น Gemini `systemInstruction` แยกช่องทาง
   จริง — แข็งแรงกว่าเดิมมาก, QA-02 เพิ่มเช็ค token expiry/`RefreshCurrentUser()` ใน SignalR hub,
   QA-04 เจอไฟล์เอกสารตกค้างเพิ่มอีกรวม 8 ไฟล์ที่ agent รอบก่อนอ้างว่า "grep ครบแล้ว" แต่ไม่จริง)
4. **QA รอบ 3 (Codex, targeted re-check) — ส่ง prompt ไปแล้ว ยังไม่ได้รับผลตอนตัดเซสชัน** ครอบ
   เฉพาะ 5 ข้อของรอบ 2 (QA-02/QA-03/QA-04/QA-06/SEC-01 residual) — **นี่คือสิ่งที่ session ใหม่จะ
   ได้รับผลมาต่อ**

**สิ่งที่ session ใหม่ต้องทำเมื่อได้ผล QA รอบ 3 มา**:
- ถ้าทุกข้อผ่าน (✅ ยืนยันแล้วจริงทั้งหมด) → Phase 7-9 ปิด QA gate ได้ (ยกเว้น security gate ที่
  ต้องรอทีม `security` เรียกเองต่างหาก) → เสนอเรียก `security` audit ให้ผู้ใช้ตัดสินใจ (SEC-01/
  SEC-02 ของรอบนี้ + ยังมี SECURITY-1 ของ `company-admin` ค้างจากก่อนหน้านี้ด้วย — ดูหัวข้อ
  `company-admin` ด้านล่าง)
- ถ้ายังมี gap → ส่ง `backend-engineer`/`frontend-engineer` แก้ต่อแบบเดียวกับ 2 รอบที่ผ่านมา
  (อ่าน pattern การ diagnose + สั่งงานแบบละเอียดในรอบก่อนๆ เป็นตัวอย่าง) แล้ววนตรวจซ้ำจนสะอาด
- **นี่เป็นรอบที่ 3 แล้วที่พบ comment/เอกสารตกค้างจากการรื้อฟีเจอร์แชต** — ถ้ารอบนี้ยังเจออีก
  ให้พิจารณาสั่งงานแบบ "grep แล้วแสดงผลลัพธ์ดิบให้ดูตรงๆ" แทนการเชื่อคำยืนยันของ agent เฉยๆ

**สภาพแวดล้อมตอนตัดเซสชัน (เช็ค process จริงก่อนใช้ ไม่ใช่เชื่อบันทึกนี้เฉยๆ)**:
- Backend dev server รันอยู่ที่ `localhost:5080` (PID เดิมของเซสชันก่อน, `dotnet run` แบบ manual
  ไม่ใช่ผ่าน docker) — เชื่อม `.env` ที่ชี้ `supportroom-pg` (container, พอร์ต **5432**)
- Frontend dev server รันอยู่ที่ `localhost:3000` (`npm run dev` แบบ manual, Node 22 ผ่าน nvm)
- **⚠️ กับดักที่พลาดมาแล้ว 2-3 ครั้งในเซสชันนี้**: มี Postgres container **สองตัว** — `supportroom-pg`
  (พอร์ต 5432) คือตัวที่ `.env`/แอปจริงใช้งาน ส่วน `supportroom-local-postgres-1` (พอร์ต 55432)
  **ไม่ได้ใช้งานจริง** ถ้า apply migration หรือตรวจ schema ผิดตัวจะเจอบั๊กหลอกๆ ที่ดูเหมือนโค้ด
  ไม่ทำงานทั้งที่จริงๆ แก้ถูกแล้วแค่เช็คผิด DB — **ก่อนรัน `dotnet ef database update` หรือ query
  ตรวจ schema ทุกครั้ง ต้องเช็ค `.env`'s `POSTGRES_CONNECTION_STRING` ก่อนเสมอว่าชี้ container ไหน**
- มี docker-compose stack เดิมอีกชุด (`supportroom-local-frontend-1`:3001,
  `supportroom-local-api-1`:5138, `supportroom-local-postgres-1`:55432) รันอยู่คู่ขนาน — **ไม่ใช่
  ชุดที่ใช้ตรวจงานในเซสชันนี้เลย** เป็นคนละสภาพแวดล้อม อย่าสับสน
- บัญชีทดสอบที่สร้างไว้แล้วที่ `company-test`: `owner@local.test`/`LocalDevOwner!2026`,
  `admin@local.test`/`LocalDevAdmin!2027` (เปลี่ยนรหัสจากค่าตั้งต้นแล้ว), `cs@local.test`/
  `LocalDevCs!2027` (เปลี่ยนรหัสจากค่าตั้งต้นแล้ว)

**เรื่องอื่นที่ค้างคาอยู่ ไม่เกี่ยวกับ QA รอบนี้โดยตรง แต่ห้ามลืม**:
1. **`company-admin`**: SECURITY-1 (SEC-01–03 ของโมดูลนั้น) แก้โค้ดไปแล้วตั้งแต่ก่อนหน้านี้มาก
   แต่ยังไม่เคยผ่าน `security` formal re-audit — เป็นตัวบล็อกที่ค้างมานานที่สุดในโปรเจกต์นี้
2. ~~**`knowledge-base`**: หนี้ D-3 — `knowledge-base/design.md` §DM-2 ยังพูดถึงฟิลด์ pacing ของ
   `LessonConfig` ที่ถูกลบออกจากระบบไปแล้วจริง (ตอน company-admin ทำ Module P)~~ ✅ **ปิดแล้ว
   2026-08-25 (`system-analyst` amend เฉพาะโฟลเดอร์ `knowledge-base`)** — ลบการอ้างถึง
   `IntroWaitMs`/`BreathPauseMs`/`FinalQuestionWaitMs` ออกจาก DM-2 และตาราง "เรื่องที่ตรวจสอบ
   ครบแล้วในรอบนี้" · เอกสารอย่างเดียว ไม่มี migration/โค้ด/checkbox ใดถูกแตะ · QA รอบหน้าของ
   โมดูลนี้จะไม่เห็นสามคอลัมน์นี้เป็น drift อีก
3. **Figma revamp**: ผู้ใช้แจ้งว่าทีม UX/UI ทำการ revamp หน้าตาทั้งระบบใหม่ทั้งหมด (ไม่ใช่ปรับเล็กๆ)
   จะส่งลิงก์ Figma มาให้ดูทีหลัง — ตกลงแผนไว้คร่าวๆ แล้วว่า: (1) ขอดูภาพรวมทั้งไฟล์ก่อน ทำตาราง
   เทียบกับแผนผังหน้าจอเดิม ([artifact ที่ทำไว้](https://claude.ai/code/artifact/ba1f3945-fae2-4240-b41b-f7293e05efa9))
   แบ่งเป็น 3 กลุ่ม (เหมือนเดิม/โครงสร้างเปลี่ยน/หน้าใหม่) ให้ผู้ใช้ยืนยันก่อนแตะโค้ด (2) เช็คว่า
   revamp เปลี่ยนสีแบรนด์/ธีมไหม ถ้าเปลี่ยนต้องแก้ที่ token เดียวใน `globals.css` ไม่ใช่ทีละหน้า
   (3) ทยอยแก้เป็นชุดเล็ก เทียบของจริงให้ดูก่อนไปกลุ่มถัดไป — **ยังไม่เริ่มทำจริง รอผู้ใช้ส่งลิงก์**
4. โมดูล `company-admin` Phase 1 ยัง TARGETED ต้องมี FULL QA ก่อนส่ง `devops` ได้
5. **🟡 `learning-session` CR-2 (บันทึกใหม่ 2026-08-24) — แยก "AI ตอบไม่ได้จริง" ออกจาก
   "ระบบพัง"**: เจ้าของโปรเจกต์เจอตอนทดสอบสดว่า Gemini timeout / 429 / API key หาย หน้าตา
   เหมือนกันเป๊ะกับกรณี AI ไม่รู้คำตอบ · ไล่โค้ดยืนยันแล้วว่าเป็นช่องว่างจริง (ความล้มเหลว
   เชิงเทคนิคไม่เคยถูกเขียนลง DB เลย, `AnswerStatus` ไม่มีค่าแทนเรื่องนี้, frontend พูดข้อความ
   เดียวตายตัว) · **บันทึกไว้เท่านั้น ยังไม่สัมภาษณ์** ตามคำสั่งเจ้าของโปรเจกต์ที่จะคุยรวบยอด
   ทีหลัง → รายละเอียดเต็มที่ `_docs/module/learning-session/requirement.md` §Open Questions
   หัวข้อ "🆕 บันทึกไว้เมื่อ 2026-08-24" · ⛔ ห้ามออกแบบ/ลงมือจากหัวข้อนี้ก่อนมีรอบสัมภาษณ์

## Scaffold
Already scaffolded — this is an existing project (`frontend/` Next.js + `backend/` ASP.NET Core), not a fresh `setup` run. See root `CLAUDE.md` for the real architecture; the pipeline's `setup` agent does not need to run here.

## Modules

| Module | Stage | Next agent |
|---|---|---|
| knowledge-base | **QA FULL (2026-08-20) — Phase 7 verified ✅ (FULL)**, module overall ✅ — 139/140 `plan.md` tasks ✅ ติ๊กแล้วทั้ง 7 phase (เหลือแค่ R-2 latency ที่บล็อกด้วย deployment ไม่ใช่ล้มเหลว), ไม่มีข้อค้าง ❌/⚠️ เหลือเลย. Phase 7 (Document scope assignment, Module G, 🔒 gate) 22/22 ✅ ยืนยันด้วยโค้ดจริง + unit test ใหม่ 14 ตัว (backend 204/204) + ทดสอบจริงกับแอปที่รันอยู่ (curl ยิง 6 กรณีปฏิเสธของ DS-3 ผ่าน JWT จริง, login+`GET /api/companies` จริง) · Phase 1's TX-5 partial ปิดแล้ว (ปิดพร้อม Phase 7 wiring call site) → Phase 1 ตอนนี้ 15/15 ✅ · **บั๊ก 2 (owner บริษัทเดียว login ไม่ได้)** แก้ใน `AdminSessionProvider.tsx` ยืนยันแล้วด้วย code trace + live data (`GET /api/companies` คืนบริษัทเดียวจริงสำหรับ `owner@local.test`) — ไม่มี browser tool ในเซสชันนี้ให้คลิกทดสอบเอง แต่ผู้ใช้ยืนยันด้วยตาตัวเองแล้ว · **บั๊ก 3 (Select ต้องเลือกสองรอบ)** แก้ใน `DocumentUploadList.tsx` + `KnowledgeQnAAnswerDialog.tsx` (ไฟล์ Phase 6 เดิม, regression check ผ่านครบไม่กระทบ flow เดิม) ยืนยันแล้วด้วย diff อ่านโค้ดตรง · Phase 3 ยังเหมือนเดิม (21/21 ✅ แต่รอบล่าสุดเป็น TARGETED ไม่ใช่ FULL ต้องมี FULL ก่อน deploy ได้) · **`security` ยังไม่เคยรันสักครั้งตลอดทั้ง 7 phase** — เป็นตัวบล็อกเดียวที่เหลือสำหรับทุก phase ที่ติด gate (2,3,4,6,7) ก่อนส่ง `devops` · รายละเอียดเต็มใน `review.md` (Phase 1–6 เดิมย้ายไป `review/phase-1-6.md` แล้ว, Phase 3 Round 1 เดิมอยู่ที่ `review/phase-3.md`) | `security` (เมื่อผู้ใช้เรียก), `devops` (Phase 1 พร้อม accept ได้ทันที ไม่ติด gate), **`system-analyst` (amend) — งานใหม่ R7/R8 จาก `requirement.md` 2026-08-24 · เคาะครบไม่มีคำถามค้าง · เพิ่มเติม 2026-08-24: R7 มีข้อย่อยใหม่ `R7.6` แก้ไข/ลบ Q&A ที่มีอยู่แล้วจากหน้ารวม (เจ้าของโปรเจกต์เคาะ "ใช่ เพิ่มเข้าไปเลย") — หลังบ้านมีครบแล้ว (`UpdateAsync`/`DeleteAsync`, `PUT`/`DELETE /api/knowledge-qna/{id}`, `updateKnowledgeQnA`/`deleteKnowledgeQnA`) แต่ไม่มี UI ไหนเรียกเลย จึงเป็นงาน UI ล้วน ไม่มี schema change** |
| company-admin | **✅ 2026-08-22: Phase 4 "ต้องถอด/แก้ย้อนหลัง" (backend+frontend, contract กลับทิศ P1) เสร็จครบแล้ว พร้อม `qa-engineer` FULL รอบแรก (R-17)** · Phase 5 (Company Settings Page) implemented ✅ พร้อม `qa-engineer` — หน้า `/admin/settings` + section pacing + `section-access.ts`/`resolveSectionAccess`/registry ครบตาม SP-1..SP-15, `AdminSidebar.tsx` แก้ gate เมนู "ตั้งค่า" ให้ derive จาก registry แล้ว, typecheck/lint/test(60)/build ผ่านหมด · checkbox ใน `plan.md` §Phase 5 ยังเป็น `[ ]` ทั้งหมดรอ QA ติ๊ก · **Security SECURITY-1 ⚠️ — 3 Important findings open (ยังไม่แก้ในรอบนี้)**: stale JWT ยังใช้ได้หลัง deactivate/demotion · `MustChangePassword` bypass ผ่าน API · login ไม่มี rate limiting. QA: Phase 1 15/15 verified ✅ (TARGETED, ต้อง FULL ก่อน devops) · Phase 2 7/7 verified ✅ (FULL) · Phase 3 (Company Switching — Owner UX) 6/6 verified ✅ (FULL) · deployed ⬜ ทั้งโมดูล · ทั้งสาม phase ยังติด `🔒 Security gate` ของ Module A — Phase 3 ยังไม่เคยผ่าน `security` เลย (SECURITY-1 คลุมแค่ Phase 1/2) · **design amend 2026-08-22: ปิด B4 · B3a · A5 (แถว pacing) แล้ว** — เกิด **Module P · Lesson Pacing Defaults** (schema change จริง 2 จุด + contract `## Lesson Pacing Resolution Rules` LP-1..LP-15) · **A2 · A3 · A4 · B3b + A6 เฉพาะส่วนที่ไม่ใช่ pacing ยังเปิด** (บล็อกเฉพาะ Module B ที่เหลือ · **A5 แถว pacing และ A8 ปิดแล้ว 2026-08-22**) · **2026-08-22 (comment cleanup, รอบแรก): แก้ comment ตกค้างใน `ICompanyService.cs` ที่ยังอ้างถึง `ILessonPacingResolver` (ถูกลบไปแล้วจริงในรอบแก้ Module P) ให้ตรงกับโมเดลปัจจุบัน — ไม่มี resolver layer แล้ว อ่านค่า pacing จาก `Company` ตรงๆ · build 0 warning/0 error · ⚠️ claim ตอนนั้นว่า "grep ทั้ง backend แล้วไม่พบ reference อื่นเหลืออยู่" ไม่จริง — QA รอบถัดมา (Codex ภายนอก) grep ซ้ำแล้วเจอเพิ่มอีก 4 จุด ดูรายการแก้จริงในบรรทัดถัดไป** · **2026-08-22 (`backend-engineer`, comment cleanup รอบสอง หลัง QA จับ claim เดิมผิด): grep repo-wide จริงหา `LP-5`/`lesson-edit form`/`lesson-form placeholder`/`inherit`/`ILessonPacingResolver` ทั่ว `backend/` แล้วแก้ครบ 5 จุดที่เหลือซึ่งยังพูดถึงโมเดล per-lesson override เก่า — `ICompanyService.cs:28` (เหตุผลที่ `cs` อ่านได้ เปลี่ยนจากอ้าง "lesson-edit form placeholder (LP-5)" เป็นอ้าง section pacing บน `/admin/settings` ที่ `visibleToRoles` รวม `cs` ตาม SP-4/SP-15), `CompanyController.cs:38-40` (เหตุผลเดียวกัน), `CompanyViewModel.cs:11-13` (เลิกเทียบกับ `LessonConfigViewModel`'s nullable fields ที่ไม่มีอยู่แล้ว เหลือแค่เหตุผลว่า `Company` เป็นชั้นสุดท้ายของ resolve chain ตาม LP-1), `CompanyDto.cs:39-44` (เหตุผลเดียวกัน ไม่เทียบกับ `LessonConfigDto` อีก), `CompanyServiceTests.cs:222,312` (comment บน test แก้ให้ตรงเหตุผลใหม่) · grep ซ้ำหลังแก้เจอ `inherit` เหลือ 2 จุดที่เป็นของจริง ไม่ใช่ของเก่า — `Company.cs:45` พูดถึง Module B (settings ที่ยังพักไว้ ตาม design.md บรรทัด 296/1373 ที่ nullable = inherit ยังใช้จริงกับ Module B ไม่เกี่ยวกับ per-lesson pacing ที่ถูกลบ) และ `Program.cs:134` (เรื่อง middleware protection ไม่เกี่ยวกับ pacing เลย) — ทั้งสองจุดนี้ถูกต้องอยู่แล้ว ไม่แก้ · ไม่แตะ logic ใดๆ ทั้งหมดเป็น comment-only · build 0 warning/0 error ยืนยันแล้วจริงรอบนี้** · **2026-08-22 (`frontend-engineer`, แก้ QA findings จาก Codex ภายนอก ตรวจ Phase 5): แก้ 4 จุดใน `frontend/` — (1) Important: `LessonPacingSettingsSection.tsx` `CardDescription` เคยเขียนตามโมเดล per-lesson override เก่า แก้เป็นข้อความที่ตรง LP-7/SP-10 ปัจจุบัน (ค่ามีผลกับทุกบทเรียนของบริษัทตั้งแต่เข้าห้องครั้งถัดไป ห้องที่กำลังเรียนไม่เปลี่ยนกลางคัน ไม่เขียนว่า "มีผลทันที") (2) Important: แก้ race condition ตอนสลับบริษัทเร็วๆ ที่ทำให้ response เก่าของบริษัท A ทับ state ของบริษัท B บนจอ (และเสี่ยงบันทึกค่าผิดบริษัทถ้ากด save) — เลือกทาง (ก) เปลี่ยน `key={id}` เป็น `key={`${id}-${companyId}`}` ใน `page.tsx` บังคับ remount ทั้ง component เมื่อสลับบริษัท (3) Minor: ย้าย `LESSON_PACING_SECTION_ACCESS` ออกจาก `LessonPacingSettingsSection.tsx` (.tsx) ไปไฟล์ใหม่ `lesson-pacing-access.ts` เพื่อให้ `section-access.test.ts` import ค่าจริงจาก production แทนการ copy literal เอง (4) Minor: แก้ comment ที่อ้างอิงโมเดล lesson-vs-company inheritance เก่าใน `types/domain.ts` และ comment "lesson form placeholder" เก่าใน `api-client.ts` ให้ตรงโมเดลปัจจุบัน (ไม่มี per-lesson override แล้ว) · typecheck/lint/test(60)/build ผ่านหมด · ยังไม่แตะ checkbox ใดๆ ใน `plan.md`, รอ `qa-engineer` ตรวจซ้ำ** · **2026-08-22 (`frontend-engineer`, แก้จุดตกหล่นรอบ QA ที่สอง): แก้ 1 จุดที่เหลือ — comment ใน `frontend/src/components/admin/settings/lesson-pacing-fields.ts` (บรรทัด ~21) เคยเทียบ SP-7 กับ "the lesson form's LP-11 (empty means inherit)" ที่ไม่มีอยู่แล้ว ตัดการเทียบทิ้ง เหลือแค่เหตุผลของ SP-7 เอง (ที่ชั้นบริษัทว่างไม่ใช่ค่าที่ถูกต้องเพราะไม่มีชั้นถัดไปให้ fallback) · ตามด้วย grep ทั่ว `frontend/src/` จริงหา `LP-11`/`LP-5`/`inherit`/`lesson form`/`lesson-edit`/`ILessonPacingResolver`/`placeholder` — ไม่พบ reference อื่นที่ยังพูดถึงโมเดล per-lesson override เก่า (`inherit` ที่เจอ 2 จุดเป็นเรื่อง learner-key/JWT ไม่เกี่ยวกับ pacing, `pacing` ที่ match ใน `card.tsx`/`sidebar.tsx` เป็น CSS `--card-spacing`/`--sidebar-width` ไม่ใช่ pacing จริง) · sweep ครบ ไม่มีจุดตกหล่นเพิ่ม · typecheck/lint/test(60)/build ผ่านหมด · ไม่แตะ checkbox/`design.md`/`requirement.md`/`plan.md`, รอ `qa-engineer` ปิด gate**
  **2026-08-22 (`backend-engineer`): `plan.md` §Phase 4 `[backend]` ทุก task เสร็จแล้ว รอ QA** — migration ใหม่ `AddCompanyLessonPacingDefaults` (ใบเดียว: `Company` เพิ่ม `DefaultIntroWaitMs`/`DefaultBreathPauseMs`/`DefaultFinalQuestionWaitMs` เป็น `int` NOT NULL backfill literal `5000`/`500`/`5000` ตาม R-11 · `LessonConfig` 3 คอลัมน์เดิมเปลี่ยนเป็น `int?` โดยไม่แตะข้อมูลเดิม) — **applied จริงกับ local Postgres แล้ว** (`docker` container `supportroom-local-postgres-1`, verified ด้วย `\d "Company"`/`\d "LessonConfig"` ตรง contract เป๊ะ **⚠️ แก้ไข 2026-08-22: นี่คือ container ที่ไม่ได้ใช้งานจริง — `.env` ชี้ไป `supportroom-pg` พอร์ต 5432 ตามที่บันทึกไว้ถูกต้องแล้วที่บรรทัดถัดๆ ไป (`AddCompanyLessonPacingDefaults` ถูก apply ซ้ำให้ `supportroom-pg` ในรอบหลังแล้ว, ดูบรรทัด ~43 และ status ของ `RemoveLessonConfigPacingOverrides` ที่ apply ถูก container ตั้งแต่แรก) — ห้ามอ้างอิงบรรทัดนี้เพื่อเช็คสถานะ migration ของ runtime DB จริง**) · `dotnet ef migrations has-pending-model-changes` clean · `ILessonPacingResolver`/`LessonPacingResolver` ใหม่ (จุดเดียวที่ resolve, ต่อเข้า `GetTeachingContentByLinkAsync`) · `ICompanyService.Create`/`SeedFirstCompanyIfEmpty` (regression surface ของ Phase 1 ตาม R-12) แก้ให้ตั้ง pacing จาก `ServerDefaults` เสมอ — **ยืนยันสดกับแอปที่รันจริง**: restart แอปสำเร็จ, `SeedFirstCompanyIfEmpty` ตั้งค่า 5000/500/5000 ให้บริษัทแรกจริง, `POST /api/companies` ตั้งค่าให้บริษัทใหม่จริง (curl จริงผ่าน JWT จริง) · endpoint ใหม่ `GET`/`PUT /api/companies/{companyId}/lesson-pacing` (LP-9) ทดสอบสดครบ: owner GET/PUT ผ่าน, cs GET ผ่าน cs PUT ได้ 403 จริง, ค่านอกช่วง LP-8 ได้ 400 จริง, บริษัทไม่มีจริงได้ 404 จริง · `backend`: build 0 warning ใหม่/0 error, `dotnet test --filter "Category!=Integration"` **243/243 ผ่านหมด** (210 Application + 23 Providers + 10 Api.IntegrationTests, รวม unit test ใหม่ตาม LP-14 ครบ 4 กรณี + regression test ของ `Create`/`SeedFirstCompanyIfEmpty`) · **`[frontend]` ของ Phase 4 ยังไม่ทำเลย** (types/`api-client.ts`/ฟอร์มบทเรียน/ค่า fallback ยังเป็นงานเดิมทั้งหมด) — endpoint พร้อมให้ frontend เรียกแล้ว แต่หน้าจอยังไม่มีอะไรเปลี่ยน · หนี้ข้ามโมดูล D-3 (`knowledge-base/design.md` §DM-2 ยังไม่ตรง DM-P2) ยังไม่ปิด ไม่บล็อก Phase 4
  **2026-08-22 (`frontend-engineer`): `plan.md` §Phase 4 `[frontend]` ทุก task ที่มีอยู่ใน `plan.md` เสร็จแล้ว รอ QA** — `types/domain.ts`: `LessonConfig` 3 ฟิลด์ pacing เป็น `number | null` ตรง `LessonConfigViewModel` จริง (LP-12), `LearnerLessonConfig` เลิกใช้ `Pick` กับ 3 ฟิลด์นี้ ประกาศเป็น `number` ตรงๆ (LP-5/LP-12), เพิ่ม type `CompanyLessonPacing` ตรง `CompanyLessonPacingViewModel` จริง · `api-client.ts` เพิ่ม `getCompanyLessonPacing(companyId)` เรียก `GET /api/companies/{companyId}/lesson-pacing` (response ไม่ wrap เป็น `{ company }` ตรงกับ controller จริง) — **ไม่เพิ่มฟังก์ชัน `PUT`** เพราะไม่มี UI ตั้งค่าบริษัทให้เรียกใช้ในรอบนี้ (ดู open question ด้านล่าง) · `admin/lessons/[slug]/page.tsx`: handler ของ 3 ช่อง pacing แยกค่าว่าง (`null`) กับ `0` ได้จริงแล้ว (เลิกใช้ `Math.max(0, Number(e.target.value) || 0)`, LP-11), เพิ่ม placeholder `ว่าง = ใช้ค่าบริษัท (N ms)` ดึงค่าจริงจาก `getCompanyLessonPacing(activeCompanyId)` ไม่ hardcode ตัวเลข · `admin/lessons/new/page.tsx`: ค่าเริ่มต้น 3 ช่อง pacing เปลี่ยนจาก `3000/800/5000` เป็น `null` ทั้งหมด (ฟอร์มนี้ไม่มี input field ให้กรอกอยู่แล้ว เป็นแค่ default ของ payload ตอนสร้าง) · `use-tutor-session.ts:46` แก้ fallback `breathPauseMs` จาก `1000` เป็น `500` ให้ตรง `TutorConfig.Default*`/`ServerDefaults` จริง (LP-13) · frontend: `typecheck`/`lint`/`test` (41/41)/`build` ผ่านทั้งหมด (Node 22) · **Phase 4 backend+frontend เสร็จครบตาม `plan.md` แล้ว พร้อมให้ `qa-engineer` verify**
  **2026-08-22 (`system-analyst` รอบที่ 5 · amend design เท่านั้น ไม่มีโค้ด): 🔓 LP-15 ข้อ "ห้ามสร้างหน้า UI ตั้งค่าบริษัท" ถูกยกแล้ว (มติ P6)** — เจ้าของโปรเจกต์ตัดสินใจใหม่ในแชทว่าให้เริ่มทำหน้าตั้งค่าเลย ไม่ต้องรอ F2 ครบชุด (เริ่มจาก section pacing ก่อน แล้วเติมลิงก์หมดอายุ/TTS/แบรนด์ทีละอย่างทีหลัง) · **`design.md` เพิ่ม contract ชุดใหม่ `## Company Settings Page Rules` (SP-1..SP-14)**: route `/admin/settings` หน้าเดียวแบ่งเป็น `Card` ต่อ section (ปฏิเสธ Tabs — รอบนี้มี section เดียว), หนึ่ง section = หนึ่ง component ที่โหลด/เซฟ/validate/ตัดสินสิทธิ์เอง ห้ามมีปุ่มบันทึกรวม, `companyId` จาก session + refetch เมื่อ owner สลับบริษัท, **สิทธิ์เป็นของ section ไม่ใช่ของหน้า**, **SP-7: ที่ชั้นบริษัท "ว่าง" ไม่ใช่ค่าที่ถูกต้อง ตรงข้ามกับ LP-11 ของฟอร์มบทเรียน** · **ปิด A6 เฉพาะส่วน "ตัวหน้า + section pacing" = `cs` อ่านอย่างเดียว** (ตรงเจตนา LP-9 เดิม) · **เพิ่ม A8** (สิทธิ์ต่อ section — ไม่บล็อกรอบนี้ แต่บล็อกการเพิ่ม section ที่สอง) · เพิ่ม **R-13/R-14** และข้อ 4 ในหมายเหตุ 🔒 Security gate ของ Module P · **ไม่มี schema change และไม่มีงาน backend ใหม่เลย — `GET`/`PUT /api/companies/{companyId}/lesson-pacing` มีอยู่แล้วจาก Phase 4 ที่ทดสอบสดผ่านหมด งานที่เหลือเป็น frontend ล้วน** · **A2/A3/A4/B3b ยังเปิด · Module B ยังพัก** (P6 อนุญาต "หน้าจอ" ไม่ได้อนุญาต "ค่า") · **ต้องมี `project-manager` มาเพิ่ม task ของงานนี้ก่อน `frontend-engineer` หยิบไปทำ** — `plan.md` §Phase 4 วันนี้ยังไม่มี task สร้างหน้านี้แม้แต่ task เดียว
  **2026-08-22 (`system-analyst` รอบที่ 6 · amend design เท่านั้น ไม่มีโค้ด ไม่มี schema change): ✅ ปิด A8 และ ✅ ปิด LP-8/A5 แถว pacing** — เจ้าของโปรเจกต์ตอบตรงในแชทวันนี้ทั้งสองข้อ · **A8**: "สิทธิ์การมองเห็น" กับ "สิทธิ์แก้" เป็น **คนละแกน** ตั้งแยกต่อ section ได้ · **บาง section ซ่อนจาก role ไปเลยได้ ไม่ใช่แค่ read-only** ส่วน section ที่ไม่อ่อนไหว (pacing/เสียง) เห็นได้ทุก role → เขียนเป็น contract ใหม่ **SP-15** (`SettingsSectionAccess` = `visibleToRoles` + `editableByRoles` แยกกัน · registry เดียว `sections.ts` · "ซ่อน" = ไม่ mount/ไม่ยิง `GET`/ไม่มีข้อความบอกว่ามีของที่คุณไม่เห็น · invariant `editableByRoles ⊆ visibleToRoles` และ `owner` อยู่ครบทั้งสองเสมอ · การซ่อนที่ UI ไม่ใช่การกั้นสิทธิ์ ต้องมีกฎ server คู่กันหรือประกาศว่า cosmetic · เมนู sidebar **derive** จาก registry ห้าม hardcode role · test `resolveSectionAccess` 3 roles) · **section pacing ไม่เปลี่ยนแม้แต่จุดเดียว** = เห็นทุก role (`owner`/`admin`/`cs`) แก้ได้ `owner`/`admin` ตรง LP-9/SP-4 เดิมทุกประการ · **LP-8**: ยืนยันช่วงค่าเดิม **0–60000 / 0–10000 / 0–120000 ms** ("จูนทีหลังได้") → เลิกสถานะ "ข้อเสนอที่ยังไม่ผ่านปากเจ้าของโปรเจกต์" ทั้งใน LP-8 · A5 · SP-8 · แก้ SP-4/SP-5/SP-8/SP-12 ให้ตรงกลไกใหม่ · เพิ่ม **R-15** (การซ่อน section ถูกเข้าใจผิดว่าเป็นการกั้นสิทธิ์ — ยังไม่เกิดจริงรอบนี้ เป็นของที่ `security` ต้องตรวจตอนมี section ที่สอง) · **ผลรวม: ไม่มี open question ใดค้างอยู่กับ Module P หรือหน้า `/admin/settings` (section pacing) อีกแล้ว — `project-manager` เพิ่ม task ได้เต็มที่** (ที่ยังเปิดคือของ F2 ที่พักอยู่: A2 · A3 · A4 · B3b · A6 ส่วนที่ไม่ใช่ pacing ซึ่งบล็อกเฉพาะ **section ที่สอง** ไม่บล็อกงานรอบนี้) · **ยังไม่มีโค้ดใดถูกแก้ · `plan.md` ยังไม่มี task ของหน้านี้**
  **⚠️ ข้อขัดแย้งที่ `frontend-engineer` รายงานไว้ — ✅ คลี่คลายแล้วด้วย amend ข้างบน (ยืนยันว่าตอนนั้นทำถูกตามกฎ ไม่ใช่การข้ามงาน)**: คำสั่งที่ส่งมาขอให้เพิ่ม "section เล็กๆ" แสดง 3 ช่องกรอกค่า pacing ระดับบริษัทที่หน้า `/admin` (พร้อม read-only สำหรับ `cs`) แต่ `design.md` §LP-15 ห้ามไว้ตรงๆ คำต่อคำ ("ห้ามสร้างหน้า UI ตั้งค่าบริษัทในงานนี้ — รอบนี้หน้าจอที่แตะมีเพียงฟอร์มบทเรียน ส่วนค่าบริษัทแก้ผ่าน API ไปก่อน") และ `plan.md` §Phase 4 ก็ไม่มี task สร้างหน้า UI ตั้งค่าบริษัทเลยแม้แต่ task เดียว (ระบุชัดใน Sequencing Notes ว่า "cs ถูกปฏิเสธที่ PUT จริง...ทดสอบด้วยตาจาก UI ไม่ได้เพราะรอบนี้ยังไม่มีหน้าจอตั้งค่า") — **ได้ทำ task อื่นทั้งหมดของ Phase 4 `[frontend]` ตาม `plan.md` แล้ว แต่ข้ามการสร้างหน้า/section ตั้งค่าบริษัท** เพราะเป็นการฝ่า design contract ตรงๆ ไม่ใช่ช่องว่างที่ต้องเดา ถ้าต้องการหน้าตั้งค่าบริษัทจริง ต้องกลับไปที่ `system-analyst` เพื่อยกเลิก/แก้ LP-15 ก่อน แล้วส่งต่อ `project-manager` เพิ่ม task ให้ตรงกับ `plan.md` **2026-08-22 (`project-manager`): เพิ่ม `plan.md` §Phase 5 — Company Settings Page — Module P** ตาม `design.md` §Company Settings Page Rules **SP-1..SP-15** (ไม่มี open question ค้างแล้ว) — เปิดเป็น **phase ใหม่แยกจาก Phase 4** (ไม่ต่อท้าย เพราะ Phase 4 กำลังรอ QA FULL รอบแรกทั้งก้อน) · 17 task ทั้งหมด `[frontend]` **ไม่มี `[backend]` เลย** (endpoint LP-9 verified แล้วใน Phase 4): `section-access.ts` + `resolveSectionAccess` (SP-15), `LessonPacingSettingsSection.tsx`, `sections.ts` registry, `app/admin/settings/page.tsx`, `updateCompanyLessonPacing()` ใหม่ใน `api-client.ts`, แก้ `AdminSidebar.tsx` ให้ derive เมนูจาก registry แทน hardcode role, empty state ตาม SP-12, test ของ SP-14 + SP-15 ข้อ 10 · ติด `🔒 Security gate` · เขียนข้อห้ามชัดในหัวข้อ phase (ห้ามแตะฟอร์มบทเรียนซ้ำ/ห้ามปุ่มบันทึกรวม/ห้าม placeholder section ใหม่/ห้ามเพิ่ม section อื่นของ F2) — **พร้อมส่งให้ `frontend-engineer` หยิบไปทำได้ทันที** | **`frontend-engineer`** — หยิบ `plan.md` §Phase 5 ได้ทันที (ไม่มีอะไรบล็อก); แยกกัน งาน Phase 4 เดิมเสร็จตาม `plan.md` แล้ว รอ `qa-engineer` verify (มี regression surface ของ Phase 1 ต้องดูด้วยตาม R-12); แยกกัน `backend-engineer` แก้ `SEC-01`–`SEC-03` เมื่อมีคนหยิบ; จากนั้นรอผู้ใช้เรียก `security` re-audit (Phase 1/2 ที่แก้แล้ว + Phase 3/4/5 ที่ยังไม่เคยตรวจ, Phase 4 มี regression surface ของ Phase 1 ที่ต้องดูเป็นพิเศษ) · `qa-engineer` ต้องรัน FULL รอบใหม่ให้ Phase 1 และรอบแรกให้ Phase 4/5 ก่อน `devops` รับได้ · **✅ 2026-08-25 (`project-manager`): เพิ่ม `plan.md` §Phase 6 (Admin User Management — Backend) + §Phase 7 (Admin User Management — Frontend) สำหรับ Module U (F5)** ตาม `design.md` §Modules → Module U + contract `## Admin User Management Rules` (AU-1..AU-16) ที่ `system-analyst` เพิ่งเคาะเสร็จ — ไม่มี schema/migration เลย · ทั้งสอง phase ติด `🔒 Security gate` ไม่มีข้อยกเว้น · **Phase 6/7 ต้อง deploy พร้อมกันเท่านั้น ห้ามปล่อยทีละ phase** (R-19 — `Email` เป็น required field ใหม่ = breaking wire-contract change ต่อหน้า `/admin/users` ที่ใช้งานจริงอยู่) · ไม่ขึ้นกับ Phase 3/4/5 (D-5) เริ่มได้ทันที · unit test 11 ข้อตาม AU-15 เขียนเป็น task แยกทีละข้อ · Phase 6 ทำเครื่องหมาย regression surface ของ Phase 1/2 ไว้ชัด (แก้ `AdminUserService.Update`) · Phase 7 เป็นงานลบเป็นหลัก (ลบ `<Select>`/ปุ่มเปิด-ปิด/`UserRow.apply()` ในแถว) — **พร้อมส่งให้ `backend-engineer` (Phase 6) และ `frontend-engineer` (Phase 7) หยิบไปทำได้ทันที จากนั้น `qa-engineer` (FULL, ต้องตรวจคู่กันเพราะ deploy พร้อมกัน) แล้ว `security` (SECURITY-1 ของโมดูลนี้ยังไม่เคย re-audit ควรรวมรอบเดียวกัน) — ทั้งสองยังต้องให้ผู้ใช้เรียกเองด้วยชื่อ ไม่ auto-chain** | ผู้รับต่อ: `backend-engineer` + `frontend-engineer` (Phase 6/7 พร้อมแล้ว) |
  **🔄 2026-08-22 (`project-manager`, amend รอบที่สาม): แก้ `plan.md` §Phase 4 ในที่เดิม (ไม่เปิด
  Phase 6) ตามการกลับคำตอบ P1 (มติ N1/N2/N3)** — `system-analyst` amend `design.md` §Module P +
  `## Lesson Pacing Resolution Rules` เสร็จแล้ว: pacing กลับเป็น "ค่ากลางระดับบริษัทล้วน ไม่มี
  override ต่อบทเรียน" (ตรงข้ามกับที่ implement ไปแล้วใน Phase 4 เดิม) พร้อม migration ใหม่
  `RemoveLessonConfigPacingOverrides` (DropColumn 3 คอลัมน์จาก `LessonConfig`, ห้าม `UPDATE`
  กู้ค่าเดิม, ต้อง deploy พร้อมโค้ดที่เลิกอ่านคอลัมน์นั้นเสมอ — R-16/R-17) · เขียน task ของ Phase 4
  ใหม่ทั้งชุดให้ตรงตาราง 3 กลุ่มใน `design.md`: งานที่ "ยังถูกต้อง ไม่ต้องทำซ้ำ" (DM-P1 บน `Company`,
  regression ของ Phase 1 สองจุด, endpoint `GET`/`PUT` ของ LP-9, unit test LP-14.2/14.3,
  `LearnerLessonConfig` ใน `domain.ts`, `use-tutor-session.ts` fallback — ทำเครื่องหมายไว้ในแต่ละ
  task แต่ยังเป็น `[ ]` ทั้งหมด ไม่ติ๊ก `[x]` เอง) กับงานที่ "ต้องถอด/แก้ย้อนหลัง" (migration ใหม่,
  ลบ 3 property จาก `LessonConfig` entity, ลบ `ILessonPacingResolver` แล้วอ่าน `company.Default*Ms`
  ตรงจุดประกอบ ViewModel, ลบ 3 ฟิลด์จาก DTO/ViewModel, ลบ assign ปะปนใน `SaveAsync`, ลบ test เก่า
  ของ resolver สองชั้น + `SaveAsync` null พร้อม test ใหม่ตาม LP-14 ข้อ 1, ลบช่องกรอก pacing +
  `getCompanyLessonPacing()` ออกจากฟอร์มบทเรียนสองหน้า, ลบ test placeholder/empty-vs-zero เดิม) ·
  **Phase 5 (Company Settings Page) ไม่แก้เลยแม้แต่ task เดียว** เพราะ `design.md` ยืนยันว่า
  SP-1..SP-15 ทั้งชุดไม่กระทบจาก N1/N2/N3 · หนี้ข้ามโมดูล D-3 ยังไม่ปิด (คำตอบเปลี่ยนเป็น "ลบสามฟิลด์
  ออกจาก `knowledge-base/design.md` §DM-2" แทน "แก้เป็น nullable") บันทึกไว้ใน Sequencing Notes
  ไม่ใช่ task ของ phase นี้ — **Phase 4 (แก้ทิศทางใหม่) พร้อมส่งให้ `backend-engineer`/
  `frontend-engineer` หยิบไปทำได้ทันที, Phase 5 ยังพร้อมส่งเหมือนเดิมไม่เปลี่ยน** ทั้งคู่ยังไม่เคย
  ผ่าน QA สักรอบ — `qa-engineer` ต้องถือรอบแรกของทั้งสอง phase เป็น FULL เสมอ (R-17)
  **2026-08-22 (`backend-engineer`): `plan.md` §Phase 4 `[backend]` งาน "ต้องถอด/แก้ย้อนหลัง"
  ทุก task เสร็จแล้ว รอ QA** — migration ใหม่ `RemoveLessonConfigPacingOverrides` (ใบแยกจาก
  `AddCompanyLessonPacingDefaults` เดิม ไม่แก้ใบเก่า) `DropColumn` 3 คอลัมน์ `IntroWaitMs`/
  `BreathPauseMs`/`FinalQuestionWaitMs` ออกจาก `LessonConfig` ไม่มี `UPDATE` กู้ค่าเดิมใดๆ
  ตาม N2 พร้อมคอมเมนต์อธิบายมติ N1/N2/N3 และชี้ไปที่ `design.md` §DM-P2 · `Down()` สร้างคอลัมน์
  คืนเป็น `int NULL` เท่านั้น ไม่เดาค่ากลับ พร้อมคอมเมนต์ว่ากู้ได้แค่รูปร่างไม่ใช่ข้อมูล ·
  **applied จริงกับ container `supportroom-pg` พอร์ต 5432** (คนละตัวกับ `supportroom-local-postgres-1`
  ที่ map พอร์ต 55432 — ยืนยันด้วย `.env` ที่ชี้ `Port=5432` ก่อน apply) ตรวจ `\d "LessonConfig"`
  แล้วไม่มี 3 คอลัมน์นี้อีกต่อไป · `dotnet ef migrations has-pending-model-changes` clean ·
  ลบ `ILessonPacingResolver`/`LessonPacingResolver` ทั้ง interface/implementation + DI registration
  ออกจาก `ServiceConfiguration.cs`, `LessonConfig` entity ลบ 3 property, `LessonConfigDto`/
  `LessonConfigViewModel` ลบ 3 ฟิลด์, `SaveAsync` เลิก assign pacing ทั้งหกบรรทัด (สร้าง+แก้),
  `GetTeachingContentByLinkAsync` อ่าน `company.DefaultIntroWaitMs/DefaultBreathPauseMs/
  DefaultFinalQuestionWaitMs` ตรงๆ (จุดเดียวในระบบ ไม่มี merge/resolver คั่นแล้ว) ·
  ลบ `LessonPacingResolverTests.cs` และ test `SaveAsync_WithNullPacing...` เดิม, แก้ทุก test ที่ seed
  `LessonConfig`/`LessonConfigDto` ด้วย 3 ฟิลด์เดิม (8 ไฟล์) ให้ compile ผ่าน, เพิ่ม test ใหม่
  `GetTeachingContentByLink_ReturnsPacingFromCompany_NotFromTheLesson` (ค่าทดสอบ 1234/222/3333
  ไม่ใช่ ServerDefaults เพื่อไม่ให้ผ่านโดยบังเอิญ) · **regression ของ Phase 1
  (`ICompanyService.Create`/`SeedFirstCompanyIfEmpty`) และ endpoint `GET`/`PUT
  /api/companies/{companyId}/lesson-pacing` ไม่ถูกแตะเลยตามคำสั่ง** (ยัง "ยังถูกต้อง ไม่ต้องทำซ้ำ"
  ตาม `plan.md`) · `dotnet build SupportRoom.slnx` **0 warning/0 error** ·
  `dotnet test --filter "Category!=Integration"` **240/240 ผ่านหมด** (207 Application + 23
  Providers + 10 Api.IntegrationTests) · **ยืนยันสดกับแอปที่รันจริง**: restart แอปสำเร็จ,
  `curl GET /api/lessons/by-link/{token}` คืน `introWaitMs`/`breathPauseMs`/`finalQuestionWaitMs`
  ตรงค่าที่เพิ่ง `UPDATE "Company"` ไว้เป๊ะ (1111/222/3333) ไม่ error, restore ค่ากลับ 5000/500/5000
  หลังทดสอบเสร็จ · **ไม่ได้ติ๊ก checkbox ใดใน `plan.md`, ไม่แก้ `design.md`/`requirement.md`, ไม่แตะ
  `[frontend]` tasks** — `[frontend]` ของ Phase 4 รอบใหม่ (ลบช่องกรอก pacing ในฟอร์มบทเรียน/
  `getCompanyLessonPacing()` call site/`domain.ts` type) ยังไม่ทำ ยังเป็นงานเดิมทั้งหมด — **พร้อมให้
  `frontend-engineer` หยิบต่อ แล้วส่งทั้งคู่ให้ `qa-engineer` verify เป็น FULL รอบเดียวกัน (R-17
  ห้ามส่งมอบครึ่งทาง)**
| learning-session | 🟡 **งานใหม่เข้าคิว 2026-08-24 (ยังไม่สัมภาษณ์ ไม่บล็อกงาน QA ที่ค้างอยู่): CR-2 · แยก "AI ตอบไม่ได้จริง" ออกจาก "ระบบพัง"** — บันทึกไว้ที่ `requirement.md` §Open Questions ("🆕 บันทึกไว้เมื่อ 2026-08-24") เป็น capture-only ตามคำสั่งเจ้าของโปรเจกต์ (คุยรวบยอดทีหลัง) · next agent ของข้อนี้คือ **`business-analyst` (สัมภาษณ์)** เมื่อเจ้าของโปรเจกต์พร้อม — ⛔ ห้าม `system-analyst`/engineer หยิบไปทำก่อน · รายละเอียด+code trace 6 ข้อ อยู่ที่หัวข้อ `## learning-session` ด้านล่าง · **รอบแรก (Module A–F): QA FULL-3 + manual-4/5 ✅ ครบ 53/53** — LS-QA-05 ปิดหมดแล้ว · Phase 1–2 (A, B) ไม่ติด gate พร้อมให้ `devops` accept ได้เลย · Phase 3–6 ยังรอ `security` audit ก่อน deploy ได้ · 🆕 **2026-08-23 (`system-analyst`, amend `design.md` เท่านั้น ไม่มีโค้ด): แปลง F9 (responsive ฝั่งผู้เรียน) + F10 (พิมพ์ถามแทนพูด) + F10-a (รื้อแชต CS ทั้งฟีเจอร์) เป็น contract ครบแล้ว** — contract ใหม่ 3 ชุด (`## Responsive Interaction Rules` RS-1..RS-14 · `## Text Question Rules` TQ-1..TQ-21 · `## Chat Removal Rules` CX-1..CX-9) · Data Model แก้ 5 จุด (DM-3a `SessionQuestion.Source` ⏳ · **DM-4 `ChatMessage` เปลี่ยนจาก "ย้าย FK" เป็น "ลบทั้ง entity"** · DM-6a · DM-7a · DM-8) · migration ใหม่ **MG-R1 `RemoveChatMessageAndAddQuestionSource` = breaking + data loss ที่ตั้งใจ** (pattern เดียวกับ `RemoveLessonConfigPacingOverrides`) · **Module G/H/I ใหม่ ติด 🔒 ทั้งสาม** · R12–R18 · **ผลตรวจโค้ดจริงที่สำคัญ**: ขอบเขตรื้อแชตกว้างกว่าที่ requirement ไล่ไว้เกือบเท่าตัว (มีฝั่ง CS เต็มรูปแบบ) · `use-agent-session-chat.ts` ทำสองหน้าที่ ลบทั้งไฟล์ = Module F เสียฟีเจอร์เงียบๆ · พิมพ์ถามไม่ต้องมี provider/env ใหม่เลย · F9 มี 3 จุดที่โค้ดวันนี้ **ไม่ทำงานจริงบนมือถือ** (passive touch listener · `100vh` ทำปุ่มจบหลุดจอ · ไม่มี `viewport` export) · 🆕 **2026-08-23 (`system-analyst` รอบที่สอง · amend `design.md` เท่านั้น ไม่มีโค้ด): ✅ เจ้าของโปรเจกต์เคาะ U1–U4 ครบแล้ว — ไม่มี open question ค้างในโมดูลนี้อีกเลย พร้อมส่ง `project-manager` เต็มรูปแบบ** · มติ: **U1 = ตัดการตอบ readiness ด้วยเสียงทิ้ง เหลือกดปุ่มทางเดียว** *(ตรงข้ามกับข้อเสนอของ `system-analyst` ที่ให้คงเสียงไว้ — เจ้าของโปรเจกต์เลือกทางที่งานหนักกว่าโดยเห็น trade-off ครบแล้ว **ไม่ใช่ความเข้าใจผิด ห้ามถามซ้ำ**)* · U2 = เพิ่ม `SessionQuestion.Source` · U3 = `DropTable` `ChatMessage` พร้อมข้อมูล · U4 = F9 รวม `/session-ended/[token]` + `/link-expired` (ตรวจตามกฎเดียวกัน ห้าม redesign) · ⚠️ **ขนาดงานรอบนี้ใหญ่กว่าที่ประเมินไว้ตอนสัมภาษณ์ครั้งแรกอย่างมีนัยสำคัญ 2 เรื่อง**: (1) **U1 = การรื้อ readiness-by-voice ออกจาก 20+ จุดใน ~15 ไฟล์** ที่ `qa-engineer` ปิด FULL-3 ไปแล้ว (DTO/controller/provider ×2/ViewModel/service early-return · reducer event+afterSpeech+script · ลิสต์ push-to-talk **สองใบ** · api-client/types/hook · test 2 ชุด · เอกสาร 11 ไฟล์) → contract ใหม่ **TQ-22..TQ-27** + **R19/R20** (2) **ขอบเขตรื้อแชตกว้างกว่าที่คิดเพราะมีฝั่ง CS เต็มรูปแบบด้วย** (27 จุด ไม่ใช่ 5 จุดตามที่ `requirement.md` ไล่ไว้) · **นี่คือ regression surface ที่พาดเข้า Module C/D/E ที่ผ่าน QA แล้ว** — `POST /api/voice-question` เปลี่ยน wire contract จริง (request ตัด `expecting`, response ตัด `readiness`) ต้อง deploy สองฝั่งพร้อมกัน และ `qa-engineer` ต้อง re-verify เส้นทางถามด้วยเสียง + หน้าจอ `ready` ด้วยมือ ห้ามเชื่อผล FULL-3 เดิม (แบบเดียวกับ R-12 ของ `company-admin`) · **U1 ไม่มี schema change — ห้ามมี migration ใบที่ 4** MG-R1 ยังเป็นใบเดียวของรอบนี้ · 🆕 **2026-08-23 (`frontend-engineer`): Phase 8 (Module H — รื้อแชต) task `[frontend]` ทำครบแล้ว** — ลบ `ChatDrawer.tsx` + `use-session-chat.ts` ทั้งไฟล์ (CX-3/CX-5 #19, #21) · เขียน `use-agent-session-chat.ts` ใหม่เป็น `hooks/use-agent-session-questions.ts` เหลือเฉพาะ `JoinSessionAsAgent`+`ReceiveNewQuestion`+`liveQuestions` ตาม CX-2 (ไม่ได้ลบไฟล์ทิ้ง คำถามสดของ CS ยังทำงานอยู่) · ลบปุ่ม "แชท" + `chat.chatMessages`/`onSendMessage` ใน `admin/learning-sessions/[id]/page.tsx` เหลือ `liveQuestions`+`mergeQuestions` เดิม · ลบ `getOwnChatMessages`/`getChatMessagesByLearningSession` + import `ChatMessage` ใน `api-client.ts` · ลบ type `ChatMessage`/`ChatSenderRole` ใน `types/domain.ts` + แก้คอมเมนต์ที่อ้างถึง · แก้ `room/[token]/page.tsx` ลบ `useSessionChat`/`chat.*`/prop `chatMessages`/`onSendMessage` (ไม่ได้ต่อ component ใหม่ — เป็นงาน Module I ตามที่ CX-5 #24 ระบุ) · เปลี่ยนชื่อ prop `ControlBar.onToggleChat`→`onToggleAskAi` + label "แชต"→"ถาม-ตอบกับ AI" (CX-5 #25, CX-7) · ไม่มี test frontend ที่เกี่ยวกับแชตให้ลบ (CX-9 ทั้ง 5 ไฟล์เป็น backend) · อัปเดตเอกสาร frontend ทั้ง 7 ไฟล์ตาม CX-8 (`ER_DIAGRAM.md`, `API_CONTRACT.md`, `SYSTEM_LOGIC.md`, `SEQUENCE_DIAGRAMS.md`, `USE_CASE_DIAGRAM.md`, `DATA_FLOW_DIAGRAM.md`, `SYSTEM_ARCHITECTURE.md`) · grep `chatmessage\|sendchatmessage\|chat-messages\|ChatDrawer\|use-session-chat\|use-agent-session-chat` บน `frontend/src` ไม่เหลือผลลัพธ์ที่เป็นโค้ดจริงแล้ว (เกณฑ์ปิดงาน CX-1) · typecheck/lint/test(60/60)/build ผ่านหมด · **ยังไม่ติ๊ก checkbox ใน `plan.md`** (ตามกฎ — รอ `qa-engineer`) · **backend task ของ Phase 8 (`[backend]`) ยังไม่ทำในรอบนี้** ต้องมี agent อื่นทำคู่ขนานตาม Sequencing Notes ก่อนพร้อม deploy จริง · 🆕 **2026-08-23 (`backend-engineer`): Phase 7 (Module G) + Phase 8 (Module H) `[backend]` ทำครบทั้งคู่ในรอบเดียว** (ทั้งสอง phase ใช้ migration ไฟล์เดียวกันตาม Sequencing Notes จึงทำรวมกัน ไม่แยก) — migration `RemoveChatMessageAndAddQuestionSource` สร้างและ apply กับ `supportroom-pg` (พอร์ต 5432) จริงแล้ว: `AddColumn("SessionQuestion","Source")` (backfill `"voice"` แล้วถอด default constraint ตาม MG-R1) + `DropTable("ChatMessage")` พร้อมข้อมูลเดิม (ยืนยันด้วย `\d "SessionQuestion"` และ `\dt "ChatMessage"` บน DB จริง) · `dotnet ef migrations has-pending-model-changes` clean · เพิ่ม `POST /api/text-question` (`TextQuestionController` ใหม่ ใช้ `IVoiceQuestionService.AskTextAsync`/`IVoiceQuestionProvider.AnswerTextAsync` ร่วมกับเสียงทั้งคู่ ไม่ copy-paste pipeline) · เพิ่ม `Domain/Enums/QuestionSource.cs` + `SessionQuestion.Source` (required) + `DtoLimits.QuestionTextMaxLength=2000` (เปลี่ยนเป็น `public` เพื่อให้ controller ใน `Api` project เข้าถึงได้ — assembly คนละตัวกับ `Application`) · ถอด readiness-by-voice ออกครบตาม TQ-22/TQ-23 ทั้งสอง provider (Gemini + RAG) รวม `AskVoiceQuestionDto.Expecting`/`VoiceQuestionResult.Readiness`/`VoiceAnswerViewModel.Readiness`/`GeminiAnswerJson.Readiness`/early-return ใน service — **`POST /api/voice-question` เปลี่ยน wire contract แล้วจริง** (ไม่มี `expecting` ใน request, ไม่มี `readiness` ใน response) ยืนยันด้วย curl จริงกับ session ทดสอบ (สร้าง/ลบผ่าน SQL ตรง เพราะไม่มีลิงก์ที่ยังไม่หมดอายุใน DB) · รื้อ `ChatMessage` ทั้ง stack ตาม CX-4/CX-5 ฝั่ง backend ครบ 18 จุด (entity/enum/service/dto/viewmodel/mapster/realtime/controller/DI/hub methods `SendChatMessage`+`SendChatMessageAsAgent`+`JoinSession`/`IsSensitiveLearnerPath`/repository/UnitOfWork/DbContext/AdminService.ResetDemoData/XML doc ของ `TrainingLink`+`LearningSession`) — **เก็บ `JoinSessionAsAgent`/`ReceiveNewQuestion` ไว้ครบตามที่ CX-2/CX-3 สั่ง ไม่ได้ลบผิดตัว** · แก้คอมเมนต์ `KnowledgeQnAConflict.cs:29` จาก "readiness check" เป็น "no_speech" (TQ-27) · `grep -ri "readiness|expecting"` และ `grep -rli "chatmessage|sendchatmessage|chat-messages"` บน `backend/src`+`backend/tests` ไม่เหลือผลลัพธ์ที่เป็นโค้ดจริง (ยกเว้นชื่อไฟล์ migration และคอมเมนต์อ้างชื่อ migration ที่ตั้งใจ) · ลบ/แก้ test ตาม CX-9 ครบ (`ChatMessageServiceTests.cs` ลบทั้งไฟล์ · `CompanyIsolationTests`/`AdminServiceTests`/`ServiceTestFakes`/`SessionQuestionServiceTests` แก้ตาม) · เพิ่ม test ใหม่ตาม TQ-21 ใน `VoiceQuestionServiceTests.cs` (`AskTextAsync` บันทึก `Source="text"`/`Transcript`ตรงตัว, เส้นเสียงยัง `Source="voice"`, session `Ended` ปฏิเสธข้อความเดียวกัน) · `dotnet build` 0 warning/0 error (มี 8 warning pre-existing ใน `Providers.DocumentParsing`/`Providers.Slides` ที่ไม่เกี่ยวกับงานนี้ ไม่ได้แตะไฟล์เหล่านั้นเลย) · `dotnet test --filter "Category!=Integration"` **234/234 ผ่าน** (23+201+10) · รีสตาร์ตแอปจริงยืนยัน endpoint ทำงาน: validation order ของ `/api/text-question` ตรง TQ-3 ทุกกรณี (missing fields → 400, blank → 400, >2000 ตัวอักษร → 400, learnerKey ผิด → 404), header `no-store`/`no-referrer` มาถูกต้อง, เรียกจริงแล้วชน `UPSTREAM_ERROR` (502) เพราะ local `.env` ไม่มี `GEMINI_API_KEY` จริง (ข้อจำกัด environment ไม่ใช่บั๊ก) และไม่มีแถวถูกเขียนตอน error (ยืนยัน TQ-10 ด้วย SQL ตรง) · `/api/voice-question` ที่ `durationMs` สั้นเกินยังตอบ `no_speech` ปกติไม่มี `readiness` ในผลลัพธ์ · อัปเดตเอกสาร backend/root ตาม CX-8/TQ-27: `docs/schema.dbml`, `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`, `backend/docs/WORKFLOW.drawio`, `docs/PROJECT_CONTEXT.md`, `docs/TECH_DECISIONS.md` (TD-015 ใหม่) — **ไม่แตะ `docs/CORE_FEATURE_SPEC.md`** · **ยังไม่ติ๊ก checkbox ใดใน `plan.md`** (รอ `qa-engineer`) · **ไม่ได้ทำ `[frontend]` ใดๆ** (Phase 8 frontend ทำไปแล้วก่อนหน้า, Phase 9 frontend ยังไม่เริ่ม) · 🆕 **2026-08-23 (`frontend-engineer`): Phase 9 (Module I — responsive + single-input room UI) `[frontend]` ทำครบทั้งชุด** — RS-1..RS-14: `room/layout.tsx`+`join/layout.tsx` ใหม่ (`viewport.interactiveWidget: "resizes-content"`, ไม่มี `maximumScale`) · `room/[token]/page.tsx` `h-screen`→`h-[100dvh]`, `md:`→`lg:` ทั้งสองจุด, `relative` ให้ container, safe-area padding บน `ControlBar` · `PushToTalkButton.tsx` เขียนใหม่เต็มด้วย Pointer Events (`setPointerCapture`, `touch-none`, `onContextMenu` preventDefault, `pointercancel` ปล่อยเสมอ) แทน mouse/touch handlers เดิมที่ไม่เคยทำงานจริงบนจอสัมผัส · `SlidesEmbed.tsx` เพิ่ม fullscreen overlay ในแอป (ไม่ใช้ Fullscreen API) ทั้งเส้น iframe (ปุ่มจริงแทน `div aria-hidden`) และ PDF `<img>`, ไม่ unmount/เปลี่ยน `key` ตอนสลับ — เลือก **auto-close fullscreen เมื่อเริ่มประมวลผล/ตอบคำถาม** (มีคอมเมนต์อธิบายเหตุผล) เพื่อไม่ให้มีสถานะกดจบไม่ได้ (RS-6) · `ControlBar.tsx` จัดลำดับปุ่มพูด>จบ>drawer>เสียง + hit target `size-11` (44px) ทุกปุ่ม + safe-area · `AiTile`/`ParticipantTile` ย่อเป็นแถวเตี้ยแนวนอน (`w-28`) บน compact ผ่าน container class ไม่แก้ component เอง · `VolumeControl` popover ไม่ล้นจอแคบ + trigger 44px · `join/[token]`/`session-ended/[token]`/`link-expired` เปลี่ยน `min-h-screen`→`min-h-[100dvh]` + ปุ่ม `h-11` ครบ (session-ended/link-expired **ไม่ redesign** ตาม U4) · ยืนยันไม่มี code path บล็อก orientation (RS-11) · สร้าง `components/meeting/AskAiDrawer.tsx` ใหม่ (ไม่มีคำว่า Chat เลย) ตาม CX-6 พร้อม props `{open, onClose, questions, onSubmitQuestion, inputEnabled, sendEnabled, disabledHint?}`, compact เต็มจอ/regular panel ลอย, input `sticky bottom-0`+safe-area+`text-base md:text-base` (กัน iOS auto-zoom)+ปุ่มส่งเห็นเสมอ (RS-7/RS-8) · เพิ่ม `askTextQuestion` ใน `api-client.ts` (JSON ผ่าน `publicRequest`, `askVoiceQuestion` signature ไม่เปลี่ยน) · เพิ่ม `QuestionSource`+`SessionQuestion.source` ใน `types/domain.ts` (`LearnerSessionQuestion` ไม่มี `source`) · เพิ่ม event `SUBMIT_TEXT_QUESTION`+effect `SEND_TEXT_QUESTION` และ `NOT_READY` ใน `tutor/intents.ts`/`types.ts`/`tutor-reducer.ts` ตรงตาม TQ-14/TQ-18 matrix (guard state ครบ, ไม่ผ่าน `push-to-talk-recording`) · เพิ่ม `textQuestionAvailability()` ใน `room/[token]/page.tsx` implement matrix TQ-20 ครบทุก state · effect runner ใน `use-tutor-session.ts` เพิ่ม `sendTextQuestion()`/`case "SEND_TEXT_QUESTION"` (ไม่เช็ค `readiness`/ไม่ map `NO_SPEECH`) · ยืนยันช่องพิมพ์ไม่มี `onFocus`/`onChange`/`onKeyDown` ใดๆ ที่ dispatch เข้า reducer (TQ-15) — หยุดบรรยายเกิดที่ `dispatch(SUBMIT_TEXT_QUESTION)`→`clearPending()` เท่านั้น · **ถอด readiness-by-voice ฝั่ง frontend ครบ TQ-24..TQ-26**: ลบ `"ready"` จาก `PUSH_TO_TALK_STATES` (reducer) **และ** `PUSH_TO_TALK_ENABLED_STATES` (room page — ลิสต์ใบที่สอง) พร้อมกัน · ลบ event `READINESS_ANSWERED` + `AfterSpeechAction "START_FIRST_SLIDE"` (เก็บ `startFirstSlide()`/`AWAIT_READINESS` ไว้ตามตาราง TQ-25) · ลบ branch `interruptedFrom === "ready"` dead code ใน `resumeAfterInterruption` · ลบ `readyConfirmScript`, แก้ข้อความ `notReadyScript` ให้ชี้ปุ่มจริง ("กดปุ่มพร้อมแล้ว") · ลบ `expecting`/`readiness` ออกจาก `api-client.ts`/`types/domain.ts`/`use-tutor-session.ts` (`stopRecordingAndSend` ไม่เช็ค `result.readiness` แล้ว) · เพิ่มปุ่ม **"ยังไม่พร้อม"** คู่ปุ่ม "พร้อมแล้ว" ในหน้าห้อง ส่ง `NOT_READY` ลบบรรทัดชวนพูดตอบเดิม · `grep -ri "readiness\|expecting" frontend/src` เหลือเฉพาะคอมเมนต์อธิบายประวัติ ไม่มีโค้ดจริง (เกณฑ์ปิดงาน) · เขียน/แก้ reducer test ตาม TQ-21 ครบ (`SUBMIT_TEXT_QUESTION` จาก 3 state ที่อนุญาต+จาก `ready`/`processing-question`/`push-to-talk-recording`→ไม่เกิดอะไร, `NOT_READY` จาก `ready`→ไม่มี `WAIT_READY_TIMEOUT` ตามมา, **`PUSH_TO_TALK_START` จาก `ready`→ไม่เกิดอะไรเลย** (เคสใหม่), ลบ describe block "answering the readiness prompt by voice" เขียนใหม่เป็นเคสของ `NOT_READY`) · อัปเดตเอกสารตาม TQ-27 ครบ 9 ไฟล์ที่ต้องแก้จริง (`STATE_MACHINE.md`, `API_CONTRACT.md`, `GEMINI_INTEGRATION.md`, `SEQUENCE_DIAGRAMS.md`, `SYSTEM_LOGIC.md`, `TESTING_GUIDE.md`, `docs/PROJECT_CONTEXT.md`, `docs/UX_UI_WORKFLOWS.md`, `docs/PROVIDER_SETTINGS_SPEC.md` — ตรวจ `docs/BACKEND_DB_HANDOFF.md`/`docs/SOLUTION_ARCHITECTURE.md` แล้วพบว่าเนื้อหาเดิมยังถูกต้อง ไม่ต้องแก้) · typecheck/lint/test **65/65**/build ผ่านหมด · **ยังไม่ติ๊ก checkbox ใดใน `plan.md`** (รอ `qa-engineer`) · ⚠️ **ไม่ได้ทดสอบด้วยอุปกรณ์สัมผัส/emulator จริง** (agent ไม่มี browser/device จริงให้ทดสอบ) — RS-14 ทั้ง 7 ข้อ และการ re-verify ด้วยมือของ IC-7/R19/R20 (2 browser จริง, กดปุ่มพูดตอน `ready` ไม่เกิดอะไรบนเครื่องจริง, กดจบได้ระหว่าง fullscreen ตามที่เลือก auto-close) **ยังเป็น Unverified Behaviour ที่ `qa-engineer` ต้องทำเองหรือส่งต่อเจ้าของโปรเจกต์ก่อน deploy** | ✅ **Phase 7+8+9 ครบทั้งสาม phase พร้อมให้ `qa-engineer` verify รอบ FULL ใหม่** — ต้องเป็น **FULL ไม่ใช่ TARGETED** (R19/R20 สั่งชัด): re-verify เส้นทางถามด้วยเสียงทั้งเส้น (Phase 7 wire contract เปลี่ยน) + ปุ่มพร้อม/ยังไม่พร้อมทำงานถูก + กดปุ่มพูด/พิมพ์ถามตอน `ready` ต้องไม่เกิดอะไร + IC-7 ทั้งชุดด้วยมือหลังเขียน `room/[token]/page.tsx` ใหม่ (React Strict Mode ด้วย) + RS-14 ทั้ง 7 ข้อบนอุปกรณ์จริงทั้ง Google Slides และ PDF lesson · แนะนำให้เรียก `security` คู่ขนานเพราะ Phase 7/8/9 ทั้งสามติด 🔒 gate | ✅ **`devops` ยังไม่ deploy จนกว่า `qa-engineer` ปิดรอบ FULL นี้และ `security` ตรวจ Phase 7/8/9** — ⛔ ห้าม deploy Phase 7 (backend) แยกจาก Phase 9 (frontend) เพราะ wire contract คร่อมสองฝั่ง (R19) · Phase 1–2 ยัง accept ได้อิสระ · 🆕 **2026-08-23 (`frontend-engineer`, แก้ QA findings รอบ Codex ที่ตรวจ Phase 7-9 — โค้ดเท่านั้น ไม่แตะ `plan.md`/`design.md`/`requirement.md`)**: แก้ครบ 3 ข้อที่เป็น frontend — **QA-03 (Important)**: `AskAiDrawer.tsx` เพิ่ม client-side length guard (`QUESTION_TEXT_MAX_LENGTH = 2000` ตรงกับ `DtoLimits.QuestionTextMaxLength` ปิดปุ่มส่ง+โชว์ error ทันทีถ้าเกิน ไม่ยิง request เลย) และเพิ่ม `failedQuestionText`/`CLEAR_FAILED_QUESTION_TEXT` เข้า `TutorRuntime`/`tutor-reducer.ts`/`use-tutor-session.ts` (ต่อสายจาก `sendTextQuestion` catch) ให้ draft คำถามที่พิมพ์แล้วส่งไม่สำเร็จ (network/upstream error) กลับเข้าช่องพิมพ์อัตโนมัติแทนที่จะหายตลอดกาล — ตรงตาม CX-6 · **QA-05 (Minor)**: `PushToTalkButton.tsx` เพิ่ม `select-none` (คู่ `touch-none`) และแยก `onPointerCancel` ออกจาก `release()` เป็น `forceRelease()` ที่เรียก `onEnd()` แบบไม่มี guard ตาม RS-5 (RS-14 ทดสอบมือจริงบนอุปกรณ์สัมผัสยังไม่ได้ทำ — ต้องทำตอน verify RS-14 รอบใหญ่อยู่แล้ว) · **QA-04 บางส่วน (Minor, comment ล้าสมัย)**: แก้ comment ใน `AskAiDrawer.tsx` (เลิกอ้าง `ChatDrawer`/`chatMessages` เดิม) และ `session-ended/[token]/page.tsx` (เลิกอ้างว่า `/admin` ไม่มี auth — ปัจจุบันมี JWT+RBAC แล้ว) + grep ทั่ว `frontend/src` ซ้ำหา `ChatMessage`/`ChatDrawer`/`chatMessages`/"chat" ที่ตกค้าง ไม่พบเพิ่มเติม · `npm run typecheck`/`lint`/`test`/`build` ผ่านหมด (65 tests) · ยังไม่ได้ติ๊ก checkbox ใดใน `plan.md` (ตามกฎ รอ `qa-engineer` ยืนยัน) — พร้อมให้ `qa-engineer` verify (TARGETED ก็พอสำหรับ 3 จุดนี้ แต่ FULL รอบ Phase 7-9 เดิมที่ค้างอยู่ยังต้องทำแยกตามที่ระบุไว้ข้างต้น) · 🆕 **2026-08-24 (`backend-engineer`, แก้ QA/security findings รอบ Codex ภายนอก ตรวจ Phase 7-9): แก้ครบทั้ง 5 ข้อ (3 Important bug + 2 Important security) + QA-06 + QA-04 (ส่วน backend/docs) — รอ `qa-engineer` ยืนยัน** · 🆕 **2026-08-24 (`frontend-engineer`, แก้ 2 QA residual รอบตรวจซ้ำ Codex): QA-03 residual (Important) — `AskAiDrawer.tsx` effect ที่ restore `failedQuestionText` เข้า `draft` เดิม clobber ข้อความคำถามถัดไปที่ผู้เรียนพิมพ์อยู่ก่อน Q1 จะล้มเหลวจริง แก้เป็น `setDraft((current) => current.length === 0 ? failedQuestionText : current)` — restore เฉพาะตอน draft ว่างเท่านั้น ไม่ทับข้อความที่พิมพ์ค้างอยู่ แต่ยังเรียก `onFailedQuestionTextConsumed()` เสมอกันค้าง เพิ่ม `AskAiDrawer.test.tsx` (component test ใหม่ทั้งไฟล์ ใช้ `@testing-library/react` + `jsdom` — เพิ่ม devDependency `@testing-library/react`/`@testing-library/dom`/`jsdom` และตั้ง `vitest.config.mts` ให้รองรับ `.test.tsx` ผ่าน `oxc.jsx: { runtime: "automatic" }` เพราะ `tsconfig.json` เดิมตั้ง `jsx: "preserve"` ไว้ให้ Next SWC ทำให้ oxc transform ข้าม JSX ถ้าไม่ override) ยืนยัน 2 เคส (ทับไม่ได้ตอน draft ไม่ว่าง / restore ได้ตอน draft ว่าง) ผ่านทั้งคู่ · QA-04 residual (Minor, grep ไม่ครบรอบที่ 3): แก้ `docs/UX_UI_WIREFRAME_SPEC.md` (wireframe ปุ่ม/drawer เดิมอ้าง "แชต"/"Chat + Q&A drawer"/"Live chat" กับ persona table) และ `frontend/docs/BACKEND_HANDOFF.md` (อ้างว่า `SessionHub` ใช้สำหรับ live chat) ให้ตรงความจริง (ไม่มีฟีเจอร์แชต มีแต่ Ask-AI drawer/`ReceiveNewQuestion`) แล้ว grep ซ้ำทั่ว `docs/` และ `frontend/docs/` ทั้งหมด (ไม่ใช่แค่ 2 ไฟล์ที่ระบุ) พบเพิ่มอีก 6 ไฟล์ที่มีคำว่า "chat" ในความหมายที่ยังบอกว่าเป็น current capability: `docs/UX_UI_HANDOFF.md` (persona/route table), `docs/SOLUTION_ARCHITECTURE.md` (เหตุผลเลือก SignalR), `docs/HANDOFF_MASTER.md` (4 จุด: audit-fixed bullet, capability table, D-06, QA smoke list), `frontend/docs/INTEGRATION_ROADMAP.md` (rate-limiting target), `docs/PROJECT_CONTEXT.md` (ER diagram mermaid ยังมี `CHAT_MESSAGE` entity + relationship ทั้งที่ข้อความข้างบนบอกว่าตารางถูกลบแล้ว — ขัดกันเองในไฟล์เดียว) แก้ครบทุกจุดที่พบ · **ยืนยันไม่แก้**: `docs/CORE_FEATURE_SPEC.md` (design.md สั่งห้ามแก้), `docs/TECH_DECISIONS.md` (decision log ประวัติ), และจุดที่เหลือใน `docs/PROJECT_CONTEXT.md`/`docs/BACKEND_DB_HANDOFF.md`/`docs/schema.dbml`/`frontend/docs/ER_DIAGRAM.md`/`frontend/docs/DATA_FLOW_DIAGRAM.md`/`docs/PRODUCTION_ROADMAP.md` ที่เป็นบันทึกประวัติ/migration name ที่ถูกต้องอยู่แล้ว (บอกว่าฟีเจอร์ถูกลบไปแล้ว ไม่ใช่ยังมีอยู่) · `npm run typecheck`/`lint`/`test`/`build` ผ่านหมด (67 tests, +2 จากรอบก่อน) · ไม่ได้ติ๊ก checkbox ใดใน `plan.md`, ไม่แก้ `design.md`/`requirement.md` — พร้อมให้ `qa-engineer` verify (TARGETED พอสำหรับ 2 จุดนี้)**·
**QA-02 (CS เข้าฟังคำถามสดไม่ได้)**: `SessionHub.EnsureAgentAuthenticated()` เดิมเรียก `guard.EnsureAuthenticated()` แต่ไม่เคย resolve `ICurrentUser`/`ICompanyContext` ในสโคปของ hub method invocation เลย (SignalR เปิด DI scope ใหม่ทุกครั้งที่เรียก hub method ไม่ใช่ครั้งเดียวต่อ connection เหมือน HTTP middleware) ทำให้ `IsAuthenticated` เป็น `false` เสมอ แก้โดยอ่าน claim จาก `Context.User` (ClaimsPrincipal เดียวกับที่ authenticate ไว้ตอน handshake ผ่าน query string `access_token` อยู่ค้างตลอด connection) มา `ICurrentUser.Resolve(...)` ก่อนเรียก guard เสมอ — **ระหว่างทดสอบสดพบบั๊กที่สองที่ QA ไม่ได้ระบุไว้**: หลัง `IsAuthenticated` ผ่านแล้ว `EnsureLearningSessionExists` ยังคืน "ไม่พบการเรียน" เพราะ `ICompanyContext` (ที่ query filter ของ `LearningSession` repository พึ่งอยู่) ก็ไม่เคย resolve ในสโคป hub เช่นกัน — แก้เพิ่มโดยดึง `?company=` จาก `Context.GetHttpContext()?.Request.Query` แล้วเรียก `guard.EnsureCanAccessCompany`+`ICompanyContext.Resolve` ตามกฎเดียวกับ `CurrentUserMiddleware.ResolveRequestedCompany` (ยกเมธอดนั้นเป็น `public static` รับ `IQueryCollection` แทน `HttpContext` เพื่อใช้ร่วมกันได้ ไม่ก็อปกฎซ้ำ) · **ยืนยันสดด้วย SignalR client จริง (Node + `@microsoft/signalr` จาก `frontend/node_modules`)**: JWT ของ `cs@local.test` จริง, เรียก `JoinSessionAsAgent` จริงกับ session ID จริงใน DB — ก่อนแก้ได้ `กรุณาเข้าสู่ระบบก่อน`, หลังแก้ `JoinSessionAsAgent SUCCEEDED` ทั้งกรณีมี `?company=` และไม่มี (fallback เป็นบริษัทของ cs เอง), และยืนยัน company isolation ยังทำงาน (`?company=` เป็นบริษัทอื่น → `403 FORBIDDEN` ที่ negotiate request ตาม middleware เดิม) — ไม่มี test infra สำหรับ SignalR hub ในโปรเจกต์นี้ จึงทดสอบสดเป็นหลัก ไม่มี unit test ใหม่สำหรับจุดนี้ ·
**QA-01 (คำถามพิมพ์อาจถูกบันทึกด้วยสถานะที่เป็นไปได้แค่ทางเสียง)**: `RagVoiceQuestionProvider.AnswerTextAsync` เดิมเช็คด้วย `GeminiRest.IsAnswerStatus()` ซึ่งเป็น union รวมทั้งสองเส้นทาง (มี `no_speech`/`transcription_failed` ปนอยู่) แก้เป็นเช็ค `answered.AnswerStatus is Answered or NotFound or OutOfScope` ตรงๆ เหมือนที่ `GeminiVoiceQuestionProvider.AnswerTextAsync` ทำอยู่แล้ว (TQ-10) · เพิ่ม unit test ใหม่ 3+3 ตัวใน `RagVoiceQuestionProviderAnswerTextTests.cs` (Providers.Tests) ด้วย fake `IHttpClientFactory`/`IEmbeddingProvider`/`IKnowledgeIndexProvider` (ไม่ยิง Gemini/Pinecone จริง) ยืนยัน throw จริงเมื่อได้ `no_speech`/`transcription_failed` และผ่านจริงกับ `answered`/`not_found`/`out_of_scope` ·
**SEC-01 (RAG provider ไม่กัน prompt injection)**: `RagVoiceQuestionProvider.BuildAnswerPrompt` เดิมแปะ transcript/typed text แบบ inline ไม่มี fence — แก้ให้ fence เป็นบล็อก "คำถามของคุณครู (untrusted input)" เหมือนที่ `GeminiVoiceQuestionProvider.BuildTextPrompt` ทำอยู่แล้ว ใช้กับทั้งเส้นเสียงและเส้นพิมพ์ (ใช้ `BuildAnswerPrompt` เดียวกัน รวมถึงเส้น OpenAI-compatible answer ที่เรียก prompt เดียวกันนี้ด้วย) ·
**SEC-02 (ไม่มี rate limiting บน endpoint คำถาม)**: เพิ่ม `IQuestionRateLimiter`/`QuestionRateLimiter` ใหม่ (`Configurations/QuestionRateLimitConfiguration.cs`) แบบ custom service เหมือน `ILoginAccountRateLimiter` เดิม (ไม่ใช่ ASP.NET Core `[EnableRateLimiting]`+`AddPolicy` เพราะ partition-key resolver ของมันรันก่อน model binding อ่าน body/form ไม่ได้) — key = `token+learnerKey` ไม่ใช่ IP (ห้องเรียนจริงอาจอยู่หลัง NAT เดียวกัน) จำกัด 10 ครั้ง/นาทีต่อคู่ token+learnerKey ผูกเข้า `TextQuestionController`/`VoiceQuestionController` ทั้งคู่ คืน `429 RATE_LIMITED` envelope เดียวกับ login · **ยืนยันสดด้วย curl จริง**: ยิงคำถามซ้ำ 10 ครั้งด้วย token+learnerKey เดิมผ่านหมด (502 เพราะ local `.env` ไม่มี `GEMINI_API_KEY` จริง — คาดหมาย ไม่ใช่บั๊ก) ครั้งที่ 11-12 ได้ `429 RATE_LIMITED` จริง, ทดสอบคู่ token+learnerKey อื่นพร้อมกันไม่ถูกบล็อกครอสกัน (isolation ถูกต้อง) ·
**QA-06**: เพิ่ม `RagVoiceQuestionProviderAnswerTextTests.cs` (ข้างบน) + `VoiceQuestionServiceTextNamespaceTests.cs` ใหม่ (Application.Tests) ยืนยัน `AskTextAsync` resolve namespace lesson/category/global จากบริษัทของ token จริง (capturing fake `IVoiceQuestionProvider` + fake `ISlidesProvider` ไม่ยิง Google Slides จริง) — ส่วน "provider ล้มเหลว → ไม่มี row ถูกเขียน" มีอยู่แล้วจากเดิม (`AskTextAsync_WhenSessionEnded_ThrowsTheSameMessageAsTheVoicePath`) ·
**QA-04 (comment ตกค้าง, backend/docs)**: แก้ 3 จุดที่ QA ระบุ (`SessionQuestionController.cs` เพิ่มอ้างอิง `/api/text-question`, `IRealtimeNotifier.cs` ตัดคำว่า "chat" ออก, `docs/BACKEND_DB_HANDOFF.md` ตัด `ChatMessage` ออกจาก ER diagram + คำอธิบาย + migration checklist) — **grep repo-wide ซ้ำทั่ว `backend/`+`docs/`** หา `ChatMessage`/`SendChatMessage`/"chat": เหลือเฉพาะไฟล์ migration `.Designer.cs`/`.cs` เดิม (ประวัติศาสตร์ ห้ามแก้ตามกฎ never edit deployed migration) และ `docs/CORE_FEATURE_SPEC.md`/`docs/TECH_DECISIONS.md`/`docs/schema.dbml` ที่พูดถึง `ChatMessage` ในบริบทมติ/ประวัติของ migration `SplitLinkAndAddAuth` เดิม (ไม่ได้อ้างว่ามีอยู่จริงตอนนี้) — ไม่แก้เพิ่ม ·
**Verify รวม**: `dotnet build SupportRoom.slnx` **0 warning/0 error** · `dotnet test --filter "Category!=Integration"` **240/240 ผ่านหมด** (28 Providers + 202 Application + 10 Api.IntegrationTests, เพิ่มจากเดิม 234 เป็น 240 ด้วย test ใหม่ 6 ตัว) · `dotnet ef migrations has-pending-model-changes` clean (ไม่มี schema change รอบนี้ ตามคาด) · รีสตาร์ตแอปจริงทดสอบทุกจุดด้วย curl/SignalR client จริงตามที่บันทึกไว้ข้างบน ไม่ได้เชื่อแค่ build/test เขียว · **ยังไม่ได้ติ๊ก checkbox ใดใน `plan.md`, ไม่แตะ `design.md`/`requirement.md`** — พร้อมให้ `qa-engineer` verify รอบใหม่ (fix นี้เป็นการแก้บั๊ก ไม่ใช่ทำ FULL รอบ Phase 7-9 เดิมที่ยังค้างอยู่ตามที่ระบุไว้ข้างต้น) · 🆕 **2026-08-24 (`backend-engineer` รอบสอง — แก้ 3 QA residual จากรอบตรวจซ้ำ Codex ภายนอก + QA-04 grep รอบที่ 4)**: **QA-02 residual (Important)** — `SessionHub.EnsureAgentAuthenticated()` เดิมเช็คตัวตนแค่ตอน handshake ครั้งเดียว (`Context.User` เป็น `ClaimsPrincipal` เดียวกันตลอด connection) ไม่เคยเช็ค token หมดอายุซ้ำตอนเรียก hub method ต่างจาก HTTP ที่ `CurrentUserMiddleware` รันทุก request — แก้เพิ่ม 2 ชั้นก่อน `EnsureLearningSessionExists` ทุกครั้ง: (1) อ่าน JWT claim `exp` มาตรฐานเทียบ `DateTimeOffset.UtcNow` เอง throw `HubException` ถ้าหมดอายุแล้ว (2) เรียก `IAuthService.RefreshCurrentUser()` เพิ่มเพื่อเช็คบัญชี/บริษัทจาก DB สดเหมือน HTTP path เป๊ะ — ยืนยันด้วย `dotnet build`+unit test ผ่านหมด (รอบนี้ไม่มี live SignalR client ทดสอบซ้ำแบบรอบก่อน) · เหลือข้อจำกัดที่บันทึกเป็นคอมเมนต์ในโค้ดตรงจุด: connection ที่ `JoinSessionAsAgent` สำเร็จแล้วเปิดค้างไม่เรียก hub method ซ้ำเลยจะยังรับ broadcast ต่อได้จนกว่า connection จะหลุดเอง (ไม่ได้ทำ periodic re-check ผ่าน timer รอบนี้ — ประเมินว่ายังไม่คุ้มต้นทุน background work ต่อ connection จนกว่าจะมีหลักฐานว่าเกิดจริง) · **QA-06 residual (Minor)** — เพิ่ม `VoiceQuestionServiceProviderFailureTests.cs` ใหม่ (Application.Tests) ด้วย fake `IVoiceQuestionProvider` ที่ throw เสมอทั้งสองเมธอด ยืนยัน `AskAsync`/`AskTextAsync` throw `HttpStatusCodeException` (`BadGateway`/`UpstreamError`) จริงและไม่มี `SessionQuestion` row หรือ broadcast เกิดขึ้นเลยเมื่อ provider ล้มเหลว (เคสที่ QA รอบก่อนชี้ว่าของเดิมทดสอบแค่ session `Ended` ไม่ใช่ provider failure จริง) · **SEC-01 residual (Important)** — เปลี่ยนจาก text-fence (marker ข้อความ ปลอมได้ด้วยการพิมพ์ marker เดียวกัน) เป็น privilege separation จริงระดับ request: เพิ่ม `systemInstruction` field ใหม่ใน `GeminiRest.CallAsync` (sibling ของ `contents` ใช้กลไกที่ Gemini เองใช้แยก system/user) · แยก `GeminiVoiceQuestionProvider.BuildTextPrompt`→`BuildTextSystemInstruction` และ `RagVoiceQuestionProvider.BuildAnswerPrompt`→`BuildAnswerSystemInstruction` ให้เหลือแค่กติกา+เอกสารอ้างอิง+schema เท่านั้น แล้วส่งคำถามดิบของผู้เรียนเข้า `contents`/user content อย่างเดียวไม่มีอะไรปนแล้ว (ครอบคลุมทั้งเส้นเสียงและเส้นพิมพ์) · ฝั่ง OpenAI-compatible answer path (`AnswerWithOpenAiAsync`) แก้เช่นกัน — ย้ายกติกา/เอกสารเข้า role `system` เดิมที่มีอยู่แล้ว ส่งเฉพาะคำถามดิบเป็น role `user` แทนที่จะยัดทั้งก้อนเป็น user message เดียว · เพิ่ม unit test ใหม่ `GeminiSystemInstructionSeparationTests.cs` (Providers.Tests) capture request body จริงด้วย fake `HttpMessageHandler` (ไม่ต้องใช้ `GEMINI_API_KEY` จริง) ยืนยันโครงสร้าง 3 ข้อ: คำถามอยู่ใน `contents` เท่านั้น, กติกาอยู่ใน `systemInstruction` เท่านั้น, คำถามที่พิมพ์ปลอม marker เดิมแบบ verbatim ก็ไม่รั่วเข้า `systemInstruction` เลยเพราะคนละ field ตั้งแต่ต้น — **นี่คือ "แก้แล้ว รอ `security` ปิดเคสอย่างเป็นทางการ" ไม่ใช่ fix ที่ backend-engineer ปิดเองได้** · **QA-04 residual (Minor, grep ไม่ครบรอบที่ 4)** — แก้ 5 จุดที่ QA ระบุตรงๆ: `ILearningSessionService.cs`/`ITrainingLinkService.cs` (comment เลิกอ้าง "chat message"/"chat"), `docs/SOLUTION_ARCHITECTURE.md`/`docs/HANDOFF_MASTER.md` (เลิกอ้างว่ามีฟีเจอร์แชตสดอยู่), `backend/docs/ER_DIAGRAM.drawio` (ลบ entity `CHAT_MESSAGE` ทั้งก้อน + edge ที่ชี้เข้าหาออกจากไดอะแกรมจริง ไม่ใช่แค่ comment) — แล้ว grep ซ้ำทั่ว `backend/`, `docs/`, `frontend/docs/` ทั้งภาษาอังกฤษ (`chat`) และไทย (`แชต`) พบเพิ่มอีก 2 จุดที่ยังอ้างว่าเป็น current capability: `docs/PROJECT_CONTEXT.md` (ตาราง role บรรทัด 32-33 — CS มี "แชตช่วยคุณครูสด", คุณครูมี "แชต") และ `docs/UX_UI_WORKFLOWS.md` (WF01 AS-IS flowchart มี node/edge "แชต") แก้ครบทุกจุดที่พบจริงรอบนี้ (ไม่ใช่แค่บอกว่าครบ) — **ยืนยันไม่แก้**: migration files ทุกไฟล์ (ประวัติ ห้ามแก้ deployed migration), `docs/CORE_FEATURE_SPEC.md` (ห้ามแก้ตาม `design.md`), `docs/TECH_DECISIONS.md` (decision log ประวัติ), `docs/schema.dbml`, คำว่า `chat/completions`/`chat-completions`/"Chat model" ที่หมายถึง LLM API ไม่เกี่ยวกับฟีเจอร์ CS chat, และจุดอื่นทั้งหมดที่พูดถึง `ChatMessage`/แชตในบริบท "ถูกลบไปแล้ว"/"ไม่มีอีกต่อไป" ที่ถูกต้องอยู่แล้ว · **Verify รวม (รอบสอง)**: `dotnet build SupportRoom.slnx` **0 warning/0 error** · `dotnet test --filter "Category!=Integration"` **244/244 ผ่านหมด** (30 Providers +2, 204 Application +2, 10 Api.IntegrationTests — เพิ่มจากเดิม 240 เป็น 244 ด้วย test ใหม่ 4 ตัว) · **ยังไม่ได้ติ๊ก checkbox ใดใน `plan.md`, ไม่แตะ `design.md`/`requirement.md`** — พร้อมให้ `qa-engineer` verify รอบใหม่ (fix นี้เป็นการแก้บั๊ก/security residual ไม่ใช่ทำ FULL รอบ Phase 7-9 เดิมที่ยังค้างอยู่) · แนะนำให้เรียก `security` ต่อเพื่อปิดเคส SEC-01 อย่างเป็นทางการ (ตอนนี้เป็นแค่ "fix claimed") |

## 🟡 เรื่องที่ค้างการตัดสินใจ — บันทึกไว้ 2026-08-21 · **อัปเดต 2026-08-22 (รอบปิดท้าย): ข้อ 1 ปิดแล้ว · ข้อ 3 ปิดสมบูรณ์แล้ว (T4-a ปิดเป็นข้อสุดท้าย — ไม่เหลือคำถามธุรกิจใดๆ) · เปิดเต็มตัวอยู่ = ข้อ 2 (CR-1) และข้อ 0 (กลับคำตอบ P1)**

> วันที่มาจาก system context **ยังไม่ได้ให้เจ้าของโปรเจกต์ยืนยันเอง** (เซสชันที่บันทึกไม่มี
> เครื่องมือถามผู้ใช้ และเจ้าของโปรเจกต์กำลังย้ายเครื่อง/ย้ายบัญชี) · **ไม่มีโค้ด ไม่มี checkbox
> ไม่มีมติใดถูกเปลี่ยนในรอบนี้** — บันทึกบริบทอย่างเดียวเพื่อให้เซสชันใหม่ที่ไม่มีความจำต่อได้

0. 🔄 **[เปิดใหม่ 2026-08-22 รอบที่สอง] ค่า pacing — เจ้าของโปรเจกต์กลับคำตอบ P1** ·
   ข้อ 1 ด้านล่างที่เคยขีดฆ่าว่า "ปิดแล้ว" **ไม่จริงอีกต่อไปเฉพาะส่วน P1/P4** · มติใหม่ที่ยืนยันแล้ว:
   **ตัดช่องกรอก pacing ออกจากฟอร์มบทเรียนถาวร (ไม่มี override ต่อบทเรียน) · ทิ้งค่า override เดิม
   ของทุกบทเรียน · ลบคอลัมน์ออกจาก DB จริง** (เหตุผล: UX เดียวกันทั้งระบบ ไม่ซับซ้อนโดยใช่เหตุ) ·
   รายละเอียดเต็มที่ `_docs/module/learning-session/requirement.md` §"🔄 กลับคำตอบ P1 เมื่อ
   2026-08-22 (รอบที่สอง)" · **กระทบ Phase 4/5 ของ `company-admin` ที่ implement แล้วแต่ยังไม่เคย
   ผ่าน QA** — LP-1..LP-15 (โดยเฉพาะ LP-6/LP-11/LP-12/LP-13) และ DM-P2 ต้องถูก amend +
   migration ใหม่สำหรับลบคอลัมน์ · **ผู้รับต่อ: `system-analyst`** (แก้ `company-admin/design.md`)
   แล้วจึง `project-manager` · ⛔ engineer ยังห้ามหยิบไปทำ
1. ~~**ค่า pacing ของบทเรียน → ค่าเริ่มต้นระดับบริษัท**~~ — ⚠️ **ถูกกลับคำตอบบางส่วนแล้ว ดูข้อ 0
   ด้านบนก่อนเชื่อบรรทัดนี้** · ~~✅ **ปิดแล้ว 2026-08-22 ไม่ค้างอีกต่อไป**~~
   · `business-analyst` สัมภาษณ์เจ้าของโปรเจกต์ครบ **P1–P5** (บันทึกใน
   `_docs/module/learning-session/requirement.md` §"🆕 บันทึกไว้เมื่อ 2026-08-21 … ✅ ตอบครบแล้ว
   2026-08-22") และ `system-analyst` ปิด **A5 (แถว pacing) · B3a · B4** ใน
   `_docs/module/company-admin/design.md` แล้ว → **B4 = เพิ่มคอลัมน์ `Default*Ms` แบบ `NOT NULL`
   ลง `Company` ตรงๆ** · การสืบทอดมี **2 ชั้น** (บทเรียน nullable → บริษัท non-null) **ไม่มี env
   fallback ตอน runtime** · งานที่เกิดขึ้นจริงคือ **Module P** พร้อม contract
   `## Lesson Pacing Resolution Rules` (LP-1..LP-15) และ migration ใบเดียว
   `AddCompanyLessonPacingDefaults` · **ขั้นถัดไปคือ `project-manager` วาง phase ไม่ใช่ engineer
   หยิบไปทำเอง** · หนี้ที่ตามมาและยังไม่ปิด: **D-3** — `LessonConfig` ถูกประกาศไว้ใน
   `knowledge-base/design.md` §DM-2 ด้วย ต้องมีรอบ `system-analyst` ของโมดูลนั้นมา amend ให้ตรงกัน
   · แยกต่างหาก: `videoDurationMs` **ยังเป็นงาน UI ล้วนที่ไม่ถูกแตะในรอบนี้** ส่ง
   `frontend-engineer` ได้ตรงๆ ไม่ต้องรออะไร
2. ~~**CR-1 ยุบ knowledge scope บทเรียน/บริษัทให้เหลือกองเดียว**~~ — ❌ **ข้อเสนอเดิมปิดแล้ว
   ไม่ทำ (สัมภาษณ์ `business-analyst` รอบ 1, 2026-08-24)**: เจ้าของโปรเจกต์ตอบว่าคำตอบจาก
   บทเรียนผิด "รับไม่ได้เลย" และสิ่งที่ขอจริงคือ "แบ่งแยกไปเลย ไม่อยากให้ก้าวก่ายกัน" ซึ่งเป็น
   ทิศทางตรงข้ามกับข้อเสนอ → **R3 และมติข้อ 5 ใน `docs/HANDOFF_MASTER.md` ไม่ถูกรื้อ
   ยังมีผลบังคับเต็มที่** (ไม่ต้องแก้ตัวมติ แต่หมายเหตุ 🟡 ที่แขวนไว้ใต้ข้อ 5 ควรถูกอัปเดต
   ให้ตรง — ยังไม่ได้แก้ในรอบนี้) · **ยืนยันซ้ำในรอบ 2 (CR-1.g)**: เจ้าของโปรเจกต์ระบุว่า
   พฤติกรรมการค้นคำตอบวันนี้ถูกต้องแล้วทั้งหมด "เป็นแค่เรื่องมองเห็น/จัดเก็บ ไม่ใช่เรื่องคำตอบ"
   → ปิดทั้งทางที่ scope จะหลวมลงและแคบลง · **สิ่งที่แทนที่เข้ามา — ขึ้นเป็น requirement แล้ว**:
   **R7** หน้าเดียวที่เห็นคลังความรู้ทั้งกอง (เห็นทุก scope พร้อมกัน + รวม Q&A + กรองหมวด/สถานะ +
   ค้นในเนื้อหา + เตือนซ้ำ — ต่อยอด `/admin/documents` ที่มีอยู่ ไม่ใช่หน้าใหม่) และ **R8**
   อัปโหลดที่เดียวแต่จัดระเบียบได้ (วันนี้มีเครื่องอัปโหลดสองที่: `/admin/documents` กับตัวที่ฝัง
   ในหน้าแก้บทเรียนแบบล็อก scope) · ทั้งคู่ **ไม่แตะ retrieval และไม่แตะ Data Model ของ scope** ·
   **ขั้นถัดไป: `system-analyst` (amend) รับ R7 และ R8 ได้ทั้งคู่ ไม่มีคำถามธุรกิจค้าง**
   (CR-1.j ปิดแล้ว 2026-08-24 = "ลบทิ้งไปเลย" → DS-8/Q-C ถูกยกเลิก, ระวัง R8.3 ที่ห้ามลบ
   ทางอัป PDF ตัวสไลด์ไปด้วย) → **⛔ ยังไม่พร้อมส่ง engineer**
3. ~~**ฝั่งผู้เรียน: Responsive มือถือ/แท็บเล็ต + พิมพ์ถามแทนการพูด**~~ — ✅ **ปิดสมบูรณ์แล้ว
   2026-08-22 · R1–R6, T1–T7 และ T4-a ครบทุกข้อ ไม่มีคำถามค้าง · ผู้รับต่อคือ `system-analyst`**
   (ไม่ใช่ capture-only อีกต่อไป) · คำตอบถูกยกขึ้นเป็น
   **F9 (responsive) + F10 (พิมพ์ถาม)** ใน `## Core Features` และบรรทัด F9/F10 ใน `## Scope → MVP`
   ของ `_docs/module/learning-session/requirement.md` (หัวข้อเดิม §"🆕 บันทึกไว้เมื่อ 2026-08-22 —
   ฝั่งผู้เรียน…" เก็บตารางคำตอบทีละข้อไว้) · **สาระที่เคาะแล้ว**: desktop เป็นหลักแต่มือถือ/แท็บเล็ต
   ต้องครบ **ทุก interaction ไม่มีข้อยกเว้น** · **portrait เต็มรูปแบบ ห้ามบังคับหมุนจอ** ·
   ขอบเขตเฉพาะ `/room/*`,`/join/*` **ไม่รวม `/admin/*`** · push-to-talk คงกดค้างแต่ต้องกัน
   scroll/context menu · แตะสไลด์ขยายเต็มจอ · แชต drawer เต็มจอ · ปุ่มจบเข้าถึงได้ตลอด ·
   แป้นพิมพ์ไม่บังช่องพิมพ์/ปุ่มส่ง · **เข้าชุด UX/UI รอบที่กำลังส่งอยู่ตอนนี้** ·
   พิมพ์ถาม **เทียบเท่าการพูด 100%** · **คำถามที่พิมพ์บันทึกลง `SessionQuestion` + เข้าคิวรีวิว F7
   เหมือนกันทุกประการ** · TTS อ่านทุกครั้งไม่มีโหมดเงียบ · หยุดบรรยาย **ตอนกดส่ง** ไม่ใช่ตอนโฟกัส
   ช่องพิมพ์ · readiness เปลี่ยนเป็น **ปุ่มกด "พร้อม/ยังไม่พร้อม"** · **ทำพร้อมชุด responsive** ·
   ✅ **T4-a ปิดแล้วด้วย (2026-08-22 รอบปิดท้าย — ยืนยัน 2 รอบในแชท)**: **ช่องพิมพ์ในห้องเรียน
   คุยกับ AI เท่านั้น · ตัดฟีเจอร์แชตคุยกับเจ้าหน้าที่ CS ระหว่างเรียนออกทั้งหมด · ไม่มีทางสำรอง
   ให้คุยกับคนจริงระหว่างเรียน** (ทางเลือกที่ 1 จาก 3 ทาง = ตัดทิ้งจริง) · ⚠️ **สำคัญ: นี่ไม่ใช่แค่
   ซ่อน UI แต่เป็นคำสั่งรื้อโค้ดเดิมจริง** — `ChatDrawer.tsx`, `use-session-chat.ts`, SignalR
   `SendChatMessage`, ปุ่มเปิดแชตใน `ControlBar` และ entity `ChatMessage` เป็นของที่ต้องถูกลบ
   (เขียนไว้เป็น **F10-a** ใน `## Scope → MVP` และตาราง T4-a ใน `## Open Questions`) ·
   คำสั่งเดิมของ **F7 ที่ให้ย้าย `ChatMessage` ไปผูกกับ "การเรียน" เป็นโมฆะแล้ว**
   (`SessionQuestion` ยังต้องย้ายตามเดิม) — `system-analyst` ต้องเคาะว่าจะลบตาราง/ข้อมูลเดิม
   หรือเก็บไว้อ่านย้อนหลัง ·
   ✅ **อัปเดต 2026-08-23: `system-analyst` รับไปทำแล้ว — `design.md` amend เสร็จ
   (contract 3 ชุด + Data Model + MG-R1 + Module G/H/I + R12–R18)** · 4 ข้อเชิงออกแบบที่เคยค้าง
   (U1–U4) **เจ้าของโปรเจกต์เคาะครบแล้วในวันเดียวกัน และ `system-analyst` amend เป็นมติเรียบร้อย**
   (รวม contract ใหม่ TQ-22..TQ-27 สำหรับการถอด readiness-by-voice ตามมติ U1 + R19/R20) →
   ดู `learning-session/design.md` §Unresolved Open Questions → ✅ มติ U1–U4 (2026-08-23) ·
   ✅ **`project-manager` วาง phase ได้แล้ว** · ⛔ engineer ยังห้ามหยิบจนกว่าจะมี task ใน `plan.md` ·
   *(ข้อความเดิมด้านล่างเก็บไว้เป็นประวัติของการสัมภาษณ์ 2026-08-22)* ·
   ➡️ **สรุปสถานะเดิมของข้อนี้: ปิดสมบูรณ์ ไม่มีคำถามธุรกิจค้างใดๆ ทั้ง F9 (responsive) และ F10
   (พิมพ์ถาม) — พร้อมส่ง `system-analyst` เต็มรูปแบบ** (⛔ **ไม่ใช่ engineer ตรงๆ**) เพราะ
   T2 ยืนยันว่าแตะ `SessionQuestion`/คิวรีวิว F7 จริง, T4 ต้องมี **endpoint ใหม่ที่รับคำถามเป็น
   ข้อความล้วน** (วันนี้ `askVoiceQuestion` บังคับมี `audioBlob` เสมอ) และ T4-a สั่งรื้อโค้ดแชตเดิม ·
   ตอนนี้ `system-analyst` **ปิดสัญญาได้ทั้งชุดรวมถึง "UI ช่อง input สุดท้าย"** ซึ่งเดิมติดที่ T4-a ·
   ขอบเขตอื่นถามปิดท้ายแล้ว เจ้าของโปรเจกต์ยืนยันว่าไม่มีเพิ่ม

## knowledge-base

**ขาเข้าสื่อการสอน + คลังความรู้ที่มีคนดูแลได้** — เกิดจากเจ้าของโปรเจกต์ยกประเด็นว่าระบบ
"ยังไม่ครบวงจร": ครึ่งซ้าย (เตรียมความรู้ → แจกลิงก์ → เรียน → จับคำถาม) ทำงานได้แล้ว
แต่ครึ่งขวา (รีวิว → แก้ความรู้ → รู้ว่าดีขึ้น) ตัน

**🆕 2026-08-24 (`business-analyst`, สัมภาษณ์ 2 รอบ · ไม่มีโค้ด ไม่แตะ checkbox):** ปิด **CR-1**
ครบทั้งหัวข้อ · **ข้อเสนอเดิม (ยุบ knowledge scope เหลือกองเดียว) ถูกปฏิเสธ** — เจ้าของโปรเจกต์
ตอบว่าคำตอบจากบทเรียนผิด "รับไม่ได้เลย" (CR-1.b) และยืนยันที่ CR-1.g ว่าพฤติกรรมการค้นคำตอบ
วันนี้ **ถูกต้องแล้วทั้งหมด** "เป็นแค่เรื่องมองเห็น/จัดเก็บ ไม่ใช่เรื่องคำตอบ" → **R3 +
contract KS-1..KS-11 + `docs/HANDOFF_MASTER.md` ข้อ 5 คงเดิมทั้งชุด ไม่ต้องแก้อะไรเลย** ·
ปัญหาจริงที่ค้นพบแทนถูกยกขึ้นเป็น requirement ใหม่: **R7** (หน้าเดียวที่เห็นคลังทั้งกอง —
R7.1 เห็นทุก scope พร้อมกัน, R7.2 รวม Q&A, R7.3 กรองหมวด/สถานะ, R7.4 ค้นในเนื้อหา,
R7.5 เตือนซ้ำ · **ต่อยอด `/admin/documents` ที่มีอยู่แล้ว ไม่ใช่หน้าใหม่** — ตรวจโค้ดจริงแล้ว
ว่าอะไรมีอยู่แล้วและอะไรยังไม่มี เขียนกำกับไว้ในตาราง R7) และ **R8** (อัปโหลดที่เดียวแต่จัดระเบียบ
ได้ — วันนี้ `DocumentUploadList` ถูกวางสองที่: `/admin/documents` และตัวที่ฝังในหน้าแก้บทเรียน
แบบ `fixedScope` ล็อก scope ตาม DS-8/Q-C) · ทั้ง R7/R8 **ไม่แตะ retrieval และไม่แตะ Data Model
ของ scope** · ✅ **CR-1.j ปิดแล้ว 2026-08-24: "ลบทิ้งไปเลย"** — การ์ด "เอกสารประกอบ" ในหน้า
แก้บทเรียนถูกลบ → **`design.md` DS-8/Q-C (โหมด `fixedScope`) ถูกยกเลิก ต้องให้ `system-analyst`
amend** · พร้อมกันนั้น **R8.3 เป็นขอบเขตห้ามข้ามที่ `business-analyst` เขียนเองหลังตรวจโค้ด**:
หน้าแก้บทเรียนมีทางอัปโหลด **สองทางคนละเรื่องกัน** — การ์ด "เอกสารประกอบ" (ลบ) กับ
`handlePdfUpload` ที่อัป **PDF ตัวสไลด์ของบทเรียนเอง** แล้วสร้าง `slideConfigs`/
`pdfDocumentResourceId` ต่อ (**ห้ามแตะเด็ดขาด** ไม่งั้นสร้างบทเรียนแบบ PDF ไม่ได้อีกเลย
กระทบ R4 ทั้งชุด) · **R8.4**: ลบการ์ดแล้วต้องไม่ตาบอดว่าบทเรียนมีเอกสารอะไร ต้องมีทางไป
หน้าคลังรวมแบบกรองมาที่บทเรียนนั้น (ต่อ R7.3) ·
**§Open Questions ของ `requirement.md` ไม่มีข้อค้างแล้ว — R1–R8 เคาะครบ** ·
⚠️ `docs/HANDOFF_MASTER.md` หมายเหตุ 🟡 ใต้มติข้อ 5 **ยังเขียนว่า "ข้อเสนอค้างอยู่ ยังไม่ตัดสิน"
ซึ่งล้าสมัยแล้ว** — `business-analyst` ไม่แก้เพราะไฟล์อยู่นอกโฟลเดอร์โมดูล (`conventions.md` §1)
ต้องมีคนตัดสินว่าใครแก้ · **ขั้นถัดไป: `system-analyst` (amend) รับ R7 ได้เลย · ⛔ engineer
ยังห้ามหยิบ**

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

🟡 **2026-08-24 — งานใหม่เข้าคิว (ยังไม่สัมภาษณ์): CR-2 · แยก "AI ตอบไม่ได้จริง" ออกจาก
"ระบบพัง"** — บันทึกไว้ที่ `requirement.md` §Open Questions หัวข้อ "🆕 บันทึกไว้เมื่อ 2026-08-24"
ตามคำสั่งเจ้าของโปรเจกต์ ("เพิ่มเป็น req ต่อเลย เราจะได้คุยรวบยอดทีเดียว และลงดีเทลกันตอนถึงคำถามนี้")
· เป็น **capture-only ยังไม่มีคำตอบและยังไม่เป็น requirement ที่ตัดสินแล้ว** — code trace 6 ข้อ
ยืนยันช่องว่างจริง: `AnswerStatus` ไม่มีค่าแทนความล้มเหลวเชิงเทคนิค · `TranscriptionFailed` ซ้อน
สองความหมาย · exception ทุกแบบ (timeout/429/config หาย) ไม่เขียนแถว `SessionQuestion` ลง DB เลย
ทั้งเส้นทางเสียงและพิมพ์ · `NoSpeech` ก็ไม่เคยถูกเขียน · frontend ทิ้ง error code แล้วพูด
"ระบบขัดข้องชั่วคราว" ตายตัวทุกกรณี → CS แยก "ล้มเหลวเชิงเทคนิค" จาก "AI ไม่มีคำตอบ" ไม่ได้เลย ·
⚠️ ชนกับมติเดิม `design.md` §TQ-10 ("ห้ามเพิ่มค่าใหม่ใน `AnswerStatus`") ถ้าคำตอบไปทางเพิ่ม enum
ต้องรื้อมติอย่างตั้งใจ · ⛔ **ห้าม `system-analyst` ออกแบบ / ห้าม engineer หยิบไปทำ จนกว่าจะมีรอบ
`business-analyst` สัมภาษณ์จริง** · ไม่บล็อกงาน QA รอบ 3 ของ Phase 7-9 ที่ค้างอยู่

🆕 **2026-08-23 — `plan.md` amended: Phase 7/8/9 (Module G/H/I) พร้อมส่ง engineer แล้ว**
`design.md` ปิด F9 (responsive ทั้งห้องเรียน + หน้าจบ/หมดอายุ), F10 (พิมพ์ถาม AI แทนพูด),
F10-a (รื้อฟีเจอร์แชตคุยกับ CS ทิ้งทั้งหมด) และมติ U1–U4 ครบเมื่อ 2026-08-23 (U1 = ตัดการตอบ
"พร้อมหรือยัง" ด้วยเสียงทิ้ง เหลือปุ่มกดอย่างเดียว, U2 = เพิ่ม `SessionQuestion.Source`, U3 =
`DropTable ChatMessage` พร้อมข้อมูล, U4 = F9 รวม `/session-ended/[token]` + `/link-expired`) ·
`project-manager` แปลงเป็น **Phase 7: Module G — Typed questions: backend + provider 🔒**,
**Phase 8: Module H — Chat feature removal (ทั้ง stack + migration) 🔒**,
**Phase 9: Module I — Learner responsive & single-input room UI 🔒** ใน `plan.md` แล้ว (ทุก
checkbox ใหม่เป็น `[ ]`, ไม่แตะ checkbox เดิมของ Phase 1–6 เลย) · **Phase 7/8 ทำขนานกันได้ แต่
ต้องเสร็จก่อน Phase 9 เริ่ม** และมีจุดประสานงานที่ต้องอ่าน `plan.md` §Sequencing Notes ก่อนแจก
งาน: (1) migration `RemoveChatMessageAndAddQuestionSource` ไฟล์เดียวกันที่ G/H เขียนคนละส่วน
(2) `IsSensitiveLearnerPath()` รายการเดียวกันที่ G/H แก้คนละบรรทัด (3) readiness-by-voice removal
คร่อม wire contract ระหว่าง Phase 7 (backend) กับ Phase 9 (frontend) ต้อง deploy พร้อมกัน · QA
รอบที่ verify Phase 7/9 ต้องเป็น FULL และ re-verify Module C/D/E ตาม R19/R20 ด้วย ไม่ใช่เชื่อผล
FULL-3 เดิม · R15 (rate limiting บน `/api/text-question`) เป็น open question แยกที่ไม่บล็อก Phase 7

Docs: requirement ✅ (F9/F10/F10-a + U1–U4 ปิดครบ) · design ✅ (ครอบ Module A–I ทั้งหมด รวม
contract RS-1..14/TQ-1..27/CX-1..9/MG-R1) · plan ✅ (50/53 ของ Phase 1–6 checked + **Phase 7–9
ใหม่ 9x/9x/9x tasks ยังเป็น `[ ]` ทั้งหมด รอ engineer**) · review ⚠️
(**verified ⚠️ (FULL)** สำหรับ Phase 1–6; 50/53 tasks ✅, 3 open pending manual browser test ·
Phase 7–9 ยังไม่เคยเข้า QA) · deploy.md ⚠️ (local-only claim not yet independently re-verified by
this QA round; not deployed to production regardless)

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

Docs: requirement ✅ (amend 2026-08-25 รอบสอง — **F5 เคาะกฎครบแล้ว (F5.2.1–F5.2.7 + F5.3) · เหลือ OQ-15 เปิดอยู่ข้อเดียว ไม่บล็อกการออกแบบ**) · design ✅ (**Module A + Module P + Module U เป็น contract แล้ว · Module B/C ที่เหลือยังพัก** — amend 2026-08-25 รอบที่ 8 · **CP-15 แก้แล้ว · Module U (F5) ไม่มี schema change**) · plan ⚠️ (2026-08-21, 2 phase, 22/22 tasks checked — checkbox เป็นของ QA · **ยังไม่มี phase ของ Module U (F5) เลย — `project-manager` ต้องเพิ่มก่อน engineer หยิบ**) · review ✅ (QA TARGETED-1) · security ⚠️ (SECURITY-1, SEC-01–03 remediation implemented; re-audit pending)

> **🔓 เปิด scope กลับเมื่อ 2026-08-25 (`business-analyst`) — F5 · รีเซ็ตรหัสผ่าน/แก้อีเมลของ
> user รายอื่น**: ข้อห้ามเดิม *"ใช้ `/admin/users` เดิมได้อยู่แล้ว"* (requirement รอบปิด scope
> ข้อ 1 + Constraints + `design.md` CP-15) **ปิดไปบนข้อเท็จจริงที่ผิด** — ตรวจโค้ดจริงแล้ว
> `UpdateAdminUserDto` มีแค่ `DisplayName`/`Role`/`IsActive` และทางเดียวที่เปลี่ยนรหัสได้ทั้งระบบ
> คือ `/api/auth/change-password` (ของตัวเอง + ต้องรู้รหัสเดิม) → **ไม่มีเส้นทางรีเซ็ตรหัสให้คนอื่น
> เลย ถ้า cs ลืมรหัสวันนี้ต้องแก้ที่ database**
> ~~**⛔ บล็อกอยู่ที่: เจ้าของโปรเจกต์ต้องตอบ OQ-9..OQ-15**~~
> **✅ ปลดบล็อกแล้ว 2026-08-25 (`business-analyst` รอบสองของวัน)** — เจ้าของโปรเจกต์ตอบ
> **OQ-9..OQ-14 ครบทั้งหกข้อ** และถูกเขียนเป็นกฎธุรกิจใน `requirement.md` **F5.2.1–F5.2.7**:
> กฎสิทธิ์เดิม **+ peer-lockout** (ห้ามทำกับ role ระดับเดียวกัน · `owner` ยกเว้น) ·
> รีเซ็ตรหัส = พิมพ์รหัสชั่วคราวเอง + `MustChangePassword = true` แบบเดียวกับ flow สร้างผู้ใช้ ·
> แก้อีเมลได้อิสระ **แต่ต้องติดธง `MustChangePassword` เสมอ** (กันยึดบัญชีเงียบๆ) ·
> **ไม่ทำ audit log** ใช้ `UpdateBy`/`UpdateDate` เดิม · โมดัล **แทนที่** control เดิมในตาราง
> (ลบ `<Select>` role + ปุ่มเปิด/ปิดออกจริง) · **ห้ามใช้กับบัญชีตัวเองเป็นกฎแข็ง**
> (รหัสตัวเองไปที่ `/api/auth/change-password` เดิมซึ่งบังคับให้รู้รหัสเดิม)
> · **F5.3 ยืนยันแล้วว่า F5 ทั้งก้อนไม่ต้องมี migration** ใช้เฉพาะคอลัมน์ที่มีอยู่แล้วบน `AdminUser`
> · **🔴 เหลือ OQ-15 เปิดอยู่ข้อเดียว**: label ปุ่ม Cancel ในโมดัล Figma เขียนว่า "เรียนอีกครั้ง"
> — เจ้าของโปรเจกต์ **ไม่อนุญาตให้แก้เอง** ต้องเช็คกับคนทำไฟล์ Figma ก่อน (ดู F5.4) ·
> **`frontend-engineer` ห้ามเปลี่ยน label นี้เองตอน implement** · ไม่บล็อกการออกแบบ/implement
> ส่วนอื่นของ F5
> ~~**ผู้รับต่อตอนนี้: `system-analyst`**~~ **✅ `system-analyst` amend เสร็จแล้ว 2026-08-25
> (design รอบที่ 8 · doc-only ไม่มีโค้ด ไม่มี schema change)** — **CP-15 แก้แล้ว** (ครึ่งหลัง
> "ใช้ `/admin/users` เดิมได้อยู่แล้ว" ถูกยกเลิกโดยระบุว่าเป็นการแก้ข้อเท็จจริงที่ผิด ไม่ใช่การ
> เปลี่ยนใจ · ส่วน "ห้ามสร้าง _หน้า_ ใหม่" ยังใช้อยู่ · บูลเล็ต role model แก้ขอบเขตเป็น
> "เพิ่มเมธอดใหม่ได้ แก้ของเดิมไม่ได้") · **contract ใหม่ `## Admin User Management Rules`
> (AU-1..AU-16)** + **`## Data Model` §Module U (ไม่มี schema change เลย ยืนยันตรง F5.3)** +
> **`## Modules` §Module U พร้อม 🔒 Security gate 6 เหตุผล** + **R-18/R-19/R-20/D-5** +
> **OQ-U1/OQ-U2/OQ-U3** (ไม่บล็อกการ implement — ตัดสินไว้ในทิศไม่ขยายสโคปเอง)
> · **จุดที่ `project-manager` ห้ามพลาด**: (1) ทุก phase ของ Module U ต้องติด `🔒 Security gate`
> รวม phase ที่ทำแค่ UI (2) **R-19 — `Email` เป็น required = breaking wire contract →
> phase backend กับ phase frontend ต้อง deploy พร้อมกัน** ไม่งั้นหน้า `/admin/users` ที่ใช้อยู่
> จริงพังทุกการกด (3) เป็น **regression surface ของ Module A** (แก้ `Update` ที่ QA ผ่านแล้ว
> และแก้ไฟล์ DTO ที่ `Create` ใช้ร่วมกัน)
> **ผู้รับต่อตอนนี้: `project-manager`** (แตก phase ของ Module U จาก `design.md` §Module U +
> AU-1..AU-16) → แล้วค่อย `backend-engineer` → `frontend-engineer` → `qa-engineer` → `security`

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

**เส้นทางที่สองที่เปิดขึ้นใหม่ 2026-08-22 (ขนานกัน ไม่ติดกับ SEC-01–03)**: **Module P** design
เป็น contract แล้ว → **`project-manager` วาง phase ใหม่** (ไม่ใช่ส่ง engineer ตรง เพราะเป็นงาน
หลายชั้น: migration + backend + 2 endpoint + frontend 2 หน้า + tests และแตะโค้ด Phase 1 ที่ QA
ผ่านไปแล้ว — R-12) · phase ใหม่ต้องติด `🔒 Security gate`

**🆕 Module P · Lesson Pacing Defaults — design ปิดแล้ว 2026-08-22 · ยังไม่มี phase**

ค่า `introWaitMs`/`breathPauseMs`/`finalQuestionWaitMs` ย้ายเป็น "ค่าเริ่มต้นระดับบริษัท +
บทเรียน override ได้" · **มติ B4 = คอลัมน์บน `Company` ตรงๆ ไม่มีตารางใหม่** — กฎ null สองแบบ
ในตารางเดียวแสดงออกด้วยชนิดของคอลัมน์ (pacing `NOT NULL` · ค่าอื่นของ F2 nullable) ไม่มี flag พิเศษ

- **schema change จริง 2 จุด**: `Company` +3 คอลัมน์ `Default*Ms` (additive + backfill) ·
  `LessonConfig` 3 คอลัมน์เป็น `int?` (**ขยายชนิด — ข้อมูลปลอดภัย แต่ breaking กับ contract ของ
  DTO/ViewModel/`domain.ts`** ดู R-10) · migration ใบเดียว `AddCompanyLessonPacingDefaults`
- **contract ที่ engineer ต้องอ่านเต็มก่อนลงมือ**: `## Lesson Pacing Resolution Rules`
  (LP-1..LP-15) — resolve จุดเดียว · **`0` ไม่ใช่ `null`** · endpoint+สิทธิ์ (`cs` อ่านได้ เขียนไม่ได้)
  · ต้องแก้ค่า default ที่เพี้ยนสองจุดในรอบเดียวกัน (P2)
- **ติด 🔒 Security gate** ด้วยเหตุผลของตัวเอง (endpoint แรกที่ให้ `admin` เขียนลง `Company`
  โดย `companyId` มาจาก path parameter)
- ~~**หนี้ที่ยังไม่ปิด — D-3**: `LessonConfig` ถูกประกาศไว้ใน `knowledge-base/design.md` §DM-2 ด้วย
  ต้องมีรอบ `system-analyst` ของโมดูล `knowledge-base` มา amend ให้ตรงกัน~~ ✅ **ปิดแล้ว
  2026-08-25** — `system-analyst` รอบแยกของโฟลเดอร์ `knowledge-base` ลบสามฟิลด์ออกจาก DM-2 แล้ว
  (ตามคำตอบรอบสองของ D-3: "ลบทิ้ง" ไม่ใช่ "แก้เป็น nullable") · ดู `## Change Log` รายการ
  2026-08-25 ของ `_docs/module/knowledge-base/design.md`
- **ตัวเลขที่ยังไม่ผ่านปากเจ้าของโปรเจกต์**: ช่วงค่าที่รับได้ของ pacing (LP-8: 0–60000 / 0–10000 /
  0–120000 ms) เป็นข้อเสนอของ `system-analyst` — แก้ทีหลังราคาถูก (validation ล้วน ไม่ใช่ชนิดคอลัมน์)

**F2 / Module B (ลิงก์หมดอายุ / TTS / แบรนด์) + Module C ยังพักไว้**: คำถาม **A2 · A3 · A4 · A6 ·
B3b** ยังเปิดอยู่แต่ **ไม่บล็อก Module A และไม่บล็อก Module P** · **รูปร่าง schema ไม่ต้องคิดใหม่แล้ว**
(B4 ปิดแล้ว) แต่ **ห้าม implement คอลัมน์เหล่านั้นล่วงหน้า** (R-4 + CP-15 + LP-15) ·
trigger ที่จะปลุก = scb (หรือลูกค้ารายอื่น) ขอแบรนด์/เสียง/อายุลิงก์เป็นของตัวเอง ·
วันปลุกต้อง re-run STATE: ANALYZE ของส่วนนั้นใหม่ตามกฎ deferred module

**อัปเดต 2026-08-22 (`frontend-engineer`): Phase 5 — Company Settings Page implemented ✅ พร้อม
QA** (Phase 4 backend `[backend]`/`[frontend]` ฟอร์มบทเรียน ยังคงรอ QA FULL แยกต่างหากตามเดิม
ไม่ถูกแตะในรอบนี้) — งานทั้งหมดเป็น `[frontend]` ตาม `design.md` §Company Settings Page Rules
(SP-1..SP-15) ครบ 17 task ใน `plan.md` §Phase 5 (checkbox ยังเป็น `[ ]` ทั้งหมดตาม convention —
รอ `qa-engineer` ติ๊ก):

- ไฟล์ใหม่: `frontend/src/components/admin/settings/section-access.ts` (`SettingsSectionAccess`
  + `resolveSectionAccess`, SP-15 ข้อ 1/3), `lesson-pacing-fields.ts` (parse+validate ช่วง LP-8
  เป็น pure function, SP-8/SP-14), `LessonPacingSettingsSection.tsx` (โหลด/เซฟ/validate/ตัดสิน
  สิทธิ์ของตัวเองครบ SP-2, ประกาศ `LESSON_PACING_SECTION_ACCESS` ตาม SP-15 ข้อ 2), `sections.ts`
  (registry กลาง SP-15 ข้อ 4), `frontend/src/app/admin/settings/page.tsx` (ประกอบอย่างเดียว, empty
  state สองเคสตาม SP-12, ไม่มี `Tabs`/ปุ่มบันทึกรวมตาม SP-1/SP-2)
- แก้ `api-client.ts` เพิ่ม `updateCompanyLessonPacing()` (payload ตรง `UpdateCompanyLessonPacingDto`
  จริงที่อ่านจากไฟล์ backend แล้ว ไม่ได้เดา) และ `AdminSidebar.tsx` — ย้าย gate ของกลุ่ม "ตั้งค่า"
  ลงระดับรายการ: "ผู้ใช้งาน" ยัง `!== "cs"` เหมือนเดิม, "ตั้งค่าบริษัท" ใหม่ derive จาก
  `sections.ts` registry ผ่าน `resolveSectionAccess` (ไม่ hardcode role ตาม SP-5/SP-15 ข้อ 7) —
  ผลลัพธ์ปัจจุบันคือแสดงทุก role รวม `cs` ตามที่ LP-9 ตั้งใจ
- test ใหม่: `section-access.test.ts` (3 role + 1 เคส `visibleToRoles` ไม่มี role → `canEdit=false`,
  SP-15 ข้อ 10) และ `lesson-pacing-fields.test.ts` (`0` ผ่าน · ค่าสูงสุดผ่าน · สูงสุด+1 ถูกปฏิเสธ ·
  ช่องว่างถูกปฏิเสธไม่กลายเป็น `0`, SP-14) — ทั้งสองไฟล์เขียนเป็น literal/pure function แยกจาก
  React component โดยตั้งใจ (import `.tsx` ตรงเข้า Vitest ชนกับ `tsconfig.json` ที่ตั้ง
  `jsx: "preserve"` ให้ Next.js ใช้ — Vitest esbuild parse ไม่ได้ ไม่ใช่บั๊กของโค้ด)
- ไม่แตะ `admin/lessons/[slug]/page.tsx`/`admin/lessons/new/page.tsx` เลยตามข้อห้าม SP-13/
  Sequencing Notes · ไม่แตะ backend แม้แต่บรรทัดเดียว · ไม่มี section อื่นของ F2 เพิ่ม
- Verify (Node 22): `typecheck` ✅ · `lint` ✅ · `test` ✅ (60/60 ผ่านทั้งหมด รวม 8 test ใหม่) ·
  `build` ✅ (route `/admin/settings` compiled)
- **ยังไม่ผ่าน `qa-engineer`/`security`** — Phase 5 ติด `🔒 Security gate` ของ Module P
  เหมือนเดิม ต้องเรียก `security` เองด้วยชื่อก่อน deploy ตามกฎ (จุดที่ต้องยิง `PUT` ตรงด้วย JWT
  ของ `cs` เพื่อยืนยัน 403 จริง ไม่ใช่แค่เชื่อว่าปุ่มหาย ตามที่ `plan.md` เตือนไว้)

**⚠️ อัปเดต 2026-08-22 (`business-analyst`, รอบที่สองของวัน): Phase 4/5 มี requirement เปลี่ยน
ก่อนได้ QA** — เจ้าของโปรเจกต์กลับคำตอบ P1 ของค่า pacing: **ไม่มี override ต่อบทเรียนอีกต่อไป
(ตัดช่องออกจากฟอร์มบทเรียนถาวร) · ทิ้งค่า override เดิมทั้งหมด · ลบคอลัมน์ออกจาก DB จริง** ·
บันทึกที่ `_docs/module/learning-session/requirement.md` §"🔄 กลับคำตอบ P1 เมื่อ 2026-08-22
(รอบที่สอง)" · **อย่าเพิ่งส่ง Phase 4/5 เข้า QA ตามสัญญาเดิม** — LP-1..LP-15 (โดยเฉพาะ
LP-6/LP-11/LP-12/LP-13 เรื่อง override + empty-vs-zero) และ DM-P2 ใน `company-admin/design.md`
ต้องถูก `system-analyst` amend ก่อน แล้วจึง `project-manager` จัดลำดับการถอดโค้ดส่วนที่เกินออก

**✅ อัปเดต 2026-08-22 (`system-analyst`, รอบที่ 7): `company-admin/design.md` amend เสร็จแล้ว
ตรงกับ N1/N2/N3** — `## Lesson Pacing Resolution Rules` เหลือโมเดลสืบทอดชั้นเดียว (อ่านค่าจาก
`Company` ตรงๆ, ตัด LP-6/LP-11/LP-12/LP-13 เดิมทิ้ง) · **DM-P2 กลับทิศทางเป็นครั้งที่สอง**:
ไม่มีคอลัมน์ pacing ใน `LessonConfig` เลย (ไม่ใช่ nullable อีกต่อไป) · เพิ่ม migration ใบใหม่
**`RemoveLessonConfigPacingOverrides`** (`DropColumn` สามคอลัมน์) เข้า Migration Plan พร้อม
ข้อบังคับ "ห้ามมี `UPDATE` กู้ค่าเดิมก่อนลบ" (ขัด N2 ตรงๆ) และ "ต้อง deploy ติดกับโค้ดที่เลิกอ่าน
คอลัมน์นี้เสมอ ไม่งั้น query `LessonConfig` ทั้งตารางพัง ไม่ใช่แค่ฟีเจอร์ pacing" · เพิ่ม **R-16**
(data loss ที่ตั้งใจ ยอมรับแล้วพร้อม 3 เงื่อนไข — comment เจตนาใน migration, `devops` backup
`LessonConfig` ก่อนรัน, คำตอบลูกค้าคือตั้งค่ากลางใหม่ไม่ใช่กู้ค่าเดิม) และ **R-17** (Phase 4/5
implement ครบแล้วแต่ยังไม่ผ่าน QA — ความเสี่ยงจริงคือถอด "ครึ่งทาง"; `qa-engineer` ต้องถือรอบแรก
เป็น FULL เสมอ) · §Modules แบ่งงานเป็น "ยังถูกต้องไม่ต้องทำซ้ำ" (`Company` +3 คอลัมน์, endpoint
LP-9, หน้า `/admin/settings`+registry) กับ "ต้องถอด/แก้ย้อนหลัง" (migration ใหม่, entity/DTO/
ViewModel/`domain.ts` ตัดสามฟิลด์, ฟอร์มบทเรียนถอดสามช่อง, ชะตากรรมของ `ILessonPacingResolver`
ให้ PM/engineer ตัดสินเอง) ให้ `project-manager` ใช้ตั้ง task ตรงๆ ไม่ต้องเดา

**D-3 เปลี่ยนคำตอบเป็นรอบที่สอง (ยังไม่ปิด)**: จาก "amend `knowledge-base/design.md` §DM-2 ให้
เป็น nullable" เป็น **"ลบสามฟิลด์ pacing ออกจาก DM-2 ไปเลย"** — ต้องสั่งรอบ `system-analyst`
แยกต่างหากให้โฟลเดอร์ `knowledge-base` เพราะ `conventions.md` §1 ห้ามเขียนนอกโฟลเดอร์ที่ resolve ไว้

**ขั้นต่อไป**: ส่ง **`project-manager`** จัดลำดับงานแก้ Phase 4/5 (แก้ในเฟสเดิมหรือเปิดใหม่ทับ
ให้ PM ตัดสินเอง) ตาม 3 กลุ่มงานที่ `design.md` §Modules เตรียมไว้ให้แล้ว — ⛔ ยังไม่เรียก
`qa-engineer`/`security` กับ Phase 4/5 จนกว่าโค้ดจะแก้ตรงกับ design ใหม่ก่อน

**✅ อัปเดต 2026-08-22 (`backend-engineer` + `frontend-engineer`): Phase 4 "ต้องถอด/แก้ย้อนหลัง"
ตาม `plan.md` §Phase 4 รอบที่ 7 เสร็จครบทั้งก้อน (backend + frontend) — พร้อม `qa-engineer` FULL**

- **[backend]** (รายงานจาก `backend-engineer`): migration ใหม่ `RemoveLessonConfigPacingOverrides`
  (`DropColumn` สามคอลัมน์ pacing ออกจาก `LessonConfig`, แยกไฟล์จาก `AddCompanyLessonPacingDefaults`
  เดิมตามข้อห้าม) applied กับ local Postgres จริงแล้ว · ลบ `IntroWaitMs`/`BreathPauseMs`/
  `FinalQuestionWaitMs` ออกจาก `LessonConfig` entity/`LessonConfigDto`/`LessonConfigViewModel` ครบ
  (ยืนยันแล้วจากการอ่านไฟล์จริงทั้งสองไฟล์ก่อนแก้ frontend — ไม่มีสามฟิลด์นี้เหลือ) · ลบ
  `ILessonPacingResolver`/`LessonPacingResolver` ทิ้งทั้งคู่ · `GetTeachingContentByLinkAsync` อ่าน
  `Company.Default*Ms` ตรงๆ ไม่มี merge · `SaveAsync` ไม่แตะค่า pacing เลย
- **[frontend]** (`frontend-engineer`, รอบนี้): `frontend/src/types/domain.ts` — ลบ 3 ฟิลด์ pacing
  ออกจาก `LessonConfig` type ให้ตรง `LessonConfigViewModel` จริง (`LearnerLessonConfig` ไม่แตะ ยัง
  ประกาศ `number` ตรงๆ เหมือนเดิมตาม task ที่ทำเครื่องหมาย "ยังถูกต้อง") · `admin/lessons/[slug]/page.tsx`
  — ถอด Card "จังหวะเวลา (ทั้งบทเรียน)" ทั้ง 3 ช่องกรอก + state/handler ที่เกี่ยวข้องออกทั้งหมด, เลิก
  เรียก `getCompanyLessonPacing()`/`useAdminSession()` จากหน้านี้ (ฟังก์ชันใน `api-client.ts` ยังอยู่
  ให้ Phase 5 ใช้ต่อ ไม่ได้ลบ) · `admin/lessons/new/page.tsx` — ถอด 3 คีย์ pacing ออกจาก
  `emptyForm`/`LessonConfigInput` payload ทั้งหมด (ไม่ใช่ `null` อีกต่อไป) · `use-tutor-session.ts`
  ไม่แตะตามคำสั่ง (fallback ถูกต้องอยู่แล้วจากรอบก่อน) · ไม่พบ unit test เดิมของฟอร์มบทเรียนที่ทดสอบ
  placeholder/empty-vs-zero ของช่อง pacing ในโค้ดจริง (ไม่มีไฟล์ `.test.*` ใต้ `admin/lessons/`) —
  จึงไม่มีอะไรให้ลบในส่วนนี้
- Verify (Node 22, ยืนยันจริงทั้ง 4 คำสั่ง): `typecheck` ✅ · `lint` ✅ · `test` ✅ (60/60 ผ่านทั้งหมด
  ไม่มี regression) · `build` ✅ (ทุก route รวม `/admin/lessons/[slug]`, `/admin/lessons/new` compile
  สำเร็จ)
- **ยังไม่ผ่าน `qa-engineer`/`security`** — ตาม R-17 รอบแรกของ Phase 4 **ต้องเป็น FULL เสมอ ไม่มี
  TARGETED** เพราะ contract เปลี่ยนทิศระหว่างทาง (ครอบคลุม regression surface ของ Phase 1 —
  `ICompanyService.Create`/`SeedFirstCompanyIfEmpty` — ตาม R-12 ด้วย) · ไม่ได้แก้ `plan.md` checkbox
  ใดๆ (สิทธิ์ของ `qa-engineer` เท่านั้น) ไม่ได้แก้ `design.md`/`requirement.md`
- **ขั้นต่อไป**: เรียก `qa-engineer` FULL รอบแรกของ Phase 4 (ครอบทั้ง backend+frontend+regression
  surface Phase 1) — ยังไม่เรียก `security` จนกว่า QA จะผ่านก่อน

**✅ อัปเดต 2026-08-22 (`project-manager`, แก้ตาม QA ภายนอกที่ผู้ใช้แจ้ง)**: `plan.md` §Phase 5 —
แก้ text ของ task "เขียนคำอธิบายบนจอ section จังหวะการสอน" ที่ยังอ้างอิงโมเดล per-lesson override
เดิม (ตกหล่นตอน amend รอบที่ 7) ให้ตรง `design.md` LP-7/SP-10 ฉบับปัจจุบัน (ค่ามีผลกับทุกบทเรียนของ
บริษัท ไม่ใช่แค่บทที่ปล่อยว่าง) — แก้เฉพาะ task text + Change Log ไม่แตะ checkbox/`design.md`/
`requirement.md` · **ขั้นต่อไป**: ส่ง `frontend-engineer` แก้ component จริง
(`LessonPacingSettingsSection.tsx`) ให้ข้อความบนจอตรงกับ task ที่แก้แล้ว
