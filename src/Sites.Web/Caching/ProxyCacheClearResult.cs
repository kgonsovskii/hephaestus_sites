namespace Sites.Web.Caching;

public sealed record ProxyCacheClearResult(string CacheRoot, int RemovedEntries);
