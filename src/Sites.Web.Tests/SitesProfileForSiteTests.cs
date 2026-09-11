using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesProfileForSiteTests
{
    [Fact]
    public void UseProfileThatContains_SwitchesToProfileWithSite()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "sites-for-site-" + Guid.NewGuid().ToString("N"));
        var repo = Path.Combine(sandbox, "hephaestus_sites");
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        File.WriteAllText(Path.Combine(repo, "src", "Sites.sln"), "");
        var defaultDir = Path.Combine(sandbox, "hephaestus_sites_data", "default");
        var gonzikDir = Path.Combine(sandbox, "hephaestus_sites_data", "gonzik");
        Directory.CreateDirectory(defaultDir);
        Directory.CreateDirectory(gonzikDir);
        File.WriteAllText(Path.Combine(defaultDir, "sites.json"), """{"other.xyz":{"sourceHost":"example.com"}}""");
        File.WriteAllText(Path.Combine(gonzikDir, "sites.json"), """{"insert-coin.xyz":{"sourceHost":"online.oldgames.sk"}}""");
        File.WriteAllText(Path.Combine(sandbox, "profile.txt"), "default");

        var previous = SitesProfileResolver.Current;
        try
        {
            SitesProfileResolver.Initialize(repo);
            Assert.Equal("default", SitesProfileResolver.Current);

            SitesProfileForSite.UseProfileThatContains("insert-coin.xyz", repo);

            Assert.Equal("gonzik", SitesProfileResolver.Current);
            Assert.True(SitesJsonFile.Load(SitesProfileResolver.ResolveSitesJsonPath(repo)).ContainsKey("insert-coin.xyz"));
        }
        finally
        {
            SitesProfileResolver.Use(previous);
            Directory.Delete(sandbox, recursive: true);
        }
    }
}
