namespace Sites.Cp.Models;

public sealed class ClearCacheResponse
{
    public string Profile { get; init; } = string.Empty;

    public string CacheRoot { get; init; } = string.Empty;

    public int RemovedEntries { get; init; }
}

public sealed class InvalidateSitesResponse
{
    public string Profile { get; init; } = string.Empty;

    public int SiteCount { get; init; }

    public string SitesJsonPath { get; init; } = string.Empty;
}
