using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sites.Track;

public sealed class TrackFlushHostedService : IHostedService, IDisposable
{
    private readonly TrackStore _store;
    private readonly TrackPostgres _postgres;
    private readonly IOptions<TrackOptions> _options;
    private readonly ILogger<TrackFlushHostedService> _logger;
    private Timer? _timer;

    public TrackFlushHostedService(
        TrackStore store,
        TrackPostgres postgres,
        IOptions<TrackOptions> options,
        ILogger<TrackFlushHostedService> logger)
    {
        _store = store;
        _postgres = postgres;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_postgres.IsConfigured)
        {
            var loaded = await _postgres.LoadRecentAsync(cancellationToken);
            _store.ReplaceAll(loaded);
            _logger.LogInformation("Track store loaded {Count} recent visits", loaded.Count);
        }
        else
        {
            _logger.LogWarning("Track postgres connection string is empty; memory-only until configured");
        }

        var interval = _options.Value.FlushInterval;
        if (interval <= TimeSpan.Zero)
            interval = TimeSpan.FromMinutes(2);

        _timer = new Timer(static state => _ = ((TrackFlushHostedService)state!).FlushSafeAsync(), this, interval, interval);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        await FlushAsync(cancellationToken);
    }

    public void Dispose() => _timer?.Dispose();

    private async Task FlushSafeAsync()
    {
        try
        {
            await FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Track flush failed");
        }
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (!_store.IsDirty || !_postgres.IsConfigured)
            return;

        var snapshot = _store.Snapshot();
        await _postgres.FlushAsync(snapshot, cancellationToken);
        _store.MarkClean();
        _logger.LogInformation("Track flushed {Count} visits", snapshot.Count);
    }
}
