using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesCatalogServiceTests
{
    [Fact]
    public void CreateUpdateDelete_PersistsSitesJsonAndReloadsRegistry()
    {
        var jsonPath = Path.Combine(Path.GetTempPath(), "sites-catalog-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(jsonPath, "{}");

        var registry = new SiteRegistry([], selectedSiteName: null);
        var catalog = new SitesCatalogService(typeof(SitesModulesAnchor).Assembly, registry, jsonPath);

        try
        {
            catalog.Create("mirror.example", new SiteDefinition { SourceHost = "upstream.example" });
            var loaded = catalog.GetAll();
            Assert.Single(loaded);
            Assert.Equal("upstream.example", loaded["mirror.example"].SourceHost);
            Assert.Single(registry.ActiveSites);

            catalog.Update("mirror.example", new SiteDefinition
            {
                SourceHost = "upstream.example",
                DisableCaching = true
            });

            var updated = catalog.Get("mirror.example");
            Assert.NotNull(updated);
            Assert.True(updated.DisableCaching);
            Assert.Equal("mirror.example", registry.ActiveSites[0].TargetHost);

            catalog.Delete("mirror.example");
            Assert.Empty(catalog.GetAll());
            Assert.Empty(registry.ActiveSites);

            catalog.Create("reload.example", new SiteDefinition { SourceHost = "reload.example" });
            File.WriteAllText(jsonPath, "{}");
            var reloaded = catalog.ReloadRegistry();
            Assert.Equal(0, reloaded);
            Assert.Empty(registry.ActiveSites);
        }
        finally
        {
            File.Delete(jsonPath);
        }
    }
}
