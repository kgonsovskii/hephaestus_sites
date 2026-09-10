using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesProfileSettingsTemplateTests
{
    [Fact]
    public void CreateDocument_UsesAppsettingsSitesSection()
    {
        var configuration = TestSitesProxyOptions.CreateConfiguration();
        var template = new SitesProfileSettingsTemplate(configuration);
        var document = template.CreateDocument();

        Assert.Equal(TimeSpan.FromDays(7), document.Sites.Cache.Ttl);
    }
}
