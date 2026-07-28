using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class ProfileDataFilePathsTests
{
    [Fact]
    public void ResolvePath_UsesRepositoryProfilesWhenRepoIsAvailable()
    {
        var repoRoot = RepositoryPaths.ResolveRoot();
        var expected = SitesProfileResolver.ResolveSitesJsonPath(repoRoot);

        var resolved = SitesJsonFile.ResolvePath();

        Assert.Equal(
            Path.GetFullPath(expected).Replace('\\', '/'),
            Path.GetFullPath(resolved).Replace('\\', '/'));
        Assert.Contains("/profiles/default/sites.json", resolved.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/output/profiles/", resolved.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
