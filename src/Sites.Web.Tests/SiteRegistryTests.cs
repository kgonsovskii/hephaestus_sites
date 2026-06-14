using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SiteRegistryTests
{
    [Fact]
    public void Constructor_SelectsSite_ByTargetHost()
    {
        var registry = new SiteRegistry([TestSites.Tube18()], "tube-18.xyz");

        Assert.True(registry.IsSingleSiteMode);
        Assert.Equal("tube-18.xyz", registry.ActiveSites[0].TargetHost);
    }

    [Fact]
    public void Constructor_SelectsSite_ByWwwTargetHost()
    {
        var registry = new SiteRegistry([TestSites.Tube18()], "www.tube-18.xyz");

        Assert.True(registry.IsSingleSiteMode);
        Assert.Equal("tube-18.xyz", registry.ActiveSites[0].TargetHost);
    }

    [Fact]
    public void Constructor_Throws_WhenSelectionUsesModuleName()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SiteRegistry([TestSites.Tube18()], "tube18"));
    }

    [Fact]
    public void ReplaceSites_InSingleSiteMode_ReloadsSelectedSite()
    {
        var registry = new SiteRegistry([TestSites.Tube18()], "tube-18.xyz");
        var updated = new JsonSiteModule(new SiteDefinition
        {
            SourceHost = "tube18.sex",
            TargetHost = "tube-18.xyz",
            DisableCaching = true
        });

        registry.ReplaceSites([updated]);

        Assert.True(registry.IsSingleSiteMode);
        Assert.True(registry.ActiveSites[0].DisableCaching);
    }
}
