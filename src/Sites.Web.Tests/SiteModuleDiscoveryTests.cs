using System.Text.Json;
using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SiteModuleDiscoveryTests
{
    [Fact]
    public void DiscoverSites_BuildsJsonSiteModulesFromTargetHostKeys()
    {
        var sitesJson = WriteSitesJson(new Dictionary<string, object>
        {
            ["tube-18.xyz"] = new { sourceHost = "tube18.sex", disableCaching = true },
            ["veryoldgames.xyz"] = new { sourceHost = "bestoldgames.net" },
            ["mirror.example"] = new
            {
                sourceHost = "newsite.example",
                disableCaching = true,
                outboundRedirectPathPrefixes = new[] { "/out" }
            }
        });

        try
        {
            var sites = SiteModuleDiscovery.DiscoverSites(typeof(SitesModulesAnchor).Assembly, sitesJson);

            Assert.Equal(3, sites.Count);
            Assert.All(sites, site => Assert.IsType<JsonSiteModule>(site));

            var tube18 = sites.Single(site => site.TargetHost == "tube-18.xyz");
            Assert.Equal("tube18.sex", tube18.SourceHost);
            Assert.True(tube18.Rules.DisableCaching);

            var jsonOnly = sites.Single(site => site.SourceHost == "newsite.example");
            Assert.Equal("mirror.example", jsonOnly.TargetHost);
            Assert.Equal(["/out"], jsonOnly.Rules.OutboundRedirectPathPrefixes);
        }
        finally
        {
            File.Delete(sitesJson);
        }
    }

    private static string WriteSitesJson(Dictionary<string, object> sites)
    {
        var path = Path.Combine(Path.GetTempPath(), "sites-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(sites));
        return path;
    }
}
