using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>
/// Singleton that tracks the current state of a feed import operation.
/// Allows the SSE endpoint to stream progress updates to the browser.
/// </summary>
public class FeedImportProgressTracker : IProgress<FeedImportProgress>
{
    private readonly Lock _lock = new();
    private FeedImportProgress _current;
    private Channel<FeedImportProgress> _channel;

    /// <summary>
    /// Whether an import is currently running or has completed with results available.
    /// </summary>
    public bool HasState
    {
        get
        {
            lock (_lock)
            {
                return _current != null;
            }
        }
    }

    /// <summary>
    /// Whether an import is currently in progress (not yet complete).
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _current != null && !_current.IsComplete;
            }
        }
    }

    /// <summary>
    /// Get a snapshot of the current progress.
    /// </summary>
    public FeedImportProgress GetCurrentProgress()
    {
        lock (_lock)
        {
            return _current == null ? null : CloneProgress(_current);
        }
    }

    /// <summary>
    /// Called by the import service to report progress updates.
    /// </summary>
    public void Report(FeedImportProgress value)
    {
        FeedImportProgress snapshot;
        lock (_lock)
        {
            _current = CloneProgress(value);
            snapshot = CloneProgress(value);
        }

        // Non-blocking write to the channel for SSE subscribers
        _channel?.Writer.TryWrite(snapshot);
    }

    /// <summary>
    /// Reset the tracker for a new import operation.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _current = null;
            _channel?.Writer.TryComplete();
            _channel = Channel.CreateUnbounded<FeedImportProgress>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        }
    }

    /// <summary>
    /// Subscribe to progress updates. Used by the SSE endpoint.
    /// </summary>
    public async Task ReadProgressAsync(
        Func<FeedImportProgress, Task> onProgress,
        CancellationToken cancellationToken)
    {
        Channel<FeedImportProgress> channel;
        lock (_lock)
        {
            channel = _channel;

            // Send current state immediately if available
            if (_current != null)
            {
                onProgress(CloneProgress(_current)).GetAwaiter().GetResult();
            }
        }

        if (channel == null)
            return;

        try
        {
            await foreach (var progress in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await onProgress(progress);

                if (progress.IsComplete)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
    }

    private static FeedImportProgress CloneProgress(FeedImportProgress source) => new()
    {
        TotalVersions = source.TotalVersions,
        Imported = source.Imported,
        Skipped = source.Skipped,
        Failed = source.Failed,
        CurrentPackage = source.CurrentPackage,
        IsComplete = source.IsComplete,
        Errors = [.. source.Errors],
    };
}
