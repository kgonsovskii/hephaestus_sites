namespace Sites.Web.Caching;

public static class ProxyCacheRoot
{
    public static string Resolve(string? overrideRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideRoot))
            return Path.GetFullPath(overrideRoot);

        return OperatingSystem.IsWindows()
            ? @"C:\_cache"
            : "/_cache";
    }

    public static string GetSiteDirectory(string sourceHost, string? overrideRoot = null)
    {
        var safeName = string.Concat(sourceHost.Select(ch =>
            Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

        return Path.Combine(Resolve(overrideRoot), safeName);
    }
}
