using Microsoft.Extensions.DependencyInjection;

namespace SupportRoom.Infrastructure.Cors;

/// <summary>
/// No CORS exists in the current single-origin Next.js app - this is net-new, required
/// because the frontend calls this API cross-origin now that its base URL is repointed at
/// SB_Ai_Supportroom. Allowed origins come from the AllowedOrigins env var (comma-separated)
/// so the production frontend domain can be added without a code change; localhost:3000 is
/// only added automatically in Development, so Production is never open to it unless
/// ALLOWED_ORIGINS explicitly lists it.
/// </summary>
public static class CorsSetup
{
    public const string PolicyName = "Frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, bool isDevelopment)
    {
        var configured = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (isDevelopment)
        {
            origins.Add("http://localhost:3000");
        }
        if (!string.IsNullOrWhiteSpace(configured))
        {
            foreach (var origin in configured.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                origins.Add(origin);
            }
        }

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy => policy
                .WithOrigins([.. origins])
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        return services;
    }
}
