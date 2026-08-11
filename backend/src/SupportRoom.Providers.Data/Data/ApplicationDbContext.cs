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
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICompanyContext companyContext) : DbContext(options)
{
    public DbSet<TrainingSession> TrainingSession => Set<TrainingSession>();
    public DbSet<SessionQuestion> SessionQuestion => Set<SessionQuestion>();
    public DbSet<LessonConfig> LessonConfig => Set<LessonConfig>();
    public DbSet<SessionSummary> SessionSummary => Set<SessionSummary>();
    public DbSet<ChatMessage> ChatMessage => Set<ChatMessage>();
    public DbSet<DocumentResource> DocumentResource => Set<DocumentResource>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TrainingSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Token stays globally unique - it is the public join secret and is looked up before
            // any company is known (GetByToken bypasses the filter), so it must not collide
            // across companies.
            entity.HasIndex(x => x.Token).IsUnique();
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

        builder.Entity<SessionSummary>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId).IsUnique();
            entity.HasIndex(x => x.CompanyId);
            // List<string> maps natively to a Postgres text[] column via Npgsql - no JSON needed.
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
