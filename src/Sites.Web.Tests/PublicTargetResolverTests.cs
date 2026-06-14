using Sites.Web;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class PublicTargetResolverTests
{
    [Fact]
    public void Tube18Site_Name_ComesFromSourceHost()
    {
        var site = TestSites.Tube18();

        Assert.Equal("tube18", site.Name);
    }

    [Fact]
    public void Resolve_LocalhostRequest_UsesRequestHostForRewrites()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost:5080");
        context.Request.Scheme = "http";

        var (baseUrl, host) = PublicTargetResolver.Resolve(context.Request, TestSites.Tube18());

        Assert.Equal("http://localhost:5080", baseUrl);
        Assert.Equal("localhost:5080", host);
    }

    [Fact]
    public void Resolve_ProductionTargetHost_UsesSiteTargetBaseUrl()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tube-18.xyz");
        context.Request.Scheme = "https";

        var (baseUrl, host) = PublicTargetResolver.Resolve(context.Request, TestSites.Tube18());

        Assert.Equal("https://tube-18.xyz", baseUrl);
        Assert.Equal("tube-18.xyz", host);
    }
}
