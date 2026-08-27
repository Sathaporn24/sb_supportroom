using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Data.Data;

/// <summary>
/// Used ONLY by `dotnet ef` (migrations add / database update / script). Without it the tooling
/// boots the whole API host, which demands every provider credential and a JWT secret before it
/// will start - so adding a column would require a fully configured environment even though
/// nothing about schema needs Gemini, Pinecone or Google Slides to be reachable.
///
/// EF discovers this by convention: when an IDesignTimeDbContextFactory exists, it is preferred
/// over the application host, so the two paths never fight.
///
/// The company context here is deliberately left unresolved. Query filters are compiled into the
/// model, not evaluated, so design time never needs a real company - and an unresolved context
/// matches zero rows, which is the safe direction if anything ever did run through it.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Same precedence the running app uses (EntityFrameworkConfiguration): the env var wins,
        // and .env is loaded first so a developer who already configured the app for `dotnet run`
        // does not have to configure the tooling separately.
        DotEnv.Load(Path.Combine(FindRepoRoot(), "src", "SupportRoom.Api", ".env"));

        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ต้องตั้ง POSTGRES_CONNECTION_STRING (หรือใส่ไว้ใน src/SupportRoom.Api/.env) ก่อนใช้คำสั่ง dotnet ef");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options, new CompanyContext(), new CurrentUser());
    }

    /// <summary>Walks up to the folder holding SupportRoom.slnx rather than assuming how deep the
    /// tooling's working directory is - the same approach the test projects' TestEnv uses.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SupportRoom.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
