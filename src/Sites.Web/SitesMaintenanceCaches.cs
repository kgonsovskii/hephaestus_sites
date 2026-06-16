using Sites.Web.Caching;

namespace Sites.Web;

public static class SitesMaintenanceCaches
{
    public static ProxyCacheClearResult ClearTextCache(ProxyDiskCache cache, ProxyCachePolicy policy)
    {
        ClearJsTransformCache();
        return cache.ClearNonBinary(policy);
    }

    public static void ClearJsTransformCache() => LocalJsTransformCache.Clear();
}
