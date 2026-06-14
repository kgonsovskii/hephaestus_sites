using Sites.Web;
using Sites.Web.Caching;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class ProxyCachePolicyTests
{
    private static SitesProfileSettingsService CreateSettingsService(Action<ProxyCacheOptions>? configure = null)
    {
        var path = Path.Combine(Path.GetTempPath(), "sites-settings-test-" + Guid.NewGuid().ToString("N") + ".json");
        var template = TestSitesProxyOptions.CreateTemplate(options => configure?.Invoke(options.Cache));
        return new SitesProfileSettingsService(template, path);
    }

    private static ProxyCachePolicy CreatePolicy(Action<ProxyCacheOptions>? configure = null) =>
        new(CreateSettingsService(configure));

    [Theory]
    [InlineData("/images/thumb.jpg", null, true)]
    [InlineData("/css/styles.min.css", null, true)]
    [InlineData("/get_file/4/foo/5658/5658.mp4", "?v-acctoken=abc", true)]
    [InlineData("/remote_control.php", "?file=a.mp4&acctoken=abc", true)]
    public void IsCacheableRequest_AllowsGetPathsRegardlessOfVideoLikeUrl(string path, string? query, bool expected)
    {
        var policy = CreatePolicy();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        if (query is not null)
            context.Request.QueryString = new QueryString(query);

        Assert.Equal(expected, policy.IsCacheableRequest(context.Request));
    }

    [Fact]
    public void IsCacheableRequest_RejectsRangeRequestsWhenConfigured()
    {
        var policy = CreatePolicy(options => options.RejectRangeRequests = true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/images/thumb.jpg";
        context.Request.Headers.Range = "bytes=0-";

        Assert.False(policy.IsCacheableRequest(context.Request));
    }

    [Theory]
    [InlineData("video/mp4", false)]
    [InlineData("application/vnd.apple.mpegurl", false)]
    [InlineData("image/jpeg", true)]
    [InlineData("text/css", true)]
    [InlineData("text/html", true)]
    public void MightBeCacheableContentType_UsesExcludedContentTypesFromSettings(string contentType, bool expected)
    {
        var policy = CreatePolicy();
        Assert.Equal(expected, policy.MightBeCacheableContentType(contentType));
    }

    [Fact]
    public void GetTtl_AlwaysUsesConfiguredTtl()
    {
        var policy = CreatePolicy(options => options.Ttl = TimeSpan.FromDays(7));

        Assert.Equal(TimeSpan.FromDays(7), policy.GetTtl());
        Assert.Equal(TimeSpan.FromDays(7), policy.Ttl);
    }

    [Fact]
    public void IsCacheableResponse_RejectsExcludedContentTypes()
    {
        var policy = CreatePolicy();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        Assert.False(policy.IsCacheableResponse(context.Request, StatusCodes.Status200OK, "video/mp4", 1024));
        Assert.True(policy.IsCacheableResponse(context.Request, StatusCodes.Status200OK, "text/css", 1024));
    }

    [Fact]
    public async Task ProxyDiskCache_StoresAndReadsPerSourceSite()
    {
        var root = Path.Combine(Path.GetTempPath(), "hephaestus-cache-test-" + Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(root, "settings.json");
        Directory.CreateDirectory(root);
        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var cache = new ProxyDiskCache(new SitesProfileSettingsService(template, settingsPath));

        try
        {
            var body = "cached-image-bytes"u8.ToArray();
            await cache.TrySetAsync(
                "tube18.sex",
                "abc123",
                StatusCodes.Status200OK,
                "image/jpeg",
                body,
                TimeSpan.FromDays(7));

            var hit = await cache.TryGetAsync("tube18.sex", "abc123");
            var miss = await cache.TryGetAsync("other.site", "abc123");

            Assert.NotNull(hit);
            Assert.Equal(body, hit!.Body);
            Assert.Null(miss);
            Assert.True(Directory.Exists(Path.Combine(root, "tube18.sex")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ProxyCacheRoot_UsesPlatformDefault()
    {
        var root = ProxyCacheRoot.Resolve();

        if (OperatingSystem.IsWindows())
            Assert.Equal(@"C:\_cache", root);
        else
            Assert.Equal("/_cache", root);
    }
}
