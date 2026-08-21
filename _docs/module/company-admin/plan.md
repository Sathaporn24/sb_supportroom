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

## Unresolved Open Questions

ไม่มีข้อไหนบล็อก Phase 1/2 — A1/B1/B2/N1/N2 เคาะครบแล้วในเชิง contract (CP-*/CH-*) ที่ใช้เขียน task
ข้างบนได้ตรงๆ

รายการที่เหลือทั้งหมดเป็นของ **F2 (Module B/C)** ซึ่งยังพักไว้ ไม่ต้องตอบก่อนเริ่ม Phase 1/2 ของแผนนี้:
A2 (โลโก้: URL หรืออัปโหลด), A3 (ช่องเลือกเสียง TTS), A4 (รอ ElevenLabs หรือทำกับ Edge TTS),
A5 (ขอบเขตค่า session expiry/rate/สี/ชื่อแบรนด์), A6 (`cs` เห็นหน้าตั้งค่าไหม), B3 (ค่ากลางของแบรนด์
คืออะไร), B4 (schema ของค่าตั้งค่า — คอลัมน์บน `Company` vs ตารางใหม่) — ดูรายละเอียดที่
`design.md` §Unresolved Open Questions

## Change Log

- 2026-08-21 — สร้างแผนเริ่มต้น (`project-manager`) ครอบเฉพาะ Module A (F1) ตาม `design.md` ที่ล็อก
  เป็น contract แล้ว — 2 phase (backend → frontend) ทั้งคู่ติด `🔒 Security gate` ตามคำสั่งตรงจาก
  `system-analyst` ไม่มีข้อยกเว้น · ไม่มี task schema/DDL ใดๆ เพราะ Module A ไม่แก้โครงตาราง —
  มีแค่ migration data-only ใบเดียว (`BackfillMissingDefaultCategoryChain`) อยู่ใน Phase 1
