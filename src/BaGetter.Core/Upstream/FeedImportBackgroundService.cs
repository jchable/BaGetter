using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BaGetter.Core;

/// <summary>
/// A job queued for background feed import execution.
/// </summary>
public class FeedImportJob
{
    public FeedImportOptions Options { get; init; }
}

/// <summary>
/// Background service that processes feed import jobs from a channel.
/// Only one import can run at a time (bounded channel of capacity 1).
/// </summary>
public class FeedImportBackgroundService : BackgroundService
{
    private readonly Channel<FeedImportJob> _jobChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FeedImportProgressTracker _tracker;
    private readonly ILogger<FeedImportBackgroundService> _logger;
    private CancellationTokenSource _importCts;

    public FeedImportBackgroundService(
        Channel<FeedImportJob> jobChannel,
        IServiceScopeFactory scopeFactory,
        FeedImportProgressTracker tracker,
        ILogger<FeedImportBackgroundService> logger)
    {
        _jobChannel = jobChannel ?? throw new ArgumentNullException(nameof(jobChannel));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cancel the currently running import, if any.
    /// </summary>
    public void CancelCurrentImport()
    {
        _importCts?.Cancel();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Feed import background service started");

        await foreach (var job in _jobChannel.Reader.ReadAllAsync(stoppingToken))
        {
            _importCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _tracker.Reset();

            try
            {
                _logger.LogInformation("Starting feed import from {FeedUrl}", job.Options.FeedUrl);

                using var scope = _scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IFeedImportService>();

                await importService.ImportAsync(job.Options, _tracker, _importCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Feed import was cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed import background job failed");
            }
            finally
            {
                _importCts.Dispose();
                _importCts = null;
            }
        }
    }
}
