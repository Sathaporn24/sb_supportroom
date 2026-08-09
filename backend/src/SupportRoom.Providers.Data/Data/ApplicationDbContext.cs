using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Entities;

namespace SupportRoom.Providers.Data.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
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
            entity.HasIndex(x => x.Token).IsUnique();
        });

        builder.Entity<SessionQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId);
        });

        builder.Entity<LessonConfig>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.OwnsMany(x => x.SlideConfigs, owned => owned.ToJson());
        });

        builder.Entity<SessionSummary>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId).IsUnique();
            // List<string> maps natively to a Postgres text[] column via Npgsql - no JSON needed.
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SessionId);
        });

        builder.Entity<DocumentResource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.LessonId);
        });
    }
}
