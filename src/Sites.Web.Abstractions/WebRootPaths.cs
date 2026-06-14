namespace Sites.Web.Abstractions;

public static class WebRootPaths
{
    public const string DefaultDirectoryName = "wwwroot";

    public static string Resolve(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        var besideApp = Path.Combine(AppContext.BaseDirectory, DefaultDirectoryName);
        if (Directory.Exists(besideApp))
            return besideApp;

        var repoRoot = RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null)
        {
            var repoWebRoot = Path.Combine(repoRoot, DefaultDirectoryName);
            if (Directory.Exists(repoWebRoot))
                return repoWebRoot;
        }

        return besideApp;
    }
}
