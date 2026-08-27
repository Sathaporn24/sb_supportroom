using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Application.Tests;

/// <summary>
/// design.md (audit-trail, Module A) สัญญา AU-1..AU-20 - เฉพาะกติกาที่ EF InMemory พิสูจน์ได้จริง
/// (AU-2, AU-4, AU-5, AU-6, AU-9) ตามที่ AU-20/Sequencing Notes ระบุ · AU-15 (atomic rollback) ทดสอบ
/// ด้วย InMemory ไม่ได้เพราะไม่มี transaction จริง เป็น unverified behaviour ที่ qa-engineer ต้อง
/// บันทึกไว้แทนการแกล้งว่าทดสอบแล้ว
///
/// รูปแบบเดียวกับ CompanyIsolationTests - ต้องใช้ ApplicationDbContext จริง ไม่ใช่ fake list เพราะ
/// interceptor ต่อกับ ChangeTracker.Entries() โดยตรง
/// </summary>
public class AuditLogInterceptorTests : IDisposable
{
    private const string CompanyA = "company-a";
    private const string ActorUserId = "admin-user-1";

    private readonly CompanyContext _companyContext = new();
    private readonly CurrentUser _currentUser = new();
    private readonly ApplicationDbContext _db;

    public AuditLogInterceptorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"audit-log-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(options, _companyContext, _currentUser);
        _companyContext.Resolve(CompanyA);
    }

    public void Dispose() => _db.Dispose();

    private static TrainingLink Link(string id, string companyId, string token) => new()
    {
        Id = id,
        CompanyId = companyId,
        Token = token,
        LessonId = "lesson-1",
        LessonSlug = "lesson-1-slug",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        CreateDate = DateTime.UtcNow,
    };

    private static LessonConfig LessonWithSlides(string id, string companyId, int slideCount) => new()
    {
        Id = id,
        CompanyId = companyId,
        Slug = $"{id}-slug",
        CategoryId = "kbcat-child",
        Title = "บทเรียนทดสอบ",
        SlidesSourceUrl = "",
        ContentSourceType = "google_slides",
        SlideConfigs = Enumerable.Range(0, slideCount)
            .Select(i => new SlideConfig { SlideObjectId = $"slide-{i}", SlideIndex = i })
            .ToList(),
        IsActive = true,
        CreateDate = DateTime.UtcNow,
    };

    [Fact]
    public void AU2_NoActorResolved_SaveChangesCreatesNoAuditLogRowEvenThoughAnEntityChanged()
    {
        // currentUser ไม่ resolve เลยตลอดเทสต์นี้ - จำลอง request ฝั่งผู้เรียน/worker
        _db.TrainingLink.Add(Link("link-1", CompanyA, "token-1"));

        _db.SaveChanges();

        Assert.Empty(_db.AuditLog.ToList());
    }

    [Fact]
    public void AU4_NormalSaveChangesWithAnActorDoesNotCreateDuplicateAuditLogRows()
    {
        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);
        _db.TrainingLink.Add(Link("link-1", CompanyA, "token-1"));

        _db.SaveChanges();

        // ถ้า interceptor นับซ้ำแถว AuditLog ที่มันเพิ่งสร้างเอง (ไม่ข้าม `entry.Entity is AuditLog`)
        // จำนวนแถวจะเพิ่มไม่รู้จบ - หนึ่งแถวของ TrainingLink ต้องได้แค่หนึ่งแถว AuditLog เท่านั้น
        var rows = _db.AuditLog.ToList();
        var row = Assert.Single(rows);
        Assert.Equal(nameof(TrainingLink), row.EntityName);
        Assert.Equal("link-1", row.EntityId);
        Assert.Equal(AuditAction.Create, row.Action);
    }

    [Fact]
    public void AU5_SavingALessonWithSeveralOwnedSlideConfigsProducesOneAuditLogRowNotNPlusOne()
    {
        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);
        _db.LessonConfig.Add(LessonWithSlides("lesson-1", CompanyA, slideCount: 5));

        _db.SaveChanges();

        var rows = _db.AuditLog.Where(x => x.EntityName == nameof(LessonConfig)).ToList();
        var row = Assert.Single(rows);
        Assert.Equal("lesson-1", row.EntityId);
        Assert.Equal(AuditAction.Create, row.Action);
    }

    [Fact]
    public void AU6_AddedEntityIsRecordedAsCreate()
    {
        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);

        _db.TrainingLink.Add(Link("link-1", CompanyA, "token-1"));
        _db.SaveChanges();

        var row = Assert.Single(_db.AuditLog.ToList());
        Assert.Equal(AuditAction.Create, row.Action);
    }

    [Fact]
    public void AU6_ModifyingAFieldThatIsNotIsDeleteIsRecordedAsUpdate()
    {
        _db.TrainingLink.Add(Link("link-1", CompanyA, "token-1"));
        _db.SaveChanges();

        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);
        var tracked = _db.TrainingLink.Single(x => x.Id == "link-1");
        tracked.UpdateBy = ActorUserId;
        tracked.UpdateDate = DateTime.UtcNow;
        _db.SaveChanges();

        var row = Assert.Single(_db.AuditLog.Where(x => x.EntityName == nameof(TrainingLink)).ToList());
        Assert.Equal(AuditAction.Update, row.Action);
    }

    [Fact]
    public void AU6_SoftDeletingATrackedRowIsRecordedAsDeleteNotUpdate()
    {
        _db.LessonConfig.Add(LessonWithSlides("lesson-1", CompanyA, slideCount: 0));
        _db.SaveChanges();

        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);
        // AU-7: ต้องอ่านแถวที่ track อยู่มาแก้ ไม่ใช่ Update() บน instance ที่ detached - เหมือนกับ
        // เส้นทาง soft-delete จริงในโค้ดวันนี้ (Get()/GetIncludingDeleted())
        var tracked = _db.LessonConfig.IgnoreQueryFilters().Single(x => x.Id == "lesson-1");
        tracked.IsDelete = true;
        tracked.DeletedAt = DateTime.UtcNow;
        tracked.DeleteBy = ActorUserId;
        _db.SaveChanges();

        var row = Assert.Single(_db.AuditLog.Where(x => x.EntityName == nameof(LessonConfig)).ToList());
        Assert.Equal(AuditAction.Delete, row.Action);
    }

    [Fact]
    public void AU9_MultipleEntitiesChangedInOneSaveChangesShareTheExactSameOccurredAt()
    {
        _currentUser.Resolve(ActorUserId, AdminRole.Admin, CompanyA);
        _db.TrainingLink.Add(Link("link-1", CompanyA, "token-1"));
        _db.LessonConfig.Add(LessonWithSlides("lesson-1", CompanyA, slideCount: 2));

        _db.SaveChanges();

        var rows = _db.AuditLog.ToList();
        Assert.Equal(2, rows.Count);
        var occurredAts = rows.Select(x => x.OccurredAt).Distinct().ToList();
        Assert.Single(occurredAts);
    }
}
