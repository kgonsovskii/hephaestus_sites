using Microsoft.Extensions.Configuration;
using Sites.Web.Caching;
using Microsoft.Extensions.Options;

namespace Sites.Web.Tests;

internal static class TestSitesProxyOptions
{
    public static IConfiguration CreateConfiguration(Action<SitesProxyOptions>? configure = null)
    {
        var options = Create(configure);
        var values = new Dictionary<string, string?>
        {
            ["Sites:UpstreamRequestTimeout"] = options.UpstreamRequestTimeout.ToString(),
            ["Sites:Cache:RootPath"] = options.Cache.RootPath ?? string.Empty,
            ["Sites:Cache:MaxEntryBytes"] = options.Cache.MaxEntryBytes.ToString(),
            ["Sites:Cache:Ttl"] = options.Cache.Ttl.ToString(),
            ["Sites:Cache:RejectRangeRequests"] = options.Cache.RejectRangeRequests.ToString(),
            ["Sites:ClientBandwidth:BrowserCacheMaxAge"] = options.ClientBandwidth.BrowserCacheMaxAge.ToString(),
            ["Sites:ClientBandwidth:SendCacheControl"] = options.ClientBandwidth.SendCacheControl.ToString(),
            ["Sites:ClientBandwidth:EnableCompression"] = options.ClientBandwidth.EnableCompression.ToString(),
            ["Sites:ClientBandwidth:CompressionMinBytes"] = options.ClientBandwidth.CompressionMinBytes.ToString(),
            ["Sites:ClientBandwidth:LocalAssetsMaxAge"] = options.ClientBandwidth.LocalAssetsMaxAge.ToString(),
            ["Sites:ClientBandwidth:ServeHeadFromCache"] = options.ClientBandwidth.ServeHeadFromCache.ToString(),
            ["Sites:ClientBandwidth:EnableNotModified"] = options.ClientBandwidth.EnableNotModified.ToString()
        };

        for (var i = 0; i < options.Cache.ExcludedContentTypes.Count; i++)
            values[$"Sites:Cache:ExcludedContentTypes:{i}"] = options.Cache.ExcludedContentTypes[i];

        for (var i = 0; i < options.ClientBandwidth.CompressionContentTypes.Count; i++)
            values[$"Sites:ClientBandwidth:CompressionContentTypes:{i}"] = options.ClientBandwidth.CompressionContentTypes[i];

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
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
            },
            ClientBandwidth = new ClientBandwidthOptions
            {
                BrowserCacheMaxAge = TimeSpan.FromDays(7),
                SendCacheControl = true,
                EnableCompression = true,
                CompressionMinBytes = 256,
                CompressionContentTypes =
                [
                    "text/html",
                    "text/css",
                    "application/javascript",
                    "application/json"
                ],
                LocalAssetsMaxAge = TimeSpan.FromDays(7),
                ServeHeadFromCache = true,
                EnableNotModified = true
            }
        };

        configure?.Invoke(options);
        SitesProfileSettingsValidator.Validate(options);
        return options;
    }

    public static SitesProfileSettingsTemplate CreateTemplate(Action<SitesProxyOptions>? configure = null) =>
        new(CreateConfiguration(configure));
}
