using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sites.Web;

namespace Sites.Web.Git;

public sealed class SitesGitMaintenanceHostedService : BackgroundService
{
    private readonly SitesGitService _git;
    private readonly SitesCatalogService _catalog;
    private readonly IOptionsMonitor<SitesGitOptions> _options;
    private readonly ILogger<SitesGitMaintenanceHostedService> _logger;

    public SitesGitMaintenanceHostedService(
        SitesGitService git,
        SitesCatalogService catalog,
        IOptionsMonitor<SitesGitOptions> options,
        ILogger<SitesGitMaintenanceHostedService> logger)
    {
        _git = git;
        _catalog = catalog;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _git.PullAsync(stoppingToken);
                if (result.Succeeded)
                {
                    _catalog.ReloadRegistry();
                    _logger.LogInformation("Sites git pull: {Message}", result.Message);
                }
                else
                {
                    _logger.LogWarning("Sites git pull: {Message}", result.Message);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Sites git pull maintenance failed.");
            }

            var interval = _options.CurrentValue.PullInterval;
            if (interval <= TimeSpan.Zero)
                interval = TimeSpan.FromHours(6);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
