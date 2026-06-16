namespace Sites.Web.Abstractions;

public static class WwwrootAssetCatalog
{
    public static Dictionary<string, string> Build(
        string webRootPath,
        string targetHost,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        var assets = Scan(webRootPath, targetHost);

        if (overrides is not { Count: > 0 })
            return assets;

        foreach (var (requestPath, relativePath) in overrides)
        {
            if (string.IsNullOrWhiteSpace(requestPath) || string.IsNullOrWhiteSpace(relativePath))
                continue;

            assets[NormalizeRequestPath(requestPath)] = relativePath.Replace('\\', '/').TrimStart('/');
        }

        return assets;
    }

    public static Dictionary<string, string> Scan(string webRootPath, string targetHost)
    {
        var assets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var siteDirectory = Path.Combine(webRootPath, targetHost.Trim());
        if (!Directory.Exists(siteDirectory))
            return assets;

        foreach (var filePath in Directory.EnumerateFiles(siteDirectory, "*", SearchOption.AllDirectories))
        {
            if (!ShouldPublishFile(filePath))
                continue;

            var relativePath = Path.GetRelativePath(siteDirectory, filePath).Replace('\\', '/');
            assets["/" + relativePath] = relativePath;
        }

        return assets;
    }

    internal static bool ShouldPublishFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.Length == 0)
            return false;

        if (fileName.StartsWith('.'))
            return false;

        if (fileName.EndsWith(".patch.js", StringComparison.OrdinalIgnoreCase))
            return false;

        if (fileName.Equals("README.txt", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string NormalizeRequestPath(string requestPath)
    {
        var normalized = requestPath.Trim();
        if (normalized.Length == 0)
            return "/";

        return normalized.StartsWith('/') ? normalized : $"/{normalized}";
    }
}
