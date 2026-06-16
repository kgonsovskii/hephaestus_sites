namespace Sites.Web;

public static class SitesMaintenanceCaches
{
    public static void ClearJsTransformCache() => LocalJsTransformCache.Clear();
}
