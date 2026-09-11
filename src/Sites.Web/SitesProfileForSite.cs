using Sites.Web.Abstractions;

namespace Sites.Web;

public static class SitesProfileForSite
{
    public static void UseProfileThatContains(string targetHost, string? repositoryRoot = null)
    {
        var host = targetHost.Trim();
        if (host.Length == 0)
            return;

        var repo = repositoryRoot ?? RepositoryPaths.TryResolveRoot();
        var currentPath = repo is null
            ? SitesJsonFile.ResolvePath()
            : SitesProfileResolver.ResolveSitesJsonPath(repo);
        if (SitesJsonFile.Load(currentPath).ContainsKey(host))
            return;

        if (repo is null)
            return;

        var data = SitesProfileResolver.ResolveSitesDataBase(repo);
        if (!Directory.Exists(data))
            return;

        foreach (var dir in Directory.EnumerateDirectories(data))
        {
            var profile = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(profile) || profile.StartsWith('.'))
                continue;

            var sitesPath = Path.Combine(dir, SitesProfileResolver.SitesJsonFileName);
            if (!SitesJsonFile.Load(sitesPath).ContainsKey(host))
                continue;

            SitesProfileResolver.Use(profile);
            return;
        }
    }
}
