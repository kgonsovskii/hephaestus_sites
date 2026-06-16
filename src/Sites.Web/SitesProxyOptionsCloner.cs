using Sites.Web.Caching;

namespace Sites.Web;

internal static class SitesProxyOptionsCloner
{
    public static SitesProxyOptions Clone(SitesProxyOptions source) => new()
    {
        UpstreamRequestTimeout = source.UpstreamRequestTimeout,
        Cache = new ProxyCacheOptions
        {
            RootPath = source.Cache.RootPath,
            MaxEntryBytes = source.Cache.MaxEntryBytes,
            Ttl = source.Cache.Ttl,
            RejectRangeRequests = source.Cache.RejectRangeRequests,
            ExcludedContentTypes = source.Cache.ExcludedContentTypes.ToList()
        },
        ClientBandwidth = new ClientBandwidthOptions
        {
            BrowserCacheMaxAge = source.ClientBandwidth.BrowserCacheMaxAge,
            SendCacheControl = source.ClientBandwidth.SendCacheControl,
            EnableCompression = source.ClientBandwidth.EnableCompression,
            CompressionMinBytes = source.ClientBandwidth.CompressionMinBytes,
            CompressionContentTypes = source.ClientBandwidth.CompressionContentTypes.ToList(),
            LocalAssetsMaxAge = source.ClientBandwidth.LocalAssetsMaxAge,
            ServeHeadFromCache = source.ClientBandwidth.ServeHeadFromCache,
            EnableNotModified = source.ClientBandwidth.EnableNotModified
        }
    };
}
