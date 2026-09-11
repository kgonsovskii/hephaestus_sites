using Microsoft.AspNetCore.Http;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class UpstreamOriginRewriteTests
{
    [Fact]
    public void BuildUpstreamOrigin_RewritesPublicSiteToSource()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("insert-coin.xyz");
        context.Request.Scheme = "http";
        context.Request.Headers.Origin = "http://insert-coin.xyz";
        var site = CreateSite();

        var origin = ReverseProxyMiddleware.BuildUpstreamOrigin(context, site);

        Assert.Equal("https://online.oldgames.sk", origin);
    }

    [Fact]
    public void BuildUpstreamOrigin_LeavesMissingOriginUnset()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("insert-coin.xyz");
        context.Request.Scheme = "http";
        var site = CreateSite();

        Assert.Null(ReverseProxyMiddleware.BuildUpstreamOrigin(context, site));
    }

    [Fact]
    public void ShouldRewriteClientOriginToSource_MatchesTargetHost()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("insert-coin.xyz");
        context.Request.Scheme = "http";
        var site = CreateSite();

        Assert.True(ReverseProxyMiddleware.ShouldRewriteClientOriginToSource(
            new Uri("http://insert-coin.xyz/play/snes/"),
            context,
            site));
    }

    [Fact]
    public void EnsureUpstreamSecFetchSite_AddsSameOriginWhenMissing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://online.oldgames.sk/api/getrom/689");

        ReverseProxyMiddleware.EnsureUpstreamSecFetchSite(request);

        Assert.Equal("same-origin", request.Headers.GetValues("Sec-Fetch-Site").Single());
    }

    [Fact]
    public void EnsureUpstreamSecFetchSite_ReplacesCrossSite()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://online.oldgames.sk/api/getrom/689");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");

        ReverseProxyMiddleware.EnsureUpstreamSecFetchSite(request);

        Assert.Equal("same-origin", request.Headers.GetValues("Sec-Fetch-Site").Single());
    }

    private static JsonSiteModule CreateSite() =>
        new(new SiteDefinition
        {
            SourceHost = "online.oldgames.sk",
            TargetHost = "insert-coin.xyz"
        });
}
