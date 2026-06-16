namespace Sites.Web.Caching;

public sealed class ClientBandwidthOptions
{
    public TimeSpan BrowserCacheMaxAge { get; set; }

    public bool SendCacheControl { get; set; }

    public bool EnableCompression { get; set; }

    public int CompressionMinBytes { get; set; }

    public List<string> CompressionContentTypes { get; set; } = [];

    public TimeSpan LocalAssetsMaxAge { get; set; }

    public bool ServeHeadFromCache { get; set; }

    public bool EnableNotModified { get; set; }
}
