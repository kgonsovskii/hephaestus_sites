using Microsoft.Extensions.Configuration;
using Sites.Web.Caching;
using Microsoft.Extensions.Options;

namespace Sites.Web.Tests;

internal static class TestSitesProxyOptions
{
    public static IConfiguration CreateConfiguration(Action<SitesProxyOptions>? configure = null)
    {
        var options = Create(configure);
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sites:UpstreamRequestTimeout"] = options.UpstreamRequestTimeout.ToString(),
                ["Sites:Cache:RootPath"] = options.Cache.RootPath ?? string.Empty,
                ["Sites:Cache:MaxEntryBytes"] = options.Cache.MaxEntryBytes.ToString(),
                ["Sites:Cache:Ttl"] = options.Cache.Ttl.ToString(),
                ["Sites:Cache:RejectRangeRequests"] = options.Cache.RejectRangeRequests.ToString(),
                ["Sites:Cache:ExcludedContentTypes:0"] = options.Cache.ExcludedContentTypes[0],
                ["Sites:Cache:ExcludedContentTypes:1"] = options.Cache.ExcludedContentTypes[1],
                ["Sites:Cache:ExcludedContentTypes:2"] = options.Cache.ExcludedContentTypes[2],
                ["Sites:Cache:ExcludedContentTypes:3"] = options.Cache.ExcludedContentTypes[3]
            })
            .Build();
    }

    public static SitesProxyOptions Create(Action<SitesProxyOptions>? configure = null)
    {
        var options = new SitesProxyOptions
        {
            UpstreamRequestTimeout = TimeSpan.FromMinutes(10),
            Cache = new ProxyCacheOptions
            {
                RootPath = string.Empty,
                MaxEntryBytes = 20 * 1024 * 1024,
                Ttl = TimeSpan.FromDays(7),
                RejectRangeRequests = true,
                ExcludedContentTypes =
                [
                    "video/",
                    "application/vnd.apple.mpegurl",
                    "application/x-mpegurl",
                    "application/dash+xml"
                ]
            }
        };

        configure?.Invoke(options);
        SitesProfileSettingsValidator.Validate(options);
        return options;
    }

    public static SitesProfileSettingsTemplate CreateTemplate(Action<SitesProxyOptions>? configure = null) =>
        new(CreateConfiguration(configure));
}
