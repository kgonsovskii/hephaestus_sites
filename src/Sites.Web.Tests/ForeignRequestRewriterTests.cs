using Sites.Web;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class ForeignRequestRewriterTests
{
    [Fact]
    public void RewriteForeignLinks_ReplacesAffiliateHref()
    {
        const string html =
            """<a href="https://go.rmhfrtnd.com/easy?campaignId=1">Live Sex</a>""";
        var request = CreateRequest("tube-18.xyz", "/video/1/");
        var site = CreateSite(redirectForeignRequests: true);

        var rewritten = ForeignRequestRewriter.RewriteForeignLinks(html, request, site);

        Assert.Equal("""<a href="https://tube-18.xyz/">Live Sex</a>""", rewritten);
    }

    [Fact]
    public void RewriteForeignLinks_LeavesSourceHostUntouched()
    {
        const string html = """<a href="https://www.tube18.sex/video/1/">Video</a>""";
        var request = CreateRequest("tube-18.xyz", "/");
        var site = CreateSite(redirectForeignRequests: true);

        var rewritten = ForeignRequestRewriter.RewriteForeignLinks(html, request, site);

        Assert.Equal(html, rewritten);
    }

    [Fact]
    public void RewriteForeignLinks_UsesConfiguredUrl()
    {
        const string html = """<a href="https://example.com/out">Go</a>""";
        var request = CreateRequest("tube-18.xyz", "/");
        var site = CreateSite(redirectForeignRequests: true, redirectForeignRequestsUrl: "/en/");

        var rewritten = ForeignRequestRewriter.RewriteForeignLinks(html, request, site);

        Assert.Equal("""<a href="https://tube-18.xyz/en/">Go</a>""", rewritten);
    }

    [Fact]
    public void RewriteAllowedLocation_ForeignLocation_RewritesToHome()
    {
        var request = CreateRequest("tube-18.xyz", "/");
        var site = CreateSite(redirectForeignRequests: true);

        var rewritten = RedirectRewriter.RewriteAllowedLocation(
            "https://go.rmhfrtnd.com/easy?campaignId=1",
            request,
            site);

        Assert.Equal("https://tube-18.xyz/", rewritten);
    }

    [Fact]
    public void IsOutboundRedirectPath_RespectsEnableFlag()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tube-18.xyz");
        context.Request.Scheme = "https";
        context.Request.Path = "/go";
        var site = CreateSite(enableOutboundRedirectPaths: false);

        Assert.False(RedirectRewriter.IsOutboundRedirectPath(context.Request, site));
    }

    private static JsonSiteModule CreateSite(
        bool redirectForeignRequests = false,
        string? redirectForeignRequestsUrl = null,
        bool enableOutboundRedirectPaths = true) =>
        new(new SiteDefinition
        {
            SourceHost = "tube18.sex",
            TargetHost = "tube-18.xyz",
            EnableOutboundRedirectPaths = enableOutboundRedirectPaths,
            OutboundRedirectPathPrefixes = ["/go"],
            RedirectForeignRequests = redirectForeignRequests,
            RedirectForeignRequestsUrl = redirectForeignRequestsUrl
        });

    private static HttpRequest CreateRequest(string host, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Scheme = "https";
        context.Request.Path = path;
        return context.Request;
    }
}
