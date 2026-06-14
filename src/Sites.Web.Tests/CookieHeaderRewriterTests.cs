using Sites.Web;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class CookieHeaderRewriterTests
{
    [Fact]
    public void RewriteSetCookieForProxy_StripsUpstreamDomain()
    {
        var site = TestSites.Tube18();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tubepleasure.xyz");
        context.Request.Scheme = "https";

        var rewritten = CookieHeaderRewriter.RewriteSetCookieForProxy(
            "token=abc; Domain=.wildberries.ru; Path=/; HttpOnly; SameSite=None",
            site,
            context.Request);

        Assert.Contains("token=abc", rewritten);
        Assert.DoesNotContain("Domain=", rewritten, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", rewritten);
    }

    [Fact]
    public void RewriteSetCookieForProxy_PrefixesOnLocalhost()
    {
        var site = TestSites.Tube18();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost", 5080);

        var rewritten = CookieHeaderRewriter.RewriteSetCookieForProxy(
            "sessionId=abc; Path=/; HttpOnly",
            site,
            context.Request);

        Assert.StartsWith("__s.tube_18_xyz.sessionId=abc", rewritten);
    }

    [Fact]
    public void FilterCookiesForUpstream_OnlySendsNamespacedCookiesOnLocalhost()
    {
        var site = TestSites.Tube18();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost", 5080);

        var cookieHeader =
            "__s.tube_18_xyz.sessionId=abc; x_wbaas_token=leaked; __s.tubepleasure_xyz.other=1";

        var filtered = CookieHeaderRewriter.FilterCookiesForUpstream(cookieHeader, site, context.Request);

        Assert.Equal("sessionId=abc", filtered);
    }

    [Fact]
    public void FilterCookiesForUpstream_PassesThroughOnProductionHost()
    {
        var site = TestSites.Tube18();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tube-18.xyz");

        var cookieHeader = "sessionId=abc; x_wbaas_token=leaked";

        var filtered = CookieHeaderRewriter.FilterCookiesForUpstream(cookieHeader, site, context.Request);

        Assert.Equal(cookieHeader, filtered);
    }
}
