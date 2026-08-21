using SupportRoom.Application.Services;

namespace SupportRoom.Api;

/// <summary>
/// Solves the same bootstrap problem as SeedFirstOwnerHostedService, one level down: the seeded
/// owner can sign in but has no company to pick from, so every company-scoped screen is stuck
/// until one exists. Runs once at startup and does nothing unless the Company table is completely
/// empty, so it cannot resurrect a company that was deliberately deactivated.
///
/// Failures are logged, never thrown: a database that is not migrated yet must not stop the API
/// from starting, or a fresh deployment would be unable to run migrations against itself.
/// </summary>
public sealed class SeedFirstCompanyHostedService(
    IServiceProvider serviceProvider,
    ILogger<SeedFirstCompanyHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ICompanyService is scoped; a hosted service is a singleton, so it needs its own scope.
            using var scope = serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<ICompanyService>().SeedFirstCompanyIfEmpty();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "First-company seeding failed - the API will still start, but the owner will have nothing to switch to");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
