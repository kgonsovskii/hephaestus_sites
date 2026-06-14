using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sites.Web.Git;

public sealed class SitesGitMaintenanceHostedService : BackgroundService
{
    private readonly SitesGitService _git;
    private readonly SitesCatalogChangedSignal _catalogChanged;
    private readonly IOptionsMonitor<SitesGitOptions> _options;
    private readonly ILogger<SitesGitMaintenanceHostedService> _logger;

    public SitesGitMaintenanceHostedService(
        SitesGitService git,
        SitesCatalogChangedSignal catalogChanged,
        IOptionsMonitor<SitesGitOptions> options,
        ILogger<SitesGitMaintenanceHostedService> logger)
    {
        _git = git;
        _catalogChanged = catalogChanged;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _git.SyncAsync(stoppingToken);
                if (result.Succeeded)
                    _logger.LogInformation("Sites git sync: {Message}", result.Message);
                else
                    _logger.LogWarning("Sites git sync: {Message}", result.Message);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Sites git sync maintenance failed.");
            }

            var interval = _options.CurrentValue.SyncInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromHours(24);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var delayTask = Task.Delay(interval, linked.Token);
                var wakeTask = _catalogChanged.WhenWakeAsync(stoppingToken);
                var winner = await Task.WhenAny(delayTask, wakeTask);
                if (winner == wakeTask)
                {
                    linked.Cancel();
                    _catalogChanged.DrainExtraSignals();
                    _logger.LogInformation("Sites git sync woken by catalog change.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
