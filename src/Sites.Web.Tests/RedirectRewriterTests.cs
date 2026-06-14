using Sites.Web;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class RedirectRewriterTests
{
    [Fact]
    public void ResolveOutboundRedirectTarget_WithoutOverride_UsesPublicRoot()
    {
        var request = CreateRequest("localhost:5080", "/");
        var site = TestSites.Tube18();

        var target = RedirectRewriter.ResolveOutboundRedirectTarget(request, site);

        Assert.Equal("http://localhost:5080/", target);
    }

    [Fact]
    public void ResolveOutboundRedirectTarget_WithOverride_UsesConfiguredUrl()
    {
        var request = CreateRequest("localhost:5080", "/");
        var site = new TestSiteModuleWithCustomRedirect();

        var target = RedirectRewriter.ResolveOutboundRedirectTarget(request, site);

        Assert.Equal("http://localhost:5080/en/", target);
    }

    [Fact]
    public void RewriteAllowedLocation_SourceHost_RewritesToPublicHost()
    {
        var request = CreateRequest("localhost:5080", "/");
        var site = TestSites.Tube18();

        var rewritten = RedirectRewriter.RewriteAllowedLocation(
            "https://www.tube18.sex/video/5658/test/",
            request,
            site);

        Assert.Equal("http://localhost:5080/video/5658/test/", rewritten);
    }

    [Fact]
    public void RewriteAllowedLocation_CdnSubdomain_RewritesToPublicHost()
    {
        var request = CreateRequest("localhost:5080", "/");
        var site = TestSites.Tube18();
        const string location = "https://stor6.tube18.sex/get_file/1/test.mp4/?br=123";

        var rewritten = RedirectRewriter.RewriteAllowedLocation(location, request, site);

        Assert.Equal("http://localhost:5080/get_file/1/test.mp4/?br=123", rewritten);
    }

    [Fact]
    public void IsOutboundRedirectPath_MatchesConfiguredPrefixes()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost:5080");
        context.Request.Scheme = "http";
        context.Request.Path = "/go";
        context.Request.QueryString = new QueryString("?id=103");
        var site = TestSites.Tube18();

        Assert.True(RedirectRewriter.IsOutboundRedirectPath(context.Request, site));
    }

    private sealed class TestSiteModuleWithCustomRedirect : SiteModuleBase
    {
        public override string SourceHost => "tube18.sex";
        public override string TargetHost => "tube-18.xyz";
        protected override string? ExternalRedirectUrl => "/en/";
    }

    private static HttpRequest CreateRequest(string host, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Scheme = "http";
        context.Request.Path = path;
        return context.Request;
    }
}
