using System.Threading.Channels;

namespace SupportRoom.Application.Common;

public interface IBackgroundTaskQueue
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Minimal fire-and-forget work queue for the one place today that needs it: document upload
/// (DocumentResourceService.UploadAsync) returns to the caller right after storage + a "Pending"
/// DB row, while the slow parts (text extraction, embedding, Pinecone upsert) continue after the
/// response is sent - drained by SupportRoom.Api's QueuedHostedService. Backed by an *unbounded*
/// Channel on purpose: a slow/stuck consumer only grows memory, it can never block a producer (an
/// upload request), which a bounded channel could under load. In-memory only, not persisted - a
/// queued item is lost if the app restarts before it runs (the document stays at "Pending"); a
/// durable queue is real extra scope this doesn't need yet.
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem)
        => _channel.Writer.TryWrite(workItem);

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
}
