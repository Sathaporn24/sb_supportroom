using SupportRoom.Application.Services;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Api;

/// <summary>
/// Polls BackgroundJob for work instead of draining an in-memory channel - replaces
/// IBackgroundTaskQueue/QueuedHostedService (design.md DI-17), which lost every queued item on
/// restart because the queue lived only in process memory.
///
/// Each claimed job runs inside its own new IServiceScope, same reason QueuedHostedService did
/// this: IUnitOfWork/IBackgroundJobProcessor etc. are Scoped, and there is no HTTP request scope
/// to reuse here.
/// </summary>
public sealed class BackgroundJobHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RequeueOrphanedRunningJobs();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<IBackgroundJobRepository>();
                var job = jobRepository.ClaimNext(DateTime.UtcNow);

                if (job is null)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                var processor = scope.ServiceProvider.GetRequiredService<IBackgroundJobProcessor>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job worker loop failed unexpectedly");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    /// <summary>DI-11 - runs once at startup. A job stuck at "running" here didn't fail, it was
    /// killed mid-job by the previous process exiting, so it goes back to "pending" without
    /// spending one of its three retry attempts. Safe only because this app runs a single
    /// instance (design.md R-6) - a second instance would need a lease instead of this.</summary>
    private void RequeueOrphanedRunningJobs()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var jobRepository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<IBackgroundJobRepository>();
            var requeued = jobRepository.RequeueOrphanedRunning();
            if (requeued > 0)
            {
                logger.LogInformation("Requeued {Count} background job(s) orphaned by the previous process", requeued);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to requeue orphaned background jobs on startup - the API will still start");
        }
    }
}
