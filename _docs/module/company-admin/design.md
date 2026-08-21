# การรับลูกค้าใหม่และตั้งค่าระดับบริษัท (company-admin) — Feasibility & Design

> # 🟢 สถานะ: ACTIVE — Module A (F1) เท่านั้น · Module B (F2) ยังพักไว้
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

**ยังเปิดอยู่**: ⏸️ 6 ข้อที่เลื่อนไปพร้อม F2 (**A2 · A3 · A4 · A5 · A6 · B4** — และ B3)
ทั้งหมดอยู่ที่ `## Unresolved Open Questions` · **ไม่มีข้อไหนบล็อก Module A**

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

### Module B (F2) — ยังไม่มี Data Model

ยังพักไว้ · **B4 (คอลัมน์บน `Company` vs ตารางใหม่) ยังไม่เคาะและยังไม่ต้องเคาะรอบนี้**
ร่างที่อยู่ใน `## Unresolved Open Questions` เป็นภาพประกอบคำถาม **ไม่ใช่ contract ห้าม implement**

### หมายเหตุถึง `qa-engineer`

โมดูลนี้ **ไม่ประกาศ model ใหม่เลยสักตัว** — ทุก entity ที่อ้างถึงเป็นของโมดูลอื่น
(`KnowledgeCategory` เป็นของ `knowledge-base`) หรือของ baseline เดิม (`Company`, `AdminUser`)
ตาม `conventions.md` §7 การเทียบจึงเป็นการยืนยันว่า **entity เหล่านั้นยังเหมือนเดิมทุกฟิลด์**
ไม่ใช่การหาตารางใหม่ · ถ้ารอบ QA เจอว่า `Company` หรือ `AdminUser` มีฟิลด์เพิ่มขึ้นจากที่ตารางข้างบน
ระบุ นั่นคือ **drift** — ตีกลับมาที่ `system-analyst` ไม่ใช่รับเป็น baseline ใหม่

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
- **ห้ามแก้ role model** (`AdminRole`, `IAuthorizationGuard`) — ใช้ตามที่มี
- **ห้ามสร้างหน้าจัดการผู้ใช้ใหม่** — เพิ่ม/ปิด/รีเซ็ตรหัส admin/cs รายอื่นใช้ `/admin/users` เดิม
- **ห้ามเพิ่มฟิลด์เผื่อแพ็กเกจ/โควตา/สัญญา/usage** ลงตารางใดก็ตาม (R-4)
- **ห้ามเพิ่มการตั้งค่าระดับบริษัท (F2) เข้ามาในรอบนี้** แม้จะดูเหมือนทำพร้อมกันง่ายกว่า

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

## Contract sections ที่เลื่อนไปพร้อม F2 (Module B) — ยังไม่เขียน

`## Company Settings Resolution Rules` (ขึ้นกับ A3/A4/A5/A6/B4) และ `## Brand Delivery Rules`
(ขึ้นกับ A2/B3 — **B2 เคาะแล้วรอบนี้ ใช้ต่อได้เลยตอนนั้น**)

## Modules

> **Module A ผ่าน GAP_ANALYSIS แล้ว** (รอบ 2026-08-21) — เหลือแค่กฎธุรกิจ 5 ข้อที่ยังไม่เคาะ
> **Module B/C ยังเป็นข้อเสนอและยังพักไว้** `project-manager` วางแผนได้เฉพาะ Module A
> และเฉพาะหลังจาก A1/B1/B2/N1/N2 ถูกเคาะแล้วเท่านั้น
>
> **ไม่แตก Module A ออกเป็นหลาย module** — F1.1–F1.6 ใช้ตารางชุดเดียวกัน อยู่ใน service เดียวกัน
> และส่งมอบครึ่งเดียวไม่ได้ (สร้างบริษัทได้แต่ดูรายการไม่ได้ = ยังต้องเปิด DB อยู่ดี)
> ขนาดงานเล็กพอที่จะส่งเป็นก้อนเดียวโดยไม่เสี่ยง ตามกฎ GAP_ANALYSIS ข้อ 2

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

### Module B · Company Settings — ⏸️ ยังพักไว้ ไม่อยู่ในสโคปรอบนี้

F2.1–F2.4 · ขึ้นกับ Module A (ต้องมีบริษัทให้ตั้งค่าก่อน) · ยังไม่มี Data Model
เพราะ B4 ยังไม่เคาะ · เหตุผลที่พัก: ทุกบริษัทรวมทั้ง scb ใช้ค่ากลางจาก env ได้อยู่แล้ว

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

### ⏸️ กลุ่มที่เลื่อนไปพร้อม F2 (ยังไม่ต้องตอบรอบนี้)

> **A1 · B1 · B2 · N1 · N2 ไม่อยู่ในกลุ่มนี้แล้ว** — เคาะครบเมื่อ 2026-08-21 ดูตาราง
> "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว"

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

**A5 (= OQ-4) · ขอบเขตค่าที่รับได้** — ตัวเลขข้างล่างเป็น *ข้อเสนอ* ต้องให้เจ้าของโปรเจกต์ยืนยันหรือแก้

| ค่า | เสนอ | เหตุผล |
|---|---|---|
| ลิงก์หมดอายุ | 1–720 ชั่วโมง (1 ชม. – 30 วัน) | ต่ำกว่า 1 ชม. ครูเปิดทีหลังในวันเดียวกันก็ไม่ทัน · เกิน 30 วัน ลิงก์ค้างกลายเป็นพื้นที่เสี่ยง |
| ความเร็วพูด | -50% ถึง +50% | ตรงกับ regex ที่ระบบ validate อยู่แล้ว (`SynthesizeSpeechDto.cs:19` รับ ±2 หลัก) และเกินกว่านี้ Edge ฟังไม่รู้เรื่อง |
| สีแบรนด์ | `^#[0-9a-fA-F]{6}$` | |
| ชื่อแบรนด์ | ≤ 60 ตัวอักษร | |

**A6 (= OQ-5) · `cs` เห็นหน้าตั้งค่าไหม**
- (ก) ไม่เห็นเลย (ไม่มีเมนู)
- (ข) เห็นแบบอ่านอย่างเดียว — *แนะนำเล็กน้อย* เพราะ CS ต้องตอบครูได้ว่า "ลิงก์ของบริษัทนี้อยู่กี่ชั่วโมง"
- (ค) เห็นแต่กดแก้แล้วเด้ง error — ไม่แนะนำ UX แย่

**A7 (= OQ-6) · เปลี่ยนค่า expiry แล้วลิงก์ที่สร้างไปแล้วเปลี่ยนตามไหม** — *ถ้าเลือก (ก) เท่ากับไม่ต้องทำอะไรเลย เพราะโค้ดทำแบบนั้นอยู่แล้ว จึงไม่ใช่ตัวบล็อก*
- (ก) ไม่ย้อนหลัง ใช้กับลิงก์ใหม่เท่านั้น — *แนะนำ* **ตรงกับที่โค้ดทำอยู่แล้ว**: `ITrainingLinkService.cs:80-82` เก็บ `ExpiresAt` ลงแถวตอนสร้าง = เลือกข้อนี้แล้วไม่ต้องแก้อะไรเลย
- (ข) ย้อนหลังด้วย — ต้องเปลี่ยนวิธีเก็บ `ExpiresAt` ทั้งระบบ (คำนวณตอนอ่านแทนตอนเขียน) = งานใหญ่กว่าทั้งโมดูลนี้รวมกัน

**หมายเหตุเรื่อง OQ-7 เดิม**: ส่วนที่ BA เสนอว่า "ไม่ต้อง backfill" **ถูกต้องสำหรับ F2**
(null = inherit ทำงานเอง ไม่ต้องแตะบริษัทเดิม) — ส่วนที่ **ไม่** ครอบคือ default category chain
ซึ่งแยกออกไปเป็น **B1** และย้ายขึ้นไปกลุ่ม 🔴 แล้ว

**B3 · "ค่ากลาง" ของแบรนด์คืออะไร** — ลิงก์หมดอายุกับ TTS มี env กลางอยู่แล้ว
(`ServerDefaults.cs:45-46,269-274`) แต่ **แบรนด์ไม่มีค่ากลางอยู่ที่ไหนเลยในระบบวันนี้**
ฉะนั้นรูปแบบ `null = inherit` ของ F2.2 ต้องนิยามใหม่สำหรับค่านี้โดยเฉพาะ
- (ก) null → ใช้ `Company.Name` เป็นชื่อ + โลโก้/สีของ School Bright ที่เป็น product default ในโค้ด — *แนะนำ* ไม่เพิ่ม env ใหม่ 3 ตัว
- (ข) เพิ่ม env กลางใหม่ `BRAND_DISPLAY_NAME`/`BRAND_LOGO_URL`/`BRAND_PRIMARY_COLOR`
- (ค) แบรนด์ไม่ใช้รูปแบบ inherit — ไม่ตั้งก็ไม่แสดงอะไรเลย

**B4 · รูปร่าง schema ของค่าตั้งค่า — ข้อที่บล็อกหนักที่สุด** ตอบข้อเดียวปลดล็อกทั้ง Data Model
- (ก) **เพิ่ม nullable column ลง `Company` ตรงๆ** — *แนะนำ* `Company` ไม่มี query filter อยู่แล้ว ซึ่งตรงกับความต้องการ "owner แก้ข้ามบริษัทได้" พอดี · ไม่มีเคส "แถวยังไม่ถูกสร้าง" ให้ engineer พลาด · ไม่กระทบ `CompanyIsolationTests` (F-6) · มีแค่ ~6 คอลัมน์
- (ข) ตารางใหม่ `CompanySetting` PK = `CompanyId` **โดยจงใจไม่ implement `ICompanyScoped`** — `Company` สะอาดกว่า แต่เพิ่ม repository + join + เคสกำกวม "ไม่มีแถว" กับ "มีแถวแต่ค่า null" ที่แปลว่า inherit เหมือนกัน
- (ค) ตารางใหม่ + `ICompanyScoped` + query filter — **ไม่แนะนำ** owner ที่ switch อยู่บริษัท A จะอ่านค่าของบริษัท B ไม่เจอเลย (F-5/F-6)

<details>
<summary>ร่างประกอบคำถาม B4 ทางเลือก (ก) — <b>ไม่ใช่ contract ห้าม implement</b></summary>

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
