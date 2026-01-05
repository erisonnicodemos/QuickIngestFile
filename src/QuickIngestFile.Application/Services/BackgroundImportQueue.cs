namespace QuickIngestFile.Application.Services;

using System.Threading.Channels;
using QuickIngestFile.Application.Parsing;

/// <summary>
/// Represents a queued import job to be processed in background.
/// </summary>
public sealed record QueuedImportJob(
    Guid ImportJobId,
    byte[] FileData,
    string FileName,
    long FileSize,
    ParserOptions Options);

/// <summary>
/// In-memory queue for background import processing.
/// Uses System.Threading.Channels for thread-safe producer/consumer pattern.
/// </summary>
public sealed class BackgroundImportQueue
{
    private readonly Channel<QueuedImportJob> _queue;
    
    public BackgroundImportQueue()
    {
        // Bounded channel to prevent memory overflow
        // If queue is full, producer will wait (backpressure)
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false // Multiple concurrent uploads allowed
        };
        
        _queue = Channel.CreateBounded<QueuedImportJob>(options);
    }
    
    /// <summary>
    /// Enqueue an import job to be processed in background.
    /// Returns immediately, processing happens asynchronously.
    /// </summary>
    public async ValueTask EnqueueAsync(QueuedImportJob job, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }
    
    /// <summary>
    /// Dequeue next job for processing. Called by background worker.
    /// </summary>
    public async ValueTask<QueuedImportJob> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get number of pending jobs in the queue.
    /// </summary>
    public int PendingCount => _queue.Reader.Count;
    
    /// <summary>
    /// Check if there are pending jobs.
    /// </summary>
    public bool HasPendingJobs => _queue.Reader.Count > 0;
}
