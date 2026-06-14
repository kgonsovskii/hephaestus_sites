using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

internal static class TestSites
{
    public static JsonSiteModule Tube18() =>
        new(new SiteDefinition
        {
            SourceHost = "tube18.sex",
            TargetHost = "tube-18.xyz",
            DisableCaching = true,
            OutboundRedirectPathPrefixes = ["/go", "/out", "/click"]
        });
}
