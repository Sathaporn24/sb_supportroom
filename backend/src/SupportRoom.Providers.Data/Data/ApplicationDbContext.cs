using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Entities;

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
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICompanyContext companyContext) : DbContext(options)
{
    /// <summary>No query filter - see the ⚠️ note on this class and on the entity itself.</summary>
    public DbSet<Company> Company => Set<Company>();

    /// <summary>No query filter - see the ⚠️ note on this class and on the entity itself.</summary>
    public DbSet<AdminUser> AdminUser => Set<AdminUser>();

    public DbSet<TrainingLink> TrainingLink => Set<TrainingLink>();
    public DbSet<LearningSession> LearningSession => Set<LearningSession>();
    public DbSet<SessionQuestion> SessionQuestion => Set<SessionQuestion>();
    public DbSet<LessonConfig> LessonConfig => Set<LessonConfig>();
    public DbSet<ChatMessage> ChatMessage => Set<ChatMessage>();
    public DbSet<DocumentResource> DocumentResource => Set<DocumentResource>();

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
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<LessonConfig>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Slug is unique PER COMPANY, not globally - two companies must both be able to own
            // a lesson called "getting-started". A globally unique slug would make the second
            // company onboarded unable to use the obvious names.
            entity.HasIndex(x => new { x.CompanyId, x.Slug }).IsUnique();
            entity.OwnsMany(x => x.SlideConfigs, owned => owned.ToJson());
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });

        builder.Entity<DocumentResource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.LessonId);
            entity.HasIndex(x => x.CompanyId);
            entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
        });
    }
}
