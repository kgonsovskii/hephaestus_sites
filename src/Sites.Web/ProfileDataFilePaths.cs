using Sites.Web.Abstractions;

namespace Sites.Web;

internal static class ProfileDataFilePaths
{
    internal static string Resolve(string fileName, Func<string, string> resolveProfilePath)
    {
        var repoRoot = RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null)
            return Path.GetFullPath(resolveProfilePath(repoRoot));

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            SitesProfileResolver.SitesDataDirectoryName,
            SitesProfileResolver.Current,
            fileName));
    }
}
