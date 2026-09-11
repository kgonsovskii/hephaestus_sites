using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sites.Web.Caching;

namespace Sites.Web;

public sealed class SitesTextCacheStartupHostedService : IHostedService
{
    private readonly ProxyDiskCache _cache;
    private readonly ProxyCachePolicy _cachePolicy;
    private readonly ILogger<SitesTextCacheStartupHostedService> _logger;

    public SitesTextCacheStartupHostedService(
        ProxyDiskCache cache,
        ProxyCachePolicy cachePolicy,
        ILogger<SitesTextCacheStartupHostedService> logger)
    {
        _cache = cache;
        _cachePolicy = cachePolicy;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var result = SitesMaintenanceCaches.ClearTextCache(_cache, _cachePolicy);
        _logger.LogInformation(
            "Startup text cache clear: {Removed} entries at {CacheRoot}",
            result.RemovedEntries,
            result.CacheRoot);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
