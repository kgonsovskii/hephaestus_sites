using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sites.CertMaintenance;

public sealed class CertMaintenanceHostedService : BackgroundService
{
    private readonly ILogger<CertMaintenanceHostedService> _logger;
    private readonly IOptionsMonitor<CertMaintenanceOptions> _options;
    private readonly CertMaintenanceWorker _worker;

    public CertMaintenanceHostedService(
        ILogger<CertMaintenanceHostedService> logger,
        IOptionsMonitor<CertMaintenanceOptions> options,
        CertMaintenanceWorker worker)
    {
        _logger = logger;
        _options = options;
        _worker = worker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_worker.IsEnabled())
        {
            _logger.LogInformation("Certificate maintenance is disabled.");
            return;
        }

        var startupDelay = _options.CurrentValue.StartupDelay;
        if (startupDelay < TimeSpan.Zero)
            startupDelay = TimeSpan.Zero;

        try
        {
            if (startupDelay > TimeSpan.Zero)
                await Task.Delay(startupDelay, stoppingToken);

            await RunOnceSafeAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await RunMaintenanceLoopAsync(stoppingToken);
    }

    private async Task RunMaintenanceLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = _options.CurrentValue.CheckInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromHours(12);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunOnceSafeAsync(stoppingToken);
        }
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _worker.RunOnceAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Certificate maintenance failed.");
        }
    }
}
