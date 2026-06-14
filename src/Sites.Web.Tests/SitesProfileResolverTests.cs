using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesProfileResolverTests
{
    private static string CreateTempRepositoryRoot()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "sites-profile-" + Guid.NewGuid().ToString("N"));
        var repo = Path.Combine(sandbox, "repo");
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        File.WriteAllText(Path.Combine(repo, "src", "Sites.sln"), "");
        Directory.CreateDirectory(Path.Combine(repo, "profiles", "default"));
        File.WriteAllText(Path.Combine(repo, "profiles", "default", "sites.json"), "{}");
        return repo;
    }

    [Fact]
    public void ResolveCloneDirectory_UsesFixedRepoNameRegardlessOfProfile()
    {
        var repo = CreateTempRepositoryRoot();
        try
        {
            SitesProfileResolver.Initialize(repo);
            SitesProfileResolver.WriteProfileFile(repo, "staging");
            var path = SitesProfileResolver.ResolveCloneDirectory("/home/root");
            Assert.Equal("/home/root/hephaestus_sites", path.Replace('\\', '/'));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(repo)!, recursive: true);
        }
    }

    [Fact]
    public void WriteProfileFile_UpdatesCurrent()
    {
        var repo = CreateTempRepositoryRoot();
        try
        {
            SitesProfileResolver.Initialize(repo);
            SitesProfileResolver.WriteProfileFile(repo, "staging");

            Assert.Equal("staging", SitesProfileResolver.Current);
            var profilePath = SitesProfileResolver.ResolveProfileFilePath(repo);
            Assert.True(File.Exists(profilePath));
            Assert.Equal("staging", File.ReadAllText(profilePath).Trim());
            Assert.Equal(
                Path.Combine(Path.GetDirectoryName(repo)!, "profile.txt").Replace('\\', '/'),
                profilePath.Replace('\\', '/'));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(repo)!, recursive: true);
        }
    }

    [Fact]
    public void ResolveSitesJsonPath_UsesProfilesDirectory()
    {
        var repo = CreateTempRepositoryRoot();
        try
        {
            var profilePath = SitesProfileResolver.ResolveProfileFilePath(repo);
            File.WriteAllText(profilePath, "default");
            SitesProfileResolver.Initialize(repo);
            var path = SitesProfileResolver.ResolveSitesJsonPath(repo).Replace('\\', '/');
            Assert.EndsWith("/profiles/default/sites.json", path);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(repo)!, recursive: true);
        }
    }

    [Fact]
    public void ResolveProfileFilePath_IsBesideRepositoryRoot()
    {
        var repo = CreateTempRepositoryRoot();
        try
        {
            var expected = Path.Combine(Path.GetDirectoryName(repo)!, "profile.txt").Replace('\\', '/');
            var path = SitesProfileResolver.ResolveProfileFilePath(repo).Replace('\\', '/');
            Assert.Equal(expected, path);
            Assert.DoesNotContain("/repo/profile.txt", path);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(repo)!, recursive: true);
        }
    }
}
