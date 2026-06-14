using Sites.Web.Caching;
using Microsoft.Extensions.Options;

namespace Sites.Web.Tests;

internal static class TestSitesProxyOptions
{
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
        new(Options.Create(Create(configure)));
}
