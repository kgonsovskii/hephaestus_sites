using Sites.Web;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;
public sealed class ContentRewriterTests
{
    [Fact]
    public void Rewrite_PornModule_RewritesCdnSubdomainAndMainHost()
    {
        var site = TestSites.Tube18();
        var replacements = SiteContentReplacements.BuildDefaults(
            "tube18.sex",
            "www.tube18.sex",
            "http://localhost:5080",
            "localhost:5080");
        const string html =
            """<a href="https://www.tube18.sex/v">main</a><video src="https://stor6.tube18.sex/get_file/1.mp4">""";

        var result = ContentRewriter.Rewrite(html, replacements);

        Assert.Contains("https://stor6.localhost:5080/get_file/1.mp4", result);
        Assert.Contains("http://localhost:5080/v", result);
        Assert.DoesNotContain("https://stor6.tube18.sex", result);
    }

    [Fact]
    public void JsonSiteModule_LocalhostReplacements_UseRequestHostNotTargetDomain()
    {
        var site = TestSites.Tube18();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("127.0.0.1:5080");
        context.Request.Scheme = "http";

        var (baseUrl, host) = PublicTargetResolver.Resolve(context.Request, site);
        var replacements = SiteContentReplacements.BuildDefaults(
            site.SourceHost,
            site.SourceUpstreamHost,
            baseUrl,
            host);

        const string html = """<img src="https://www.tube18.sex/pic.jpg">""";
        var result = ContentRewriter.Rewrite(html, replacements);

        Assert.Contains("http://127.0.0.1:5080/pic.jpg", result);
        Assert.DoesNotContain("tube-18.xyz", result);
    }

    [Fact]
    public void Rewrite_PornModule_RewritesGetFileToProxy()
    {
        var site = TestSites.Tube18();
        var replacements = SiteContentReplacements.BuildDefaults(
            "tube18.sex",
            "www.tube18.sex",
            "http://localhost:5080",
            "localhost:5080");
        const string html =
            """<a href="https://www.tube18.sex/video/1">v</a>https://www.tube18.sex/get_file/4/test.mp4""";

        var result = ContentRewriter.Rewrite(html, replacements);

        Assert.Contains("http://localhost:5080/get_file/4/test.mp4", result);
        Assert.Contains("http://localhost:5080/video/1", result);
        Assert.DoesNotContain("https://www.tube18.sex/get_file/", result);
    }
}
