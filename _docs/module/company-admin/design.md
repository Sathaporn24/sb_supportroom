# การรับลูกค้าใหม่และตั้งค่าระดับบริษัท (company-admin) — Feasibility & Design

> # 🟢 สถานะ: ACTIVE — Module A (F1) + Module P (ค่า pacing ระดับบริษัท) · Module B/C (F2 ที่เหลือ) ยังพักไว้
>
> ## 🔄 อัปเดตล่าสุด 2026-08-22 (รอบที่ 7 · **กลับทิศทางของ Module P**) — อ่านก่อนทุกอย่าง
>
> เจ้าของโปรเจกต์ **กลับคำตอบ P1** หลังเห็นหน้าจอจริง (บันทึกโดย `business-analyst` ที่
> `_docs/module/learning-session/requirement.md` §"🔄 กลับคำตอบ P1 เมื่อ 2026-08-22 (รอบที่สอง)")
> · มติใหม่ **N1 · N2 · N3** ยืนยันแล้ว ไม่ใช่ข้อเสนอ:
>
> - **N1** — ตัดช่อง pacing ออกจากฟอร์มบทเรียน **ถาวร** · **ไม่มี override ต่อบทเรียนอีกต่อไป**
>   ทุกบทเรียนใช้ค่ากลางของบริษัทล้วน
> - **N2** — ค่า override เดิมของบทเรียนที่มีอยู่แล้ว (เช่น 3000/500/3000) **ทิ้งทั้งหมด**
>   ไม่ archive ไม่ย้ายไปไหน · **แทนที่ P4 เดิมทั้งข้อ**
> - **N3** — **ลบคอลัมน์ pacing ออกจาก `LessonConfig` จริงในฐานข้อมูล** ไม่ใช่ซ่อน UI
>   และไม่ใช่ปล่อยค้างเป็น nullable ที่ไม่มีใครใช้
>
> **โมเดลการสืบทอดเหลือชั้นเดียว**: `Company.Default*Ms` → ใช้ตรง ๆ ทุกบทเรียน
> · **DM-P1 ไม่เปลี่ยนแม้แต่ฟิลด์เดียว** (เป็นแหล่งความจริงเดียวที่เหลืออยู่)
> · **DM-P2 เปลี่ยนทิศทาง**: จาก "เพิ่ม nullable column ลง `LessonConfig`" เป็น
> **"ไม่มีคอลัมน์ pacing ใน `LessonConfig` เลย — ต้องลบออก"** พร้อม migration ใบใหม่
> `RemoveLessonConfigPacingOverrides` ที่เป็น **breaking + data loss ที่ตั้งใจ**
> · **LP-3 · LP-6 · LP-11 ถูกยกเลิกทั้งข้อ** และ **LP-1 · LP-4 · LP-5 · LP-7 · LP-12 · LP-13 ·
> LP-14 · LP-15 ถูกเขียนใหม่** — ข้อที่ยังใช้ได้ตามเดิมคือ **LP-2 · LP-8 · LP-9 · LP-10**
>
> ⚠️ **Phase 4 (backend) และ Phase 5 (frontend) ที่ implement ไปแล้วตามสัญญาเดิม
> กลายเป็นงานที่ต้องแก้ย้อนหลัง ไม่ใช่งานที่รอ QA เฉย ๆ** — ทั้งสอง phase **ยังไม่เคยผ่าน QA
> สักรอบ** ซึ่งเป็นจังหวะที่ถูกที่สุดที่จะกลับทิศ · **`project-manager` เป็นผู้จัดลำดับ**
> ว่าจะแก้ในเฟสเดิมหรือเปิดเฟสใหม่ทับ — `system-analyst` ไม่ตัดสินข้อนี้
>
> **อัปเดต 2026-08-22 (รอบที่ 4)**: **A5 (เฉพาะส่วน pacing) · B3a · B4 ปิดแล้ว** จากการสัมภาษณ์
> เจ้าของโปรเจกต์ P1–P5 ที่ `business-analyst` บันทึกไว้ใน
> `_docs/module/learning-session/requirement.md` §"🆕 บันทึกไว้เมื่อ 2026-08-21 … ✅ ตอบครบแล้ว 2026-08-22"
> → **B4 เคาะเป็นทางเลือก (ก) เพิ่มคอลัมน์ลง `Company` ตรงๆ** · เกิด **Module P · Lesson Pacing
> Defaults** ที่แยกออกมาจาก Module B และ **อยู่ในสโคปแล้ว** (P3: ไม่ต้องรอหน้า UI ตั้งค่าบริษัท) ·
> contract ชุดใหม่ `## Lesson Pacing Resolution Rules` (LP-1..LP-15) · **A2 · A3 · A4 · A6 · B3b
> (แบรนด์) ยังเปิดอยู่และยังบล็อก Module B ส่วนที่เหลือ ไม่ได้ถูกปิดไปด้วย**
>
> ⚠️ **Module P มี schema change จริง 2 จุด** (ต่างจาก Module A ที่ไม่มีเลย) และ **หนึ่งในนั้นแตะ
> `LessonConfig` ซึ่งเป็น entity ที่ `knowledge-base/design.md` DM-2 ประกาศไว้** — อ่าน
> `## Data Model` §Module P และ D-3 ก่อนลงมือ
>
> **สถานะเปลี่ยนเมื่อ 2026-08-21 (รอบที่ 2)**: มีลูกค้าใหม่จริงที่มีโอกาสเข้ามา (เรียกในบทสนทนาว่า
> **"scb"**) → **F1 (สร้างบริษัทใหม่) กลับมาเป็นงานจริงที่ต้องทำ** ส่วน **F2 (ตั้งค่าระดับบริษัท)
> ยังพักไว้ตามเดิม** เพราะทุกบริษัทใช้ค่ากลางได้อยู่แล้ว
>
> **ทำไม F1.3 (default category chain) กลายเป็น critical path**: scb คือบริษัทใหม่ที่
> **ไม่มีบทเรียนและไม่มีเอกสารเลยตอนเริ่มต้น** ซึ่งตรงกับเงื่อนไขของ F-3 เป๊ะ — ถ้า F1.3 ไม่ถูกทำ
> ให้ถูกต้อง scb จะเจอ error ทันทีที่พยายามสร้างบทเรียนแรก (`GetSystemDefault()` คืน null)
> นี่ไม่ใช่ความเสี่ยงทางทฤษฎีอีกต่อไป
>
> **✅ Module A ปิดงาน design แล้ว — เป็น contract ที่ implement ได้จริง (2026-08-21)**
>
> - **`## Data Model`**: F1 **ไม่ต้องแก้ schema เลยแม้แต่ฟิลด์เดียว** — ยืนยันแล้ว
> - **กฎธุรกิจครบทั้ง 5 ข้อ** (B1 · A1 · B2 · N1 · N2) เคาะกับเจ้าของโปรเจกต์แล้ว
>   บันทึกอยู่ในตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว"
> - **contract sections เขียนครบ 2 ชุด**: `## Company Provisioning Rules` (CP-1..CP-15) และ
>   `## Default Category Chain Rules` (CH-1..CH-8)
> - **พร้อมส่ง `project-manager` วางแผน Phase ได้แล้ว** — ดู `## Modules` สำหรับ 🔒 Security gate
>
> **F2/Module B/C ยังพักไว้**: `## Data Model` ของ F2 ยังว่าง · คำถาม A2–A6/B3/B4 ยังเปิดอยู่แต่
> **ไม่บล็อก Module A** · วันที่ปลุก F2 ต้อง re-run STATE: ANALYZE ของส่วนนั้นใหม่ตามกฎ deferred module
> — **ห้ามถือว่าส่วน F2 ในไฟล์นี้วิเคราะห์เสร็จแล้ว**

## Feasibility Summary

**ทุกฟีเจอร์ใน `requirement.md` (F1.1–F1.6, F2.1–F2.4) ทำได้ด้วย stack ปัจจุบันทั้งหมด
ไม่ต้องเพิ่ม dependency, provider หรือ service ภายนอกใหม่แม้แต่ตัวเดียว** — แม้แต่ทางเลือก
"อัปโหลดโลโก้" ก็ใช้ `IDocumentStorageProvider` (`local`/`huawei-obs`) ที่มีอยู่แล้ว และค่า
เสียง/ความเร็ว TTS ต่อบริษัทก็ไม่ต้องแก้ `ITtsProvider` เลยเพราะ `TtsInput.Voice`/`Rate`
เป็น optional override อยู่แล้ว

สิ่งที่บล็อกไม่ใช่ความเป็นไปได้ทางเทคนิค แต่เป็น **คำถามที่ยังไม่ถูกเคาะ 11 ข้อ** (7 ข้อจาก
`requirement.md` + 4 ข้อที่เจอเพิ่มระหว่างตรวจโค้ด) ซึ่งข้อที่หนักที่สุดคือ **B4 (รูปร่าง schema
ของค่าตั้งค่า)** — ตอบข้อเดียวก็ปลดล็อกโครง Data Model ได้ทั้งหมด

> **อัปเดต 2026-08-22**: **B4 เคาะแล้ว** (คอลัมน์บน `Company` ตรงๆ) พร้อม B3a และ A5 แถว pacing
> → เหลือคำถามเปิด **5 ข้อ: A2 · A3 · A4 · A6 · B3b** ซึ่งบล็อกเฉพาะ Module B ส่วนที่เหลือ
> (ลิงก์หมดอายุ/TTS/แบรนด์) กับ Module C · **Module A และ Module P เดินหน้าได้ทั้งคู่**
> · ประโยคข้างบนที่ว่า F1 ไม่มี schema change **ยังจริง** — schema change ที่เพิ่มเข้ามาเป็นของ
> **Module P** ซึ่งเป็นคนละก้อนกับ F1

งานจริงที่ต้องทำมีน้อยกว่าที่ `requirement.md` ประเมินไว้ในบางจุด (OQ-1 ตรงกับกลไกที่ระบบ
ทำอยู่แล้วเป๊ะ, OQ-6 ตรงกับพฤติกรรมโค้ดปัจจุบันอยู่แล้ว) แต่มากกว่าในบางจุด
(`GET /api/companies` ใช้ต่อไม่ได้จริง, การ backfill default chain ครอบคลุมกว้างกว่าที่คิด)
— ดู `## Findings from Feasibility Check`

**สโคปรอบนี้ (2026-08-21 รอบที่ 2)**: **Module A (F1) เท่านั้น** · F2/Module B/Module C ยังพักไว้
· ผลวิเคราะห์ที่สำคัญที่สุดของรอบนี้คือ **F1 ทั้งก้อนไม่ต้องแก้ schema เลยแม้แต่ฟิลด์เดียว** —
งานทั้งหมดอยู่ระดับ service/endpoint/UI ทำให้ความเสี่ยงของรอบนี้ต่ำกว่าที่ประเมินไว้ตอนแรกมาก
และไม่มี migration ใดที่จำเป็น **ยกเว้น** ใบ data-only ที่ขึ้นกับคำตอบของ B1

## Feature-by-Feature Feasibility

ตรวจกับ stack จริงตามที่ประกาศใน `.claude/agents/backend-engineer.md` และ
`.claude/agents/frontend-engineer.md` (ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR ·
Next.js 15 + React 19 + Tailwind v4 + shadcn/Base UI) เทียบกับโค้ดจริงในโปรเจกต์

| Feature | คำตัดสิน | หมายเหตุ |
|---|---|---|
| F1.1 owner เท่านั้นสร้างบริษัทได้ | **straightforward** | `guard.EnsureOwner()` มีอยู่แล้วและ `CompanyService.Create` เรียกอยู่แล้ว (`ICompanyService.cs:59`) |
| F1.2 ฟอร์มเดียวจบ (3 อย่างพร้อมกัน) | **straightforward แต่มีกับดัก** | ห้ามเรียก `AdminUserService.Create` ซ้ำจาก service ใหม่ เพราะมัน `UnitOfWork.Commit()` ในตัวเอง (`IAdminUserService.cs:87`) จะกลายเป็น 2 transaction = เกิด "บริษัทครึ่งๆ" ได้จริง ซึ่งเป็นสิ่งที่ F1.4 ห้าม |
| F1.3 default category chain อัตโนมัติ | **straightforward** | port ตรรกะจาก `AddKnowledgeTaxonomyAndScope.cs:46-49` มาเป็น service · แต่ id ต้องเลิกใช้ `'kbcat-backfill-parent-' \|\| md5("CompanyId")` เปลี่ยนไปใช้ `IdGenerator.GenerateId("kbcat")` ตาม convention ของ runtime code |
| F1.4 all-or-nothing | **straightforward** | `UnitOfWork.Commit()` = `SaveChanges()` ครั้งเดียว = 1 transaction (`UnitOfWork.cs:42`) — stage ทุก entity แล้ว commit ครั้งเดียวจบ ไม่ต้องใช้ explicit transaction API |
| F1.5 กันอีเมลซ้ำข้ามทั้งระบบ | **straightforward** | `_users.GetByEmail()` + unique index บน `AdminUser.Email` (`ApplicationDbContext.cs:66`) มีครบแล้ว · แบบแผนข้อความ error ก็มีแล้วที่ `IAdminUserService.cs:68` |
| F1.6 หน้ารายการบริษัท + ปิดใช้งาน | **ต้องเพิ่มของใหม่ มากกว่าที่ requirement ประเมิน** | `GET /api/companies` ใช้ต่อไม่ได้ (F-1) และความหมายของ "ปิดใช้งาน" ยังไม่ครบ (F-2) — ดู Findings |
| F2.1 ลิงก์หมดอายุต่อบริษัท | **straightforward** | จุดอ่านมีจุดเดียวคือ `ITrainingLinkService.cs:82` |
| F2.1 เสียง + ความเร็ว TTS ต่อบริษัท | **ทำได้ ไม่ต้องเพิ่ม dependency** | `TtsInput.Voice`/`Rate` เป็น optional override อยู่แล้ว (`ITtsProvider.cs:6,12`) — แต่ต้องย้ายจุดตัดสินใจจาก client มาที่ server ก่อน (F-4) |
| F2.1 แบรนด์ที่ผู้เรียนเห็น | **ทำได้ · ขนาดงานขึ้นกับ A2** | ทาง URL = ไม่ต้องเพิ่ม endpoint เลย เสิร์ฟผ่าน `GET /api/training-links/{token}` `[AllowAnonymous]` ที่มีอยู่แล้ว (`TrainingLinkController.cs:56-63`) · ทางอัปโหลด = ต้องเพิ่ม endpoint อัปโหลด + endpoint สาธารณะ + validation ชนิด/ขนาดไฟล์ |
| F2.2 null = inherit (ไม่ copy ค่ากลาง) | **straightforward** | เป็นแค่ nullable column + ตัว resolve — แต่ "ค่ากลาง" ของแบรนด์ยังไม่มีอยู่จริงในระบบ (B3) |
| F2.3 สิทธิ์ owner + admin ของบริษัทนั้น | **straightforward** | `guard.EnsureCanAccessCompany(companyId)` (`IAuthorizationGuard.cs:55-71`) ครอบกฎนี้พอดีเป๊ะ — owner ผ่านทุกบริษัท, admin/cs ผ่านเฉพาะของตัวเอง, `CompanyId` ว่าง = fail closed |
| F2.4 ขอบเขตค่าที่รับได้ | **ทำได้ แต่ต้องได้ตัวเลขจากผู้ใช้ก่อน** | validation ใช้ data annotation บน DTO ตามแบบแผนเดิมได้เลย · ตัวเลขจริงเป็น A5 |
| F3 แจ้งเตือน CS | **n/a — ไม่มีงาน** | ปิดด้วยของที่มีอยู่แล้ว (`AdminSidebar.tsx:130,142`) ตาม DC-1 |
| **F2 · หน้าจอตั้งค่าระดับบริษัท (UI surface ของ F2 — รอบนี้มีเฉพาะ section pacing)** (ใหม่ 2026-08-22 · มติ P6) | **straightforward · frontend ล้วน ไม่ต้องเพิ่ม dependency** | ตรวจของจริงแล้ว: shadcn primitive ที่ต้องใช้มีครบใน `frontend/src/components/ui/` (`card`/`input`/`label`/`button`/`alert`/`separator`/`toast`/`spinner`) · แบบแผนการดึง `companyId` จาก session มีอยู่แล้ว (`admin/users/page.tsx:41`) · `getCompanyLessonPacing()` มีแล้วใน `api-client.ts` เหลือเพิ่มแค่ตัว `PUT` · **ไม่มี endpoint ใหม่ ไม่มี DTO ใหม่ ไม่มี migration** · กับดักเดียวที่มีจริงคือ gate ของกลุ่มเมนู "ตั้งค่า" ที่ `AdminSidebar.tsx:170` ซึ่งซ่อนทั้งกลุ่มจาก `cs` อยู่ (SP-5) · contract = `## Company Settings Page Rules` |
| **F2.5 (ใหม่ 2026-08-22 · แก้ 2026-08-22 รอบที่ 7) ค่า pacing เป็นค่ากลางระดับบริษัทล้วน — ~~+ บทเรียน override ได้~~ ไม่มี override แล้ว (N1)** | **ทำได้ · เป็นฟีเจอร์แรกของโมดูลนี้ที่มี schema change จริง — และตอนนี้เป็นฟีเจอร์แรกที่มี `DROP COLUMN` ด้วย (N3)** | ตรวจโค้ดจริงยืนยันแล้ว: `LessonConfig.IntroWaitMs/BreathPauseMs/FinalQuestionWaitMs` เป็น `required int` (`LessonConfig.cs:53-55`) · `ServerDefaults.GetLessonTimingDefaults()` (`ServerDefaults.cs:38-43`) **ไม่มี call site ในโค้ด production เลยแม้แต่จุดเดียววันนี้** — ค่าที่บทเรียนได้มาจากฟอร์มฝั่ง frontend ล้วน ซึ่งอธิบายว่าทำไม default สามชุดถึงเพี้ยนออกจากกันได้ (P2) · contract = `## Lesson Pacing Resolution Rules` |

**F5 · จัดการบัญชีผู้ใช้รายอื่น (เพิ่ม 2026-08-25 · Module U)** — ตรวจกับโค้ดจริงวันเดียวกัน

| Feature | คำตัดสิน | หมายเหตุ |
|---|---|---|
| F5.2.1 peer-lockout ที่ server | **straightforward · เป็นกฎใหม่จริง ไม่ใช่ของที่มีอยู่แล้ว** | `AdminRole.CanAssign` = `RankOf(actor) >= RankOf(target)` (`AdminRole.cs:44-45`) **อนุญาต role เท่ากัน** → ของเดิมไม่ครอบ peer-lockout เลย · แก้ด้วยเมธอดใหม่ **หนึ่งเมธอด** ใน `IAuthorizationGuard` ไม่แตะของเดิม (AU-4) |
| F5.2.2 รีเซ็ตรหัสผ่านให้คนอื่น | **straightforward · ไม่ต้องเพิ่ม dependency และไม่มีกลไก auth ใหม่** | `IPasswordHasher<AdminUser>` ถูก inject เข้า `AdminUserService` อยู่แล้ว (`IAdminUserService.cs:35`) และใช้ตอน `Create` อยู่แล้ว · การบังคับเปลี่ยนรหัสเป็นของ middleware เดิมที่รันทุก request (AU-8) |
| F5.2.3 แก้อีเมลคนอื่น + ติดธงบังคับ | **straightforward** | `GetByEmail` + unique index มีครบ (`IAdminUserRepository.cs:38-39`, `ApplicationDbContext.cs:66`) · แบบแผนข้อความอีเมลซ้ำมีแล้วที่ `IAdminUserService.cs:68` · กับดักเดียวคือต้องยกเว้นแถวของตัวเองตอนเช็คซ้ำ และต้องไม่ติดธงเมื่อเปลี่ยนแค่ตัวพิมพ์ (AU-6) |
| F5.2.5 ไม่มี audit log | **n/a — ไม่มีงาน** | `UpdateBy`/`UpdateDate` ถูกเซ็ตอยู่แล้วใน `Update` (`IAdminUserService.cs:126-127`) |
| F5.2.6 โมดัลแทนที่ control เดิม | **straightforward · frontend ล้วน แต่เป็นงาน _ลบ_ ไม่ใช่งานเพิ่ม** | `Dialog` primitive + แบบแผนโมดัลมีอยู่แล้วในไฟล์เดียวกัน (`CreateUserDialog`, `page.tsx:236-363`) · สิ่งที่ต้องลบจริงระบุไว้ที่ AU-13 |
| F5.2.7 ห้ามใช้กับตัวเอง | **straightforward** | เทียบ `currentUser.UserId` กับ `user.Id` · `POST /api/auth/change-password` เดิมยังบังคับรหัสเดิมอยู่แล้วจริง (`IAuthService.cs:203-207`) ไม่ต้องแตะ |
| F5.3 ไม่ต้องมี migration | **ยืนยันแล้วว่าจริง** | ทุกฟิลด์มีอยู่บน `AdminUser` ครบ — ดู `## Data Model` §Module U |
| **F5.2.1 ช่อง `owner` × `owner`** | **⚠️ ทำได้ที่ server แต่ _ไม่มีหน้าจอไปถึง_** | `GetByCompanyId` กรอง `CompanyId == companyId` และ `owner` มี `CompanyId = null` เสมอ → owner ไม่เคยอยู่ในตาราง `/admin/users` · เป็นช่องว่างระหว่างกฎที่เคาะกับจอที่มี → **OQ-U1** |

### การตัดสินใจที่ผู้ใช้ยืนยันแล้ว

| คำถาม | เจ้าของโปรเจกต์เลือก | สิ่งที่ถูกตัดออกด้วยการเลือกนี้ |
|---|---|---|
| จะเดินหน้าออกแบบ `company-admin` ต่อเลยไหม (2026-08-21 รอบที่ 1) | **พักไว้ก่อน** — ไม่ใช่ core feature, มี workaround (owner insert DB เอง), ยังไม่มีลูกค้าใหม่รอ onboard จริง | ตัด `design.md` เต็มรูปแบบในรอบนั้น — **ถูก supersede โดยมติรอบที่ 2 ข้างล่าง** |
| ปลุกโมดูลกลับมาไหม เมื่อมีลูกค้าใหม่ "scb" เข้ามาจริง (2026-08-21 รอบที่ 2) | **ปลุกเฉพาะ F1** — Module A กลับมาเป็นงานจริง | ตัด **F2 ทั้งหมด (Module B + C)** ออกจากรอบนี้ · ตัดคำถาม A2–A6/B3/B4 ออกจากรอบนี้ · **ไม่ได้ตัด F2 ออกจาก roadmap** แค่ยังไม่ถึงคิว — scb ใช้ค่ากลางไปก่อนได้ |
| **B1** · default category chain สร้าง/ซ่อมยังไง (2026-08-21) | **สร้างอัตโนมัติสำหรับบริษัทใหม่ + ซ่อมบริษัทเก่าที่ค้างอยู่ทันที** (service idempotent + migration ซ่อมของเก่าในรอบเดียว) | ตัดทางเลือก lazy repair (ซ่อมตอนมีคนเปิดใช้) และตัดทางเลือก "ทำเฉพาะบริษัทใหม่ ปล่อยของเก่าค้าง" → **R-6 ถูกปิดในรอบนี้ ไม่เหลือเป็นบั๊กค้าง** · ผลคือมี migration 1 ใบ (CH-6) |
| **A1** · admin คนแรกได้รหัสผ่านยังไง (2026-08-21) | **owner พิมพ์รหัสเองในฟอร์ม + `MustChangePassword = true`** | ตัดการสุ่มรหัสให้แสดงครั้งเดียว และตัดแบบสลับสองโหมด → **ไม่ต้องสร้างกลไกใหม่เลย** ใช้ `IPasswordHasher` + `MustChangePassword` + หน้า `/admin/change-password` ที่มีอยู่แล้ว · แลกกับการที่ owner รู้รหัสของลูกค้าชั่วคราวจนกว่าเจ้าตัวจะเปลี่ยน (R-9) |
| **B2** · ปิดบริษัทแล้วลิงก์เดิมเป็นยังไง (2026-08-21) | **ปล่อยให้เรียนจนลิงก์หมดอายุเอง** — ไม่แก้เส้นทางฝั่งผู้เรียน | ตัดทางเลือก "ตัดทันที" → **ไม่ต้องแตะ `TrainingLinkController`/`TtsController`/`VoiceQuestionController`/join flow เลย** · แลกกับการที่ offboard ไม่ใช่การตัดทันที ซึ่งต้องเขียนให้ชัดใน UI (CP-14) และบันทึกเป็นความเสี่ยงที่ยอมรับแล้ว (R-8) |
| **N1** · slug ของบริษัทที่ถูกปิดใช้ซ้ำได้ไหม (2026-08-21) | **ปฏิเสธการใช้ซ้ำ + แก้ข้อความ error ให้บอกเหตุผลชัดเจน** | ตัดทางเลือก "เปิดบริษัทเดิมกลับอัตโนมัติ" → ไม่มีทางที่ข้อมูลเดิมของลูกค้าเก่าจะกลับมาโดยไม่ตั้งใจ · ข้อความใหม่อยู่ที่ CP-4 |
| **N2** · `DisplayName` ของ admin คนแรก (2026-08-21) | **เพิ่มช่องกรอกในฟอร์มสร้างบริษัท** | ตัดทางเลือก "ใช้อีเมลแทน" และ "ใช้ชื่อบริษัท" → `CreateCompanyDto` มีฟิลด์ `AdminDisplayName` (CP-2) |
| ~~**P1** · บทเรียนยัง override ค่า pacing เองได้ไหมเมื่อมีค่าระดับบริษัทแล้ว (2026-08-22 รอบแรก)~~ ⛔ **ถูกกลับคำตอบแล้ว 2026-08-22 รอบที่สอง — ดูแถว N1 ท้ายตาราง · ห้ามใช้ช่องขวาเป็นข้ออ้างอิงอีกต่อไป** | ~~**ได้** — ค่าที่ระดับบทเรียนเป็น nullable · `null` = สืบทอดจากบริษัท · ฟอร์มบทเรียนยังมี 3 ช่องนี้อยู่ ปล่อยว่างได้ | ตัดทางเลือก "ย้ายขึ้นบริษัทอย่างเดียว ลบช่องออกจากฟอร์มบทเรียน" → **ความรกของฟอร์มไม่ได้หายไปทั้งหมด** แลกกับบทเรียนที่ต้องการจังหวะพิเศษ · ผลคือ `LessonConfig` 3 คอลัมน์ต้องกลายเป็น nullable (LP-3)~~ **← ทางเลือกที่เคย "ถูกตัดออก" ในช่องนี้ คือสิ่งที่เจ้าของโปรเจกต์เลือกจริงในรอบที่สอง (N1)** |
| **P2** · ชุดค่า default ที่ถูกต้องคือชุดไหน (2026-08-22) | **ยึด server env — `introWaitMs = 5000` · `breathPauseMs = 500` · `finalQuestionWaitMs = 5000`** (`ServerDefaults.GetLessonTimingDefaults()` → `TutorConfig.Default*`) | ตัดชุดของฟอร์มสร้างบทเรียน (3000/800/5000) และชุด fallback ตอนเล่นจริง (5000/1000/5000) ออกจากการเป็น "ค่าอ้างอิง" → **ทั้งสองจุดเป็นบั๊กที่ต้องแก้ให้ตรง ไม่ใช่ทางเลือก** (LP-13) |
| **P3** · ต้องรอหน้า UI ตั้งค่าบริษัท (F2) ก่อนไหม (2026-08-22) | **ไม่ต้องรอ — ลง schema/API ระดับบริษัทได้เลย** | ตัดการผูกงานนี้ไว้กับ Module B ทั้งก้อน → **แยกออกมาเป็น Module P ต่างหาก** ทำให้ A2/A3/A4/A6/B3b ที่ยังไม่เคาะไม่บล็อกงานนี้ |
| ~~**P4** · ค่าที่บทเรียนเดิมกรอกไว้แล้วจะเป็นอย่างไรตอน migrate (2026-08-22 รอบแรก)~~ ⛔ **ถูกแทนที่ด้วย N2 — ดูท้ายตาราง** | ~~**backfill เป็น override ทุกบท** — ไม่มีบทไหนกลายเป็น `null` อัตโนมัติ พฤติกรรมเดิมคงเดิม 100% | ตัดทางเลือก "ล้างค่าที่ตรงกับ default ให้กลายเป็น inherit อัตโนมัติ" → ไม่มี heuristic เดาว่าบทไหน "ตั้งใจตั้ง" · ผลข้างเคียงที่ดี: การขยาย `NOT NULL → NULL` เก็บค่าเดิมไว้ครบอยู่แล้ว **จึงไม่ต้องเขียน UPDATE ใดๆ ใน migration** (LP-3)~~ **← หมดผลบังคับแล้ว: N2 สั่งให้ทิ้งค่าเดิมทั้งหมด** |
| **P5** · ค่าระดับบริษัทของบริษัทใหม่มาจากไหน (2026-08-22) | **backfill จาก server env ทันทีตอนสร้างบริษัท — ห้ามเป็น `null` ที่ชั้นบริษัทเด็ดขาด** | ตัดชั้น fallback ที่สาม (env ตอน runtime) ออกทั้งชั้น → เหลือ **2 ชั้นเท่านั้น: บทเรียน → บริษัท** · env ถูกอ่านครั้งเดียวตอน seed ค่าให้บริษัท (LP-1/LP-2) |
| **B3a** · `null` ที่ชั้นบริษัทแปลว่าอะไร สำหรับค่าที่ไม่มี "ค่ากลาง" ใช้ตอน runtime (2026-08-22) | **สำหรับ pacing: คำถามนี้ไม่มีอยู่จริง** — คอลัมน์เป็น `NOT NULL` ที่ชั้นบริษัท จึงไม่มีสถานะ `null` ให้ตีความ | ตัดความจำเป็นต้องมี flag/sentinel/กฎพิเศษใดๆ เพื่อแยก "ยังไม่ตั้ง" ออกจาก "ตั้งเป็นค่านี้" · **ไม่ครอบแบรนด์** — ส่วนแบรนด์แยกเป็น **B3b ยังเปิดอยู่** |
| **B4** · รูปร่าง schema ของค่าตั้งค่าระดับบริษัท (2026-08-22) — *`system-analyst` ตัดสินทางเทคนิค บนข้อจำกัดธุรกิจที่ผู้ใช้ให้มา* | **(ก) เพิ่มคอลัมน์ลง `Company` ตรงๆ** — กฎ null สองแบบในตารางเดียวแสดงออกด้วย **ชนิดของคอลัมน์เอง**: pacing = `NOT NULL`, ค่าอื่นของ F2 = nullable | ตัด (ข) ตาราง `CompanySetting` — จะเกิดเคสกำกวม "ไม่มีแถว" ที่ขัดกับ P5 โดยตรง และต้องมี repair path เพิ่ม · ตัด (ค) ตาราง + `ICompanyScoped` + query filter — owner อ่านค่าบริษัทอื่นไม่เจอ (F-5/F-6) · **ไม่มี flag พิเศษเกิดขึ้นเลยแม้แต่ตัวเดียว** |
| **A5 (เฉพาะแถว pacing)** · ขอบเขตค่าที่รับได้ของ 3 ค่านี้ (2026-08-22) | **`system-analyst` เสนอช่วง 0–60000 / 0–10000 / 0–120000 ms** และบังคับเท่ากันทั้งสองชั้น (LP-8) | ตัดการปล่อยให้ engineer เลือกเอง · **ตัวเลขนี้เป็นข้อเสนอที่ยังไม่ได้ผ่านปากเจ้าของโปรเจกต์** — แก้ทีหลังราคาถูกเพราะเป็น validation ล้วน ไม่ใช่ชนิดคอลัมน์ (ดู `## Unresolved Open Questions` §A5) |
| **P6** · เริ่มทำหน้า UI ตั้งค่าบริษัทเลย หรือรอ F2 ครบทั้งชุด (2026-08-22 · เจ้าของโปรเจกต์คุยตรงในแชท) | **เริ่มเลย ไม่ต้องรอ** — ทำหน้าตั้งค่าที่มี **section เดียวก่อน (pacing)** แล้วค่อยเติม section อื่นของ F2 (ลิงก์หมดอายุ/TTS/แบรนด์) ทีหลัง **ทีละอย่าง** | **ยกเลิกข้อห้าม LP-15 ข้อสุดท้าย** ("ห้ามสร้างหน้า UI ตั้งค่าบริษัท") · ตัดทางเลือก "รอ A2/A3/A4/B3b เคาะครบแล้วค่อยทำทั้งหน้าเดียวจบ" ทิ้ง → บังคับให้หน้าจอต้อง **ขยายทีละ section ได้จริง** ไม่ใช่หน้าเดี่ยวของ pacing (SP-1/SP-2) · **ไม่ได้ปลุก Module B** — ค่าอื่นของ F2 ยังห้าม implement เหมือนเดิม (SP-13) · **ไม่มี schema change และไม่มีงาน backend ใด ๆ ตามมา** |
| **A6 (เฉพาะ section pacing)** · `cs` เห็นหน้าตั้งค่าไหม (2026-08-22) | **(ข) เห็นแบบอ่านอย่างเดียว** — สอดคล้องกับ LP-9 ที่**ตั้งใจ**ให้ `cs` `GET` ได้อยู่แล้ว และกับความจริงที่ `cs` เห็นค่าบริษัทเป็น placeholder ในฟอร์มบทเรียนอยู่แล้ว (LP-5) การซ่อนหน้าจึงไม่ได้ปิดข้อมูลอะไรเลย แค่ทำให้ `cs` ไม่รู้ว่าเลขนั้นมาจากไหน | ตัด (ก) ซ่อนทั้งหน้าจาก `cs` — จะขัดกับ LP-9 ที่ให้สิทธิ์อ่านไว้แล้ว · ตัด (ค) เห็นแล้วกดแก้จนเด้ง error — UX แย่ · **ผลที่ตามมาเป็นงานจริง**: ต้องขยับ gate ของกลุ่มเมนู "ตั้งค่า" จากระดับกลุ่มลงไประดับรายการ (SP-5) · **ครอบเฉพาะ section pacing เท่านั้น** — `cs` จะเห็น section อื่นของ F2 ไหม ยังไม่ตอบ (ดู A6 ที่เหลือ + A8) |

| **A8** · สิทธิ์ต่อ section ต่างกันได้ไหม และใครตัดสิน (2026-08-22 · เจ้าของโปรเจกต์ตอบตรงในแชท) | **ได้ — และเป็น 2 แกนที่แยกกัน**: "เห็น" (`visibleToRoles`) กับ "แก้" (`editableByRoles`) ประกาศแยกกันต่อ section · section ที่อ่อนไหว **ซ่อนจาก role ไปเลยได้** ไม่ใช่แค่ read-only · section ที่ไม่อ่อนไหว (pacing/เสียง) เห็นได้ทุก role · **สำหรับ section pacing รอบนี้: เห็นทุก role (`owner`/`admin`/`cs`) · แก้ได้เฉพาะ `owner`/`admin` = ตรง LP-9/SP-4 เดิมทุกประการ ไม่มีอะไรเปลี่ยน** | ตัดสมมติฐานว่า "ทุก section ต้องเห็นได้ทุก role แล้วต่างกันแค่ read-only" ทิ้ง → กลไกต้องรองรับการ**ซ่อน**ตั้งแต่รอบนี้ ไม่ใช่ไปเพิ่มทีหลัง (SP-15 · เมนู sidebar ต้อง derive จาก registry ไม่ hardcode role — SP-5) · ตัดสิทธิ์การเดาของ engineer: ค่าของสองแกนนี้เป็นคำตอบของ**เจ้าของโปรเจกต์ต่อ section** ไม่มีค่า default ให้เดา (SP-15 ข้อ 8) · **ปิดเฉพาะ "กลไก"** — เนื้อของ section ที่สองยังบล็อกด้วย A6 ที่เหลือ + A2/A3/A4/B3b เหมือนเดิม |
| **A5 (แถว pacing) / LP-8** · ยืนยันช่วงค่าที่รับได้ (2026-08-22) | **ยืนยันใช้ตัวเลขเดิมที่ `system-analyst` เสนอ**: `introWaitMs` **0–60000** · `breathPauseMs` **0–10000** · `finalQuestionWaitMs` **0–120000** ms — เจ้าของโปรเจกต์ระบุว่า "ใช้ค่าเดิมไปก่อน จูนทีหลังได้" | ปิดสถานะ "ข้อเสนอที่ยังไม่ผ่านปากเจ้าของโปรเจกต์" ของ LP-8 → **เป็นมติแล้ว ห้ามถามซ้ำ ห้ามรอคำยืนยันก่อนเขียน validation** ทั้งฝั่ง server (data annotation) และ client (constant ตาม SP-8) · ตัดความจำเป็นต้องแยก "ค่าชั่วคราว" ออกจาก "ค่าจริง" · **ยังไม่กระทบชนิดคอลัมน์และไม่ต้อง migrate** — การจูนภายหลังยังแก้ที่ LP-8 ที่เดียวเหมือนเดิม |

| **N1** · ยังคงช่องกรอก pacing ไว้ในฟอร์มบทเรียนไหม (2026-08-22 **รอบที่สองของวันเดียวกัน** · เจ้าของโปรเจกต์กลับคำตอบเองหลังเห็นหน้าจอจริง) | **ตัดออกถาวร — ไม่มี override ต่อบทเรียนอีกต่อไป** ทุกบทเรียนใช้ค่ากลางของบริษัทล้วน · เหตุผลคำต่อคำ: *"ส่วนที่ย้ายไปแล้วก็ควรเคลียร์ทิ้ง เพราะมันซ้ำ จำเจ จุดประสงค์ที่ย้ายออกไปเป็นของรวม คือให้เป็น UX เดียวกันทั้งระบบ ไม่ต้องให้ซับซ้อนโดยใช่เหตุ"* | **กลับมติ P1 ทั้งข้อ** → ตัดโมเดล 2 ชั้นทิ้งเหลือชั้นเดียว · ตัดกฎ empty-vs-zero ที่ชั้นบทเรียนทั้งก้อน (LP-3/LP-11 ยกเลิก) · ตัดความจำเป็นของ placeholder "ว่าง = ใช้ค่าบริษัท" (LP-5) · ตัดเหตุผล**เดิม**ที่ `cs` ต้อง `GET` ค่าบริษัท (LP-9 ยังให้ `cs` อ่านได้ แต่ด้วยเหตุผลใหม่คือหน้า `/admin/settings`) · **แลกกับ: บทเรียนที่ต้องการจังหวะพิเศษทำไม่ได้อีกต่อไป** จนกว่าจะมีรอบออกแบบใหม่ (OQ-P7) |
| **N2** · ค่า override ที่บทเรียนเดิมมีอยู่แล้ว (2026-08-22 รอบที่สอง) | **ทิ้งทั้งหมด** — ไม่เก็บ ไม่ archive ไม่ย้ายไปคอลัมน์อื่น · เจ้าของโปรเจกต์ยอมรับแล้วว่าบทเรียนเดิมบางบทจะมีจังหวะเปลี่ยนไปหลัง migration | **แทนที่ P4 ทั้งข้อ** → ตัดทางเลือก "เก็บค่าเดิมไว้ก่อนเผื่อกลับมาใช้" และ "ย้ายค่าเดิมของบทเรียนไปเป็นค่ากลางของบริษัทนั้น" · ผลคือ migration ใหม่เป็น **data loss ที่ตั้งใจ** (DM-P2 · R-16) ไม่ใช่ผลข้างเคียงที่บังเอิญเกิด |
| **N3** · ชะตากรรมของคอลัมน์ pacing ใน `LessonConfig` (2026-08-22 รอบที่สอง) | **ลบคอลัมน์ออกจากฐานข้อมูลจริง** — ไม่ใช่แค่ซ่อน UI และไม่ใช่ปล่อยค้างเป็น nullable ที่ไม่มีใครใช้ ("ต้องการให้สะอาดครบทั้ง schema") | ตัดทางเลือก "ซ่อนที่ UI พอ" (จะเหลือคอลัมน์ที่ไม่มีโค้ดไหนเขียนแต่ยังอ่านได้ = กับดักของคนอ่านคนถัดไป) และ "ปล่อยเป็น nullable ที่ตายแล้ว" · ผลคือ **migration ใบใหม่ `RemoveLessonConfigPacingOverrides`** และทำให้ **D-3 เปลี่ยนคำตอบอีกรอบ** (ไม่ใช่ nullable แล้ว — คือ **ไม่มีฟิลด์เลย**) |

**มติของ F5 (2026-08-25) — เจ้าของโปรเจกต์ตอบผ่าน `business-analyst` ครบทั้งหกข้อในวันเดียว**
(OQ-9..OQ-14 · เนื้อคำตอบเป็นกฎธุรกิจ F5.2.1–F5.2.7 ใน `requirement.md` แล้ว)

| คำถาม | เจ้าของโปรเจกต์เลือก | สิ่งที่ถูกตัดออกด้วยการเลือกนี้ |
|---|---|---|
| **การเปิด scope F5 กลับ** (2026-08-25) — `design.md` CP-15 เคยห้ามไว้ | **เปิดกลับ เพราะข้อห้ามเดิมตั้งอยู่บนข้อเท็จจริงที่ผิด** — `/admin/users` ไม่เคยแก้อีเมล/รีเซ็ตรหัสของคนอื่นได้เลย · **ไม่ใช่การเปลี่ยนใจ** | ตัดสถานะ "ปิดแล้ว" ของข้อนี้ทิ้ง · **ไม่ได้ยกเลิกทั้ง CP-15** — ส่วน "ห้ามสร้าง _หน้า_ ใหม่" ยังใช้อยู่ทุกตัวอักษร (F5.0) |
| **OQ-9** · ใครทำกับใครได้ | **กฎเดิม + peer-lockout** (`admin` ทำกับ `admin` ไม่ได้ · `owner` เป็นข้อยกเว้นเดียว) | ตัด (ก) "ใช้กฎเดิมเฉย ๆ" ซึ่งเปิดช่องให้ admin สองคนของบริษัทเดียวกันยึดบัญชีกันเอง · ตัด (ค) "owner เท่านั้น" ซึ่งทำให้ admin ของลูกค้าช่วย cs ที่ลืมรหัสไม่ได้ = ปัญหาเดิมไม่หาย |
| **OQ-10** · รีเซ็ตรหัสแล้วเกิดอะไร | **ผู้มีสิทธิ์พิมพ์รหัสชั่วคราวเอง + `MustChangePassword = true`** | ตัดตัวสุ่มรหัส + กติกาห้ามดูซ้ำ · ตัดลิงก์รีเซ็ตทางอีเมล (ไม่มี SMTP) → **ไม่มีงาน auth ใหม่เลย** ใช้ middleware เดิมทั้งหมด (AU-8) |
| **OQ-11** · แก้อีเมลคนอื่น | **แก้ได้อิสระ แต่ _บังคับ_ ติดธงเปลี่ยนรหัสเสมอ** | ตัด (ก) ล้วน ๆ ที่เปิดช่อง "ย้ายอีเมลมาเป็นของตัวเองแล้วรีเซ็ตรหัส = ยึดบัญชีเงียบ ๆ" · ตัด (ค) ห้ามแก้ ซึ่งขัดกับดีไซน์ Figma · ตัดการยืนยันอีเมลด้วยลิงก์ |
| **OQ-12** · audit log | **ไม่ทำ ใช้ `UpdateBy`/`UpdateDate`** | **ตัดสิ่งเดียวใน F5 ที่จะทำให้ต้องมี migration** → F5.3 · แลกกับการเก็บได้แค่ "คนล่าสุด" และแยกรีเซ็ตรหัสออกจากแก้อีเมลไม่ได้ |
| **OQ-13** · โมดัลทับ control เดิมไหม | **แทนที่ — ต้องลบของเดิมออกจริง** | ตัด (ข) "อยู่ร่วมกัน" → ไม่ต้องดูแลตรรกะสิทธิ์เดียวกันสองที่ · **ผลที่ตามมาเป็นงานลบจริงใน `page.tsx`** ไม่ใช่แค่งานเพิ่ม (AU-13) |
| **OQ-14** · ใช้กับตัวเองได้ไหม | **ห้าม เป็นกฎแข็ง ไม่ยกเว้นแม้แต่ `owner`** | ตัด (ข) "ใช้กับตัวเองได้ สะดวกกว่า" ซึ่งจะทำลายจุดประสงค์ของหน้าเปลี่ยนรหัสเดิมทั้งหมด (คนที่ยึดเครื่องที่ล็อกอินค้างไว้ตั้งรหัสใหม่ได้โดยไม่ต้องรู้รหัสเดิม) |

**การตัดสินทางเทคนิคของ `system-analyst` ในรอบนี้** (ไม่ใช่คำถามธุรกิจ — ตัดสินเองพร้อมเหตุผล
ตามที่ `requirement.md` มอบไว้ให้)

| คำถาม | เลือก | สิ่งที่ถูกตัดออก |
|---|---|---|
| **U1** · รูปร่าง endpoint/DTO | **ขยาย `UpdateAdminUserDto` ใบเดิม ใช้ `PUT /api/admin-users/{id}` เดิม** (AU-2) | ตัดการแยกเป็นสาม endpoint (`/role`, `/email`, `/password`) — จะทำให้เกิด partial failure แบบ "บริษัทครึ่ง ๆ" ที่ CP-6 กันไว้ และต้องเขียนกฎสิทธิ์ซ้ำสามที่บนตารางที่ guard เป็นด่านเดียว (R-1) · ราคาที่จ่าย: DTO ใบเดียวถือ credential ด้วย → คุมด้วย `null` = ไม่แตะ |
| **U2** · peer-lockout ไปอยู่ที่ไหน | **เมธอด _ใหม่_ `EnsureNotSameRankPeer` ใน `IAuthorizationGuard`** ตามที่ F5.2.1 สั่งตรง ๆ | ตัดการเขียนเป็น private method ในเซอร์วิส (จะขัดกับ F5.2.1 และกับคอมเมนต์ของ guard เองที่บอกว่าเป็นที่เดียวที่ตอบคำถามสิทธิ์) · **ตัดการแก้เมธอดเดิม/แก้ `AdminRole` ทุกกรณี** → ต้องแก้ขอบเขต CP-15 ให้ตรง (เพิ่มได้ แก้ไม่ได้) |
| **U3** · self-check ไปอยู่ที่ไหน | **private method ใน `AdminUserService`** ข้าง `EnsureNotRemovingLastGuardian` (AU-5) | ตัดการยกขึ้นเป็นกฎกลางใน guard — การทำกับตัวเองไม่ผิดโดยทั่วไป (F5.2.7 บอกเองว่าทำได้ที่ `change-password`) ถ้ายกขึ้นไปจะชวนให้คนถัดไปเอาไปใช้ผิดที่ |
| **U4** · peer-lockout เทียบ role ไหน | **role _ปัจจุบัน_ ของเป้าหมายเท่านั้น** (AU-4) | ตัดการกันเพิ่มเองว่า "ห้ามเลื่อนใครขึ้นมาเป็น peer" ซึ่ง F5.2.1 ไม่ได้สั่ง และจะทำให้ endpoint แก้ไข เข้มกว่า endpoint สร้างผู้ใช้เดิมโดยไม่มีใครเคาะ · **ผลข้างเคียงถูกบันทึกเป็น R-18 + OQ-U2 ไม่ใช่ซ่อนไว้** |

**ยังเปิดอยู่**: ⏸️ **A2 · A3 · A4 · B3b** (แบรนด์/TTS) · **A6 เฉพาะส่วนที่ไม่ใช่ pacing**
— ทั้งหมดอยู่ที่ `## Unresolved Open Questions`
· **A8 ปิดแล้ว 2026-08-22** (กลไกอยู่ที่ SP-15) · **A5 ปิดครบทั้งสามแถว pacing แล้ว**
· **ไม่มีข้อไหนบล็อก Module A และไม่มีข้อไหนบล็อก Module P** (รวมงานหน้าจอตาม P6 ด้วย —
ตรวจทีละข้อแล้ว ไม่มีข้อไหนแตะ section pacing) · ทั้งหมดบล็อกเฉพาะ Module B ส่วนที่เหลือ
(ลิงก์หมดอายุ / TTS / แบรนด์) กับ Module C

## Findings from Feasibility Check — ยืนยันจากโค้ดจริง 2026-08-21 ยังไม่ implement

> เจ็ดข้อนี้อ่านจากโค้ดจริง ไม่ใช่การอนุมาน — **ไม่ต้องกลับไปตรวจซ้ำ** อ้างไฟล์และบรรทัดไว้ทุกจุด
> F-1 ถึง F-5 คือกับดักที่จะกินเวลาถ้าไม่รู้ล่วงหน้า · F-6/F-7 เป็นบริบทที่กระทบทางเลือกออกแบบ

### F-1 · `GET /api/companies` คืนเฉพาะบริษัท active — F1.6 ใช้ต่อไม่ได้

`ICompanyService.GetSwitchableCompanies()` (`ICompanyService.cs:39-55`) เรียก
`_companies.GetAllActive()` ซึ่งคือ `FindBy(x => x.IsActive)` (`ICompanyRepository.cs:23-24`)
แปลว่า **บริษัทที่ถูกปิดไปแล้วหายจากผลลัพธ์ทั้งหมด**

ผลกระทบ: F1.6 ขอ "หน้ารายการบริษัททั้งหมดสำหรับ owner (ดูว่ามีลูกค้ากี่ราย ใครยัง active)"
ซึ่งแปลว่าต้องเห็นตัวที่ปิดไปแล้วด้วย ไม่งั้น **เปิดกลับมาไม่ได้เลยผ่าน UI** (`PUT /api/companies/{id}`
มีอยู่ แต่ไม่มีทางรู้ id ของบริษัทที่ปิดไปแล้วถ้ามันไม่โผล่ในรายการไหนเลย)

→ ต้องมี **endpoint ใหม่สำหรับ owner** ที่คืนทุกบริษัทรวมตัวที่ปิดแล้ว ไม่ใช่ reuse ของเดิม
`requirement.md` §Pre-existing assets ที่เขียนว่า `GET /api/companies` "ใช้ต่อ ไม่แก้" คลาดเคลื่อนตรงนี้
(ประโยคนั้นถูกในบริบท company switcher ซึ่งเป็นคนละหน้ากับ F1.6)

### F-2 · ปิดบริษัท (`IsActive = false`) ไม่ได้บล็อกฝั่งผู้เรียนเลย — เป็นกฎธุรกิจที่ยังไม่เคาะ

`IAuthService.EnsureCompanyStillUsable` (`IAuthService.cs:105-117`) บล็อกเฉพาะ **login ของ
`admin`/`cs`** เท่านั้น ส่วนเส้นทางฝั่งผู้เรียนทั้งหมด — `GET /api/training-links/{token}`
(`TrainingLinkController.cs:56`), join/learning-session, `POST /api/tts`
(`TtsController.cs:20`), `POST /api/voice-question` (`VoiceQuestionController.cs:39`) —
**ไม่เคยเช็ค company IsActive เลยสักจุดเดียว** (`ITrainingLinkService.cs:75` เช็คแค่ `lesson.IsActive`)

ผลกระทบ: วันนี้ "offboard ลูกค้า" ตาม `Company.cs:28-29` แปลว่า *พนักงานของลูกค้าเข้าระบบไม่ได้*
แต่ **ลิงก์ที่แจกไปแล้วยังเรียนได้ตามปกติจนกว่าจะหมดอายุเอง** — ซึ่งอาจตรงหรือไม่ตรงกับที่
เจ้าของโปรเจกต์เข้าใจก็ได้ engineer ตัดสินเองไม่ได้ (ดู B2)

### F-3 · บริษัทเก่าบางรายก็ไม่มี default category chain เหมือนบริษัทใหม่ — P3 กว้างกว่าที่บันทึกไว้

`AddKnowledgeTaxonomyAndScope.cs:47-49` backfill เฉพาะบริษัทที่อยู่ใน
`(SELECT "CompanyId" FROM "LessonConfig" UNION SELECT "CompanyId" FROM "DocumentResource")`
เท่านั้น → **บริษัทที่ยังไม่เคยมีบทเรียนหรือเอกสารเลยแม้แต่ชิ้นเดียว ไม่ได้รับ chain**
และจะพังแบบเดียวกับบริษัทที่สร้างใหม่เป๊ะ: `GetSystemDefault()` (`IKnowledgeCategoryRepository.cs:27-28`)
คืน null ตอนสร้างบทเรียนแรก

ผลกระทบ: OQ-7 ใน `requirement.md` ถูกต้องเฉพาะครึ่งเดียว — **เรื่อง F2 inherit ไม่ต้อง backfill จริง**
(null = ใช้ค่ากลาง ทำงานเองอัตโนมัติ) แต่ **เรื่อง default chain ต้อง backfill** และเป็นงานคนละใบกัน
→ กลายเป็น B1

### F-4 · `POST /api/tts` เชื่อ `Voice`/`Rate` ที่ browser ส่งมาตรงๆ

`TtsController.cs:20-34` เป็น `[AllowAnonymous]` (ผูกกับ learner session เท่านั้น ไม่มี auth)
และ `ITtsService.cs:23` ส่ง `input.Voice`/`input.Rate` จาก DTO เข้า provider ตรงๆ โดยไม่ผ่าน
การตัดสินใจฝั่ง server เลย

ผลกระทบ: ถ้าจะทำ "เสียงต่อบริษัท" ให้เป็นค่าที่ **บังคับได้จริง** server ต้อง resolve เองจากบริษัท
ของ session ไม่ใช่รับจาก client — โดยต้องคงกรณี per-utterance override ที่ใช้งานจริงอยู่แล้วไว้ด้วย:
frontend ส่ง `rate` มาเฉพาะตอนเล่น filler (`config/response-texts.ts:57-62` ค่า `-45%`/`-40%`)
ผ่าน `api-client.ts:431-436` และ **ไม่เคยส่ง `voice` เลยสักจุด**
→ ต้องมีกฎลำดับความสำคัญเขียนเป็น contract ตอนกลับมาทำ (per-utterance override > ค่าบริษัท > env)

หมายเหตุ: `SynthesizeSpeechDto.Rate` มี `[RegularExpression(@"^[+-]\d{1,2}%$")]` อยู่แล้ว
(`SynthesizeSpeechDto.cs:19`) ซึ่งจำกัดช่วงไว้ที่ ±99% โดยปริยาย — เป็นข้อมูลที่ใช้ตอบ A5 ได้

### F-5 · `KnowledgeCategory` มี query filter ตาม company context — กับดักตอนสร้างบริษัทใหม่

`ApplicationDbContext.cs:136` ตั้ง `HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete)`
และ filter อ่าน `companyContext` ที่ query time (`ApplicationDbContext.cs:7-15`)

ผลกระทบ: ตอน owner สร้างบริษัท B ขณะที่ switch อยู่ที่บริษัท A — **การ insert ผ่านปกติ**
(query filter ไม่มีผลกับ insert) **แต่การอ่านกลับเพื่อตรวจ invariant จะเห็น 0 แถวเสมอ**
engineer ที่เขียนโค้ดตรวจซ้ำหลัง commit จะเจออาการ "สร้างแล้วแต่หาไม่เจอ" และมีโอกาสสูงที่จะ
"แก้" ด้วยการใส่ `IgnoreQueryFilters()` ซึ่งเป็นทางที่เคยทำให้เกิด data leak มาแล้วในโมดูลนี้
(`CompanyIsolationTests.cs:211-214` — `IgnoreQueryFilters()` ทิ้ง `CompanyId` ไปพร้อมกับ `IsDelete`)
→ ต้องเขียนเป็นข้อห้ามชัดๆ ใน contract section ตอนกลับมาทำ

### F-6 · `CompanyIsolationTests.EveryEntityIsCompanyScoped` จะ fail ทันทีถ้าเพิ่ม entity ผิดแบบ

`CompanyIsolationTests.cs:227-251` วนทุก entity ที่ implement `ICompanyScoped` แล้วบังคับว่า
ต้องมี `GetDeclaredQueryFilters().Count > 0` โดยมี allowlist แค่ `BackgroundJob` ตัวเดียว

ผลกระทบต่อ B4 โดยตรง: ตารางใหม่สำหรับค่าตั้งค่าที่ implement `ICompanyScoped` **ต้อง** มี query filter
ไม่งั้น test แดง — แต่การมี query filter จะทำให้ owner ที่ switch อยู่บริษัท A อ่านค่าของบริษัท B
ไม่เจอเลย (ปัญหาเดียวกับ F-5) ซึ่งขัดกับ F2.3 ที่ให้ owner แก้ค่าของทุกบริษัทได้
→ ทางที่รอด: เพิ่มคอลัมน์ลง `Company` ตรงๆ (ไม่ใช่ `ICompanyScoped` อยู่แล้ว) หรือทำตารางใหม่ที่
**จงใจไม่ implement `ICompanyScoped`** โดยใช้ `CompanyId` เป็น PK แบบเดียวกับที่ `Company` ทำ

### F-7 · `.claude/agents/backend-engineer.md` §Auth ล้าสมัย

ไฟล์นั้นระบุว่า *"Auth: none implemented yet in this project (`/admin/*` is currently open)"*
แต่ของจริงมี JWT + `AdminRole` (owner/admin/cs) + `IAuthorizationGuard` ครบแล้ว
(`IAuthService.cs`, `IAuthorizationGuard.cs`, `AdminUser.cs`) และ `knowledge-base` ก็ออกแบบโดย
อาศัยข้อเท็จจริงนี้ไปแล้ว (บันทึกไว้ใน `status.md` §knowledge-base "Contract dependency ที่ตรวจแล้ว" ข้อ 2)

`system-analyst` แก้ไฟล์นั้นไม่ได้ (เป็นของ `backend-engineer` เท่านั้นตาม `conventions.md` §9)
— บันทึกไว้เพื่อให้ชัดว่า **คำตัดสิน feasibility ทุกข้อในเอกสารนี้อ้างจากโค้ดจริง ไม่ใช่บรรทัดนั้น**

## Data Model

### Module A (F1) — **ไม่มี schema change เลยแม้แต่ฟิลด์เดียว** ✅ ยืนยันแล้ว 2026-08-21

ไล่ requirement ของ F1.1–F1.6 ทีละข้อเทียบกับ entity ที่มีอยู่จริงแล้ว **ทุกข้อ map ลงของเดิมได้ครบ**
ไม่ต้องเพิ่มตาราง ไม่ต้องเพิ่มคอลัมน์ ไม่ต้องเปลี่ยนชนิด ไม่ต้องเพิ่ม index

| requirement | entity/field ที่รองรับอยู่แล้ว | ที่มา |
|---|---|---|
| F1.2 แถว `Company` | `Company.Id` (slug) · `Name` · `IsActive` · `CreateBy`/`CreateDate` | `Company.cs:15-38` |
| F1.2 แถว `AdminUser` role `admin` | `AdminUser.Id`/`CompanyId`/`Role`/`Email`/`PasswordHash`/`DisplayName`/`IsActive`/`MustChangePassword` | `AdminUser.cs:19-63` |
| F1.2 ผูกกับบริษัทที่เพิ่งสร้าง | `AdminUser.CompanyId` (nullable เฉพาะ owner) | `AdminUser.cs:23-28` |
| F1.3 default chain | `KnowledgeCategory.Id`/`CompanyId`/`ParentId`/`Level`/`Name`/`SortOrder`/`IsSystemDefault` | `KnowledgeCategory.cs:5-22` — **เป็น entity ของโมดูล `knowledge-base` โมดูลนี้ใช้ตามสัญญาเดิม ห้ามแก้** |
| F1.5 กันอีเมลซ้ำทั้งระบบ | unique index บน `AdminUser.Email` | `ApplicationDbContext.cs:66` |
| F1.6 ปิดใช้งานบริษัท | `Company.IsActive` | `Company.cs:28-30` |
| audit ว่าใครสร้าง/ปิด (ระดับฟิลด์ ตาม R-3) | `Company.CreateBy`/`UpdateBy`/`UpdateDate` | `Company.cs:32-35` |

**ทำไมข้อสรุปนี้มั่นคงพอที่จะเขียนก่อนคำถามถูกเคาะ**: ตรวจแล้วว่า **ไม่มีคำตอบของ A1/B1/B2/N1/N2
ทางไหนเลยที่ทำให้ต้องแก้ schema** —

- A1 ไม่ว่าจะเลือก "owner พิมพ์เอง" หรือ "ระบบสุ่มให้" ก็ลงที่ `PasswordHash` + `MustChangePassword` เหมือนกัน
- B1 ทุกทางเลือกใช้คอลัมน์ของ `KnowledgeCategory` ที่มีอยู่แล้วทั้งหมด (การ backfill คือการ **insert แถว** ไม่ใช่ DDL)
- B2 ทุกทางเลือกอ่าน `Company.IsActive` ที่มีอยู่แล้ว ต่างกันแค่ *เช็คที่ไหนบ้าง*
- N1/N2 เป็นกฎ validation และรูปฟอร์ม ไม่แตะโครงตาราง

**สิ่งที่ต้องยืนยัน** จึงไม่ใช่ "หน้าตาตารางเป็นยังไง" แต่เป็นประโยคเดียว: *"ยืนยันว่า F1 ไม่ต้องแก้
โครงฐานข้อมูลเลย ใช้ `Company`/`AdminUser`/`KnowledgeCategory` เดิมตามที่เป็นอยู่"*

### Migration Plan (Module A)

| ใบ | เนื้อหา | ชนิด | เงื่อนไข |
|---|---|---|---|
| — | **ไม่มี migration สำหรับ F1 เอง** | — | เพราะไม่มี schema change |
| `BackfillMissingDefaultCategoryChain` | **data-only migration ไม่มี DDL** — insert default chain ให้บริษัทเดิมที่ยังไม่มี (F-3) · สเปกเต็มอยู่ที่ **CH-6** | **additive** (insert อย่างเดียว + `UPDATE` เฉพาะ `ParentId` ของ leaf ที่กำพร้า ไม่ลบแถวใด) | ✅ **ยืนยันแล้วว่ามีใบนี้** — B1 เคาะเป็น "ซ่อมของเก่าทันที" |

ชื่อใบนี้แยกจาก migration ของ `knowledge-base` (`AddKnowledgeTaxonomyAndScope`,
`AddDurableIndexingJobs`, `AddDocumentChunks`, `AddLessonSlideNarrations`, `AddKnowledgeQnA`)
และของ `learning-session` ชัดเจน ตามแบบแผน "หนึ่ง phase หนึ่ง migration ของเจ้าของ phase"

**ข้อบังคับของใบนี้ถ้าเกิดขึ้นจริง**: ต้องมี guard ว่าบริษัทที่มี chain อยู่แล้วจะ **ไม่ได้แถวที่สอง**
ไม่งั้น `GetSystemDefault()` (`IKnowledgeCategoryRepository.cs:27-28`) จะเจอ 2 แถวแล้ว
`SingleOrDefault()` throw ทันที — เปลี่ยนจากบั๊ก "ไม่มีหมวด" เป็นบั๊ก "พังทั้งบริษัท" ซึ่งแย่กว่าเดิม

### Module P (ค่า pacing ระดับบริษัท) — ✅ เคาะแล้ว 2026-08-22 · **เป็น contract**

**B4 เคาะเป็นทางเลือก (ก): คอลัมน์บน `Company` ตรงๆ ไม่มีตารางใหม่** — เหตุผลที่เลือกทางนี้
อยู่ในตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" แถว B4 · หัวใจคือ **กฎ null สองแบบในตารางเดียว
แสดงออกด้วยชนิดของคอลัมน์เอง ไม่ต้องมี flag เพิ่มแม้แต่ตัวเดียว**:

| กลุ่มค่า | ชนิดคอลัมน์ที่ชั้น `Company` | ความหมาย |
|---|---|---|
| **pacing 3 ตัว** (รอบนี้) | `int` **NOT NULL** | ตั้งเสมอตอนสร้างบริษัท (P5) · ไม่มีสถานะ "ยังไม่ตั้ง" ให้ตีความ · ไม่มี fallback ไป env ตอน runtime |
| ลิงก์หมดอายุ / TTS / แบรนด์ (Module B ที่ยังพัก) | nullable | `null` = inherit จากค่ากลาง/env ตามกฎเดิมของ F2.2 |

**⚠️ รอบนี้เพิ่มเฉพาะ 3 คอลัมน์ pacing เท่านั้น** — คอลัมน์ของ Module B ที่เหลือ **ห้ามใส่มาล่วงหน้า**
แม้จะรู้รูปร่างแล้ว (R-4 + CP-15) เพราะความหมายของมันยังขึ้นกับ A2/A3/A4/B3b ที่ยังไม่เคาะ

#### DM-P1 · `Company` (แก้ของเดิม — **additive + backfill**)

```csharp
public sealed class Company : IEntityMaster<string>
{
    // ── ของเดิมทั้งหมด ไม่แตะแม้แต่ฟิลด์เดียว ──
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required bool IsActive { get; set; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ── ใหม่ (Module P) — ค่าเริ่มต้นจังหวะการสอนของทุกบทเรียนในบริษัทนี้ ──
    /// <summary>ห้ามเป็น null เด็ดขาด (P5) - ตั้งจาก ServerDefaults.GetLessonTimingDefaults()
    /// ตอนสร้างบริษัท และเป็น "แหล่งความจริงเดียว" ของค่านี้ทั้งระบบ: ไม่มีชั้นบทเรียนอยู่เหนือมัน
    /// (N1 ตัดทิ้งแล้ว) และไม่มี fallback ไป env ใต้มันตอน runtime (LP-1).
    /// ต่างจากคอลัมน์ตั้งค่าอื่นของ F2 ที่ null = inherit - ความต่างนี้ตั้งใจ ดู DM-P1 หัวตาราง</summary>
    public required int DefaultIntroWaitMs { get; set; }
    public required int DefaultBreathPauseMs { get; set; }
    public required int DefaultFinalQuestionWaitMs { get; set; }
}
```

**ชื่อคอลัมน์มีคำนำหน้า `Default`** โดยตั้งใจ — อ่านคู่กับ `LessonConfig.IntroWaitMs` แล้วต้องแยกออก
ทันทีว่าตัวไหนคือ "ค่าตั้งต้นของบริษัท" ตัวไหนคือ "ค่าที่บทเรียนนี้ใช้จริง" · **ห้ามตั้งชื่อซ้ำกันสองชั้น**

`Company` **ยังคงไม่เป็น `ICompanyScoped` และยังต้องไม่มี query filter** (CP-15) — นี่คือคุณสมบัติที่
ทำให้ทางเลือก (ก) ใช้ได้ตั้งแต่แรก (F-5/F-6) ไม่ใช่รายละเอียดที่บังเอิญเป็นแบบนั้น

#### DM-P2 · `LessonConfig` — **ไม่มีคอลัมน์ pacing เลย · ต้องลบออกถ้ามีอยู่** (เขียนใหม่ทั้งข้อ 2026-08-22 รอบที่ 7 · มติ N1/N2/N3)

> ⚠️ **ข้อนี้เปลี่ยนทิศทางจากรอบก่อนแบบตรงกันข้าม** — เวอร์ชันเดิม (2026-08-22 รอบที่ 4) สั่งให้
> "ขยายชนิดเป็น `int?` เพื่อรองรับ override ต่อบทเรียน" และ **ถูก implement ไปแล้วจริงใน Phase 4**
> · N1 ตัด override ทิ้งถาวร N3 จึงสั่งให้ **ลบคอลัมน์ออกจากฐานข้อมูลจริง** ไม่ใช่ปล่อยค้างไว้

```csharp
public sealed class LessonConfig : IEntityMaster<string>, ICompanyScoped
{
    // ── ทุกฟิลด์อื่นคงเดิมทั้งหมด (Id · CompanyId · audit · Slug · CategoryId · Title ·
    //    Description · SlidesSourceUrl · PresentationId · SlidesEmbedUrl · ContentSourceType ·
    //    PdfDocumentResourceId · SlideConfigs · IsActive) ──

    // ── ไม่มี IntroWaitMs / BreathPauseMs / FinalQuestionWaitMs ที่นี่ (N1/N3) ──
    // ค่าเหล่านี้เป็นของ Company.Default*Ms เท่านั้น (DM-P1) - บทเรียนไม่มีสิทธิ์ override
    // ถ้าเห็นสามชื่อนี้โผล่ใน LessonConfig อีก ไม่ว่าเป็น int, int? หรือ [NotMapped] = ผิด contract
}
```

**สถานะจริงของฐานข้อมูลวันนี้ (ตรวจไฟล์จริงแล้ว ไม่ใช่ความจำ):** สามคอลัมน์นี้ **มีอยู่จริงใน
`LessonConfig`** และเป็น `integer NULL` หลังจาก migration `20260822074306_AddCompanyLessonPacingDefaults`
(applied กับ local Postgres ไปแล้ว) — **คอลัมน์ไม่ได้ถูกสร้างโดยใบนั้น** ใบนั้นแค่ `AlterColumn`
`NOT NULL → NULL`; ตัวคอลัมน์มีมาตั้งแต่ `InitialCreate` · ฉะนั้นงานของรอบนี้คือ **`DROP COLUMN`
ของจริง** ไม่ใช่การ "ถอน migration ที่เพิ่งเพิ่ม"

**⚠️ นี่คือ breaking change + data loss ที่ตั้งใจ (N2):**

| ประเด็น | คำตัดสิน |
|---|---|
| ค่า override เดิมของบทเรียน (เช่น 3000/500/3000) | **หายถาวรเมื่อ migration รัน** — ไม่ backfill ไปที่ไหน ไม่ archive ไม่ export เข้าตารางอื่น (N2) |
| ผลต่อพฤติกรรม | บทเรียนที่เคยมีจังหวะเฉพาะตัว **จะเปลี่ยนไปใช้ค่ากลางของบริษัททันที** — เจ้าของโปรเจกต์รับทราบและยอมรับแล้ว |
| ย้อนกลับได้ไหม | **ไม่ได้** — down migration สร้างคอลัมน์คืนได้ แต่ **ค่าเดิมไม่กลับมา** (ดู migration `RemoveLessonConfigPacingOverrides` ข้อบังคับข้อ 3) |
| ทำไมไม่เก็บไว้ก่อน | เจ้าของโปรเจกต์ตัดสินโดยตรงว่าเป็น "ความซับซ้อนโดยใช่เหตุ" ที่ขัดกับเหตุผลของการย้ายค่าขึ้นบริษัทตั้งแต่แรก — **เป็นการตัดสินใจทางธุรกิจ ไม่ใช่ข้อจำกัดทางเทคนิค** |

**⚠️ `LessonConfig` เป็น entity ที่ `_docs/module/knowledge-base/design.md` §DM-2 ประกาศไว้**
(ที่นั่นเขียนไว้ว่า 3 ฟิลด์นี้ "คงเดิม" ในฐานะ `required int`) — ไม่ใช่ของโมดูลนี้ · การแก้ครั้งนี้
เป็น **cross-module change ที่จงใจ** เพราะกฎที่บังคับให้แก้ (N1: ค่านี้เป็นของบริษัทล้วน) เป็นกฎ
ของค่าตั้งค่าระดับบริษัท ซึ่งเป็นเรื่องของโมดูลนี้ · **คำตอบที่ถูกต้องของ D-3 เปลี่ยนอีกรอบ**:
ไม่ใช่ "แก้ DM-2 เป็น nullable" อีกต่อไป แต่เป็น **"ลบสามฟิลด์นี้ออกจาก DM-2"** → D-3

#### Migration Plan (Module P)

| ใบ | เนื้อหา | ชนิด | หมายเหตุ |
|---|---|---|---|
| `AddCompanyLessonPacingDefaults` (มีอยู่แล้ว · applied กับ local Postgres แล้ว) | (1) `ALTER TABLE "LessonConfig"` ทำ 3 คอลัมน์ pacing ให้ **nullable** · (2) `ALTER TABLE "Company"` เพิ่ม 3 คอลัมน์ `Default*Ms` แบบ **NOT NULL พร้อม backfill ค่าให้แถวเดิม** | (1) ขยายชนิด · (2) **additive + backfill** | **ห้ามแก้ใบนี้ย้อนหลังและห้าม `migrations remove`** (CLAUDE.md กฎข้อ 6 + incident ที่บันทึกไว้ใน `status.md`) — ส่วน (2) ยังถูกต้องและยังต้องอยู่ · ส่วน (1) ถูก **แทนที่** โดยใบถัดไป ไม่ใช่ถูกยกเลิก |
| **`RemoveLessonConfigPacingOverrides` (ใบใหม่ · รอบที่ 7)** | `ALTER TABLE "LessonConfig" DROP COLUMN "IntroWaitMs", "BreathPauseMs", "FinalQuestionWaitMs"` (สามคำสั่ง `DropColumn`) | **breaking + data loss ที่ตั้งใจ (N2/N3)** | **ห้ามมี `UPDATE` ใด ๆ ที่พยายามรักษาค่าเดิมไว้ก่อนลบ** — การเก็บค่าเดิมคือสิ่งที่ N2 สั่งไม่ให้ทำ |

**ข้อบังคับของใบ `RemoveLessonConfigPacingOverrides` (ห้ามเดา):**

1. **ต้องมีคอมเมนต์ในไฟล์ migration ระบุว่านี่คือการตัดสินใจทางธุรกิจ ไม่ใช่อุบัติเหตุ** —
   อย่างน้อยต้องบอกสามอย่าง: (ก) มติ **N1/N2/N3** ของเจ้าของโปรเจกต์เมื่อ 2026-08-22 ที่กลับคำตอบ
   P1 · (ข) ค่าที่อยู่ในสามคอลัมน์นี้ **ถูกทิ้งโดยเจตนา** ไม่ใช่ลืม backfill · (ค) ชี้มาที่
   `design.md` §DM-2 นี้ · เหตุผล: คนที่อ่าน migration ในอีกหกเดือนจะเห็นแค่ `DropColumn` สามบรรทัด
   และไม่มีทางแยกออกว่านี่คือ "ตั้งใจทิ้งข้อมูลลูกค้า" หรือ "พลาด" — ในโค้ดเบสนี้คอมเมนต์มีไว้
   อธิบาย "ทำไม" อยู่แล้ว (CLAUDE.md) และนี่คือกรณีที่ราคาของการไม่เขียนสูงที่สุด
2. **ใบนี้แยกจากใบเดิมเสมอ ห้ามรวมหรือแก้ใบเดิม** — `AddCompanyLessonPacingDefaults` ถูก apply
   ไปแล้วกับ local Postgres จริง การแก้ใบที่ apply แล้วคือสิ่งที่ CLAUDE.md ข้อ 6 ห้ามตรง ๆ
3. **down migration**: สร้างคอลัมน์คืนได้ (`AddColumn` เป็น `int NULL` — สถานะก่อนหน้าคือ nullable)
   **แต่ค่าเดิมไม่กลับมา และห้ามเดาค่าใส่กลับ** (ห้าม default `0` ห้าม copy ค่าจาก `Company`)
   · ต้องเขียนคอมเมนต์ว่า rollback ใบนี้ **กู้ข้อมูลไม่ได้** — เป็นการกู้ *รูปร่าง* ไม่ใช่กู้ *ข้อมูล*
4. **ลำดับ deploy**: ใบนี้ต้องรัน **หลัง** โค้ดที่เลิกอ่าน/เขียนสามคอลัมน์นี้ถูก deploy แล้ว
   (หรือ deploy พร้อมกันในรอบเดียว) — ถ้าโค้ดเก่าที่ยัง `SELECT` คอลัมน์เหล่านี้ยังวิ่งอยู่
   หลังคอลัมน์ถูกลบ ทุก query ของ `LessonConfig` จะพังทั้งตาราง ไม่ใช่แค่ฟีเจอร์ pacing
5. `devops` ต้องมี **backup ของตาราง `LessonConfig` ก่อนรันใบนี้** ตามขั้นตอนปกติของ migration
   ที่ทำลายข้อมูล — **ไม่ใช่เพื่อจะเอาค่ากลับเข้า schema** (N2 ตัดทิ้งแล้ว) แต่เพื่อให้ยังตอบได้ว่า
   "ค่าเดิมของบทเรียนนี้คืออะไร" ถ้ามีคนถามย้อนหลัง → R-16

**ข้อบังคับของใบ `AddCompanyLessonPacingDefaults` (ยังมีผลบังคับตามเดิมสำหรับส่วน `Company`):**

1. **ค่าที่ backfill ลง `Company` ใช้ literal `5000` / `500` / `5000`** ซึ่งตรงกับ
   `TutorConfig.DefaultIntroWaitMs`/`DefaultBreathPauseMs`/`DefaultFinalQuestionWaitMs`
   (`ServerDefaults.cs:6-8`) — **ห้ามให้ migration ไปอ่าน environment variable** เพราะ
   `dotnet ef database update` รันคนละบริบทกับแอป ค่าที่ได้จะขึ้นกับว่ารันจากเครื่องไหน = ไม่ deterministic
2. **ผลตามมาจากข้อ 1 ที่ `devops` ต้องเช็คก่อน deploy**: ถ้า environment ปลายทางตั้ง
   `DEFAULT_INTRO_WAIT_MS` / `DEFAULT_BREATH_PAUSE_MS` / `DEFAULT_FINAL_QUESTION_WAIT_MS`
   ไว้ต่างจาก literal ข้างบน **แถวบริษัทที่ backfill จะไม่ตรงกับพฤติกรรมเดิมของระบบนั้น**
   → ต้อง `UPDATE "Company" SET ...` ตามค่า env ของ environment นั้นหลัง migrate ทันที
   (บันทึกลง `deploy.md` เป็นขั้นตอนใน runbook ไม่ใช่ความจำของคน)
3. ~~**rollback ไม่สมมาตร**: เมื่อมีบทเรียนที่ตั้งค่าเป็น `null` (inherit) เกิดขึ้นแล้ว การย้อน
   `LessonConfig` กลับเป็น `NOT NULL` **จะล้มทันที** ต้อง backfill ค่าจากบริษัทกลับลงบทเรียนก่อน~~
   → **หมดความหมายตั้งแต่รอบที่ 7**: หลัง `RemoveLessonConfigPacingOverrides` รัน คอลัมน์
   ที่ down migration ของใบนี้จะย้อนไม่มีอยู่แล้ว · **การย้อนใบนี้ต้องย้อนใบใหม่ก่อนเสมอ**
   (ลำดับปกติของ EF อยู่แล้ว แต่เขียนไว้เพราะ down ของใบนี้มี `UPDATE` ที่อ่านคอลัมน์ทั้งสองฝั่ง)
   · down migration ที่เขียนไว้แล้วในไฟล์จริงยังถูกต้องสำหรับสถานะ "ก่อนใบใหม่รัน" ไม่ต้องแก้
4. ใบนี้ **แยกจาก `BackfillMissingDefaultCategoryChain` (Module A)** และจาก migration ของ
   `knowledge-base`/`learning-session` ตามแบบแผน "หนึ่ง phase หนึ่ง migration ของเจ้าของ phase"

### Module U (F5 · จัดการบัญชีผู้ใช้รายอื่น) — **ไม่มี schema change เลยแม้แต่ฟิลด์เดียว** ✅ ยืนยัน 2026-08-25

ตรวจ `AdminUser` entity จริงเมื่อ 2026-08-25 (`backend/src/SupportRoom.Domain/Entities/AdminUser.cs`)
— ทุกฟิลด์ที่ F5 ต้องใช้ **มีอยู่แล้วครบ** ตรงกับที่ `requirement.md` F5.3 สรุปไว้:

| ฟิลด์ที่ Module U ใช้ | สถานะในโค้ดวันนี้ | ใช้ทำอะไรใน Module U |
|---|---|---|
| `Email` (`required string`, unique ทั้งระบบ) | มีอยู่แล้ว (`AdminUser.cs:36`) | AU-6 |
| `PasswordHash` (`string?` — null ได้โดยเจตนา) | มีอยู่แล้ว (`AdminUser.cs:43`) | AU-7 · **ความ nullable ของมันคือที่มาของ AU-9** |
| `MustChangePassword` (`required bool`) | มีอยู่แล้ว (`AdminUser.cs:55`) | AU-8 |
| `Role` (`required string`) | มีอยู่แล้ว | AU-3 ข้อ 3 (กฎเดิม) |
| `IsActive` (`required bool`) | มีอยู่แล้ว | เปิด/ปิดบัญชีในโมดัล (F5.2.6) |
| `UpdateBy` / `UpdateDate` | มีอยู่แล้ว | AU-10 (แทน audit log ตาม F5.2.5) |

**การตัดสินแบบ amend-mode ตาม `conventions.md` §4 (โมดูลนี้มีข้อมูลจริงอยู่แล้ว)**:
**ไม่ใช่ทั้ง additive และไม่ใช่ breaking — เป็น "ไม่มี schema change"** · ไม่มี EF Core migration
ใบใหม่ ไม่มี `ApplicationDbContext` mapping เปลี่ยน ไม่มี snapshot เปลี่ยน · **ถ้ามีใครสร้าง
migration ให้ Module U = ผิด contract ตีกลับ** (AU-16)

ข้อเดียวที่จะทำให้ต้องมี migration คือ audit log ซึ่งถูกปฏิเสธไปแล้วที่ F5.2.5 · สิ่งที่เปลี่ยน
ทั้งหมดอยู่ที่ชั้น **DTO + service + guard + UI** เท่านั้น — รูปร่างเต็มอยู่ที่
`## Admin User Management Rules` AU-2

> **ถึง `qa-engineer`**: การเทียบ contract ของ Module U คือการยืนยันว่า `AdminUser`
> **ยังเหมือนเดิมทุกฟิลด์** ตามตารางข้างบน · **เจอฟิลด์ใหม่บน `AdminUser` = drift** ตีกลับ
> `system-analyst` ทันที ไม่ใช่รับเป็น baseline ใหม่

### Module B (F2 ส่วนที่เหลือ) — ยังไม่มี Data Model

ลิงก์หมดอายุ / TTS voice+rate / แบรนด์ **ยังพักไว้** · **รูปร่างเคาะแล้ว** (คอลัมน์ nullable บน
`Company` ตาม B4) แต่ **ความหมายยังไม่เคาะ** (A2/A3/A4/B3b) → **ห้าม implement คอลัมน์เหล่านี้ล่วงหน้า**
ร่างที่อยู่ใน `## Unresolved Open Questions` เป็นภาพประกอบคำถาม **ไม่ใช่ contract**

### หมายเหตุถึง `qa-engineer`

โมดูลนี้ **ไม่ประกาศตารางใหม่เลยสักตาราง** — ทุก entity ที่อ้างถึงเป็นของโมดูลอื่น
(`KnowledgeCategory` และ `LessonConfig` เป็นของ `knowledge-base`) หรือของ baseline เดิม
(`Company`, `AdminUser`) ตาม `conventions.md` §7 การเทียบจึงเป็นการยืนยันว่า **entity เหล่านั้น
ตรงกับที่ประกาศไว้ข้างบนทุกฟิลด์** ไม่ใช่การหาตารางใหม่

- **Module A**: `Company`/`AdminUser`/`KnowledgeCategory` ต้องเหมือนเดิมทุกฟิลด์ ถ้ามีฟิลด์เพิ่ม
  นอกเหนือจาก DM-P1 = **drift** ตีกลับมาที่ `system-analyst`
- **Module P** (แก้ 2026-08-22 รอบที่ 7): `Company` ต้องมี `DefaultIntroWaitMs`/`DefaultBreathPauseMs`/
  `DefaultFinalQuestionWaitMs` เป็น `int` **NOT NULL** (ถ้าเจอเป็น nullable = ❌ ผิด contract
  ข้อสำคัญที่สุดของ Module P — **ข้อนี้ไม่เปลี่ยน**) และ **`LessonConfig` ต้อง _ไม่มี_ สามฟิลด์นี้
  เลย** ทั้งใน entity, `ApplicationDbContext` mapping, snapshot และตารางจริง
  · **เจอเป็น `int?` = ❌** (นั่นคือสถานะ Phase 4 เดิมที่ contract เพิ่งกลับทิศ ไม่ใช่สถานะที่ถูกต้อง)
  · **เจอเป็น `required int` = ❌** เช่นกัน (สถานะก่อน Phase 4)
- **`LessonConfig` ปรากฏในสอง `design.md`** (ที่นี่ DM-P2 และ `knowledge-base` DM-2) — ระหว่างที่
  D-3 ยังไม่ถูกปิด **รอบ QA ของ `knowledge-base` จะเห็นว่าสามฟิลด์นี้หายไปจากโค้ด ทั้งที่ DM-2
  เขียนไว้ว่าเป็น `required int`** นั่นคือ **ช่องว่างของเอกสาร ไม่ใช่ drift ของโค้ด** — รายงานเป็น
  open issue ที่ route มา `system-analyst` อย่าตีเป็นความผิดของ engineer และอย่าแก้โค้ดกลับ
  · **คำตอบที่ถูกต้องของ D-3 ตอนนี้คือ "ลบสามฟิลด์นี้ออกจาก DM-2" ไม่ใช่ "แก้เป็น nullable"**
  (ทิศทางที่เคยบันทึกไว้ในรอบที่ 4 ล้าสมัยแล้ว)

## Company Provisioning Rules

> **contract — `backend-engineer`/`frontend-engineer` ทำตามนี้ตรงตัว** ทุกข้อเคาะกับเจ้าของโปรเจกต์
> แล้วเมื่อ 2026-08-21 · ถ้าเจอเคสที่ contract นี้ไม่ครอบ **ห้ามเดา ให้ตีกลับมาที่ `system-analyst`**

**CP-1 · สิทธิ์ (F1.1)** — `guard.EnsureOwner()` เป็น **บรรทัดแรก** ของ service ก่อน validate อะไรทั้งสิ้น
`admin`/`cs` ได้ 403 พร้อมข้อความเดิมของ guard ("เฉพาะผู้ดูแลระบบเท่านั้นที่ทำรายการนี้ได้")
· การซ่อนปุ่มใน UI เป็นเรื่องของ UX **ไม่นับเป็นการป้องกัน** ต้องปฏิเสธที่ server เสมอ

**CP-2 · `CreateCompanyDto` (ขยายของเดิม — ฟิลด์เดิม 2 ตัวไม่เปลี่ยน)**

```csharp
public sealed class CreateCompanyDto
{
    [Required(ErrorMessage = "กรุณากรอกรหัสบริษัท")]
    public required string Id { get; init; }

    [Required(ErrorMessage = "กรุณากรอกชื่อบริษัท")]
    [MaxLength(200, ErrorMessage = "ชื่อบริษัทยาวเกินไป")]
    public required string Name { get; init; }

    // ── ใหม่ทั้ง 3 ตัว: admin คนแรกของบริษัทนี้ (F1.2) ──
    // annotation ชุดนี้คัดลอกมาจาก CreateAdminUserDto ตรงตัวโดยเจตนา ให้ข้อความ validation
    // ที่ผู้ใช้เห็นเหมือนกันทั้งสองหน้า
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string AdminEmail { get; init; }

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(100, ErrorMessage = "ชื่อผู้ใช้ยาวเกินไป")]
    public required string AdminDisplayName { get; init; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านเริ่มต้น")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.TooShortTh)]
    public required string AdminInitialPassword { get; init; }
}
```

**ไม่มีฟิลด์ `Role`** — ดู CP-8 · **ไม่มีฟิลด์ `CompanyId`** — บริษัทคือตัวที่กำลังสร้าง

**CP-3 · ลำดับการ validate — ต้องเรียงตามนี้เป๊ะ** เพื่อให้ข้อความ error ตรงกับสิ่งที่ผู้ใช้ต้องแก้ก่อน
1. `guard.EnsureOwner()`
2. normalize slug: `input.Id.Trim().ToLowerInvariant()` (เหมือนโค้ดวันนี้ `ICompanyService.cs:61`)
3. `CompanySlug.IsValid(id)` ไม่ผ่าน → `ValidationError(CompanySlug.RuleTh)`
4. slug ซ้ำ → CP-4
5. อีเมลซ้ำ → CP-5

**CP-4 · slug ซ้ำ — สองข้อความแยกกัน (N1)**

`_companies.Get(id)` เจอแถว:
- `IsActive == true` → `ValidationError("รหัสบริษัทนี้ถูกใช้งานแล้ว")` *(ข้อความเดิม ไม่เปลี่ยน)*
- `IsActive == false` → `ValidationError("มีบริษัทรหัสนี้อยู่แล้วแต่ถูกปิดใช้งาน หากต้องการใช้งานอีกครั้ง ให้เปิดกลับจากหน้ารายการบริษัท ไม่ใช่สร้างใหม่")`

**ห้ามเปิดบริษัทเดิมกลับให้อัตโนมัติ และห้ามเขียนทับแถวเดิมไม่ว่ากรณีใด** — ข้อมูลเดิมทั้งหมด
(บทเรียน/เอกสาร/Q&A/ประวัติการเรียน) จะกลับมาพร้อมกันโดยที่คนกดไม่ได้ตั้งใจ

**CP-5 · อีเมลซ้ำ (F1.5)** — `_users.GetByEmail(email)` (case-insensitive อยู่แล้ว
`IAdminUserRepository.cs:38-39`) เจอ → `ValidationError("อีเมลนี้ถูกใช้งานแล้ว")`

**ข้อความต้องเป็นประโยคนี้เป๊ะและห้ามบอกว่าอีเมลนั้นอยู่บริษัทไหนหรือมี role อะไร** — เป็นข้อความ
เดียวกับ `IAdminUserService.cs:68` เพื่อไม่ให้กลายเป็นเครื่องมือ enumerate บัญชีข้ามลูกค้า
· เก็บอีเมลด้วย `.Trim()` **ไม่ต้อง lowercase ตอนเก็บ** (ให้ตรงกับ `IAdminUserService.cs:65,76`
ที่เก็บตามที่กรอกแต่ค้นแบบ case-insensitive)

**CP-6 · ทรานแซกชันเดียว (F1.4) — กฎที่สำคัญที่สุดของ contract นี้**

- **ห้ามเรียก `IAdminUserService.Create`** และห้ามเรียก service ใดก็ตามที่ `Commit()` ในตัวเอง —
  `IAdminUserService.cs:87` commit ทันที ซึ่งจะทำให้เกิด 2 ทรานแซกชันและเหลือ "บริษัทครึ่งๆ" ได้จริง
  ถ้าขั้นถัดไปล้ม
- ลำดับที่ต้องเป็น: สร้าง entity ทั้งหมดในหน่วยความจำ → `_companies.Add(company)` →
  `_users.Add(adminUser)` → stage default chain (CP-9) → **`UnitOfWork.Commit()` ครั้งเดียวที่ท้ายสุด**
- **ต้องมี `Commit()` ปรากฏใน code path นี้เพียง 1 ครั้งเท่านั้น** — เป็นเงื่อนไขที่ `qa-engineer`
  ตรวจด้วยการอ่านโค้ดได้โดยตรง
- exception ก่อน `Commit()` = ไม่มีอะไรถูกเขียนลง database เลย (EF ยังไม่ `SaveChanges`)
  → ไม่ต้องเขียน rollback เอง และ **ห้ามใส่ try/catch ที่กลืน exception แล้วเดินต่อ**

**CP-7 · ค่าของแถว `Company`** — `Id` = slug ที่ normalize แล้ว · `Name` = `input.Name.Trim()` ·
`IsActive = true` · `CreateBy = currentUser.UserId` · `CreateDate = DateTime.UtcNow`

**CP-8 · ค่าของแถว `AdminUser` (admin คนแรก)**

| field | ค่า |
|---|---|
| `Id` | `IdGenerator.GenerateId("user")` |
| `CompanyId` | slug ของบริษัทที่เพิ่งสร้าง |
| `Role` | **`AdminRole.Admin` ตายตัว — ห้ามรับจาก request ไม่ว่าทางตรงหรือทางอ้อม** |
| `Email` | `input.AdminEmail.Trim()` |
| `DisplayName` | `input.AdminDisplayName.Trim()` (N2) |
| `IsActive` | `true` |
| `MustChangePassword` | **`true` ตายตัว (A1)** |
| `PasswordHash` | `passwordHasher.HashPassword(user, input.AdminInitialPassword)` |
| `CreateBy` / `CreateDate` | `currentUser.UserId` / `DateTime.UtcNow` |

`Role` ตายตัวเป็น `admin` เพราะ endpoint นี้มีหน้าที่เดียวคือสร้าง **admin คนแรกของบริษัทลูกค้า**
— ถ้ารับ role จาก request จะกลายเป็นช่องสร้าง `owner` ใบใหม่ที่มองไม่เห็นในหน้า `/admin/users`

**CP-9 · default category chain** — เรียก `CreateDefaultChain(companyId)` ตาม `## Default Category
Chain Rules` (CH-1) โดย **stage อย่างเดียว ห้าม commit ข้างใน** ให้ CP-6 เป็นคน commit ครั้งเดียว

**CP-10 · response** — `201 Created` + `{ company: CompanyViewModel }` **คง shape เดิมไว้ทุกประการ**
(`CompanyViewModel` มี `Id`/`Name`/`IsActive` ครบแล้ว ไม่ต้องเพิ่มฟิลด์)

**ห้ามคืนรหัสผ่านกลับมาใน response ไม่ว่ารูปแบบใด** — frontend มีค่าที่ตัวเองเพิ่งกรอกอยู่แล้ว
ถ้าจะแสดงหน้าจอสรุป "แจ้งลูกค้าด้วยข้อมูลนี้" ให้ใช้ค่าจาก form state ของตัวเอง ไม่ใช่จาก response

**CP-11 · log** — `Logger.LogInformation("Company created: {CompanyId} admin={AdminUserId} by={ActorId}", ...)`

**ห้าม log อีเมล และห้าม log รหัสผ่านหรือ hash ของมัน** ไม่ว่าที่ระดับ log ใด

**CP-12 · กับดัก query filter (F-5) — ข้อห้ามที่ต้องอ่านก่อนเขียนโค้ด**

**หลัง `Commit()` ห้ามอ่าน `KnowledgeCategory` กลับมาตรวจภายในคำขอเดียวกัน** — owner ที่กำลังสร้าง
บริษัทมักไม่มี `?company=` ในคำขอ ทำให้ `ICompanyContext.CompanyId` เป็น `null`
(`CurrentUserMiddleware.cs:72-81` — owner ที่ไม่ระบุ `?company=` ได้ `null` โดยเจตนา) แล้ว query filter
`CompanyId == companyContext.CompanyId` (`ApplicationDbContext.cs:136`) จะแมตช์ **0 แถวเสมอ**
· ถ้า owner มี `?company=บริษัทอื่น` ติดมา ก็จะเห็นหมวดของบริษัทนั้นแทน ไม่ใช่ของใหม่

**ห้ามแก้อาการนี้ด้วย `IgnoreQueryFilters()`** — เป็นวิธีที่เคยทำให้เกิด data leak จริงในโปรเจกต์นี้
มาแล้ว เพราะมันทิ้งเงื่อนไข `CompanyId` ไปพร้อมกับ `IsDelete` (`CompanyIsolationTests.cs:211-214`)

ถ้าต้องพิสูจน์ว่า chain ถูกสร้างจริง ให้พิสูจน์ใน unit test ที่ `Resolve()` company context
ไปยังบริษัทใหม่ก่อน **ไม่ใช่ในโค้ด production**

**CP-13 · หน้ารายการบริษัท + endpoint ใหม่ (F1.6 / F-1)**

- **เพิ่ม `GET /api/companies/all`** — owner เท่านั้น (`guard.EnsureOwner()`) คืน **ทุกบริษัทรวมที่
  `IsActive = false`** เรียงตาม `Name`
- เพิ่ม `IQueryable<Company> GetAllIncludingInactive()` ใน `ICompanyRepository`
- ใช้ `CompanyViewModel` เดิม (มี `IsActive` อยู่แล้ว) **ไม่ต้องเพิ่มฟิลด์ใด**
- **ห้ามแก้ `GET /api/companies` เดิม** — company switcher พึ่งพฤติกรรม "active เท่านั้น" อยู่
  (`ICompanyService.cs:39-55`) การเผลอใส่บริษัทที่ปิดแล้วเข้าไปจะทำให้สลับเข้าไปยังบริษัทที่ปิดได้

**CP-14 · ปิด/เปิดใช้งานบริษัท (F1.6) — ใช้ `PUT /api/companies/{id}` เดิมทั้งดุ้น ไม่แก้ service**

**B2 เคาะแล้ว: ไม่แตะเส้นทางฝั่งผู้เรียนเลยแม้แต่บรรทัดเดียว** — ลิงก์ที่แจกไปแล้วยังเรียนได้จน
หมดอายุตาม `TrainingLink.ExpiresAt` ที่เก็บไว้ตอนสร้าง นี่เป็น **การตัดสินใจที่ยืนยันแล้ว
ไม่ใช่สิ่งที่มองข้าม** (ดู R-8)

**แต่ UI ต้องพูดความจริงข้อนี้** — กล่องยืนยันก่อนกดปิดใช้งานต้องบอกทั้งสองผลลัพธ์:
*"พนักงานของบริษัทนี้จะเข้าสู่ระบบไม่ได้ทันที แต่ลิงก์เรียนที่แจกออกไปแล้วยังใช้งานได้จนกว่าจะหมดอายุ"*
— ห้ามเขียนข้อความที่ทำให้เข้าใจว่าการปิดตัดทุกอย่างทันที

**CP-15 · สิ่งที่ห้ามทำในโมดูลนี้**
- **ห้ามเพิ่ม query filter ให้ `Company` หรือ `AdminUser`** — จะทำให้ login และ company switcher พังทั้งระบบ (`ApplicationDbContext.cs:48-54`)
- **ห้ามแก้ role model** (`AdminRole`, `IAuthorizationGuard`)
  — ⚠️ **แก้ไขขอบเขตข้อนี้ 2026-08-25 (F5.2.1)**: ที่ห้ามคือ **ห้ามเปลี่ยนความหมายของของเดิม**
  (`AdminRole.RankOf`/`CanAssign`, พฤติกรรมของห้าเมธอดเดิมใน `IAuthorizationGuard`) ·
  **การ _เพิ่ม_ เมธอดใหม่ที่ไม่แตะพฤติกรรมเดิมเลยทำได้** เมื่อ requirement สั่งไว้ตรง ๆ —
  รอบนี้คือ peer-lockout ตาม F5.2.1 ซึ่ง `requirement.md` ระบุเองว่าต้องอยู่ที่ `IAuthorizationGuard`
  (เมธอดใหม่ = **AU-4**) · เหตุผลที่ยอมให้เพิ่มแต่ไม่ยอมให้แก้: การเพิ่มเมธอดใหม่ไม่มี call site
  เดิมเรียก จึงเป็นไปไม่ได้ที่จะทำ Module A/P ที่ verified ไปแล้วพัง ส่วนการแก้เมธอดเดิมกระทบ
  ทุกจุดในระบบพร้อมกันบนตารางที่ไม่มี query filter รอง (R-1)
- ~~**ห้ามสร้างหน้าจัดการผู้ใช้ใหม่** — เพิ่ม/ปิด/รีเซ็ตรหัส admin/cs รายอื่นใช้ `/admin/users` เดิม~~
  🔓 **แก้ 2026-08-25 — ครึ่งหลังของข้อนี้เป็นเท็จ และถูกยกเลิก** (ที่มา: `requirement.md` §F5,
  โดยเฉพาะ F5.0 · เป็น **การแก้ข้อเท็จจริงที่ผิด ไม่ใช่การเปลี่ยนใจของเจ้าของโปรเจกต์**)
  - **สิ่งที่ผิด**: ประโยค "รีเซ็ตรหัส admin/cs รายอื่นใช้ `/admin/users` เดิม" ไม่เคยเป็นความจริงเลย
    — `UpdateAdminUserDto` มีแค่ `DisplayName`/`Role`/`IsActive` (`AdminUserDto.cs:32-42`)
    **ไม่มี `Email` ไม่มีฟิลด์รหัสผ่าน** และทางเดียวที่เปลี่ยนรหัสผ่านได้ทั้งระบบคือ
    `POST /api/auth/change-password` ซึ่งเปลี่ยน**ของตัวเอง**และ**ต้องรู้รหัสเดิม**
    (`IAuthService.cs:194-222`) → วันนี้ cs ที่ลืมรหัสผ่านไม่มีใครในระบบช่วยได้เลย
  - **สิ่งที่ยังใช้ได้เหมือนเดิม**: **ห้ามสร้าง _หน้า_ จัดการผู้ใช้ใหม่** — ความสามารถชุดนี้ต้อง
    อยู่ใน `/admin/users` เดิม ในรูปโมดัลตาม F5.0/F5.1 **ห้ามเปิด route ใหม่** และห้ามทำหน้าที่สอง
    ที่ทำเรื่องเดียวกัน
  - contract ของงานนี้อยู่ที่ **`## Admin User Management Rules` (AU-1..AU-16)** และเป็น
    **Module U** ใน `## Modules`
- **ห้ามเพิ่มฟิลด์เผื่อแพ็กเกจ/โควตา/สัญญา/usage** ลงตารางใดก็ตาม (R-4)
- **ห้ามเพิ่มการตั้งค่าระดับบริษัท (F2) เข้ามาในรอบนี้** แม้จะดูเหมือนทำพร้อมกันง่ายกว่า
  — ⚠️ **แก้ไขขอบเขตข้อนี้ 2026-08-22**: ข้อห้ามนี้ยังใช้กับ **ลิงก์หมดอายุ / TTS / แบรนด์** ตามเดิม
  แต่ **ไม่ใช้กับค่า pacing 3 ตัว** ซึ่งถูกแยกออกมาเป็น **Module P** และอยู่ในสโคปแล้ว (P3) ·
  ค่า pacing มี contract ของตัวเองที่ `## Lesson Pacing Resolution Rules`

**CP-16 · ทุกเส้นทางที่สร้างแถว `Company` ต้องตั้งค่า pacing ในคำสั่งเดียวกัน (P5)**

`Company` มีคอลัมน์ `Default*Ms` แบบ `NOT NULL` (DM-P1) → **ทุกจุดที่ `new Company` ต้องกรอกครบ
ไม่งั้นคอมไพล์ไม่ผ่านตั้งแต่แรก** ซึ่งเป็นสิ่งที่ตั้งใจ · จุดที่มีอยู่จริงวันนี้มี **สองจุด ไม่ใช่จุดเดียว**:

1. `ICompanyService.Create` (`ICompanyService.cs:105`) — เส้นทางที่ owner สร้างบริษัทใหม่ (CP-2..CP-9)
2. `ICompanyService.SeedFirstCompanyIfEmpty` (`ICompanyService.cs:172`) — เส้นทาง auto-seed
   บริษัทแรกตอนระบบว่าง · **จุดนี้ลืมง่ายที่สุดเพราะไม่ได้อยู่ในฟอร์มใดๆ**

ทั้งสองจุดใช้ `ServerDefaults.GetLessonTimingDefaults()` และยังคง commit **ครั้งเดียว** ตาม CP-6
(การตั้งค่า pacing เป็นการเซ็ต property บน entity ที่ stage ไว้แล้ว ไม่ใช่ transaction เพิ่ม)
— รายละเอียดเต็มอยู่ที่ **LP-2**

## Default Category Chain Rules

> **contract** · invariant ในส่วนนี้เป็นของโมดูล `knowledge-base` — โมดูลนี้มีหน้าที่ **รักษาไว้
> ไม่ใช่นิยามใหม่** ห้ามแก้ `GetSystemDefault()` หรือกติกาใดของโมดูลนั้นเพื่อให้โค้ดฝั่งนี้ผ่าน

**CH-1 · ต้องมี code path ที่ runtime เรียกได้ — วันนี้ไม่มีเลย (P3 / F-3)**

เพิ่มเมธอด **`void CreateDefaultChain(string companyId)`** ลงใน `IKnowledgeCategoryService`
(`SupportRoom.Application/Services/IKnowledgeCategoryService.cs`)

**นี่เป็นการแตะ service ของโมดูล `knowledge-base` แบบ add-only** — เพิ่มเมธอดใหม่ ไม่แก้เมธอดเดิม
ไม่แก้ signature เดิม ไม่แก้ entity · วางไว้ที่นี่เพราะ **รูปร่างของ chain กับ invariant ที่มันต้อง
รักษาอยู่ไฟล์เดียวกัน** ถ้าแยกไปไว้ฝั่ง company-admin จะมีนิยาม chain สองชุดที่ drift จากกันได้
(เป็นแบบแผนเดียวกับที่ `knowledge-base` เคยเพิ่ม index ให้ `SessionQuestion` ของ `learning-session` — R-9)

**CH-2 · รูปร่างของ chain — สองแถวเป๊ะ ต้องตรงกับที่ migration เดิมสร้างไว้**
(`AddKnowledgeTaxonomyAndScope.cs:47-49`)

| field | parent | leaf |
|---|---|---|
| `Id` | `IdGenerator.GenerateId("kbcat")` | `IdGenerator.GenerateId("kbcat")` |
| `CompanyId` | `companyId` ที่รับเข้ามา | เหมือนกัน |
| `ParentId` | `null` | **`Id` ของ parent** |
| `Level` | `1` | `2` |
| `Name` | `"ยังไม่จัดหมวด"` | `"ยังไม่จัดหมวด"` |
| `Description` | `null` | `null` |
| `SortOrder` | `9999` | `9999` |
| `IsSystemDefault` | **`true`** | **`true`** |
| `IsDelete` | `false` | `false` |
| `CreateDate` | `DateTime.UtcNow` | เหมือนกัน |
| `CreateBy` | `currentUser.UserId` (null ได้ถ้าเรียกจาก context ที่ไม่มีผู้ใช้) | เหมือนกัน |

**`Id` ใช้ `IdGenerator.GenerateId("kbcat")` ไม่ใช่ `'kbcat-backfill-parent-' || md5(CompanyId)`
แบบใน migration** — รูปแบบ md5 มีไว้เพื่อให้ SQL backfill idempotent ผ่าน `ON CONFLICT` เท่านั้น
ไม่ใช่แบบแผนของโค้ด runtime (ทุก entity ในระบบใช้ `IdGenerator`)

**CH-3 · invariant ที่ห้ามพังเด็ดขาด**

> **หนึ่งบริษัทต้องมีแถวที่ `IsSystemDefault && Level == 2` เพียงแถวเดียวเป๊ะ**

`GetSystemDefault()` (`IKnowledgeCategoryRepository.cs:27-28`) ใช้ `SingleOrDefault()` —
สองแถวเมื่อไหร่ **throw `InvalidOperationException` ทันที** ซึ่งแย่กว่าอาการเดิม (คืน null) เพราะ
เปลี่ยนจาก "สร้างบทเรียนไม่ได้" เป็น "ทั้งบริษัทพัง" · ทุกกฎที่เหลือในส่วนนี้มีไว้เพื่อกันข้อนี้ข้อเดียว

**CH-4 · `CreateDefaultChain` ต้อง stage อย่างเดียว ห้าม commit และห้ามอ่าน**

- เรียก `_repository.Add(parent)` และ `_repository.Add(leaf)` แล้ว **return ทันที**
- **ห้าม `UnitOfWork.Commit()` ข้างใน** — ผู้เรียกเป็นคน commit (CP-6/CH-7)
- **ห้าม query `KnowledgeCategory` เพื่อเช็คก่อนสร้าง** ในเมธอดนี้ ด้วยเหตุผลของ CP-12
  (query filter จะแมตช์ 0 แถวเสมอในบริบทที่เมธอดนี้ถูกเรียก การเช็คจึงไม่มีความหมายและ
  ให้ความมั่นใจผิดๆ)
- **ความ idempotent มาจากผู้เรียก ไม่ใช่จากเมธอดนี้** — ดู CH-5

**CH-5 · ทำไมไม่ต้องเช็คซ้ำตอนสร้างบริษัทใหม่**

บริษัทที่เพิ่งผ่าน CP-4 มาได้แปลว่า **ยังไม่เคยมีแถวไหนในระบบอ้างถึง `CompanyId` นี้เลย** เพราะ slug
ยังไม่เคยถูกใช้ → เป็นไปไม่ได้ที่จะมี `KnowledgeCategory` ค้างอยู่ก่อน → สร้างสองแถวได้เลยโดยไม่ต้อง
ตรวจ นี่คือเหตุผลที่ contract ไม่ต้องมีกติกา "ถ้ามี chain อยู่แล้วให้ทำอย่างไร" สำหรับเส้นทางนี้

**เส้นทางเดียวที่ chain อาจมีอยู่ก่อนคือบริษัทเดิม ซึ่งจัดการด้วย CH-6 ไม่ใช่เมธอดนี้**

**CH-6 · migration ซ่อมบริษัทเดิม `BackfillMissingDefaultCategoryChain` (B1)**

**data-only migration ไม่มี DDL แม้แต่บรรทัดเดียว** — `Up()` มีแต่ `migrationBuilder.Sql(...)`

ต้องครอบบริษัท **ทุกราย** ใน `"Company"` ที่ยังไม่มี leaf `IsSystemDefault && Level = 2`
— **ไม่ใช่แค่บริษัทที่มี `LessonConfig`/`DocumentResource`** ซึ่งเป็นช่องโหว่ของ migration เดิม (F-3)

กติกาของ SQL:
- ต้องเป็น `INSERT ... SELECT ... WHERE NOT EXISTS (...)` ที่เช็คต่อบริษัท **ห้ามใช้ `ON CONFLICT`
  แทนการเช็ค** เพราะ id ที่สร้างใหม่จะไม่มีวันชนกับ id เดิม (คนละรูปแบบ) `ON CONFLICT` จึงไม่กัน
  อะไรเลยและจะได้ leaf แถวที่สอง = CH-3 พัง
- บริษัทที่มี **parent อยู่แล้วแต่ไม่มี leaf** → เติมเฉพาะ leaf โดยให้ `ParentId` ชี้ไป parent
  ที่ `IsSystemDefault && Level = 1` ของบริษัทนั้น
- บริษัทที่มี **leaf อยู่แล้วแต่ไม่มี parent** → เติมเฉพาะ parent แล้ว `UPDATE` leaf ให้ `ParentId`
  ชี้ไป parent ใหม่
- บริษัทที่มี **leaf มากกว่า 1 แถวอยู่แล้ว** → **ห้าม migration เลือกลบทิ้งเองหรือเลือกเก็บอันใดอันหนึ่ง**
  เป็น data corruption ที่ต้องให้คนดู · ให้ `Down()`/`Up()` ปล่อยผ่านแถวนั้น แล้ว **รายงานเป็น
  open issue ให้ผู้ใช้ตัดสิน** (ระบบวันนี้ยังไม่เคยเกิดเคสนี้ — migration เดิมสร้างทีละคู่เสมอ)
- ค่าของแถวที่ insert ต้องตรง CH-2 ทุกช่อง (`CreateBy` เป็น `null` ได้ เพราะ migration ไม่มีผู้ใช้)

`Down()`: ลบเฉพาะแถวที่ migration นี้สร้าง — ถ้าแยกไม่ออกจากของเดิม ให้ `Down()` เป็น no-op
พร้อมคอมเมนต์อธิบายว่าทำไม (ลบผิดแถว = ทำลาย chain ของบริษัทที่ใช้งานอยู่)

**CH-7 · จุดเรียกทั้งหมด มีสองจุดเท่านั้น** — (1) `ICompanyService.Create` ตาม CP-9
(2) migration CH-6 · **ห้ามเพิ่มจุดที่สาม** โดยเฉพาะห้ามเรียกแบบ lazy ตอนเปิดหน้าหมวดหรือ
ตอนสร้างบทเรียน เพราะจะกลายเป็นการสร้าง chain ในบริบทที่ company context อาจไม่ตรงกับบริษัท
ที่ตั้งใจ และเป็นเส้นทางที่ทำให้เกิด leaf ซ้ำได้ง่ายที่สุด

**CH-8 · สิ่งที่ห้ามแตะในโมดูล `knowledge-base`** — `GetSystemDefault()` · กติกา "ห้ามลบ/เปลี่ยนชื่อ/
ย้ายชั้นแถว `IsSystemDefault`" · `KnowledgeCategory` entity · query filter ของมัน
ถ้าดูเหมือนต้องแก้อะไรในนั้นเพื่อให้ F1.3 ทำงานได้ **แปลว่าออกแบบผิด ให้ตีกลับมาที่ `system-analyst`**

## Lesson Pacing Resolution Rules

> **contract — `backend-engineer`/`frontend-engineer` ทำตามนี้ตรงตัว** · ฐานของทุกข้อคือคำตอบ
> **P2 · P3 · P5** (ยังมีผลบังคับ) บวก **N1 · N2 · N3** ที่เจ้าของโปรเจกต์ยืนยันเมื่อ
> **2026-08-22 รอบที่สอง** (ดู `learning-session/requirement.md` §"🔄 กลับคำตอบ P1") บวกมติ B4
> · **P1 และ P4 ถูกกลับคำตอบแล้ว — ห้ามอ้างอิง**
> · **ถ้าเจอเคสที่ contract นี้ไม่ครอบ ห้ามเดา ให้ตีกลับมาที่ `system-analyst`**
>
> ค่าที่พูดถึงในส่วนนี้คือสามตัวนี้เท่านั้น: `introWaitMs` · `breathPauseMs` · `finalQuestionWaitMs`
> · **`videoDurationMs` ไม่เกี่ยวกับ contract นี้เลย** (เป็นค่าระดับสไลด์ อยู่ใน `SlideConfig` ตามเดิม
> ไม่แตะ ไม่ย้าย ไม่ resolve)
>
> ### 🔄 สถานะรายข้อหลังการกลับคำตอบ (2026-08-22 รอบที่ 7) — อ่านตารางนี้ก่อนอ่านข้อไหนก็ตาม
>
> | ข้อ | สถานะ |
> |---|---|
> | **LP-1 · LP-4 · LP-5 · LP-7 · LP-12 · LP-13 · LP-14 · LP-15** | **เขียนใหม่** — ใช้ข้อความปัจจุบันในไฟล์นี้เท่านั้น |
> | **LP-3 · LP-6 · LP-11** | ❌ **ยกเลิกทั้งข้อ** — เก็บหมายเลขไว้พร้อมเหตุผล เพราะ `plan.md`/`review.md`/`status.md` อ้างเลขเหล่านี้อยู่ ไม่ใช่เพราะยังมีผล |
> | **LP-2 · LP-8 · LP-9 · LP-10** | ✅ **ไม่เปลี่ยน** (มีการแก้ *เหตุผลประกอบ* ของ LP-9 แต่กฎเหมือนเดิมทุกตัวอักษร) |

**LP-1 · โมเดลการสืบทอดเหลือชั้นเดียว (เขียนใหม่ 2026-08-22 · มติ N1)**

```
บริษัท   Company.DefaultIntroWaitMs (int NOT NULL)  ← แหล่งความจริงเดียว ใช้ตรง ๆ ทุกบทเรียน
```

**ไม่มีชั้นบทเรียนอยู่เหนือมัน และไม่มีชั้น env อยู่ใต้มัน:**

- **ไม่มี override ต่อบทเรียน** — `LessonConfig` ไม่มีคอลัมน์ pacing แล้ว (DM-P2) ฉะนั้น
  "การ resolve" ในความหมายเดิม (merge สองชั้น) **ไม่มีอยู่จริงอีกต่อไป** เหลือแค่ **การอ่านค่า
  จากแถว `Company` ของบทเรียนนั้น**
- **ไม่มีชั้น env ตอน runtime** — `ServerDefaults.GetLessonTimingDefaults()` ถูกเรียกที่
  **จุดเดียวในระบบ** คือตอนสร้างแถว `Company` (LP-2) · ถ้าเห็นโค้ดเรียกเมธอดนี้ตอนอ่าน/ตอนสอน
  **นั่นคือผิด contract** ไม่ว่าจะดูปลอดภัยแค่ไหน (มันจะกลายเป็นชั้นเงียบที่กลบค่าที่เจ้าของบริษัท
  ตั้งไว้โดยไม่มีใครเห็น)
- **ห้ามสร้างชั้นทดแทนในรูปแบบอื่น** — ไม่ใช่ `[NotMapped]` บน `LessonConfig`, ไม่ใช่ค่าใน
  `SlideConfigs` JSON, ไม่ใช่ query string ตอนเข้าห้อง, ไม่ใช่ค่าใน localStorage ฝั่ง frontend
  · ถ้ามีความต้องการ override จริงในอนาคต ต้องกลับมาที่ `system-analyst` (OQ-P7) ไม่ใช่แทรกเอง

**LP-2 · ตั้งค่าที่ชั้นบริษัทตอนสร้างเท่านั้น — ครบทั้งสองเส้นทาง (P5)**

ทั้ง `ICompanyService.Create` (`:105`) และ `ICompanyService.SeedFirstCompanyIfEmpty` (`:172`)
เซ็ตค่าจาก `ServerDefaults.GetLessonTimingDefaults()` ลงแถวใหม่ **ก่อน** `UnitOfWork.Commit()`
ครั้งเดียวเดิม (CP-6/CP-16) — ไม่เพิ่ม transaction ไม่เพิ่ม service call

**ห้าม copy ค่าจากบริษัทอื่น ห้ามรับค่าจาก request ตอนสร้างบริษัท** — ฟอร์มสร้างบริษัท (CP-2)
**ไม่มีช่อง pacing** และ `CreateCompanyDto` **ไม่มีฟิลด์ pacing** · การแก้ค่าทำผ่าน LP-9 หลังสร้างเสร็จ
(เหตุผล: ฟอร์มสร้างบริษัทเป็น endpoint ที่ `security` จับตาอยู่แล้ว การเพิ่ม field ที่ไม่จำเป็น
เข้าไปคือการขยาย attack surface ของ endpoint ที่แพงที่สุดในโมดูล โดยไม่ได้อะไรกลับมา)

**LP-3 · ❌ ยกเลิกทั้งข้อ (2026-08-22 · มติ N1/N3)**

~~ที่ชั้นบทเรียน `null` = สืบทอด · `0` = override เป็นศูนย์จริงๆ (P1/P4)~~

**กฎ empty-vs-zero ที่ชั้นบทเรียนหมดเหตุผลที่จะมีอยู่** เมื่อไม่มีคอลัมน์และไม่มีช่องกรอก
ที่ระดับบทเรียนอีกต่อไป — **ข้อนี้ไม่มีผลบังคับใด ๆ แล้ว ห้ามนำไป implement**

⚠️ **สิ่งที่ยังจริงอยู่ และย้ายไปอยู่ที่อื่นแล้ว ไม่ได้หายไปพร้อมข้อนี้:**
`0` **ยังเป็นค่าที่ถูกต้อง** ที่ชั้นบริษัท ("ไม่หยุดหายใจระหว่างสไลด์เลย" ตั้งได้จริง และ unit test
เดิมหลายตัวใช้ `0` อยู่) — กฎอยู่ที่ **LP-8** (ช่วงเริ่มที่ `0`) และ **SP-7** (ที่ชั้นบริษัท
"ว่าง" ไม่ถูกต้อง แต่ `0` ถูกต้อง) · **ข้อห้ามแพตเทิร์น `Number(x) || 0` ยังอยู่ครบที่ SP-7**

**LP-4 · จุดเดียวในระบบที่อ่านค่า pacing (เขียนใหม่ 2026-08-22 · มติ N1)**

**พฤติกรรมที่ต้องได้ (นี่คือสิ่งที่ contract บังคับ):** `LearnerLessonConfigViewModel` ที่ประกอบใน
`ILessonConfigService.GetTeachingContentByLinkAsync` ต้องได้ค่าสามตัวนี้ **จากแถว `Company`
ของบทเรียนนั้นตรง ๆ** (`company.DefaultIntroWaitMs` → `IntroWaitMs` และอีกสองตัวแบบเดียวกัน)
· ไม่มีการ merge ไม่มี `??` ไม่มีเงื่อนไข ไม่มีค่า default ระหว่างทาง

- **ยังเป็นจุดเดียวเหมือนเดิม** — จุดที่สองที่อ่านค่าเหล่านี้เพื่อส่งให้ผู้เรียนคือบั๊ก
- แถว `Company` ถูกโหลดอยู่แล้วที่ call site นี้ (`_companyRepository.Get(link.CompanyId)`
  พร้อม `throw GeneralException.NotFound("บริษัท")` ถ้าไม่เจอ) — **พฤติกรรมกรณีบริษัทหาย
  ไม่เปลี่ยน** ยังเป็น 404 เหมือนเดิม
- **ห้ามอ่าน/คำนวณค่านี้ที่ frontend** ไม่ว่าใน hook, reducer หรือ component

**ℹ️ เรื่อง `ILessonPacingResolver` (มีอยู่จริงในโค้ดแล้วจาก Phase 4) — เจตนาของ contract:**
ตัว interface นี้ถูกสร้างขึ้นเพื่อรองรับ **การ merge สองชั้น** ซึ่งตอนนี้ไม่มีแล้ว · contract นี้
บังคับแค่ **พฤติกรรม** ข้างบน — **จะลบ interface ทิ้งแล้วอ่าน `company.Default*Ms` ตรงจุดประกอบ
ViewModel หรือจะคง interface ไว้เป็น pass-through บาง ๆ เป็นการตัดสินใจของ `project-manager`/
engineer ไม่ใช่ของ `system-analyst`** · เงื่อนไขเดียวที่ห้ามละเมิดคือ: **ถ้าคงไว้ ห้ามให้มันรับ
`LessonConfig` เข้ามาเป็นพารามิเตอร์ในฐานะแหล่งค่า pacing อีก** (จะเป็นการเปิดช่องให้ชั้นที่สอง
กลับมาเงียบ ๆ) และ **ห้ามให้มันมี logic เชิงเงื่อนไขใด ๆ**

**LP-5 · ฝั่งผู้เรียนได้ค่าครบเสมอ · ฝั่งแอดมิน (ฟอร์มบทเรียน) ไม่เห็นค่านี้เลย (เขียนใหม่ 2026-08-22 · มติ N1)**

| ViewModel / DTO / type | ค่า pacing | เหตุผล |
|---|---|---|
| `LearnerLessonConfigViewModel` (`ILessonConfigService.cs:34-36`) | **`int` NOT NULL เหมือนเดิมทุกประการ ไม่แก้** | ค่ามาจาก `Company` ตรง ๆ (LP-4) · reducer ฝั่ง tutor รับ `number` ตามเดิม ไม่ต้องแก้ · **นี่คือส่วนเดียวของ LP-5 เดิมที่ไม่เปลี่ยน** |
| `LessonConfigViewModel` (`ViewModel/LessonConfigViewModel.cs`) | **ลบสามฟิลด์นี้ออก** (~~`int?`~~) | ฟอร์มบทเรียนไม่มีช่องนี้แล้ว (N1) การส่งค่าที่ไม่มีใครใช้ไปให้ฟอร์มคือการเชิญให้มีคนเอาไปแสดงอีกรอบ |
| `LessonConfigDto` (`Dto/LessonConfigDto.cs:34-36`) | **ลบสามฟิลด์นี้ออก** (~~`int?`~~) | ไม่มีอะไรให้รับจากฟอร์มแล้ว · การคงฟิลด์ไว้แบบ "รับแต่ไม่ใช้" คือ API ที่โกหกผู้เรียกใช้ |

**เคสที่ต้องตอบให้ครบ (ห้ามให้ engineer ตัดสินเอง):**

- **client เก่าที่ยัง POST/PUT สามฟิลด์นี้มา** → ฟิลด์ไม่มีใน DTO แล้ว ASP.NET Core จะ
  **เพิกเฉยโดยปริยาย ไม่ error** — **นี่คือพฤติกรรมที่ต้องการ** ห้ามเพิ่ม validation ให้ 400
  ("ส่งฟิลด์ที่ไม่รู้จัก") เพราะจะทำให้แท็บที่ค้างอยู่ของ CS เซฟไม่ได้โดยไม่มีเหตุผลที่เขาเข้าใจได้
- **`GET` ของบทเรียนที่ client เก่ายังอ่านสามฟิลด์นี้อยู่** → ได้ `undefined` ฝั่ง JS
  · ยอมรับได้เพราะเป็นช่วงสั้น ๆ ระหว่าง deploy และค่านี้ไม่มีผลกับการแสดงผลอื่นของฟอร์ม
- **ค่าที่บริษัทใช้อยู่ ไม่ต้องส่งไปให้ฟอร์มบทเรียนอีกต่อไป** — placeholder "ว่าง = ใช้ค่าบริษัท"
  ถูกยกเลิกพร้อม LP-11 · ฟอร์มบทเรียน **ห้ามเรียก `getCompanyLessonPacing()`** อีก
  (จะกลายเป็น request ที่ไม่มีใครใช้ผลลัพธ์)

**LP-6 · ❌ ยกเลิกทั้งข้อ (2026-08-22 · มติ N1)**

~~`SaveAsync` เขียนค่าดิบผ่านตรงๆ ไม่ตีความ~~

**สิ่งที่แทนที่ข้อนี้:** `ILessonConfigService.SaveAsync` **ต้องไม่แตะค่า pacing เลย** ทั้งตอนสร้าง
(`:161-163` เดิม) และตอนแก้ (`:186-188` เดิม) — บรรทัด assign ทั้งหกต้องหายไป ไม่ใช่เปลี่ยนเงื่อนไข
· entity ไม่มี property ให้ assign แล้ว (DM-P2) ฉะนั้นถ้าโค้ดยัง compile ผ่านโดยมีบรรทัดพวกนี้อยู่
แปลว่า DM-P2 ยังไม่ถูกทำครบ · **ห้ามเรียก `ServerDefaults.GetLessonTimingDefaults()` ที่นี่**
เหมือนเดิม (LP-1)

**LP-7 · การเปลี่ยนค่าที่ชั้นบริษัทมีผลกับ _ทุก_ บทเรียนของบริษัทนั้น — และไม่ย้อนหลังเข้าห้องที่กำลังเรียนอยู่ (แก้ 2026-08-22 · มติ N1)**

- **ทุกบทเรียนของบริษัทนั้นได้ค่าใหม่** ตั้งแต่ **การเข้าห้องครั้งถัดไป** เพราะค่าถูกอ่านตอน
  `GetTeachingContentByLinkAsync` (LP-4) · ไม่มีบทเรียนที่ "ไม่ได้รับผล" อีกต่อไป (ไม่มี override แล้ว)
  · ไม่มี cache ที่ต้อง invalidate ไม่มีงาน background
- **การเรียนที่เปิดค้างอยู่ไม่เปลี่ยนกลางคัน** — ค่าถูกส่งไปกับ payload ตอนเข้าห้องแล้ว
  นี่เป็นพฤติกรรมที่ยอมรับ ไม่ต้องทำอะไรเพิ่ม (ตรงแนวเดียวกับมติ A7 ของลิงก์หมดอายุ)

**LP-8 · ขอบเขตค่าที่รับได้ — ~~บังคับเท่ากันทั้งสองชั้น~~ ที่ชั้นบริษัทชั้นเดียว (A5 ส่วน pacing) · ✅ ตัวเลขไม่เปลี่ยน**

| ค่า | ช่วงที่รับ | เหตุผล |
|---|---|---|
| `introWaitMs` | `0`–`60000` | `0` = เริ่มสอนทันทีไม่รอ (ต้องรับ เพราะเป็นพฤติกรรมที่ตั้งได้จริงและ test เดิมใช้อยู่) · เกิน 1 นาทีคือห้องที่ดูเหมือนค้าง |
| `breathPauseMs` | `0`–`10000` | ช่วงหยุดหายใจระหว่างสไลด์ เกิน 10 วิ ผู้เรียนจะคิดว่าระบบพัง |
| `finalQuestionWaitMs` | `0`–`120000` | ช่วงเปิดให้ถามคำถามสุดท้ายก่อนปิดห้องอัตโนมัติ — ยาวได้กว่าเพื่อน แต่ต้องมีเพดานเพราะห้องที่ไม่ปิดคือ session ค้าง |

- บังคับด้วย data annotation บน DTO ตามแบบแผนเดิม (`[Range(0, 60000)]`) — **เหลือที่เดียวคือ
  DTO ของ `PUT /api/companies/{companyId}/lesson-pacing` (LP-9)** เพราะไม่มี DTO ฝั่งบทเรียนแล้ว
  (LP-5) · ที่ชั้นบริษัท `null` **ไม่ผ่าน** (ค่าต้องครบสามตัวเสมอ)
- **ข้อความ error เป็นภาษาไทย** ตาม convention เดิมของโปรเจกต์
- ✅ **ยืนยันแล้ว 2026-08-22 — เจ้าของโปรเจกต์เคาะใช้ตัวเลขสามแถวนี้ตามที่เสนอ** ("ใช้ค่าเดิมไปก่อน
  จูนทีหลังได้") → **เป็นมติแล้ว ไม่ใช่ข้อเสนออีกต่อไป ห้ามถามซ้ำและห้ามเดาค่าอื่น**
  · ถ้าวันหนึ่งจูนใหม่ ให้แก้ที่ตารางนี้ที่เดียวแล้วตามไปแก้ annotation + constant ฝั่ง client (SP-8)
  · **ไม่กระทบชนิดคอลัมน์และไม่ต้อง migrate** (เป็น validation ล้วน) — นี่คือเหตุผลที่การจูนทีหลังราคาถูก

**LP-9 · endpoint ของค่าระดับบริษัท — แยกจาก `PUT /api/companies/{id}` เดิม**

- **`GET /api/companies/{companyId}/lesson-pacing`** → คืนสามค่าปัจจุบันของบริษัทนั้น
  · สิทธิ์: `guard.EnsureCanAccessCompany(companyId)` (owner ทุกบริษัท · `admin`/`cs` เฉพาะของตัวเอง)
  · **`cs` อ่านได้โดยตั้งใจ** — ⚠️ **เหตุผลเปลี่ยนแล้ว (2026-08-22)**: ~~เพราะหน้าแก้บทเรียนของ CS
  ต้องแสดง placeholder ตาม LP-5~~ (ยกเลิกพร้อม LP-11) · **เหตุผลปัจจุบันคือหน้า `/admin/settings`
  ที่ `cs` เห็น section pacing แบบอ่านอย่างเดียว** (มติ A6 · SP-4 · SP-15) — **สิทธิ์ไม่เปลี่ยน
  แม้แต่ตัวเดียว ห้าม "เก็บกวาด" ด้วยการปิด `GET` ของ `cs` เพราะเหตุผลเดิมหายไป**
- **`PUT /api/companies/{companyId}/lesson-pacing`** → รับสามค่า **ครบทั้งสามตัว ห้าม partial
  และห้าม `null`** · สิทธิ์: `guard.EnsureCanAccessCompany(companyId)` **บวกการปฏิเสธ `cs` อย่างชัดแจ้ง**
  (owner + `admin` ของบริษัทนั้นเท่านั้น ตาม F2.3) — `cs` ได้ 403
- **ห้ามเพิ่มสามค่านี้เข้าไปใน `UpdateCompanyDto` ของ `PUT /api/companies/{id}` เดิม** — endpoint นั้น
  เป็น owner-only (CP-14) ซึ่งจะทำให้ `admin` ของบริษัทตัวเองแก้ค่าของตัวเองไม่ได้ และจะทำให้
  payload ของ endpoint ปิด/เปิดบริษัทกลายเป็นที่รวมทุกอย่าง
- เคสที่ต้องตอบให้ครบ: บริษัทไม่มีจริง → **404** ("บริษัท") · ค่านอกช่วง LP-8 → **400** ·
  **บริษัทที่ `IsActive = false` ยัง `GET`/`PUT` ได้ตามปกติ** (การแก้ค่าตั้งค่าไม่ใช่การเปิดบริษัทกลับ
  และ owner ต้องเตรียมค่าก่อนเปิดใช้ใหม่ได้)

**LP-10 · การอ่านค่าบริษัทข้ามบริษัท — จุดที่พังเงียบได้ (F-5/F-6)**

`Company` **ไม่มี query filter** จึงอ่านข้ามบริษัทได้ตรงๆ อยู่แล้ว — **นี่คือเหตุผลที่ B4 เลือกทาง (ก)**
· ข้อห้ามที่ตามมา: **ห้ามใช้ `IgnoreQueryFilters()` ที่ไหนก็ตามในงานนี้** (CP-12) และ
**ห้ามเพิ่ม query filter ให้ `Company`** (CP-15) · ส่วน `LessonConfig` **มี** query filter ตามเดิม
และต้องคงไว้ — การอ่านค่า pacing เกิดจากแถว `Company` ของลิงก์นั้นโดยตรง (LP-4) ไม่ได้ query
ข้ามบริษัทตรงไหนเลย (ข้อนี้ไม่เปลี่ยนจากรอบก่อน — เดิมเขียนว่า "การ resolve" ตอนที่ยังมีสองชั้น)

**LP-11 · ❌ ยกเลิกทั้งข้อ และแทนที่ด้วย "ฟอร์มบทเรียนต้องไม่มีช่อง pacing เลย" (2026-08-22 · มติ N1)**

~~ฝั่ง frontend — ช่องกรอกต้องแยก "ว่าง" ออกจาก "ศูนย์" ได้จริง · placeholder `ใช้ค่าบริษัท (N ms)`
· ฟอร์มสร้างใหม่เริ่มเป็นค่าว่างทั้งสามช่อง~~ — **กฎ empty-vs-zero ที่ฟอร์มบทเรียนหมดผลบังคับทั้งก้อน**

**สิ่งที่ต้องเป็นแทน (contract ใหม่ · นี่คือการ _ถอด_ ของที่ implement ไปแล้วใน Phase 4):**

| จุด | ต้องเป็น |
|---|---|
| `frontend/src/app/admin/lessons/[slug]/page.tsx` (หน้าแก้บทเรียน) | **ไม่มีช่องกรอก pacing ทั้งสามช่อง** — ลบ input, label, คำอธิบาย, placeholder, handler และ state ของทั้งสามค่าออกให้หมด · **ห้ามเหลือไว้แบบ `disabled`/`hidden`/comment out** |
| `frontend/src/app/admin/lessons/new/page.tsx` (ฟอร์มสร้างบทเรียน) | **payload ตอนสร้างไม่มีสามคีย์นี้เลย** — ไม่ใช่ `null` ไม่ใช่ `0` ไม่ใช่ `undefined` ที่ยังประกาศไว้ใน object · ค่าคงที่ `3000/800/5000` หายไปพร้อมกัน (ปิด LP-13 แถวแรกไปในตัว) |
| การเรียก `getCompanyLessonPacing()` จากหน้าฟอร์มบทเรียน (เพิ่มไว้ตอน Phase 4 เพื่อทำ placeholder) | **ลบออก** — ไม่มีอะไรใช้ผลลัพธ์แล้ว · ฟังก์ชันใน `api-client.ts` **ยังต้องอยู่** เพราะหน้า `/admin/settings` ใช้ (SP-11) |
| ข้อความอธิบายในฟอร์มบทเรียนที่บอกว่า "ปล่อยว่างเพื่อใช้ค่าบริษัท" | **ลบออก** ทุกจุด — ถ้าอยากบอก CS ว่าค่าจังหวะการสอนอยู่ที่ไหน ให้ชี้ไปที่หน้าตั้งค่าบริษัท ไม่ใช่บอกวิธีที่ใช้ไม่ได้แล้ว |

**ต้องไม่เกิด**: ช่องที่ยังอยู่แต่ส่งค่าไม่ถึง server (CS จะกรอกแล้วงงว่าทำไมไม่มีผล) — นี่คือ
failure mode ที่แย่ที่สุดของการถอดครึ่งทาง

**LP-12 · TypeScript types ที่ต้องแก้คู่กัน (Architecture Rule 7) — เขียนใหม่ 2026-08-22 · มติ N1/N3**

- `frontend/src/types/domain.ts` — **`LessonConfig` ต้องไม่มีสามฟิลด์นี้เลย**
  (~~`number | null`~~ ที่ Phase 4 เพิ่งแก้ไป **ต้องถูกลบทิ้ง** ให้ตรง `LessonConfigViewModel`
  ที่ไม่มีฟิลด์แล้วตาม LP-5)
- `frontend/src/types/domain.ts` — **`LearnerLessonConfig` ประกาศสามฟิลด์นี้เป็น `number` ตรง ๆ
  ต่อไปเหมือนเดิม** (ห้ามกลับไปใช้ `Pick<LessonConfig, ...>` — ตอนนี้ยิ่งเป็นไปไม่ได้เพราะ
  `LessonConfig` ไม่มีฟิลด์ให้ `Pick` แล้ว) · งานที่ Phase 4 ทำไว้ตรงนี้ **ยังถูกต้อง เก็บไว้**
- `frontend/src/types/domain.ts` — type `CompanyLessonPacing` (สามค่าเป็น `number`) **คงไว้**
  ใช้กับหน้า `/admin/settings`
- `frontend/src/tutor/tutor-reducer.ts:9` — **ไม่แก้** reducer ยังรับ `number` เท่านั้น (LP-4)

**LP-13 · ปิดชุดค่า default ที่เพี้ยนสองจุด (P2) — เป็นส่วนหนึ่งของงานนี้ ไม่ใช่ follow-up**

| จุด | วันนี้ (ก่อน Phase 4) | ต้องเป็น |
|---|---|---|
| `frontend/src/app/admin/lessons/new/page.tsx:27-29` | `3000 / 800 / 5000` | ~~ว่างทั้งสามช่อง~~ → **ไม่มีสามคีย์นี้ใน payload เลย** (LP-11 ใหม่) — ค่าคงที่ชุดนี้หายไปทั้งชุดเหมือนเดิม เพียงแต่หายไปแรงกว่าเดิม |
| `frontend/src/hooks/use-tutor-session.ts:45` | `5000 / 1000 / 5000` | `5000 / 500 / 5000` ให้ตรง `TutorConfig.Default*` — **ข้อนี้ไม่เปลี่ยน · Phase 4 แก้ไปแล้วและยังถูกต้อง เก็บไว้** |
| `backend` `ServerDefaults.cs:6-8` | `5000 / 500 / 5000` | **ไม่แก้ — นี่คือค่าที่ถูกต้อง (P2)** |

fallback ใน `use-tutor-session.ts` จะกลายเป็น **โค้ดที่ไม่มีทางถูกใช้** หลัง LP-4 (server ส่งค่า
ครบเสมอ) — **เก็บไว้ได้ในฐานะ safety net แต่ต้องถูกต้อง** ค่าที่ผิดในโค้ดที่ไม่เคยรัน
คือกับดักของคนอ่านคนถัดไป

**LP-14 · unit test ที่ต้องมี (ไม่ใช่ทางเลือก) — เขียนใหม่ 2026-08-22 · มติ N1**

1. ~~resolver: lesson มีค่า → ได้ค่าบทเรียน · lesson เป็น `null` → ได้ค่าบริษัท · lesson เป็น `0`
   → ได้ `0`~~ ❌ **ลบ test ชุดนี้ทิ้ง** — มันทดสอบพฤติกรรมที่ contract เพิ่งยกเลิก · **การปล่อยไว้
   แย่กว่าการลบ** เพราะ test สีเขียวที่ยืนยันกฎเก่าจะทำให้คนอ่านเชื่อว่ากฎเก่ายังมีผล
   → **แทนด้วย**: test ที่ยืนยันว่า `GetTeachingContentByLinkAsync` คืนค่า pacing **เท่ากับ
   `Company.Default*Ms` เป๊ะทั้งสามตัว** (รวมกรณีค่าเป็น `0`)
2. `SeedFirstCompanyIfEmpty` และ `Create` ตั้งค่า pacing ครบสามตัวจาก `ServerDefaults` (CP-16)
   — **ไม่เปลี่ยน · test ที่ Phase 4 เขียนไว้ยังใช้ได้ทั้งชุด**
3. `PUT` ของ LP-9: `cs` ถูกปฏิเสธ · ค่านอกช่วงถูกปฏิเสธ · `null` ถูกปฏิเสธ
   — **ไม่เปลี่ยน · test ที่ Phase 4 เขียนไว้ยังใช้ได้ทั้งชุด**
4. ~~`SaveAsync` ส่ง `null` ทับค่าเดิม แล้วแถวกลายเป็น `null` จริง (ยกเลิก override ได้)~~
   ❌ **ลบทิ้ง** — ไม่มีฟิลด์ให้ทดสอบแล้ว (LP-6 ยกเลิก)
5. **(ใหม่)** test/หลักฐานว่า `SaveAsync` **ไม่แตะค่า pacing** — ในทางปฏิบัติข้อนี้ถูกบังคับด้วย
   compiler อยู่แล้ว (entity ไม่มี property) ฉะนั้น **ถ้าเขียน test ไม่ได้อย่างมีความหมาย
   ไม่ต้องฝืนเขียน** ให้ `qa-engineer` ยืนยันด้วยการอ่านโค้ด + build ผ่านแทน

**LP-15 · สิ่งที่ห้ามทำในงานนี้**

- ~~**ห้ามลบสามช่องนี้ออกจากฟอร์มบทเรียน** — P1 ยืนยันว่ายังต้องมี~~
  → 🔄 **กลับด้าน 2026-08-22 (มติ N1): ตอนนี้ "ต้องลบ" ไม่ใช่ "ห้ามลบ"** ดู LP-11 ใหม่
  · เก็บข้อความเดิมไว้แบบขีดฆ่าเพื่อให้อ่านย้อนได้ว่า Phase 4/5 ที่ทำตามข้อห้ามเดิม
  **ทำถูกตามสัญญาที่มีผลอยู่ ณ ตอนนั้น** ไม่ใช่ทำผิด
- **ห้ามแตะ `videoDurationMs`** ในงานนี้ (เป็นงาน UI แยก ส่งตรงไป `frontend-engineer` ได้เอง)
- **ห้ามเพิ่มคอลัมน์ตั้งค่าอื่นของ F2** (ลิงก์หมดอายุ/TTS/แบรนด์) มาพร้อมกันเพราะ "อยู่ในตารางเดียวกันอยู่แล้ว"
  — R-4 + CP-15 ห้ามไว้ และความหมายของมันยังไม่เคาะ (A2/A3/A4/B3b)
- ~~**ห้ามสร้างหน้า UI ตั้งค่าบริษัทในงานนี้** — P3 บอกว่าไม่ต้องรอ F2 ไม่ได้บอกว่าให้ทำ F2 · รอบนี้
  หน้าจอที่แตะมีเพียงฟอร์มบทเรียน (LP-11) ส่วนค่าบริษัทแก้ผ่าน API (LP-9) ไปก่อน~~
  → 🔓 **ยกเลิกข้อห้ามนี้แล้ว 2026-08-22 (มติ P6)** · เจ้าของโปรเจกต์ตัดสินใจใหม่ว่า **ให้เริ่มทำ
  หน้าตั้งค่าบริษัทได้เลย ไม่ต้องรอ F2 ครบชุด** — เริ่มจาก section ของ pacing ก่อน แล้วเติม section
  อื่น (ลิงก์หมดอายุ/TTS/แบรนด์) ทีหลังทีละอย่างเมื่อ A2/A3/A4/B3b ถูกเคาะ · เหตุผลเดิมของข้อห้าม
  ("รอทำพร้อม F2 ทั้งก้อน") ไม่ใช่ข้อจำกัดทางเทคนิค แต่เป็นการจัดคิวงาน ซึ่งเจ้าของโปรเจกต์
  เปลี่ยนคิวเองได้ · **contract ของหน้าจอนี้อยู่ที่ `## Company Settings Page Rules` (SP-1..SP-14)**
  · เก็บข้อความเดิมไว้แบบขีดฆ่าเพื่อให้อ่านย้อนได้ว่า `frontend-engineer` ที่หยุดไม่ทำเมื่อรอบก่อน
  **ทำถูกตามกฎแล้ว** ไม่ใช่การข้ามงาน
- ⚠️ **ข้อห้ามอีกสามข้อข้างบนยังอยู่ครบ ไม่ได้ถูกยกไปด้วย** — การอนุญาตให้ทำ "หน้าจอ" ไม่ใช่การ
  อนุญาตให้เพิ่ม "ค่า" ของ F2 (ดู SP-13)

## Company Settings Page Rules

> **contract — `frontend-engineer` ทำตามนี้ตรงตัว** · เกิดจากมติ **P6 (2026-08-22)** ที่ยกข้อห้าม
> LP-15 ข้อสุดท้ายออก · **ทั้งส่วนนี้เป็นงาน frontend ล้วน ไม่มีงาน backend แม้แต่บรรทัดเดียว** —
> endpoint `GET`/`PUT /api/companies/{companyId}/lesson-pacing` (LP-9) implement + ทดสอบสดไปแล้ว
> ใน Phase 4 · **ไม่มี schema change ในรอบนี้** `## Data Model` ไม่ถูกแตะเลย
> · **ถ้าเจอเคสที่ contract นี้ไม่ครอบ ห้ามเดา ให้ตีกลับมาที่ `system-analyst`**
>
> ชื่อย่อ **SP** = Settings Page (จงใจไม่ใช้ `CS-*` เพราะจะชนกับชื่อ role `cs` ในเอกสารเดียวกัน)

**SP-1 · หน้าเดียว แบ่งเป็น section — ไม่ใช่ tab และไม่ใช่หน้าเดี่ยวของ pacing**

- route: **`/admin/settings`** (ชื่อกลาง ไม่ผูกกับ pacing) · ห้ามตั้งเป็น `/admin/settings/pacing`
  หรือ `/admin/lesson-pacing` — ชื่อที่ผูกกับ section แรกคือหนี้ที่ต้องย้าย route ทันทีที่มี section ที่สอง
- โครง: หน้าเดียว วาง section ต่อกันลงมาเป็น **`Card`** ต่อหนึ่ง section (`components/ui/card.tsx`
  มีอยู่แล้ว) · แต่ละ section มีหัวข้อ + คำอธิบายสั้นว่าค่าในนั้นมีผลกับอะไร
- **ห้ามใช้ `Tabs` ในรอบนี้** แม้ `components/ui/tabs.tsx` จะมีอยู่แล้ว — รอบนี้มี section เดียว
  แถบ tab ที่มี tab เดียวคือ **control หลอก** ด้วยเหตุผลเดียวกับที่ F4.2 ปฏิเสธ dropdown
  ที่มีตัวเลือกเดียว · ถ้าวันหนึ่ง section ≥ 4 แล้วอยากเปลี่ยนเป็น tab **ทำได้โดยไม่ต้องแก้ตัว
  section เลย** ถ้า SP-2 ถูกทำตาม (นั่นคือเหตุผลที่ SP-2 มีอยู่)
- **ห้ามใส่ placeholder ของ section ที่ยังไม่มี** ("ตั้งค่าเสียง — เร็ว ๆ นี้") — การเผื่อที่ในที่นี้
  หมายถึงโครงโค้ดที่เพิ่ม section ได้โดยไม่ต้องรื้อ ไม่ใช่ช่องว่างบนหน้าจอที่ลูกค้าเห็นแล้วรอ

**SP-2 · หนึ่ง section = หนึ่ง component ที่ดูแลตัวเองครบ**

- ไฟล์อยู่ที่ **`frontend/src/components/admin/settings/`** ตั้งชื่อตามเรื่องที่มันคุม เช่น
  `LessonPacingSettingsSection.tsx` (ตาม convention เดิมที่ component เฉพาะโดเมนอยู่นอก `ui/`)
- `app/admin/settings/page.tsx` **ทำหน้าที่ประกอบอย่างเดียว** — ห้ามมี state ของฟิลด์,
  ห้ามมี logic การ validate, ห้ามรู้จัก endpoint ใด ๆ
- แต่ละ section **โหลดข้อมูลของตัวเอง เซฟของตัวเอง validate ของตัวเอง และตัดสินสิทธิ์ของตัวเอง**
  · **ห้ามมี state ก้อนกลางของทั้งหน้า และห้ามมีปุ่ม "บันทึกทั้งหมด" ปุ่มเดียว** — เหตุผลไม่ใช่
  ความสวยงาม: B4 ห้ามยุบค่าเหล่านี้เข้า `PUT /api/companies/{id}` เดิมไว้แล้ว ค่าแต่ละกลุ่มจึงอยู่
  คนละ endpoint คนละสิทธิ์ ปุ่มเดียวจะกลายเป็นการยิงหลาย request ที่สำเร็จบางตัวล้มบางตัว
  = "บริษัทครึ่ง ๆ" แบบเดียวกับที่ F1.4/CP-6 ห้ามไว้ฝั่ง backend

**SP-3 · `companyId` มาจาก session เท่านั้น**

```ts
const { user, activeCompanyId } = useAdminSession();
const companyId = activeCompanyId ?? user?.companyId ?? null;
```

- ตรงกับแบบแผนที่ใช้อยู่จริงที่ `frontend/src/app/admin/users/page.tsx:41` — **ห้ามอ่าน `?company=`
  จาก URL เอง** (`AdminSessionProvider` แปลงให้แล้ว) และห้ามให้ผู้ใช้พิมพ์ companyId
- **owner สลับบริษัทกลางหน้าได้ → ต้อง refetch ทุก section ใหม่** (`useEffect` ผูกกับ `companyId`)
  ห้ามค้างค่าของบริษัทก่อนหน้าไว้บนจอ เพราะจอนี้คือจอที่กดเซฟแล้วเขียนทับข้อมูลลูกค้าจริง
- `companyId` เป็น `null` (owner ที่ยังไม่มีบริษัทเลย) → แสดง empty state ไม่ยิง request ไม่ crash
  ตามแบบแผนเดียวกับ `/admin/users`

**SP-4 · สิทธิ์เป็นของ "section" ไม่ใช่ของ "หน้า"**

| ระดับ | กฎรอบนี้ |
|---|---|
| ตัวหน้า `/admin/settings` | เปิดได้ทุก role ที่ล็อกอินแล้ว (`owner`/`admin`/`cs`) — ตัวหน้าเองไม่มีข้อมูลอะไรของตัวเอง |
| section `pacing` | **เห็น: `owner`/`admin`/`cs`** · **แก้: `owner`/`admin` เท่านั้น** — ตรง LP-9 เป๊ะ ไม่ใช่กฎใหม่ |

- `cs` เห็นค่าจริงพร้อมคำอธิบาย แต่ **input ทุกช่อง `disabled` และ "ไม่มีปุ่มบันทึกอยู่บนจอเลย"**
  พร้อมข้อความสั้น ๆ ว่าอ่านอย่างเดียว · **ห้ามทำแบบ "ปุ่มกดได้แล้วเด้ง error 403"**
  (A6 ทางเลือก (ค) ถูกปฏิเสธไปแล้วด้วยเหตุผล UX)
- **การซ่อน/ปิดปุ่มที่ UI ไม่ใช่การกั้นสิทธิ์** — ด่านจริงคือ `guard` ที่ server (LP-9) ซึ่งทดสอบสด
  แล้วว่า `cs` ได้ 403 จริง · เขียนคอมเมนต์กำกับไว้ในโค้ดเหมือนที่ `/admin/users/page.tsx:27-33` ทำ
- **โครงต้องรองรับกรณีที่ section ถัดไปมีสิทธิ์ไม่เหมือนกัน** (เช่นแบรนด์อาจเป็น owner-only) —
  สิทธิ์จึงประกาศอยู่ใน component ของ section นั้น ไม่ใช่ที่ตัวหน้า
- 🔓 **อัปเดต 2026-08-22 — A8 ปิดแล้ว กลไกไม่ใช่ของค้างอีกต่อไป**: "เห็น" กับ "แก้" เป็น
  **คนละแกน** และ section หนึ่งอาจถูก **ซ่อนจาก role ไปเลย** ไม่ใช่แค่ read-only
  → **แบบจำลองและ invariant อยู่ที่ SP-15 ซึ่งเป็น contract ผูกมัดตั้งแต่รอบนี้**
  · **สำหรับ section `pacing` ไม่มีอะไรเปลี่ยนแม้แต่จุดเดียว** — ตารางข้างบนยังถูกต้องทั้งแถว
  (เห็นทุก role, แก้ได้ owner/admin) เพราะมันคือ section กลุ่ม "ไม่อ่อนไหว" ตามที่เจ้าของโปรเจกต์ระบุ

**SP-5 · เมนูใน sidebar — ต้องขยับ gate ที่มีอยู่ ไม่ใช่เพิ่มกลุ่มใหม่**

- เพิ่มรายการ **"ตั้งค่าบริษัท" → `/admin/settings`** ในกลุ่ม **"ตั้งค่า"** ที่มีอยู่แล้ว
  (`AdminSidebar.tsx:170-189`) ไม่สร้างกลุ่มใหม่
- **จุดที่ต้องแก้และพลาดง่ายที่สุดของ SP ทั้งชุด**: วันนี้ทั้งกลุ่มถูกกั้นด้วย
  `{user?.role !== "cs" && (...)}` ที่ `AdminSidebar.tsx:170` → ถ้าวางเมนูใหม่ไว้ในนั้นเฉย ๆ
  **`cs` จะไม่เห็นหน้านี้เลย ซึ่งขัดกับ SP-4 และขัดกับเจตนาของ LP-9 ที่ตั้งใจให้ `cs` อ่านได้**
  · ต้อง **ย้าย gate ลงไปที่ระดับรายการ**: "ผู้ใช้งาน" คง `!== "cs"` ไว้เหมือนเดิมทุกประการ
  ส่วน "ตั้งค่าบริษัท" แสดงทุก role
- 🔓 **อัปเดต 2026-08-22 (A8/SP-15)**: เงื่อนไขแสดงรายการ "ตั้งค่าบริษัท" ต้อง **derive จาก registry
  ของ section** = "role นี้เห็นอย่างน้อย 1 section" (SP-15 ข้อ 7) **ห้าม hardcode รายชื่อ role
  ที่ตัวเมนู** — รอบนี้ทุก role เห็น section pacing อยู่แล้ว ผลลัพธ์จึงเท่ากับ "แสดงทุก role"
  เป๊ะ ๆ ตามบรรทัดบน · เขียนแบบ derive ตั้งแต่รอบนี้เพราะวันที่มี section ที่ซ่อนจาก `cs`
  ถ้าเมนู hardcode ไว้ `cs` จะกดเข้ามาเจอหน้าเปล่า
- ไอคอนใช้จาก `lucide-react` ที่เป็น dependency อยู่แล้ว (เช่น `SettingsIcon`) ห้ามเพิ่ม dependency

**SP-6 · เนื้อของ section `pacing`**

สามช่อง ตรงกับสามค่าของ LP-1 เท่านั้น — `introWaitMs` · `breathPauseMs` · `finalQuestionWaitMs`
· **ห้ามมี `videoDurationMs`** (LP-15 ยังห้ามอยู่ และมันเป็นค่าระดับสไลด์ ไม่ใช่ระดับบริษัท)

- ค่าตั้งต้นบนจอมาจาก `getCompanyLessonPacing(companyId)` ที่มีใน `api-client.ts` อยู่แล้ว
- แต่ละช่องมีคำอธิบายภาษาไทยว่าค่านั้นคือจังหวะอะไรในห้องเรียน + บอกช่วงที่รับได้ตาม LP-8
- **ห้ามมีปุ่ม "คืนค่าเริ่มต้น"/"ใช้ค่ากลาง"** — ที่ชั้นบริษัทไม่มีชั้นที่สูงกว่าให้สืบทอด (LP-1 มี 2 ชั้น
  และ `ServerDefaults` ถูกอ่านครั้งเดียวตอนสร้างบริษัทเท่านั้น) ปุ่มแบบนั้นจะกลายเป็นชั้นที่สาม
  ที่เงียบ ซึ่ง LP-1 ห้ามไว้ตรง ๆ

**SP-7 · ที่ชั้นบริษัท "ว่าง" ไม่ใช่ค่าที่ถูกต้อง (แก้ 2026-08-22 รอบที่ 7 — ~~ตรงข้ามกับฟอร์มบทเรียน~~ ไม่มีฟอร์มบทเรียนให้เทียบแล้ว)**

**หลังมติ N1 ไม่มีช่อง pacing ที่ฟอร์มบทเรียนอีกต่อไป (LP-11 ยกเลิก) — "ความสับสนสองมาตรฐาน"
ที่ข้อนี้เคยเตือนไว้จึงหายไปเอง** · กฎของ section นี้ **ไม่เปลี่ยนแม้แต่ตัวเดียว**:

| ค่าที่กรอก | section นี้ (SP-7) |
|---|---|
| ช่องว่าง | ❌ **ไม่ถูกต้อง** — ปฏิเสธตั้งแต่ที่ client ไม่ต้องยิง request |
| `0` | ✅ **รับได้** (LP-8 ช่วงเริ่มที่ `0`) |

~~| | ฟอร์มบทเรียน (LP-11) | ... |~~ (ตารางเปรียบเทียบเดิมถูกตัดออกเพราะคอลัมน์ซ้ายไม่มีอยู่จริงแล้ว)

- `PUT` **ส่งครบสามค่าเสมอ ห้าม partial ห้าม `null`** (LP-9) — แม้ผู้ใช้แก้ช่องเดียวก็ส่งทั้งสามค่า
- ช่องว่าง / ค่าที่ไม่ใช่ตัวเลข → ข้อความ error ภาษาไทยใต้ช่องนั้น **และไม่ยิง request**
- **ห้ามใช้แพตเทิร์น `Number(x) || 0`** ที่นี่ — มันจะกลืนช่องว่างให้กลายเป็น `0`
  ซึ่งที่นี่แปลว่า "เซฟค่า 0 ให้ลูกค้าโดยที่เขาไม่ได้ตั้งใจ" · **ตอนนี้เป็นจุดเดียวในระบบที่รับ
  ค่า pacing จากมนุษย์** ฉะนั้นถ้าพลาดที่นี่ ไม่มีด่านอื่นให้พลาดแทน

**SP-8 · validation ที่ client เป็นแค่การช่วยผู้ใช้ — server เป็นเจ้าของกฎ**

- เช็คช่วงตาม LP-8 ที่ client ได้ (`0`–`60000` / `0`–`10000` / `0`–`120000`) เพื่อไม่ให้ผู้ใช้
  ต้องรอ round-trip เพื่อรู้ว่าพิมพ์เกิน — **แต่ห้ามถือว่าเป็นด่านจริง**
- ตัวเลขช่วงต้องอ้างค่าจากที่เดียว (constant ในไฟล์ section นั้น) **ห้ามกระจาย literal ตามหลาย
  component** — ตัวเลขชุดนี้**ยืนยันแล้ว 2026-08-22** (LP-8) แต่เจ้าของโปรเจกต์บอกไว้ว่า
  "จูนทีหลังได้" ฉะนั้นจุดแก้ต้องมีจุดเดียวต่อฝั่งเสมอ
- 400 จาก server → **แสดงข้อความจาก server ตรง ๆ** (เป็นภาษาไทยอยู่แล้วตาม convention)
  ห้ามเขียนทับด้วยข้อความ generic ของ frontend

**SP-9 · หลังเซฟสำเร็จ**

- ใช้ค่าที่ server ตอบกลับ หรือ refetch — **ห้าม optimistic update** (จำนวน request น้อยมาก
  ไม่คุ้มกับความเสี่ยงที่จอโชว์ค่าที่ยังไม่ถูกเขียนจริง)
- แจ้งสำเร็จด้วย toast/ข้อความสั้น (`components/ui/toast.tsx` มีอยู่แล้ว) แล้ว **อยู่หน้าเดิม**
  ไม่ redirect

**SP-10 · ข้อความบนจอต้องตรงกับความจริงของ LP-7**

**แก้ 2026-08-22 รอบที่ 7 (มติ N1) — ข้อความบนจอเปลี่ยน เพราะความจริงเปลี่ยน:**
ต้องเขียนให้ชัดว่าค่านี้มีผลกับ **ทุกบทเรียนของบริษัทนี้** (~~เฉพาะบทที่ปล่อยช่องว่างไว้~~ —
ไม่มีช่องให้ปล่อยว่างแล้ว) และมีผล **ตั้งแต่การเข้าห้องเรียนครั้งถัดไป**
· **ยังห้ามเขียนว่า "มีผลทันที"** — ห้องที่กำลังเรียนค้างอยู่ไม่เปลี่ยนกลางคัน (LP-7)
(ข้อบังคับแบบเดียวกับ CP-14 ที่ห้าม UI สัญญาเกินกว่าที่ระบบทำจริง)

**SP-11 · สิ่งเดียวที่ต้องเพิ่มใน `api-client.ts`**

- เพิ่ม **`updateCompanyLessonPacing(companyId, payload)`** → `PUT /api/companies/{companyId}/lesson-pacing`
  · รอบก่อน `frontend-engineer` **จงใจไม่เพิ่มฟังก์ชันนี้** เพราะยังไม่มี UI ที่เรียก — ตอนนี้มีแล้ว
- `getCompanyLessonPacing()` และ type `CompanyLessonPacing` (สามค่าเป็น `number` ไม่ nullable)
  **มีอยู่แล้ว ใช้ต่อ ห้ามประกาศซ้ำ**
- payload ของ `PUT` เป็นสามค่าเดียวกัน `number` ทั้งหมด — ถ้าต้องประกาศ type ใหม่ ต้องตรงกับ DTO
  ฝั่ง server ที่มีอยู่จริง (Architecture Rule 7) **ห้ามเดาชื่อฟิลด์** ให้เปิดไฟล์ DTO จริงอ่าน

**SP-12 · เคสที่ต้องตอบให้ครบ ไม่ปล่อยให้ engineer ตัดสินเอง**

| เคส | ต้องเป็น |
|---|---|
| `companyId` เป็น `null` | empty state ไม่ยิง request (SP-3) |
| `GET` ล้มเหลว / 404 | ข้อความ error ในตัว section นั้น · section อื่น (ในอนาคต) ต้องไม่พังตาม |
| บริษัทที่ `IsActive = false` | **แสดงและแก้ได้ตามปกติ** (LP-9 ระบุไว้ชัด) ไม่ต้องมีจอกั้น |
| `cs` เปิดหน้านี้ | เห็น section pacing แบบอ่านอย่างเดียว (SP-4) ไม่ใช่หน้าเปล่าและไม่ใช่ 403 |
| `PUT` ได้ 403 | ไม่ควรเกิดถ้า SP-4 ถูกทำ — ถ้าเกิด ให้แสดงข้อความจาก server ห้ามกลืนเงียบ |
| กด "บันทึก" ซ้ำระหว่างรอ response | ปุ่มอยู่ในสถานะ loading/disabled ระหว่างรอ (ไม่ยิงซ้ำ) |
| **role หนึ่งเห็น 0 section** (เกิดไม่ได้รอบนี้ — pacing เห็นทุก role — แต่ต้องตอบไว้ก่อนตาม SP-15) | เมนู sidebar **ไม่แสดง** สำหรับ role นั้น (SP-5) · ถ้าพิมพ์ URL เข้ามาตรง ๆ ให้แสดง **empty state กลาง ๆ** ("ยังไม่มีการตั้งค่าที่คุณเข้าถึงได้") **ห้ามเป็นจอ 403 และห้ามไล่ออกไปหน้าอื่น** |
| **section ที่ role นี้ไม่มีสิทธิ์เห็น** | **ไม่ render เลย ไม่ยิง `GET` เลย** — ไม่ใช่ disabled ไม่ใช่กล่อง "คุณไม่มีสิทธิ์" (SP-15 ข้อ 5) |

**SP-13 · สิ่งที่ห้ามทำในงานนี้**

- **ห้ามเพิ่ม section อื่นของ F2** (ลิงก์หมดอายุ / TTS / แบรนด์) แม้จะ "เผื่อไว้ก่อน" —
  A2 · A3 · A4 · B3b ยังไม่ถูกเคาะ และ R-4 + CP-15 ยังห้ามอยู่ · **P6 อนุญาต "หน้าจอ" ไม่ได้
  อนุญาต "ค่า"** · Module B ยัง ⏸️ พักอยู่เหมือนเดิม
- **ห้ามแตะ backend แม้แต่บรรทัดเดียว** — ไม่มี endpoint ใหม่ ไม่มี DTO ใหม่ ไม่มี migration
  ถ้ารู้สึกว่าต้องแก้ backend แปลว่าเจอเคสที่ contract นี้ไม่ครอบ → ตีกลับ `system-analyst`
- ~~**ห้ามแตะฟอร์มบทเรียน** (`admin/lessons/**`) ในรอบนี้ — LP-11/LP-12/LP-13 ทำเสร็จและรอ QA อยู่~~
  → 🔄 **แก้ 2026-08-22 รอบที่ 7**: ฟอร์มบทเรียน **ต้องถูกแก้** แล้ว (ถอดสามช่องออกตาม LP-11 ใหม่)
  · ข้อห้ามนี้เปลี่ยนเป็น: **งานถอดฟอร์มบทเรียนกับงานหน้า `/admin/settings` เป็นคนละชิ้น
  อย่าเอามารวมเป็น commit/แถบงานเดียวกันโดยไม่มี task รองรับ** — `project-manager` เป็นคนจัดว่า
  อยู่เฟสไหน (`system-analyst` ไม่ตัดสิน) · **ตัวหน้า `/admin/settings` เองไม่ต้องแก้อะไรเลย
  จากมติ N1/N2/N3** — SP-1..SP-15 ยังถูกต้องทั้งชุด ยกเว้นถ้อยคำใน SP-7/SP-10 ที่แก้ไปแล้ว
- **ห้ามติดตั้ง shadcn primitive ใหม่** — ตรวจแล้วว่า `card`/`input`/`label`/`button`/`alert`/
  `separator`/`toast`/`spinner` มีครบใน `frontend/src/components/ui/` แล้ว
- **ห้ามทำหน้าจอรวมค่า pacing ของหลายบริษัทในจอเดียว** — หนึ่งจอ = บริษัทที่กำลังดูอยู่หนึ่งบริษัท
  ตาม SP-3 (owner สลับบริษัทด้วย `CompanySwitcher` เดิม ไม่มีตัวเลือกบริษัทซ้อนในหน้านี้)

**SP-14 · test ที่ต้องมี (เล็กแต่ไม่ใช่ทางเลือก)**

แยกการแปลงค่าจากช่องกรอก + เช็คช่วงออกมาเป็น **pure function** แล้วเขียน unit test (vitest ที่มี
อยู่แล้ว) ครอบสี่กรณีนี้เป็นอย่างน้อย: `0` **ผ่าน** · ค่าสูงสุดของช่วง **ผ่าน** · สูงสุด+1 **ถูกปฏิเสธ** ·
**ช่องว่างถูกปฏิเสธ** (ไม่ถูกแปลงเป็น `0`) — สามข้อแรกคุม LP-8 ข้อสุดท้ายคุม SP-7 ซึ่งเป็นบั๊ก
ประเภทเดียวกับที่ LP-3/LP-11 พิสูจน์แล้วว่าเกิดจริงในโค้ดนี้มาก่อน

**SP-15 · แบบจำลองสิทธิ์ต่อ section — "เห็น" กับ "แก้" เป็นคนละแกน (ปิด A8 · มติ 2026-08-22)**

> เจ้าของโปรเจกต์ตอบตรงในแชท 2026-08-22: *"เห็นหน้าตั้งค่าได้ แต่บางการตั้งค่าก็มีการติด permission
> ในการมองเห็นไว้ได้ไหม เช่น ฟังก์ชันที่แอดมินสูงสุดตั้งค่าได้เท่านั้น ก็เปิดให้เห็นทั้งหมด
> ถ้าแค่ตั้งค่าเสียง ก็เห็นได้ทุกคน"*
>
> → **สิทธิ์การมองเห็น (visibility) กับสิทธิ์แก้ (edit) แยกกัน และตั้งค่าแยกต่อ section ได้** ·
> section ที่อ่อนไหว **ซ่อนจาก role ไปเลยได้** (ไม่ใช่แค่ read-only) ส่วน section ที่ไม่อ่อนไหว
> (เช่น pacing / เสียง) **เห็นได้ทุก role** · นี่คือ**หลักการทั่วไปของ section ทุกตัวในอนาคต**
> ไม่ใช่กฎเฉพาะของ pacing

**1 · ทุก section ประกาศสิทธิ์ของตัวเองเป็นสองรายการที่แยกกันจริง**

```ts
// frontend/src/components/admin/settings/section-access.ts (ไฟล์ใหม่ เล็ก ไม่มี logic อื่นปน)
import type { AdminRole } from "@/types/domain";

export type SettingsSectionAccess = {
  /** role ที่ไม่อยู่ในรายการนี้ = "ไม่ render section นี้เลย" ไม่ใช่ disabled ไม่ใช่กล่อง 403 */
  visibleToRoles: readonly AdminRole[];
  /** อยู่ใน visibleToRoles แต่ไม่อยู่ในนี้ = เห็นค่าจริงแบบอ่านอย่างเดียวตาม SP-4 */
  editableByRoles: readonly AdminRole[];
};
```

**2 · ค่าของ section `pacing` รอบนี้ — เท่ากับ LP-9/SP-4 เดิมทุกประการ ไม่ใช่กฎใหม่**

```ts
// frontend/src/components/admin/settings/LessonPacingSettingsSection.tsx
export const LESSON_PACING_SECTION_ACCESS: SettingsSectionAccess = {
  visibleToRoles: ["owner", "admin", "cs"],   // ← กลุ่ม "ไม่อ่อนไหว" · cs GET ได้จริงตาม LP-9
  editableByRoles: ["owner", "admin"],        // ← cs PUT ได้ 403 จริง ทดสอบสดแล้วใน Phase 4
};
```

**3 · invariant ที่ engineer ตรวจเองได้ ไม่ต้องถามใคร** (ผิดข้อใดข้อหนึ่ง = ตีกลับ `system-analyst`)

- `editableByRoles` ⊆ `visibleToRoles` **เสมอ** — role ที่มองไม่เห็นจะแก้ไม่ได้โดยนิยาม
- `owner` ต้องอยู่ทั้งสองรายการ **เสมอ** — ไม่มีค่าตั้งค่าใดที่ owner มองไม่เห็นหรือแก้ไม่ได้
  (owner ผ่าน `EnsureCanAccessCompany` ทุกบริษัทอยู่แล้ว การมี section ที่ owner แตะไม่ได้
  คือค่าที่ไม่มีใครแก้ได้เลย)
- `visibleToRoles` ว่างเปล่าไม่ได้

**4 · registry เดียว — page ยังคง "ประกอบอย่างเดียว" ตาม SP-2**

- `frontend/src/components/admin/settings/sections.ts` ประกาศรายการ
  `{ id, access, Component }` เรียงตามลำดับที่จะแสดง
- `app/admin/settings/page.tsx` ทำแค่: อ่าน role จาก session → กรองด้วย `visibleToRoles` →
  render `Card` ตามลำดับ · **หน้ายังไม่รู้จัก endpoint, ไม่รู้จัก validation, ไม่ถือ state ของฟิลด์**
- **หน้าห้ามส่ง `canEdit` ลงไปให้ section** — section อ่าน `editableByRoles` ของตัวเองจาก descriptor
  ของตัวเอง (ไม่งั้นหน้าจะกลายเป็นจุดที่ทำให้ section ถูกเปิดสิทธิ์ผิดโดยที่ section ไม่รู้ตัว)

**5 · "ซ่อน" แปลว่าอะไรให้ชัด**

ไม่ mount · ไม่ยิง `GET` · ไม่มีหัวข้อค้างบนจอ · **ไม่มีข้อความว่า "คุณไม่มีสิทธิ์ดูส่วนนี้"** —
การบอกว่ามี section ที่คุณเห็นไม่ได้ คือการเปิดเผยว่าค่านั้นมีอยู่ ซึ่งขัดกับเจตนาของการซ่อน
(และขัด SP-1 ที่ห้าม placeholder ของ section ที่ยังไม่มีอยู่แล้ว)

**6 · การซ่อนที่ UI ไม่ใช่การกั้นสิทธิ์ — ข้อบังคับที่มาคู่กัน**

ถ้า section ใดประกาศ **ซ่อนจาก role ใด** contract ของ section นั้น **ต้องระบุด้วยว่า endpoint
ฝั่ง server ปฏิเสธ role นั้นที่ `GET` ด้วย** · ถ้า endpoint ปฏิเสธไม่ได้ (เช่นใช้ endpoint ร่วมกับ
ค่าอื่น) ต้องเขียนกำกับตรง ๆ ว่าการซ่อนนั้นเป็น **cosmetic เท่านั้น** และ **ห้ามใช้กับข้อมูล
ที่อ่อนไหว** · **section `pacing` ไม่เข้าเคสนี้เลย** — ไม่ซ่อนจากใคร และ `cs` `GET` ได้จริง
โดยตั้งใจตาม LP-9

**7 · sidebar derive จาก registry (แก้ SP-5)**

เงื่อนไขแสดงรายการ "ตั้งค่าบริษัท" = **role นี้เห็นอย่างน้อย 1 section** คำนวณจาก registry
ข้อ 4 · **ห้าม hardcode รายชื่อ role ที่ตัวเมนู** · รอบนี้ผลลัพธ์ = แสดงทุก role เหมือน SP-5 เดิมเป๊ะ

**8 · ใครตัดสินว่า section ไหนได้ค่าอะไร**

**เจ้าของโปรเจกต์ ไม่ใช่ engineer และไม่ใช่ `system-analyst`** — SP-15 ให้แค่ *กลไก* ไม่ได้ให้ *คำตอบ*
ของ section ในอนาคต · ตอนเพิ่ม section ใหม่ ต้องมีค่าของสองแกนนี้เขียนอยู่ใน contract ของ section นั้น
**ก่อน** เริ่มเขียนโค้ด ถ้าไม่มี → ตีกลับ `system-analyst` → `business-analyst`
(ส่วนที่ยังต้องถามคือ **A6 ที่เหลือ** ไม่ใช่ A8 ซึ่งปิดแล้ว) · **ห้ามเหมาว่า section ใหม่ = เห็นทุก role
เพราะ pacing เป็นแบบนั้น**

**9 · ห้ามสร้างแกนที่สาม** เช่น "เห็นแต่ค่าถูกปิดบังเป็น `•••`" หรือสิทธิ์ระดับฟิลด์ในหนึ่ง section —
จนกว่าจะมีคนขอจริง (R-4 ห้ามเผื่ออนาคตอยู่แล้ว)

**10 · test เพิ่มจาก SP-14 (เล็ก แต่ไม่ใช่ทางเลือก)**

pure function `resolveSectionAccess(access, role) → { visible, canEdit }` + unit test ครบสาม role
ของ section pacing: `owner` = (เห็น, แก้ได้) · `admin` = (เห็น, แก้ได้) · `cs` = **(เห็น, แก้ไม่ได้)**
บวกหนึ่งเคสสังเคราะห์ที่ `visibleToRoles` ไม่มี role นั้น → `visible = false` **และ `canEdit = false`**
(กันไม่ให้ invariant ข้อ 3 ถูกทำหลุดเงียบ ๆ ในอนาคต)

## Admin User Management Rules

> **contract** · เขียน 2026-08-25 จาก `requirement.md` §F5 (F5.0–F5.4) ซึ่งเคาะครบแล้วทุกข้อ
> ยกเว้น OQ-15 (ข้อความปุ่ม Cancel — cosmetic ไม่บล็อก) · **เจ้าของกฎชุดนี้คือ Module U**
> · engineer ที่ทำ phase ของ Module U ต้องอ่านหัวข้อนี้ **ทั้งหัวข้อ** ไม่ใช่เฉพาะข้อที่ดูเกี่ยวข้อง
>
> ทุก AU-* ข้างล่างถูกตรวจกับโค้ดจริงเมื่อ 2026-08-25 — เลขบรรทัดที่อ้างถึงเป็นสถานะ ณ วันนั้น

**AU-1 · ขอบเขต: หนึ่งความสามารถ หนึ่งที่ หนึ่ง endpoint**

ความสามารถที่เพิ่มคือ **แก้อีเมล / รีเซ็ตรหัสผ่าน / เปลี่ยน role / เปิด-ปิดบัญชี ของผู้ใช้ _รายอื่น_**
· อยู่ในหน้า `/admin/users` เดิมในรูปโมดัล **ห้ามมี route ใหม่** (CP-15 ที่แก้ขอบเขตแล้ว)
· ฝั่ง server ใช้ **`PUT /api/admin-users/{id}` เดิม** ที่ `AdminUserController.cs:36-38` เรียก
`IAdminUserService.Update(id, dto)` — **ห้ามเพิ่ม endpoint ใหม่** (เหตุผลอยู่ที่ AU-2)
· **ไม่มี schema change** — ดู `## Data Model` §Module U และ F5.3

**AU-2 · รูปร่าง DTO: ขยาย `UpdateAdminUserDto` ใบเดิม ไม่แตกเป็นสาม endpoint**

`system-analyst` ตัดสินข้อนี้ (เป็นคำถามทางเทคนิค ไม่ใช่คำถามธุรกิจ) · รูปร่างสุดท้าย:

```csharp
public sealed class UpdateAdminUserDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(100, ErrorMessage = "ชื่อผู้ใช้ยาวเกินไป")]
    public required string DisplayName { get; init; }   // เดิม — ไม่เปลี่ยน

    // ใหม่ · attribute ชุดเดียวกับ CreateAdminUserDto.Email เป๊ะ ห้ามคิดข้อความใหม่
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "กรุณาเลือกสิทธิ์")]
    public required string Role { get; init; }          // เดิม — ไม่เปลี่ยน

    public required bool IsActive { get; init; }        // เดิม — ไม่เปลี่ยน

    /// <summary>null / ว่าง / ช่องว่างล้วน = ไม่รีเซ็ตรหัสผ่าน (AU-7).
    /// ⚠️ ห้ามใส่ [Required] หรือ [MinLength] ที่ฟิลด์นี้ — เหตุผลใน AU-7.</summary>
    public string? NewPassword { get; init; }
}
```

**ทำไมไม่แยกเป็น `/role` · `/email` · `/password` สามใบ**:
1. **F5.2.4 บังคับให้สองเส้นทางมาบรรจบที่ธงเดียว** — endpoint เดียว = `UnitOfWork.Commit()` ครั้งเดียว
   = ธงถูกตั้งครั้งเดียวและถูกยกเลิกพร้อมกันทั้งชุดเมื่ออีเมลซ้ำ · แยกสามใบจะเกิดสภาพ
   "อีเมลเปลี่ยนแล้วแต่รหัสยังไม่ถูกตั้ง" ซึ่งเป็น failure mode เดียวกับ "บริษัทครึ่ง ๆ" ที่ CP-6
   ตั้งใจกันไว้ทั้งโมดูล
2. **AU-3..AU-5 จะต้องถูกเขียนซ้ำสามที่** บนตารางที่ `IAuthorizationGuard` เป็นด่านเดียวไม่มีอะไรรอง
   (R-1) — สามสำเนาของกฎเดียวคือวิธีมาตรฐานที่กฎหนึ่งจะถูกลืม
3. **โมดัลมีปุ่มบันทึกปุ่มเดียว** ตาม F5.2.6 · การยิงสาม request จากปุ่มเดียวคือการย้าย
   partial failure ไปให้ UI แก้เอง

**ราคาที่ยอมจ่าย**: DTO ใบเดียวถือทั้งฟิลด์ธรรมดาและ credential · ชดเชยด้วย AU-7 (`null` = ไม่แตะ)
และด้วยข้อเท็จจริงว่าหลัง F5.2.6 **เหลือผู้เรียก endpoint นี้เพียงโมดัลเดียว** ไม่มี call site
แบบ partial หลงเหลือ

⚠️ **`Email` เป็น `required` = breaking change ของ wire contract** — คำขอเดิมที่ไม่ส่ง `email`
จะได้ 400 ทันที · ผู้เรียกเดิมมีจุดเดียวคือ `UserRow.apply()` (`page.tsx:167-184`) ซึ่งถูกลบใน
งานเดียวกันตาม AU-13 → **backend กับ frontend ของ Module U ต้อง deploy พร้อมกัน** (R-19)

**AU-3 · ลำดับการตรวจใน `AdminUserService.Update` — ลำดับนี้เป็นส่วนหนึ่งของ contract**

```
1. var user = _users.Get(id) ?? throw NotFound("ผู้ใช้")                    // เดิม ไม่แก้
2. ตรวจตาม role ปัจจุบันของเป้าหมาย                                          // เดิม ไม่แก้
     user.Role == Owner            → guard.EnsureOwner()
     user.CompanyId ว่าง           → throw ConfigError("ข้อมูลผู้ใช้ไม่สมบูรณ์ - ...")
     อื่น ๆ                        → guard.EnsureCanManageUsers(user.CompanyId)
3. guard.EnsureCanAssignRole(input.Role)                                    // เดิม ไม่แก้
4. EnsureNotSelf(user)                    ← ใหม่ (AU-5)
5. guard.EnsureNotSameRankPeer(user.Role) ← ใหม่ (AU-4)
6. EnsureNotRemovingLastGuardian(user, input)                               // เดิม ไม่แก้
7. กฎอีเมล (AU-6)  → รู้ผลว่า emailChanged จริงหรือไม่
8. กฎรหัสผ่าน (AU-7 + AU-9) → รู้ผลว่า passwordReset จริงหรือไม่
9. เขียนค่าทั้งหมด + UpdateBy/UpdateDate → _users.Update(user) → Commit() ครั้งเดียว (AU-10)
```

**ทำไมลำดับนี้ ไม่ใช่ลำดับอื่น**:
- **ข้อ 3 ต้องอยู่ก่อนข้อ 4/5 เสมอ** — guard กัน privilege escalation ห้ามถูกข้ามด้วย early return
  ใด ๆ ที่เพิ่มเข้ามาทีหลัง (หลักการเดียวกับคอมเมนต์ลำดับใน `Create`, `IAdminUserService.cs:52-54`)
- **ข้อ 4 ต้องอยู่ก่อนข้อ 5** — `admin` ที่กดกับบัญชีตัวเองเข้าเงื่อนไข "role เดียวกัน" ด้วย
  ถ้าให้ peer-lockout ยิงก่อน จะได้ข้อความว่า "จัดการคนระดับเดียวกันไม่ได้" ซึ่งไม่ใช่เหตุผลจริง
  และทำให้คนอ่านเข้าใจผิดว่าถ้าไปเปลี่ยน role ตัวเองก่อนจะทำได้
- **ข้อ 7/8 ต้องอยู่หลังทุก guard** — ห้ามแตะข้อมูล credential ก่อนรู้ว่าผู้เรียกมีสิทธิ์จริง

**AU-4 · peer-lockout (F5.2.1) — เมธอดใหม่ใน `IAuthorizationGuard`**

เพิ่มเมธอดใหม่ **หนึ่งเมธอด ไม่แก้ของเดิมสักบรรทัด**:

```csharp
/// <summary>Throws unless the signed-in user may act on an account holding
/// <paramref name="targetRole"/>. Owner is the single exemption (F5.2.1): everyone else is
/// refused against their own rank, so two admins of one company cannot seize each other's
/// account through the very feature built for managing their people.</summary>
void EnsureNotSameRankPeer(string targetRole);
```

- implementation: `EnsureAuthenticated()` → ถ้า `currentUser.Role == AdminRole.Owner` ให้ผ่าน
  → ถ้า `currentUser.Role == targetRole` ให้ `throw GeneralException.Forbidden("ไม่สามารถจัดการบัญชีที่มีสิทธิ์ระดับเดียวกับคุณได้")`
  → นอกนั้นผ่าน
- **เทียบด้วย string equality ตรง ๆ ห้ามไปเปิด `AdminRole.RankOf` ให้เป็น public** — rank เป็น 1:1
  กับชื่อ role อยู่แล้ว และ `AdminRole` ถูกห้ามแก้ (CP-15) · คอมเมนต์ในไฟล์นั้นก็ระบุเองว่า rank
  **จงใจไม่เปิดเป็น ordering ทั่วไป** (`AdminRole.cs:28-29`)
- **`targetRole` คือ role _ปัจจุบัน_ ของเป้าหมายที่อ่านมาจาก DB (`user.Role`) ไม่ใช่ `input.Role`**
  — role ที่ขอเปลี่ยนไปถูกคุมด้วย `EnsureCanAssignRole` อยู่แล้วในข้อ 3 · ผลที่ตามมาโดยตั้งใจ:
  **`admin` ยังเลื่อน `cs` ขึ้นเป็น `admin` ได้** (เหมือนที่วันนี้สร้าง `admin` ใหม่ได้อยู่แล้วที่
  `POST /api/admin-users`) แต่หลังจากนั้นจะจัดการบัญชีนั้นไม่ได้อีก ต้องให้ `owner` ทำ — ดู R-18
  และ **OQ-U2** · **ห้าม engineer เพิ่มกฎกันการเลื่อนขึ้นเป็น peer เอง** เพราะ F5.2.1 ไม่ได้สั่งไว้
- **จุดเรียกมีจุดเดียว: `AdminUserService.Update`** — **ห้ามไปเรียกใน `Create`** เพราะจะเปลี่ยน
  พฤติกรรมที่ Module A ผ่าน QA ไปแล้ว (admin สร้าง admin ได้) ซึ่ง `requirement.md` ไม่ได้ขอ

**AU-5 · ห้ามใช้เส้นทางนี้กับบัญชีตัวเอง (F5.2.7) — กฎแข็ง ไม่มีข้อยกเว้นแม้แต่ `owner`**

- private method ใน `AdminUserService` ข้าง `EnsureNotRemovingLastGuardian`:
  ถ้า `currentUser.UserId == user.Id` → `throw GeneralException.Forbidden("ไม่สามารถใช้หน้าจัดการผู้ใช้กับบัญชีของตัวเองได้ กรุณาเปลี่ยนรหัสผ่านที่หน้าเปลี่ยนรหัสผ่านของคุณเอง")`
- **ทำไมอยู่ใน service ไม่ใช่ใน `IAuthorizationGuard` ทั้งที่ AU-4 อยู่ในนั้น** — เส้นแบ่งคือ
  guard ตอบว่า *"ผู้เรียกคนนี้ทำกับเป้าหมายชนิดนี้ได้ไหม"* ส่วน service ตอบว่า *"ปฏิบัติการนี้
  ถูกกฎไหม"* · การทำกับตัวเอง**ไม่ใช่**เรื่องผิดโดยทั่วไป — F5.2.7 ระบุเองว่ายังทำได้ที่
  `POST /api/auth/change-password` ซึ่ง**ยังบังคับให้รู้รหัสเดิม** · ถ้าเอาไปไว้ในกฎกลาง
  จะชวนให้คนถัดไปเอาไปใช้กับ endpoint ที่ไม่ควรใช้
- **ห้ามผ่อนกฎนี้ให้ `owner`** — เหตุผลของ F5.2.7 คือกันคนที่ยึดเครื่องที่ล็อกอินค้างไว้ตั้งรหัสใหม่
  โดยไม่ต้องรู้รหัสเดิม ซึ่งเป็นเหตุผลที่ใช้กับ `owner` หนักที่สุด ไม่ใช่เบาที่สุด

**AU-6 · กฎอีเมล (F5.2.3)**

1. `var email = input.Email.Trim();` (แบบเดียวกับ `Create`, `IAdminUserService.cs:65`)
2. **"เปลี่ยนจริง" = `!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)`**
   — `GetByEmail` เทียบแบบ case-insensitive อยู่แล้ว (`IAdminUserRepository.cs:38-39`) การแก้
   เฉพาะตัวพิมพ์จึงไม่ใช่การย้ายบัญชีไปหาใคร **จึงต้องไม่ติดธง** (ถ้าติด = ล็อกคนออกจากระบบ
   เพราะ CS เผลอพิมพ์ตัวใหญ่)
3. ถ้าเปลี่ยนจริง → `_users.GetByEmail(email)` · เจอแถวที่ **`Id != user.Id`** →
   `throw GeneralException.ValidationError("อีเมลนี้ถูกใช้งานแล้ว")` — **ข้อความเดียวกับ `Create`
   คำต่อคำ** (`IAdminUserService.cs:68`) ห้ามแต่งใหม่ ห้ามปล่อยให้เป็น 500 จาก unique index
   · เงื่อนไข `Id != user.Id` สำคัญ: ไม่งั้นการบันทึกโดยไม่แตะอีเมลจะฟ้องว่าอีเมลตัวเองซ้ำ
4. เขียน `user.Email = email` **เสมอ** (แม้เปลี่ยนแค่ตัวพิมพ์ — normalize ค่า trim ลงไป)
5. ติดธงตาม AU-8 **เฉพาะเมื่อข้อ 2 เป็นจริง**
6. **ไม่ยืนยันอีเมลใหม่ ไม่ส่งลิงก์** (ระบบไม่มี SMTP · F5.2.3)
7. ⚠️ ข้อความ "อีเมลนี้ถูกใช้งานแล้ว" **บอกได้ว่ามีบัญชีนี้อยู่ในระบบ รวมถึงในบริษัทอื่น** —
   เป็นพฤติกรรมเดิมของ `Create` ที่ยอมรับไว้แล้ว ไม่ใช่ของใหม่ที่ Module U สร้าง แต่
   **`security` ต้องเห็นข้อนี้ตอน audit** (R-20)

**AU-7 · กฎรหัสผ่าน (F5.2.2)**

- **ห้ามใส่ `[Required]`/`[MinLength]` บน `NewPassword`** — data annotation แสดงกฎ
  "ว่าง = ไม่รีเซ็ต" ไม่ได้ · ถ้าใส่ `[MinLength(10)]` ฟอร์มที่ส่ง `""` (ช่องที่ผู้ใช้ไม่แตะ)
  จะได้ 400 ทั้งที่ผู้ใช้ไม่ได้ตั้งใจรีเซ็ตอะไรเลย และจะเกิดพฤติกรรมประหลาดที่
  "ช่องว่าง 3 ตัว = 400 แต่ช่องว่าง 10 ตัว = ผ่าน" · กฎทั้งหมดอยู่ใน service ที่เดียว
- ลำดับในเซอร์วิส:
  1. `string.IsNullOrWhiteSpace(input.NewPassword)` → **ไม่รีเซ็ต** ข้ามทั้งบล็อก ไม่ error
  2. ไม่งั้น `input.NewPassword.Length < PasswordRules.MinLength` →
     `throw GeneralException.ValidationError(PasswordRules.TooShortTh)` (ใช้ค่าคงที่เดิมใน
     `AuthDto.cs:31-35` ห้าม hardcode เลข 10 หรือข้อความใหม่)
  3. `user.PasswordHash = passwordHasher.HashPassword(user, input.NewPassword);`
     **ใช้ค่าดิบ ห้าม `Trim()`** — ช่องว่างหัวท้ายเป็นส่วนหนึ่งของรหัสผ่านได้จริง
     (ต่างจากอีเมลที่ trim ตาม AU-6 โดยเจตนา)
  4. ติดธงตาม AU-8
- **ไม่มีตัวสุ่มรหัส ไม่มีการแสดงรหัสซ้ำ ไม่มีลิงก์รีเซ็ตทางอีเมล** (F5.2.2) · การส่งมอบรหัส
  ชั่วคราวเกิดนอกระบบ เหมือน admin คนแรกของบริษัท (A1 · R-9)
- **ห้ามคืนรหัสผ่านใน response และห้าม log** (แนวเดียวกับ CP-10/CP-11) · log line เดิมที่
  `IAdminUserService.cs:132-134` เพิ่มได้เฉพาะ **boolean** `passwordReset={bool}`
  `emailChanged={bool}` — **ห้าม log ค่าอีเมลใหม่และห้าม log รหัสผ่านทุกกรณี**

**AU-8 · สองเส้นทางเดียวกันของ `MustChangePassword` (F5.2.4) — ไม่มีกลไก auth ใหม่แม้แต่ชิ้นเดียว**

- `user.MustChangePassword = true` เมื่อ **(AU-6 ข้อ 2 เป็นจริง) หรือ (AU-7 ข้อ 3 ทำงาน)**
  · ทำทั้งสองอย่างในการบันทึกครั้งเดียว = ธงเดียวกัน ไม่มีพฤติกรรมพิเศษเพิ่ม
- **ถ้าไม่เกิดทั้งสองอย่าง ห้ามแตะฟิลด์นี้เลย** (ห้ามเซ็ต `false`) — บัญชีที่ค้างธงอยู่แล้วต้อง
  ไม่ถูกปลดธงเพราะมีคนมาแก้ role ให้
- **กลไกบังคับที่มีอยู่แล้วและใช้ต่อได้ 100%** (ตรวจโค้ดจริง 2026-08-25):
  `CurrentUserMiddleware` เรียก `authService.RefreshCurrentUser()` **ทุก request**
  (`CurrentUserMiddleware.cs:52`) ซึ่งอ่าน `AdminUser` จาก DB ใหม่ทุกครั้ง แล้ว throw 403
  `"กรุณาเปลี่ยนรหัสผ่านก่อนใช้งานระบบ"` เมื่อธงเป็น true โดยอนุญาตเพียงสอง path คือ
  `/api/auth/me` และ `/api/auth/change-password` (`CurrentUserMiddleware.cs:54-58, 99-100`)
- ผลที่ตามมาที่ต้องเข้าใจให้ตรงกัน: **token ที่เป้าหมายถืออยู่ไม่ถูกเพิกถอนและยังไม่หมดอายุ
  แต่ใช้ทำอะไรไม่ได้เลยจนกว่าจะเปลี่ยนรหัส** → **ห้ามสร้าง token blacklist / revocation list /
  ตาราง session ใด ๆ** เพื่องานนี้
- ธงถูกล้างที่เดียวคือ `AuthService.ChangePassword` (`IAuthService.cs:215`) ซึ่ง**ยังบังคับให้
  รู้รหัสปัจจุบัน** = รหัสชั่วคราวที่เพิ่งตั้งให้ · **ห้ามผ่อนข้อบังคับนั้น** (F5.2.7)

**AU-9 · เคสที่ทำให้บัญชีถูกล็อกถาวร — ต้องปฏิเสธ ไม่ใช่ปล่อยผ่าน**

`AdminUser.PasswordHash` เป็น **nullable โดยเจตนา** (บัญชีที่ล็อกอินด้วย SSO อย่างเดียว —
`AdminUser.cs:38-43`) · ถ้าเป้าหมายมี `PasswordHash == null` แล้วคำขอนี้จะติดธงตาม AU-8
**โดยไม่ได้ตั้งรหัสผ่านมาในคำขอเดียวกัน** เจ้าตัวจะล้างธงไม่ได้ตลอดไป เพราะ `ChangePassword`
ต้องการรหัสปัจจุบันมา verify แต่ไม่มี hash ให้ verify → **บัญชีตายถาวร ต้องแก้ที่ DB มือ**

**กฎ**: ในกรณีนั้นให้ `throw GeneralException.ValidationError("บัญชีนี้ยังไม่มีรหัสผ่านในระบบ กรุณาตั้งรหัสผ่านชั่วคราวมาพร้อมกับการแก้ไขครั้งนี้")`
· วันนี้ยังไม่มีบัญชีแบบนี้จริงในระบบ (ยังไม่มี SSO) แต่คอลัมน์เปิดช่องไว้แล้ว — **เขียนกฎไว้
เพื่อไม่ให้ engineer ต้องเดา ไม่ใช่เพราะเคสนี้เกิดบ่อย**

**AU-10 · บันทึกว่าใครแก้ + หนึ่ง transaction (F5.2.5)**

- `user.UpdateBy = currentUser.UserId` · `user.UpdateDate = DateTime.UtcNow` — โค้ดเดิมทำอยู่แล้ว
  (`IAdminUserService.cs:126-127`) **ไม่ต้องเพิ่มอะไร**
- **ไม่มีตาราง audit ใหม่** (F5.2.5) · ข้อจำกัดที่ยอมรับแล้ว: เก็บได้แค่ "คนล่าสุด" และแยก
  "รีเซ็ตรหัส" ออกจาก "แก้อีเมล" ในบันทึกไม่ได้
- **`UnitOfWork.Commit()` ครั้งเดียวที่ท้ายสุดเท่านั้น** (โค้ดเดิมเป็นแบบนี้อยู่แล้ว) — อีเมลซ้ำ
  ต้องทำให้ **ไม่มีอะไรถูกบันทึกเลย รวมถึงรหัสผ่านใหม่** แนวเดียวกับ CP-6

**AU-11 · กฎเดิมที่ยังบังคับครบ ห้ามแตะ**

`EnsureNotRemovingLastGuardian` (`IAdminUserService.cs:150-169`) และ branch "แถวเสีย
CompanyId ว่าง" ยังทำงานเหมือนเดิมทุกประการ · Module U **ไม่แก้ทั้งสองอย่าง** — เพิ่มเฉพาะ
ข้อ 4/5 ของ AU-3 เข้าไปในลำดับ

**AU-12 · ตารางผลลัพธ์ครบทุกเคส — engineer ไม่ต้องอนุมานเอง**

| ผู้เรียก \ เป้าหมาย | `owner` | `admin` บริษัทเดียวกัน | `admin` บริษัทอื่น | `cs` บริษัทเดียวกัน | `cs` บริษัทอื่น | บัญชีตัวเอง |
|---|---|---|---|---|---|---|
| `owner` | ✅ (ข้อยกเว้น F5.2.1) | ✅ | ✅ | ✅ | ✅ | ❌ **AU-5** |
| `admin` | ❌ `EnsureOwner` | ❌ **AU-4** | ❌ `EnsureCanManageUsers` | ✅ | ❌ `EnsureCanManageUsers` | ❌ **AU-5** |
| `cs` | ❌ `EnsureOwner` | ❌ `EnsureCanManageUsers` | ❌ `EnsureCanManageUsers` | ❌ `EnsureCanManageUsers` | ❌ `EnsureCanManageUsers` | ❌ `EnsureCanManageUsers` |

- แถว `cs` ถูกปฏิเสธด้วยกฎ**เดิม**ทั้งแถว ก่อนจะไปถึงกฎใหม่เลย — และนั่นคือข้อความที่ถูกต้อง
  (`"ไม่มีสิทธิ์จัดการผู้ใช้"`) ตามหลักการเดียวกับคอมเมนต์ลำดับใน `Create`
- ⚠️ **ช่อง `owner` × `owner` เป็นจริงที่ระดับ server แต่ _วันนี้ไม่มีหน้าจอไปถึง_** —
  `GetByCompanyId` กรอง `CompanyId == companyId` และบัญชี `owner` มี `CompanyId = null` เสมอ
  จึงไม่เคยปรากฏในตาราง `/admin/users` เลย (`IAdminUserRepository.cs:41-42` + `AdminUser.cs:23-28`)
  → **OQ-U1** · **ห้าม engineer แก้ `GetByCompanyId` เองเพื่อให้ owner โผล่**

**AU-13 · frontend: โมดัลนี้ _แทนที่_ ของเดิม (F5.2.6) — เป็นการลบ ไม่ใช่การเพิ่ม**

สิ่งที่ต้อง **ลบออกจริง** จาก `frontend/src/app/admin/users/page.tsx` (เลขบรรทัด ณ 2026-08-25):
- `<Select>` เปลี่ยน role ในแถว (`page.tsx:194-211`)
- ปุ่ม "ปิดบัญชี"/"เปิดบัญชี" ในแถว (`page.tsx:218-231`)
- ฟังก์ชัน `UserRow.apply()` ที่ยิง `updateAdminUser` แบบ partial (`page.tsx:167-184`)

**ห้ามซ่อนไว้เฉย ๆ ห้ามปล่อยให้อยู่คู่กัน** — F5.2.6 เลือกทางเลือก "แทนที่" โดยตรงเพื่อไม่ให้มี
ตรรกะสิทธิ์เดียวกันสองที่ · เหลือ **ปุ่มเดียวต่อแถว** ที่เปิดโมดัล "จัดการผู้ใช้"

โมดัล:
- ใช้แบบแผนเดียวกับ `CreateUserDialog` ที่อยู่ในไฟล์เดียวกันแล้ว (`Dialog`/`DialogContent`/
  `DialogHeader` + `form` + `Alert variant="destructive"` แสดงข้อความจาก server ตรง ๆ)
  **ห้ามคิด pattern ใหม่**
- ฟิลด์: **อีเมล** (prefill ค่าเดิม) · **รหัสผ่านใหม่** (ว่างไว้ = ไม่รีเซ็ต — ต้องมีข้อความ
  อธิบายใต้ช่องให้ชัด ไม่ใช่ปล่อยให้เดา) · **สิทธิ์** (`Select` จำกัดด้วย `assignableRoles` เดิม)
  · **สถานะเปิด/ปิดบัญชี**
- ⚠️ **ช่อง "สถานะเปิด/ปิดบัญชี" ต้องมี แม้ไฟล์ Figma จะวาดไว้แค่สามช่องแรก** — F5.2.6 ย้าย
  ปุ่มเปิด/ปิดเข้ามาในโมดัล ถ้าโมดัลไม่มี ก็จะไม่เหลือทางปิดบัญชีใครได้เลยทั้งระบบ
  (หน้าตาของ control นี้เป็นเรื่อง cosmetic แบบเดียวกับ OQ-15 — **ความสามารถไม่ใช่ cosmetic**)
- **ไม่มีช่อง "ชื่อที่แสดง"** ตามไฟล์ Figma → ส่ง `displayName` ค่าเดิมกลับไปไม่เปลี่ยน
  (แบบเดียวกับที่ `UserRow.apply()` ทำอยู่วันนี้) · ดู **OQ-U3**
- **ปุ่ม Cancel: ห้ามตั้งข้อความเอง** จนกว่า OQ-15 จะถูกเคาะกับดีไซเนอร์ (F5.4)
- `api-client.ts` `updateAdminUser` ต้องรับ `email` (บังคับ) และ `newPassword` (optional)
  ให้ตรง AU-2 · TypeScript type กับ DTO ต้องแก้คู่กันตาม `CLAUDE.md` §Architecture Rules ข้อ 7

**AU-14 · frontend: ปุ่มโผล่เมื่อไหร่ — และการซ่อนไม่ใช่การกั้นสิทธิ์**

- แสดงปุ่ม "จัดการ" ในแถวเมื่อ **ทั้งสองข้อเป็นจริง**: (ก) `row.id !== user.id` (AU-5)
  (ข) `user.role === "owner" || row.role !== user.role` (AU-4)
- **นี่คือ courtesy ไม่ใช่ด่าน** — ด่านจริงคือ AU-3..AU-5 ที่ server · หลักการเดียวกับ SP-15 ข้อ 6
  และ R-15 · **ห้ามลดการตรวจฝั่ง server ลงเพราะ "UI ซ่อนให้แล้ว"**
- `cs` ยังเห็นข้อความ `"บัญชีของคุณไม่มีสิทธิ์จัดการผู้ใช้"` เหมือนเดิม (`page.tsx:73-79`) ไม่ต้องแก้

**AU-15 · unit test ที่ต้องมี (ไม่ใช่ทางเลือก)**

ในโปรเจกต์ที่ไม่มี integration test ยืนยัน endpoint จริง ชุดนี้คือหลักฐานเดียวที่บอกว่ากฎทำงาน:

1. `admin` → `admin` คนอื่นบริษัทเดียวกัน = **Forbidden** (AU-4)
2. `owner` → `owner` คนอื่น = **ผ่าน** (ข้อยกเว้น F5.2.1)
3. ผู้เรียก → บัญชีตัวเอง = **Forbidden** ทดสอบทั้ง `owner` และ `admin` (AU-5)
4. เปลี่ยนอีเมลอย่างเดียว → `MustChangePassword == true`
5. รีเซ็ตรหัสอย่างเดียว → `MustChangePassword == true` และ `PasswordHash` เปลี่ยนค่า
6. แก้แค่ `role`/`IsActive` → **`MustChangePassword` ไม่ถูกแตะ** (ทดสอบทั้งกรณีธงเดิม `false`
   และธงเดิม `true`)
7. อีเมลเปลี่ยนเฉพาะตัวพิมพ์ → **ไม่ติดธง** (AU-6 ข้อ 2)
8. อีเมลซ้ำกับผู้ใช้อื่น → `ValidationError` และ **ไม่มีฟิลด์ใดถูกบันทึก รวมถึงรหัสผ่าน** (AU-10)
9. `NewPassword` เป็น `null` และเป็นช่องว่างล้วน → ไม่รีเซ็ต ไม่ error
10. `PasswordHash == null` + เปลี่ยนอีเมลโดยไม่ตั้งรหัส → `ValidationError` (AU-9)
11. **regression**: `EnsureNotRemovingLastGuardian` ยังทำงานทั้งสองเคส (owner คนสุดท้าย /
    admin คนสุดท้ายของบริษัท) — เป็นโค้ดที่ QA ผ่านไปแล้วและถูกแทรกลำดับใหม่เข้าไปข้างหน้า

**AU-16 · สิ่งที่ห้ามทำในงานนี้**

- **ห้ามเพิ่ม endpoint ใหม่ ห้ามเพิ่ม route ใหม่** (AU-1/AU-2)
- **ห้ามเพิ่มคอลัมน์/ตาราง/migration ใด ๆ** (F5.3 · R-4) — ถ้ารู้สึกว่าต้องมี แปลว่าเข้าใจ AU-*
  ผิด ให้ตีกลับ `system-analyst`
- **ห้ามทำ audit log** (F5.2.5)
- **ห้ามสุ่มรหัสผ่าน ห้ามส่งอีเมล ห้ามทำลิงก์รีเซ็ต** — ระบบไม่มี SMTP (F5.2.2)
- **ห้ามแตะ `POST /api/auth/change-password` และห้ามผ่อนกฎ "ต้องรู้รหัสเดิม"** (F5.2.7)
- **ห้ามสร้าง token revocation/blacklist/ตาราง session** (AU-8)
- **ห้ามแก้ `EnsureCanManageUsers`/`EnsureCanAssignRole`/`EnsureOwner`/`AdminRole`** — เพิ่ม
  `EnsureNotSameRankPeer` ได้อย่างเดียว (CP-15 ที่แก้ขอบเขตแล้ว)
- **ห้ามแก้ `GetByCompanyId` ให้ `owner` โผล่ในตาราง** (OQ-U1 ยังไม่เคาะ)
- **ห้ามเรียก `EnsureNotSameRankPeer` ใน `Create`** (AU-4)
- **ห้ามตั้งข้อความปุ่ม Cancel เอง** (OQ-15 · F5.4)

## Contract sections ที่เลื่อนไปพร้อม F2 (Module B) — ยังไม่เขียน

`## Company Settings Resolution Rules` (ขึ้นกับ A3/A4/A5 ส่วนที่เหลือ/A6 — **B4 เคาะแล้ว 2026-08-22
ไม่ใช่ตัวบล็อกอีกต่อไป**) และ `## Brand Delivery Rules` (ขึ้นกับ A2/B3b — **B2 เคาะแล้วรอบก่อน
ใช้ต่อได้เลยตอนนั้น**)

**ค่า pacing ไม่อยู่ในกลุ่มนี้แล้ว** — เขียนเสร็จแล้วที่ `## Lesson Pacing Resolution Rules`
(LP-1..LP-15) · ตอนปลุก Module B ส่วนที่เหลือ ให้ใช้ LP-1..LP-2 เป็นตัวอย่างของ "กฎ null คนละแบบ
ในตารางเดียว" **ไม่ใช่คัดลอกกฎ non-null ของ pacing ไปใช้กับค่าอื่น** — ค่าอื่นเป็น `null = inherit` ตาม F2.2

**อัปเดต 2026-08-22 (P6) — "โครงหน้าจอ" ไม่อยู่ในกลุ่มที่เลื่อนแล้วเช่นกัน** · `## Company Settings
Page Rules` (SP-1..SP-14) เขียนเสร็จแล้วและครอบ **ตัวหน้า `/admin/settings` + วิธีเพิ่ม section**
ไว้ครบ · สิ่งที่ยังเลื่อนคือ **เนื้อของแต่ละ section ที่เหลือ** เท่านั้น — ตอนปลุกค่าใดค่าหนึ่งของ F2
งานที่ต้องทำคือ **เขียน contract ของค่านั้นแล้วเพิ่ม section ตาม SP-2 ไม่ใช่ออกแบบหน้าจอใหม่**

## Modules

> **Module A ผ่าน GAP_ANALYSIS แล้ว** (รอบ 2026-08-21) — เหลือแค่กฎธุรกิจ 5 ข้อที่ยังไม่เคาะ
> **Module B/C ยังเป็นข้อเสนอและยังพักไว้** `project-manager` วางแผนได้เฉพาะ Module A
> และเฉพาะหลังจาก A1/B1/B2/N1/N2 ถูกเคาะแล้วเท่านั้น
>
> **ไม่แตก Module A ออกเป็นหลาย module** — F1.1–F1.6 ใช้ตารางชุดเดียวกัน อยู่ใน service เดียวกัน
> และส่งมอบครึ่งเดียวไม่ได้ (สร้างบริษัทได้แต่ดูรายการไม่ได้ = ยังต้องเปิด DB อยู่ดี)
> ขนาดงานเล็กพอที่จะส่งเป็นก้อนเดียวโดยไม่เสี่ยง ตามกฎ GAP_ANALYSIS ข้อ 2
>
> **อัปเดต 2026-08-22 — GAP_ANALYSIS รอบใหม่เฉพาะค่า pacing**: เกิด **Module P** ขึ้นมาอีกก้อน
> `project-manager` วางแผนได้แล้วทั้ง **Module A** (ทำไปแล้ว) และ **Module P** (ยังไม่มี phase)
> · **ไม่แตก Module P ออกเป็นหลาย module เช่นกัน** — migration/backend/frontend ของมันแยกส่งมอบ
> ไม่ได้จริง (ปล่อย migration ไปโดยไม่มี resolver = บทเรียนที่ `null` ไม่มีค่าให้ใช้ · ปล่อย backend
> ไปโดยไม่แก้ฟอร์ม = CS ยังกรอกเลขซ้ำเหมือนเดิม ซึ่งคือปัญหาทั้งหมดที่ขอให้แก้) — **แต่ซอยเป็น
> หลาย phase ภายใน module เดียวได้ตามที่ `project-manager` เห็นควร** ตราบใดที่ phase ที่มี
> migration กับ phase ที่มี resolver อยู่ลำดับติดกันและ deploy พร้อมกัน
>
> **อัปเดต 2026-08-22 (รอบที่ 7)**: ข้อบังคับ "ติดกันและ deploy พร้อมกัน" **เข้มขึ้น ไม่ใช่ผ่อนลง**
> — ใบ `RemoveLessonConfigPacingOverrides` ลบคอลัมน์จริง ถ้าโค้ดที่ยัง `SELECT` คอลัมน์นั้นถูก
> deploy คู่กันผิดลำดับ **ทุก query ของ `LessonConfig` พังทั้งตาราง ไม่ใช่แค่ฟีเจอร์ pacing**
> (ดู Migration Plan ข้อบังคับข้อ 4)

### Module A · Company Provisioning — 🟢 อยู่ในสโคปรอบนี้

F1.1–F1.6 · แตะ `Company`, `AdminUser`, `KnowledgeCategory` (ทั้งหมดเป็นตารางที่มีอยู่แล้ว
**ไม่แก้โครงสร้างใดๆ**) · ไม่ขึ้นกับ module อื่น

**งานที่ต้องสร้างใหม่** (สรุปจาก Findings — เป็นระดับ service/endpoint/UI ทั้งหมด ไม่มี schema):
1. Service สร้างบริษัทแบบ transaction เดียว (Company + AdminUser + default chain)
2. Service/repository สร้าง default category chain ที่ **runtime เรียกได้** — วันนี้ไม่มีเลย มีแต่ใน migration (P3/F-3)
3. **Endpoint ใหม่สำหรับ owner ที่คืนบริษัททั้งหมดรวมตัวที่ปิดแล้ว** — `GET /api/companies` เดิมใช้ไม่ได้ (F-1)
4. ขยาย payload ของ `POST /api/companies` ให้รับข้อมูล admin คนแรก
5. UI: หน้าสร้างบริษัท + หน้ารายการบริษัท + ปุ่มปิด/เปิดใช้งาน (ต่อกับ `createCompany()`/`updateCompany()` ที่ประกาศไว้แล้วแต่ยังไม่มีใครเรียก — `api-client.ts:559,563`)
6. (มีเงื่อนไข B1) migration data-only ซ่อม default chain ของบริษัทเดิม
7. (มีเงื่อนไข B2) เพิ่มการเช็ค company IsActive ในเส้นทางฝั่งผู้เรียน

#### 🔒 Security gate — คำสั่งถึง `project-manager`

**ทุก phase ที่ implement Module A ต้องมี `🔒 Security gate` ต่อท้ายหัวข้อ phase ใน `plan.md`**
เช่น `## Phase 1: สร้างบริษัทใหม่ (backend) 🔒 Security gate` — **ไม่มีข้อยกเว้น ไม่มี phase ไหน
ของโมดูลนี้ที่ปลอดจาก gate** เพราะแม้แต่ phase ที่ทำแค่ UI ก็ต่อกับ endpoint ที่สร้าง tenant/บัญชี

**เหตุผลที่ต้องระบุใน `plan.md` ด้วย** (ไม่ใช่แค่ติดสัญลักษณ์ — `security` และ `devops` อ่านบรรทัดนี้):
1. **สร้าง tenant ใหม่** — เป็น endpoint เดียวในระบบที่เพิ่มลูกค้ารายใหม่ได้
2. **สร้างบัญชีผู้ใช้ + รับรหัสผ่านจากฟอร์ม** — personal data (อีเมล) + credential ในคำขอเดียว
3. **แตะ `Company` และ `AdminUser` ซึ่งเป็นสองตารางเดียวในระบบที่ไม่มี query filter โดยเจตนา**
   `IAuthorizationGuard` คือแนวป้องกันชั้นเดียว ไม่มีอะไรรองข้างหลัง (TD-014,
   `ApplicationDbContext.cs:48-54`, `IAuthorizationGuard.cs:7-18`)
4. **`Role` ถูก hardcode เป็น `admin` (CP-8)** — ถ้าใครเผลอเปิดให้ request กำหนดได้ จะกลายเป็น
   ช่องสร้าง `owner` ที่มองไม่เห็นในหน้า `/admin/users`

**ผลตามมาที่ PM ต้องรู้**: phase ที่ติด gate จะ **deploy ไม่ได้จนกว่า `security` จะ audit จริง**
(`devops` บล็อก) และ `qa-engineer` จะรายงาน gate ค้างทุกรอบจนกว่าจะมีคนเรียก `security`
· **`security` ไม่ถูกเรียกอัตโนมัติในทุกโหมด** ผู้ใช้ต้องเรียกเองด้วยชื่อ — PM ควรเขียนไว้ใน
`plan.md` §Sequencing Notes ว่าต้องเรียกเมื่อไหร่ ไม่ใช่ปล่อยให้ไปรู้ตอน deploy

**หมายเหตุสำหรับ `security` ตอน audit**: จุดที่ควรดูหนักที่สุดคือ CP-1 (guard มาก่อนทุกอย่าง),
CP-5 (ข้อความอีเมลซ้ำต้องไม่ enumerate), CP-8 (`Role` ตายตัว), CP-10/CP-11 (ห้ามคืน/ห้าม log
รหัสผ่านและอีเมล), CP-12 (ห้าม `IgnoreQueryFilters()`), CP-13 (endpoint ใหม่ต้อง owner-only)
· และ **R-2 ยังเปิดอยู่** — ไม่มี rate limiting บน endpoint นี้

### Module P · Lesson Pacing Defaults (ระดับบริษัท) — 🟢 อยู่ในสโคปแล้ว (ปลุก 2026-08-22)

ค่า `introWaitMs`/`breathPauseMs`/`finalQuestionWaitMs` ย้ายจาก "ตั้งต่อบทเรียนอย่างเดียว"
เป็น **"ค่ากลางระดับบริษัทล้วน ไม่มี override ต่อบทเรียน"** (~~+ บทเรียน override ได้~~ —
กลับคำตอบ 2026-08-22 รอบที่สอง · N1) · **แตะ `Company` (เพิ่ม 3 คอลัมน์ NOT NULL — ไม่เปลี่ยน)
และ `LessonConfig` (~~3 คอลัมน์เป็น nullable~~ → **ลบ 3 คอลัมน์ทิ้ง** · N3)**
· contract = `## Lesson Pacing Resolution Rules`

**ทำไมแยกออกมาจาก Module B แทนที่จะรอไปด้วยกัน**: P3 ยืนยันว่าไม่ต้องรอหน้า UI ตั้งค่าบริษัท
และคำถามที่ยังเปิดของ Module B (A2 แบรนด์ · A3/A4 TTS · A6 สิทธิ์ `cs` เห็นหน้าตั้งค่า · B3b
ค่ากลางของแบรนด์) **ไม่มีข้อไหนแตะค่า pacing เลย** — มัดรวมกันจะกลายเป็นการเอางานที่เคาะครบแล้ว
ไปติดอยู่กับงานที่ยังเคาะไม่ครบ

**ขึ้นกับ Module A แบบ "โค้ดที่ implement ไปแล้ว" ไม่ใช่ "งานที่ต้องทำก่อน"** — Module A
implement เสร็จแล้ว (Phase 1–3 verified ✅) · งานนี้ **แก้โค้ดของ Module A จริงสองบรรทัดกลุ่มเดียว**
คือจุด `new Company` ทั้งสองจุด (CP-16/LP-2) → `qa-engineer` ต้องถือว่าเป็น **regression surface
ของ Phase 1** ไม่ใช่โค้ดใหม่ล้วน

**งานที่ต้องทำ** (สรุปจาก LP-*):
1. migration `AddCompanyLessonPacingDefaults` ใบเดียว (DM-P1 + DM-P2)
2. entity + `ApplicationDbContext` mapping
3. `ILessonPacingResolver` + จุดเรียกจุดเดียวใน `GetTeachingContentByLinkAsync` (LP-4)
4. `CP-16`/`LP-2` — ตั้งค่าที่ `Create` และ `SeedFirstCompanyIfEmpty`
5. `GET`/`PUT /api/companies/{companyId}/lesson-pacing` (LP-9)
6. DTO/ViewModel/TypeScript types เป็น nullable ฝั่งแอดมิน · ฝั่งผู้เรียนคงเดิม (LP-5/LP-12)
7. ฟอร์มบทเรียน: แยกว่าง/ศูนย์ + placeholder + ฟอร์มสร้างใหม่เริ่มเป็นค่าว่าง (LP-11)
8. แก้ค่า fallback ที่เพี้ยนใน `use-tutor-session.ts` (LP-13)
9. unit tests ตาม LP-14
10. **(เพิ่ม 2026-08-22 · มติ P6)** หน้า `/admin/settings` แบบขยายได้ + section `pacing` เป็น
    section แรกและ section เดียวของรอบนี้ + เมนูใน sidebar + `updateCompanyLessonPacing()`
    ใน `api-client.ts` + test ของ SP-14 — contract = `## Company Settings Page Rules`
    · **frontend ล้วน 100% ไม่มีงาน backend ใหม่เลย** (endpoint LP-9 เสร็จและทดสอบสดไปแล้ว)
    · ข้อ 1–9 ข้างบน implement ไปแล้วและรอ QA อยู่ — **ข้อ 10 เป็นงานที่ยังไม่ได้ทำ**
    เพราะตอนนั้น LP-15 ยังห้ามอยู่
    · **อัปเดต 2026-08-22 (มติ A8)**: ข้อ 10 รวม **`section-access.ts` + registry `sections.ts`
    ตาม SP-15** ด้วย (แกน `visibleToRoles`/`editableByRoles` แยกกัน + เมนู sidebar derive จาก
    registry + test `resolveSectionAccess`) — **ยังเป็น frontend ล้วนเหมือนเดิม ไม่มีงาน backend
    เพิ่มแม้แต่บรรทัดเดียว และค่าของ section pacing ไม่เปลี่ยนจากเดิมเลย**

**🔄 อัปเดต 2026-08-22 (รอบที่ 7 · มติ N1/N2/N3) — งานของ Module P เปลี่ยนไปสามกลุ่ม:**

| กลุ่ม | สถานะ |
|---|---|
| **ยังถูกต้อง ไม่ต้องทำซ้ำ** (ข้อ 2 บางส่วน · 4 · 5 · 8 · 10 ทั้งข้อ) | `Company` + 3 คอลัมน์ NOT NULL (DM-P1) · การตั้งค่าที่ `Create`/`SeedFirstCompanyIfEmpty` (CP-16/LP-2) · endpoint `GET`/`PUT` (LP-9) · fallback ที่แก้แล้วใน `use-tutor-session.ts` (LP-13) · หน้า `/admin/settings` + section pacing + registry (SP-1..SP-15) |
| **ต้องถอด/แก้ย้อนหลัง** (ข้อ 1 บางส่วน · 3 · 6 · 7 · 9) | migration ใบใหม่ `RemoveLessonConfigPacingOverrides` + entity/mapping/snapshot ไม่มีสามฟิลด์ (DM-P2) · จุดอ่านค่าเปลี่ยนเป็นอ่านจาก `Company` ตรง ๆ (LP-4 · ชะตากรรมของ `ILessonPacingResolver` ให้ PM/engineer ตัดสิน) · `SaveAsync` ไม่แตะ pacing (LP-6 ยกเลิก) · DTO/ViewModel/`domain.ts` ลบสามฟิลด์ (LP-5/LP-12) · ฟอร์มบทเรียนถอดสามช่อง + เลิกเรียก `getCompanyLessonPacing()` (LP-11 ใหม่) · test ของ resolver สองชั้นและ test `SaveAsync` null **ต้องถูกลบ** พร้อม test ใหม่ตาม LP-14 ข้อ 1 |
| **งานใหม่ที่ไม่เคยมีในรายการเดิม** | ข้อบังคับ 5 ข้อของ migration ใบใหม่ (คอมเมนต์เจตนา · แยกใบ · down กู้รูปร่างไม่กู้ข้อมูล · ลำดับ deploy · backup ก่อนรัน) |

⛔ **ทั้งหมดนี้ยังไม่เคยผ่าน QA สักรอบ** — Phase 4/5 อยู่ในสถานะ "implemented รอ QA" มาตลอด
· `project-manager` เป็นผู้ตัดสินว่า **แก้ในเฟสเดิมหรือเปิดเฟสใหม่ทับ** และเป็นผู้เขียนลำดับ
· **`system-analyst` ไม่ตัดสินข้อนี้ และ engineer ห้ามหยิบไปทำก่อน `plan.md` ถูกอัปเดต**

#### 🔒 Security gate — คำสั่งถึง `project-manager`

**phase ที่ implement Module P ต้องมี `🔒 Security gate` เหมือนกัน** — เหตุผลต่างจาก Module A
และต้องเขียนลง `plan.md` ให้ `security`/`devops` อ่านได้:

1. **เป็น endpoint แรกของระบบที่ให้ `admin` (ไม่ใช่แค่ owner) เขียนค่าลงแถว `Company`** —
   ตารางที่ไม่มี query filter รองหลัง (R-1) · `guard.EnsureCanAccessCompany` เป็นด่านเดียว
2. **`companyId` มาจาก path parameter** ไม่ใช่จาก JWT — ถ้า guard หลุด/เรียกผิดลำดับ
   จะกลายเป็นการแก้ค่าข้ามลูกค้าได้ทันที
3. **ต้องยืนยันว่า `cs` ถูกปฏิเสธที่ `PUT` จริง** (LP-9) — `cs` อ่านได้แต่เขียนไม่ได้
   · ~~เป็นความต่างที่ทดสอบด้วยตาจาก UI ไม่ได้ เพราะรอบนี้ยังไม่มีหน้าจอตั้งค่า~~
   → **อัปเดต 2026-08-22 (P6): มีหน้าจอแล้ว** (SP-1) จึงตรวจด้วยตาได้ **แต่การกดจาก UI ไม่ใช่
   หลักฐานเพียงพอ** — SP-4 ซ่อนปุ่มบันทึกจาก `cs` ตั้งแต่ที่จอ ฉะนั้นการที่ "กดไม่ได้"
   ไม่ได้พิสูจน์ว่า server ปฏิเสธจริง · **`security`/`qa-engineer` ต้องยิง `PUT` ตรงด้วย JWT ของ `cs`
   เหมือนเดิม** ไม่ใช่ทดสอบผ่านจอ
4. **(เพิ่ม 2026-08-22)** หน้า `/admin/settings` เป็นจอแรกที่ `cs` เปิดได้ในกลุ่มเมนู "ตั้งค่า"
   ที่เดิมปิดทั้งกลุ่มจาก `cs` (SP-5) — จุดที่ต้องดูคือ **การขยับ gate ทำให้ `cs` เห็นเมนู
   "ผู้ใช้งาน" (`/admin/users`) ติดมาด้วยหรือไม่** ซึ่งจะเป็นการเปิดหน้าจัดการบัญชีให้ role
   ที่ไม่ควรเห็น

### Module U · Admin User Account Management (F5) — 🟢 อยู่ในสโคปแล้ว (เปิด 2026-08-25)

แก้อีเมล / รีเซ็ตรหัสผ่าน / เปลี่ยน role / เปิด-ปิดบัญชี **ของผู้ใช้รายอื่น** ในโมดัลบนหน้า
`/admin/users` เดิม · แตะ `AdminUser` (**ไม่แก้โครงสร้าง ไม่มี migration** — `## Data Model` §Module U)
· contract = **`## Admin User Management Rules` (AU-1..AU-16)**

**GAP_ANALYSIS — ไม่แตกเป็นหลาย module** · งานทั้งก้อนอยู่บน entity เดียว service เดียว
endpoint เดียว หน้าจอเดียว · ส่งมอบครึ่งเดียวไม่ได้จริง: ปล่อย backend ไปโดยไม่แก้ frontend =
`UserRow.apply()` เดิมยิงคำขอที่ไม่มี `email` แล้วได้ 400 ทุกครั้ง (**หน้าเดิมพังทันที** — R-19)
· ปล่อย frontend ไปก่อน = โมดัลส่งฟิลด์ที่ server ยังไม่รู้จัก · **แต่ซอยเป็นหลาย phase ภายใน
module เดียวได้ตามที่ `project-manager` เห็นควร** ตราบใดที่ phase backend กับ phase frontend
**ถูก deploy พร้อมกัน**

**ขึ้นกับ Module A แบบ "โค้ดที่ implement และ verify ไปแล้ว" ไม่ใช่ "งานที่ต้องทำก่อน"** —
`AdminUserService`/`AdminUserDto`/`/admin/users` เป็นของ baseline เดิมและถูก QA ผ่านไปแล้ว
(Phase 1/2) · Module U **แทรกลำดับตรวจใหม่เข้าไปกลาง `Update` และแก้ DTO ที่ `Create` ใช้ไฟล์
ร่วมกัน** → `qa-engineer` ต้องถือเป็น **regression surface ของ Module A** ไม่ใช่โค้ดใหม่ล้วน
(แนวเดียวกับ R-12 ของ Module P) · **ห้ามแตะเส้นทาง `Create` เลย** (AU-4/AU-16)

**งานที่ต้องทำ**:
1. `UpdateAdminUserDto` เพิ่ม `Email` (required) + `NewPassword` (optional) — AU-2
2. `IAuthorizationGuard` **เพิ่มเมธอดใหม่หนึ่งตัว** `EnsureNotSameRankPeer` — AU-4
3. `AdminUserService.Update` แทรกข้อ 4/5 ของลำดับ AU-3 + กฎอีเมล AU-6 + กฎรหัสผ่าน AU-7/AU-9
   + ธง AU-8 + log ที่เพิ่มได้แค่ boolean AU-7
4. `api-client.ts` + `types/domain.ts` แก้คู่กับ DTO — AU-13
5. `page.tsx` **ลบ** `<Select>` ในแถว + ปุ่มเปิด/ปิดในแถว + `UserRow.apply()` แล้วแทนด้วย
   ปุ่มเดียว + โมดัล "จัดการผู้ใช้" — AU-13/AU-14
6. unit test 11 ข้อตาม AU-15 (รวม regression ของ `EnsureNotRemovingLastGuardian`)

**ไม่มีงานเหล่านี้เลย**: migration · repository ใหม่ · endpoint ใหม่ · ตาราง audit · กลไก auth ใหม่

#### 🔒 Security gate — คำสั่งถึง `project-manager`

**ทุก phase ที่ implement Module U ต้องมี `🔒 Security gate` ต่อท้ายหัวข้อ phase ใน `plan.md`**
เช่น `## Phase N: จัดการบัญชีผู้ใช้รายอื่น (backend) 🔒 Security gate` — **ไม่มีข้อยกเว้น
รวมถึง phase ที่ทำแค่ UI** เพราะ UI คือสิ่งที่ตัดสินว่าปุ่มโผล่กับบัญชีไหน

**เหตุผลที่ต้องเขียนลง `plan.md` ด้วย** (`security`/`devops` อ่านบรรทัดนี้) — โมดูลนี้อ่อนไหว
**หนักกว่า Module A/P** เพราะเป็นครั้งแรกที่ระบบให้คนหนึ่งแตะ **credential ของอีกคน**:
1. **auth + credential ของผู้ใช้รายอื่นโดยตรง** — ตั้ง `PasswordHash` ให้บัญชีที่ไม่ใช่ของตัวเอง
   เป็นความสามารถที่ระบบนี้ไม่เคยมีมาก่อนเลย
2. **personal data** — แก้ `Email` ของคนอื่นได้อิสระ ไม่มีการยืนยัน (F5.2.3)
3. **เป็นการเพิ่มเมธอดใน `IAuthorizationGuard` ซึ่งเป็นแนวป้องกันชั้นเดียวของ `AdminUser`**
   (R-1 · TD-014) — `AdminUser` ไม่มี query filter รองข้างหลัง
4. **กฎใหม่สองข้อ (AU-4 peer-lockout · AU-5 ห้ามใช้กับตัวเอง) ถูกซ่อนที่ UI ด้วย (AU-14)** →
   `security`/`qa-engineer` **ต้องยิง API ตรงด้วย JWT ของแต่ละ role** ตามตาราง AU-12
   **การกดจากจอไม่ใช่หลักฐาน** (บทเรียนเดียวกับข้อ 3 ของ Module P)
5. **ลำดับใน AU-3 เป็นส่วนหนึ่งของความปลอดภัย ไม่ใช่ความสวยงาม** — `EnsureCanAssignRole`
   ต้องอยู่ก่อนกฎใหม่เสมอ ถ้ามีใครสลับลำดับแล้ว test ยังเขียว ต้องจับให้ได้ที่ขั้นนี้
6. **ยังไม่มี rate limiting** (R-2) และตอนนี้ endpoint นี้ตั้งรหัสผ่านได้ = น่าสนใจกว่าเดิมมาก
   สำหรับผู้โจมตีที่ได้ token ของ admin มา

**หมายเหตุสำหรับ `security` ตอน audit**: จุดที่ควรดูหนักที่สุดคือ **AU-3 (ลำดับ)** · **AU-5
(ไม่ยกเว้น owner จริงไหม)** · **AU-7 (ไม่มีรหัสผ่านหลุดเข้า log/response)** · **AU-8 (ไม่มีใคร
เผลอเซ็ตธงเป็น `false`)** · **AU-9** · **AU-6 ข้อ 7 (ข้อความอีเมลซ้ำ enumerate ข้ามบริษัทได้ —
พฤติกรรมเดิมของ `Create` ไม่ใช่ของใหม่ แต่ควรอยู่ในรายงาน · R-20)**
· และ **SECURITY-1 ของโมดูลนี้ยังไม่เคย re-audit** — `MustChangePassword` bypass ที่เคยเป็น
finding อยู่ในเส้นทางเดียวกับ AU-8 เป๊ะ ควรตรวจรวมรอบเดียวกัน

### Module B · Company Settings (ลิงก์หมดอายุ / TTS / แบรนด์) — ⏸️ ยังพักไว้

F2.1–F2.4 **หักส่วน pacing ที่ย้ายไป Module P แล้ว** · ขึ้นกับ Module A (ต้องมีบริษัทให้ตั้งค่าก่อน)
· **รูปร่าง schema เคาะแล้ว** (คอลัมน์ nullable บน `Company` ตาม B4 — ไม่ต้องคิดใหม่)
แต่ **ยังไม่มี Data Model เพราะความหมายของค่ายังไม่เคาะ** (A2 · A3 · A4 · A6 · B3b)
· เหตุผลที่ยังพัก: ทุกบริษัทรวมทั้ง scb ใช้ค่ากลางจาก env ได้อยู่แล้ว

**อัปเดต 2026-08-22 (P6) — ต้นทุนของการปลุก Module B ถูกลง แต่เงื่อนไขการปลุกไม่เปลี่ยน**:
หน้าจอที่ Module B เคยต้องสร้างเองทั้งหน้า **มีอยู่แล้ว** (`/admin/settings` ตาม SP-1) ฉะนั้น
การปลุกค่าใดค่าหนึ่งของ F2 = เคาะคำถามของค่านั้น → เขียน contract ของค่านั้น → เพิ่มคอลัมน์
nullable + **หนึ่ง section ตาม SP-2** · **ปลุกทีละค่าได้ ไม่ต้องรอครบสามค่า** (นี่คือสิ่งที่มติ P6
เปิดทางไว้) — เช่น ลิงก์หมดอายุมี env กลางอยู่แล้วและติดแค่แถว A5 ที่ยังไม่ยืนยัน
ส่วนแบรนด์ติดทั้ง A2 และ B3b ซึ่งหนักกว่ามาก · **แต่ยังห้าม implement ค่าใดก็ตามล่วงหน้า**
ก่อนคำถามของมันถูกเคาะ (R-4 · CP-15 · SP-13)

**🔒 Security gate (เมื่อถูกปลุก)**: เป็นการ **เขียนข้ามบริษัท** (owner แก้ค่าของบริษัทใดก็ได้)
บนตารางที่ไม่มี query filter — ช่องโหว่ที่พลาดตรงนี้แปลว่า admin ของลูกค้ารายหนึ่งแก้ค่าของอีกรายได้

### Module C · Learner-facing Brand — ⏸️ ยังพักไว้ (เป็นส่วนหนึ่งของ F2)

**แยกออกมาเฉพาะถ้า A2 เลือกทาง "อัปโหลดไฟล์"** เพราะจะกลายเป็น untrusted file input +
endpoint สาธารณะที่ไม่ต้อง auth · **ถ้า A2 เลือกทาง URL ให้ยุบรวมเข้า Module B**

## Risks & Dependencies

| # | เรื่อง | สถานะ |
|---|---|---|
| R-1 | **ทั้งโมดูลกั้นข้อมูลข้ามลูกค้าด้วย `IAuthorizationGuard` ชั้นเดียว** ไม่มี query filter รองรับ (TD-014) — งานทุกชิ้นในโมดูลนี้อยู่บนตารางสองตัวนั้น | ความเสี่ยงที่มีอยู่แล้วในระบบ ไม่ใช่ของใหม่ · จัดการด้วย 🔒 Security gate ทุก phase |
| R-2 | **ไม่มี rate limiting / abuse control** (TD-002 ทำเพียงบางส่วน) — endpoint สร้างบริษัท+บัญชีผู้ใช้ กั้นด้วย `EnsureOwner()` อย่างเดียว | ยังไม่แก้ · ควรอยู่ในสายตา `security` ตอนกลับมาทำ |
| R-3 | **ไม่มี audit log ว่าใครสร้าง/ปิดบริษัท** — เจ้าของโปรเจกต์ยืนยันแล้ว 2026-08-21 ว่ายังไม่ต้องมี ใช้ `CreateBy`/`UpdateBy` เฉยๆ พอ · ระบบมี `Logger.LogInformation`/`LogWarning` ที่ `ICompanyService.cs:84,108` อยู่แล้วซึ่งครอบระดับ log | **ความเสี่ยงที่ยอมรับแล้ว** ไม่ต้องยกขึ้นมาใหม่ |
| R-4 | **แพ็กเกจ/โควตา/สัญญา/usage จะทำให้ต้องกลับมาแตะ `Company` ซ้ำ** | **ความเสี่ยงที่ยอมรับแล้ว** 2026-08-21 · **ห้ามใส่ฟิลด์เผื่ออนาคตลง Data Model โดยอ้างว่า "เดี๋ยวก็ต้องใช้"** |
| R-5 | **Edge TTS ไม่เหมาะกับ production อยู่แล้ว** (TD-001) — การทำ UI เลือกเสียงตอนนี้เสี่ยงต้องรื้อเมื่อย้าย provider | ยังไม่เคาะ → A4 |
| R-6 | **F-3 คือ critical path ของรอบนี้** — scb เป็นบริษัทใหม่ที่ไม่มีบทเรียน/เอกสารเลย ซึ่งตรงกับเงื่อนไขที่ทำให้ `GetSystemDefault()` คืน null เป๊ะ ถ้า F1.3 ทำไม่ถูก **scb จะพังทันทีที่สร้างบทเรียนแรก** | ✅ **ปิดในรอบนี้** — B1 เคาะเป็น "สร้างให้บริษัทใหม่ + ซ่อมของเก่าทันที" → CH-1 คุมบริษัทใหม่, CH-6 คุมบริษัทเดิม ไม่เหลือช่องว่าง |
| R-8 | **ปิดบริษัทไม่ตัดผู้เรียนทันที** — ลิงก์ที่แจกไปแล้วยังเรียนได้จนหมดอายุ (สูงสุดตาม `DEFAULT_SESSION_EXPIRY_HOURS` = 24 ชม. เว้นแต่ CS ตั้งวันหมดอายุเองยาวกว่านั้นตอนสร้างลิงก์) | **ความเสี่ยงที่ยอมรับแล้ว 2026-08-21 (B2)** · ข้อบังคับที่มากับการยอมรับ: **UI ต้องเขียนข้อความให้ตรงความจริงตาม CP-14** ห้ามทำให้ owner เข้าใจว่าปิดแล้วตัดทุกอย่างทันที |
| R-9 | **owner รู้รหัสผ่านตั้งต้นของ admin ลูกค้า** จนกว่าเจ้าตัวจะเข้าครั้งแรกแล้วเปลี่ยน · และการส่งมอบเกิดนอกระบบ (โทร/แชท) ซึ่งระบบตรวจสอบไม่ได้ | **ความเสี่ยงที่ยอมรับแล้ว 2026-08-21 (A1)** — เป็นข้อจำกัดจากการที่ระบบไม่มีการส่งอีเมลเลย ไม่ใช่ทางเลือกที่ออกแบบมา · บรรเทาด้วย `MustChangePassword = true` บังคับ (CP-8) และห้าม log/คืนรหัสผ่านทุกกรณี (CP-10/CP-11) · **ถ้าวันหนึ่งมี SMTP ควรกลับมาทบทวนข้อนี้เป็นอันดับแรก** |
| R-7 | **F1 ส่งมอบแล้วยังไม่พอให้ scb ใช้งานจริงได้ครบ** — สร้างบริษัทได้ + admin คนแรกเข้าได้ + สร้างบทเรียนได้ แต่ค่าทุกตัว (ลิงก์หมดอายุ/เสียง/แบรนด์) ยังเป็นค่ากลางร่วมกับ School Bright เพราะ F2 ยังพัก | **ยอมรับแล้ว 2026-08-21** — เจ้าของโปรเจกต์ระบุว่าค่ากลางใช้ได้จริงตอนนี้ · ถ้า scb ขอแบรนด์ตัวเองเมื่อไหร่ = trigger ปลุก Module B |
| D-1 | Module B ขึ้นกับ Module A · Module C (ถ้ามี) ขึ้นกับ Module B — **ทั้งคู่ยังไม่อยู่ในสโคป** | — |
| D-2 | F1.3 เป็น **hard dependency ข้ามโมดูลไปที่ `knowledge-base`** — invariant ของ `GetSystemDefault()` เป็นของโมดูลนั้น โมดูลนี้ต้องรักษาไว้ ห้ามแก้ | — |
| R-10 | **DM-P2 เป็น breaking change ที่ระดับ contract ไม่ใช่ที่ระดับข้อมูล** — ข้อมูลปลอดภัย (ขยาย `NOT NULL → NULL` เก็บค่าเดิมครบ, P4 ได้ฟรี) แต่ `LessonConfigDto`/`LessonConfigViewModel`/`domain.ts` เปลี่ยนชนิดเป็น nullable ซึ่งกระทบทุกที่ที่อ่านค่านี้ · จุดที่พังเงียบที่สุดคือ `LearnerLessonConfig = Pick<LessonConfig, ...>` ใน `domain.ts:41-50` ที่จะพา `null` เข้าไปฝั่งผู้เรียนทั้งที่ server resolve ให้แล้ว | ~~**ยอมรับแล้ว 2026-08-22** — เป็นราคาที่ P1 (บทเรียน override ได้) บังคับให้จ่าย~~ · **แก้ 2026-08-22 รอบที่ 7**: P1 ถูกกลับคำตอบ → ตอนนี้ **DM-P2 เป็น breaking ทั้งที่ระดับ contract _และ_ ที่ระดับข้อมูล** (คอลัมน์หายจริง ค่าหายจริง — R-16) · จุดที่พังเงียบเปลี่ยนจาก "`null` ไหลเข้าฝั่งผู้เรียน" เป็น **"ฟิลด์หายไปแต่ยังมีโค้ดอ้างถึง"** ซึ่ง TypeScript/C# compiler จับได้เกือบทั้งหมด = **เสี่ยงน้อยกว่ารอบก่อน** · คุมด้วย LP-5/LP-11/LP-12 + build/typecheck ทั้งสองฝั่ง |
| R-11 | **migration backfill ใช้ literal ไม่ใช่ env** (Migration Plan ข้อ 1) — ถ้า environment ปลายทางตั้ง `DEFAULT_*_MS` ไว้ต่างจาก `5000/500/5000` บริษัทเดิมจะได้ค่าที่ไม่ตรงกับพฤติกรรมเดิมของระบบนั้นเงียบๆ | **ยอมรับโดยมีเงื่อนไข** — ทางเลือกอีกทางคือให้ migration อ่าน env ซึ่งไม่ deterministic กว่า · เงื่อนไข: `devops` ต้องมีขั้นตอน "ตรวจ env แล้ว UPDATE ถ้าไม่ตรง" ใน `deploy.md` **ก่อน** deploy ใบนี้ ไม่ใช่หลัง |
| R-12 | **Module P แก้โค้ดที่ QA ผ่านไปแล้ว** — จุด `new Company` ทั้งสองจุดเป็นของ Phase 1/Module A ที่ verified ✅ ไปแล้ว (ยังไม่ deploy) | จัดการด้วยการประกาศไว้ล่วงหน้า: `project-manager` ต้องเขียนใน `plan.md` ว่า phase นี้แตะ `ICompanyService` ของ Phase 1 · `qa-engineer` ถือเป็น regression surface ของ Phase 1 ไม่ใช่โค้ดใหม่ล้วน |
| D-3 | **`LessonConfig` ถูกประกาศไว้ใน `knowledge-base/design.md` §DM-2 ด้วย** (ที่นั่นระบุ pacing เป็น `required int`) — เมื่อ DM-P2 ถูก implement ทั้งสองเอกสารจะขัดกัน และรอบ QA ของ `knowledge-base` จะเห็นเป็น drift | **ยังไม่ปิด · 🔄 คำตอบที่ถูกต้องเปลี่ยนเป็นรอบที่สองแล้ว (2026-08-22 รอบที่ 7)**: ~~amend DM-2 ให้เป็น nullable~~ → **ต้องลบสามฟิลด์ `IntroWaitMs`/`BreathPauseMs`/`FinalQuestionWaitMs` ออกจาก DM-2 ไปเลย** และชี้มาที่ DM-P2 ว่าเจ้าของกฎคือโมดูลนี้ · **`system-analyst` แก้ไม่ได้ในรอบนี้เพราะ `conventions.md` §1 บังคับว่าหนึ่งรอบเขียนได้แค่ในโฟลเดอร์โมดูลที่ resolve ไว้** — ผู้ใช้ต้องสั่งรอบใหม่ให้โมดูล `knowledge-base` · ⚠️ **ถ้ารอบนั้นไปหยิบทิศทางเก่า (nullable) มาใช้ จะผิดทันที** ให้ยึดข้อความในแถวนี้ |
| R-16 | **`RemoveLessonConfigPacingOverrides` ทำลายข้อมูลจริงและกู้กลับไม่ได้** — ค่า override ของบทเรียนเดิม (เช่น 3000/500/3000) หายถาวรเมื่อ migration รัน | **ยอมรับแล้ว 2026-08-22 (มติ N2 · เจ้าของโปรเจกต์ตัดสินโดยตรง)** — เป็นการตัดสินใจทางธุรกิจ ไม่ใช่ผลข้างเคียง · **เงื่อนไขที่มากับการยอมรับ 3 ข้อ**: (1) migration ต้องมีคอมเมนต์อธิบายเจตนา (Migration Plan ข้อ 1) (2) `devops` ต้อง backup ตาราง `LessonConfig` ก่อนรัน — **เพื่อตอบคำถามย้อนหลังได้ ไม่ใช่เพื่อเอาค่ากลับเข้า schema** (3) ถ้าลูกค้าทักว่า "บทเรียนนี้จังหวะเปลี่ยนไป" คำตอบคือ **ตั้งค่ากลางของบริษัทใหม่** ไม่ใช่กู้ค่าเดิมกลับ |
| R-17 | **Phase 4/5 ถูก implement ครบตามสัญญาเดิมแล้วและยังไม่เคยผ่าน QA — การถอดออกครึ่งทางคือความเสี่ยงที่แท้จริงของรอบนี้** (เช่น ลบคอลัมน์แล้วแต่ฟอร์มยังมีช่อง, ลบช่องแล้วแต่ DTO ยังรับค่า, ลบ test เก่าแล้วไม่มี test ใหม่มาแทน) | **คุมด้วยการเขียนไว้ล่วงหน้า**: LP-11 ระบุ failure mode นี้ตรง ๆ ("ช่องที่ยังอยู่แต่ส่งค่าไม่ถึง server") · ตาราง 3 กลุ่มใน §Module P แยก "ยังถูกต้อง / ต้องถอด / งานใหม่" ให้ `project-manager` ใช้ตั้ง task ได้โดยไม่ต้องเดา · **`qa-engineer` ต้องถือรอบแรกของ Phase 4/5 เป็น FULL เสมอ** — ไม่มี TARGETED สำหรับ phase ที่ contract เปลี่ยนทิศระหว่างทาง |
| D-4 | **Module P ไม่ขึ้นกับ A2/A3/A4/A6/B3b เลย** — ยืนยันแล้วทีละข้อว่าไม่มีข้อไหนแตะค่า pacing · **ทบทวนซ้ำ 2026-08-22 รอบที่ 5 พร้อมงานหน้าจอของ P6**: ยังจริงอยู่ ยกเว้น **A6 ที่กลายเป็นตัวบล็อกทันทีที่มีหน้าจอเกิดขึ้น** (ต้องตัดสินว่าเมนูโผล่ให้ `cs` ไหม) จึงปิดเฉพาะส่วนที่จำเป็นในรอบนี้ ไม่ปล่อยให้ engineer เดา | — |
| R-13 | **หน้า `/admin/settings` จะมี section เดียวไปอีกระยะหนึ่ง** — ผู้ใช้เปิดเมนูชื่อ "ตั้งค่าบริษัท" แล้วเจอแค่ค่าจังหวะการสอน อาจคาดหวังว่าจะเจอลิงก์หมดอายุ/เสียง/แบรนด์ด้วย (F2 ที่ยังพัก) | **ยอมรับ 2026-08-22 (P6)** — เป็นราคาที่มติ P6 เลือกจ่ายโดยตั้งใจ เพื่อไม่ให้ค่าที่เคาะครบแล้วติดอยู่กับค่าที่ยังเคาะไม่ครบ · **บรรเทาด้วย SP-1 ข้อสุดท้าย: ห้ามใส่ placeholder "เร็ว ๆ นี้" ของ section ที่ยังไม่มี** — หน้าที่มี section เดียวจริง ๆ ดีกว่าหน้าที่โฆษณาของที่ยังไม่มี · ถ้าลูกค้าถามถึงค่าอื่นเมื่อไหร่ = trigger ปลุกค่านั้นตาม R-7 |
| R-14 | **`/admin/settings` เป็นจอแรกที่ทำให้ `cs` เห็นกลุ่มเมนู "ตั้งค่า"** ซึ่งวันนี้ถูกซ่อนทั้งกลุ่มจาก `cs` (`AdminSidebar.tsx:170`) — การขยับ gate ผิดระดับจะเปิดเมนู "ผู้ใช้งาน" (`/admin/users`) ให้ `cs` ไปด้วยโดยไม่มีใครสังเกต เพราะทั้งสองรายการอยู่ใน `SidebarGroup` เดียวกัน | **คุมด้วย SP-5 ที่ระบุวิธีแก้ไว้ตรง ๆ** (ย้าย gate ลงระดับรายการ คง `!== "cs"` ของ "ผู้ใช้งาน" ไว้เหมือนเดิม) + เพิ่มเป็นข้อ 4 ในหมายเหตุ 🔒 Security gate ของ Module P ให้ `security`/`qa-engineer` ตรวจโดยเฉพาะ · **หมายเหตุ: `/admin/users` มีการกั้นสิทธิ์ที่ server อยู่แล้ว** ความเสี่ยงจริงคือ UI พาไปหน้าที่ใช้ไม่ได้ ไม่ใช่ข้อมูลรั่ว แต่ก็ยังต้องไม่เกิด |
| R-15 | **มติ A8 เพิ่มความสามารถ "ซ่อน section จาก role" ซึ่งอ่านง่ายมากว่าเป็นการกั้นสิทธิ์** — วันที่มี section แรกที่ซ่อนจริง ถ้า endpoint ของค่านั้นยังตอบ `GET` ให้ role ที่ถูกซ่อน ระบบจะดู"ปลอดภัย"บนจอทั้งที่ข้อมูลยังดึงได้ด้วย `curl` ตัวเดียว | **คุมไว้ล่วงหน้าแล้วที่ SP-15 ข้อ 6**: section ที่ประกาศซ่อนจาก role ใด ต้องระบุใน contract ว่า server ปฏิเสธ role นั้นที่ `GET` ด้วย ถ้าปฏิเสธไม่ได้ต้องเขียนว่า **cosmetic เท่านั้น** และห้ามใช้กับข้อมูลอ่อนไหว · **รอบนี้ยังไม่มีความเสี่ยงจริงเลย** — pacing ไม่ซ่อนจากใคร และ `cs` `GET` ได้โดยตั้งใจ (LP-9) · ข้อนี้เป็นสิ่งที่ `security` ต้องตรวจในรอบที่มี section ที่สอง ไม่ใช่รอบนี้ |

| R-18 | **peer-lockout เทียบ role ปัจจุบันเท่านั้น (AU-4) → `admin` ยังเลื่อน `cs` ขึ้นเป็น `admin` ได้ แล้วหลังจากนั้นจัดการบัญชีนั้นไม่ได้อีก** — บริษัทจะมี admin สองคนที่แตะกันไม่ได้ ต้องรบกวน `owner` ทุกครั้งที่คนใดคนหนึ่งลืมรหัส ซึ่งเป็นคอขวดเดียวกับที่ role split ตั้งใจกำจัด | **ยอมรับโดยมีเงื่อนไข 2026-08-25** — เป็นการอ่าน F5.2.1 ตามตัวอักษร และความสามารถ "สร้าง peer" มีอยู่แล้ววันนี้ที่ `POST /api/admin-users` ซึ่ง `requirement.md` ไม่ได้ขอให้แตะ · **`system-analyst` จงใจไม่เพิ่มกฎเข้มกว่าที่เคาะ** · ทางออกเมื่อเกิดจริง = `owner` เข้าไปจัดการ · **ถ้าเจ้าของโปรเจกต์ต้องการกันตั้งแต่ต้นทาง ให้ตอบ OQ-U2 แล้วกลับมา amend — ห้าม engineer เพิ่มกฎนี้เอง** |
| R-19 | **`Email` เป็น `required` ใน `UpdateAdminUserDto` = breaking wire contract** — คำขอเดิมที่ไม่ส่ง `email` ได้ 400 ทันที · ถ้า deploy backend ก่อน frontend **หน้า `/admin/users` ที่ใช้งานอยู่จริงจะพังทุกการกด** (เปลี่ยน role/เปิด-ปิดบัญชี) ไม่ใช่แค่ฟีเจอร์ใหม่ไม่ทำงาน | **คุมด้วยลำดับ deploy** — `project-manager` ต้องเขียนใน `plan.md` §Sequencing Notes ว่า phase backend กับ phase frontend ของ Module U **ต้องขึ้นพร้อมกัน** (ข้อบังคับแบบเดียวกับ migration ของ Module P รอบที่ 7) · `devops` ต้องไม่ปล่อยครึ่งเดียว · **นี่คือ breaking change ที่ระดับ contract ไม่ใช่ที่ระดับข้อมูล** — ไม่มีข้อมูลใดเสียหาย |
| R-20 | **ข้อความ "อีเมลนี้ถูกใช้งานแล้ว" บอกได้ว่ามีบัญชีนั้นอยู่ในระบบ รวมถึงในบริษัทของลูกค้ารายอื่น** (AU-6 ข้อ 7) — `admin` ของลูกค้า A ลองใส่อีเมลทีละอันแล้วรู้ได้ว่าใครเป็นผู้ใช้ของลูกค้า B | **พฤติกรรมเดิมของ `Create` ที่ยอมรับไว้แล้ว ไม่ใช่ของที่ Module U สร้างขึ้น** (`IAdminUserService.cs:68`) · Module U ทำให้มีจุดที่ลองได้เพิ่มอีกจุด · **ยังไม่แก้ในรอบนี้** เพราะ F5.2.3 บังคับให้ข้อความต้องชัด และการทำให้กำกวมจะย้อนแย้งกับ requirement · **ต้องอยู่ในรายงานของ `security`** พร้อม R-2 (ไม่มี rate limiting) ซึ่งเป็นตัวที่ทำให้การไล่ลองเป็นไปได้จริง |
| D-5 | **Module U ไม่ขึ้นกับ Module B/C/P เลย** — ตรวจทีละข้อแล้ว: A2/A3/A4/A6/B3b เป็นเรื่องค่าระดับบริษัท ไม่มีข้อไหนแตะ `AdminUser` หรือ `/admin/users` · Module P แตะ `Company` ไม่ใช่ `AdminUser` | — |

## Unresolved Open Questions

> **✅ คำถามของ Module A เคาะครบแล้วทั้ง 5 ข้อ (2026-08-21)** — B1 · A1 · B2 · N1 · N2
> ย้ายไปอยู่ในตาราง **"การตัดสินใจที่ผู้ใช้ยืนยันแล้ว"** ใต้ `## Feature-by-Feature Feasibility`
> พร้อมสิ่งที่แต่ละคำตอบตัดออก · เนื้อของคำตอบกลายเป็น CP-*/CH-* ใน contract sections แล้ว
> **ไม่มี open question ใดที่บล็อก Module A อีก**
>
> **ที่เหลือข้างล่างนี้เป็นของ F2 (Module B/C) ล้วน — ยังไม่ต้องตอบ** เก็บไว้เพื่อไม่ให้ต้องคิดใหม่
> จากศูนย์วันที่ปลุก F2 · **A7 ตอบไปโดยปริยายแล้ว** (ทางเลือก ก ตรงกับพฤติกรรมโค้ดปัจจุบัน)
>
> ⚠️ **ห้ามเดาข้อใดข้อหนึ่งในกลุ่มนี้แล้วสร้าง F2 พ่วงไปกับ Module A** — CP-15 ห้ามไว้ชัด

### 🆕 กลุ่ม OQ-U · ของ Module U (F5) — เปิด 2026-08-25

> **ไม่มีข้อไหนบล็อกการ implement Module U** — contract AU-1..AU-16 ตอบครบทุกเคสที่ engineer
> ต้องเจอแล้ว สามข้อนี้เป็น **การเลื่อนโดยรู้ตัว** ที่ `system-analyst` ตัดสินแบบไม่ขยายสโคปเอง
> เพราะเป็นคำถามธุรกิจ/ดีไซน์ ไม่ใช่คำถามทางเทคนิค
>
> ⚠️ **ทั้งสามข้อถูกตัดสินไปแล้วในทิศ "ทำน้อยที่สุด" และเขียนลง contract แล้ว** — ถ้าเจ้าของ
> โปรเจกต์ตอบต่างจากนี้ ให้กลับมา amend `design.md` **ห้าม engineer เปลี่ยนเอง**

| # | คำถาม | ที่ `design.md` ตัดสินไว้รอบนี้ | ถ้าคำตอบต่างออกไปต้องทำอะไร |
|---|---|---|---|
| **OQ-U1** 🟠 | **บัญชี `owner` ไม่เคยปรากฏในตาราง `/admin/users` เลย** (`GetByCompanyId` กรอง `CompanyId == companyId` แต่ `owner` มี `CompanyId = null` เสมอ) → กฎ F5.2.1 ที่เคาะไว้ว่า "`owner` จัดการ `owner` คนอื่นได้" **มีผลที่ server แต่ไม่มีหน้าจอไปถึง** · เจ้าของโปรเจกต์อาจเข้าใจตอนตอบว่ามีจอให้ทำ | **เขียนกฎ server ไว้ตามที่เคาะ (AU-12 ช่อง `owner`×`owner` = ✅) แต่ _ไม่แตะตาราง_ ในรอบนี้** — ไม่ขยายสโคปที่ `business-analyst` ไม่เคยยืนยัน · **ห้ามแก้ `GetByCompanyId`** (AU-16) | ถ้าต้องการให้ทำได้จริงจากจอ = **งาน backend เพิ่มจริง** (repository/endpoint ที่คืนบัญชี owner + ต้องเคาะว่าแสดงที่หน้าบริษัทไหน เพราะ owner ไม่สังกัดบริษัท) → **ต้องกลับไป `business-analyst` ก่อน** ไม่ใช่ให้ engineer เดา |
| **OQ-U2** 🟡 | **peer-lockout ควรกันการ "เลื่อนคนอื่นขึ้นมาเป็นระดับเดียวกับตัวเอง" ด้วยไหม** — F5.2.1 เขียนแค่ "ห้ามทำกับบัญชีที่ role ระดับเดียวกัน" ไม่ได้ตอบเคสนี้ | **เทียบ role _ปัจจุบัน_ อย่างเดียว (AU-4)** — `admin` ยังเลื่อน `cs` เป็น `admin` ได้ เหมือนที่วันนี้สร้าง `admin` ใหม่ได้อยู่แล้ว · ผลข้างเคียงบันทึกเป็น **R-18** | ถ้าตอบว่า "กันทั้งสองทาง" ต้อง amend AU-4 **และต้องตัดสินด้วยว่า `POST /api/admin-users` (Create) ต้องกันด้วยไหม** ไม่งั้นจะมีกฎสองที่ไม่ตรงกัน — เป็นคำถามที่ต้องเคาะพร้อมกัน |
| **OQ-U3** 🟢 | **โมดัลไม่มีช่อง "ชื่อที่แสดง"** ตามไฟล์ Figma → หลังรอบนี้ `DisplayName` จะยังแก้ไม่ได้จากที่ไหนเลยในระบบ (ตั้งได้ตอนสร้างบัญชีเท่านั้น) | **คงตาม Figma ไม่มีช่องชื่อ** (AU-13) — ส่ง `displayName` ค่าเดิมกลับไป · **ไม่ใช่ regression**: วันนี้ก็แก้ไม่ได้อยู่แล้ว | ถ้าต้องการให้แก้ได้ = เพิ่มหนึ่ง `Input` ในโมดัล (DTO รับ `DisplayName` อยู่แล้ว **ไม่ต้องแก้ backend เลย**) แต่เป็น field ที่ไม่มีในไฟล์ Figma ต้องให้ดีไซเนอร์รับรู้ — **คนละเรื่องกับ OQ-15 ซึ่งเป็นแค่ข้อความบนปุ่ม** |

**ของที่ยังเปิดอยู่ฝั่ง `requirement.md` และไม่ได้ถูกตัดสินที่นี่**: **OQ-15** (ข้อความปุ่ม Cancel
ในไฟล์ Figma เขียนว่า "เรียนอีกครั้ง" ซึ่งหลุดมาจาก flow บทเรียน) — เจ้าของโปรเจกต์ต้องเช็คกับ
คนทำไฟล์ Figma เอง · **ไม่บล็อกอะไรนอกจากข้อความบนปุ่มนั้นปุ่มเดียว** · `frontend-engineer`
**ห้ามตั้งข้อความเอง** แม้ "ยกเลิก" จะดูชัดเจน (F5.4 · AU-13 · AU-16)

### 📌 OQ-P7 · ถ้าวันหนึ่งต้องการ "จังหวะเฉพาะบทเรียน" กลับมา — **รู้แล้ว จงใจเลื่อน** (บันทึก 2026-08-22 รอบที่ 7)

**ไม่ใช่คำถามที่ต้องตอบตอนนี้ และไม่บล็อกอะไรเลย** — บันทึกไว้เพราะมติ N1/N2/N3 ปิดทางนี้แบบ
**มีราคาที่จ่ายทีหลังแพงกว่าจ่ายตอนนี้** ถ้าเกิดต้องการขึ้นมาจริง:

- **ค่าเดิมไม่กลับมา** — N2 ทิ้งไปแล้ว (R-16) การเปิด override รอบใหม่ = ทุกบทเรียนเริ่มจากศูนย์
- **ต้องมี migration เพิ่มคอลัมน์ใหม่อีกใบ** และกฎ empty-vs-zero ทั้งชุด (LP-3/LP-11 เดิม)
  ต้องถูกเขียนใหม่ทั้งหมด — ของเดิมถูกยกเลิกแล้ว ไม่ใช่พักไว้
- **สัญญาณที่บอกว่าถึงเวลาต้องกลับมาคุย**: มีบทเรียนประเภทที่จังหวะกลางใช้ไม่ได้จริง (เช่น
  บทเรียนสั้นมาก/ยาวมาก หรือกลุ่มผู้เรียนที่ต่างกันชัดเจน) — **ถ้าเจอสัญญาณนี้ให้กลับไปที่
  `business-analyst` ก่อน อย่าเพิ่มคอลัมน์เอง**

**ทางเลือกที่จะพิจารณาในวันนั้น (ยังไม่ตัด ยังไม่เลือก)**: override ต่อบทเรียนแบบเดิม ·
override ต่อ**หมวด** (`KnowledgeCategory`) ซึ่งเข้ากับ taxonomy ของ `knowledge-base` มากกว่า ·
หรือ preset ระดับบริษัทหลายชุดให้บทเรียนเลือก

### ⏸️ กลุ่มที่เลื่อนไปพร้อม F2 (ยังไม่ต้องตอบรอบนี้)

> **A1 · B1 · B2 · N1 · N2 ไม่อยู่ในกลุ่มนี้แล้ว** — เคาะครบเมื่อ 2026-08-21 ดูตาราง
> "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว"
>
> **อัปเดต 2026-08-22 — ปิดเพิ่มอีกสามข้อ (บางส่วน):** **B4 ปิดทั้งข้อ** · **B3 แตกเป็น B3a
> (pacing — ปิด) กับ B3b (แบรนด์ — ยังเปิด)** · **A5 ปิดเฉพาะแถว pacing** ส่วนแถวลิงก์หมดอายุ/
> ความเร็วพูด/สีแบรนด์/ชื่อแบรนด์ **ยังเป็นข้อเสนอที่ยังไม่ยืนยัน** · **A2 · A3 · A4 · A6 ยังเปิดครบ
> ไม่ถูกแตะเลยในรอบนี้**
>
> **อัปเดต 2026-08-22 (รอบที่ 5 · มติ P6):** **A6 ปิดเฉพาะส่วน "ตัวหน้า + section pacing"**
> (= (ข) `cs` อ่านอย่างเดียว) ส่วนที่เหลือของ A6 ยังเปิด · **เพิ่มคำถามใหม่ A8** (สิทธิ์ต่อ section)
> ซึ่ง **ไม่บล็อกรอบนี้ แต่บล็อกการเพิ่ม section ที่สอง** · **A2 · A3 · A4 · B3b ยังเปิดครบ
> ไม่ถูกแตะเลยในรอบนี้ และยังไม่มีข้อไหนบล็อกงานหน้าจอของ P6**
>
> **อัปเดต 2026-08-22 (รอบที่ 6):** ✅ **A8 ปิดแล้ว** (กลไก 2 แกน visible/edit ต่อ section →
> contract อยู่ที่ **SP-15**) และ ✅ **A5 แถว pacing ปิดครบทั้งสามแถว** (เจ้าของโปรเจกต์ยืนยัน
> ตัวเลขเดิมของ LP-8) · **เหลือเปิดจริง 4 ข้อครึ่ง: A2 · A3 · A4 · B3b · A6 เฉพาะส่วนที่ไม่ใช่
> pacing** — ทั้งหมดเป็นของ F2 ที่ยังพัก · **ไม่มี open question ใดค้างอยู่กับ Module P
> หรือหน้า `/admin/settings` (section pacing) อีกแล้ว**

**A2 (= OQ-2) · โลโก้: อัปโหลดไฟล์ หรือกรอก URL**
- (ก) กรอก URL รูปที่โฮสต์ที่อื่น — *แนะนำสำหรับรอบแรก* ไม่แตะ storage, ไม่ต้องเปิด endpoint สาธารณะใหม่, ไม่ต้อง validate ชนิด/ขนาดไฟล์, ไม่ต้องคิดเรื่องลบไฟล์ตอนปิดบริษัท · ข้อเสีย: ลูกค้าต้องมีที่ฝากรูปเอง และรูปหายเมื่อไหร่หน้าเข้าห้องเรียนก็พัง
- (ข) อัปโหลดเข้าระบบ — ใช้ `IDocumentStorageProvider` เดิมได้ แต่เพิ่มของใหม่ 3 อย่าง (endpoint อัปโหลด, endpoint สาธารณะให้ผู้เรียนที่ไม่ล็อกอินโหลดรูป, validation) และ**เปลี่ยนขนาด security surface ทั้งโมดูล** (จะทำให้เกิด Module C)
- (ค) ยังไม่ทำโลโก้รอบแรก เอาแค่ชื่อ + สี

**A3 (= OQ-3a) · ช่องเลือกเสียง TTS: dropdown หรือกรอก string เอง**
- (ก) dropdown จาก constant list ที่ backend กำหนด + server ปฏิเสธค่านอกรายการ — *แนะนำ* เป็นทางเดียวที่ทำให้ F2.4 validate ได้จริง
- (ข) กรอก string ดิบ — validate ได้แค่ความยาว ไม่รู้ว่าเสียงมีจริงไหมจนกว่าจะสอนแล้วเงียบ
- (ค) dropdown สำหรับ `admin` + ช่องกรอกดิบสำหรับ `owner`

**A4 (= OQ-3b) · รอ ElevenLabs ก่อน หรือทำกับ Edge TTS ไปเลย**
- (ก) ทำกับ Edge ไปก่อน แต่เก็บค่าแบบ provider-neutral แล้วให้ชั้น provider แปลงเป็นรูปแบบ Edge (`"-10%"`) — *แนะนำ* วันย้าย provider แก้แค่ตัวแปลง + รายการเสียง ไม่แตะ schema/UI
- (ข) ทำแค่ความเร็ว พักเรื่องเสียงไว้ (ความเร็วเป็นแนวคิดที่ทุก provider มี ส่วนชื่อเสียงคนละชุดกันสิ้นเชิง)
- (ค) ตัดค่า TTS ออกจาก F2 ทั้งข้อ เหลือ 2 ค่า

**A5 (= OQ-4) · ขอบเขตค่าที่รับได้** — 🟡 **แถว pacing ปิดสนิทแล้ว (ยืนยัน 2026-08-22)
แถวที่เหลือยังเปิด**

| ค่า | เสนอ | สถานะ | เหตุผล |
|---|---|---|---|
| **`introWaitMs`** | **0–60000 ms** | ✅ **เจ้าของโปรเจกต์ยืนยันค่าเดิมแล้ว 2026-08-22** — contract อยู่ที่ **LP-8** ไม่ใช่ข้อเสนออีกต่อไป (จูนทีหลังได้ แก้ที่ LP-8 จุดเดียว) | `0` = เริ่มทันที ต้องรับได้ · เพดานกันห้องที่ดูเหมือนค้าง |
| **`breathPauseMs`** | **0–10000 ms** | ✅ ยืนยันแล้ว 2026-08-22 (LP-8) | เกิน 10 วิ ผู้เรียนคิดว่าระบบพัง |
| **`finalQuestionWaitMs`** | **0–120000 ms** | ✅ ยืนยันแล้ว 2026-08-22 (LP-8) | ยาวได้กว่าเพื่อน แต่ต้องมีเพดานกัน session ค้าง |
| ลิงก์หมดอายุ | 1–720 ชั่วโมง (1 ชม. – 30 วัน) | ⏸️ ยังเป็นข้อเสนอ | ต่ำกว่า 1 ชม. ครูเปิดทีหลังในวันเดียวกันก็ไม่ทัน · เกิน 30 วัน ลิงก์ค้างกลายเป็นพื้นที่เสี่ยง |
| ความเร็วพูด | -50% ถึง +50% | ⏸️ ยังเป็นข้อเสนอ (ผูกกับ A3/A4) | ตรงกับ regex ที่ระบบ validate อยู่แล้ว (`SynthesizeSpeechDto.cs:19` รับ ±2 หลัก) และเกินกว่านี้ Edge ฟังไม่รู้เรื่อง |
| สีแบรนด์ | `^#[0-9a-fA-F]{6}$` | ⏸️ ยังเป็นข้อเสนอ (ผูกกับ A2/B3b) | |
| ชื่อแบรนด์ | ≤ 60 ตัวอักษร | ⏸️ ยังเป็นข้อเสนอ (ผูกกับ A2/B3b) | |

**A6 (= OQ-5) · `cs` เห็นหน้าตั้งค่าไหม** — 🟡 **ปิดเฉพาะ section pacing (2026-08-22) ส่วนที่เหลือยังเปิด**

- (ก) ไม่เห็นเลย (ไม่มีเมนู)
- (ข) เห็นแบบอ่านอย่างเดียว — *แนะนำเล็กน้อย* เพราะ CS ต้องตอบครูได้ว่า "ลิงก์ของบริษัทนี้อยู่กี่ชั่วโมง"
- (ค) เห็นแต่กดแก้แล้วเด้ง error — ไม่แนะนำ UX แย่

| ขอบเขต | สถานะ |
|---|---|
| **ตัวหน้า `/admin/settings` + section `pacing`** | ✅ **ปิดแล้ว = (ข) อ่านอย่างเดียว** เขียนเป็น contract ที่ **SP-4/SP-5** · เหตุผลเต็มอยู่ในตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" |
| section ลิงก์หมดอายุ / TTS / แบรนด์ | ⏸️ **ยังเปิด** — ตอบตอนปลุกค่านั้น ไม่ต้องตอบตอนนี้ · **ห้ามเหมาว่าเป็น (ข) ตามที่ pacing เลือก** เพราะเหตุผลที่ทำให้ pacing เป็น (ข) คือ `cs` เห็นค่านั้นอยู่แล้วผ่าน placeholder ในฟอร์มบทเรียน (LP-5) ซึ่ง**ไม่จริงกับค่าอื่นสักค่า** · **อัปเดต 2026-08-22**: ตัวเลือกของแต่ละ section ตอนนี้กว้างกว่า (ก)/(ข)/(ค) เดิมแล้ว — ต้องตอบเป็น **สองแกนตาม SP-15** คือ `visibleToRoles` (ซ่อนจาก `cs` ไปเลยก็ได้) และ `editableByRoles` (owner-only ก็ได้) แยกกัน |

**A8 (ใหม่ 2026-08-22) · สิทธิ์ต่อ section ต่างกันได้ไหม และใครตัดสิน** — ✅ **ปิดแล้ว 2026-08-22
(ตอบตรงในแชท) · contract = SP-15**

> **มติ**: **ได้ และเป็นสองแกนที่แยกจากกัน** — "สิทธิ์การมองเห็น" (`visibleToRoles`) กับ
> "สิทธิ์แก้" (`editableByRoles`) ตั้งแยกกันได้ต่อ section · **บาง section ซ่อนจาก role ไปเลยได้
> ไม่ใช่แค่ read-only** (เช่นค่าที่ "แอดมินสูงสุด" เท่านั้นควรเห็น) ส่วน section ที่ไม่อ่อนไหว
> อย่างเสียง/pacing **เห็นได้ทุก role** · **ผู้ตัดสินค่าของสองแกนนี้คือเจ้าของโปรเจกต์ ต่อ section
> ไม่ใช่ engineer และไม่มีค่า default ให้เดา**
>
> **สำหรับ section `pacing` ของรอบนี้ไม่มีอะไรเปลี่ยนแม้แต่จุดเดียว** — จัดอยู่กลุ่ม "ไม่อ่อนไหว"
> = เห็นได้ทุก role (`owner`/`admin`/`cs`) แก้ได้เฉพาะ `owner`/`admin` ตรงกับที่ LP-9 และ SP-4
> เคาะไว้แล้วทุกประการ

สิ่งที่คำตอบนี้เปลี่ยนจริง (อยู่ที่ **SP-15** ทั้งหมด):
1. section ประกาศสิทธิ์ของตัวเองเป็น **สองรายการแยกกัน** ไม่ใช่ boolean เดียวหรือ "role ต่ำสุดที่แก้ได้"
2. **"ซ่อน" เป็นผลลัพธ์ที่ถูกต้องได้** — ไม่ render ไม่ยิง `GET` ไม่มีข้อความบอกว่ามีของที่คุณไม่เห็น
3. เมนู sidebar ต้อง **derive** จาก registry ("เห็นอย่างน้อย 1 section") ห้าม hardcode role (SP-5)
4. invariant: `editableByRoles` ⊆ `visibleToRoles` และ `owner` อยู่ในทั้งสองรายการเสมอ

**สิ่งที่ยังต้องถามตอนเพิ่ม section ที่สอง (นี่คือ A6 ที่เหลือ ไม่ใช่ A8 อีกแล้ว)** — ตอบทีละ section ได้:
1. `cs` **เห็น** section นั้นไหม
2. `admin` **แก้** section นั้นได้ไหม หรือ **owner-only** — จุดนี้ต่างจาก pacing จริง ๆ ได้:
   F2.3 ให้ `admin` แก้ค่าบริษัทตัวเองได้ แต่ **แบรนด์ที่ผู้เรียนเห็น** อาจเป็นเรื่องที่เจ้าของ
   โปรเจกต์อยากคุมเอง (เป็นสิ่งเดียวใน F2 ที่คนนอกองค์กรมองเห็น ตาม F2.1)
3. ถ้าซ่อนจาก role ใด — endpoint ของค่านั้นปฏิเสธ role นั้นที่ `GET` ด้วยหรือไม่ (SP-15 ข้อ 6)

**ห้าม engineer ตัดสินสามข้อนี้เอง** — ถ้าถึงเวลาเพิ่ม section แล้วยังไม่มีคำตอบ ให้ตีกลับมาที่
`system-analyst` → `business-analyst` · **แต่ "ไม่มีคำตอบ" ไม่ใช่เหตุผลให้รื้อโครงหน้าจออีกแล้ว**
เพราะ SP-15 รองรับทุกคำตอบไว้ครบแล้ว

**A7 (= OQ-6) · เปลี่ยนค่า expiry แล้วลิงก์ที่สร้างไปแล้วเปลี่ยนตามไหม** — *ถ้าเลือก (ก) เท่ากับไม่ต้องทำอะไรเลย เพราะโค้ดทำแบบนั้นอยู่แล้ว จึงไม่ใช่ตัวบล็อก*
- (ก) ไม่ย้อนหลัง ใช้กับลิงก์ใหม่เท่านั้น — *แนะนำ* **ตรงกับที่โค้ดทำอยู่แล้ว**: `ITrainingLinkService.cs:80-82` เก็บ `ExpiresAt` ลงแถวตอนสร้าง = เลือกข้อนี้แล้วไม่ต้องแก้อะไรเลย
- (ข) ย้อนหลังด้วย — ต้องเปลี่ยนวิธีเก็บ `ExpiresAt` ทั้งระบบ (คำนวณตอนอ่านแทนตอนเขียน) = งานใหญ่กว่าทั้งโมดูลนี้รวมกัน

**หมายเหตุเรื่อง OQ-7 เดิม**: ส่วนที่ BA เสนอว่า "ไม่ต้อง backfill" **ถูกต้องสำหรับ F2**
(null = inherit ทำงานเอง ไม่ต้องแตะบริษัทเดิม) — ส่วนที่ **ไม่** ครอบคือ default category chain
ซึ่งแยกออกไปเป็น **B1** และย้ายขึ้นไปกลุ่ม 🔴 แล้ว

**B3a · `null` ที่ชั้นบริษัทแปลว่าอะไร สำหรับค่า pacing** — ✅ **ปิดแล้ว 2026-08-22**

คำถามนี้ **หายไปทั้งข้อ** เพราะ P5 ตัดสินว่าค่า pacing ที่ชั้นบริษัท **ห้ามเป็น `null`** →
คอลัมน์เป็น `NOT NULL` (DM-P1) จึงไม่มีสถานะ `null` ให้ตีความตั้งแต่แรก และไม่ต้องมี flag/sentinel
ใดๆ มาแยก "ยังไม่ตั้ง" ออกจาก "ตั้งเป็นค่านี้" · **นี่คือข้อยกเว้นของกฎ `null = inherit` ที่ F2.2
วางไว้ และเป็นข้อยกเว้นที่ตั้งใจ** — เขียนไว้ชัดที่ LP-1 และหัวตาราง DM-P1 แล้ว
· **คำตอบนี้ใช้กับ pacing เท่านั้น ห้ามเหมารวมไปยังค่าอื่นของ F2**

**B3b · "ค่ากลาง" ของแบรนด์คืออะไร** — ⏸️ **ยังเปิดอยู่ ไม่ถูกแตะในรอบ 2026-08-22**
· ลิงก์หมดอายุกับ TTS มี env กลางอยู่แล้ว
(`ServerDefaults.cs:45-46,269-274`) แต่ **แบรนด์ไม่มีค่ากลางอยู่ที่ไหนเลยในระบบวันนี้**
ฉะนั้นรูปแบบ `null = inherit` ของ F2.2 ต้องนิยามใหม่สำหรับค่านี้โดยเฉพาะ
- (ก) null → ใช้ `Company.Name` เป็นชื่อ + โลโก้/สีของ School Bright ที่เป็น product default ในโค้ด — *แนะนำ* ไม่เพิ่ม env ใหม่ 3 ตัว
- (ข) เพิ่ม env กลางใหม่ `BRAND_DISPLAY_NAME`/`BRAND_LOGO_URL`/`BRAND_PRIMARY_COLOR`
- (ค) แบรนด์ไม่ใช้รูปแบบ inherit — ไม่ตั้งก็ไม่แสดงอะไรเลย

**B4 · รูปร่าง schema ของค่าตั้งค่า** — ✅ **ปิดแล้ว 2026-08-22 · เลือกทางเลือก (ก)**

> **มติ**: คอลัมน์บน `Company` ตรงๆ · **กฎ null สองแบบในตารางเดียวแสดงออกด้วยชนิดของคอลัมน์เอง**
> — pacing = `NOT NULL` (P5) · ค่าอื่นของ F2 = nullable (`null = inherit` ตาม F2.2)
> **ไม่เกิด flag พิเศษแม้แต่ตัวเดียว** · Data Model จริงอยู่ที่ `## Data Model` §DM-P1/DM-P2
> ซึ่งเป็น contract แล้ว ไม่ใช่ร่างอีกต่อไป
>
> **ทำไมไม่ใช่ (ข)**: ตาราง `CompanySetting` ทำให้ "ไม่มีแถว" เป็นสถานะที่เป็นไปได้ ซึ่งขัดกับ P5
> โดยตรง (pacing ต้องมีค่าเสมอ) → ต้องมี repair path + invariant ใหม่ที่ต้องเฝ้า เท่ากับซื้อปัญหา
> แบบเดียวกับ default category chain (B1/CH-6) มาอีกชุดโดยไม่ได้อะไรตอบแทน
> **ทำไมไม่ใช่ (ค)**: F-5/F-6 — owner ที่ switch อยู่บริษัท A จะอ่านค่าของบริษัท B ไม่เจอเลย
>
> **ราคาที่ยอมจ่ายกับทางเลือก (ก) และรู้ตัวแล้ว**: `Company` จะค่อยๆ กว้างขึ้นเมื่อ Module B
> ถูกปลุก (รวมสูงสุด ~9 คอลัมน์) · ยอมรับได้เพราะจำนวนแถวของตารางนี้เท่ากับจำนวนลูกค้า
> (หลักสิบ ไม่ใช่หลักล้าน) และ **R-4 ยังห้ามใส่ฟิลด์เผื่ออนาคตอยู่** — เพิ่มได้เฉพาะค่าที่เคาะแล้ว

ทางเลือกเดิมทั้งสาม เก็บไว้เพื่อให้อ่านย้อนได้ว่าเทียบอะไรบ้าง:
- (ก) **เพิ่ม nullable column ลง `Company` ตรงๆ** — ✅ **เลือกทางนี้** *(เดิมแนะนำ)* `Company` ไม่มี query filter อยู่แล้ว ซึ่งตรงกับความต้องการ "owner แก้ข้ามบริษัทได้" พอดี · ไม่มีเคส "แถวยังไม่ถูกสร้าง" ให้ engineer พลาด · ไม่กระทบ `CompanyIsolationTests` (F-6) · มีแค่ ~6 คอลัมน์
- (ข) ตารางใหม่ `CompanySetting` PK = `CompanyId` **โดยจงใจไม่ implement `ICompanyScoped`** — `Company` สะอาดกว่า แต่เพิ่ม repository + join + เคสกำกวม "ไม่มีแถว" กับ "มีแถวแต่ค่า null" ที่แปลว่า inherit เหมือนกัน
- (ค) ตารางใหม่ + `ICompanyScoped` + query filter — **ไม่แนะนำ** owner ที่ switch อยู่บริษัท A จะอ่านค่าของบริษัท B ไม่เจอเลย (F-5/F-6)

<details>
<summary>ร่างประกอบคำถาม B4 ทางเลือก (ก) — <b>ยัง"ไม่ใช่" contract ห้าม implement</b> (contract จริงคือ DM-P1)</summary>

> ⚠️ **อ่านก่อน (2026-08-22)**: ร่างนี้เขียนไว้ตอนที่ยังไม่มีคำตอบ **และไม่มีค่า pacing อยู่ในนั้นเลย**
> · ส่วนที่กลายเป็น contract จริงแล้วคือ **DM-P1 (3 คอลัมน์ pacing แบบ `NOT NULL`)** เท่านั้น
> · หกคอลัมน์ในร่างนี้ **ยังห้าม implement** จนกว่า A2/A3/A4/B3b จะถูกเคาะ (R-4 + CP-15 + LP-15)

```csharp
// ⚠️ ร่างเพื่อให้เห็นภาพตอนถามเท่านั้น ยังไม่มีใครยืนยัน ชื่อฟิลด์/ชนิด/หน่วยยังเปลี่ยนได้ทั้งหมด
public sealed class Company : IEntityMaster<string>
{
    // ── ของเดิม ไม่แตะ ──
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required bool IsActive { get; set; }
    // + audit fields เดิม

    // ── ใหม่: null = สืบทอดค่ากลาง ห้าม copy ค่ากลางมาเก็บตอนสร้าง (F2.2) ──
    public int? SessionExpiryHours { get; set; }    // ⚠️ ขอบเขตรอ A5
    public string? TtsVoiceId { get; set; }         // ⚠️ รูปแบบรอ A3/A4
    public int? TtsRatePercent { get; set; }        // ⚠️ หน่วยรอ A4
    public string? BrandDisplayName { get; set; }   // ⚠️ ความหมายของ null รอ B3
    public string? BrandLogoUrl { get; set; }       // ⚠️ URL vs storage key รอ A2
    public string? BrandPrimaryColor { get; set; }
}
```

ถ้าทางนี้ถูกเลือกจริง `AdminUser` และ `KnowledgeCategory` **ไม่ต้องแก้เลยแม้แต่ฟิลด์เดียว** —
F1 ใช้ของเดิมทั้งหมด และ migration จะเป็น **additive ล้วน** (เพิ่มคอลัมน์ nullable ไม่มี default
ไม่มี backfill ไม่แตะแถวเดิม) แยกจาก migration ของ `knowledge-base`/`learning-session`
ชื่อประมาณ `AddCompanySettings` · ส่วน B1 ถ้าเลือก (ก)/(ข) จะเป็นอีกใบแยกต่างหาก
ประมาณ `BackfillMissingDefaultCategoryChain` (additive เช่นกัน insert อย่างเดียว
ต้องมี guard ว่าบริษัทที่มี chain แล้วต้องไม่ได้แถวที่สอง ไม่งั้น `GetSystemDefault()` throw ทันที)

</details>

## Change Log

- 2026-08-21 — สร้างเอกสาร (`system-analyst`) จากการสำรวจ feasibility รอบที่ 1 · อ่าน
  `requirement.md` ครบ + ตรวจโค้ดจริง (`Company`, `AdminUser`, `CompanyController`/`ICompanyService`,
  `IAdminUserService`, `IAuthService`, `IAuthorizationGuard`, `ApplicationDbContext`, `UnitOfWork`,
  `ServerDefaults`, `ITtsProvider`/`TtsController`/`ITtsService`, `ITrainingLinkService`,
  `IKnowledgeCategoryRepository`, `CompanyIsolationTests`, migration `AddKnowledgeTaxonomyAndScope`)
  ผลลัพธ์: **ทุกฟีเจอร์ทำได้ด้วย stack เดิม ไม่ต้องเพิ่ม dependency ใดๆ** · บันทึก findings 7 ข้อ
  (F-1..F-7) · ยืนยันว่า OQ-1 และ OQ-6 ตรงกับกลไก/พฤติกรรมที่โค้ดทำอยู่แล้ว · พบคำถามใหม่ 4 ข้อ
  (B1–B4) รวมเป็น 11 ข้อที่ยังไม่เคาะ · เสนอ Module A/B/C พร้อม 🔒 Security gate
- 2026-08-21 — **เจ้าของโปรเจกต์สั่งพักทั้งโมดูล** (ไม่ใช่ core feature · มี workaround คือ owner
  insert DB เอง · ยังไม่มีลูกค้าใหม่รอ onboard จริง) → **ไม่เขียน Data Model, ไม่เคาะ A1–A7/B1–B4,
  ไม่ส่งต่อ `project-manager`** · เอกสารนี้จบที่สถานะ PARKED เก็บผลสำรวจไว้ไม่ให้ต้องตรวจโค้ดซ้ำ
  · วันที่ทั้งหมดใช้ 2026-08-21 ตามที่เจ้าของโปรเจกต์ยืนยันไว้กับ `business-analyst` แล้วในรอบเดียวกัน
- 2026-08-21 (รอบที่ 2) — **เลิกสถานะ PARKED · โมดูลกลับมา ACTIVE เฉพาะ Module A (F1)**
  เพราะมีลูกค้าใหม่จริงที่มีโอกาสเข้ามา ("scb") · **F2/Module B/C ยังพักไว้ตามเดิม** (ทุกบริษัท
  ใช้ค่ากลางได้) · **F-3 ยกระดับเป็น critical path** — scb คือบริษัทใหม่ที่ไม่มีบทเรียน/เอกสารเลย
  ซึ่งตรงเงื่อนไขที่ทำให้ `GetSystemDefault()` คืน null เป๊ะ (R-6 แก้ถ้อยคำใหม่, เพิ่ม R-7)
  · **เขียน `## Data Model` ของ Module A แล้ว: ผลคือไม่มี schema change เลยแม้แต่ฟิลด์เดียว**
  พร้อมเหตุผลว่าทำไมข้อสรุปนี้ไม่ขึ้นกับคำตอบของคำถามที่เหลือ · เพิ่ม Migration Plan (มีได้มากสุด
  1 ใบ และเป็น data-only ขึ้นกับ B1) · แยก contract sections เป็น "รอบนี้ 2 ชุด" กับ "เลื่อนไปพร้อม F2"
  · Module A ผ่าน GAP_ANALYSIS แล้ว (ยืนยันว่า **ไม่แตกเป็นหลาย module**) · จัดกลุ่ม open questions
  ใหม่เป็น 🔴 ต้องเคาะรอบนี้ 5 ข้อ (**A1 · B1 · B2 · N1 · N2** — N1/N2 เป็นคำถามใหม่ที่เจอรอบนี้:
  slug ของบริษัทที่ถูกปิด, `AdminUser.DisplayName` ที่ `requirement.md` ไม่ได้พูดถึงแต่เป็น
  `required` ในโค้ด) กับ ⏸️ เลื่อนไปพร้อม F2 6 ข้อ · **ยังไม่ล็อกเป็น contract** — รอ 5 คำตอบ
- 2026-08-21 (รอบที่ 3) — **เคาะครบทั้ง 5 ข้อ · Module A ปิดงาน design เป็น contract แล้ว**
  · มติ: **B1** = สร้างอัตโนมัติให้บริษัทใหม่ + ซ่อมบริษัทเก่าทันที · **A1** = owner พิมพ์รหัสเอง +
  `MustChangePassword = true` · **B2** = ปล่อยให้ลิงก์เดิมเรียนจนหมดอายุ ไม่แตะฝั่งผู้เรียน ·
  **N1** = ปฏิเสธ slug ซ้ำ + แก้ข้อความ error ให้บอกเหตุผล · **N2** = เพิ่มช่อง `AdminDisplayName`
  ในฟอร์ม · บันทึกทั้ง 5 พร้อม "สิ่งที่คำตอบนี้ตัดออก" ลงตารางการตัดสินใจที่ยืนยันแล้ว
  · **เขียน contract 2 ชุดครบ**: `## Company Provisioning Rules` (CP-1..CP-15) และ
  `## Default Category Chain Rules` (CH-1..CH-8) — รวมกฎที่เคยเป็นกับดัก: single-`Commit()` (CP-6),
  ข้อห้าม `IgnoreQueryFilters()` (CP-12), `Role` ตายตัวกัน privilege escalation (CP-8),
  ข้อความอีเมลซ้ำห้าม enumerate (CP-5), `ON CONFLICT` ใช้แทนการเช็คไม่ได้ใน backfill (CH-6)
  · **R-6 ปิดแล้ว** (B1 ครอบทั้งบริษัทใหม่และบริษัทเดิม) · **เพิ่ม R-8/R-9** เป็นความเสี่ยงที่ยอมรับแล้ว
  จากมติ B2/A1 · เพิ่มคำสั่งถึง `project-manager` เรื่อง 🔒 Security gate แบบระบุเหตุผล 4 ข้อและ
  จุดที่ `security` ต้องดูหนักที่สุด · **F2/Module B/C ยังพักไว้ ไม่แตะ**
- 2026-08-22 (รอบที่ 4 · amend) — **ปิด B4 · B3a · A5 (เฉพาะแถว pacing) จากคำตอบ P1–P5**
  ที่ `business-analyst` สัมภาษณ์เจ้าของโปรเจกต์ไว้ใน `learning-session/requirement.md`
  · **B4 = ทางเลือก (ก) คอลัมน์บน `Company` ตรงๆ** — กฎ null สองแบบในตารางเดียวแสดงออกด้วย
  ชนิดของคอลัมน์ (pacing `NOT NULL` · ค่าอื่นของ F2 nullable) ไม่เกิด flag พิเศษเลย ·
  **เขียน `## Data Model` §Module P ใหม่ทั้งส่วน**: DM-P1 (`Company` +3 คอลัมน์ `Default*Ms`
  **additive + backfill**) · DM-P2 (`LessonConfig` 3 คอลัมน์เป็น `int?` — **ขยายชนิด ไม่ breaking
  กับข้อมูล แต่ breaking กับ contract ของ DTO/ViewModel/TS types** → R-10) · Migration Plan
  ใบเดียว `AddCompanyLessonPacingDefaults` พร้อมข้อบังคับ 4 ข้อ (literal ไม่ใช่ env · ขั้นตอนของ
  `devops` · rollback ไม่สมมาตร · ห้ามแยกสองใบ) · **contract ชุดใหม่ `## Lesson Pacing Resolution
  Rules` LP-1..LP-15** ครอบทั้ง resolve จุดเดียว, `0` ≠ `null`, endpoint+สิทธิ์, TS types,
  การปิดชุดค่า default ที่เพี้ยนสองจุด (P2), unit test ที่ต้องมี, และข้อห้าม · เพิ่ม **CP-16**
  (ทั้งสองจุดที่ `new Company` ต้องตั้งค่า) และแก้ขอบเขตข้อห้ามท้าย CP-15 · **แตก Module P
  ออกมาจาก Module B** พร้อม 🔒 Security gate ที่มีเหตุผลของตัวเอง 3 ข้อ · เพิ่ม **R-10 · R-11 ·
  R-12 · D-3 · D-4** · **ยังเปิดอยู่: A2 · A3 · A4 · A6 · B3b (แบรนด์)** และ **D-3 คือหนี้เอกสาร
  ข้ามโมดูลที่ต้องปิดด้วยรอบ `system-analyst` ของ `knowledge-base` ต่างหาก**
  · วันที่ 2026-08-22 ผู้ใช้ยืนยันในเซสชันแล้ว
- 2026-08-22 (รอบที่ 5 · amend) — **ยกข้อห้าม LP-15 ข้อสุดท้ายออก และเปิดทางให้ทำหน้า UI
  ตั้งค่าบริษัทได้ทันที (มติ P6)** จากที่เจ้าของโปรเจกต์ตัดสินใจใหม่ในแชทว่า **ไม่ต้องรอ F2
  ทั้งก้อน** ให้เริ่มจาก section ของ pacing ก่อนแล้วเติมค่าอื่นทีละอย่างทีหลัง · **ไม่มี schema
  change ในรอบนี้ — `## Data Model` ไม่ถูกแตะแม้แต่ฟิลด์เดียว** และ **ไม่มีงาน backend ใหม่เลย**
  (endpoint LP-9 เสร็จ + ทดสอบสดไปแล้วใน Phase 4) งานที่เกิดเป็น **frontend ล้วน** ·
  **เขียน contract ชุดใหม่ `## Company Settings Page Rules` (SP-1..SP-14)**: route
  `/admin/settings` แบบหน้าเดียวแบ่ง `Card` ต่อ section (**ปฏิเสธ Tabs** ด้วยเหตุผลเดียวกับ F4.2
  ที่ปฏิเสธ dropdown ตัวเลือกเดียว) · หนึ่ง section = หนึ่ง component ที่โหลด/เซฟ/validate/
  ตัดสินสิทธิ์ของตัวเอง **ห้ามมีปุ่มบันทึกรวม** (B4 แยก endpoint ไว้แล้ว) · `companyId` จาก session
  เท่านั้นและ refetch เมื่อ owner สลับบริษัท · **สิทธิ์เป็นของ section ไม่ใช่ของหน้า** ·
  **SP-7 ระบุชัดว่าที่ชั้นบริษัท "ว่าง" ไม่ใช่ค่าที่ถูกต้อง — ตรงข้ามกับ LP-11 ของฟอร์มบทเรียน**
  (จุดที่พลาดง่ายที่สุดของรอบนี้) · ข้อห้าม SP-13 + test บังคับ SP-14 · **ปิด A6 เฉพาะส่วน
  "ตัวหน้า + section pacing" = (ข) `cs` อ่านอย่างเดียว** ให้ตรงเจตนา LP-9 ที่ให้ `cs` `GET` ได้อยู่แล้ว
  · **เพิ่มคำถามใหม่ A8** (สิทธิ์ต่อ section ตอนเพิ่ม section ที่สอง — ไม่บล็อกรอบนี้ แต่ห้าม
  engineer ตัดสินเอง) · **เพิ่ม R-13** (หน้าที่มี section เดียวไปอีกระยะ — ยอมรับแล้ว) และ
  **R-14** (การขยับ gate ของกลุ่มเมนู "ตั้งค่า" อาจเปิด `/admin/users` ให้ `cs` ติดมาด้วย) ·
  เพิ่มข้อ 4 ในหมายเหตุ 🔒 Security gate ของ Module P และแก้ข้อ 3 ที่เขียนไว้ว่า "ทดสอบด้วยตา
  จาก UI ไม่ได้เพราะยังไม่มีหน้าจอ" ให้ตรงความจริงใหม่ (มีจอแล้ว **แต่การกดจากจอไม่ใช่หลักฐาน**
  ต้องยิง `PUT` ตรงด้วย JWT ของ `cs` เหมือนเดิม) · เพิ่มงานข้อ 10 ใน Module P · **A2 · A3 · A4 ·
  B3b ยังเปิดเหมือนเดิม และ Module B ยัง ⏸️ พักอยู่ — P6 อนุญาต "หน้าจอ" ไม่ได้อนุญาต "ค่า"**
  · วันที่ 2026-08-22 ผู้ใช้ยืนยันในเซสชันแล้ว
- 2026-08-22 (รอบที่ 6 · amend เท่านั้น ไม่มีโค้ด **ไม่มี schema change** `## Data Model` ไม่ถูกแตะ)
  — **ปิด A8 และปิด LP-8/A5 แถว pacing จากคำตอบที่เจ้าของโปรเจกต์ให้ตรงในแชทวันนี้**
  · **A8**: "สิทธิ์การมองเห็น" กับ "สิทธิ์แก้" เป็น **คนละแกน** ตั้งค่าแยกต่อ section ได้ และ
  **บาง section ซ่อนจาก role ไปเลยได้ ไม่ใช่แค่ read-only** ส่วน section ที่ไม่อ่อนไหว (pacing/เสียง)
  เห็นได้ทุก role → **เขียนเป็น contract ใหม่ SP-15** (type `SettingsSectionAccess` ที่มี
  `visibleToRoles`/`editableByRoles` แยกกัน · registry เดียวที่ `sections.ts` · "ซ่อน" = ไม่ mount
  ไม่ยิง `GET` ไม่มีข้อความบอกว่ามีของที่คุณไม่เห็น · invariant `editableByRoles ⊆ visibleToRoles`
  และ `owner` อยู่ครบทั้งสองรายการเสมอ · การซ่อนที่ UI ไม่ใช่การกั้นสิทธิ์ ต้องมีกฎฝั่ง server คู่กัน
  หรือประกาศว่า cosmetic · เมนู sidebar **derive** จาก registry ห้าม hardcode role · test เพิ่ม
  `resolveSectionAccess` สามroles) · **section pacing ไม่เปลี่ยนแม้แต่จุดเดียว** — เห็นทุก role
  แก้ได้ `owner`/`admin` ตรง LP-9/SP-4 เดิม · **LP-8**: เจ้าของโปรเจกต์ยืนยันใช้ช่วงเดิม
  `0–60000` / `0–10000` / `0–120000` ms ("จูนทีหลังได้") → ลบสถานะ "ข้อเสนอที่ยังไม่ผ่านปาก"
  ออกจาก LP-8 · A5 · SP-8 · เพิ่มสองแถวลงตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" · แก้ SP-4/SP-5/
  SP-8/SP-12 ให้ตรงกลไกใหม่ · **เพิ่ม R-15** (ความเสี่ยงว่าการซ่อน section จะถูกเข้าใจผิดว่าเป็น
  การกั้นสิทธิ์ — ยังไม่เกิดจริงรอบนี้ แต่เป็นของที่ `security` ต้องตรวจในรอบที่มี section ที่สอง)
  · **ผลรวม: ไม่มี open question ใดค้างอยู่กับ Module P หรือหน้า `/admin/settings` (section pacing)
  อีกแล้ว** — เหลือเปิดเฉพาะของ F2 ที่ยังพัก (A2 · A3 · A4 · B3b · A6 ส่วนที่ไม่ใช่ pacing)
  · วันที่ 2026-08-22 มาจากผู้ใช้ในคำสั่งของรอบนี้
- 2026-08-22 (รอบที่ 7 · amend — **กลับคำตอบ P1 ของ `learning-session/requirement.md`,
  ผลกระทบเป็น breaking schema change**) — เจ้าของโปรเจกต์เห็นหน้าจอฟอร์มบทเรียนจริงแล้วตัดสินใจ
  ใหม่ว่า **ช่อง pacing ระดับบทเรียนซ้ำซ้อนกับหน้า `/admin/settings` ที่เพิ่งทำ ควรตัดทิ้งให้สะอาด
  ไม่ใช่เก็บไว้ควบคู่กัน** — กลับคำตอบ P1 จาก "บทเรียน override ได้" เป็น **"บังคับใช้ค่ากลางบริษัท
  ล้วน ไม่มี per-lesson override อีกต่อไป"** พร้อมมติ **N1/N2/N3** สามข้อที่ยืนยันตรงในแชท:
  **N1** ตัดช่อง pacing ออกจากฟอร์มบทเรียนถาวร · **N2** บทเรียนเดิมที่เคยมีค่าเฉพาะบท (เช่น
  3000/500/3000) **ทิ้งทั้งหมด ไม่ backfill เป็น override** (แทนที่ P4 เดิมที่เพิ่งทำไปหมาดๆ) ·
  **N3** ลบคอลัมน์ pacing ออกจาก `LessonConfig` จริงในฐานข้อมูล ไม่ใช่แค่ซ่อน UI หรือปล่อยเป็น
  nullable ที่ตายแล้ว

  **ผลต่อ contract**: `## Lesson Pacing Resolution Rules` ตัด/เขียนใหม่ทุกข้อที่เคยรองรับ
  per-lesson override และ empty-vs-zero (LP-3/LP-6/LP-11/LP-12/LP-13 เดิม) เหลือ **โมเดลสืบทอด
  ชั้นเดียว** (อ่านค่าจาก `Company` ตรงๆ เท่านั้น) · **DM-P2 กลับทิศทางเป็นครั้งที่สอง**:
  จาก "เพิ่ม nullable column" (รอบที่ 4) กลายเป็น **"ไม่มีคอลัมน์ pacing ใน `LessonConfig` เลย"**
  · เพิ่ม migration ใบใหม่ **`RemoveLessonConfigPacingOverrides`** (`DropColumn` สามคอลัมน์)
  เข้า Migration Plan (Module P) พร้อมข้อบังคับ: ห้ามมี `UPDATE` กู้ค่าเดิมก่อนลบ (ขัดกับ N2
  ตรงๆ), ต้อง deploy ติดกับโค้ดที่เลิกอ่านคอลัมน์นี้เสมอ (ไม่งั้น query `LessonConfig` ทั้งตาราง
  พังจากคอลัมน์ที่หายไปกลางทาง ไม่ใช่แค่ฟีเจอร์ pacing) · เพิ่ม **R-16** (data loss ที่ตั้งใจ —
  ยอมรับแล้วพร้อม 3 เงื่อนไข: comment อธิบายเจตนาใน migration, `devops` backup ตาราง
  `LessonConfig` ก่อนรัน, ถ้าลูกค้าทักเรื่องจังหวะเปลี่ยนคำตอบคือตั้งค่ากลางใหม่ไม่ใช่กู้ค่าเดิม) และ
  **R-17** (Phase 4/5 implement ครบตามสัญญาเดิมแล้วแต่ยังไม่ผ่าน QA — ความเสี่ยงจริงของรอบนี้คือ
  ถอดออก "ครึ่งทาง" เช่น ลบคอลัมน์แต่ฟอร์มยังมีช่อง, `qa-engineer` ต้องถือรอบแรกของ Phase 4/5
  เป็น FULL เสมอ ไม่มี TARGETED)

  **§Modules** แบ่งงานของ Module P เป็น 3 กลุ่มให้ `project-manager` ใช้ตั้ง task โดยไม่ต้องเดา:
  ยังถูกต้องไม่ต้องทำซ้ำ (`Company` +3 คอลัมน์, endpoint LP-9, หน้า `/admin/settings`+registry) ·
  ต้องถอด/แก้ย้อนหลัง (migration ใบใหม่, entity/DTO/ViewModel/`domain.ts` ตัดสามฟิลด์, ฟอร์ม
  บทเรียนถอดสามช่อง, ชะตากรรมของ `ILessonPacingResolver` ให้ PM/engineer ตัดสินเอง — contract
  ต้องการแค่ "อ่านค่าบริษัทตรงๆ ไม่มี logic 2 ชั้น" ไม่ได้สั่ง implementation detail) · งานใหม่
  (ไม่มี — รอบนี้เป็นการถอดออกล้วน) · เพิ่ม **D-4** ยืนยันซ้ำว่า Module P ไม่ขึ้นกับ A2/A3/A4/A6/B3b

  **D-3 เปลี่ยนคำตอบเป็นรอบที่สอง**: จาก "amend `knowledge-base/design.md` §DM-2 ให้เป็น
  nullable" (รอบที่ 4) เป็น **"ลบสามฟิลด์ `IntroWaitMs`/`BreathPauseMs`/`FinalQuestionWaitMs`
  ออกจาก DM-2 ไปเลย"** — ยังไม่ปิดในรอบนี้เพราะ `conventions.md` §1 ห้ามเขียนนอกโฟลเดอร์โมดูล
  ที่ resolve ไว้ ต้องสั่งรอบใหม่ให้ `knowledge-base` โดยเฉพาะ พร้อมคำเตือนไว้ในตารางว่าอย่าไปใช้
  ทิศทาง nullable เดิมถ้ารอบนั้นยังไม่เห็นการอัปเดตนี้

  เพิ่ม **OQ-P7** (ถ้าวันหนึ่งต้องการจังหวะเฉพาะบทเรียนกลับมา — บันทึกไว้ว่าต้องทำอะไรบ้าง
  ไม่ใช่คำถามเปิดที่ต้องตอบตอนนี้) · **ไม่มีการ implement โค้ดในรอบนี้** — เป็น doc-only amend
  ทั้งหมด ตามที่เจ้าของโปรเจกต์สั่งชัดเจนว่าให้ `system-analyst` แก้ design ก่อน แล้วค่อยส่ง
  `project-manager` จัดลำดับงานถอด/แก้ Phase 4/5 · วันที่ 2026-08-22 ผู้ใช้ยืนยันในเซสชันแล้ว
- 2026-08-25 (รอบที่ 8 · amend — **เปิด scope F5 กลับ · ไม่มี schema change · ไม่มี migration**)
  — `business-analyst` แก้ `requirement.md` §F5 หลังพบว่าการปิด scope เมื่อ 2026-08-21
  **ตั้งอยู่บนข้อเท็จจริงที่ผิด** (ตรวจโค้ดจริงแล้ว `/admin/users` แก้อีเมล/รีเซ็ตรหัสของผู้ใช้
  รายอื่นไม่ได้เลย — `UpdateAdminUserDto` มีแค่ `DisplayName`/`Role`/`IsActive` และทางเดียวที่
  เปลี่ยนรหัสได้คือ `POST /api/auth/change-password` ซึ่งเปลี่ยนของตัวเองและต้องรู้รหัสเดิม)
  · **เป็นการแก้ข้อเท็จจริง ไม่ใช่การเปลี่ยนใจของเจ้าของโปรเจกต์** — บันทึกไว้ชัดทั้งใน CP-15
  และตารางมติ เพื่อไม่ให้รอบหน้าอ่านว่าเป็นการกลับกลอก
  · **แก้ CP-15**: ครึ่งหลังของบูลเล็ต "ห้ามสร้างหน้าจัดการผู้ใช้ใหม่" ถูกยกเลิก (ส่วน
  "ห้ามสร้าง _หน้า_ ใหม่" ยังใช้อยู่ทุกตัวอักษร) และ **แก้ขอบเขตบูลเล็ต role model**: ห้าม
  *เปลี่ยนความหมาย* ของ `AdminRole`/เมธอดเดิมของ `IAuthorizationGuard` แต่ **เพิ่ม** เมธอดใหม่ได้
  เมื่อ requirement สั่งตรง (F5.2.1)
  · **contract ชุดใหม่ `## Admin User Management Rules` (AU-1..AU-16)** ครอบ: รูปร่าง DTO
  (U1 — ขยายใบเดิม ไม่แตกสาม endpoint) · ลำดับการตรวจสิทธิ์ทั้งเก้าขั้นพร้อมเหตุผลว่าทำไม
  ลำดับนี้ (AU-3) · peer-lockout เป็นเมธอดใหม่ `EnsureNotSameRankPeer` (AU-4) · ห้ามใช้กับ
  ตัวเองแบบไม่ยกเว้น `owner` (AU-5) · กฎอีเมลรวมเคส "เปลี่ยนแค่ตัวพิมพ์ = ไม่ติดธง" และ
  "ห้ามเช็คซ้ำกับแถวตัวเอง" (AU-6) · กฎรหัสผ่านรวม "ห้ามใส่ `[MinLength]` บน DTO" และ
  "ห้าม trim รหัสผ่าน" (AU-7) · สองเส้นทางของ `MustChangePassword` + ยืนยันจากโค้ดจริงว่า
  middleware เดิมบังคับให้ทุก request อยู่แล้ว **จึงห้ามสร้าง token revocation ใด ๆ** (AU-8) ·
  **AU-9 เคสที่ `PasswordHash == null` แล้วติดธง = บัญชีตายถาวร → ต้องปฏิเสธ** (เคสที่ไม่มีใคร
  ยกขึ้นมาก่อน เจอตอนอ่าน entity จริง) · ตารางผลลัพธ์ครบทุกคู่ actor×target (AU-12) ·
  งาน **ลบ** ที่ frontend ต้องทำจริงพร้อมเลขบรรทัด (AU-13) · unit test 11 ข้อ (AU-15) ·
  ข้อห้าม 10 ข้อ (AU-16)
  · **`## Data Model` เพิ่ม §Module U: ไม่มี schema change เลยแม้แต่ฟิลด์เดียว** — ตรวจ
  `AdminUser` จริงแล้วทุกฟิลด์ที่ F5 ใช้มีครบ ตรงกับ F5.3 · **amend-mode call: ไม่ใช่ทั้ง
  additive และไม่ใช่ breaking — คือ "ไม่มี schema change"** ถ้ามีใครสร้าง migration ให้ Module U
  = ผิด contract
  · **`## Modules` เพิ่ม Module U** (ไม่แตกเป็นหลาย module · ขึ้นกับ Module A แบบ regression
  surface ไม่ใช่ dependency) พร้อม **🔒 Security gate ที่มีเหตุผลเฉพาะของตัวเอง 6 ข้อ** —
  โมดูลนี้อ่อนไหวกว่า Module A/P เพราะเป็นครั้งแรกที่ระบบให้คนหนึ่งตั้ง credential ของอีกคน
  · **ทุก phase ที่ implement Module U ต้องติด `🔒 Security gate` ไม่มีข้อยกเว้น รวม phase ที่ทำแค่ UI**
  · เพิ่ม **R-18** (peer-lockout เทียบ role ปัจจุบัน → เลื่อน cs เป็น admin แล้วแตะกันไม่ได้) ·
  **R-19** (`Email` required = breaking wire contract → backend/frontend ต้อง deploy พร้อมกัน
  ไม่งั้นหน้า `/admin/users` ที่ใช้อยู่จริงพังทุกการกด) · **R-20** (ข้อความอีเมลซ้ำ enumerate
  ข้ามบริษัทได้ — พฤติกรรมเดิมของ `Create` แต่ต้องอยู่ในรายงาน `security` คู่กับ R-2) · **D-5**
  · เพิ่มกลุ่ม **OQ-U1/OQ-U2/OQ-U3** — ทั้งสามข้อ **ไม่บล็อกการ implement** เพราะ contract
  ตัดสินไว้แล้วในทิศ "ทำน้อยที่สุด ไม่ขยายสโคปเอง" แต่บันทึกไว้เพราะเป็นคำถามธุรกิจ/ดีไซน์:
  **OQ-U1** บัญชี `owner` ไม่เคยอยู่ในตาราง `/admin/users` (`CompanyId = null`) → กฎ
  `owner`×`owner` ที่เคาะไว้ไม่มีหน้าจอไปถึงจริง · **OQ-U2** peer-lockout ควรกันการเลื่อนคนอื่น
  ขึ้นมาเป็น peer ด้วยไหม · **OQ-U3** โมดัลไม่มีช่อง "ชื่อที่แสดง" ตาม Figma → `DisplayName`
  ยังแก้ไม่ได้จากที่ไหนเลย (ไม่ใช่ regression) · **OQ-15 (ข้อความปุ่ม Cancel) ยังเปิดที่
  `requirement.md` ตามเดิม — `frontend-engineer` ห้ามตั้งเอง**
  · **ไม่มีการแก้โค้ดในรอบนี้** เป็น doc-only amend · **`schema.prisma`/EF schema ไม่ต้อง
  propagate อะไรเลย** เพราะไม่มี schema change (ต่างจากรอบที่ 4/7) · วันที่ 2026-08-25
  ผู้ใช้ยืนยันในเซสชันแล้ว
