using Sites.Web.Abstractions;

namespace Sites.Web;

internal static class ProfileDataFilePaths
{
    internal static string Resolve(string fileName, Func<string, string> resolveProfilePath)
    {
        var repoRoot = RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null)
        {
            var repoPath = resolveProfilePath(repoRoot);
            if (File.Exists(repoPath))
                return Path.GetFullPath(repoPath);
        }

        var publishedPath = Path.Combine(
            AppContext.BaseDirectory,
            SitesProfileResolver.ProfilesDirectoryName,
            SitesProfileResolver.Current,
            fileName);

        return Path.GetFullPath(publishedPath);
    }
}
