using SupportRoom.Application.Common;

namespace SupportRoom.Api;

/// <summary>
/// Drains IBackgroundTaskQueue for the lifetime of the app. Each work item runs inside its own
/// new IServiceScope - IUnitOfWork/IKnowledgeIndexingService etc. are Scoped, and the HTTP
/// request that enqueued the work is long gone by the time this runs, so nothing Scoped captured
/// from that request can be reused here.
/// </summary>
public sealed class QueuedHostedService(
    IBackgroundTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<QueuedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task> workItem;
            try
            {
                workItem = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background task queue work item failed");
            }
        }
    }
}
