using SupportRoom.Application.Services;

namespace SupportRoom.Api;

/// <summary>
/// Solves the bootstrap problem: nobody can sign in to create the first account, and creating one
/// by hand means writing a password hash into the database directly.
///
/// Runs once at startup and does nothing at all unless the AdminUser table is completely empty, so
/// it cannot resurrect an account that was deliberately deactivated, and cannot overwrite anything.
///
/// Failures are logged, never thrown: a database that is not migrated yet must not stop the API
/// from starting, or a fresh deployment would be unable to run migrations against itself.
/// </summary>
public sealed class SeedFirstOwnerHostedService(
    IServiceProvider serviceProvider,
    ILogger<SeedFirstOwnerHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // IAuthService is scoped; a hosted service is a singleton, so it needs its own scope.
            using var scope = serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IAuthService>().SeedFirstOwnerIfEmpty();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "First-owner seeding failed - the API will still start, but nobody may be able to sign in");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
