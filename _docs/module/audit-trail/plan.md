# บันทึกประวัติการกระทำในระบบ (audit-trail) — Implementation Plan

## ✅ C1 ปลดล็อกแล้ว 2026-08-28 — เจ้าของโปรเจกต์ให้ไฟเขียว migration ของโมดูลนี้โดยตรง

**เจ้าของโปรเจกต์พูดเองในแชท 2026-08-28 ว่า "ให้ไฟเขียวตอนนี้เลย"** เมื่อถูกถามตรงๆ ว่าจะให้ไฟเขียว
migration ของ `audit-trail` หรือยัง — เข้าเงื่อนไขที่ `requirement.md`/`design.md` วางไว้ว่าต้องเป็น
คำพูดยืนยันแยกต่างหาก ไม่ใช่แค่เอกสารเขียนเสร็จ **นับเป็นไฟเขียวแล้วตั้งแต่นี้**

- `backend-engineer` **dispatch ได้แล้ว** ทั้ง Phase 1 และ Phase 2 (Phase 2 ยังต้องรอ Phase 1 เสร็จ
  ก่อนตามลำดับใน Sequencing Notes — คนละเงื่อนไขกับ C1)
- `dotnet ef database update` **รันได้แล้ว** สำหรับ migration `AddAuditLog` ของโมดูลนี้ — แต่ต้อง
  rehearsal บน staging ก่อนเสมอตามที่ Sequencing Notes ระบุ (คิว migration ที่ยังไม่เคย apply มีอยู่
  ก่อนหน้าแล้ว คือ `20260813140603_SplitLinkAndAuth`) และห้ามรัน `database update` กับ production
  โดยไม่ผ่าน `devops`
- ทั้งสอง phase ยังติด `🔒 Security gate` ตามปกติ — ไม่ถูกปลดไปพร้อม C1 เป็นเงื่อนไขคนละชั้น
  `security` ยังต้อง audit ก่อน `devops` จะ ship ได้

## Plan Summary

โมดูลนี้มี 2 phase ตรงกับ 2 Module ใน `design.md` เป๊ะ — **Phase 1 = Module A (ตาราง `AuditLog`
+ `SaveChanges` interceptor), Phase 2 = Module B (5 จุด raw-SQL ที่ interceptor มองไม่เห็น)**
Phase 2 ขึ้นกับ Phase 1 โดยตรง: ทั้งตาราง `AuditLog` และกติกากันนับซ้ำ (AU-4) ต้องมีอยู่ก่อน
5 จุดใน Phase 2 จะเขียนแถวได้ถูกต้อง — เรียงตามที่ `design.md` §"ทำไมแบ่งเป็น 2 โมดูล" ระบุไว้แล้ว
ไม่ใช่การจัดลำดับใหม่ของ `project-manager`

โปรเจกต์ scaffold ไว้แล้ว (ASP.NET Core + EF Core + PostgreSQL) ไม่ต้องผ่าน `setup` — งานทั้งหมด
เป็น `[backend]` ล้วน ไม่มี `[frontend]` เลย เพราะ R6 ตัด UI/endpoint ออกจากขอบเขตรอบนี้ทั้งหมด

โปรเจกต์มี test suite จริง (xUnit, `backend/tests/`) จึงมีงานเทสต์ตาม AU-20 — เฉพาะกติกาที่
ทดสอบได้จริงด้วย EF InMemory เท่านั้น (AU-15 ทดสอบด้วย InMemory ไม่ได้เพราะไม่มี transaction จริง
ดู Sequencing Notes)

## Phase 1: Module A — ตาราง `AuditLog` + `SaveChanges` interceptor 🔒 Security gate

**✅ C1 ปลดล็อกแล้ว 2026-08-28 (ดูหัวไฟล์) — dispatch ให้ `backend-engineer` ได้เลย**

### Entity / constants (DM-A1, DM-A2)

- [ ] [backend] สร้าง `backend/src/SupportRoom.Domain/Entities/AuditLog.cs` ตาม DM-A1 เป๊ะ — 7
      field เท่านั้น (`Id`, `CompanyId` nullable, `ActorUserId` required, `Action` required,
      `EntityName` required, `EntityId` required, `OccurredAt` required) · **ห้าม** implement
      `IEntityMaster` (มติ Q-A1) · **ห้าม** เติม `BeforeJson`/`AfterJson`/`MetadataJson`/
      `IpAddress`/`UserAgent`/`ActorRole`/`CorrelationId` แม้แต่ field เดียว
- [ ] [backend] สร้าง `backend/src/SupportRoom.Domain/Enums/AuditAction.cs` ตาม DM-A2 — static
      class + const string (`Create`/`Update`/`Delete`) ตาม convention ของโปรเจกต์ ห้ามใช้ C# enum

### `ApplicationDbContext` — schema + ctor (DM-A3, DM-A4)

- [ ] [backend] เพิ่ม `builder.Entity<AuditLog>(...)` ใน `OnModelCreating` ตาม DM-A3 — `HasKey`,
      3 index (`{CompanyId, OccurredAt}`, `{EntityName, EntityId}`, `ActorUserId`) · **ห้ามเติม
      `HasQueryFilter`** (มติ OQ-2 — ดู AR-1)
- [ ] [backend] เพิ่ม `public DbSet<AuditLog> AuditLog => Set<AuditLog>();`
- [ ] [backend] แก้ constructor ของ `ApplicationDbContext` เพิ่มพารามิเตอร์ `ICurrentUser currentUser`
      ตาม DM-A4 (`ICurrentUser` ลงทะเบียนเป็น `AddScoped` อยู่แล้วที่ `ServiceConfiguration.cs:26`)

### `SaveChanges` interceptor (สัญญา AU-1 … AU-16 — อ่านให้ครบก่อนเขียน ไม่ใช่อ่านผ่าน)

- [ ] [backend] เขียน private method ร่วม (เรียกจากทั้งสอง overload) ที่สร้างแถว `AuditLog` จาก
      `ChangeTracker.Entries()` ตามลำดับกติกา: AU-2 (คนแรก — `if (string.IsNullOrEmpty(currentUser.UserId)) return;`)
      → AU-3 (`.ToList()` ทันที ก่อนสร้างแถวใดๆ) → AU-4 (ข้าม `entry.Entity is AuditLog`) →
      AU-5 (ข้าม `entry.Metadata.IsOwned()`) → AU-6/AU-7 (map action พร้อมกติกา soft-delete) →
      AU-8 (`IsDelete: true→false` = `update` ไม่ใช่ `restore`) → AU-9 (`DateTime.UtcNow` ครั้งเดียว
      ต่อการเรียกหนึ่งครั้ง ใช้ค่าเดียวกันทุกแถว) → AU-10 (`EntityId` จาก PK string เดี่ยว, throw
      `InvalidOperationException` ภาษาไทยถ้า PK ไม่ใช่ string เดี่ยวหรือว่างเปล่า ห้ามข้ามเงียบ) →
      AU-11 (`CompanyId`: `ICompanyScoped` → `s.CompanyId` · `Company` → `c.Id` · อื่นๆ → `null`
      อ่านจาก CLR object ห้ามอ่านจาก `entry.Property()`) → AU-12 (`EntityName` =
      `entry.Metadata.ClrType.Name` ห้ามพิมพ์ชื่อตารางมือ) → AU-13 (หนึ่งแถวต่อหนึ่ง entity ต่อหนึ่ง
      `SaveChanges` ไม่ใช่ต่อ property)
- [ ] [backend] override `SaveChanges(bool acceptAllChangesOnSuccess)` — เรียก private method,
      `Set<AuditLog>().AddRange(rows)`, แล้ว `base.SaveChanges(acceptAllChangesOnSuccess)` ครั้งเดียว
      (AU-1, AU-14 — ห้ามเรียก `SaveChanges` ซ้อนภายใน)
- [ ] [backend] override `SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken)` —
      เรียก private method เดียวกัน (AU-1 — ต้อง override เวอร์ชันมี `bool` ไม่ใช่เวอร์ชันไม่มี
      พารามิเตอร์ ไม่งั้น call site ที่เรียก overload แบบมี `bool` จะหลุด log เงียบๆ)
- [ ] [backend] ตรวจสอบว่า path การเขียน `AuditLog` **ไม่มี** `try/catch` ที่กลืน exception ที่ไหน
      เลย (AU-15 — ข้อยกเว้นที่เจ้าของโปรเจกต์เคาะแล้ว ขัดกับ convention "degrade แทน throw" ปกติ
      ของ `CLAUDE.md` โดยตั้งใจ: log เขียนไม่ได้ = business write ต้องล้มตามทั้ง transaction)

### Blast radius ของ Module A (ต้องแก้คู่กัน ไม่งั้น build พัง)

- [ ] [backend] แก้ `backend/src/SupportRoom.Providers.Data/Data/DesignTimeDbContextFactory.cs:41`
      — เพิ่ม `new CurrentUser()` เป็นอาร์กิวเมนต์ที่สามของ `new ApplicationDbContext(...)`
- [ ] [backend] แก้ `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs:47` —
      เพิ่มอาร์กิวเมนต์ `_currentUser`/เทียบเท่าให้ตรง ctor ใหม่ ไม่งั้น test project compile ไม่ผ่าน
      ทั้งชุด

### Migration (MG-A1)

- [ ] [backend] เขียน EF Core migration ชื่อ `AddAuditLog` — additive ล้วน (`CREATE TABLE
      "AuditLog"` + 3 index) ไม่แตะตารางเดิม ไม่มี backfill · **สร้างไฟล์ migration ได้ แต่ห้ามรัน
      `dotnet ef database update` จนกว่าจะมีไฟเขียว C1 แยกต่างหาก** (migration นี้จะต่อท้ายคิว
      `20260813140603_SplitLinkAndAddAuth` ที่ยังไม่เคย apply — ดู Sequencing Notes)

### เอกสารที่ต้อง regenerate/แก้คู่กับ migration (Definition of Done ของ `CLAUDE.md`)

- [ ] [backend] เพิ่ม `AuditLog` เข้า `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`
- [ ] [backend] regenerate `backend/docs/DATABASE_SCHEMA_SUMMARY.md`,
      `backend/docs/supportroom-schema.sql`, `backend/docs/supportroom.dbml`,
      `backend/docs/supportroom-migrations-idempotent.sql` ให้มี `AuditLog` (16 → 17 ตาราง)

### Tests (AU-20 — เฉพาะกติกาที่ EF InMemory พิสูจน์ได้จริง)

- [ ] [backend] เทสต์ AU-2: ไม่มี `currentUser.UserId` → `SaveChanges` ไม่สร้างแถว `AuditLog`
      เลยแม้จะมี entity อื่นเปลี่ยนแปลงจริง
- [ ] [backend] เทสต์ AU-4: `SaveChanges` ที่มีการเปลี่ยนแปลง entity ปกติ ต้องไม่สร้างแถว `AuditLog`
      ซ้อนแถว `AuditLog` (กันวนซ้ำไม่รู้จบ)
- [ ] [backend] เทสต์ AU-5: บันทึก `LessonConfig` ที่มี `SlideConfigs` (owned, `ToJson()`) หลายรายการ
      → ต้องได้ **1 แถว** ไม่ใช่ N+1 แถวตามจำนวนสไลด์
- [ ] [backend] เทสต์ AU-6 ครบสามทาง: `Added` → `create`, `Modified` (ไม่ใช่ soft-delete) →
      `update`, `IsDelete: false→true` → `delete`
- [ ] [backend] เทสต์ AU-9: หลาย entity เปลี่ยนแปลงพร้อมกันใน `SaveChanges` เดียว → ทุกแถวที่ได้มี
      `OccurredAt` เท่ากันเป๊ะ

## Phase 2: Module B — เขียน log ด้วยมือที่ 5 จุด raw SQL 🔒 Security gate

**✅ C1 ปลดล็อกแล้ว 2026-08-28 (ดูหัวไฟล์) — ตัวบล็อกที่เหลือของ Phase นี้คือ Phase 1 ต้องเสร็จและ
merge ก่อน (ดู Sequencing Notes) ไม่ใช่ C1 อีกต่อไป**

### Signature change (DM-A5)

- [ ] [backend] แก้ signature `IBackgroundJobRepository.AccelerateLessonPurge` เพิ่มพารามิเตอร์
      `string? actorUserId` ต่อท้าย (`bool AccelerateLessonPurge(string companyId, string lessonId,
      string purgeJobId, string? actorUserId)`)

### 5 จุด raw-SQL (สัญญา RS-0 … RS-8)

- [ ] [backend] RS-1: `LessonConfigRepository.TryArchive` (`ILessonConfigRepository.cs:78-87`) —
      หลัง `archived == 1` และก่อน `transaction.Commit()` เขียน **1 แถว**: `delete` / `LessonConfig`
      / `lessonId` / `companyId` / `actorUserId` · **ห้าม**เขียนแถวซ้ำสำหรับ `BackgroundJob` ที่
      `Add` ต่อจากนั้น (บรรทัด 94-106) — เส้นนั้นผ่าน `Context.SaveChanges()` ปกติ interceptor
      ของ Phase 1 จับให้แล้ว
- [ ] [backend] RS-2: cascade ปิด `TrainingLink` ใน `TryArchive` (`ILessonConfigRepository.cs:108-115`)
      — `SELECT` id ก่อนด้วย `Where` เงื่อนไขเดียวกันเป๊ะ (`CompanyId == companyId && LessonId ==
      lessonId && !IsDelete`) แล้วค่อย `ExecuteUpdate` แล้วเขียน **1 แถวต่อ id ที่ปิดจริง**
      (`delete`/`TrainingLink`/id) · ห้ามเขียนแถวรวม · ไม่มีลิงก์เลย → 0 แถว
- [ ] [backend] RS-3: `LessonConfigRepository.TryRestore` (`ILessonConfigRepository.cs:133-144`) —
      หลัง `rows == 1` เขียน **1 แถว**: `update`/`LessonConfig`/`lessonId` (ตาม AU-8) · เปิด
      transaction ใหม่ (วันนี้เป็น `ExecuteSqlRaw` เดี่ยวไม่มี transaction) — เมธอดนี้ไม่มี caller
      ใน production วันนี้แต่ยังต้องทำให้ครบตามสัญญา RS-3
- [ ] [backend] RS-4: `LessonConfigRepository.TryRestoreAndCancelPurge`
      (`ILessonConfigRepository.cs:146-189`) — หลัง `canceled == 1` และก่อน `transaction.Commit()`
      เขียน **2 แถว**: (`update`/`LessonConfig`/`lessonId`) และ (`update`/`BackgroundJob`/
      `purgeJobId`) · เส้นทาง rollback ทั้งสองเส้น (บรรทัด 161, 183) → 0 แถว
- [ ] [backend] RS-5: `BackgroundJobRepository.AccelerateLessonPurge`
      (`IBackgroundJobRepository.cs:100-110`) — เปิด `BeginTransaction` ครอบ (วันนี้เป็น
      `ExecuteSqlRaw` เดี่ยว) แล้วหลัง `rows == 1` เขียน **1 แถว**: `update`/`BackgroundJob`/
      `purgeJobId`/`companyId`/`actorUserId` · **ห้าม**เขียนแถวของ `LessonConfig` ที่นี่ — คำสั่งนี้
      แตะแค่แถว job

### Call site + fake (blast radius ของ Module B)

- [ ] [backend] แก้ call site ที่ `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs:502`
      ให้ส่ง `CurrentUserId` เป็นอาร์กิวเมนต์ `actorUserId` ตาม signature ใหม่ของ
      `AccelerateLessonPurge`
- [ ] [backend] แก้ `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` —
      fake ของ `IBackgroundJobRepository` ให้รับ signature ใหม่ของ `AccelerateLessonPurge` (DM-A5)
      และตรวจว่า fake ของ `ILessonConfigRepository` (`TryRestore` ที่บรรทัด 118) ยัง compile ผ่าน

### กติกาถาวรที่ต้องบันทึกลงเอกสารโปรเจกต์ (RS-8, ดู AR-4)

- [ ] [backend] เพิ่มบรรทัดใหม่ใน `CLAUDE.md` §"Architecture Rules": ทุกครั้งที่มีใครเพิ่ม
      `ExecuteUpdate`/`ExecuteSqlRaw`/`ExecuteDelete` ใหม่ที่ไหนก็ตามในโปรเจกต์ ต้องเขียน
      `AuditLog` ในธุรกรรมเดียวกันด้วย หรือเขียนคอมเมนต์ระบุเหตุผลว่าทำไมไม่ต้อง (worker เท่านั้น/
      ไม่มีคนลงมือ) — ถ้าข้อนี้ไม่ถูกเขียนลง `CLAUDE.md` จริง ความรู้จะตายไปพร้อมโมดูลนี้ (AR-4)

**ไม่มีงานสำหรับ RS-6 และ RS-7** — RS-6 (`TryClaimPurge`/`ClaimNext`/`RequeueOrphanedRunning`)
เป็นจุดที่ **จงใจไม่เขียน log** (worker ก่อเอง ตามมติ OQ-3) ห้ามเผลอเติมโค้ด · RS-7
(`CancelPendingLessonPurge`) **ยังไม่กำหนดกติกาในรอบนี้** เพราะไม่มี caller ใน production และ
ความหมายกำกวมกว่า RS-3 — ถ้าวันหนึ่งมี caller ต้องกลับไปหา `system-analyst` ก่อน ห้าม engineer
ตัดสินเอง

## Sequencing Notes

- **Phase 1 ต้องเสร็จและถูก merge ก่อน Phase 2 เริ่มได้** — ทั้งตาราง `AuditLog` และกติกา AU-4
  (กันแถวที่ Phase 2 เขียนเองไม่ถูกนับซ้ำโดย interceptor) ต้องมีอยู่ก่อน 5 จุด raw-SQL ของ Phase 2
  จะเขียนแถวได้ถูกต้อง — ระบุไว้ใน `design.md` §Module B ("ขึ้นกับ: Module A ต้องเสร็จก่อน")
  ไม่ใช่การจัดลำดับที่ `project-manager` ตัดสินเอง
- **🔒 Security gate — Phase 1**: (1) เก็บ `ActorUserId` ซึ่งเป็น personal data ที่ระบุตัวคนได้
  ในตารางที่ไม่มีวันถูกลบ (R4) (2) สร้างตารางที่ไม่มี company query filter ในระบบ multi-tenant
  ที่ทุกตารางอื่นมี filter — พื้นผิวรั่วข้ามบริษัททันทีที่มีทางอ่านเกิดขึ้น (AR-1) (3) แตะ
  `SaveChanges` ของทั้งระบบ — จุดที่ทุกการเขียนข้อมูลของทุกโมดูลไหลผ่าน `security` ต้องตรวจเป็น
  พิเศษว่า AU-15 (ห้ามกลืน exception) ไม่ได้กลายเป็นช่องทำให้ระบบล่มทั้งระบบ
- **🔒 Security gate — Phase 2**: เป็นเส้นทาง archive/restore/permanent-delete ของบทเรียน —
  การกระทำที่ทำลายข้อมูลได้จริงและเป็นสิ่งที่ "อยากรู้ว่าใครทำ" มากที่สุด ถ้าจุดใดใน 5 จุดหลุด
  ช่องว่างจะเงียบสนิท (ไม่มี error เตือน) ตรงกับการกระทำที่ผู้ไม่หวังดีจะเลือกใช้พอดี · `security`
  ต้องตรวจครบทั้ง 5 จุดทีละจุดเทียบกับ RS-1..RS-5 ไม่ใช่ตรวจตัวอย่าง
- **AU-15 ทดสอบด้วย EF InMemory ไม่ได้** (ไม่มี transaction จริง) — `qa-engineer` ต้องบันทึกเป็น
  `## Unverified Behaviour` แทนการแกล้งว่าทดสอบแล้ว (AU-20, AR-9) และ `devops` ต้องเห็นรายการนี้
  ก่อน deploy
- **migration `AddAuditLog` ต่อท้ายคิวที่ยังไม่เคย apply** (`20260813140603_SplitLinkAndAddAuth`,
  ดู `CLAUDE.md` §Known Baseline Issues และ AR-8) — เมื่อไฟเขียว C1 มาแล้ว `devops` ต้อง rehearsal
  ทั้งคิวบน staging ไม่ใช่แค่ migration ใหม่ตัวเดียว
- **AR-5 (ปริมาณ log จริง)**: R4 (ไม่มี retention) เป็นมติที่นิ่งแล้ว แต่ตั้งอยู่บนสมมติฐานปริมาณต่ำ
  ที่ยังไม่เคยวัด — ข้อเสนอใน `design.md`: วัดจำนวนแถวจริงหลัง Phase 1 ขึ้น production ~1 เดือน
  แล้วค่อยตัดสินใจเรื่อง retention ด้วยตัวเลขจริง ไม่ใช่ task ของรอบนี้ บันทึกไว้เผื่ออนาคต

## Unresolved Open Questions

ไม่มีข้อที่บล็อกการ implement เหลืออยู่ในเอกสารแล้ว — `design.md` ปิดครบทุกข้อ (OQ-A1 ปิด
2026-08-28) ตัวบล็อกเดียวที่เหลือคือไฟเขียว C1 ซึ่งเป็นการอนุมัติ ไม่ใช่คำถามที่ต้องเคาะเพิ่ม

ข้อที่เลื่อนโดยตั้งใจ (ไม่บล็อก ไม่ต้องทำอะไรตอนนี้): OQ-A2 (action `restore` แยกจาก `update`
ไหม), OQ-A3 (`ActorRole` snapshot), OQ-A4 (ทบทวน retention เมื่อวัดปริมาณจริงแล้ว), RS-7
(`CancelPendingLessonPurge` — รอจนกว่าจะมี caller จริง)

## Change Log

- **2026-08-28** — สร้าง `plan.md` ครั้งแรก · Phase 1 = Module A (ตาราง `AuditLog` +
  `SaveChanges` interceptor), Phase 2 = Module B (5 จุด raw-SQL) ทั้งคู่ `🔒 Security gate` ·
  Phase 2 ขึ้นกับ Phase 1 (ระบุไว้ใน Sequencing Notes) · **ทุก phase ถูกบล็อกด้วยเงื่อนไข C1
  (ไฟเขียว migration จากเจ้าของโปรเจกต์) แยกต่างหากจาก security gate ปกติ** — ประกาศไว้ที่หัวไฟล์
  และซ้ำที่หัวแต่ละ phase ตามที่ `design.md` สั่ง · ไม่มีงาน `[frontend]` เพราะ R6 ตัด UI/endpoint
  ออกทั้งหมด · เพิ่มงานเทสต์ตาม AU-20 เฉพาะกติกาที่ EF InMemory พิสูจน์ได้จริง (AU-2/AU-4/AU-5/
  AU-6/AU-9) — AU-15 ทดสอบด้วย InMemory ไม่ได้ บันทึกไว้ใน Sequencing Notes ให้ `qa-engineer` ขึ้น
  `Unverified Behaviour`
- **2026-08-28** — **C1 ปลดล็อกแล้ว** เจ้าของโปรเจกต์ยืนยันไฟเขียว migration ของโมดูลนี้เองในแชท
  ("ให้ไฟเขียวตอนนี้เลย" ตอบคำถามตรงๆ ว่าจะให้ไฟเขียว `audit-trail` หรือยัง) แก้แบนเนอร์หัวไฟล์และ
  หัวทั้งสอง phase จาก ⛔ เป็น ✅ · `backend-engineer` dispatch ได้แล้วทั้งสอง phase (Phase 2 ยังรอ
  Phase 1 merge ก่อนตามลำดับเดิม ไม่เกี่ยวกับ C1) · `🔒 Security gate` ไม่ถูกปลดไปด้วย ยังต้องผ่าน
  `security` ก่อน `devops` ship
