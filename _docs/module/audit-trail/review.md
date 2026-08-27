# บันทึกประวัติการกระทำในระบบ (audit-trail) — Verification & Review

## Open Issues — all phases

ไม่มีรายการที่ต้องส่งกลับ `backend-engineer`/`system-analyst`/`business-analyst` — ทุก task ของทั้ง
Phase 1 และ Phase 2 ผ่านการตรวจแล้ว (✅ Verified ทั้งหมด) อย่างไรก็ตาม **ทั้งสอง phase ยังเปิด
`🔒 Security gate` และยังไม่ผ่าน `security` audit** — นี่ไม่ใช่ประเด็นที่ต้องแก้โค้ด แต่เป็นเงื่อนไข
ที่ `devops` gate อยู่ ต้องคงอยู่ในตารางนี้จนกว่า `security` จะรันรอบนั้น

| Issue | Phase | Routed to | Blocking | Re-check rounds |
|---|---|---|---|---|
| `🔒 Security gate` ยังไม่ผ่าน `security` audit (ระบุความกังวล 3 ข้อใน `design.md` §Module A: personal data ที่ไม่มีวันถูกลบ, ไม่มี company query filter, แตะ `SaveChanges` ทั้งระบบ) | Phase 1 (Module A) | `security` | ใช่ — บล็อก `devops` deploy | 0 |
| `🔒 Security gate` ยังไม่ผ่าน `security` audit (เส้นทาง archive/restore/permanent-delete ที่ทำลายข้อมูลได้จริง — ต้องตรวจครบ RS-1..RS-5 ทีละจุด) | Phase 2 (Module B) | `security` | ใช่ — บล็อก `devops` deploy | 0 |

## Verification Summary (current round)

**รอบนี้: FULL — รอบ QA แรกของโมดูลนี้** ตรวจทุก task จากศูนย์ทั้ง Phase 1 (Module A) และ
Phase 2 (Module B) เทียบกับ `design.md` (Feature-by-Feature Feasibility, Data Model DM-A1..DM-A5,
สัญญา AU-1..AU-20, RS-0..RS-8, Risks & Dependencies, Unresolved Open Questions — อ่านครบทุกส่วน)
และ `plan.md` (ทั้งไฟล์ รวม Sequencing Notes) ไม่ใช่แค่เชื่อรายงานของ `backend-engineer`

**ภาพรวม: ทุก task ทั้ง 28 รายการ (19 ของ Phase 1 + 9 ของ Phase 2) ✅ Verified** — ไม่มี Partial
ไม่มี Failed

**Automated checks ที่รันจริง (ผลจริง ไม่ใช่คำบอกเล่า):**

- `dotnet build SupportRoom.slnx` → **Build succeeded, 0 Warning(s), 0 Error(s)**
- `dotnet test SupportRoom.slnx --filter "Category!=Integration"` →
  **358/358 ผ่าน, 0 ล้มเหลว** (41 `SupportRoom.Providers.Tests` + 10
  `SupportRoom.Api.IntegrationTests` (non-Integration traits ในโปรเจกต์นี้) + 307
  `SupportRoom.Application.Tests`) — ยืนยันตัวเลข 358/358 ที่ engineer รายงานเองด้วยการรันจริง
  ไม่ใช่เชื่อคำบอกเล่า
- `dotnet ef migrations has-pending-model-changes` → **"No changes have been made to the model
  since the last migration."** — schema กับ migration ตรงกัน ไม่มี drift
- `dotnet ef migrations list` → ยืนยันว่า `20260827181826_AddAuditLog` ขึ้นสถานะ **`(Pending)`**
  ต่างจาก migration อื่นทั้งหมดที่ก่อนหน้า — **ยังไม่เคยรัน `dotnet ef database update` กับ
  migration นี้** ตรงตามเงื่อนไขที่ต้องรอ staging rehearsal ผ่าน `devops` ก่อน (แม้ C1 จะปลดล็อกแล้ว
  ก็ตาม — C1 คือไฟเขียวให้ backend-engineer เริ่มงานและสร้างไฟล์ migration ได้ ไม่ใช่คำสั่งให้รัน
  update เอง)
- ไม่มี `test` script ล้วนๆ นอกเหนือจาก xUnit — โปรเจกต์นี้มี test suite จริง (xUnit) ไม่ใช่
  "ไม่มี automated tests" แต่มีช่องว่างเฉพาะจุด (ดู `## Unverified Behaviour` ด้านล่าง) เพราะ EF
  InMemory พิสูจน์ transaction/rollback จริงไม่ได้

**ข้อสังเกตเล็กน้อยที่ไม่กระทบผลการตรวจ:** คำบรรยายงานที่ dispatch มาระบุว่ามี "8 tests ใหม่"
แต่ไฟล์ `AuditLogInterceptorTests.cs` มี **7 `[Fact]` จริง** (AU-2 ×1, AU-4 ×1, AU-5 ×1, AU-6 ×3,
AU-9 ×1) — ครอบคลุมครบทุกข้อที่ `plan.md`/AU-20 ต้องการ (5 บรรทัด task แปลงเป็น 7 test method เพราะ
AU-6 ต้องการ 3 เคส) ไม่ใช่ความบกพร่อง แค่ตัวเลขที่บอกต่อกันมาไม่ตรง

## Verified File Manifest — Phase 1 (Module A) + Phase 2 (Module B)

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `backend/src/SupportRoom.Domain/Entities/AuditLog.cs` | 5498 | 58 | FULL #1 |
| `backend/src/SupportRoom.Domain/Enums/AuditAction.cs` | 782 | 13 | FULL #1 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 22960 | 386 | FULL #1 |
| `backend/src/SupportRoom.Providers.Data/Data/DesignTimeDbContextFactory.cs` | 2791 | 55 | FULL #1 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 13900 | 291 | FULL #1 |
| `backend/src/SupportRoom.Providers.Data/Repository/IBackgroundJobRepository.cs` | 6823 | 143 | FULL #1 |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 50543 | 964 | FULL #1 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260827181826_AddAuditLog.cs` | 2049 | 54 | FULL #1 |
| `backend/tests/SupportRoom.Application.Tests/AuditLogInterceptorTests.cs` | 7196 | 169 | FULL #1 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 20746 | 458 | FULL #1 |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | 38833 | 751 | FULL #1 |
| `backend/tests/SupportRoom.Api.IntegrationTests/ModuleLRepositoryIsolationTests.cs` | 11236 | 199 | FULL #1 |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | 20580 | 463 | FULL #1 |
| `backend/docs/DATABASE_SCHEMA_SUMMARY.md` | 15460 | 377 | FULL #1 |
| `backend/docs/supportroom-schema.sql` | 13957 | 453 | FULL #1 |
| `backend/docs/supportroom.dbml` | 11451 | 438 | FULL #1 |
| `backend/docs/supportroom-migrations-idempotent.sql` | 55001 | 1591 | FULL #1 |
| `CLAUDE.md` | 39795 | 398 | FULL #1 |

## Per-Task Results — Phase 1 (Module A) (this round)

- ✅ [backend] `AuditLog.cs` ตาม DM-A1 — ตรวจโค้ดจริงแล้วมีครบ 7 field เป๊ะ (`Id`, `CompanyId`
  nullable, `ActorUserId`/`Action`/`EntityName`/`EntityId` required, `OccurredAt`) ไม่ implement
  `IEntityMaster`/`ICompanyScoped` ไม่มี `BeforeJson`/`AfterJson`/`MetadataJson`/`IpAddress`/
  `UserAgent`/`ActorRole`/`CorrelationId` แม้ field เดียว
- ✅ [backend] `AuditAction.cs` ตาม DM-A2 — `static class` + `const string` (`Create`/`Update`/
  `Delete`) ตาม convention ไม่ใช้ C# enum
- ✅ [backend] `OnModelCreating` เพิ่ม `builder.Entity<AuditLog>()` ตาม DM-A3 — `HasKey`, 3 index
  (`{CompanyId, OccurredAt}`, `{EntityName, EntityId}`, `ActorUserId`) ตรวจแล้วว่า**ไม่มี**
  `HasQueryFilter` (มติ OQ-2) ยืนยันจากทั้งโค้ดและ migration file
- ✅ [backend] `DbSet<AuditLog> AuditLog => Set<AuditLog>();` มีจริง
- ✅ [backend] ctor ของ `ApplicationDbContext` เพิ่ม `ICurrentUser currentUser` ตาม DM-A4 — ตรวจแล้ว
  ว่า `ICurrentUser` ลงทะเบียน `AddScoped` อยู่แล้ว (ไม่ต้องแก้ DI เพิ่ม)
- ✅ [backend] private method `BuildAuditLogRows()` — ไล่ตรวจทีละกติกาเทียบโค้ดจริงบรรทัดต่อบรรทัด:
  AU-2 (บรรทัดแรก return ถ้าไม่มี actor) → AU-3 (`.ToList()` ก่อนสร้างแถว) → AU-4 (ข้าม
  `entry.Entity is AuditLog`) → AU-5 (ข้าม `entry.Metadata.IsOwned()`) → AU-6/AU-7/AU-8 (map
  action ผ่าน `IsSoftDelete()` — `false→true` = delete, `true→false` = update ตาม default) →
  AU-9 (`DateTime.UtcNow` ครั้งเดียวนอก loop) → AU-10 (`ResolveEntityId` throw
  `InvalidOperationException` ภาษาไทยถ้า PK ไม่ใช่ string เดี่ยวหรือว่างเปล่า) → AU-11
  (`ResolveCompanyId`: `ICompanyScoped`→`s.CompanyId`, `Company`→`c.Id`, อื่นๆ→`null`, อ่านจาก
  `entry.Entity` ไม่ใช่ `entry.Property()`) → AU-12 (`entry.Metadata.ClrType.Name`) → AU-13 (หนึ่ง
  entry = หนึ่งแถว) — ตรงตามสัญญาทุกข้อ
- ✅ [backend] `SaveChanges(bool)` override — เรียก `BuildAuditLogRows()`, `AddRange`, แล้ว
  `base.SaveChanges(acceptAllChangesOnSuccess)` ครั้งเดียว ไม่มี `SaveChanges` ซ้อนภายใน (AU-14)
- ✅ [backend] `SaveChangesAsync(bool, CancellationToken)` override — เรียก private method เดียวกัน
  ไม่มี logic ซ้ำสองชุด
- ✅ [backend] ไม่มี `try/catch` กลืน exception ในเส้นทางเขียน `AuditLog` เลย (AU-15) — ตรวจทั้ง
  `ApplicationDbContext.cs`, `ILessonConfigRepository.cs`, `IBackgroundJobRepository.cs` ไม่พบ
  `try/catch` รอบการเขียน audit log แม้จุดเดียว
- ✅ [backend] `DesignTimeDbContextFactory.cs:41` — ส่ง `new CurrentUser()` เป็นอาร์กิวเมนต์ที่สาม
  ถูกต้อง, `dotnet ef` ทุกคำสั่งรันผ่านจริง (ยืนยันด้วยการรัน `migrations list`/
  `has-pending-model-changes` จริง)
- ✅ [backend] `CompanyIsolationTests.cs:47-51` — แก้ ctor ให้ตรง มี comment อธิบายว่าทำไม
  `_currentUser` ไม่ resolve ในเทสต์ isolation (กัน `AuditLog` แทรกปนกับ assertion) — compile ผ่าน
  และรันผ่านจริง (ยืนยันจาก `dotnet test`)
- ✅ [backend] migration `AddAuditLog` — additive ล้วน (`CREATE TABLE` + 3 `CreateIndex`) ไม่แตะ
  ตารางเดิม ไม่มี backfill ยืนยันด้วย `dotnet ef migrations has-pending-model-changes` ว่า schema
  ตรงกับ model และ `dotnet ef migrations list` ว่ายังเป็น `(Pending)` — **ยังไม่เคยรัน
  `database update`** ตรงตามเงื่อนไข
- ✅ [backend] `ER_DIAGRAM_AND_WORKFLOW.md` — เพิ่ม `AuditLog` เข้า ER จริง พร้อมอธิบายเหตุผลที่ไม่มี
  query filter สอดคล้องกับ `design.md`
- ✅ [backend] เอกสาร schema อีก 4 ไฟล์ (`DATABASE_SCHEMA_SUMMARY.md`,
  `supportroom-schema.sql`, `supportroom.dbml`, `supportroom-migrations-idempotent.sql`) —
  regenerate จริง สรุป 17 business tables, `AuditLog` เอกสารครบ 7 columns ตรงกับ entity เป๊ะ
- ✅ [backend] เทสต์ AU-2 — `AU2_NoActorResolved_SaveChangesCreatesNoAuditLogRowEvenThoughAnEntityChanged`
  มีจริง ยืนยันว่าไม่มี actor → 0 แถว แม้มี entity อื่นเปลี่ยนแปลงจริง — ผ่านการรัน
- ✅ [backend] เทสต์ AU-4 — `AU4_NormalSaveChangesWithAnActorDoesNotCreateDuplicateAuditLogRows` —
  ยืนยัน 1 แถวต่อ 1 entity ไม่วนซ้ำ — ผ่านการรัน
- ✅ [backend] เทสต์ AU-5 — `AU5_SavingALessonWithSeveralOwnedSlideConfigsProducesOneAuditLogRowNotNPlusOne`
  — `LessonConfig` ที่มี `SlideConfigs` 5 รายการ ได้ 1 แถว ไม่ใช่ N+1 — ผ่านการรัน
- ✅ [backend] เทสต์ AU-6 ครบสามทาง — `AU6_AddedEntityIsRecordedAsCreate`,
  `AU6_ModifyingAFieldThatIsNotIsDeleteIsRecordedAsUpdate`,
  `AU6_SoftDeletingATrackedRowIsRecordedAsDeleteNotUpdate` — ทั้งสามผ่านการรัน
- ✅ [backend] เทสต์ AU-9 — `AU9_MultipleEntitiesChangedInOneSaveChangesShareTheExactSameOccurredAt`
  — 2 entity ใน `SaveChanges` เดียว ได้ `OccurredAt` เท่ากันเป๊ะ — ผ่านการรัน

## Per-Task Results — Phase 2 (Module B) (this round)

- ✅ [backend] signature `AccelerateLessonPurge` เพิ่ม `string? actorUserId` — ตรวจตรงกับ DM-A5,
  compiler จับ call site ได้ครบ (build 0 error)
- ✅ [backend] RS-1 `TryArchive` — ตรวจทีละบรรทัด: `ExecuteUpdate` ก่อน, `archived != 1` →
  rollback ก่อนเขียนอะไร, `archived == 1` → เขียน 1 แถว `delete`/`LessonConfig`/`lessonId` เฉพาะเมื่อ
  `actorUserId` ไม่ว่าง, **ไม่เขียนแถวซ้ำสำหรับ `BackgroundJob`** (ปล่อยให้ interceptor ของ Phase 1
  จับผ่าน `Context.SaveChanges()` ปกติ — ตรวจแล้วว่า `AuditLog` entry ที่เพิ่มเองไม่ถูกนับซ้ำเพราะ
  AU-4 skip `entry.Entity is AuditLog`)
- ✅ [backend] RS-2 cascade `TrainingLink` — SELECT id ก่อนด้วย `Where` เงื่อนไขเดียวกันเป๊ะกับ
  `ExecuteUpdate` ที่ตามมา (`CompanyId == companyId && LessonId == lessonId && !IsDelete`), เขียน
  1 แถวต่อ id ที่ปิดจริง (ไม่ใช่แถวรวม), ไม่มีลิงก์เลย → `revokedLinkIds.Count == 0` → ไม่เขียนอะไร
  (0 แถว ถูกต้อง)
- ✅ [backend] RS-3 `TryRestore` — เปิด `Context.Database.BeginTransaction()` ใหม่ (เดิมไม่มี
  ยืนยันจากโค้ด `ExecuteSqlRaw` เดี่ยว), หลัง `rows == 1` เขียน 1 แถว `update`/`LessonConfig`/
  `lessonId` ตาม AU-8 (ไม่ใช่ action `restore` ใหม่) — เมธอดนี้ไม่มี caller ใน production วันนี้
  (ยืนยันจาก `RestoreAsync` เรียก `TryRestoreAndCancelPurge` แทน) แต่โค้ดยังทำครบตามสัญญา
- ✅ [backend] RS-4 `TryRestoreAndCancelPurge` — 2 แถวเขียนหลัง `canceled == 1` และก่อน
  `transaction.Commit()` เท่านั้น (`update`/`LessonConfig`/`lessonId` และ `update`/`BackgroundJob`/
  `purgeJobId`) — ตรวจ rollback path ทั้งสองเส้น (`restored != 1`, `canceled != 1`) ว่าไม่มีการเขียน
  `AuditLog` ก่อน rollback จริง (0 แถว)
- ✅ [backend] RS-5 `AccelerateLessonPurge` — เปิด `BeginTransaction` ครอบ (เดิมเป็น `ExecuteSqlRaw`
  เดี่ยว), หลัง `rows == 1` เขียน 1 แถว `update`/`BackgroundJob`/`purgeJobId` เท่านั้น **ไม่เขียน
  แถวของ `LessonConfig`** ตรงตามข้อห้าม, `actorUserId` เป็น `null` → ไม่เขียนแถว (มติ OQ-3)
- ✅ [backend] RS-6/RS-7 ไม่ถูกแตะ — grep ยืนยันว่า `TryClaimPurge`, `ClaimNext`,
  `RequeueOrphanedRunning`, `CancelPendingLessonPurge` ไม่มีโค้ดเขียน `AuditLog` เพิ่มเลยแม้บรรทัด
  เดียว ตรงตามที่สัญญาสั่งห้าม
- ✅ [backend] call site `ILessonConfigService.cs:502` — `jobRepository.AccelerateLessonPurge(CurrentCompanyId, id, lesson.PurgeJobId, CurrentUserId)` ส่ง `CurrentUserId` ถูกต้องตาม signature ใหม่
- ✅ [backend] `ServiceTestFakes.cs` — fake ของ `IBackgroundJobRepository.AccelerateLessonPurge`
  รับ 4 พารามิเตอร์ตรงกับ signature ใหม่, fake ของ `ILessonConfigRepository.TryRestore` ยัง compile
  ผ่าน (ยืนยันจาก build 0 error)
- ✅ [backend] `CLAUDE.md` §Architecture Rules ข้อ 9 — มีบรรทัด RS-8 จริง อ้างอิงรูปแบบที่
  `TryArchive`/`AccelerateLessonPurge` ตรงตามที่สัญญาสั่ง

**เพิ่มเติมนอกเหนือ task list ที่ตรวจตามคำสั่ง dispatch — blast radius ที่ backend-engineer เจอเอง:**

- ✅ `ModuleLRepositoryIsolationTests.cs` (`Category=Integration`) — แก้ ctor ของ
  `ApplicationDbContext` (`new(..., companyContext, new CurrentUser())`) และแก้ call site
  `AccelerateLessonPurge` ให้ตรง signature ใหม่ — compile ผ่านและรันผ่านจริง (10/10 ในชุด
  `SupportRoom.Api.IntegrationTests` ที่ไม่ติด `Category=Integration` filter — ตัวเทสต์นี้เองติด
  `[Trait("Category", "Integration")]` จึงไม่ได้รันในรอบ `--filter "Category!=Integration"`
  ที่ CLAUDE.md ระบุเป็นค่าเริ่มต้น แต่ code inspection ยืนยันว่า compile ถูกต้องและตรรกะตรงตาม
  Company isolation guard เดิม)

## Design/requirement contract checks — Phase 1 + Phase 2

**Data Model (design.md DM-A1..DM-A5) เทียบ `schema.prisma`-equivalent ของโปรเจกต์นี้ (EF Core
`ApplicationDbContext`/migration) แบบ field-by-field:**

- `AuditLog` — โมเดลเดียวที่โมดูลนี้เป็นเจ้าของ: 7 field ตรงเป๊ะกับ DM-A1 ทั้งชื่อและ nullability
  (`CompanyId string?`, ที่เหลือ required) ไม่มี field เกิน ไม่มี field ขาด — **ตรงตามสัญญา 100%**
- `AuditAction` (DM-A2) — constants ตรงเป๊ะ (`create`/`update`/`delete`)
- `ApplicationDbContext` ctor (DM-A4) และ index 3 ตัว (DM-A3) — ตรงเป๊ะ
- `IBackgroundJobRepository.AccelerateLessonPurge` signature (DM-A5) — ตรงเป๊ะ

โมดูลนี้ไม่มี model อื่นให้เทียบ (R6 ตัด repository/service/DTO/ViewModel ออกทั้งหมด, ยืนยันด้วย
grep `AuditLog` ทั่ว `backend/src` — เจอแค่ 7 ไฟล์ที่คาดไว้พอดี: entity, migration ×2, DbContext,
สอง repository ที่เขียน raw-SQL log) **ไม่มี model ใดใน `schema.prisma`-equivalent ที่ไม่ได้ประกาศไว้
ใน `design.md` ของโมดูลนี้** — ไม่มีการ improvise schema เพิ่ม

**Business-rule checks เทียบ `requirement.md`:**

- R1/R1.2 (เฉพาะ admin-side, ไม่รวมผู้เรียน) — ยืนยันด้วยการอ่าน `CurrentUserMiddleware.cs` ว่า
  anonymous request (ฝั่งผู้เรียนทั้งหมด) ไม่ resolve `ICurrentUser` เลย ทำให้ AU-2 ตัดทิ้งได้จริง
  โดยไม่ต้อง blocklist ตาราง (AU-18) — ตรงตามเจตนา
- R1.1 (soft-delete = delete) — ยืนยันจาก `IsSoftDelete()` และเทสต์ AU-6 เคสที่สาม
- R2 (ไม่มี diff, ไม่มี `MetadataJson`) — ยืนยันจากโครงสร้าง entity 7 field
- R3/R3.1/OQ-2/OQ-A1 (`CompanyId` nullable, ไม่มี query filter, `AdminUser`→null เสมอ) — ยืนยันจาก
  `ResolveCompanyId()` ตรงลำดับ AU-11 เป๊ะ และ `OnModelCreating` ไม่มี `HasQueryFilter` บน `AuditLog`
- R4 (ไม่มี retention) — ไม่มีโค้ด retention job ใดๆ ถูกเพิ่ม ตรงตามขอบเขต
- R5 (ขยาย action ได้) — `AuditAction` เป็น string constants ไม่ใช่ enum ตรงตาม convention
- R6 (ไม่มี UI/endpoint) — ยืนยันด้วย grep ไม่พบ repository/service/controller/DTO ของ `AuditLog`
- OQ-3 (ไม่บันทึก action ของระบบ) — AU-2 ครอบคลุมเพราะ background worker ไม่ resolve `ICurrentUser`

ไม่พบ drift ระหว่าง `design.md` กับโค้ดจริงแม้จุดเดียวในรอบนี้

## Unverified Behaviour — undeployed phases

โปรเจกต์นี้**มี** automated test suite จริง (xUnit) แต่มีช่องว่างเฉพาะจุดที่ EF Core InMemory
พิสูจน์ไม่ได้ — ตามที่ `design.md` (AU-20, AR-9) และ `plan.md` (Sequencing Notes) ระบุไว้ล่วงหน้าแล้ว
ว่าต้องบันทึกเป็น unverified behaviour แทนการแกล้งว่าทดสอบแล้ว

### Phase 1 (Module A)

- **AU-15 (atomic rollback: log เขียนไม่ได้ → business write ต้องล้มตามทั้ง transaction, มติ Q-A3)**
  — ตรวจได้แค่ว่า**ไม่มี** `try/catch` ที่จะกลืน exception (code inspection ยืนยันแล้ว) แต่**ไม่มีการ
  ทดสอบจริงว่า exception ระหว่างเขียน `AuditLog` (เช่น constraint violation, connection drop)
  ทำให้ business write ทั้งก้อน rollback จริง** — EF Core InMemory ไม่มี transaction จริงให้ทดสอบ
  พฤติกรรมนี้ ไฟล์: `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs`
  (`SaveChanges`/`SaveChangesAsync` override)

### Phase 2 (Module B)

- **RS-1..RS-5 (5 จุด raw-SQL: การเขียน `AuditLog` เกิดขึ้นจริงในธุรกรรมเดียวกันกับคำสั่ง SQL, และ
  rollback path ไม่ทิ้งแถว log ค้าง)** — `ModuleLRepositoryIsolationTests.cs`
  (`Category=Integration`, รันจริงกับ PostgreSQL) ยืนยัน company-isolation guard ของทั้ง 5 เมธอด
  ถูกต้อง (ทุกเคสที่ควร reject ก็ reject จริง) **แต่ไม่มี assertion ใดเลยที่ตรวจว่าเมื่อคำสั่งสำเร็จ
  จริง (`rows == 1`) แถว `AuditLog` ถูกเขียนลงจริงในธุรกรรมเดียวกัน** — เพราะ fixture ของเทสต์นี้
  ออกแบบมาให้ทุกเรียกจาก company ผิดล้มเหลวทั้งหมด (0 แถวสำเร็จ) จึงไม่มีเคสที่ผ่านให้ตรวจ
  ไฟล์: `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` (`TryArchive`,
  cascade `TrainingLink`, `TryRestore`, `TryRestoreAndCancelPurge`),
  `backend/src/SupportRoom.Providers.Data/Repository/IBackgroundJobRepository.cs`
  (`AccelerateLessonPurge`) — ยืนยันความถูกต้องได้เฉพาะด้วยการอ่านโค้ด (ทำไปแล้วในรอบนี้ ทีละบรรทัด)
  ไม่ใช่ด้วยการรันจริง

ทั้งสอง block นี้จะคงอยู่ในไฟล์นี้จนกว่า phase ที่เกี่ยวข้องจะ `deployed ✅` ใน `_docs/status.md`
ตามธรรมเนียมโปรเจกต์ — `devops` ต้องเห็นรายการนี้ก่อน deploy จริง

## Issues Found — Phase 1 + Phase 2

ไม่มี — ทุก task ✅ Verified ไม่มีรายการที่ต้องส่งกลับ `frontend-engineer`/`backend-engineer`/
`system-analyst`/`business-analyst` ในรอบนี้

## Review Outcome — Phase 1 (Module A) + Phase 2 (Module B)

**Accepted** — รอบ FULL แรกของโมดูลนี้ ทุก task ทั้ง 28 รายการผ่านการตรวจจริงเทียบกับ `design.md`/
`requirement.md` ครบทุกข้อ ไม่มี ⚠️ Partial หรือ ❌ Failed แม้รายการเดียว build/test ผ่านทั้งหมด
schema ตรงกับ `design.md` 100% — ยอมรับตามเงื่อนไข autonomous-round exception ของ `qa-engineer.md`
(FULL round + ทุก task ✅ Verified) โดยไม่ต้องหยุดรอ AskUserQuestion

**ทั้งสอง phase มีสิทธิ์ deploy ได้เมื่อผ่าน `security` แล้วเท่านั้น** — รอบนี้เป็น FULL round ตาม
เงื่อนไข "Deploy eligibility requires a FULL round" แต่**ยังบล็อกอยู่ที่ `🔒 Security gate`** ที่
`design.md` ระบุไว้ทั้งสอง Module (Module A: personal data ที่ไม่มีวันถูกลบ + ไม่มี company query
filter + แตะ `SaveChanges` ทั้งระบบ; Module B: เส้นทาง archive/restore/permanent-delete ที่ทำลาย
ข้อมูลได้จริง) — **gate ยังเปิดอยู่ ไม่ได้ถูกปิดโดยรอบ QA นี้** (functional correctness เท่านั้นคือ
ขอบเขตของ `qa-engineer`) พร้อมส่งต่อ `security` agent ทันทีที่ผู้ใช้ต้องการ

## Change Log

- **2026-08-28** — QA รอบแรกของโมดูลนี้ (FULL) ตรวจ Phase 1 (Module A) และ Phase 2 (Module B) พร้อม
  กัน — ตรวจโค้ดจริงเทียบสัญญา AU-1..AU-20/RS-0..RS-8/DM-A1..DM-A5 ครบทุกข้อ ทุก task ทั้ง 28
  รายการ ✅ Verified ติ๊ก `[x]` ใน `plan.md` ครบ · `dotnet build` 0 error, `dotnet test
  --filter "Category!=Integration"` 358/358 ผ่าน, `dotnet ef migrations has-pending-model-changes`
  ไม่มี drift, `dotnet ef migrations list` ยืนยัน `AddAuditLog` ยังเป็น `(Pending)` (ยังไม่รัน
  `database update`) · บันทึก Unverified Behaviour 2 รายการ (AU-15 rollback, RS-1..RS-5 log-in-
  transaction) ตามที่ `design.md`/`plan.md` เตือนไว้ล่วงหน้าว่า EF InMemory พิสูจน์ไม่ได้ · ยืนยันซ้ำ
  ว่า `🔒 Security gate` ทั้งสอง phase ยังเปิดอยู่ ไม่ถูกปิดโดยรอบนี้ · ไม่พบ drift ระหว่าง
  `design.md` Data Model กับโค้ดจริงแม้จุดเดียว
