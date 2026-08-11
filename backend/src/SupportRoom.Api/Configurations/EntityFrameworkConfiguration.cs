using Microsoft.EntityFrameworkCore;
using SupportRoom.Domain.Common;
using SupportRoom.Providers.Data.Data;
using SupportRoom.Providers.Data.Data.UnitOfWork;

namespace SupportRoom.Api.Configurations;

/// <summary>
/// Connection string: ConnectionStrings:Postgres in appsettings/appsettings.Development.json,
/// overridable by the POSTGRES_CONNECTION_STRING env var - same "env var overrides config"
/// pattern ProviderSelectionReader already uses elsewhere in this project.
/// </summary>
public static class EntityFrameworkConfiguration
{
    public static IServiceCollection AddEntityFrameworkConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("Postgres");
        }
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing ConnectionStrings:Postgres (or POSTGRES_CONNECTION_STRING env var).");
        }

        // Scoped, and registered before the DbContext that injects it: ApplicationDbContext's
        // company query filters read this instance on every query, and the recipient-side flow
        // resolves it mid-request from a session token.
        services.AddScoped<ICompanyContext, CompanyContext>();

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
