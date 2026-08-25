# company-admin (Module A · Company Provisioning) — Implementation Plan

## Plan Summary

โปรเจกต์นี้ scaffold ไว้แล้ว (ASP.NET Core .NET 10 + EF Core/PostgreSQL ฝั่ง `backend/`,
Next.js 15 + React 19 ฝั่ง `frontend/`) — ไม่ต้องใช้ `setup` agent

แผนนี้ครอบเฉพาะ **Module A (F1 — สร้างบริษัทใหม่แบบฟอร์มเดียวจบ)** ตามที่ `design.md`
ล็อกไว้ชัดว่า Module B/C ยังพักไว้ ไม่วางแผนในรอบนี้ Module A **ไม่แก้ schema เลยแม้แต่ฟิลด์เดียว**
— ทุกงานอยู่ระดับ service/endpoint/UI บนตาราง `Company`/`AdminUser`/`KnowledgeCategory` เดิม
มี migration เดียวและเป็น data-only (`BackfillMissingDefaultCategoryChain`)

แบ่งเป็น 2 phase เรียงตามการพึ่งพา ไม่ใช่ตามความซับซ้อน: **Phase 1 (backend) ต้องเสร็จก่อน
Phase 2 (frontend)** เพราะหน้าจอ Phase 2 ทั้งหมด — ฟอร์มสร้างบริษัท, หน้ารายการบริษัท,
ปุ่มปิด/เปิดใช้งาน — เรียก endpoint ที่ Phase 1 สร้าง/ขยายอยู่ (`POST /api/companies` ที่ขยาย
payload แล้ว และ `GET /api/companies/all` ที่เป็นของใหม่) แม้ Phase 1 จะไม่มี task `[frontend]`
เลยก็ตาม — สร้างบริษัทได้แต่ดูรายการไม่ได้ยังใช้งานจริงไม่ได้ ตาม `design.md` §Modules
("ส่งมอบครึ่งเดียวไม่ได้") ซึ่งเป็นเหตุผลที่ทั้งสอง phase ยังอยู่ใน Module เดียวไม่แยก module
แค่แยก phase ตาม role

**ทั้งสอง phase ติด `🔒 Security gate` ไม่มีข้อยกเว้น** ตามคำสั่งตรงจาก `system-analyst`
ใน `design.md` §Modules → Module A → "🔒 Security gate — คำสั่งถึง `project-manager`" — แม้ Phase 2
จะเป็นงาน UI ล้วนก็ยังต่อกับ endpoint ที่สร้าง tenant/บัญชีใหม่โดยตรง

โปรเจกต์นี้มี test script ทั้งสองฝั่ง (`dotnet test` backend, `npm run test` frontend ผ่าน Vitest)
จึงมี task เขียน test สำหรับกฎที่พลาดแล้วเสียหายหนัก (CP-6 single-commit, CH-3 invariant, CP-4/CP-5
ข้อความ error, CP-8 Role hardcode) ไม่ใช่ "เขียน test ให้ครบ phase" แบบเหมารวม

**เพิ่ม 2026-08-22 (amend)**: **Phase 4: Lesson Pacing Defaults — Module P** ตาม `design.md`
§Module P ที่ `system-analyst` เพิ่งเคาะเสร็จวันเดียวกัน (contract `## Lesson Pacing Resolution Rules`,
LP-1..LP-15) ไม่แยก migration/backend/frontend ออกเป็นหลาย phase ตามคำสั่งตรงใน `design.md`
§Modules → Module P ("ปล่อย migration ไปโดยไม่มี resolver = บทเรียนที่ `null` ไม่มีค่าให้ใช้ ·
ปล่อย backend ไปโดยไม่แก้ฟอร์ม = CS ยังกรอกเลขซ้ำเหมือนเดิม") จึงเป็น **phase เดียวมี
[backend]/[frontend] คู่กัน** เหมือน Module A แต่รวมไว้ใน phase เดียวเพราะขนาดงานเล็กกว่า
Module A ทั้งก้อน (ไม่มีเหตุผลให้แยกตาม role แบบ Phase 1/2) — **ติด `🔒 Security gate`**
เพราะเป็น endpoint แรกที่ให้ `admin` เขียนค่าลง `Company` โดย `companyId` มาจาก path parameter
(ดูเหตุผลเต็มที่หัวข้อ Phase 4) และ **แตะโค้ดของ Phase 1 ที่ verified ไปแล้ว** สองจุด
(`ICompanyService.Create`/`SeedFirstCompanyIfEmpty`) ซึ่งต้องถือเป็น regression surface ไม่ใช่
โค้ดใหม่ล้วน (R-12)

**เพิ่ม 2026-08-22 (amend รอบสอง): Phase 5: Company Settings Page — Module P** ตาม
`design.md` §Company Settings Page Rules (SP-1..SP-15) ที่ `system-analyst` เพิ่งปิด open
question ครบ (A8/A6-เฉพาะส่วนนี้/LP-8 ปิดหมดวันเดียวกัน) — **เปิดเป็น phase ใหม่แยกจาก Phase 4**
แทนการต่อท้าย เพราะ Phase 4 กำลังรอ QA FULL รอบแรกทั้งก้อน (มี regression surface ของ Phase 1
ตาม R-12 ต้องตรวจแยก) และ Phase 5 เป็นงานคนละก้อน (หน้าจอใหม่ + กลไกสิทธิ์ต่อ section) ที่ยังไม่
เคย implement เลย — Phase 5 **เป็น `[frontend]` ล้วน 100% ไม่มีงาน `[backend]`** เพราะ endpoint
`GET`/`PUT /api/companies/{companyId}/lesson-pacing` (LP-9) implement + ทดสอบสดกับ JWT จริง
ไปแล้วใน Phase 4 · ติด `🔒 Security gate` เหมือน phase อื่นของ Module P (เหตุผลใหม่: หน้าที่แก้
ค่าระดับบริษัทได้จาก client ครั้งแรก + permission-gating logic ใหม่ `section-access.ts`/SP-15
ต้อง enforce ถูกทั้ง UI และ server + เป็นจอแรกที่ทำให้ `cs` เห็นเมนูกลุ่ม "ตั้งค่า", R-14)

**🔄 อัปเดต 2026-08-22 (amend รอบที่สาม) — Phase 4 แก้ไขในเฟสเดิม ไม่เปิด Phase 6**: เจ้าของโปรเจกต์
กลับคำตอบ P1 หลังเห็นหน้าจอจริง (มติ N1/N2/N3, `learning-session/requirement.md` §"🔄 กลับคำตอบ P1
รอบที่สอง") — pacing กลับเป็น **"ค่ากลางระดับบริษัทล้วน ไม่มี override ต่อบทเรียน"** `system-analyst`
amend `design.md` §Module P + `## Lesson Pacing Resolution Rules` เสร็จแล้ว (LP-1/4/5/7/12/13/14/15
เขียนใหม่ · LP-3/6/11 ยกเลิก) พร้อม migration ใหม่ `RemoveLessonConfigPacingOverrides` — **เลือกแก้
task ของ Phase 4 ในที่เดิมด้วย `Edit` แทนการเปิด Phase 6 ใหม่** เพราะ Phase 4 ยังไม่เคยผ่าน QA/deploy
เลยสักครั้ง (ทุก task ยังเป็น `[ ]`) การเปิด phase ใหม่ทับจะทำให้ `plan.md` มีงานสองชุดขัดแย้งกัน
ในไฟล์เดียว (task เดิมบอก "เพิ่มช่อง `int?`" กับ task ใหม่บอก "ลบคอลัมน์เดียวกัน") ซึ่งจะทำให้ engineer
สับสนว่าต้องทำชุดไหน — Phase 5 (Company Settings Page) **ไม่แก้เลยแม้แต่ task เดียว** เพราะ
`design.md` ยืนยันว่า SP-1..SP-15 ทั้งชุด "ยังถูกต้อง ไม่ต้องทำซ้ำ" ไม่กระทบจาก N1/N2/N3 — task ของ
Phase 4 แต่ละอันถูกทำเครื่องหมายไว้ชัดว่าอยู่กลุ่มไหนจากตาราง 3 กลุ่มใน `design.md` §Module P
("ยังถูกต้อง ไม่ต้องทำซ้ำ" / "ต้องถอด/แก้ย้อนหลัง" / "งานใหม่") — **ไม่มี task ใดถูกติ๊ก `[x]` เอง**
แม้จะทำเครื่องหมายว่า "implemented แล้ว" เพราะ checkbox เป็นสิทธิ์ของ `qa-engineer` เท่านั้นตาม
`conventions.md` §4 · หนี้ข้ามโมดูล D-3 (`knowledge-base/design.md` §DM-2) ยังไม่ปิด บันทึกไว้ใน
Sequencing Notes เป็น note ไม่ใช่ task ของ phase นี้ (คำตอบที่ถูกต้องเปลี่ยนจาก "แก้เป็น nullable"
เป็น "ลบสามฟิลด์ออกจาก DM-2")

**เพิ่ม 2026-08-25 (amend รอบที่สี่): Phase 6 (backend) + Phase 7 (frontend) — Module U (F5,
จัดการบัญชีผู้ใช้รายอื่น)** ตาม `design.md` §Modules → Module U ที่ `system-analyst` เพิ่งเปิด
scope กลับและเขียน contract `## Admin User Management Rules` (AU-1..AU-16) เสร็จวันเดียวกัน —
**ไม่มี schema/migration เลยแม้แต่ฟิลด์เดียว** (ทุกฟิลด์ที่ต้องใช้มีอยู่แล้วบน `AdminUser`)
แยกเป็นสอง phase ตาม role เหมือน Phase 1/2 เพื่อความละเอียดของ checkbox แต่ **ต่างจากทุกคู่
phase อื่นในแผนนี้ตรงที่ Phase 6/7 ต้อง deploy พร้อมกันเท่านั้น** เพราะ `Email` กลายเป็น
required field ใหม่ในคำขอแก้ผู้ใช้ ซึ่งเป็น breaking wire-contract change ต่อหน้า `/admin/users`
ที่ใช้งานจริงอยู่วันนี้ (R-19) — ทั้งสอง phase ติด `🔒 Security gate` ไม่มีข้อยกเว้นตามคำสั่งตรง
ของ `design.md` (โมดูลนี้ให้คนหนึ่งแตะ credential ของอีกคนเป็นครั้งแรกของระบบ)

## Phase 1: Company Provisioning — Backend 🔒 Security gate

**เหตุผลที่ติด gate** (คัดลอกจาก `design.md` §Modules → Module A ตรงตัว):
1. **สร้าง tenant ใหม่** — เป็น endpoint เดียวในระบบที่เพิ่มลูกค้ารายใหม่ได้
2. **สร้างบัญชีผู้ใช้ + รับรหัสผ่านจากฟอร์ม** — personal data (อีเมล) + credential ในคำขอเดียว
3. **แตะ `Company` และ `AdminUser` ซึ่งเป็นสองตารางเดียวในระบบที่ไม่มี query filter โดยเจตนา**
   (TD-014) — `IAuthorizationGuard` คือแนวป้องกันชั้นเดียว ไม่มีอะไรรองข้างหลัง
4. **`Role` ถูก hardcode เป็น `admin` (CP-8)** — ถ้าใครเผลอเปิดให้ request กำหนดได้ จะกลายเป็น
   ช่องสร้าง `owner` ที่มองไม่เห็นในหน้า `/admin/users`

- [x] [backend] เพิ่มเมธอด `void CreateDefaultChain(string companyId)` ลงใน `IKnowledgeCategoryService`
      (`SupportRoom.Application/Services/IKnowledgeCategoryService.cs`) แบบ **add-only** — ไม่แก้
      เมธอดเดิม ไม่แก้ signature เดิม ไม่แก้ entity ของ `knowledge-base` (CH-1) — สร้างสองแถว
      (parent Level 1 + leaf Level 2) ตรงค่าใน CH-2 เป๊ะ ใช้ `IdGenerator.GenerateId("kbcat")`
      ไม่ใช่รูปแบบ md5 ของ migration เดิม — เมธอดนี้ **stage อย่างเดียวผ่าน `_repository.Add(...)`
      ห้าม `UnitOfWork.Commit()` ข้างใน และห้าม query `KnowledgeCategory` เพื่อเช็คก่อนสร้าง**
      (CH-4, เหตุผลเรื่อง query filter อยู่ที่ CP-12)
- [x] [backend] ขยาย `CreateCompanyDto` เพิ่ม `AdminEmail`/`AdminDisplayName`/`AdminInitialPassword`
      ตาม CP-2 คำต่อคำ (annotation เดียวกับ `CreateAdminUserDto`) — ไม่มีฟิลด์ `Role`,
      ไม่มีฟิลด์ `CompanyId`, ฟิลด์เดิม 2 ตัว (`Id`/`Name`) ไม่เปลี่ยน
- [x] [backend] แก้ `ICompanyService.Create` ให้เรียง validate ตาม CP-3 เป๊ะ: `guard.EnsureOwner()`
      เป็นบรรทัดแรก (CP-1) → normalize slug → `CompanySlug.IsValid` → slug ซ้ำ (CP-4, สองข้อความ
      แยกกันตาม `IsActive`) → อีเมลซ้ำ (CP-5, ข้อความคงที่ ห้ามบอก company/role)
- [x] [backend] แก้ `ICompanyService.Create` ให้สร้าง `Company` ตามค่าใน CP-7 และ `AdminUser` ตามค่า
      ใน CP-8 (**`Role = AdminRole.Admin` ตายตัว ห้ามรับจาก request ไม่ว่าทางตรงหรือทางอ้อม**,
      `MustChangePassword = true` ตายตัว, `PasswordHash` ผ่าน `passwordHasher.HashPassword`)
      แล้วเรียก `CreateDefaultChain(companyId)` (CP-9) — **stage ทั้งสาม entity ก่อน แล้ว
      `UnitOfWork.Commit()` ครั้งเดียวที่ท้ายสุดเท่านั้น (CP-6) — ห้ามเรียก `IAdminUserService.Create`
      หรือ service ใดก็ตามที่ `Commit()` ในตัวเอง และห้ามใส่ try/catch ที่กลืน exception แล้วเดินต่อ**
- [x] [backend] ปรับ response ของ `Create` ให้เป็น `201 Created` + `{ company: CompanyViewModel }`
      คง shape เดิมทุกประการ (CP-10) — **ห้ามคืนรหัสผ่านกลับมาในรูปแบบใดๆ**
- [x] [backend] เพิ่ม log `Logger.LogInformation("Company created: {CompanyId} admin={AdminUserId}
      by={ActorId}", ...)` (CP-11) — **ห้าม log อีเมลและห้าม log รหัสผ่าน/hash ไม่ว่าระดับ log ใด**
- [x] [backend] เพิ่ม `IQueryable<Company> GetAllIncludingInactive()` ใน `ICompanyRepository`
      และ endpoint ใหม่ `GET /api/companies/all` (owner-only ผ่าน `guard.EnsureOwner()`) คืนทุกบริษัท
      รวมที่ `IsActive = false` เรียงตาม `Name` ใช้ `CompanyViewModel` เดิม ไม่เพิ่มฟิลด์ (CP-13)
      — **ห้ามแก้ `GET /api/companies` เดิม** เพราะ company switcher พึ่งพฤติกรรม "active เท่านั้น" อยู่
- [x] [backend] migration ใหม่ `BackfillMissingDefaultCategoryChain` — **data-only ห้ามมี DDL แม้แต่
      บรรทัดเดียว** (`Up()` มีแต่ `migrationBuilder.Sql(...)`) ครอบทุกบริษัทที่ยังไม่มี leaf
      `IsSystemDefault && Level = 2` ไม่ใช่แค่บริษัทที่มี `LessonConfig`/`DocumentResource` (CH-6):
      ใช้ `INSERT ... SELECT ... WHERE NOT EXISTS (...)` เช็คต่อบริษัท (**ห้ามใช้ `ON CONFLICT`
      แทนการเช็ค**), เติมเฉพาะ leaf ถ้ามี parent อยู่แล้ว (ผูก `ParentId` ไปหา parent เดิม), เติมเฉพาะ
      parent ถ้ามี leaf อยู่แล้ว (แล้ว `UPDATE` leaf ให้ `ParentId` ชี้ parent ใหม่), ถ้าบริษัทใดมี leaf
      มากกว่า 1 แถวอยู่แล้วให้ **ปล่อยผ่านไม่แก้ไขเอง** (data corruption ต้องให้คนดู ไม่ใช่ให้ migration
      เลือกเอง) — ค่าที่ insert ต้องตรง CH-2 ทุกช่อง (`CreateBy = null` ได้) — `Down()` เป็น no-op
      พร้อมคอมเมนต์อธิบายเหตุผล ถ้าแยกแถวที่ตัวเองสร้างจากของเดิมไม่ได้
- [x] [backend] apply migration `BackfillMissingDefaultCategoryChain` กับ local Postgres แล้วตรวจ
      invariant CH-3 ด้วยมือ (หนึ่งบริษัทมีแถว `IsSystemDefault && Level == 2` เพียงแถวเดียวเป๊ะทุกบริษัท
      หลัง migrate)
- [x] [backend] unit test — CP-6 single-commit: verify code path ของ `Create` เรียก `Commit()`
      เพียงครั้งเดียว และไม่เรียก `IAdminUserService.Create`
- [x] [backend] unit test — CH-3 invariant: `CreateDefaultChain` สร้างสองแถวที่เชื่อมกันถูกต้อง
      (parent/leaf, `ParentId` ชี้ parent, `IsSystemDefault` ทั้งคู่) และไม่ query ก่อนสร้าง (CH-4)
- [x] [backend] unit test — CP-4: สร้างบริษัทด้วย slug ที่มีแถว `IsActive = true` อยู่แล้ว ได้ข้อความ
      "รหัสบริษัทนี้ถูกใช้งานแล้ว" และ slug ที่มีแถว `IsActive = false` ได้ข้อความแนะนำให้เปิดกลับจาก
      หน้ารายการบริษัทแทนการสร้างใหม่ — ทั้งสองกรณีต้อง **ไม่เปิดบริษัทเดิมกลับอัตโนมัติ**
- [x] [backend] unit test — CP-5: อีเมลซ้ำได้ข้อความคงที่ที่ไม่บอก company/role ของอีเมลนั้น
- [x] [backend] unit test — CP-8: ส่ง `Role` ปนมาใน payload (ถ้า field มีทางเข้าโดยไม่ตั้งใจ) หรือ
      ทดสอบว่าไม่มี override ทางใดที่ทำให้ `AdminUser.Role` ออกมาเป็นอย่างอื่นนอกจาก `admin`
- [x] [backend] unit test — company context resolve ไปยังบริษัทใหม่ก่อน แล้วค่อย query
      `KnowledgeCategory` เพื่อพิสูจน์ว่า chain ถูกสร้างจริง (ตามที่ CP-12 กำหนดว่าต้องพิสูจน์ผ่าน
      unit test ที่ resolve context เอง ไม่ใช่โค้ด production ที่อ่านกลับหลัง commit)

## Phase 2: Company Provisioning — Frontend 🔒 Security gate

**เหตุผลที่ติด gate** (เหมือน Phase 1 ทุกข้อ — คัดลอกจาก `design.md` §Modules → Module A):
แม้ Phase นี้เป็นงาน UI ล้วน แต่ทุกหน้าจอต่อกับ endpoint ที่ (1) สร้าง tenant ใหม่ (2) สร้างบัญชี
ผู้ใช้ + รับรหัสผ่านจากฟอร์มในคำขอเดียว (3) แตะ `Company`/`AdminUser` สองตารางที่ไม่มี query filter
โดยเจตนา (TD-014) (4) พึ่ง `Role` hardcode ฝั่ง backend (CP-8) — UI ที่พลาดจุดใดจุดหนึ่งในสี่ข้อนี้
(เช่น ไม่ซ่อนปุ่มสำหรับ non-owner หรือแสดงรหัสผ่านที่ backend คืนมาผิดที่) ทำให้ความเสี่ยงสี่ข้อจริง

- [x] [frontend] สร้างหน้าฟอร์มสร้างบริษัทใหม่ (owner เท่านั้น) — ฟิลด์ตรง `CreateCompanyDto` ที่ขยาย
      แล้วใน Phase 1 (`Id`/`Name`/`AdminEmail`/`AdminDisplayName`/`AdminInitialPassword`) เรียก
      `createCompany()` ที่มีอยู่แล้วใน `api-client.ts:559` (ยังไม่เคยมีใครเรียกมาก่อน)
- [x] [frontend] ซ่อนเมนู/route เข้าหน้าสร้างบริษัทสำหรับ role ที่ไม่ใช่ `owner` — เป็น UX เท่านั้น
      ไม่ใช่การป้องกันจริง (CP-1 บังคับที่ server แล้วใน Phase 1) แต่ต้องมีเพื่อไม่ให้ admin/cs
      เห็นฟอร์มที่กดแล้วโดน 403 เสมอ
- [x] [frontend] แสดงข้อความ error ของ CP-4 (slug ซ้ำ กรณี active/inactive แยกข้อความ) และ CP-5
      (อีเมลซ้ำ) แบบ inline ที่ฟิลด์ที่เกี่ยวข้อง ไม่ใช่ error กลางหน้าจอทั่วไป
- [x] [frontend] หลังสร้างสำเร็จ ถ้าจะมีหน้าจอสรุป "ข้อมูลสำหรับแจ้งลูกค้า" ต้องใช้ค่าจาก form state
      ของตัวเอง (อีเมล/รหัสผ่านที่เพิ่งกรอก) — **ห้ามอ่านรหัสผ่านจาก response ของ `createCompany()`**
      เพราะ backend ไม่คืนมาให้ตาม CP-10 อยู่แล้ว
- [x] [frontend] สร้างหน้ารายการบริษัททั้งหมดสำหรับ owner — เรียก `GET /api/companies/all` ที่เพิ่ง
      เพิ่มใน Phase 1 (**ไม่ใช่** `GET /api/companies` เดิมที่ใช้กับ company switcher) แสดงชื่อ/สถานะ
      active ของทุกบริษัทรวมที่ปิดไปแล้ว
- [x] [frontend] เพิ่มปุ่มปิด/เปิดใช้งานบริษัทในหน้ารายการ เรียก `updateCompany()` ที่มีอยู่แล้วใน
      `api-client.ts:563` (`PUT /api/companies/{id}` เดิม ไม่มีการเปลี่ยน backend เพิ่มสำหรับปุ่มนี้)
- [x] [frontend] กล่องยืนยันก่อนกดปิดใช้งานต้องมีข้อความสองส่วนตาม CP-14 เป๊ะ: "พนักงานของบริษัทนี้จะ
      เข้าสู่ระบบไม่ได้ทันที แต่ลิงก์เรียนที่แจกออกไปแล้วยังใช้งานได้จนกว่าจะหมดอายุ" — **ห้ามเขียนข้อความ
      ที่ทำให้เข้าใจว่าการปิดตัดทุกอย่างทันที** (ความเสี่ยงที่ยอมรับแล้ว R-8/B2 — ไม่ใช่สิ่งที่มองข้าม)

## Phase 3: Company Switching — Owner UX 🔒 Security gate

Change request จาก developer หลังทดลองใช้งานจริงหลัง Phase 1/2 ส่งมอบแล้ว (`requirement.md` F4,
เคาะรอบที่ 2, ✅ 2026-08-21) — เป็นการปรับ UX ล้วนบนของที่มีอยู่แล้ว **ไม่แตะ schema ไม่แตะ
endpoint ใดๆ** ทั้งสามไฟล์ที่แก้เป็น frontend ทั้งหมด: `CompanySwitcher.tsx`,
`AdminSessionProvider.tsx`, `AdminGuard.tsx`

**เหตุผลที่ติด gate** (คัดลอกจาก `design.md` §Modules → Module A → "🔒 Security gate — คำสั่งถึง
`project-manager`" ตรงตัว): **"ทุก phase ที่ implement Module A ต้องมี `🔒 Security gate` ต่อท้าย
หัวข้อ phase ใน `plan.md` ... ไม่มีข้อยกเว้น ไม่มี phase ไหนของโมดูลนี้ที่ปลอดจาก gate เพราะแม้แต่
phase ที่ทำแค่ UI ก็ต่อกับ endpoint ที่สร้าง tenant/บัญชี"** — Phase นี้อยู่ใน Module A (F1/F4
เป็น feature เดียวกันที่ทำต่อ ไม่ใช่ module แยก) กฎนี้จึงใช้แบบไม่มีเงื่อนไข **แม้ F4 เองจะไม่แตะ
endpoint สร้าง tenant/บัญชีโดยตรง และไม่เข้าเกณฑ์ความเสี่ยง 4 ข้อของ Phase 1/2 เป็นรายข้อก็ตาม**
— ติด gate เพราะกฎของ `design.md` บังคับทั้ง Module ไม่มีข้อยกเว้น ไม่ใช่เพราะ F4 มีความเสี่ยงชนิด
เดียวกับ Phase 1/2 เอง

- [x] [frontend] `CompanySwitcher.tsx` — เปลี่ยนเงื่อนไขแสดงผลจาก `companies.length <= 1` (ปัจจุบัน
      บรรทัด 15-20) เป็นการ branch ตาม **role**: ถ้า `user?.role === "owner"` ให้แสดง `Select`
      dropdown เสมอไม่ว่าจะมีกี่บริษัท (F4.1) — ใช้ `Select`/`SelectTrigger`/`SelectContent`/
      `SelectItem` เดิมที่ import อยู่แล้ว **ห้ามเปลี่ยนเป็น Combobox แบบค้นหาได้** (developer เลื่อน
      ไว้จนกว่าจำนวนบริษัทจะโตจริง) ถ้า role เป็น `admin`/`cs` ให้คงข้อความเฉยๆ `บริษัท: {only.name}`
      แบบเดิมไว้ **ไม่เปลี่ยนพฤติกรรมของสอง role นี้เลย** (F4.2) — ห้ามซ่อนชื่อบริษัททิ้งสำหรับ
      admin/cs เด็ดขาด
- [x] [frontend] `CompanySwitcher.tsx` — ยืนยันว่า dropdown ที่แก้ใหม่ไม่มีลิงก์/ทางลัดไปหน้า
      `/admin/companies` อยู่ภายใน (F4.4) — dropdown ทำหน้าที่เดียวคือสลับบริษัทที่กำลังดู
      การจัดการบริษัท (สร้าง/ปิด/เปิด) อยู่ที่เมนู sidebar เดิมเท่านั้น
- [x] [frontend] `AdminSessionProvider.tsx` — ขยาย effect "exactly 1 company auto-select" (ปัจจุบัน
      บรรทัด ~113-121) ให้ auto-select บริษัทแรก (`companies[0].id`) ทุกครั้งที่ยังไม่มี `resolved`
      **ไม่ว่า `companies.length` จะเป็นเท่าไหร่** (ตัดเงื่อนไข `companies.length !== 1` ออก) เพื่อให้
      owner ลงหน้าแรกโดยอัตโนมัติเสมอ ไม่มีขั้นตอนเลือกบริษัทคั่นอีกต่อไป (F4.3)
- [x] [frontend] `AdminSessionProvider.tsx` — เพิ่ม logic ให้เมื่อ `activeCompanyId`/`resolved`
      ปัจจุบันไม่อยู่ในรายการ `companies` ที่ fetch ล่าสุดอีกแล้ว (กรณีบริษัทที่กำลังดูอยู่ถูกปิดใช้งาน
      กลางคันโดยคนอื่น) ให้ auto-switch ไปบริษัท active ตัวแรกในรายการทันที โดยอัปเดต `?company=`
      ใน URL แบบเดียวกับ effect อื่นๆ ในไฟล์นี้ **ห้ามมีจอคั่นบอกว่า "บริษัทนี้ถูกปิดแล้ว"** (F4.6)
      — กลไก detect (fetch ตอนไหน/ผูกกับ effect ไหน) ให้ตัดสินใจตามโค้ดจริงในไฟล์นี้ ขอแค่ผลลัพธ์
      ตรงตามกฎ F4.6
- [x] [frontend] `AdminGuard.tsx` — ลบบล็อกจอ **"เลือกบริษัทก่อนเริ่มทำงาน"** ทั้งหมด (ปัจจุบัน
      บรรทัด ~76-85) เพราะ effect ที่ขยายแล้วใน `AdminSessionProvider.tsx` (task ด้านบน) ทำให้ owner
      มี `activeCompanyId` เสมอ จอนี้จึงไม่มีทาง reachable อีกต่อไป — ลบทิ้งจริง ไม่ใช่ปล่อยเป็น
      dead code (F4.3)
- [x] [frontend] `AdminGuard.tsx` — ลบบล็อกจอ **"ยังไม่มีบริษัทในระบบ"** ทั้งหมด (ปัจจุบันบรรทัด
      ~60-74) ตามความเสี่ยงที่ยอมรับแล้วใน `requirement.md` §Constraints & Assumptions ("สถานะ
      0 บริษัทจะไม่มี UI รองรับเลย") — **ห้ามสร้างจอทดแทนใดๆ** owner ที่ปิดใช้งานทุกบริษัทจะเจอ
      request ที่ล้มเหลวแบบ "company unknown" ทั่วไปแทน ซึ่งเป็นความเสี่ยงที่ยอมรับแล้วโดยตั้งใจ
      ไม่ใช่สิ่งที่ตกหล่น — หลังลบสองจอนี้ (task นี้ + task ก่อนหน้า) ให้ลบเงื่อนไข
      `if (user.role === "owner" && !activeCompanyId ...)` ที่ครอบทั้งสองบล็อกออกไปด้วย เพราะจะไม่มี
      เนื้อหาเหลือให้ครอบ (F4.3, ความเสี่ยงที่ยอมรับใน Constraints & Assumptions)

## Phase 4: Lesson Pacing Defaults — Module P 🔒 Security gate

**🔄 อัปเดต 2026-08-22 (รอบที่ 7 · มติ N1/N2/N3) — contract กลับทิศทาง P1**: `introWaitMs`/
`breathPauseMs`/`finalQuestionWaitMs` **ไม่ใช่** "ค่าเริ่มต้นระดับบริษัท + บทเรียน override ได้"
อีกต่อไป — ตอนนี้เป็น **"ค่ากลางระดับบริษัทล้วน ไม่มี override ต่อบทเรียนเลย"** ตาม `design.md`
§Module P (เขียนใหม่) และ contract `## Lesson Pacing Resolution Rules` เวอร์ชันปัจจุบัน
(LP-1/LP-4/LP-5/LP-7/LP-12/LP-13/LP-14/LP-15 เขียนใหม่ · LP-3/LP-6/LP-11 ยกเลิก · LP-2/LP-8/LP-9/LP-10
ไม่เปลี่ยน) — schema change จริง 2 จุด: **DM-P1 ไม่เปลี่ยน** (`Company` มี 3 คอลัมน์ `int` NOT NULL
อยู่แล้ว) · **DM-P2 กลับทิศทางเป็นครั้งที่สอง** — จาก "ขยาย `LessonConfig` เป็น `int?`" (สิ่งที่
implement ไปแล้วในรอบก่อน) เป็น **"ลบ 3 คอลัมน์นี้ออกจาก `LessonConfig` ให้หมด"**

⛔ **task ทั้งหมดข้างล่างนี้เขียนใหม่ทั้งชุดให้ตรง contract ปัจจุบัน — เดิม (ก่อนวันนี้) เคย implement
ไปแล้วครบตาม contract เก่าที่ตอนนั้นถูกต้อง (Phase 4 เดิมยังไม่เคยผ่าน QA สักรอบ) แต่ contract
เก่านั้นถูกกลับคำตอบแล้ว งานที่ implement ไปแล้วบางส่วน "ยังถูกต้อง ไม่ต้องทำซ้ำ" (ทำเครื่องหมายไว้
ในแต่ละ task) ส่วนที่เหลือคือ "ต้องถอด/แก้ย้อนหลัง" — ดู `design.md` §Module P ตาราง 3 กลุ่ม
(2026-08-22 รอบที่ 7) ก่อนหยิบไปทำ**

**เหตุผลที่ติด gate** (คัดลอกจาก `design.md` §Modules → Module P → "🔒 Security gate — คำสั่งถึง
`project-manager`" ตรงตัว):
1. **เป็น endpoint แรกของระบบที่ให้ `admin` (ไม่ใช่แค่ owner) เขียนค่าลงแถว `Company`** — ตารางที่
   ไม่มี query filter รองหลัง (R-1) `guard.EnsureCanAccessCompany` เป็นด่านเดียว
2. **`companyId` มาจาก path parameter** ไม่ใช่จาก JWT — ถ้า guard หลุด/เรียกผิดลำดับ จะกลายเป็น
   การแก้ค่าข้ามลูกค้าได้ทันที
3. **ต้องยืนยันว่า `cs` ถูกปฏิเสธที่ `PUT` จริง** (LP-9) — `cs` อ่านได้แต่เขียนไม่ได้ · Phase 5 เพิ่ม
   หน้าจอ `/admin/settings` ที่ตรวจด้วยตาได้แล้ว แต่ **การกดจาก UI ไม่ใช่หลักฐานเพียงพอ** (SP-4
   ซ่อนปุ่มบันทึกจาก `cs` ตั้งแต่ที่จอ) — `security`/`qa-engineer` ต้องยิง `PUT` ตรงด้วย JWT ของ `cs`

**⚠️ regression surface ของ Phase 1 (verified ✅ ไปแล้ว แต่ยังไม่ deploy) — ไม่ใช่โค้ดใหม่ล้วน**:
สอง task ที่ทำเครื่องหมายไว้ด้านล่างแก้ `ICompanyService.Create`/`SeedFirstCompanyIfEmpty` ตรงๆ —
`qa-engineer` รอบหน้าต้อง re-verify สองจุดนี้ ไม่ใช่แค่ตรวจโค้ดใหม่ (R-12)

**⚠️ R-17 — ห้ามส่งมอบครึ่งทาง**: migration ที่ลบคอลัมน์ + entity + resolver/จุดอ่านค่า + DTO/ViewModel
+ frontend (types/ฟอร์ม) **ต้องทำในรอบเดียวกันและ deploy พร้อมกันเป๊ะ** — ถ้าลบคอลัมน์ไปแล้วแต่ฟอร์ม
บทเรียนยังมีช่อง หรือถอดช่องไปแล้วแต่ backend ยังคาดหวัง field เดิม คือ failure mode ที่แย่ที่สุด
ของรอบนี้ (ดู Sequencing Notes) · `qa-engineer` **ต้องถือรอบแรกของ Phase 4/5 เป็น FULL เสมอ ไม่มี
TARGETED** เพราะ contract เปลี่ยนทิศระหว่างทาง

- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] migration `AddCompanyLessonPacingDefaults` (มีอยู่แล้ว ·
      applied กับ local Postgres จริงแล้ว) — **ส่วน (2) `ALTER TABLE "Company"` เพิ่ม 3 คอลัมน์
      `int` NOT NULL พร้อม backfill literal `5000`/`500`/`5000` ยังถูกต้องทั้งหมด ใช้ต่อได้เลย**
      · **ห้ามแก้ใบนี้ย้อนหลังและห้าม `migrations remove`** (CLAUDE.md ข้อ 6) — ส่วน (1) ที่ทำ
      `LessonConfig` เป็น nullable ถูก **แทนที่** โดย task ถัดไปแล้ว ไม่ต้องแตะใบนี้เพื่อ "แก้กลับ"
- [ ] [backend] migration ใหม่ `RemoveLessonConfigPacingOverrides` (Migration Plan Module P รอบที่ 7)
      — `ALTER TABLE "LessonConfig" DROP COLUMN "IntroWaitMs", "BreathPauseMs", "FinalQuestionWaitMs"`
      (สามคำสั่ง `DropColumn`) · **แยกไฟล์ใหม่เสมอ ห้ามรวมหรือแก้ใบ `AddCompanyLessonPacingDefaults`
      เดิม** · **ห้ามมี `UPDATE` ใดๆ ที่พยายามรักษาค่าเดิมไว้ก่อนลบ** (N2 สั่งให้ทิ้งโดยเจตนา ไม่ใช่ลืม
      backfill) · **ต้องมีคอมเมนต์ในไฟล์ migration** ระบุ (ก) มติ N1/N2/N3 ของเจ้าของโปรเจกต์
      2026-08-22 ที่กลับคำตอบ P1 (ข) ค่าที่อยู่ในสามคอลัมน์นี้ถูกทิ้งโดยเจตนา ไม่ใช่ลืม backfill
      (ค) ชี้มาที่ `design.md` §DM-P2 · **down migration**: สร้างคอลัมน์คืนเป็น `int NULL` ได้ (สถานะ
      ก่อนหน้าคือ nullable) **แต่ห้ามเดาค่าใส่กลับ** (ห้าม default `0` ห้าม copy จาก `Company`) พร้อม
      คอมเมนต์ว่า rollback กู้ได้แค่รูปร่าง ไม่ใช่ข้อมูล
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] `Company` entity — มี `DefaultIntroWaitMs`/
      `DefaultBreathPauseMs`/`DefaultFinalQuestionWaitMs` เป็น `required int` ตาม DM-P1 อยู่แล้ว
      (ไม่เปลี่ยน) — **`Company` ยังคงไม่มี query filter** (ห้ามเพิ่ม แม้แต่บรรทัดเดียว, CP-15/LP-10)
- [ ] [backend] `LessonConfig` entity — **ลบ** `IntroWaitMs`/`BreathPauseMs`/`FinalQuestionWaitMs`
      ออกทั้งสามฟิลด์ตาม DM-P2 (เขียนใหม่รอบที่ 7) — ไม่ใช่เปลี่ยนเป็น `int?` อีกต่อไป, ไม่ใช่
      `[NotMapped]`, ไม่ใช่ property ที่เหลือไว้เฉยๆ · field อื่นทั้งหมดคงเดิม, query filter เดิมของ
      `LessonConfig` ต้องคงไว้ · อัปเดต `ApplicationDbContext` mapping และ migration snapshot
      (`ApplicationDbContextModelSnapshot.cs`) ให้ตรง — ถ้าเห็นสามชื่อนี้โผล่ที่ไหนใน `LessonConfig`
      อีก (ไม่ว่าชนิดใด) คือผิด contract
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] **regression — Phase 1** `ICompanyService.Create`
      (`:105`) — ตั้งค่า pacing สามตัวจาก `ServerDefaults.GetLessonTimingDefaults()` ลงแถว
      `Company` ใหม่ก่อน `UnitOfWork.Commit()` ครั้งเดียวเดิม (CP-16/LP-2) implement ไปแล้วและยัง
      ถูกต้องตาม contract ปัจจุบัน (LP-1/LP-2 ไม่เปลี่ยน) — `qa-engineer` ยัง re-verify เป็น
      regression surface ของ Phase 1 ตามเดิม (R-12)
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] **regression — Phase 1**
      `ICompanyService.SeedFirstCompanyIfEmpty` (`:172`) — เหมือน task ข้างบน implement ไปแล้ว
      และยังถูกต้อง (CP-16/LP-2 ไม่เปลี่ยน)
- [ ] [backend] `ILessonPacingResolver`/`LessonPacingResolver` (มีอยู่แล้วจากรอบก่อน ทำ merge สองชั้น)
      — **ลบ interface/implementation นี้ทิ้งทั้งคู่** พร้อม DI registration ใน
      `ServiceConfiguration.cs` (ตัดสินใจ: การ merge สองชั้นที่มันมีไว้รองรับไม่มีอยู่จริงอีกต่อไป
      หลัง N1 — คงไว้เป็น pass-through บางๆ จะเป็น indirection ที่ไม่มีเหตุผล) — **ห้ามให้โค้ดใดยัง
      อ้างอิง `ILessonPacingResolver` หลัง task นี้เสร็จ**
- [ ] [backend] `ILessonConfigService.GetTeachingContentByLinkAsync` (`:328-337`) — ตอนประกอบ
      `LearnerLessonConfigViewModel` **อ่าน `company.DefaultIntroWaitMs`/`DefaultBreathPauseMs`/
      `DefaultFinalQuestionWaitMs` ตรงๆ** แทนที่การเรียก resolver เดิม (LP-4 เขียนใหม่) — ไม่มี merge
      ไม่มี `??` ไม่มีเงื่อนไข ไม่มีค่า default ระหว่างทาง · แถว `Company` โหลดอยู่แล้วที่ call site นี้
      (`_companyRepository.Get(link.CompanyId)`) พฤติกรรมกรณีบริษัทหาย (404 "บริษัท") ไม่เปลี่ยน ·
      **จุดเดียวในระบบที่อ่านค่านี้** ห้ามมีจุดที่สอง ห้ามเรียก `ServerDefaults.GetLessonTimingDefaults()`
      ที่นี่หรือที่ไหนนอกจากสองจุด regression ข้างบน (LP-1)
- [ ] [backend] `LessonConfigDto`/`LessonConfigViewModel` — **ลบ** `IntroWaitMs`/`BreathPauseMs`/
      `FinalQuestionWaitMs` ออกทั้งสามฟิลด์ (LP-5 เขียนใหม่ — ไม่ใช่ `int?` อีกต่อไป) · client เก่าที่
      ยัง POST/PUT สามฟิลด์นี้มาต้องถูก ASP.NET Core เพิกเฉยโดยปริยาย **ห้ามเพิ่ม validation ปฏิเสธ
      unknown field** — **`LearnerLessonConfigViewModel` ไม่แก้** ยังเป็น `int` NOT NULL เหมือนเดิม
      ทุกประการ (LP-5)
- [ ] [backend] `ILessonConfigService.SaveAsync` (`:161-163` สร้าง, `:186-188` แก้) — **ลบบรรทัด
      assign ทั้งหกที่เขียนค่า pacing ทิ้งทั้งหมด** (LP-6 ยกเลิกทั้งข้อ) — `SaveAsync` ต้องไม่แตะค่า
      pacing เลยไม่ว่าตอนสร้างหรือแก้ · เพราะ entity ไม่มี property ให้ assign แล้ว โค้ดที่ยังมีบรรทัด
      เหล่านี้จะ compile ไม่ผ่าน (สัญญาณว่า entity task ก่อนหน้ายังไม่เสร็จ) · **ห้ามเรียก
      `ServerDefaults.GetLessonTimingDefaults()` ที่นี่**
- [ ] [backend] เพิ่ม `[Range(0, 60000)]` บน `introWaitMs`, `[Range(0, 10000)]` บน `breathPauseMs`,
      `[Range(0, 120000)]` บน `finalQuestionWaitMs` ตาม LP-8 (ตัวเลขไม่เปลี่ยนจากรอบก่อน) —
      **เหลือที่เดียวคือ DTO ใหม่ของ `PUT /api/companies/{companyId}/lesson-pacing`** (task ถัดไป)
      เพราะไม่มี DTO ฝั่งบทเรียนอีกแล้ว (`LessonConfigDto` ไม่มีฟิลด์นี้แล้วตาม task ก่อนหน้า) ·
      ข้อความ error เป็นภาษาไทยตาม convention เดิม
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] DTO สำหรับ `PUT /api/companies/{companyId}/lesson-pacing`
      — รับสามค่าเป็น `int` **NOT NULL ครบทั้งสามตัว ห้าม partial ห้าม `null`** (LP-9 ไม่เปลี่ยน)
      พร้อม `[Range(...)]` ตาม LP-8 — implement ไปแล้วและยังถูกต้อง
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] `GET /api/companies/{companyId}/lesson-pacing` — คืนสามค่า
      ปัจจุบันของบริษัทนั้น · สิทธิ์ `guard.EnsureCanAccessCompany(companyId)` (owner ทุกบริษัท,
      `admin`/`cs` เฉพาะของตัวเอง) **`cs` อ่านได้โดยตั้งใจ** (LP-9 ไม่เปลี่ยน — เหตุผลเปลี่ยนเป็น
      "หน้า `/admin/settings` ของ Phase 5" แต่สิทธิ์เดิมทุกตัวอักษร) · 404 ("บริษัท") ถ้าไม่มีบริษัทจริง
      · บริษัทที่ `IsActive = false` ยัง `GET` ได้ตามปกติ — implement + ทดสอบสดไปแล้ว ยังถูกต้อง
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] `PUT /api/companies/{companyId}/lesson-pacing` — สิทธิ์
      `guard.EnsureCanAccessCompany(companyId)` **บวกปฏิเสธ `cs` อย่างชัดแจ้ง** (owner + `admin`
      ของบริษัทนั้นเท่านั้น, `cs` ได้ 403) (LP-9 ไม่เปลี่ยน) · 404/400 ตามเดิม — implement + ทดสอบสด
      ไปแล้ว ยังถูกต้อง
- [ ] [backend] **ลบ** unit test ของ `ILessonPacingResolver` สองชั้น (resolver: lesson มีค่า →
      ได้ค่าบทเรียน · lesson `null` → ได้ค่าบริษัท · lesson `0` → ได้ `0`) — ทดสอบพฤติกรรมที่ contract
      เพิ่งยกเลิก การปล่อยไว้แย่กว่าการลบเพราะ test สีเขียวจะทำให้คนอ่านเชื่อว่ากฎเก่ายังมีผล
      **แทนด้วย** unit test ใหม่ (LP-14 ข้อ 1): `GetTeachingContentByLinkAsync` คืนค่า pacing
      **เท่ากับ `Company.Default*Ms` เป๊ะทั้งสามตัว** (รวมกรณีค่าเป็น `0`)
- [ ] [backend] **ลบ** unit test ของ `SaveAsync` ที่ยิง `null` ทับค่าเดิมแล้วคาดว่าแถวกลายเป็น `null`
      จริง (LP-6 ยกเลิก, ไม่มีฟิลด์ให้ทดสอบแล้ว) — ไม่ต้องเขียน test ทดแทนถ้าเขียนไม่ได้อย่างมี
      ความหมาย (compiler บังคับพฤติกรรมนี้อยู่แล้วเพราะ entity ไม่มี property, LP-14 ข้อ 5) —
      `qa-engineer` ยืนยันด้วยการอ่านโค้ด + build ผ่านแทน
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] unit test — regression (LP-14 ข้อ 2): `Create` และ
      `SeedFirstCompanyIfEmpty` ตั้งค่า pacing ครบสามตัวจาก `ServerDefaults` ให้แถว `Company` ใหม่
      ทุกครั้ง — implement ไปแล้ว ยังใช้ได้ทั้งชุด ไม่ต้องแก้
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [backend] unit test — `PUT` ของ LP-9 (LP-14 ข้อ 3): `cs` ถูกปฏิเสธ
      (403) · ค่านอกช่วง LP-8 ถูกปฏิเสธ (400) · payload ที่ขาดฟิลด์ใดฟิลด์หนึ่ง/เป็น `null` ถูกปฏิเสธ
      — implement ไปแล้ว ยังใช้ได้ทั้งชุด ไม่ต้องแก้
- [ ] [backend] apply migration `RemoveLessonConfigPacingOverrides` กับ local Postgres จริง (แยกจาก
      การ apply `AddCompanyLessonPacingDefaults` ที่ทำไปแล้วก่อนหน้านี้) แล้วตรวจด้วยมือว่า
      `\d "LessonConfig"` ไม่มีสามคอลัมน์นี้อีกต่อไป และ `dotnet ef migrations
      has-pending-model-changes` ไม่มี pending change เหลือ
- [ ] [frontend] `frontend/src/types/domain.ts:31-33` — **ลบ** สามฟิลด์ pacing ออกจาก `LessonConfig`
      type ทั้งหมด (LP-12 เขียนใหม่) — ของเดิม (`number | null` ที่ Phase 4 รอบก่อนเพิ่งแก้ไป)
      **ต้องถูกลบทิ้ง** ให้ตรง `LessonConfigViewModel` ที่ไม่มีฟิลด์แล้ว
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [frontend] `frontend/src/types/domain.ts:41-50` —
      `LearnerLessonConfig` ประกาศสามฟิลด์นี้เป็น `number` ตรงๆ ต่อไป (ห้ามกลับไปใช้
      `Pick<LessonConfig, ...>` — ยิ่งเป็นไปไม่ได้แล้วเพราะ `LessonConfig` ไม่มีฟิลด์ให้ `Pick`) —
      implement ไปแล้ว ยังถูกต้อง ไม่ต้องแก้ (LP-12) · **`tutor-reducer.ts:9` ไม่แก้**
- [ ] [frontend] **ลบ** การเรียก `getCompanyLessonPacing()` ออกจาก
      `frontend/src/app/admin/lessons/[slug]/page.tsx` — ไม่มีอะไรใช้ผลลัพธ์แล้วหลังลบ placeholder
      (task ถัดไป) — **ฟังก์ชัน `getCompanyLessonPacing()` ใน `api-client.ts` ยังต้องอยู่** เพราะ
      Phase 5 (`/admin/settings`) ใช้ — **ห้ามลบฟังก์ชันนี้**
- [ ] [frontend] `frontend/src/app/admin/lessons/[slug]/page.tsx` — **ลบช่องกรอก pacing ทั้งสามช่อง
      ออกทั้งหมด** (input, label, คำอธิบาย, placeholder `ใช้ค่าบริษัท (N ms)`, handler, state ของ
      ทั้งสามค่า) ตาม LP-11 เขียนใหม่ — **ห้ามเหลือไว้แบบ `disabled`/`hidden`/comment out** — ต้องไม่
      เกิดกรณี "ช่องยังอยู่แต่ส่งค่าไม่ถึง server" (failure mode ที่แย่ที่สุดของรอบนี้ ตาม R-17)
- [ ] [frontend] `frontend/src/app/admin/lessons/new/page.tsx:27-29` — **payload ตอนสร้างไม่มีสามคีย์
      pacing นี้เลย** (ไม่ใช่ `null` ไม่ใช่ `0` ไม่ใช่ `undefined` ที่ยังประกาศไว้ใน object) ตาม LP-11
      เขียนใหม่ — ค่าคงที่เดิม (ไม่ว่าจะเป็น `3000/800/5000` หรือค่าว่างที่รอบก่อนเพิ่งแก้ไป) ต้องหายไป
      พร้อมกัน
- [ ] **ยังถูกต้อง ไม่ต้องทำซ้ำ (implemented แล้ว รอ QA ติ๊ก)** [frontend] `frontend/src/hooks/use-tutor-session.ts:45` — fallback
      `5000/500/5000` ตรง `TutorConfig.Default*` แล้ว (LP-13 ไม่เปลี่ยน) — implement ไปแล้วในรอบก่อน
      และยังถูกต้อง ไม่ต้องแก้อีก (ค่านี้จะไม่ถูกใช้จริงหลัง LP-4 ทำงานที่ server แต่ต้องเป็นค่าที่ถูกต้อง
      ไว้เป็น safety net)
- [ ] [frontend] **ลบ** unit test ของฟอร์มบทเรียนที่เกี่ยวกับ placeholder/empty-vs-zero ของช่อง pacing
      (LP-11 เดิม, vitest) — ทดสอบพฤติกรรมของช่องกรอกที่ถูกลบไปแล้ว การปล่อยไว้แย่กว่าการลบด้วยเหตุผล
      เดียวกับฝั่ง backend

## Phase 5: Company Settings Page — Module P 🔒 Security gate

งานใหม่ตามมติ P6 (ยกเลิก LP-15 ข้อ "ห้ามสร้างหน้า UI ตั้งค่าบริษัท") และมติ A8 (สิทธิ์
visibility/edit แยกแกน) — ทั้งสองมติเคาะที่ `design.md` §Company Settings Page Rules
(**SP-1..SP-15**) เมื่อ 2026-08-22 ไม่มี open question ค้างแล้วสำหรับหน้านี้

**เป็นงาน `[frontend]` ล้วน 100% ไม่มีงาน `[backend]` แม้แต่บรรทัดเดียว** — endpoint
`GET`/`PUT /api/companies/{companyId}/lesson-pacing` (LP-9) implement + ทดสอบสดกับ JWT จริง
ไปแล้วใน Phase 4 (backend 243/243) งานที่เหลือทั้งหมดคือประกอบหน้าจอ + กลไกสิทธิ์ต่อ section
บน endpoint ที่มีอยู่แล้ว **เปิดเป็น phase ใหม่แยกจาก Phase 4** แทนที่จะต่อท้าย เพราะเป็นงานคนละ
ก้อนกับ backend/ฟอร์มบทเรียนของ Phase 4 ที่กำลังรอ QA FULL รอบแรกอยู่ — แยก phase ทำให้ QA
ตรวจ Phase 4 (พร้อม regression surface ของ Phase 1 ตาม R-12) เป็นก้อนเดียวจบ ไม่ปนกับหน้าจอใหม่
ที่ยังไม่เคย implement เลย

**เหตุผลที่ติด gate** (คัดลอกจาก `design.md` §Modules → Module P → "🔒 Security gate — คำสั่งถึง
`project-manager`" ข้อ 4 ที่เพิ่งเพิ่มพร้อมมติ P6):
1. หน้าที่แก้ค่าระดับบริษัทได้จาก client — แม้ endpoint จะ verified แล้ว แต่เป็นทางเข้าใหม่
   ที่ทำให้ `admin` เขียนค่าลง `Company` ได้จากหน้าจอจริงเป็นครั้งแรก (ก่อนหน้านี้ทดสอบผ่าน
   `curl`/JWT เท่านั้น)
2. **permission-gating logic ใหม่** (`section-access.ts`/SP-15) ต้อง enforce ถูกทั้งสองชั้น —
   UI (ซ่อน/disable ตาม role) และ server (`guard.EnsureCanAccessCompany` + ปฏิเสธ `cs` ที่ `PUT`
   ตาม LP-9) — SP-15 ข้อ 6 เตือนไว้ตรงๆ ว่า "ซ่อนที่ UI ไม่ใช่การกั้นสิทธิ์"
3. **หน้านี้เป็นจอแรกที่ทำให้ `cs` เห็นกลุ่มเมนู "ตั้งค่า"** ซึ่งวันนี้ปิดทั้งกลุ่มจาก `cs`
   (`AdminSidebar.tsx:170`, R-14) — ถ้าขยับ gate ผิดระดับจะพา `cs` ไปเห็นเมนู "ผู้ใช้งาน"
   (`/admin/users`) ติดไปด้วยโดยไม่มีใครตั้งใจ

**⚠️ ข้อห้ามที่ต้องรู้ก่อนเริ่ม (SP-13, เขียนไว้ตรงนี้ไม่ปล่อยให้เจอเอง)**:
- **ห้ามแตะ `admin/lessons/[slug]/page.tsx`/`admin/lessons/new/page.tsx` ซ้ำในรอบนี้** —
  LP-11/LP-12/LP-13 ที่ `frontend-engineer` เพิ่งแก้ใน Phase 4 ยังไม่ผ่าน QA ถ้าแก้ทับตอนนี้
  จะแยกไม่ออกว่าโค้ดไหนของ phase ไหน
- **ห้ามมีปุ่ม "บันทึกทั้งหมด"** (SP-4/SP-2) — แต่ละ section เซฟแยก endpoint ของตัวเอง เพราะ
  ค่าคนละกลุ่มอยู่คนละ endpoint คนละสิทธิ์ (B4 ห้ามยุบรวม)
- **ห้ามใส่ placeholder "เร็ว ๆ นี้" สำหรับ section ที่ยังไม่มี** (SP-1) — เผื่อที่ไว้ที่โครงโค้ด
  (registry ขยายได้) ไม่ใช่บนหน้าจอที่ลูกค้าเห็น
- **ห้ามเพิ่ม section อื่นของ F2** (ลิงก์หมดอายุ/TTS/แบรนด์) แม้จะ "เผื่อไว้ก่อน" — A2/A3/A4/B3b
  ยังไม่เคาะ (SP-13)
- **section ที่ซ่อนจาก role ต้องมี server-side gate คู่กันเสมอ ไม่ใช่แค่ UI** (SP-15 ข้อ 6, R-15)
  — รอบนี้ไม่มี section ที่ต้องซ่อนจริง (pacing เห็นได้ทุก role) แต่เป็นกฎที่ผูกมัดตั้งแต่รอบนี้
  เผื่อ section ถัดไป ไม่ใช่ทางเลือก

- [ ] [frontend] สร้าง `frontend/src/components/admin/settings/section-access.ts` — ประกาศ type
      `SettingsSectionAccess { visibleToRoles: readonly AdminRole[]; editableByRoles: readonly
      AdminRole[] }` ตาม SP-15 ข้อ 1 คำต่อคำ — ไฟล์นี้มีแค่ type ไม่มี logic อื่นปน
- [ ] [frontend] เพิ่มฟังก์ชัน pure `resolveSectionAccess(access: SettingsSectionAccess, role:
      AdminRole) → { visible: boolean; canEdit: boolean }` ในไฟล์เดียวกับ task ก่อนหน้าหรือไฟล์
      ข้างเคียงเล็กๆ — `visible = visibleToRoles.includes(role)`, `canEdit = visible &&
      editableByRoles.includes(role)` (role ที่มองไม่เห็น = แก้ไม่ได้เสมอ ตาม SP-15 invariant ข้อ 3)
- [ ] [frontend] สร้าง `frontend/src/components/admin/settings/LessonPacingSettingsSection.tsx` —
      หนึ่ง component ที่โหลด/เซฟ/validate/ตัดสินสิทธิ์ของตัวเองครบตาม SP-2: เรียก
      `getCompanyLessonPacing(companyId)` เอง (ไม่รับ props ค่าจาก page), ประกาศ
      `LESSON_PACING_SECTION_ACCESS: SettingsSectionAccess = { visibleToRoles: ["owner","admin","cs"],
      editableByRoles: ["owner","admin"] }` ตาม SP-15 ข้อ 2 คำต่อคำ (เท่ากับ LP-9/SP-4 เดิม
      ไม่ใช่กฎใหม่), อ่าน `editableByRoles` ของตัวเองเพื่อตัดสิน `disabled`/ซ่อนปุ่มบันทึก
      — **ห้าม**รับ `canEdit` เป็น prop จาก page (SP-15 ข้อ 4)
- [ ] [frontend] ในคอมโพเนนต์เดียวกัน — สามช่องกรอก `introWaitMs`/`breathPauseMs`/
      `finalQuestionWaitMs` เท่านั้น **ห้ามมี `videoDurationMs`** (SP-6) พร้อม validate ที่ client
      ตามช่วง LP-8 (`0–60000` / `0–10000` / `0–120000`) โดยตัวเลขช่วงอ้างจาก constant จุดเดียว
      ในไฟล์นี้ (SP-8) — **ช่องว่างต้องถูกปฏิเสธไม่ยิง request** (SP-7, ตรงข้าม LP-11 ของฟอร์ม
      บทเรียน) **ห้ามใช้แพตเทิร์น `Number(x) || 0`** (จะกลืนค่าว่างเป็น `0` โดยไม่ตั้งใจ)
- [ ] [frontend] ต่อปุ่มบันทึกของ section นี้เข้า `updateCompanyLessonPacing(companyId, payload)`
      (task ถัดไปใน `api-client.ts`) — **ส่งครบสามค่าเสมอ ห้าม partial** (SP-7/LP-9), ปุ่มเข้า
      สถานะ loading/disabled ระหว่างรอ response กันกดซ้ำ (SP-12), หลังสำเร็จใช้ค่าจาก response
      หรือ refetch **ห้าม optimistic update** (SP-9) แล้วแจ้งผลด้วย toast โดยอยู่หน้าเดิม
      ไม่ redirect · ถ้า server ตอบ 400/403 ให้แสดงข้อความจาก server ตรงๆ ห้ามเขียนทับด้วยข้อความ
      generic (SP-8/SP-12)
- [ ] [frontend] เขียนคำอธิบายบนจอของ section นี้ให้ตรง LP-7 ตาม SP-10 (ฉบับปัจจุบัน หลังรอบที่ 7
      กลับคำตอบ P1 — ไม่มี "ช่อง pacing ที่ปล่อยว่างได้" อีกต่อไป เพราะฟอร์มบทเรียนถอดสามช่องออกหมด):
      ค่ามีผลกับ**ทุกบทเรียนของบริษัท** ตั้งแต่การเข้าห้องเรียนครั้งถัดไปเท่านั้น (ห้องที่กำลังเรียนอยู่
      ไม่เปลี่ยนกลางคัน) **ห้ามเขียนทำนองว่า "มีผลทันที"** เพราะขัดกับกฎ "ไม่ย้อนหลังเข้าห้องที่กำลัง
      เรียนอยู่"
- [ ] [frontend] สร้าง `frontend/src/components/admin/settings/sections.ts` — registry กลาง
      รายการ `{ id, access, Component }` ตาม SP-15 ข้อ 4 เรียงตามลำดับที่จะแสดง รอบนี้มีแค่
      รายการเดียว (`pacing` → `LessonPacingSettingsSection`)
- [ ] [frontend] สร้าง `frontend/src/app/admin/settings/page.tsx` — route **`/admin/settings`**
      (ชื่อกลาง ห้ามผูกกับ pacing ตาม SP-1) ทำหน้าที่ประกอบอย่างเดียวตาม SP-2/SP-15 ข้อ 4:
      อ่าน `companyId`/`role` จาก `useAdminSession()` แบบเดียวกับ `admin/users/page.tsx:41`
      (`activeCompanyId ?? user?.companyId ?? null`, **ห้ามอ่าน `?company=` จาก URL เอง**, SP-3),
      กรอง `sections.ts` ด้วย `visibleToRoles` ของแต่ละ section แล้ว render เป็น `Card` ต่อหนึ่ง
      section เรียงลงมา (`components/ui/card.tsx`) — **ห้ามใช้ `Tabs`** แม้มี component อยู่แล้ว
      (SP-1) · หน้านี้ **ห้ามมี state ของฟิลด์ ห้ามรู้จัก endpoint ใดๆ ห้ามส่ง `canEdit` ลงไปให้
      section** (SP-2/SP-15 ข้อ 4)
- [ ] [frontend] ใน `page.tsx` เดียวกัน — ผูก re-render ของทุก section เข้ากับการเปลี่ยน
      `companyId` (`useEffect`/dependency array) เพื่อให้ owner สลับบริษัทกลางหน้าแล้ว refetch
      ใหม่ทุก section ไม่ค้างค่าบริษัทก่อนหน้า (SP-3)
- [ ] [frontend] ใน `page.tsx` เดียวกัน — เพิ่ม empty state กลางๆ สำหรับสองกรณี: `companyId` เป็น
      `null` (owner ที่ยังไม่มีบริษัทเลย, SP-3/SP-12) และ role ที่มองเห็น 0 section หลังกรอง
      registry (SP-12) — ข้อความกลางๆ เช่น "ยังไม่มีการตั้งค่าที่คุณเข้าถึงได้" **ห้ามเป็นจอ 403
      และห้ามไล่ออกไปหน้าอื่น** ไม่ยิง request ใดๆ ในทั้งสองกรณี
- [ ] [frontend] `frontend/src/lib/api-client.ts` — เพิ่ม `updateCompanyLessonPacing(companyId,
      payload)` เรียก `PUT /api/companies/{companyId}/lesson-pacing` (SP-11) — payload สามค่า
      `number` ทั้งหมด ต้องตรงกับ DTO จริงฝั่ง server (เปิดไฟล์ DTO อ่านชื่อฟิลด์จริง **ห้ามเดา**
      ตาม Architecture Rule 7) · `getCompanyLessonPacing()` และ type `CompanyLessonPacing` มีอยู่
      แล้วจาก Phase 4 **ห้ามประกาศซ้ำ**
- [ ] [frontend] `frontend/src/components/admin/AdminSidebar.tsx` — เพิ่มรายการ "ตั้งค่าบริษัท"
      → `/admin/settings` ในกลุ่ม "ตั้งค่า" ที่มีอยู่แล้ว (บรรทัด ~170-189) **ห้ามสร้างกลุ่มใหม่**
      (SP-5) — เงื่อนไขแสดงรายการนี้ต้อง **derive จาก `sections.ts` registry** ("role นี้เห็น
      อย่างน้อย 1 section") ตาม SP-15 ข้อ 7 **ห้าม hardcode รายชื่อ role ที่ตัวเมนู** (บั๊กที่เจอ
      ไว้แล้ว: ทั้งกลุ่มถูกกั้นด้วย `{user?.role !== "cs" && (...)}` ที่บรรทัด 170 ซึ่งจะซ่อนเมนูนี้
      จาก `cs` ทั้งที่ `cs` ควรเห็น pacing ได้ — ต้อง**ย้าย gate ลงระดับรายการ**: รายการ "ผู้ใช้งาน"
      คง `!== "cs"` ไว้เหมือนเดิมทุกประการ ไม่แตะ, รายการ "ตั้งค่าบริษัท" ใหม่แสดงตามผล derive
      (รอบนี้ผลลัพธ์ = แสดงทุก role)
- [ ] [frontend] test — pure function ของช่องกรอก pacing (SP-14): แยก parse+validate ออกเป็น
      pure function แล้วเขียน unit test (vitest) ครอบ 4 กรณีอย่างน้อย: `0` ผ่าน · ค่าสูงสุดของ
      แต่ละช่วง (`60000`/`10000`/`120000`) ผ่าน · สูงสุด+1 ถูกปฏิเสธ · **ช่องว่างถูกปฏิเสธ
      ไม่ถูกแปลงเป็น `0`**
- [ ] [frontend] test — `resolveSectionAccess` (SP-15 ข้อ 10): ครบสาม role ของ
      `LESSON_PACING_SECTION_ACCESS` (`owner` = เห็น+แก้ได้ · `admin` = เห็น+แก้ได้ · `cs` = เห็น+
      แก้ไม่ได้) บวกหนึ่งเคสสังเคราะห์ที่ `visibleToRoles` ไม่มี role นั้น → `visible = false`
      **และ `canEdit = false`** (กัน invariant ข้อ 3 ของ SP-15 หลุดเงียบๆ ในอนาคต)

## Phase 6: Admin User Management — Backend 🔒 Security gate

ตาม `design.md` §Modules → Module U (F5) และ contract `## Admin User Management Rules`
(AU-1..AU-16) ที่ `system-analyst` เพิ่งเปิด scope กลับและเขียน contract เสร็จ 2026-08-25
**ไม่มี schema/migration ใดๆ ในทั้ง phase นี้** — ทุกฟิลด์มีอยู่บน `AdminUser` แล้ว (F5.3,
`## Data Model` §Module U) — **ถ้าพบว่าต้องเพิ่มคอลัมน์/ตาราง = เข้าใจ contract ผิด ให้ตีกลับ
`system-analyst` ไม่ใช่สร้างเอง** (AU-16)

**⚠️ regression surface — ไม่ใช่โค้ดใหม่ล้วน**: `AdminUserService.Update`/`AdminUserDto`/
`/admin/users` เป็น baseline ที่ผ่าน QA ไปแล้วพร้อม Module A (Phase 1/2) — Phase นี้แทรกลำดับ
ตรวจใหม่เข้ากลาง `Update` ที่มีอยู่ `qa-engineer` ต้องถือเป็น regression surface (แนวเดียวกับ R-12
ของ Module P) ไม่ใช่โค้ดใหม่ล้วน · **ห้ามแตะเส้นทาง `Create` เลย** (AU-4/AU-16 — peer-lockout
ต้องไม่ถูกเรียกใน `Create`)

**เหตุผลที่ติด gate** (คัดลอกจาก `design.md` §Modules → Module U → "🔒 Security gate — คำสั่งถึง
`project-manager`" ตรงตัว — **อ่อนไหวกว่า Module A/P** เพราะเป็นครั้งแรกที่ระบบให้คนหนึ่งแตะ
credential ของอีกคน):
1. **auth + credential ของผู้ใช้รายอื่นโดยตรง** — ตั้ง `PasswordHash` ให้บัญชีที่ไม่ใช่ของตัวเอง
   เป็นความสามารถที่ระบบไม่เคยมีมาก่อน
2. **personal data** — แก้ `Email` ของคนอื่นได้อิสระ ไม่มีการยืนยัน (F5.2.3)
3. **เพิ่มเมธอดใน `IAuthorizationGuard`** ซึ่งเป็นแนวป้องกันชั้นเดียวของ `AdminUser` (R-1/TD-014)
4. **กฎใหม่สองข้อ (peer-lockout · ห้ามใช้กับตัวเอง) ถูกซ่อนที่ UI ด้วย** — การกดจาก UI ไม่ใช่
   หลักฐานว่า server ปฏิเสธจริง ต้องยิง endpoint ตรงด้วย JWT ของแต่ละ role
5. **ยังไม่มี rate limiting** (R-2) — endpoint นี้ตั้งรหัสผ่านได้แล้ว น่าสนใจกว่าเดิมสำหรับผู้โจมตี
   ที่ได้ token ของ admin มา

- [ ] [backend] ขยาย `UpdateAdminUserDto` เพิ่ม `Email` (`required string`, attribute ชุดเดียวกับ
      `CreateAdminUserDto.Email` เป๊ะ) และ `NewPassword` (`string?`) ตาม AU-2 คำต่อคำ —
      **ห้ามใส่ `[Required]`/`[MinLength]` บน `NewPassword`** (AU-7 เหตุผลเต็มอยู่ในหัวข้อ) ·
      `DisplayName`/`Role`/`IsActive` เดิมไม่เปลี่ยน
- [ ] [backend] เพิ่มเมธอดใหม่ **หนึ่งเมธอด** `void EnsureNotSameRankPeer(string targetRole)` ใน
      `IAuthorizationGuard` (AU-4) — **add-only ห้ามแก้เมธอดเดิมสักบรรทัด**: `EnsureAuthenticated()`
      → ถ้า `currentUser.Role == AdminRole.Owner` ผ่าน → ถ้า `currentUser.Role == targetRole` throw
      `GeneralException.Forbidden("ไม่สามารถจัดการบัญชีที่มีสิทธิ์ระดับเดียวกับคุณได้")` → นอกนั้นผ่าน
      — **เทียบด้วย string equality ตรงๆ ห้ามเปิด `AdminRole.RankOf` เป็น public** ·
      **`targetRole` ต้องเป็น role _ปัจจุบัน_ ของเป้าหมายที่อ่านจาก DB (`user.Role`) ไม่ใช่
      `input.Role`** (ผลที่ตามมาโดยตั้งใจตาม R-18/OQ-U2 — `admin` ยังเลื่อน `cs` ขึ้นเป็น `admin`
      ได้ ห้ามเพิ่มกฎกันการเลื่อนขึ้นเป็น peer เอง) · **ห้ามเรียกเมธอดนี้ใน `Create`**
- [ ] [backend] แก้ `AdminUserService.Update` แทรกลำดับใหม่ตาม AU-3 **ตรงตำแหน่งที่ระบุเป๊ะ ห้าม
      สลับ**: (1-3 เดิมไม่แก้: `Get` → guard ตาม role ปัจจุบันของเป้าหมาย → `EnsureCanAssignRole`)
      → (4 ใหม่) `EnsureNotSelf(user)` → (5 ใหม่) `guard.EnsureNotSameRankPeer(user.Role)` →
      (6 เดิมไม่แก้) `EnsureNotRemovingLastGuardian` → (7) กฎอีเมล AU-6 → (8) กฎรหัสผ่าน AU-7/AU-9
      → (9) เขียนค่า + `UpdateBy`/`UpdateDate` + `_users.Update(user)` → `Commit()` **ครั้งเดียว**
      — ข้อ 4 ต้องอยู่ก่อนข้อ 5 เสมอ (เหตุผล: peer-lockout จะให้ error message ผิดถ้ายิงก่อน
      self-check) ข้อ 7/8 ต้องอยู่หลังทุก guard เสมอ (ห้ามแตะ credential ก่อนรู้ว่ามีสิทธิ์)
- [ ] [backend] เพิ่ม private method `EnsureNotSelf(AdminUser user)` ใน `AdminUserService` ข้าง
      `EnsureNotRemovingLastGuardian` (AU-5) — ถ้า `currentUser.UserId == user.Id` throw
      `GeneralException.Forbidden("ไม่สามารถใช้หน้าจัดการผู้ใช้กับบัญชีของตัวเองได้ กรุณาเปลี่ยน
      รหัสผ่านที่หน้าเปลี่ยนรหัสผ่านของคุณเอง")` — **กฎแข็ง ไม่มีข้อยกเว้นแม้แต่ `owner`** ·
      **ห้ามยกไปไว้ใน `IAuthorizationGuard`** (เหตุผลอยู่ที่ AU-5: การทำกับตัวเองไม่ใช่เรื่องผิด
      โดยทั่วไป — F5.2.7 ยังอนุญาตที่ `POST /api/auth/change-password`)
- [ ] [backend] เขียนกฎอีเมล (AU-6) ใน `Update`: `email = input.Email.Trim()` → "เปลี่ยนจริง" =
      `!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)` (**เปลี่ยนแค่ตัวพิมพ์
      ห้ามติดธง**) → ถ้าเปลี่ยนจริง `_users.GetByEmail(email)` เจอแถวที่ `Id != user.Id` → throw
      `GeneralException.ValidationError("อีเมลนี้ถูกใช้งานแล้ว")` (**ข้อความเดียวกับ `Create` คำต่อคำ**
      ห้ามแต่งใหม่) → เขียน `user.Email = email` **เสมอ** (แม้เปลี่ยนแค่ตัวพิมพ์) →
      ติดธงตาม AU-8 **เฉพาะเมื่อเปลี่ยนจริง** — **ไม่ยืนยันอีเมลใหม่ ไม่ส่งลิงก์**
- [ ] [backend] เขียนกฎรหัสผ่าน (AU-7) ใน `Update`: `string.IsNullOrWhiteSpace(input.NewPassword)`
      → **ไม่รีเซ็ต** ข้ามทั้งบล็อกไม่ error → ไม่งั้น เช็ค `input.NewPassword.Length <
      PasswordRules.MinLength` → throw `GeneralException.ValidationError(PasswordRules.TooShortTh)`
      (ค่าคงที่เดิมใน `AuthDto.cs` ห้าม hardcode เลข/ข้อความใหม่) → `user.PasswordHash =
      passwordHasher.HashPassword(user, input.NewPassword)` **ใช้ค่าดิบ ห้าม `Trim()`** →
      ติดธงตาม AU-8
- [ ] [backend] เขียนกฎ AU-9 (เคสบัญชีถูกล็อกถาวร): ถ้า `user.PasswordHash == null` และคำขอนี้จะติด
      ธงตาม AU-8 (อีเมลเปลี่ยนจริงหรือรีเซ็ตรหัส) **โดยไม่ได้ตั้งรหัสผ่านมาในคำขอเดียวกัน** → throw
      `GeneralException.ValidationError("บัญชีนี้ยังไม่มีรหัสผ่านในระบบ กรุณาตั้งรหัสผ่านชั่วคราวมา
      พร้อมกับการแก้ไขครั้งนี้")` — ตรวจก่อนเขียนค่าใดๆ ลง entity
- [ ] [backend] บังคับ AU-8: `user.MustChangePassword = true` เมื่อ (อีเมลเปลี่ยนจริง) หรือ
      (รีเซ็ตรหัสสำเร็จ) เท่านั้น — **ถ้าไม่เกิดทั้งสองอย่าง ห้ามแตะฟิลด์นี้เลย ห้ามเซ็ต `false`**
      (บัญชีที่ค้างธงอยู่แล้วต้องไม่ถูกปลดธงเพราะมีคนมาแก้ role/สถานะให้)
- [ ] [backend] ขยาย log line เดิมที่ `IAdminUserService.cs:132-134` เพิ่มได้เฉพาะ **boolean**
      `passwordReset={bool}` `emailChanged={bool}` — **ห้าม log ค่าอีเมลใหม่และห้าม log รหัสผ่าน
      ทุกกรณี** (แนวเดียวกับ CP-10/CP-11)
- [ ] [backend] ยืนยันว่า `Commit()` ยังเป็นครั้งเดียวที่ท้ายสุดของ `Update` (AU-10) — อีเมลซ้ำต้อง
      ทำให้ **ไม่มีอะไรถูกบันทึกเลย รวมถึงรหัสผ่านใหม่** แนวเดียวกับ CP-6 · ยืนยันว่า
      `EnsureNotRemovingLastGuardian` และ branch "แถวเสีย `CompanyId` ว่าง" ยังทำงานเหมือนเดิม
      ทุกประการ ไม่ถูกแก้ (AU-11)
- [ ] [backend] `api-client.ts`/`types/domain.ts` — เตรียม type ฝั่ง TypeScript ให้ตรง DTO ใหม่
      (`email: string` required, `newPassword?: string`) ตาม `CLAUDE.md` §Architecture Rules ข้อ 7
      — **task นี้เป็นสะพานให้ Phase 7 ใช้ต่อ ไม่ใช่งาน UI** (ไฟล์ที่ frontend engineer แก้จริงคือ
      `page.tsx`/โมดัลใหม่ใน Phase 7)
- [ ] [backend] unit test — AU-15 ข้อ 1: `admin` → `admin` คนอื่นบริษัทเดียวกัน = **Forbidden**
- [ ] [backend] unit test — AU-15 ข้อ 2: `owner` → `owner` คนอื่น = **ผ่าน** (ข้อยกเว้น F5.2.1)
- [ ] [backend] unit test — AU-15 ข้อ 3: ผู้เรียก → บัญชีตัวเอง = **Forbidden** ทั้งกรณี `owner`
      และ `admin`
- [ ] [backend] unit test — AU-15 ข้อ 4: เปลี่ยนอีเมลอย่างเดียว → `MustChangePassword == true`
- [ ] [backend] unit test — AU-15 ข้อ 5: รีเซ็ตรหัสอย่างเดียว → `MustChangePassword == true` และ
      `PasswordHash` เปลี่ยนค่า
- [ ] [backend] unit test — AU-15 ข้อ 6: แก้แค่ `role`/`IsActive` → `MustChangePassword` ไม่ถูกแตะ
      (ทดสอบทั้งกรณีธงเดิม `false` และธงเดิม `true`)
- [ ] [backend] unit test — AU-15 ข้อ 7: อีเมลเปลี่ยนเฉพาะตัวพิมพ์ → **ไม่ติดธง**
- [ ] [backend] unit test — AU-15 ข้อ 8: อีเมลซ้ำกับผู้ใช้อื่น → `ValidationError` และ**ไม่มีฟิลด์ใด
      ถูกบันทึก รวมถึงรหัสผ่าน**
- [ ] [backend] unit test — AU-15 ข้อ 9: `NewPassword` เป็น `null` และเป็นช่องว่างล้วน → ไม่รีเซ็ต
      ไม่ error
- [ ] [backend] unit test — AU-15 ข้อ 10: `PasswordHash == null` + เปลี่ยนอีเมลโดยไม่ตั้งรหัส →
      `ValidationError` (AU-9)
- [ ] [backend] unit test — AU-15 ข้อ 11 (**regression**): `EnsureNotRemovingLastGuardian` ยัง
      ทำงานทั้งสองเคส (owner คนสุดท้าย / admin คนสุดท้ายของบริษัท) หลังแทรกลำดับใหม่เข้าไปข้างหน้า

## Phase 7: Admin User Management — Frontend 🔒 Security gate

ตาม `design.md` §Modules → Module U (F5), contract AU-13/AU-14 — **ต้อง deploy พร้อม Phase 6
เท่านั้น ไม่ใช่ phase ที่ส่งมอบแยกได้อิสระแบบปกติ** (ดู Sequencing Notes/R-19) เพราะ `Email` เป็น
`required` ใน DTO ใหม่ของ Phase 6 เป็น **breaking wire-contract change** ต่อหน้า `/admin/users`
ที่ใช้งานจริงอยู่วันนี้

**เหตุผลที่ติด gate** (เหมือน Phase 6 ทุกข้อ ตามกฎ Module U ที่ไม่มีข้อยกเว้นแม้แต่ phase ที่ทำ
แค่ UI): โมดัลนี้เป็นตัวตัดสินว่าใครเห็นปุ่มแก้ credential ของใคร และเป็นจุดที่ผู้ใช้จริงพิมพ์
รหัสผ่านของ**คนอื่น**ลงในฟอร์ม

**⚠️ นี่คืองาน _ลบ_ เป็นหลัก ไม่ใช่งานเพิ่ม** (F5.2.6/AU-13) — ต้องลบ control เดิมออกจริง
ห้ามซ่อนไว้เฉยๆ ห้ามปล่อยให้อยู่คู่กับโมดัลใหม่

- [ ] [frontend] `frontend/src/app/admin/users/page.tsx` — **ลบ** `<Select>` เปลี่ยน role ในแถว
      (`page.tsx:194-211`) ออกทั้งหมด (AU-13)
- [ ] [frontend] `frontend/src/app/admin/users/page.tsx` — **ลบ** ปุ่ม "ปิดบัญชี"/"เปิดบัญชี" ในแถว
      (`page.tsx:218-231`) ออกทั้งหมด (AU-13) — **ต้องยังมีทางปิด/เปิดบัญชีอยู่ที่อื่น**: ย้ายเข้า
      โมดัลใหม่ตามสองงานถัดไป ไม่ใช่ลบทิ้งเฉยๆ (AU-13 ข้อ "ช่องสถานะเปิด/ปิดบัญชีต้องมี แม้ Figma
      จะวาดแค่สามช่องแรก" — ระบบต้องไม่เหลือ 0 ทางปิดใช้งานผู้ใช้)
- [ ] [frontend] `frontend/src/app/admin/users/page.tsx` — **ลบ** ฟังก์ชัน `UserRow.apply()` ที่ยิง
      `updateAdminUser` แบบ partial (`page.tsx:167-184`) ออกทั้งหมด (AU-13)
- [ ] [frontend] เพิ่มปุ่มเดียวต่อแถว **"จัดการ"** เปิดโมดัล "จัดการผู้ใช้" — แสดงเมื่อทั้งสองข้อ
      เป็นจริงตาม AU-14: (ก) `row.id !== user.id` (AU-5) (ข) `user.role === "owner" ||
      row.role !== user.role` (AU-4) — **นี่คือ courtesy ไม่ใช่ด่าน** ด่านจริงอยู่ที่ server
      (Phase 6) ห้ามลดการตรวจฝั่ง server เพราะ UI ซ่อนให้แล้ว
- [ ] [frontend] สร้างโมดัล "จัดการผู้ใช้" — ใช้แบบแผนเดียวกับ `CreateUserDialog` ในไฟล์เดียวกัน
      (`Dialog`/`DialogContent`/`DialogHeader` + `form` + `Alert variant="destructive"` แสดง
      ข้อความจาก server ตรงๆ) **ห้ามคิด pattern ใหม่** — ฟิลด์: **อีเมล** (prefill ค่าเดิม) ·
      **รหัสผ่านใหม่** (ว่างไว้ = ไม่รีเซ็ต ต้องมีข้อความอธิบายใต้ช่องชัดเจน ไม่ปล่อยให้เดา) ·
      **สิทธิ์** (`Select` จำกัดด้วย `assignableRoles` เดิม) · **สถานะเปิด/ปิดบัญชี** — **ไม่มีช่อง
      "ชื่อที่แสดง"** ตามไฟล์ Figma (OQ-U3) → ส่ง `displayName` ค่าเดิมกลับไปไม่เปลี่ยน (ไม่ใช่
      regression — วันนี้ก็แก้ไม่ได้จากที่ไหนอยู่แล้ว)
- [ ] [frontend] ปุ่ม Cancel ของโมดัลนี้ — **ห้ามตั้งข้อความเอง** (OQ-15 ยังเปิดอยู่ตั้งใจ) ใช้
      label เดียวกับปุ่ม Cancel ที่มีอยู่แล้วใน `CreateUserDialog` ไฟล์เดียวกัน (ของเดิมที่มีอยู่
      แล้วในโค้ด ไม่ใช่ค่าที่ engineer เลือกใหม่) — ถ้า label นั้นต่างจากคำว่า "เรียนอีกครั้ง"
      ที่ Figma เขียนไว้ ความต่างนั้นเป็นเรื่องของ OQ-15 ที่เจ้าของโปรเจกต์ต้องเช็คกับดีไซเนอร์เอง
      ไม่ใช่สิ่งที่ต้อง "แก้ให้ตรง Figma" ในรอบนี้
- [ ] [frontend] `frontend/src/lib/api-client.ts` — แก้ `updateAdminUser()` ให้รับ `email`
      (บังคับ) และ `newPassword` (optional) ให้ตรง AU-2/DTO ใหม่ของ Phase 6 — เปิด DTO จริงอ่าน
      ชื่อฟิลด์ **ห้ามเดา** ตาม `CLAUDE.md` §Architecture Rules ข้อ 7
- [ ] [frontend] `frontend/src/types/domain.ts` — แก้ type ของ update payload ให้ตรง DTO ใหม่คู่กับ
      task ก่อนหน้า (`email: string` required, `newPassword?: string`)
- [ ] [frontend] ต่อปุ่มบันทึกของโมดัลเข้า `updateAdminUser()` ที่แก้แล้ว — หลังสำเร็จปิดโมดัล +
      refetch รายการผู้ใช้ (ไม่ optimistic update) แสดง error จาก server ตรงๆ ผ่าน
      `Alert variant="destructive"` เดิมของ `CreateUserDialog` (**ไม่เขียนข้อความ generic ทับ**)
- [ ] [frontend] ยืนยันว่าข้อความเดิม `"บัญชีของคุณไม่มีสิทธิ์จัดการผู้ใช้"` ที่ `page.tsx:73-79`
      สำหรับ `cs` ยังอยู่เหมือนเดิม ไม่ต้องแก้ (AU-14)

## Sequencing Notes

- **Phase 2 ขึ้นกับ Phase 1 ทั้งหมด** — ทุก task ของ Phase 2 เรียก endpoint ที่ Phase 1 สร้างหรือขยาย
  (`POST /api/companies` payload ใหม่, `GET /api/companies/all` ใหม่) ห้ามเริ่ม Phase 2 ก่อน Phase 1
  เสร็จ
- **Phase 1 บังคับให้เมธอด `CreateDefaultChain` (CH-1..CH-5) แตะโค้ดของโมดูล `knowledge-base` แบบ
  add-only** — เพิ่มเมธอดใหม่ใน `IKnowledgeCategoryService` เท่านั้น ห้ามแก้เมธอด/entity/query filter
  เดิมของโมดูลนั้น (CH-8) แบบแผนเดียวกับที่ `knowledge-base` เคยเพิ่ม index ให้ `SessionQuestion`
  ของ `learning-session` มาก่อน
- **Phase 1 มี hard invariant ที่ห้ามพัง (CH-3)**: หนึ่งบริษัทต้องมีแถว `IsSystemDefault && Level == 2`
  เพียงแถวเดียวเป๊ะ — ทั้ง `CreateDefaultChain` และ migration `BackfillMissingDefaultCategoryChain`
  ต้องรักษากฎนี้พร้อมกัน สองจุดที่เรียก `CreateDefaultChain`/สร้าง chain มีแค่สองจุดตาม CH-7
  (`ICompanyService.Create` และ migration นี้) **ห้ามเพิ่มจุดที่สาม** โดยเฉพาะห้ามเรียกแบบ lazy
- **Phase 1 ทั้ง phase ต้องเรียก `security` ก่อน deploy จริง** ผู้ใช้ต้องเรียกเองด้วยชื่อ
  (`security` ไม่ถูก auto-chain แม้ในโหมดอัตโนมัติ) — จุดที่ควรดูหนักที่สุดตามที่ `design.md` ระบุ:
  CP-1 (guard มาก่อนทุกอย่าง), CP-5 (ข้อความอีเมลซ้ำต้องไม่ enumerate), CP-8 (`Role` ตายตัว),
  CP-10/CP-11 (ห้ามคืน/ห้าม log รหัสผ่านและอีเมล), CP-12 (ห้าม `IgnoreQueryFilters()`),
  CP-13 (endpoint ใหม่ต้อง owner-only) — และ R-2 (ไม่มี rate limiting บน endpoint นี้เลย ยังไม่แก้)
- **Phase 2 ต้อง `security` ตรวจซ้ำก่อน deploy เช่นกัน** เพราะเป็นหน้าที่แสดง/ส่งต่อ credential
  ที่เพิ่งสร้าง — โดยเฉพาะ task ที่ห้ามอ่านรหัสผ่านจาก response
- **ห้ามแตะเส้นทางฝั่งผู้เรียนเลยในทั้งสอง phase** — B2 เคาะแล้วว่าปิดบริษัทไม่ตัดผู้เรียนทันที ลิงก์เดิม
  เรียนได้จนหมดอายุเอง ไม่มี task ให้แก้ `TrainingLinkController`/`TtsController`/
  `VoiceQuestionController`/join flow ในแผนนี้เลยโดยตั้งใจ
- **Phase 3 ขึ้นกับ Phase 1/2 ในเชิงข้อมูลเท่านั้น ไม่ใช่เชิง sequencing** — Phase 1/2 เสร็จและ
  verify แล้วทั้งคู่ ณ ตอนที่ Phase 3 ถูกเพิ่มเข้ามา (`[x]` ครบทุก task) จึงไม่มีอะไรบล็อก Phase 3
  ให้เริ่มได้ทันที Phase 3 **ไม่แตะ schema ไม่แตะ endpoint ใดๆ เลย** — งานทั้งหมดเป็น frontend-only
  บน `CompanySwitcher.tsx`/`AdminSessionProvider.tsx`/`AdminGuard.tsx` ที่ Phase 2 สร้าง/แก้ไว้แล้ว
  จึงไม่ต้องรอ Phase ใดใหม่ก่อน และไม่กระทบ backend endpoint ที่ Phase 1 ทำไว้เลยแม้แต่จุดเดียว
- **Phase 3 ติด `🔒 Security gate` เพราะกฎของ Module A ใน `design.md` ไม่มีข้อยกเว้นต่อ phase**
  ไม่ใช่เพราะ F4 เข้าเกณฑ์ความเสี่ยง 4 ข้อของ Phase 1/2 เอง (ดูเหตุผลเต็มที่หัวข้อ Phase 3) —
  ผู้ใช้ต้องเรียก `security` เองด้วยชื่อก่อน deploy Phase 3 เช่นเดียวกับ Phase 1/2 (`security` ไม่ถูก
  auto-chain แม้ในโหมดอัตโนมัติ) จุดที่ควรดูคือการลบเงื่อนไข guard ใน `AdminGuard.tsx` ต้องไม่เผลอ
  เปิดช่องให้ non-owner เห็น dropdown หรือให้ owner ที่ยังไม่มี `activeCompanyId` หลุดเข้าหน้าที่ยิง
  request ข้ามบริษัทได้โดยไม่ตั้งใจ
- **Phase 4 ไม่ขึ้นกับ Phase 1/2/3 ในเชิง sequencing** (Phase 1–3 verified ✅ ทั้งหมดก่อน Phase 4
  ถูกเพิ่มเข้ามา) แต่ **แก้โค้ดของ Phase 1 โดยตรงสองจุด** (`ICompanyService.Create`/
  `SeedFirstCompanyIfEmpty`) — `qa-engineer` ต้องถือ task ทั้งสองนี้เป็น **regression surface ของ
  Phase 1** ไม่ใช่โค้ดใหม่ล้วน (R-12) และต้อง re-verify ว่า path การสร้างบริษัทเดิมยังทำงานถูกต้อง
  หลังแก้ ไม่ใช่แค่ตรวจว่า pacing ถูกตั้งค่า
- **🔄 อัปเดต 2026-08-22 (รอบที่ 7) — Phase 4 มี migration สองใบเรียงกัน ไม่ใช่ใบเดียวอีกต่อไป**:
  `AddCompanyLessonPacingDefaults` (applied กับ local Postgres แล้ว, ส่วน `Company` ยังถูกต้อง)
  ตามด้วย `RemoveLessonConfigPacingOverrides` (ใบใหม่) — **ทั้งสองใบต้องรันตามลำดับนี้เท่านั้น
  ห้ามสลับและห้ามรวมเป็นใบเดียว** (การแก้ใบแรกที่ apply แล้วผิดกฎ CLAUDE.md ข้อ 6 ตรงๆ)
- **⚠️ hard invariant ใหม่ (R-17) — ห้ามส่งมอบ/deploy ครึ่งทาง**: migration
  `RemoveLessonConfigPacingOverrides` + entity `LessonConfig` (ลบ 3 property) + การลบ
  `ILessonPacingResolver`/จุดอ่านค่าใหม่ที่ `GetTeachingContentByLinkAsync` + DTO/ViewModel (ลบ
  3 ฟิลด์) + frontend (`domain.ts`/ฟอร์มบทเรียนสองไฟล์) **ต้อง deploy พร้อมกันในรอบเดียวเป๊ะ** —
  ถ้าคอลัมน์ถูกลบไปแล้วแต่โค้ดเก่ายัง `SELECT` คอลัมน์นั้นอยู่ **ทุก query ของ `LessonConfig` จะพัง
  ทั้งตาราง ไม่ใช่แค่ฟีเจอร์ pacing** (design.md §Modules → Module P อัปเดตรอบที่ 7) · ถ้าลบคอลัมน์
  ไปแล้วแต่ฟอร์มบทเรียนยังมีช่องกรอก หรือถอดช่องไปแล้วแต่ backend ยังคาดหวัง field เดิม คือ failure
  mode ที่แย่ที่สุดของรอบนี้เช่นกัน
- **Phase 4 ต้องเรียก `security` ก่อน deploy จริง** เช่นเดียวกับ Phase 1–3 — จุดที่ควรดูหนักที่สุด:
  `companyId` มาจาก path parameter ไม่ใช่ JWT (ถ้า guard หลุด = แก้ค่าข้ามลูกค้าได้ทันที), `cs` ต้อง
  ถูกปฏิเสธจริงที่ `PUT` (Phase 5 มีหน้าจอแล้วแต่การกดจาก UI ไม่ใช่หลักฐานเพียงพอ ต้องยิง `PUT` ตรง
  ด้วย JWT ของ `cs`), และ `Company` ยังไม่มี query filter รองหลัง (R-1)
- **`devops` ต้องเช็ค env ก่อน deploy migration `AddCompanyLessonPacingDefaults` ของ Phase 4** (R-11)
  — ถ้า environment ปลายทางตั้ง `DEFAULT_INTRO_WAIT_MS`/`DEFAULT_BREATH_PAUSE_MS`/
  `DEFAULT_FINAL_QUESTION_WAIT_MS` ไว้ต่างจาก literal `5000`/`500`/`5000` ที่ migration backfill ใช้
  ต้อง `UPDATE "Company" SET ...` ตามค่า env ของ environment นั้นทันทีหลัง migrate — บันทึกเป็นขั้นตอน
  ใน `deploy.md`, ไม่ใช่ความจำของคน
- **`devops` ต้อง backup ตาราง `LessonConfig` ก่อนรัน `RemoveLessonConfigPacingOverrides`** (R-16,
  เงื่อนไขที่มากับการยอมรับ data loss ของมติ N2) — **ไม่ใช่เพื่อกู้ค่ากลับเข้า schema** (N2 ตัดทิ้งแล้ว
  ถาวร) แต่เพื่อให้ยังตอบคำถามย้อนหลังได้ว่า "ค่าเดิมของบทเรียนนี้คืออะไร" — บันทึกเป็นขั้นตอนใน
  `deploy.md` เหมือนกัน
- **หนี้ข้ามโมดูลที่ยังไม่ปิด ไม่ใช่งานของ phase นี้ (D-3) — 🔄 คำตอบที่ถูกต้องเปลี่ยนแล้ว**:
  `knowledge-base/design.md` §DM-2 ยังประกาศ `LessonConfig` pacing เป็น `required int` — **คำตอบเดิม
  ("แก้เป็น nullable") ล้าสมัยแล้ว** เพราะ DM-P2 กลับทิศทางเป็นครั้งที่สอง คำตอบที่ถูกต้องตอนนี้คือ
  **"ลบสามฟิลด์นี้ออกจาก DM-2 ไปเลย"** และชี้มาที่ DM-P2 ว่าเจ้าของกฎคือโมดูลนี้ — ต้องมีรอบ
  `system-analyst` แยกไปแก้ที่โฟลเดอร์ `knowledge-base` ก่อนหรือพร้อมกับการ deploy Phase 4 ไม่งั้น QA
  รอบหน้าของ `knowledge-base` จะเห็น drift ที่ไม่จริง (เป็นช่องว่างของเอกสาร ไม่ใช่ของโค้ด) —
  **ไม่บล็อก Phase 4 ให้เริ่มหรือ implement ได้** เป็นแค่สิ่งที่ต้องปิดคู่กันไม่ให้ QA ของอีกโมดูลสับสน
- **`qa-engineer` ต้องถือรอบแรกของ Phase 4 (และ Phase 5) เป็น FULL เสมอ ไม่มี TARGETED** (R-17) —
  contract เปลี่ยนทิศทางระหว่างทางสองรอบแล้ว (P1→P4 ตอนแรก, กลับเป็น N1 ตอนนี้) โค้ดที่ implement
  ไปแล้วบางส่วนถูกต้องตาม contract ปัจจุบัน บางส่วนต้องถูกถอด — verify แบบ TARGETED จะไม่พอตรวจว่า
  ถอดครบทุกจุดหรือไม่
- **Phase 5 ขึ้นกับ Phase 4 ในเชิงข้อมูลเท่านั้น ไม่ใช่เชิง sequencing** — `GET`/`PUT
  /api/companies/{companyId}/lesson-pacing` (LP-9) implement + ทดสอบสดผ่าน `curl`/JWT จริงไปแล้ว
  ใน Phase 4 `[backend]` (243/243) ก่อน Phase 5 ถูกเพิ่มเข้ามา จึงไม่มีอะไรบล็อก Phase 5 ให้เริ่มได้
  ทันทีแม้ Phase 4 `[frontend]`/`[backend]` ทั้งก้อนจะยังไม่ผ่าน QA ก็ตาม (endpoint คือสัญญาที่
  ทดสอบแล้ว ไม่ใช่สัญญาที่ยังไม่ยืนยัน) — Phase 5 **ไม่แตะ schema ไม่มี migration ใหม่ ไม่มี
  `[backend]` แม้แต่ task เดียว**
- **Phase 5 ห้ามแตะไฟล์ของ Phase 4 `[frontend]` ที่กำลังรอ QA** — `admin/lessons/[slug]/page.tsx`
  และ `admin/lessons/new/page.tsx` (LP-11/LP-12/LP-13) เป็น regression surface ที่ต้อง verify
  แยกจาก Phase 5 เท่านั้น แก้ทับตอนนี้จะทำให้ QA แยกไม่ออกว่าโค้ดไหนของ phase ไหน
- **Phase 5 ติด `🔒 Security gate` ตามกฎของ Module P ข้อ 4 ที่เพิ่มพร้อมมติ P6** (ดูเหตุผลเต็มที่
  หัวข้อ Phase 5) — ผู้ใช้ต้องเรียก `security` เองด้วยชื่อก่อน deploy เช่นเดียวกับทุก phase อื่น
  ของ Module A/P (`security` ไม่ถูก auto-chain แม้ในโหมดอัตโนมัติ) จุดที่ควรดูหนักที่สุด: การกด
  จาก UI "กดปุ่มบันทึกไม่ได้" สำหรับ `cs` **ไม่ใช่หลักฐานว่า server ปฏิเสธจริง** — ต้องยิง `PUT`
  ตรงด้วย JWT ของ `cs` เหมือน Phase 4 เดิม และต้องตรวจว่าการขยับ gate ใน `AdminSidebar.tsx`
  ไม่ได้เปิดเมนู "ผู้ใช้งาน" ให้ `cs` เห็นติดไปด้วย (R-14)

- **Phase 6/7 (Module U) ไม่ขึ้นกับ Phase 3/4/5 เลย (D-5)** — ตรวจแล้วทีละข้อ: Module P
  แตะ `Company` ไม่ใช่ `AdminUser`, F2 ที่ยังพัก (A2/A3/A4/A6/B3b) เป็นเรื่องค่าระดับบริษัท
  ไม่มีข้อไหนแตะ `AdminUser`/`/admin/users` — Phase 6/7 **เริ่มได้ทันทีไม่ต้องรอ Phase 3/4/5
  ให้ผ่าน QA/deploy ก่อน**
- **⚠️ Phase 6 กับ Phase 7 (Module U) ต้อง deploy พร้อมกันเสมอ ห้ามปล่อยทีละ phase (R-19)** —
  ไม่เหมือน Phase คู่อื่นในแผนนี้ (เช่น Phase 1/2 ที่ Phase 2 พึ่ง Phase 1 แต่ deploy เหลื่อมกัน
  ได้ในทางเทคนิคถ้าจำเป็น) `Email` เป็น `required` ใน `UpdateAdminUserDto` ใหม่ของ Phase 6 คือ
  **breaking wire-contract change** ต่อหน้า `/admin/users` ที่ใช้งานจริงอยู่วันนี้ — ถ้า deploy
  backend (Phase 6) ก่อน frontend (Phase 7) ผู้เรียกเดิม (`UserRow.apply()`) จะยิงคำขอที่ไม่มี
  `email` แล้วได้ 400 **ทุกการกดเปลี่ยน role/เปิด-ปิดบัญชีที่ใช้งานอยู่จริงจะพังทันที** ไม่ใช่แค่
  ฟีเจอร์ใหม่ไม่ทำงาน · ถ้า deploy frontend ก่อน backend โมดัลใหม่จะส่งฟิลด์ที่ server ยังไม่รู้จัก
  — `devops` ต้องไม่ปล่อยครึ่งเดียวไม่ว่าทิศทางไหน (R-19 คัดลอกจาก `design.md` ตรงตัว)
- **เหตุผลที่ Phase 6/7 แยกเป็นสอง phase ตาม role แทนที่จะรวมเป็น phase เดียว**: `project-manager`
  เลือกคงรูปแบบเดิมของแผนนี้ (Phase 1/2 ก็แยกตาม role) เพื่อให้ checkbox ระดับ task ยังละเอียดพอ
  ให้ `qa-engineer` ติ๊กทีละงานและให้ engineer แต่ละฝั่งหยิบงานของตัวเองได้ตรง ๆ โดยไม่ต้องกรอง
  task ของอีกฝั่งออกเอง — **แต่ต่างจาก Phase 1/2 ตรงที่ Phase 6/7 ไม่ใช่คู่ที่ deploy เหลื่อมกันได้**
  บรรทัดข้างบน (R-19) จึงเขียนไว้ชัดว่าคู่นี้ deploy พร้อมกันเท่านั้น — `qa-engineer` ควรตรวจทั้งคู่
  พร้อมกันเป็นรอบเดียวด้วยเหตุผลเดียวกัน แม้จะติ๊ก checkbox แยก phase กันตามปกติ
- **Phase 6 แก้ `AdminUserService.Update`/`/admin/users` ซึ่งเป็น baseline ที่ผ่าน QA ไปแล้วพร้อม
  Module A (Phase 1/2)** — `qa-engineer` ต้องถือเป็น regression surface (แนวเดียวกับ R-12 ของ
  Module P) ไม่ใช่โค้ดใหม่ล้วน โดยเฉพาะ `EnsureNotRemovingLastGuardian` และ branch "แถวเสีย
  `CompanyId` ว่าง" ที่ AU-3 แทรกลำดับใหม่เข้าไปข้างหน้า
- **Phase 6/7 ต้องเรียก `security` ก่อน deploy จริง** เช่นเดียวกับทุก phase อื่นของโมดูลนี้ —
  จุดที่ควรดูหนักที่สุดตาม `design.md` §Modules → Module U: AU-3 (ลำดับการตรวจ), AU-5 (ไม่ยกเว้น
  `owner` จริงไหม), AU-7 (ไม่มีรหัสผ่านหลุดเข้า log/response), AU-8 (ไม่มีใครเผลอเซ็ตธงเป็น
  `false`), AU-9, AU-6 ข้อ 7 (ข้อความอีเมลซ้ำ enumerate ข้ามบริษัทได้ — พฤติกรรมเดิมของ `Create`
  ไม่ใช่ของใหม่ แต่ต้องอยู่ในรายงาน, R-20) — และ **`security` ควรรวมการ re-audit
  SECURITY-1 (`MustChangePassword` bypass) เข้ารอบเดียวกัน** เพราะอยู่ในเส้นทางเดียวกับ AU-8
  ตามที่ `design.md` ระบุไว้ตรง ๆ
- **OQ-U1/OQ-U2/OQ-U3 ไม่บล็อก Phase 6/7 ทั้งสาม** — contract AU-1..AU-16 ตอบครบทุกเคสที่ต้องใช้
  เขียน task ข้างบนแล้ว งานในแผนนี้เขียนตามทางเลือก "ทำน้อยที่สุด" ที่ `design.md` เคาะไว้ชั่วคราว
  (AU-4 เทียบ role ปัจจุบันเท่านั้น · ไม่แตะ `GetByCompanyId` ให้ `owner` โผล่ในตาราง · โมดัลไม่มี
  ช่องชื่อที่แสดง) — **ไม่มี task ใดในสอง phase นี้ปิดทางขยายในอนาคตถ้าคำตอบเปลี่ยน**: ถ้า OQ-U2
  ตอบว่าต้องกัน "เลื่อนขึ้นเป็น peer" ด้วย ก็เป็นการ amend `EnsureNotSameRankPeer`/`Create` เพิ่ม
  ไม่ใช่การเขียนใหม่ทั้งก้อน ถ้า OQ-U3 ตอบว่าต้องมีช่องชื่อ ก็เป็นการเพิ่ม `Input` หนึ่งช่องใน
  โมดัลที่มีอยู่แล้ว (DTO รับ `DisplayName` อยู่แล้ว ไม่ต้องแก้ backend) — ทั้งสามข้อจึงเป็นงาน
  ขยายในรอบหน้า ไม่ใช่งานรื้อ

## Unresolved Open Questions

ไม่มีข้อไหนบล็อก Phase 1/2 — A1/B1/B2/N1/N2 เคาะครบแล้วในเชิง contract (CP-*/CH-*) ที่ใช้เขียน task
ข้างบนได้ตรงๆ

รายการที่เหลือทั้งหมดเป็นของ **F2 (Module B/C)** ซึ่งยังพักไว้ ไม่ต้องตอบก่อนเริ่ม Phase 1/2 ของแผนนี้:
A2 (โลโก้: URL หรืออัปโหลด), A3 (ช่องเลือกเสียง TTS), A4 (รอ ElevenLabs หรือทำกับ Edge TTS),
A5 (ขอบเขตค่า session expiry/rate/สี/ชื่อแบรนด์), A6 (`cs` เห็นหน้าตั้งค่าไหม), B3 (ค่ากลางของแบรนด์
คืออะไร), B4 (schema ของค่าตั้งค่า — คอลัมน์บน `Company` vs ตารางใหม่) — ดูรายละเอียดที่
`design.md` §Unresolved Open Questions

**ไม่มีข้อไหนบล็อก Phase 6/7 (Module U) เช่นกัน** — `design.md` §🆕 กลุ่ม OQ-U มีสามข้อ (OQ-U1
บัญชี `owner` ไม่มีหน้าจอให้จัดการกันเอง, OQ-U2 peer-lockout ควรกันการเลื่อนขึ้นเป็น peer ด้วยไหม,
OQ-U3 โมดัลควรมีช่องแก้ชื่อที่แสดงไหม) ทั้งสามข้อถูก `system-analyst` ตัดสินไว้แล้วในทิศ
"ทำน้อยที่สุด" และเขียนลง contract (AU-4/AU-12/AU-13) แล้ว — เก็บไว้เป็น open question เพื่อ
รู้ว่ามีทางเลือกอื่นถ้าเจ้าของโปรเจกต์ตอบต่างในอนาคต ไม่ใช่เพราะบล็อกงานวันนี้ (ดู Sequencing
Notes ท้าย Phase 6/7 สำหรับผลกระทบถ้าคำตอบเปลี่ยน) · ส่วน OQ-15 (ข้อความปุ่ม Cancel) ก็ไม่บล็อก
เช่นกัน — เป็นแค่ label เดียวที่ frontend-engineer ห้ามตั้งเอง (ดู task ของ Phase 7)

## Change Log

- 2026-08-22 — แก้ task เดียวใน Phase 5 (`project-manager`, amend, ตาม QA ภายนอกที่ผู้ใช้แจ้งมา):
  task "เขียนคำอธิบายบนจอของ section นี้ให้ตรง LP-7 ตาม SP-10" ยังอ้างอิงโมเดลเก่า (per-lesson
  override — "ค่ามีผลเฉพาะบทเรียนที่ปล่อยช่อง pacing ว่างไว้") ทั้งที่ `design.md` LP-7/SP-10 ถูก
  `system-analyst` แก้เป็นความจริงใหม่ไปแล้วตั้งแต่รอบที่ 7 (กลับคำตอบ P1: ไม่มี override ระดับ
  บทเรียนอีกต่อไป ค่ามีผลกับทุกบทเรียนของบริษัท) — ตกหล่นตอน amend Phase 5 เข้าไปตอนนั้น เพราะตอนนั้น
  เพิ่งอ้างอิง LP-7 เดิมโดยไม่ได้ตามไปแก้ข้อความ task ที่ผูกกับมัน แก้เป็น "ค่ามีผลกับทุกบทเรียนของ
  บริษัท ตั้งแต่การเข้าห้องเรียนครั้งถัดไปเท่านั้น (ห้องที่กำลังเรียนอยู่ไม่เปลี่ยนกลางคัน)" และเปลี่ยน
  ข้อห้ามเป็น "ห้ามเขียนว่ามีผลทันที" แทน — ไม่แตะ checkbox ใดๆ ไม่แก้ `design.md`/`requirement.md`
- 2026-08-21 — สร้างแผนเริ่มต้น (`project-manager`) ครอบเฉพาะ Module A (F1) ตาม `design.md` ที่ล็อก
  เป็น contract แล้ว — 2 phase (backend → frontend) ทั้งคู่ติด `🔒 Security gate` ตามคำสั่งตรงจาก
  `system-analyst` ไม่มีข้อยกเว้น · ไม่มี task schema/DDL ใดๆ เพราะ Module A ไม่แก้โครงตาราง —
  มีแค่ migration data-only ใบเดียว (`BackfillMissingDefaultCategoryChain`) อยู่ใน Phase 1
- 2026-08-21 — เพิ่ม **Phase 3: Company Switching — Owner UX** (`project-manager`, amend) ตาม
  change request F4 ที่ `business-analyst` เพิ่งเคาะเสร็จใน `requirement.md` (F4.0–F4.6, developer
  ยืนยันครบทุกข้อ 9 คำถามข้ามสามรอบ ไม่มีอะไรค้าง) — 7 task ทั้งหมดเป็น `[frontend]` บน
  `CompanySwitcher.tsx`/`AdminSessionProvider.tsx`/`AdminGuard.tsx` ไม่แตะ schema/endpoint เลย
  ติด `🔒 Security gate` ตามกฎ Module A ที่ไม่มีข้อยกเว้นต่อ phase (ไม่ใช่เพราะ F4 เข้าเกณฑ์ความเสี่ยง
  4 ข้อของ Phase 1/2 เอง — ระบุเหตุผลนี้ไว้ตรงๆ ในหัวข้อ Phase 3 กับ Sequencing Notes) · ไม่บล็อก
  ด้วย Phase 1/2 เพราะทั้งคู่เสร็จ/verify แล้วก่อนหน้านี้
- 2026-08-22 — เพิ่ม **Phase 4: Lesson Pacing Defaults — Module P** (`project-manager`, amend) ตาม
  `design.md` §Module P ที่ `system-analyst` เพิ่งเคาะเสร็จวันเดียวกัน (contract
  `## Lesson Pacing Resolution Rules`, LP-1..LP-15) — phase เดียวรวม `[backend]`/`[frontend]`
  ไม่แยกตาม role เพราะ `design.md` ห้ามส่งมอบครึ่งเดียว · schema change จริง 2 จุด (DM-P1: `Company`
  เพิ่ม 3 คอลัมน์ `int` NOT NULL, DM-P2: `LessonConfig` 3 คอลัมน์เดิมเปลี่ยนเป็น `int?`) + migration
  เดียว `AddCompanyLessonPacingDefaults` · ติด `🔒 Security gate` (endpoint แรกที่ให้ `admin` เขียน
  ค่าลง `Company` โดย `companyId` มาจาก path parameter, ยังไม่เคยผ่าน `security`) · มี task ที่ทำ
  เครื่องหมายชัดว่าเป็น **regression surface ของ Phase 1** (`ICompanyService.Create`/
  `SeedFirstCompanyIfEmpty`) ไม่ใช่โค้ดใหม่ล้วน (R-12) · บันทึกหนี้ข้ามโมดูล D-3
  (`knowledge-base/design.md` §DM-2 ยังไม่ตรงกับ DM-P2) ไว้ใน Sequencing Notes เป็น dependency
  ที่ยังไม่ปิด ไม่ใช่ checkbox ของ phase นี้
- 2026-08-22 — เพิ่ม **Phase 5: Company Settings Page — Module P** (`project-manager`, amend)
  ตาม `design.md` §Company Settings Page Rules (SP-1..SP-15) ที่ `system-analyst` เพิ่งปิด open
  question ครบ (A8 · LP-8 · A5-แถว pacing ปิดหมดวันเดียวกัน — ไม่มีข้อค้างกับหน้านี้อีกแล้ว)
  — เปิดเป็น **phase ใหม่แยกจาก Phase 4** (ไม่ต่อท้าย) เพราะเป็นงานคนละก้อนกับ backend/ฟอร์ม
  บทเรียนของ Phase 4 ที่กำลังรอ QA FULL รอบแรก · 17 task ทั้งหมดเป็น `[frontend]` (ไม่มี
  `[backend]` เลย — endpoint LP-9 verified แล้วใน Phase 4): `section-access.ts` + `resolveSectionAccess`
  ตาม SP-15, `LessonPacingSettingsSection.tsx`, `sections.ts` registry, `app/admin/settings/page.tsx`,
  `updateCompanyLessonPacing()` ใหม่ใน `api-client.ts`, แก้ `AdminSidebar.tsx` ให้ derive เมนูจาก
  registry แทน hardcode role (แก้บั๊กที่เจอไว้แล้ว: กลุ่ม "ตั้งค่า" ปิดทั้งกลุ่มจาก `cs`), empty
  state กลางๆ ตาม SP-12, test ของ SP-14 (4 กรณี validate ช่วง) + SP-15 ข้อ 10
  (`resolveSectionAccess` 3 role + 1 เคสสังเคราะห์) · ติด `🔒 Security gate` ตามกฎ Module P ข้อ 4
  ที่เพิ่มพร้อมมติ P6 · เขียนข้อห้ามชัดในหัวข้อ phase (ห้ามแตะฟอร์มบทเรียนซ้ำ, ห้ามปุ่มบันทึกรวม,
  ห้าม placeholder section ที่ยังไม่มี, ห้ามเพิ่ม section อื่นของ F2, section ที่ซ่อนต้องมี
  server-side gate คู่กัน) กันไม่ให้ engineer ต้องเจอเอง
- 2026-08-22 — **แก้ Phase 4 ในที่เดิม (`project-manager`, amend รอบที่สาม)** ตามการกลับคำตอบ P1
  ของเจ้าของโปรเจกต์ (มติ N1/N2/N3) ที่ `system-analyst` amend `design.md` §Module P +
  `## Lesson Pacing Resolution Rules` เสร็จแล้ว — pacing กลับเป็น "ค่ากลางระดับบริษัทล้วน ไม่มี
  override ต่อบทเรียน" **เลือกแก้ Phase 4 เดิมแทนการเปิด Phase 6 ใหม่** เพราะ Phase 4 ยังไม่เคยผ่าน
  QA/deploy เลยสักครั้ง (ทุก task ยังเป็น `[ ]`) เปิด phase ใหม่ทับจะทำให้มีงานสองชุดขัดแย้งกันในไฟล์
  เดียว — เขียน task ทุกอันใหม่ให้ตรงตาราง 3 กลุ่มใน `design.md` §Module P: **"ยังถูกต้อง ไม่ต้องทำซ้ำ"**
  (`Company` entity/DM-P1, สอง regression point ของ Phase 1, endpoint `GET`/`PUT` ของ LP-9 ทั้งคู่,
  DTO ของ endpoint บริษัท, unit test LP-14 ข้อ 2/3, `domain.ts` ของ `LearnerLessonConfig`,
  `use-tutor-session.ts` fallback — ทำเครื่องหมายไว้ในแต่ละ task แต่ **ไม่ติ๊ก `[x]` เอง** เพราะ
  checkbox เป็นสิทธิ์ของ `qa-engineer` เท่านั้น) · **"ต้องถอด/แก้ย้อนหลัง"** (migration ใหม่
  `RemoveLessonConfigPacingOverrides` DropColumn 3 คอลัมน์พร้อมข้อบังคับ 3 ข้อ (คอมเมนต์เจตนา/
  ห้ามรวมกับใบเดิม/down กู้รูปร่างไม่กู้ข้อมูล), ลบ 3 property จาก `LessonConfig` entity + mapping +
  snapshot, ลบ `ILessonPacingResolver` ทิ้งทั้งคู่แล้วอ่าน `company.Default*Ms` ตรงจุดประกอบ
  `LearnerLessonConfigViewModel` (ตัดสินใจลบแทนคง pass-through), ลบ 3 ฟิลด์จาก `LessonConfigDto`/
  `LessonConfigViewModel`, ลบบรรทัด assign pacing ใน `SaveAsync` ทั้งหมด, ลบ `[Range]` ฝั่ง
  `LessonConfigDto` (ไม่มีฟิลด์ให้ตรวจแล้ว), ลบ unit test ของ resolver สองชั้น + test `SaveAsync`
  null แล้วแทนด้วย test ใหม่ตาม LP-14 ข้อ 1, ลบ `domain.ts` สามฟิลด์ pacing ออกจาก `LessonConfig`
  type, ลบการเรียก `getCompanyLessonPacing()` และช่องกรอกทั้งสามช่องออกจากฟอร์มบทเรียนสองหน้า
  (`[slug]`/`new`), ลบ unit test placeholder/empty-vs-zero ของฟอร์มบทเรียน) · เพิ่ม apply migration
  ใหม่กับ local Postgres เป็น task แยก · เพิ่มคำเตือน R-16/R-17 และ hard invariant ลำดับ migration
  สองใบเข้า Phase 4 heading และ Sequencing Notes (ห้ามส่งมอบครึ่งทาง, `qa-engineer` ต้องถือรอบแรก
  ของ Phase 4/5 เป็น FULL เสมอ, `devops` ต้อง backup `LessonConfig` ก่อนรัน migration ใหม่) ·
  **Phase 5 ไม่แก้เลยแม้แต่ task เดียว** (`design.md` ยืนยัน SP-1..SP-15 ทั้งชุดไม่กระทบ) · แก้ D-3
  ใน Sequencing Notes ให้ตรงคำตอบใหม่ ("ลบสามฟิลด์ออกจาก DM-2" แทน "แก้เป็น nullable") ·
  อัปเดต Plan Summary อธิบายเหตุผลที่เลือกแก้ในที่เดิม
- 2026-08-25 — เพิ่ม **Phase 6: Admin User Management — Backend** และ **Phase 7: Admin User
  Management — Frontend** (`project-manager`, amend) ตาม `design.md` §Modules → Module U (F5)
  ที่ `system-analyst` เพิ่งเปิด scope กลับและเขียน contract `## Admin User Management Rules`
  (AU-1..AU-16) เสร็จวันเดียวกัน — **ไม่มี schema/migration ใดๆ** (F5.3, `## Data Model` §Module U
  ยืนยันทุกฟิลด์มีอยู่แล้วบน `AdminUser`) · แยกเป็นสอง phase ตาม role เหมือน Phase 1/2 (checkbox
  ละเอียดพอให้ engineer แต่ละฝั่งหยิบงานตรงๆ) **แต่ต่างจาก Phase 1/2 ตรงที่ทั้งคู่ต้อง deploy
  พร้อมกันเท่านั้น ห้ามปล่อยทีละ phase** — เขียนไว้ชัดใน Sequencing Notes (R-19: `Email` เป็น
  `required` ใน DTO ใหม่ = breaking wire-contract change ต่อหน้า `/admin/users` ที่ใช้งานจริงอยู่
  วันนี้) · ทั้งสอง phase ติด `🔒 Security gate` ตามคำสั่งตรงจาก `design.md` §Modules → Module U
  ไม่มีข้อยกเว้นแม้ Phase 7 จะเป็นงาน UI ล้วน (โมดัลเก็บ credential ของคนอื่นโดยตรง) · Phase 6
  ทำเครื่องหมายไว้ชัดว่าแก้ `AdminUserService.Update`/`/admin/users` ซึ่งเป็น regression surface
  ของ Module A (Phase 1/2 ที่ verified ไปแล้ว) ไม่ใช่โค้ดใหม่ล้วน (แนวเดียวกับ R-12) · Phase 7
  ทำเครื่องหมายว่าเป็นงาน**ลบ**เป็นหลัก (ลบ `<Select>`/ปุ่มเปิด-ปิด/`UserRow.apply()` ในแถว)
  ไม่ใช่งานเพิ่ม พร้อมย้ำว่าต้องมีทางปิด/เปิดบัญชีเหลืออยู่ในโมดัลใหม่ (AU-13, กันไม่ให้ระบบเหลือ
  0 ทางปิดบัญชีผู้ใช้) และห้ามตั้งข้อความปุ่ม Cancel เอง (OQ-15 ยังเปิด) · unit test 11 ข้อตาม
  AU-15 เขียนเป็น task แยกทีละข้อ (ไม่ใช่ "เขียน test ให้ครบ" แบบเหมารวม) · Sequencing Notes บันทึก
  D-5 (ไม่ขึ้นกับ Phase 3/4/5) และย้ำว่า OQ-U1/OQ-U2/OQ-U3 ไม่บล็อกงานทั้งสอง phase — task ที่เขียน
  ไว้เลือกทางที่ขยายได้ในอนาคตถ้าคำตอบเปลี่ยน ไม่ปิดทางไว้
