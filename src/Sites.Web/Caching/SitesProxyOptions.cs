namespace Sites.Web.Caching;

public sealed class SitesProxyOptions
{
    public const string SectionName = "Sites";

    public TimeSpan UpstreamRequestTimeout { get; set; }

    public ProxyCacheOptions Cache { get; set; } = new();
}

public sealed class ProxyCacheOptions
{
    public string? RootPath { get; set; }

    public long MaxEntryBytes { get; set; }

    public TimeSpan Ttl { get; set; }

    public bool RejectRangeRequests { get; set; }

    public List<string> ExcludedContentTypes { get; set; } = [];
}
