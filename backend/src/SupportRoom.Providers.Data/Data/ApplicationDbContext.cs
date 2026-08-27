using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SupportRoom.Domain;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Providers.Data.Data;

/// <summary>
/// Every ICompanyScoped entity carries a company query filter (see OnModelCreating). The filter
/// reads companyContext at query time rather than model-build time, so one DbContext instance
/// still sees the right company once a service resolves it mid-request (the recipient-side flow
/// resolves from a session token, which can only happen after the first query).
///
/// The filter compares against a nullable CompanyId on purpose: an unresolved context matches
/// zero rows. Forgetting to resolve therefore surfaces as empty results, never as another
/// company's data.
///
/// ⚠️ TWO entities sit outside that safety net - Company and AdminUser (see below). They are the
/// substrate authentication is built from, and both are consulted BEFORE a company is known, so a
/// filter on them would match zero rows and nothing would work. For those two, IAuthorizationGuard
/// is the only protection, which is why its rules are covered by tests directly (TD-014).
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICompanyContext companyContext,
    ICurrentUser currentUser) : DbContext(options)
{
    /// <summary>No query filter - see the ⚠️ note on this class and on the entity itself.</summary>
    public DbSet<Company> Company => Set<Company>();

    /// <summary>No query filter - see the ⚠️ note on this class and on the entity itself.</summary>
    public DbSet<AdminUser> AdminUser => Set<AdminUser>();

    public DbSet<TrainingLink> TrainingLink => Set<TrainingLink>();
    public DbSet<LearningSession> LearningSession => Set<LearningSession>();
    public DbSet<SessionQuestion> SessionQuestion => Set<SessionQuestion>();
    public DbSet<LessonConfig> LessonConfig => Set<LessonConfig>();
    public DbSet<DocumentResource> DocumentResource => Set<DocumentResource>();
    public DbSet<KnowledgeCategory> KnowledgeCategory => Set<KnowledgeCategory>();
    public DbSet<BackgroundJob> BackgroundJob => Set<BackgroundJob>();
    public DbSet<DocumentChunk> DocumentChunk => Set<DocumentChunk>();
    public DbSet<LessonSlideNarration> LessonSlideNarration => Set<LessonSlideNarration>();
    public DbSet<LessonExcludedSlide> LessonExcludedSlide => Set<LessonExcludedSlide>();
    public DbSet<KnowledgeQnA> KnowledgeQnA => Set<KnowledgeQnA>();
    public DbSet<KnowledgeQnASource> KnowledgeQnASource => Set<KnowledgeQnASource>();
    public DbSet<KnowledgeQnAConflict> KnowledgeQnAConflict => Set<KnowledgeQnAConflict>();
    public DbSet<SessionQuestionReviewExclusion> SessionQuestionReviewExclusion => Set<SessionQuestionReviewExclusion>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Company and AdminUser get NO HasQueryFilter, deliberately:
        //   Company  - it IS the tenant registry; filtering it would leave the company switcher
        //              with nothing to list.
        //   AdminUser- sign-in finds a user by email before any company is known, and an owner's
        //              CompanyId is null, which `CompanyId == context` can never match.
        // Their scoping lives in IAuthorizationGuard instead. Do not "fix" this by adding a
        // filter - it would break login, not tighten security.
        builder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Unique across the system, not per company: sign-in supplies only an email, so the
            // same address under two companies would make the account ambiguous.
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.CompanyId);
        });

        builder.Entity<TrainingLink>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Token stays globally unique - it is the public join secret and is looked up before
            // any company is known (GetByToken bypasses the filter), so it must not collide
            // across companies.
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<LearningSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Not unique: pressing "เรียนอีกครั้ง" creates another round under the same key, so
            // the same (link, learner) pair legitimately has several rows.
            entity.HasIndex(x => new { x.TrainingLinkId, x.LearnerKey });
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<SessionQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.CompanyId);
            // R-9 - these two are the only change Phase 6 (knowledge-base) makes to this entity,
            // which belongs to the learning-session module: the review queue (QQ-1/P8/R5.1) filters
            // on exactly these two columns, across every company's questions, on every page load.
            entity.HasIndex(x => new { x.CompanyId, x.AnswerStatus });
            entity.HasIndex(x => new { x.CompanyId, x.ReviewResult });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<LessonConfig>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Slug is unique PER COMPANY, not globally - two companies must both be able to own
            // a lesson called "getting-started". A globally unique slug would make the second
            // company onboarded unable to use the obvious names.
            entity.HasIndex(x => new { x.CompanyId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.CategoryId);
            // R9/MG-L1 - the normal list and the trash tab's list are the same shape of query
            // (company + trash flag), and the purge worker/preflight scan by DeletedAt too.
            entity.HasIndex(x => new { x.CompanyId, x.IsDelete, x.DeletedAt });
            entity.OwnsMany(x => x.SlideConfigs, owned => owned.ToJson());
            // R9 - a trashed lesson must disappear from every normal list/get/save the moment it
            // is archived (LT-7). Reading the trash tab bypasses this via IgnoreQueryFilters() in
            // ILessonConfigRepository's trash-specific methods only (LT-23).
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<DocumentResource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.ScopeType, x.ScopeId });
            // R7.5/KL-19 - the duplicate-detection query hits this on every CS upload, not a spare
            // index. Not unique: "duplicate" is a warning, not a constraint (KL-21).
            entity.HasIndex(x => new { x.CompanyId, x.ContentHash });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<KnowledgeCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CompanyId);
            entity.HasIndex(x => new { x.CompanyId, x.ParentId, x.SortOrder });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<BackgroundJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            // The worker's polling query hits this on every claim attempt - not a spare index.
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt });
            entity.HasIndex(x => new { x.CompanyId, x.JobType, x.TargetId });
            // ⚠️ NO HasQueryFilter here, deliberately - this is the one entity in the project a
            // background worker reads before any company is known: the worker picks the next
            // ready job across every company (ClaimNext/RequeueOrphanedRunning both
            // IgnoreQueryFilters), then resolves ICompanyContext FROM the row it just claimed
            // (design.md DI-4), the same "credential resolves the context" shape as
            // TrainingLink.GetByToken. A filter here would make every claim query match zero
            // rows and no job would ever run. Any code reading this table from a normal request
            // scope (where CompanyId is already known) MUST filter by CompanyId itself - see
            // design.md Module C security gate note (SEC-2).
        });

        builder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(x => x.Id);
            // หน้าดูข้อความที่แปลงได้ (DI-7) เรียงตาม SeqNo ต่อเอกสารเดียว - นี่คือคิวรีนั้น
            entity.HasIndex(x => new { x.DocumentId, x.SeqNo });
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<LessonSlideNarration>(entity =>
        {
            entity.HasKey(x => x.Id);
            // ไม่ IsUnique: soft delete ทำให้แถวที่ถูกลบยังกินคีย์อยู่ กติกา "หน้าละหนึ่งแถว"
            // บังคับที่ service layer (NR-2) ด้วยเหตุผลเดียวกับ TX-3
            entity.HasIndex(x => new { x.LessonId, x.SlideObjectId });
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<LessonExcludedSlide>(entity =>
        {
            entity.HasKey(x => x.Id);
            // ไม่ IsUnique: soft delete ทำให้แถวที่ถูกลบยังกินคีย์อยู่ กติกา "หน้าละหนึ่งแถว"
            // บังคับที่ service layer (EX-4) ด้วยเหตุผลเดียวกับ LessonSlideNarration/TX-3
            entity.HasIndex(x => new { x.LessonId, x.SlideObjectId });
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<KnowledgeQnA>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.ScopeType, x.ScopeId });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<KnowledgeQnASource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.QnAId);
            // The queue checks "does this question already have a Q&A" for every row on the page,
            // in one batched query (design.md DM-16) - this index is that query's shape.
            entity.HasIndex(x => new { x.CompanyId, x.SessionQuestionId });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<KnowledgeQnAConflict>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.QnAId);
            entity.HasIndex(x => new { x.CompanyId, x.ResolvedAt });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

        builder.Entity<SessionQuestionReviewExclusion>(entity =>
        {
            entity.HasKey(x => x.Id);
            // One question can only ever be permanently excluded once - this is also what makes
            // ISessionQuestionReviewExclusionRepository.AddMissingForLesson idempotent on retry
            // (LT-16): a re-run of the same insert just hits this constraint and is a no-op.
            entity.HasIndex(x => new { x.CompanyId, x.SessionQuestionId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.LessonId });
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
        });

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
    }

    /// <summary>
    /// สัญญา AU-1..AU-16 (design.md, Module A) - จุดเดียวที่สร้างแถว AuditLog ทั้งหมด เรียกจากทั้ง
    /// SaveChanges(bool) และ SaveChangesAsync(bool, CancellationToken) เท่านั้น ห้ามมี logic ซ้ำสองชุด
    /// (AU-1) · ห้ามครอบ try/catch กลืน exception ที่นี่หรือที่เรียก (AU-15, มติ Q-A3) - log เขียน
    /// ไม่ได้ต้องทำให้ business write ล้มตามทั้ง transaction
    /// </summary>
    private List<AuditLog> BuildAuditLogRows()
    {
        // AU-2: ไม่มีคน = ไม่มีแถว ไม่มีข้อยกเว้น - ครอบคลุม R1.2 (ผู้เรียน), OQ-3 (worker/job),
        // OQ-4 (login) พร้อมกันโดยไม่ต้องเขียนเงื่อนไขเพิ่ม
        if (string.IsNullOrEmpty(currentUser.UserId))
        {
            return [];
        }

        // AU-3: .ToList() ทันที ก่อนสร้างแถวใดๆ - กัน Entries() ที่ lazy โดนแก้ระหว่างวนอยู่
        var entries = ChangeTracker.Entries().ToList();
        // AU-9: DateTime.UtcNow ครั้งเดียวต่อการเรียก SaveChanges หนึ่งครั้ง ใช้ค่าเดียวกันทุกแถว
        var occurredAt = DateTime.UtcNow;
        var rows = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // AU-4: กันวนซ้ำไม่รู้จบ
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            // AU-5: owned type (วันนี้คือ LessonConfig.SlideConfigs, OwnsMany().ToJson()) ไม่มี PK
            // ของตัวเอง - นับที่ entity เจ้าของแทน ไม่ใช่นับแยกต่อรายการ
            if (entry.Metadata.IsOwned())
            {
                continue;
            }

            string action;
            if (entry.State == EntityState.Added)
            {
                action = AuditAction.Create;
            }
            else if (entry.State == EntityState.Deleted)
            {
                action = AuditAction.Delete;
            }
            else if (entry.State == EntityState.Modified)
            {
                // AU-6/AU-7/AU-8: soft-delete (IsDelete false->true) = delete · true->false
                // (กู้คืน) และการแก้ฟิลด์อื่นๆ = update ตาม default ของ AU-8
                action = IsSoftDelete(entry) ? AuditAction.Delete : AuditAction.Update;
            }
            else
            {
                continue;
            }

            // AU-13: หนึ่งแถวต่อหนึ่ง entity ต่อหนึ่ง SaveChanges ไม่ใช่ต่อ property ที่เปลี่ยน
            rows.Add(new AuditLog
            {
                Id = IdGenerator.GenerateId("audit"),
                CompanyId = ResolveCompanyId(entry),
                ActorUserId = currentUser.UserId,
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = ResolveEntityId(entry),
                OccurredAt = occurredAt,
            });
        }

        return rows;
    }

    /// <summary>AU-6/AU-7: `_set.Update(entity)` บน entity ที่ยัง detached จะตั้ง OriginalValues =
    /// CurrentValues ทำให้ transition false->true มองไม่เห็น แต่ทุกเส้นทาง soft-delete ในโค้ดวันนี้
    /// อ่านแถวมาก่อน (Get()/GetIncludingDeleted()) แล้วค่อยแก้ instance ที่ track อยู่ จึงไม่โดนข้อนี้
    /// - ถ้ามีเส้นทางใหม่ที่ Update() entity แบบ detached ต้องตีกลับไปที่ system-analyst</summary>
    private static bool IsSoftDelete(EntityEntry entry)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(IEntityMaster<string>.IsDelete));
        return property is not null && property.OriginalValue is false && property.CurrentValue is true;
    }

    /// <summary>AU-11: ลำดับนี้เท่านั้น - ICompanyScoped -> CompanyId ของแถวนั้น · Company -> Id
    /// ตัวเอง · นอกนั้น -> null (ระดับระบบ) · อ่านจาก CLR object (entry.Entity) ไม่ใช่
    /// entry.Property() เพราะ object materialize แล้วเสมอทุก state รวมถึง Deleted · วันนี้ entity
    /// ที่ตกมาที่ null มีตัวเดียวคือ AdminUser (มติ OQ-A1 2026-08-28 - ห้าม "ทำให้ฉลาดขึ้น" ด้วยการ
    /// อ่าน AdminUser.CompanyId ของแถวนั้น)</summary>
    private static string? ResolveCompanyId(EntityEntry entry) => entry.Entity switch
    {
        ICompanyScoped scoped => scoped.CompanyId,
        Company company => company.Id,
        _ => null,
    };

    /// <summary>AU-10: EntityId = ค่าของ primary key เป็น string เดี่ยว - ทุก entity ในโปรเจกต์นี้
    /// มี PK เป็น string เดี่ยวที่ service สร้างเองด้วย IdGenerator ก่อน SaveChanges ถ้าเจอ entity ที่
    /// PK ไม่ใช่ string เดี่ยว หรือค่าที่ได้เป็นค่าว่าง ต้อง throw ห้ามข้ามเงียบๆ</summary>
    private static string ResolveEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties is null || keyProperties.Count != 1 || keyProperties[0].ClrType != typeof(string))
        {
            throw new InvalidOperationException(
                $"AuditLog: entity {entry.Metadata.ClrType.Name} ไม่มี primary key เป็น string เดี่ยว ไม่สามารถบันทึก audit log ได้");
        }

        var value = entry.Property(keyProperties[0].Name).CurrentValue as string;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"AuditLog: entity {entry.Metadata.ClrType.Name} มีค่า primary key ว่างเปล่า ไม่สามารถบันทึก audit log ได้");
        }

        return value;
    }

    // AU-1: override SaveChanges(bool) และ SaveChangesAsync(bool, CancellationToken) เท่านั้น -
    // ไม่ใช่เวอร์ชันไม่มีพารามิเตอร์ - เพราะ DbContext ฐานส่ง call ไปที่เวอร์ชันมี bool เสมออยู่แล้ว
    // สองตัวนี้คือจุดที่ทุกเส้นทางไหลผ่านจริง
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // AU-14: AddRange แล้วปล่อยให้ base.SaveChanges เขียนทั้งหมดในครั้งเดียว - ห้ามเรียก
        // SaveChanges ซ้อนภายใน (ทำให้มติ Q-A3 เป็นจริงโดยไม่ต้องเปิด transaction เอง)
        var rows = BuildAuditLogRows();
        if (rows.Count > 0)
        {
            Set<AuditLog>().AddRange(rows);
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var rows = BuildAuditLogRows();
        if (rows.Count > 0)
        {
            Set<AuditLog>().AddRange(rows);
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
