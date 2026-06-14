using Sites.CertMaintenance;
using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class PublishDomainDiscoveryTests
{
    [Fact]
    public void DiscoverFromRegistry_CollectsActiveSiteTargetHosts()
    {
        var sitesJson = Path.Combine(Path.GetTempPath(), "sites-domains-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(sitesJson, """
            {
              "tube-18.xyz": { "sourceHost": "tube18.sex" },
              "veryoldgames.xyz": { "sourceHost": "bestoldgames.net" }
            }
            """);

        try
        {
            var sites = SiteModuleDiscovery.DiscoverSites(typeof(SitesModulesAnchor).Assembly, sitesJson);
            var registry = new SiteRegistry(sites, selectedSiteName: null);
            var domains = PublishDomainDiscovery.DiscoverFromRegistry(registry);

            Assert.Contains("tube-18.xyz", domains);
            Assert.Contains("www.tube-18.xyz", domains);
            Assert.Contains("veryoldgames.xyz", domains);
            Assert.Equal(4, domains.Count);
        }
        finally
        {
            File.Delete(sitesJson);
        }
    }
}
