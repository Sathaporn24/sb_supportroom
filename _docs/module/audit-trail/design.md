# บันทึกประวัติการกระทำในระบบ (audit-trail) — Feasibility & Design

**โมดูล:** `audit-trail` · **สถานะ:** ออกแบบครบ 2026-08-27 · **มติ OQ-A1 ปิดแล้ว 2026-08-28 →
ไม่มีข้อใดรอคำตอบจากเจ้าของโปรเจกต์เหลืออยู่ในโมดูลนี้** · **`plan.md` เขียนเสร็จแล้วโดย
`project-manager`** · **✅ C1 ปลดล็อกแล้ว 2026-08-28** — ดู `## ✅ เงื่อนไขก่อนลงมือ` ข้างล่าง

## ✅ เงื่อนไขก่อนลงมือ — C1 ปลดล็อกแล้ว 2026-08-28

**เจ้าของโปรเจกต์ให้ไฟเขียว migration เองในแชท 2026-08-28** ("ให้ไฟเขียวตอนนี้เลย") — เข้าเงื่อนไขที่
`requirement.md` ข้อ 3 วางไว้ว่าต้องเป็นคำพูดยืนยันแยกต่างหาก ไม่ใช่แค่เอกสารเขียนเสร็จ

ผลที่ตามมา:

| ใคร | ทำได้แล้ว |
|---|---|
| `project-manager` | วาง `plan.md` ครบทุก phase ✅ (เสร็จแล้ว) |
| `backend-engineer` | **เริ่ม Module A และ B ได้ตาม `plan.md`** |
| ใครก็ตาม | `dotnet ef migrations add` ได้ · `dotnet ef database update` รันได้ (ต้อง rehearsal
  บน staging ก่อนตามที่ `plan.md` §Sequencing Notes ระบุ) |

`🔒 Security gate` ของทั้งสอง Module **ไม่ถูกปลดไปพร้อม C1** — ยังต้องผ่าน `security` audit ก่อน
`devops` จะ ship ได้ เป็นเงื่อนไขคนละชั้น

---

## Feasibility Summary

**ทำได้ทั้งหมดด้วย stack ปัจจุบัน ไม่ต้องเพิ่ม dependency ใหม่แม้แต่ตัวเดียว** — EF Core มีจุดต่อ
(`SaveChanges` override + `ChangeTracker`) ที่ให้ "บันทึกทุก entity ทุก create/update/delete" ได้จาก
จุดเดียว โดยไม่ต้องไปไล่แก้ service ทีละตัว และ PostgreSQL เดิมรับตารางใหม่ตารางเดียวจบ

ความยากทั้งหมดของโมดูลนี้ไม่ได้อยู่ที่ "จะเขียน log ยังไง" แต่อยู่ที่ **"อะไรบ้างที่ interceptor
มองไม่เห็น"** — โปรเจกต์นี้มี raw SQL / `ExecuteUpdate` อยู่ 10 จุด ซึ่งข้ามหัว `ChangeTracker`
ไปเลยโดยธรรมชาติ ในจำนวนนั้น **5 จุดเป็นการกระทำที่คนลงมือ** และต้องเขียน log ด้วยมือ ไม่งั้น
การ archive/restore บทเรียน — ซึ่งเป็นการกระทำที่ "อยากรู้ว่าใครทำ" มากที่สุดในระบบ — จะเป็น
ช่องว่างที่เงียบสนิท ทั้ง 5 จุดถูกไล่จากโค้ดจริงและระบุไว้เป็นสัญญาที่ `## Raw-SQL Manual Audit Rules`

ขนาดงานรวม: **1 ตารางใหม่ · 1 migration (additive ล้วน ไม่แตะคอลัมน์เดิมสักคอลัมน์) ·
1 override ใน `ApplicationDbContext` · 5 จุดแก้ด้วยมือ · 1 signature เปลี่ยน · 2 จุด construction
นอก DI ที่ต้องแก้ตาม** ไม่มี endpoint ไม่มี UI ไม่มี repository ไม่มี service (R6)

## Feature-by-Feature Feasibility

| ความต้องการ | ผล | หมายเหตุ |
|---|---|---|
| **R1** บันทึก create/update/delete ทุก entity ที่ admin ก่อ | ✅ ตรงไปตรงมา | `ChangeTracker.Entries()` ใน `SaveChanges` override เห็นครบทุก entity โดยไม่ต้องแตะ service ใดเลย |
| **R1.1** "delete" รวม soft-delete | ✅ ตรงไปตรงมา | อ่าน `IsDelete` original vs current ที่ระดับ `EntityEntry` — AU-6/AU-7 |
| **R1.2** ไม่รวมกิจกรรมผู้เรียน | ✅ **ได้มาฟรี ไม่ต้องเขียนโค้ดกัน** | ฝั่งผู้เรียนเป็น request ที่ไม่เคย resolve `ICurrentUser` → AU-2 ตัดทิ้งตั้งแต่บรรทัดแรก · **ห้ามทำ blocklist รายตาราง** (AU-18) |
| **R2** ใคร/อะไร/แถวไหน/เมื่อไหร่ ไม่มี diff | ✅ ตรงไปตรงมา | 7 คอลัมน์ จบ |
| **R3 / R3.1** แยกตามบริษัท | ⚠️ ทำได้ แต่ **ไม่มี query filter บังคับ** ตามมติ OQ-2 | ดู AU-11 และความเสี่ยง **AR-1** — การบังคับ isolation ถูกเลื่อนไปอยู่ที่ทางอ่านในอนาคต ไม่ใช่ที่ตัวตาราง · **มติ OQ-A1 (2026-08-28): แถวของ `AdminUser` เป็นระดับระบบ (`null`) ทั้งหมด** จึงอยู่นอกขอบเขต R3.1 โดยตั้งใจ — ดู **AR-10** |
| **R4** เก็บถาวร ไม่มี retention | ✅ ไม่ต้องทำอะไร | แต่ดู **AR-5** เรื่องปริมาณจริงหลังมติ Q-A4 |
| **R5** ขยายชนิด action ได้ทีหลัง | ✅ ตรงไปตรงมา | `Action` เป็น `string` + `static class AuditAction` ตาม convention ของโปรเจกต์ (ห้าม C# enum) — เพิ่มค่าใหม่ไม่ต้อง migration |
| **R6** ไม่มี UI/endpoint | ✅ | ระบุเป็นข้อห้ามที่ AU-17 เพราะ "Feature Development Pattern" ใน `CLAUDE.md` จะพา engineer ไปสร้าง repository→service→controller โดยอัตโนมัติถ้าไม่ห้ามไว้ |
| **OQ-4** login/logout | ⛔ ตัดออกจากรอบนี้ (มติ) | **ได้ผลข้างเคียงที่ดี:** ตอน login ยังไม่มี JWT → `ICurrentUser` ยังไม่ resolve → การเขียน `LastLoginAt` จึงไม่เกิดแถว log เองอยู่แล้ว ไม่ต้อง special-case · เป็นผลพลอยได้ ไม่ใช่การรับประกัน — ถ้าเส้นทาง login เปลี่ยนต้องกลับมาดูข้อนี้ |

### ของเดิมที่ตรวจแล้วในรอบนี้ (reconcile ครบ ไม่มีอะไรค้าง)

| ของที่มีอยู่แล้ว | คำตัดสินรอบนี้ |
|---|---|
| คอลัมน์ `CreateBy`/`UpdateBy`/`DeleteBy`/`CreateDate`/`UpdateDate`/`DeletedAt` ทุก entity | **ไม่แตะ ไม่แทนที่** — โมดูลนี้เพิ่มตารางข้างๆ · ยืนยันจาก `IEntityMaster.cs:12-18` ว่าทั้ง 16 entity implement จริง |
| soft-delete (`IsDelete`/`DeletedAt`) | **ใช้ต่อ** — และเป็นตัวตัดสิน action ที่ AU-6 |
| `AdminUser.LastLoginAt` | **ไม่แตะ** — OQ-4 ตัดออกจากรอบนี้ |
| `ILogger<T>` | คนละชั้น ไม่เกี่ยวกัน |
| `backend/docs/supportroom-schema.sql` · `supportroom.dbml` · `DATABASE_SCHEMA_SUMMARY.md` · `supportroom-migrations-idempotent.sql` | **artifact ที่ generate จาก schema ปัจจุบัน (16 ตาราง ไม่มีตาราง audit)** — ไม่ใช่ asset ที่เตรียมไว้ล่วงหน้าสำหรับโมดูลนี้ · **ต้อง regenerate หลัง migration ผ่าน** (อยู่ในงานของ Module A) |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | ต้องเพิ่ม `AuditLog` (Definition of Done ข้อสุดท้ายของ `CLAUDE.md`) — งานของ Module A |
| รูปแบบ worker ของ `lesson_purge` | ✅ ยืนยันแล้วว่ามีจริงและใช้ซ้ำได้ (`IBackgroundJobProcessor.cs` + `BackgroundJob` entity) — ปิดข้อสงสัยที่ `requirement.md` §References ฝากไว้ · **แต่ยังไม่ใช้ในรอบนี้** เพราะ R4 ไม่มี retention |
| `TryRestore` และ `CancelPendingLessonPurge` | **ไม่มี caller ใน production เลยทั้งคู่** (ยืนยันด้วย grep ทั้ง `backend/src`) — จัดการต่างกัน ดู RS-3 กับ RS-7 |

### การตัดสินใจที่ผู้ใช้ยืนยันแล้ว

| # | คำถาม | มติ | ตัดอะไรทิ้ง |
|---|---|---|---|
| **OQ-2** | `CompanyId` ของแถว log เมื่อ entity ไม่มีบริษัท | **nullable** · `ICompanyScoped` → `CompanyId` ของแถวนั้น · `Company` → `Id` ตัวเอง · ที่เหลือ → `null` = ระดับระบบ · **ไม่มี query filter บังคับ** | ตัดทางเลือก "บังคับ non-null แล้วใช้ค่า sentinel" และตัด `HasQueryFilter` ออก — isolation จึงไม่ได้ถูกบังคับที่ตาราง (AR-1) |
| **OQ-A1** *(ปิด 2026-08-28 — ข้ออื่นในตารางนี้ปิด 2026-08-27)* | `CompanyId` ของแถว log ที่ entity เป็น `AdminUser` โดยเฉพาะกรณี `admin` แก้ไข/ปิดบัญชีพนักงานคนอื่นในบริษัทตัวเอง | **ทางเลือก ก — `null` เสมอ (ระดับระบบ)** ยึดตามตัวอักษรของมติ OQ-2 · **ไม่รับ**ทางเลือก ข (`AdminUser` → `CompanyId` ของแถวนั้น) ที่ `system-analyst` เสนอเป็นทางเลือก · **AU-11 ที่เขียนไว้เป็น default อยู่แล้วถูกต้อง ไม่ต้องแก้แม้บรรทัดเดียว** | ตัดทางเลือก ข ทิ้ง · **trade-off ที่เจ้าของโปรเจกต์ยอมรับแล้ว: dashboard ในอนาคต — `admin` ระดับบริษัทจะไม่เห็นว่าใครแก้/ปิดบัญชีพนักงานในบริษัทตัวเอง** เพราะทุกแถวของ `AdminUser` เป็นระดับระบบที่มีแต่ `owner` เห็น (ดู **AR-10**) · ถ้าวันหนึ่งต้องการให้เห็น = **กลับมติ** ต้องผ่าน `system-analyst` ไม่ใช่แก้ AU-11 เอง |
| **OQ-3** | action ที่ระบบก่อเอง (worker/job/migration) | **ไม่บันทึก — ไม่มีคน = ไม่มีแถว** | ตัด actor แบบ `"system"` ทิ้ง · `ActorUserId` จึงเป็น non-null ได้ |
| **OQ-4** | login / logout / login ล้มเหลว | **ไม่รวมในรอบนี้** เป็นงานอนาคต | ตัดการทำ schema ให้รองรับ event ที่ไม่ผูกกับแถวข้อมูล (ไม่ต้องมี `EntityId` ที่ nullable) |
| **Q-A1** | `AuditLog` ควร implement `IEntityMaster` ไหม | **ไม่ — รับข้อยกเว้นนี้** append-only ไม่มี `IsDelete`/`UpdateBy`/`DeleteBy` โดยตั้งใจ | ตัด 6 คอลัมน์ที่ไม่มีความหมายกับตาราง append-only ทิ้ง |
| **Q-A2** | มี `MetadataJson` ไหม | **ไม่ใส่ในรอบนี้** — `Action` string ขยายได้พอแล้วตาม R5 | **ตัดช่องที่ diff จะแอบกลับเข้ามาโดยไม่ผ่านการตัดสินใจ** (R2 ตัด diff ไปแล้ว) |
| **Q-A3** | log เขียนไม่สำเร็จ → business write ล้มตามไหม | **ล้มด้วยกัน ทั้ง transaction rollback** | ตัดโหมด "degrade แล้วเดินต่อ" ทิ้ง — **ขัดกับ convention ประจำโปรเจกต์โดยตั้งใจ** ดู AU-15 |
| **OQ-5** | `cs` เห็น log ไหมตอนมี dashboard | **ไม่เห็น — เฉพาะ `owner`/`admin`** | ไม่กระทบ schema/implementation รอบนี้ · บันทึกไว้เป็นแนวทางตอนออกแบบ dashboard |
| **Q-A4** | ขอบเขต "ทุก entity" รวม entity ปริมาณสูงจริงไหม | **ใช่ ทุกแถวจริงๆ** รวม `LessonSlideNarration`/`LessonExcludedSlide` ตาม R1 เดิม | ตัดทางเลือก "ยกเว้น entity ปริมาณสูง" — แลกมาด้วยความเสี่ยงปริมาณที่ **AR-5** |

---

## Data Model

### DM-A1 · `AuditLog` (ตารางใหม่ — ตารางเดียวของโมดูลนี้)

ไฟล์: `backend/src/SupportRoom.Domain/Entities/AuditLog.cs`

```csharp
namespace SupportRoom.Domain.Entities;

/// <summary>
/// บันทึกดิบเรียงตามเวลาว่า AdminUser คนไหนทำอะไรกับแถวไหนเมื่อไหร่ (R1/R2) - แก้ปัญหา P1
/// ที่คอลัมน์ CreateBy/UpdateBy/DeleteBy เก็บได้แค่ "คนล่าสุด" ตารางนี้ไม่ได้มาแทนคอลัมน์พวกนั้น
/// มันอยู่ข้างๆ กัน
///
/// ⛔ จงใจ NOT implement IEntityMaster (มติ Q-A1) - append-only: ไม่มี IsDelete/UpdateBy/DeleteBy
///    ให้แก้หรือลบ ถ้าวันหนึ่งมีโค้ดที่ Update หรือ Delete แถวในตารางนี้ นั่นคือบั๊ก ไม่ใช่ฟีเจอร์
///
/// ⛔ จงใจ NOT implement ICompanyScoped และ **ไม่มี HasQueryFilter** (มติ OQ-2) ด้วยสองเหตุผล
///    ที่เป็นอิสระจากกัน: (1) CompanyId เป็น null ได้ ซึ่ง ICompanyScoped ไม่รองรับ
///    (2) filter `CompanyId == context` จะทำให้แถวระดับระบบ (null) หายไปจากทุกคนตลอดกาล
///    ผลที่ตามมาซึ่งต้องรู้: ตารางนี้ไม่มีตาข่ายนิรภัยเหมือน 14 ตารางที่มี filter - **ทางอ่านใดๆ
///    ในอนาคตต้อง filter CompanyId ด้วยตัวเอง** รูปแบบเดียวกับ BackgroundJob (design.md
///    ของ knowledge-base, SEC-2) เป๊ะ
///
/// ⛔ ไม่เก็บค่าก่อน/หลัง (R2 - เจ้าของโปรเจกต์ตัดออกแล้ว 2026-08-27) และไม่มี MetadataJson
///    (มติ Q-A2) - ตารางนี้ตอบว่า "ใครแก้แถวนี้ตอนไหน" ไม่ตอบว่า "เปลี่ยนจากอะไรเป็นอะไร"
///    ถ้าต้องกู้ค่าเดิม ต้องพึ่ง database backup (C5)
/// </summary>
public sealed class AuditLog
{
    /// <summary>IdGenerator.GenerateId("audit")</summary>
    public required string Id { get; init; }

    /// <summary>
    /// บริษัทของ **ข้อมูลที่ถูกกระทำ** ไม่ใช่ของคนที่ลงมือ (R3.1) - owner ที่ CompanyId = null
    /// แก้ข้อมูลของบริษัท A แถวนี้ต้องเป็น A ไม่ใช่ null ไม่งั้นบริษัทเจ้าของข้อมูลจะมองไม่เห็น
    /// การกระทำที่เกิดกับข้อมูลตัวเอง · null = ระดับระบบ ดูกติกาการหาค่าที่ AU-11
    /// </summary>
    public string? CompanyId { get; init; }

    /// <summary>
    /// AdminUser.Id ของคนที่ลงมือ - **ไม่มีวันเป็น null** (มติ OQ-3: ไม่มีคน = ไม่มีแถว)
    /// ไม่ทำ FK จริงตาม convention ของโปรเจกต์ (ทุกความสัมพันธ์ข้าม entity เป็น logical string id)
    /// และเพราะ AdminUser ถูก deactivate ไม่ใช่ลบ (AdminUser.cs:47-49) id จึง resolve ได้เสมอ
    /// </summary>
    public required string ActorUserId { get; init; }

    /// <summary>
    /// ค่าจาก AuditAction - รอบนี้มี create | update | delete (R5: เพิ่มค่าใหม่ได้ทีหลัง
    /// โดยไม่ต้อง migration) · เป็น string ไม่ใช่ C# enum ตาม convention ของโปรเจกต์
    /// </summary>
    public required string Action { get; init; }

    /// <summary>ชื่อคลาสของ entity ที่ถูกกระทำ เช่น "LessonConfig" - มาจาก
    /// entry.Metadata.ClrType.Name ไม่ใช่ชื่อตารางที่พิมพ์มือ (AU-12)</summary>
    public required string EntityName { get; init; }

    /// <summary>primary key ของแถวที่ถูกกระทำ - ทุก PK ในโปรเจกต์นี้เป็น string เดี่ยว
    /// ที่ service สร้างเองก่อน SaveChanges (IdGenerator) ดู AU-10 ว่าทำไมข้อเท็จจริงนี้สำคัญ</summary>
    public required string EntityId { get; init; }

    /// <summary>UTC เสมอ · หนึ่ง SaveChanges ที่เกิดหลายแถวต้องใช้ค่าเดียวกันทุกแถว (AU-9)
    /// เพื่อให้ "การกระทำครั้งเดียว" ยังจับกลุ่มกันได้ตอน SELECT แม้ไม่มีคอลัมน์ correlation id</summary>
    public required DateTime OccurredAt { get; init; }
}
```

> **7 ฟิลด์ เท่านี้ และห้ามเติม** — ใครกำลังจะเพิ่ม `BeforeJson`/`AfterJson`/`MetadataJson`/
> `IpAddress`/`UserAgent`/`ActorRole`/`CorrelationId` แปลว่ากำลังทำสิ่งที่ R2 หรือมติ Q-A2 ปฏิเสธ
> ไปแล้ว ให้หยุดแล้วตีกลับมาที่ `system-analyst` (`ActorRole` เป็นข้อที่คุยกันแล้วและเลื่อนโดยตั้งใจ
> ดู OQ-A3)

### DM-A2 · `AuditAction` (constants ใหม่)

ไฟล์: `backend/src/SupportRoom.Domain/Enums/AuditAction.cs`

```csharp
namespace SupportRoom.Domain.Enums;

/// <summary>String constants ด้วยเหตุผลเดียวกับ BackgroundJobStatus/AnswerStatus - ห้ามใช้ C# enum
/// (convention ของโปรเจกต์) · R5: เพิ่มค่าใหม่ที่นี่ได้โดยไม่ต้องแตะ schema</summary>
public static class AuditAction
{
    public const string Create = "create";
    public const string Update = "update";

    /// <summary>รวม soft-delete (IsDelete false -> true) ด้วย ไม่ใช่แค่ hard delete - R1.1
    /// soft-delete คือรูปแบบการลบปกติของระบบนี้ ดู AU-6</summary>
    public const string Delete = "delete";
}
```

### DM-A3 · `ApplicationDbContext.OnModelCreating` (ส่วนที่เพิ่ม)

```csharp
builder.Entity<AuditLog>(entity =>
{
    entity.HasKey(x => x.Id);
    // "บริษัทนี้เกิดอะไรบ้างช่วงนี้" - คิวรีหลักของ dashboard ในอนาคต และของ dev ที่เปิด SELECT
    entity.HasIndex(x => new { x.CompanyId, x.OccurredAt });
    // "แถวนี้ใครแตะบ้าง" - นี่คือคำถามที่ P1 ตอบไม่ได้ และเป็นเหตุผลที่โมดูลนี้มีอยู่
    entity.HasIndex(x => new { x.EntityName, x.EntityId });
    // "คนนี้ทำอะไรไปบ้าง" - คิวรีตอนสืบหาตัวการ
    entity.HasIndex(x => x.ActorUserId);
    // ⛔ ไม่มี HasQueryFilter โดยตั้งใจ (มติ OQ-2) - เหตุผลเต็มอยู่ที่ doc comment ของ entity
    //    ห้าม "แก้" ด้วยการเติม filter: แถว CompanyId = null จะหายไปจากทุกคน
});
```

พร้อม `public DbSet<AuditLog> AuditLog => Set<AuditLog>();`

### DM-A4 · `ApplicationDbContext` constructor (แก้ของเดิม — **additive แต่กระทบ 2 จุดนอก DI**)

```csharp
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICompanyContext companyContext,
    ICurrentUser currentUser) : DbContext(options)
```

`ICurrentUser` ถูกลงทะเบียนเป็น `AddScoped` อยู่แล้วที่ `ServiceConfiguration.cs:26` จึง resolve
ผ่าน DI ได้ทันที **แต่มีอีก 2 จุดที่ `new ApplicationDbContext(...)` ตรงๆ ต้องแก้ตาม** — ดู
`## Blast Radius`

### DM-A5 · `IBackgroundJobRepository.AccelerateLessonPurge` (แก้ signature — **breaking ต่อ caller/fake ไม่ใช่ต่อข้อมูล**)

```csharp
// เดิม
bool AccelerateLessonPurge(string companyId, string lessonId, string purgeJobId);
// ใหม่
bool AccelerateLessonPurge(string companyId, string lessonId, string purgeJobId, string? actorUserId);
```

จำเป็นเพราะเมธอดนี้เป็น raw SQL ที่ไม่รู้จักผู้ลงมือเลย ต่างจาก `TryArchive`/`TryRestore` ที่มี
`actorUserId` อยู่แล้ว · เป็นการเปลี่ยนที่ compiler จับได้ทั้งหมด ไม่ใช่การเปลี่ยนที่เงียบ

### สรุปผลกระทบต่อข้อมูลที่มีอยู่จริง

**เป็น additive ล้วน 100% ไม่มี breaking change ต่อข้อมูล** — ตารางใหม่หนึ่งตาราง ไม่แตะคอลัมน์
เดิมสักคอลัมน์ ไม่เปลี่ยน type ไม่ลบอะไร ไม่ต้อง backfill (log ย้อนหลังสร้างไม่ได้อยู่แล้ว
เพราะข้อมูลไม่เคยมี) · rollback = drop ตารางเดียว

---

## Audit Capture Rules (AU-1 … AU-20) — contract

**สัญญานี้คือทั้งหมดของ Module A** — engineer ต้องอ่านครบทุกข้อ ไม่ใช่อ่านผ่าน

| # | กติกา |
|---|---|
| **AU-1** | override **`SaveChanges(bool acceptAllChangesOnSuccess)`** และ **`SaveChangesAsync(bool, CancellationToken)`** ใน `ApplicationDbContext` — **ไม่ใช่** เวอร์ชันไม่มีพารามิเตอร์ · สองตัวนี้คือจุดที่ทุกเส้นทางไหลผ่านจริง การ override เฉพาะเวอร์ชันไม่มีพารามิเตอร์จะทำให้ call site ที่เรียก overload แบบมี `bool` หลุด log ไปเงียบๆ · ทั้งสองเรียก private method ตัวเดียวกัน ห้ามเขียน logic ซ้ำสองชุด |
| **AU-2** | บรรทัดแรกของ method นั้น: `if (string.IsNullOrEmpty(currentUser.UserId)) return;` — **ไม่มีคน = ไม่มีแถว ไม่มีข้อยกเว้น** (มติ OQ-3) · นี่คือข้อเดียวที่ทำให้ R1.2 (ไม่บันทึกกิจกรรมผู้เรียน), OQ-3 (ไม่บันทึก worker) และ OQ-4 (ไม่บันทึก login) เป็นจริงพร้อมกันโดยไม่ต้องเขียนเงื่อนไขเพิ่มอีกเลย |
| **AU-3** | อ่าน `ChangeTracker.Entries()` แล้ว **`.ToList()` ทันที** ก่อนสร้างแถวใดๆ · การ `Add` แถว `AuditLog` ระหว่างวน `Entries()` แบบ lazy จะแก้ collection ที่กำลังวนอยู่ |
| **AU-4** | ข้าม entry ที่ `entry.Entity is AuditLog` — กันวนซ้ำไม่รู้จบ · ข้อนี้ยังเป็นสิ่งที่ทำให้แถวที่จุด raw SQL เขียนด้วยมือ (RS-*) ไม่ถูกนับซ้ำอีกรอบ |
| **AU-5** | ข้าม entry ที่ `entry.Metadata.IsOwned()` — วันนี้คือ `LessonConfig.SlideConfigs` (`OwnsMany(...).ToJson()` ที่ `ApplicationDbContext.cs:116`) · `SlideConfig` ไม่มี PK ของตัวเอง (ใช้ shadow key) การบันทึกมันจะได้แถวที่ `EntityId` ไม่มีความหมาย และบทเรียนหนึ่งบทที่ถูกบันทึกจะสร้าง log เท่าจำนวนสไลด์ทั้งที่คนกดแค่ครั้งเดียว |
| **AU-6** | map action: `Added` → `create` · `Deleted` → `delete` · `Modified` → `update` **ยกเว้น** entity ที่มี property `IsDelete` และ `OriginalValue == false && CurrentValue == true` → `delete` (R1.1) |
| **AU-7** | **ข้อจำกัดที่รู้ตัวของ AU-6:** `DbSet.Update(entity)` บน entity ที่ยัง detached จะตั้ง `OriginalValues = CurrentValues` ทำให้ transition `false → true` มองไม่เห็น และจะถูกบันทึกเป็น `update` แทน `delete` · **ทุกเส้นทาง soft-delete ในโค้ดวันนี้อ่านแถวมาก่อน** (`Get()` / `GetIncludingDeleted()`) แล้วค่อยแก้ instance ที่ track อยู่ จึงไม่โดนข้อนี้ · ถ้ามีเส้นทางใหม่ที่ `Update()` entity ที่ยัง detached engineer **ต้องตีกลับมาที่ `system-analyst`** ไม่ใช่แก้ด้วยการเดา |
| **AU-8** | `IsDelete` `true → false` (การกู้คืน) → บันทึกเป็น **`update`** ในรอบนี้ ไม่สร้าง action `restore` ใหม่ · เป็น default ที่ตั้งใจและบันทึกไว้ที่ **OQ-A2** ไม่ใช่การมองข้าม |
| **AU-9** | คำนวณ `var now = DateTime.UtcNow;` **ครั้งเดียว** ต่อการเรียก `SaveChanges` หนึ่งครั้ง แล้วใช้ค่าเดียวกันทุกแถวที่เกิดในรอบนั้น — ตารางนี้ไม่มี correlation id ค่า timestamp ที่ตรงกันเป๊ะคือสิ่งเดียวที่บอกได้ว่าหลายแถวมาจากการกระทำครั้งเดียวกัน |
| **AU-10** | `EntityId` = ค่าของ primary key เป็น string · ทุก entity ในโปรเจกต์นี้มี PK เป็น `string` เดี่ยวที่ **service สร้างเองด้วย `IdGenerator` ก่อน `SaveChanges`** — ข้อเท็จจริงนี้คือเหตุผลเดียวที่วิธีนี้ใช้ได้ (ถ้า PK มาจาก database `EntityId` ของแถว `create` จะว่างเปล่า) · ถ้าเจอ entity ที่ PK ไม่ใช่ string เดี่ยว หรือค่าที่ได้เป็นค่าว่าง ให้ **`throw new InvalidOperationException` พร้อมข้อความภาษาไทยที่บอกชื่อ entity** ห้ามเขียนแถวที่ `EntityId` ว่างและห้ามข้ามเงียบๆ |
| **AU-11** | `CompanyId` หาแบบนี้ **ตามลำดับนี้เท่านั้น**: `entry.Entity is ICompanyScoped s` → `s.CompanyId` · `entry.Entity is Company c` → `c.Id` · นอกนั้น → `null` · อ่านจาก CLR object ไม่ใช่ `entry.Property("CompanyId")` เพราะ object materialize แล้วเสมอทุก state รวมถึง `Deleted` · **วันนี้ entity ที่ตกมาที่ `null` มีตัวเดียวคือ `AdminUser`** (`Company` กับ `AdminUser` เป็นสองตัวเดียวที่ไม่ implement `ICompanyScoped`) · **ยืนยันเป็นมติแล้ว (OQ-A1, 2026-08-28): แถวของ `AdminUser` เป็น `null` ทุกแถวไม่มีข้อยกเว้น รวมกรณีที่ `admin` แก้ไข/ปิดบัญชีพนักงานคนอื่นในบริษัทตัวเอง** — ⛔ **ห้าม "ทำให้ฉลาดขึ้น" ด้วยการอ่าน `AdminUser.CompanyId` ของแถวนั้น** นั่นคือทางเลือกที่ถูกปฏิเสธไปแล้ว ถ้าเห็นว่าควรเปลี่ยนให้ตีกลับมาที่ `system-analyst` ไม่ใช่แก้เอง |
| **AU-12** | `EntityName` = `entry.Metadata.ClrType.Name` ห้ามพิมพ์ชื่อตารางเป็น string มือ และห้ามใช้ `entry.Entity.GetType().Name` |
| **AU-13** | หนึ่งแถว log ต่อ **หนึ่ง entity ต่อหนึ่ง `SaveChanges`** ไม่ใช่ต่อ property ที่เปลี่ยน · แก้ 5 ฟิลด์ในแถวเดียว = 1 แถว |
| **AU-14** | `Set<AuditLog>().AddRange(rows)` แล้วปล่อยให้ `base.SaveChanges(...)` เขียนทั้งหมดในครั้งเดียว · **ห้ามเรียก `SaveChanges` ซ้อนภายใน** ไม่ว่ากรณีใด — นั่นคือสิ่งที่ทำให้มติ Q-A3 (atomic) เป็นจริงโดยไม่ต้องเปิด transaction เอง |
| **AU-15** | **ห้ามครอบ `try/catch` แล้วกลืน exception เด็ดขาด** · `CLAUDE.md` มี convention ว่า "service ห้ามพัง flow หลักเพราะ integration รอง — log warning + degrade แทน throw" — **ข้อนี้เป็นข้อยกเว้นที่เจ้าของโปรเจกต์เคาะแล้ว (มติ Q-A3)**: log เขียนไม่ได้ = business write ต้องล้มตาม ทั้ง transaction rollback · engineer ที่ทำตาม convention โดยไม่อ่านข้อนี้จะทำให้ log หายเงียบพอดีตอนที่ระบบมีปัญหา ซึ่งคือช่วงเวลาที่ต้องการมันที่สุด |
| **AU-16** | `AuditLog` เป็น **append-only** — ห้ามมีโค้ดที่ `Update` หรือ `Delete` แถวในตารางนี้ที่ไหนเลยในระบบ (มติ Q-A1) |
| **AU-17** | **รอบนี้ไม่สร้าง `IAuditLogRepository`, service, DTO, ViewModel, controller หรือ endpoint ใดๆ** (R6) · `CLAUDE.md` §"Feature Development Pattern" จะพา engineer ไปสร้างครบทั้งสาย — ที่นี่ไม่ต้อง · ห้ามลงทะเบียนอะไรเพิ่มใน `UnitOfWork.Register` ด้วย ตัว interceptor เขียนผ่าน `Set<AuditLog>()` ตรงๆ และจุด raw SQL ใช้ `Context` ที่ repository มีอยู่แล้ว |
| **AU-18** | **ห้ามทำ blocklist/allowlist รายตาราง** เพื่อกัน `LearningSession`/`SessionQuestion` · R1.2 เขียนไว้ชัดว่าเส้นแบ่งคือ **"ใครลงมือ" ไม่ใช่ "ตารางไหน"** — CS ที่แก้ `SessionQuestion` ในคิวรีวิว **ต้องถูกบันทึก** ส่วนผู้เรียนที่สร้างแถวเดียวกันต้องไม่ถูกบันทึก AU-2 แยกสองกรณีนี้ถูกต้องอยู่แล้ว blocklist จะทำให้ผิด |
| **AU-19** | scope ของ background worker ไม่เคย resolve `ICurrentUser` → ไม่เกิดแถวใดๆ (ถูกต้องตามมติ OQ-3) · **ผลที่ต้องรู้และยอมรับ:** การ hard-delete แบบ cascade ทั้งหมดตอน `lesson_purge` ทำงานจริง จะไม่มี log เลย — ประวัติของบทเรียนที่ถูกลบถาวรจะมีแค่แถว "คนสั่ง archive" (RS-1) กับ "คนสั่งเร่งลบ" (RS-5) ไม่มีแถวของการลบจริง |
| **AU-20** | เขียน unit test ที่ยืนยัน AU-2 (ไม่มี actor → 0 แถว), AU-4, AU-5 (บันทึก `LessonConfig` ที่มี `SlideConfigs` หลายอัน → 1 แถว ไม่ใช่ N+1), AU-6 ทั้งสามทาง และ AU-9 (หลาย entity ใน SaveChanges เดียว → `OccurredAt` เท่ากันทุกแถว) · ใช้ EF InMemory ได้เหมือน `CompanyIsolationTests` แต่ **AU-15 ทดสอบด้วย InMemory ไม่ได้** (ไม่มี transaction จริง) ให้บันทึกเป็น unverified behaviour แทนการแกล้งว่าทดสอบแล้ว |

---

## Raw-SQL Manual Audit Rules (RS-1 … RS-8) — contract

**สัญญานี้คือทั้งหมดของ Module B** · `ExecuteUpdate` / `ExecuteSqlRaw` / `ExecuteDelete` **ข้าม
`ChangeTracker` ทั้งหมด** — interceptor ที่ AU-* มองไม่เห็นแม้แต่แถวเดียว ทุกจุดข้างล่างจึงต้องสร้าง
`AuditLog` เองด้วยมือ

**RS-0 · กติกาที่ใช้กับทุกจุด:**
- สร้างแถวด้วย `Context.Set<AuditLog>().Add(...)` แล้วให้มันลงไปพร้อมกับคำสั่ง SQL **ในธุรกรรม
  เดียวกัน** (มติ Q-A3) · จุดไหนที่วันนี้ยังไม่มี transaction ต้องเปิด `Context.Database.BeginTransaction()` เพิ่ม
- **เขียน log เฉพาะเมื่อคำสั่งนั้นแก้แถวได้จริง** (`rows == 1` / `archived == 1`) — คำสั่งที่แพ้ race
  หรือไม่ match เงื่อนไข ต้องไม่ทิ้งแถว log ไว้ ไม่งั้น log จะบอกว่ามีคนลบบทเรียนทั้งที่ไม่มีอะไรเกิดขึ้น
- ถ้า `actorUserId` เป็น `null` → **ไม่เขียนแถว** (มติ OQ-3) ไม่ใช่เขียนด้วย actor ว่าง

| # | จุด (ไฟล์:บรรทัดวันนี้) | ต้องเขียนอะไร |
|---|---|---|
| **RS-1** | `LessonConfigRepository.TryArchive` — `ExecuteUpdate` บน `LessonConfig` (`ILessonConfigRepository.cs:78-87`) | **1 แถว** หลัง `archived == 1` และก่อน `transaction.Commit()`: `delete` / `LessonConfig` / `lessonId` / `companyId` / `actorUserId` · ⚠️ แถว `BackgroundJob` ที่ `Add` ต่อจากนั้น (บรรทัด 94-106) **ไม่ต้องเขียนเอง** — มันผ่าน `Context.SaveChanges()` ปกติ interceptor จับให้แล้ว การเขียนซ้ำจะได้ 2 แถว |
| **RS-2** | cascade ปิด `TrainingLink` ใน `TryArchive` (`ILessonConfigRepository.cs:108-115`) | **1 แถวต่อลิงก์ที่ถูกปิดจริง** — `ExecuteUpdate` คืนแค่จำนวน ไม่คืน id จึงต้อง **`SELECT` id ก่อน** ด้วย `Where` ชุดเดียวกันเป๊ะ (`CompanyId == companyId && LessonId == lessonId && !IsDelete`) แล้วค่อย `ExecuteUpdate` แล้วเขียน `delete` / `TrainingLink` / `<แต่ละ id>` · **ห้ามเขียนแถวรวมแถวเดียว** ("ปิดไป 5 ลิงก์") — R2 บอกว่าหนึ่งแถวผูกกับ **แถวข้อมูลหนึ่งแถว** · ถ้าไม่มีลิงก์เลย → 0 แถว ไม่ใช่แถวว่าง |
| **RS-3** | `LessonConfigRepository.TryRestore` (`ILessonConfigRepository.cs:133-144`) | **1 แถว** หลัง `rows == 1`: `update` / `LessonConfig` / `lessonId` (ตาม AU-8) · ⚠️ เมธอดนี้ **ไม่มี caller ใน production วันนี้** (`RestoreAsync` เรียก `TryRestoreAndCancelPurge` แทน) แต่ยังอยู่บน interface — ต้องทำให้ครบเพื่อไม่ให้ "วันที่มีคนต่อสายมัน" กลายเป็นช่องว่างเงียบ · **ต้องเปิด transaction เพิ่ม** เพราะวันนี้เป็น `ExecuteSqlRaw` เดี่ยวๆ ไม่มี transaction |
| **RS-4** | `LessonConfigRepository.TryRestoreAndCancelPurge` (`ILessonConfigRepository.cs:146-189`) | **2 แถว** เขียนหลัง `canceled == 1` และก่อน `transaction.Commit()` เท่านั้น: (`update` / `LessonConfig` / `lessonId`) และ (`update` / `BackgroundJob` / `purgeJobId`) · เส้นทาง rollback ทั้งสองเส้น (บรรทัด 161, 183) → **0 แถว** |
| **RS-5** | `BackgroundJobRepository.AccelerateLessonPurge` (`IBackgroundJobRepository.cs:100-110`) | **1 แถว** หลัง `rows == 1`: `update` / `BackgroundJob` / `purgeJobId` / `companyId` / `actorUserId` · ต้องแก้ 3 อย่างพร้อมกัน: (ก) เพิ่มพารามิเตอร์ `string? actorUserId` (DM-A5) (ข) เปิด `BeginTransaction` ครอบ เพราะวันนี้เป็น `ExecuteSqlRaw` เดี่ยว (ค) call site `ILessonConfigService.cs:502` ส่ง `CurrentUserId` · ⚠️ **ไม่เขียนแถวของ `LessonConfig` ที่นี่** — คำสั่งนี้แตะแค่แถว job ไม่ได้แตะบทเรียน |
| **RS-6** | จุดที่ **จงใจไม่เขียน log** และห้ามเผลอเติม: `LessonConfigRepository.TryClaimPurge` · `BackgroundJobRepository.ClaimNext` · `BackgroundJobRepository.RequeueOrphanedRunning` | ทั้งสามเป็นการกระทำที่ **worker ก่อเอง ไม่มีคนลงมือ** → มติ OQ-3 ตัดออกชัดเจน · ถ้าเห็นแล้วรู้สึกว่า "น่าจะบันทึกด้วย" ให้ตีกลับมาที่ `system-analyst` ไม่ใช่เติมเอง |
| **RS-7** | `BackgroundJobRepository.CancelPendingLessonPurge` (`IBackgroundJobRepository.cs:88-98`) | **ไม่มี caller ใน production วันนี้** และความหมายกำกวมกว่า RS-3 (การยกเลิก job อาจเกิดจากคนหรือจากระบบก็ได้) จึง **ยังไม่กำหนดกติกาในรอบนี้** — ถ้าวันหนึ่งมี caller ที่เป็นคำสั่งของคน **ต้องกลับมาที่ `system-analyst` ก่อนต่อสาย** ห้าม engineer ตัดสินเอง |
| **RS-8** | **กติกาถาวรสำหรับอนาคต:** ทุกครั้งที่มีใครเพิ่ม `ExecuteUpdate` / `ExecuteSqlRaw` / `ExecuteDelete` ใหม่ที่ไหนก็ตามในโปรเจกต์ ต้องเขียน `AuditLog` ในธุรกรรมเดียวกันด้วย **หรือ** เขียนคอมเมนต์ระบุเหตุผลว่าทำไมไม่ต้อง (worker เท่านั้น/ไม่มีคนลงมือ) · ข้อนี้ต้องถูกคัดลงเป็นบรรทัดใน `CLAUDE.md` §"Architecture Rules" ตอน implement Module B — ไม่งั้นมันจะเป็นความรู้ที่อยู่แค่ในเอกสารนี้ ดู **AR-4** |

---

## Blast Radius — ไฟล์ที่ต้องแก้คู่กัน ไม่งั้น build พังหรือ log หายเงียบ

| ไฟล์ | ทำไม | ถ้าลืม |
|---|---|---|
| `backend/src/SupportRoom.Providers.Data/Data/DesignTimeDbContextFactory.cs:41` | `new ApplicationDbContext(options, new CompanyContext())` — ctor เพิ่มพารามิเตอร์ที่สาม ให้ส่ง `new CurrentUser()` ที่ยังไม่ resolve (design time ไม่มีคนอยู่แล้ว → AU-2 ตัดทิ้ง ปลอดภัยโดยธรรมชาติ เหตุผลเดียวกับที่ไฟล์นั้นปล่อย company context ไว้ไม่ resolve) | `dotnet ef` ทุกคำสั่งพัง = สร้าง migration ไม่ได้เลย |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs:47` | `new ApplicationDbContext(options, _companyContext)` เหมือนกัน | test project compile ไม่ผ่านทั้งชุด |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | fake ของ `IBackgroundJobRepository` ต้องรับ signature ใหม่ของ `AccelerateLessonPurge` (DM-A5) และ fake ของ `ILessonConfigRepository` (`TryRestore` อยู่ที่บรรทัด 118) ยังต้อง compile ได้ | test project compile ไม่ผ่าน |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs:502` | call site ของ `AccelerateLessonPurge` ต้องส่ง `CurrentUserId` | compile ไม่ผ่าน (ดี — จับได้แน่นอน) |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | เพิ่ม `AuditLog` เข้า ER | เอกสารกับโค้ดไม่ตรง (ผิด Definition of Done) |
| `backend/docs/DATABASE_SCHEMA_SUMMARY.md` · `supportroom-schema.sql` · `supportroom.dbml` · `supportroom-migrations-idempotent.sql` | artifact ที่ generate จาก schema — ต้อง regenerate หลัง migration (16 → 17 ตาราง) | เอกสาร schema ชี้ผิดสำหรับทุกคนที่อ่านหลังจากนี้ |
| `CLAUDE.md` §Architecture Rules | เพิ่มบรรทัดของ RS-8 | ดู **AR-4** — ความรู้ตายไปพร้อมโมดูลนี้ |

**ไฟล์ที่ตั้งใจ *ไม่* แตะ:** `UnitOfWork.cs` (ไม่มี repository ใหม่ — AU-17) · entity เดิมทั้ง 16 ตัว ·
คอลัมน์ audit เดิมทุกคอลัมน์ · `IEntityMaster.cs`

---

## Migration Plan

**MG-A1 · migration เดียว: `AddAuditLog`**

- **additive ล้วน** — `CREATE TABLE "AuditLog"` + 3 index ไม่แตะตารางเดิมสักตาราง ไม่มี backfill
  (log ย้อนหลังไม่มีอยู่จริง สร้างไม่ได้)
- rollback = drop ตารางเดียว ไม่มีผลข้างเคียงกับข้อมูลเดิม
- ✅ **C1 ปลดล็อกแล้ว 2026-08-28** — `dotnet ef database update` รันได้ แต่ต้อง rehearsal บน
  staging ก่อนเสมอ (ดูบริบทคิว migration ค้างด้านล่าง) และห้ามรันตรงกับ production โดยไม่ผ่าน `devops`
- ⚠️ **บริบทที่ต้องรู้ก่อน apply:** โปรเจกต์นี้ยังมี migration ค้างที่ไม่เคย apply กับ PostgreSQL จริง
  (`20260813140603_SplitLinkAndAddAuth` — `CLAUDE.md` §Known Baseline Issues) · migration ของ
  โมดูลนี้จะต่อท้ายคิวนั้น ไม่ใช่รันเดี่ยวๆ — `devops` ต้องนับรวมตอน rehearsal บน staging

---

## Modules

### Module A — ตาราง `AuditLog` + `SaveChanges` interceptor 🔒 Security gate

**ครอบคลุม:** DM-A1, DM-A2, DM-A3, DM-A4, MG-A1, สัญญา AU-1..AU-20 ทั้งหมด, blast radius
`DesignTimeDbContextFactory.cs` + `CompanyIsolationTests.cs` + เอกสาร schema ทั้ง 5 ไฟล์

**Entity/ไฟล์ใหม่:** `AuditLog.cs`, `AuditAction.cs`, migration `AddAuditLog`

**ขึ้นกับ:** ไม่ขึ้นกับโมดูลอื่น — ✅ ไฟเขียว migration ปลดล็อกแล้ว 2026-08-28

**🔒 Security gate — ระบุความกังวลให้ชัด (ไม่ใช่คำว่า "sensitive" ลอยๆ):**
1. **เก็บ identity ของบุคคลจริง** (`ActorUserId`) ผูกกับบริษัทและเวลา — เป็น personal data
   ที่ใช้ระบุตัวคนได้ ในตารางที่ไม่มีวันถูกลบ (R4)
2. **สร้างตารางที่ไม่มี company query filter** ในระบบ multi-tenant ที่ทุกตารางอื่นมี — เป็นพื้นผิว
   รั่วข้ามบริษัททันทีที่มีทางอ่านเกิดขึ้น (AR-1)
3. **แตะ `SaveChanges` ของทั้งระบบ** — จุดที่ทุกการเขียนข้อมูลของทุกโมดูลไหลผ่าน ความผิดพลาด
   ที่นี่กระทบทุกฟีเจอร์พร้อมกัน ไม่ใช่แค่โมดูลนี้ · `security` ควรตรวจโดยเฉพาะว่า AU-15
   (ไม่กลืน exception) ไม่ได้กลายเป็นช่องทำให้ระบบล่มทั้งระบบจาก log

### Module B — เขียน log ด้วยมือที่ 5 จุด raw SQL 🔒 Security gate

**ครอบคลุม:** สัญญา RS-0..RS-8 ทั้งหมด, DM-A5 (signature), blast radius `ServiceTestFakes.cs` +
`ILessonConfigService.cs:502` + บรรทัด RS-8 ใน `CLAUDE.md`

**ขึ้นกับ: Module A ต้องเสร็จก่อน** — ทั้งตาราง `AuditLog` และ AU-4 (ที่กันการนับซ้ำ) ต้องมีอยู่
ก่อนจุดเหล่านี้จะเขียนอะไรได้

**🔒 Security gate:** เป็นเส้นทาง archive/restore/permanent-delete ของบทเรียน — การกระทำที่ทำลาย
ข้อมูลได้จริงและเป็นสิ่งที่ "อยากรู้ว่าใครทำ" มากที่สุดตามปัญหา P1 · ถ้าจุดใดจุดหนึ่งใน 5 จุดหลุด
ช่องว่างจะเงียบสนิท (ไม่มี error ไม่มีอะไรเตือน) และตรงกับการกระทำที่ผู้ไม่หวังดีจะเลือกใช้พอดี ·
`security` ต้องตรวจครบทั้ง 5 จุดทีละจุดเทียบกับตาราง RS-1..RS-5 ไม่ใช่ตรวจตัวอย่าง

### ทำไมแบ่งเป็น 2 โมดูล ไม่ใช่ 1 หรือ 3

ตัดสินอย่างชัดเจน ไม่ได้แบ่งโดยอัตโนมัติ: **A ปิดจบเป็นก้อนเดียวได้และ verify ได้ด้วยตัวเอง**
(ครอบคลุมทางเขียนปกติเกือบทั้งหมดของระบบ) ส่วน **B เป็นการไล่แก้ทีละจุดที่พลาดง่ายคนละแบบ**
และต้องแก้ signature/fake/call site ตาม · รวมเป็นก้อนเดียวจะได้ phase ที่ปนสองลักษณะงานและ
QA แยกไม่ออกว่าอะไรพัง · แบ่งเป็น 3 (แยก migration ออกมา) ไม่คุ้ม เพราะ migration กับ entity
แยกส่งมอบจากกันไม่ได้อยู่แล้ว

**ทั้งสองเป็น Module ภายในโฟลเดอร์ `audit-trail` เดียวกัน ไม่ใช่โฟลเดอร์ใหม่** — ตอบคำถามธุรกิจ
ข้อเดียวกัน ("ย้อนหลังได้ไหมว่าใครทำอะไร") และไม่มีทางที่ B จะถูกยกเลิกโดย A ยังมีความหมายครบ

---

## Risks & Dependencies

| # | ความเสี่ยง | ผล / สิ่งที่ต้องทำ |
|---|---|---|
| **AR-1** | **`AuditLog` ไม่มี company query filter** (มติ OQ-2) — ต่างจาก 14 ตารางที่มี | รอบนี้ไม่มีทางอ่านจึงยังไม่รั่ว **แต่ R3 จะเป็นจริงก็ต่อเมื่อทางอ่านในอนาคต filter เอง** · ตอนออกแบบ dashboard ต้องถือว่านี่คือข้อบังคับข้อแรก ไม่ใช่รายละเอียด · รูปแบบเดียวกับ `BackgroundJob`/SEC-2 ที่โมดูล `knowledge-base` เคยเจอมาแล้ว |
| **AR-2** | **AU-15 ขัดกับ convention "degrade แทน throw" ของ `CLAUDE.md`** | เป็นข้อยกเว้นที่เจ้าของเคาะแล้ว (Q-A3) · engineer ที่ทำตาม convention โดยไม่อ่าน AU-15 จะครอบ `try/catch` แล้วกลืน log หายเงียบ · `qa-engineer` ต้องตรวจข้อนี้เป็นรายการเฉพาะ ไม่ใช่ปล่อยผ่าน |
| **AR-3** | **ไม่มี diff → log มีเสียงรบกวน** · `_set.Update(entity)` สร้างแถวแม้ค่าไม่เปลี่ยนสักฟิลด์ เพราะไม่มีอะไรให้เทียบ | ยอมรับแล้วตาม R2/C5 · ระบุไว้ที่ AU-13 เพื่อไม่ให้ใครไป "ปรับให้ฉลาดขึ้น" ด้วยการเทียบค่า ซึ่งคือ diff ที่ R2 ตัดไปแล้วในรูปแบบอื่น |
| **AR-4** | **ความเสี่ยงระยะยาวข้อใหญ่ที่สุด: `ExecuteUpdate`/`ExecuteSqlRaw` จุดที่ 11 ในอนาคตจะหลุด log เงียบๆ** — ไม่มี compiler ไม่มี test ไม่มีอะไรเตือน | 5 จุดวันนี้ปิดได้ครบ แต่กันจุดถัดไปไม่ได้ด้วยโค้ด · มาตรการเดียวที่ทำได้คือ **RS-8 ต้องถูกเขียนลง `CLAUDE.md` จริงๆ** ตอน implement Module B ไม่ใช่อยู่แค่ในเอกสารนี้ |
| **AR-5** | **ปริมาณ log จริงอาจสูงกว่าที่ประเมิน** · มติ Q-A4 ยืนยัน "ทุกแถวจริงๆ" รวม entity ปริมาณสูง — บทเรียน PDF 60 หน้าที่ commit narration จะสร้าง log ระดับ **หลายสิบแถวจากการกดปุ่มครั้งเดียว** และการตัด/เอาหน้ากลับ (`LessonExcludedSlide`) ก็เช่นกัน | ตัวเลข "ปริมาณต่ำ" ใน `requirement.md` §References ยัง **เป็นสมมติฐานที่ไม่เคยวัด** · R4 (ไม่มี retention) ยังคงเป็นมติ **แต่ `requirement.md` C6 บอกไว้เองว่าถ้าขอบเขตขยายต้องกลับมาทบทวน R4** — ข้อเสนอ: หลัง Module A ขึ้น production ~1 เดือน ให้วัดจำนวนแถวจริงแล้วค่อยตัดสินใจเรื่อง retention ด้วยตัวเลข ไม่ใช่ด้วยการคาดเดา |
| **AR-6** | **`ApplicationDbContext` ctor เปลี่ยน** กระทบทุกจุดที่ `new` เอง | 2 จุดวันนี้ ระบุครบใน Blast Radius · compiler จับได้ทั้งหมด ไม่มีความเสี่ยงแบบเงียบ |
| **AR-7** | **การลบจริงตอน `lesson_purge` ไม่มี log** (AU-19 — ผลโดยตรงของมติ OQ-3) | เป็นผลที่ตั้งใจ ไม่ใช่บั๊ก · แต่ต้องรู้ล่วงหน้า: ถ้าวันหนึ่งมีคนถามว่า "ข้อมูลบทเรียนนี้หายไปตอนไหน" log จะตอบได้แค่ว่าใครสั่ง archive/เร่งลบ ไม่ตอบว่าการลบจริงเกิดเมื่อไหร่ |
| **AR-8** | **ขึ้นกับ migration ค้างที่ยังไม่เคย apply** (`SplitLinkAndAddAuth`) | migration ของโมดูลนี้ต่อท้ายคิวนั้น — `devops` ต้อง rehearsal ทั้งคิวบน staging ไม่ใช่แค่ตัวใหม่ |
| **AR-9** | **ไม่มี test suite ที่พิสูจน์ AU-15 ได้** — EF InMemory ไม่มี transaction จริง | AU-20 สั่งให้บันทึกเป็น unverified behaviour แทนการแกล้งว่าทดสอบแล้ว · `qa-engineer` ต้องยกข้อนี้ขึ้น `## Unverified Behaviour` และ `devops` ต้องเห็นก่อน deploy |
| **AR-10** | **แถว log ของ `AdminUser` เป็นระดับระบบทั้งหมด** (มติ OQ-A1, 2026-08-28) — `CompanyId = null` แม้เป็นการแก้/ปิดบัญชีพนักงานที่สังกัดบริษัทเดียวชัดเจน | เป็น trade-off ที่เจ้าของโปรเจกต์เคาะแล้ว **ไม่ใช่บั๊กและไม่ใช่ช่องว่างที่ลืม** · **ผลที่ต้องรู้ตอนออกแบบ dashboard (อ่านคู่กับ AR-1 ที่บอกว่าทางอ่านต้อง filter เอง และมติ OQ-5 ที่ให้เฉพาะ `owner`/`admin` เห็น): คิวรีของ `admin` ระดับบริษัทที่ filter `CompanyId == companyId` จะไม่เห็นการกระทำกับบัญชีผู้ใช้เลยแม้แต่แถวเดียว — เห็นได้เฉพาะ `owner` ที่มองแถวระดับระบบ** · ถ้าธุรกิจต้องการให้ `admin` เห็น = กลับมติ ต้องผ่าน `system-analyst` ห้ามแก้ AU-11 ตรงๆ |

---

## Unresolved Open Questions

**สถานะรวม (2026-08-28): ไม่มีข้อใดรอคำตอบจากเจ้าของโปรเจกต์อีกแล้ว** — ทุกแถวข้างล่างคือข้อที่
**ปิดแล้ว** หรือ **เลื่อนโดยตั้งใจ** (มีต้นทุนศูนย์ที่จะทำทีหลัง เพราะ R5/additive) ไม่มีข้อใดบล็อก
`project-manager` หรือ engineer · **ไฟเขียว migration (C1) ปลดล็อกแล้ว 2026-08-28 เช่นกัน —
ไม่มีตัวบล็อกใดเหลืออยู่ในโมดูลนี้แล้ว**

| # | คำถาม | สถานะ | บล็อกอะไร |
|---|---|---|---|
| **OQ-A1** | **`CompanyId` ของแถว log ที่ entity เป็น `AdminUser`** (รวมกรณี `admin` แก้ไข/ปิดบัญชีพนักงานคนอื่นในบริษัทตัวเอง) — `AdminUser` เป็น entity เดียวเป๊ะที่ตกมาที่ "ที่เหลือ → null" ของมติ OQ-2 ทั้งที่ตัวมันมีคอลัมน์ `CompanyId` ที่ไม่ null สำหรับ `admin`/`cs` | **✅ ปิดแล้ว 2026-08-28: ทางเลือก ก — `null` เสมอ (ระดับระบบ)** ยืนยันตามคำตอบเดิมของ OQ-2 ไม่เปลี่ยนเป็นทางเลือก ข · **AU-11 ที่เป็น default อยู่แล้วถูกต้อง ไม่ต้องแก้** · เหตุผลและ trade-off เต็มอยู่ที่ตารางมติ §"การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" และ **AR-10** | ไม่บล็อกอะไรแล้ว — เงื่อนไขเดิม "ควรเคาะก่อน implement Module A" **ปลดแล้ว** |
| **OQ-A2** | ควรมี action `"restore"` แยกจาก `"update"` ไหม (`IsDelete` true→false) | **เลื่อนโดยตั้งใจ** — AU-8 ตั้ง default เป็น `"update"` รอบนี้ · R5 ทำให้เพิ่มค่าใหม่ทีหลังได้โดยไม่ต้อง migration ไม่มีต้นทุนจม | ไม่บล็อก |
| **OQ-A3** | ควรเก็บ `ActorRole` snapshot (role ตอนที่ลงมือ) ไหม — role ของคนเปลี่ยนได้ การ join `AdminUser` ทีหลังจะได้ role ปัจจุบัน ไม่ใช่ role ตอนทำ | **เลื่อนโดยตั้งใจ** — R2 บอกว่าหนึ่งแถวตอบสี่อย่าง การเพิ่มคอลัมน์ nullable ทีหลังเป็น additive ล้วน | ไม่บล็อก |
| **OQ-4** (จาก `requirement.md`) | login / logout / login ล้มเหลว | **ปิดแล้ว: ไม่รวมในรอบนี้** เป็นงานอนาคต · **หมายเหตุสำคัญ: schema ปัจจุบันรองรับไม่ได้ตรงๆ** — `EntityId` เป็น required และ login ไม่ผูกกับแถวข้อมูลใด งานอนาคตข้อนี้จึงต้องผ่าน `system-analyst` ใหม่ ไม่ใช่แค่เพิ่มค่า `Action` | ไม่บล็อก |
| **OQ-5** (จาก `requirement.md`) | `cs` เห็น log ไหม | **ปิดแล้ว: ไม่เห็น เฉพาะ `owner`/`admin`** · บันทึกไว้ที่นี่เป็นข้อบังคับสำหรับตอนออกแบบ dashboard · ⚠️ `requirement.md` §Target Users บรรทัด `cs` อ้าง "ดู OQ-4" ซึ่งเป็นเลขผิด (ที่ถูกคือ OQ-5) — ให้ `business-analyst` แก้ตอน amend ครั้งถัดไป ผมไม่แก้ `requirement.md` เอง | ไม่บล็อก |
| **OQ-A4** | **retention จะถูกทบทวนเมื่อไหร่** — R4 บอก "ไม่มีเลย" บนสมมติฐานปริมาณต่ำที่ยังไม่เคยวัด ขณะที่มติ Q-A4 ยืนยันขอบเขตที่กว้างที่สุด | **เลื่อนโดยตั้งใจ** พร้อมข้อเสนอที่ **AR-5**: วัดจำนวนแถวจริงหลังขึ้น production ~1 เดือนแล้วค่อยตัดสินด้วยตัวเลข | ไม่บล็อก |

**สิ่งที่ตัดออกจากรอบนี้ชัดเจน — ห้ามทำโดยไม่ amend เอกสารนี้ก่อน:** dashboard/UI ทุกชนิด ·
endpoint/API สำหรับอ่าน log · repository/service/DTO/ViewModel ของ `AuditLog` (AU-17) ·
before/after diff และ `MetadataJson` · retention job · การแจ้งเตือนเชิงรุก · การบันทึกกิจกรรม
ที่ผู้เรียนก่อ (R1.2) · การบันทึก action ที่ระบบก่อเอง (OQ-3) · การแตะคอลัมน์ audit เดิมหรือ
`IEntityMaster`

---

## Change Log

- **2026-08-28** — **ไฟเขียว C1 ปลดล็อกแล้ว** เจ้าของโปรเจกต์ยืนยันเองในแชทว่าให้ migrate โมดูลนี้ได้
  ("ให้ไฟเขียวตอนนี้เลย") — แก้กล่องสถานะหัวไฟล์, `## ⛔ เงื่อนไขก่อนลงมือ` → `## ✅`, บรรทัด MG-A1
  ที่ห้าม `database update`, `Module A` §ขึ้นกับ, และสรุปรวมใน `## Unresolved Open Questions` ให้
  สะท้อนว่าไม่มีตัวบล็อกเหลือแล้ว · ไม่มีการแก้ Data Model/สัญญา/Migration Plan เอง แค่ปลดเงื่อนไข
  การ dispatch · `🔒 Security gate` ทั้งสอง Module ยังคงอยู่ ไม่ถูกปลดไปพร้อมกัน
- **2026-08-28** — **ปิด OQ-A1 ตามมติเจ้าของโปรเจกต์: ทางเลือก ก — `CompanyId` ของแถว log ที่
  entity เป็น `AdminUser` เป็น `null` เสมอ (ระดับระบบ)** ยืนยันตามคำตอบเดิมของ OQ-2 ไม่รับทางเลือก ข
  ที่ `system-analyst` เสนอ · **ไม่มีการแก้ Data Model / สัญญา AU-* / RS-* / Blast Radius /
  Migration Plan / การแบ่ง Module แม้จุดเดียว** เพราะ AU-11 เขียน default นี้ไว้ถูกต้องอยู่แล้ว
  (ไม่ใช่ additive และไม่ใช่ breaking — ไม่แตะ schema เลย ไม่มี migration) · แก้ 6 จุดที่เป็น
  bookkeeping ล้วน: กล่องสถานะหัวไฟล์ · แถว R3/R3.1 ใน Feature-by-Feature · **เพิ่มแถว OQ-A1
  ในตารางมติ** · AU-11 (เปลี่ยนจาก "ดู OQ-A1 ซึ่งยังเปิดอยู่" เป็นมติที่บังคับใช้ + ข้อห้ามอ่าน
  `AdminUser.CompanyId`) · **เพิ่ม AR-10** (บันทึก trade-off ที่ยอมรับแล้ว: dashboard ในอนาคตของ
  `admin` ระดับบริษัทจะไม่เห็นการกระทำกับบัญชีผู้ใช้ เห็นได้เฉพาะ `owner`) · `## Unresolved Open
  Questions` (OQ-A1 → ปิดแล้ว + หัวข้อสรุปว่าไม่มีข้อรอคำตอบเหลือ) · **ผลรวม: ไม่มีคำถามค้าง
  ในโมดูลนี้แล้ว — OQ-A2/OQ-A3/OQ-A4 เป็นข้อที่เลื่อนโดยตั้งใจ ไม่ใช่ข้อที่รอคำตอบ** ·
  **เงื่อนไข C1 (ไฟเขียว migration) ยังไม่ถูกปลดโดยรอบนี้**
- **2026-08-27** — สร้าง `design.md` ครั้งแรก · ปิดมติ 8 ข้อกับเจ้าของโปรเจกต์ (OQ-2 nullable
  ไม่มี query filter · OQ-3 ไม่บันทึก action ของระบบ · OQ-4 ตัด login ออกจากรอบนี้ · Q-A1
  ไม่ implement `IEntityMaster` · Q-A2 ไม่มี `MetadataJson` · Q-A3 log ล้ม = business write
  ล้มตาม · OQ-5 `cs` ไม่เห็น log · Q-A4 ยืนยันขอบเขตทุกแถวจริง) · Data Model **DM-A1..DM-A5**
  (1 ตารางใหม่ `AuditLog` 7 ฟิลด์ · `AuditAction` constants · index 3 ตัว · ctor ของ
  `ApplicationDbContext` · signature ของ `AccelerateLessonPurge`) — **additive ล้วน ไม่มี breaking
  change ต่อข้อมูล** · สัญญา **AU-1..AU-20** (interceptor ผ่าน `SaveChanges` override) และ
  **RS-0..RS-8** (5 จุด raw SQL ที่ต้องเขียนด้วยมือ + 3 จุดที่จงใจไม่เขียน + 1 จุดที่ยังไม่กำหนด) ·
  ความเสี่ยง **AR-1..AR-9** · แบ่งเป็น **Module A / Module B** ทั้งคู่ติด 🔒 Security gate ·
  เปิด **OQ-A1** (CompanyId ของ `AdminUser` — ไม่บล็อก schema) และบันทึก OQ-A2/OQ-A3/OQ-A4
  เป็นข้อที่เลื่อนโดยตั้งใจ · ยืนยันจากโค้ดจริงว่า `TryRestore`/`CancelPendingLessonPurge`
  ไม่มี caller ใน production และรูปแบบ worker ของ `lesson_purge` มีอยู่จริง (ปิดข้อสงสัยที่
  `requirement.md` §References ฝากไว้) · **เงื่อนไข C1 (ไฟเขียว migration) ยังไม่ถูกปลด**
