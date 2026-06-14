using Sites.Web;
using Sites.Web.Caching;

namespace Sites.Web.Tests;

public sealed class SitesProfileSettingsValidatorTests
{
    [Fact]
    public void Validate_FailsWhenTtlMissing()
    {
        var options = ValidOptions();
        options.Cache.Ttl = TimeSpan.Zero;

        var ex = Assert.Throws<InvalidOperationException>(() => SitesProfileSettingsValidator.Validate(options));
        Assert.Contains("Ttl", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_FailsWhenExcludedContentTypesEmpty()
    {
        var options = ValidOptions();
        options.Cache.ExcludedContentTypes = [];

        var ex = Assert.Throws<InvalidOperationException>(() => SitesProfileSettingsValidator.Validate(options));
        Assert.Contains("ExcludedContentTypes", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_SucceedsForConfiguredOptions()
    {
        SitesProfileSettingsValidator.Validate(ValidOptions());
    }

    private static SitesProxyOptions ValidOptions() => TestSitesProxyOptions.Create();
}
