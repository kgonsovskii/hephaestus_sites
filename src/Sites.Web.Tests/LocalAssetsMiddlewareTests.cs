using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sites.Web.Tests;

public sealed class LocalAssetsMiddlewareTests
{
    [Theory]
    [InlineData("/x/app.js", "x/app.js")]
    [InlineData("/x/nested/app.js", "x/nested/app.js")]
    [InlineData("/x/", "x/index.html")]
    public void TryGetAdditionsRelativePath_MapsUnderPrefix(string requestPath, string expectedRelative)
    {
        var matched = LocalAssetsMiddleware.TryGetAdditionsRelativePath(
            requestPath,
            "/x/",
            out var relativePath);

        Assert.True(matched);
        Assert.Equal(expectedRelative, relativePath);
    }

    [Theory]
    [InlineData("/x/app.js", "tube-18.xyz/app.js")]
    [InlineData("/x/js/inject.js", "tube-18.xyz/js/inject.js")]
    [InlineData("/x/", "tube-18.xyz/index.html")]
    public void TryGetAdditionsFileRelativePath_MapsToDomainFolder(string requestPath, string expectedRelative)
    {
        var matched = LocalAssetsMiddleware.TryGetAdditionsFileRelativePath(
            requestPath,
            "/x/",
            "tube-18.xyz",
            out var relativePath);

        Assert.True(matched);
        Assert.Equal(expectedRelative, relativePath);
    }

    [Fact]
    public async Task InvokeAsync_ServesAdditionFromDomainFolder()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), "sites-additions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(webRoot, "tube-18.xyz", "js"));
        await File.WriteAllTextAsync(Path.Combine(webRoot, "tube-18.xyz", "js", "app.js"), "console.log('ok');");

        try
        {
            var context = CreateContext("/x/js/app.js", webRoot, targetHost: "tube-18.xyz");
            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await new LocalAssetsMiddleware(next, NullLogger<LocalAssetsMiddleware>.Instance)
                .InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("text/javascript; charset=utf-8", context.Response.ContentType);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_RejectsPathTraversal()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), "sites-additions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(webRoot);

        try
        {
            var context = CreateContext("/x/../secret.js", webRoot);
            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await new LocalAssetsMiddleware(next, NullLogger<LocalAssetsMiddleware>.Instance)
                .InvokeAsync(context);

            Assert.True(nextCalled);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_ServesMappedLocalAssetOutsidePrefix()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), "sites-additions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(webRoot, "proxy.example", "img"));
        await File.WriteAllTextAsync(Path.Combine(webRoot, "proxy.example", "img", "logo.png"), "png");

        try
        {
            var context = CreateContext("/img/layout/logo.png", webRoot, rules: new SiteProxyRules
            {
                LocalAssets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/img/layout/logo.png"] = "img/logo.png"
                }
            });

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await new LocalAssetsMiddleware(next, NullLogger<LocalAssetsMiddleware>.Instance)
                .InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("image/png", context.Response.ContentType);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    private static DefaultHttpContext CreateContext(
        string path,
        string webRoot,
        SiteProxyRules? rules = null,
        string targetHost = "proxy.example")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.SetSite(new TestSiteModule(webRoot, rules ?? new SiteProxyRules(), targetHost));
        return context;
    }

    private sealed class TestSiteModule : ISiteModule
    {
        public TestSiteModule(string webRoot, SiteProxyRules rules, string targetHost)
        {
            WebRootPath = webRoot;
            Rules = rules;
            TargetHost = targetHost;
        }

        public string Name => "test";
        public string SourceBaseUrl => "https://www.example.com";
        public string SourceHost => "example.com";
        public string SourceUpstreamHost => "www.example.com";
        public string TargetHost { get; }
        public string TargetBaseUrl => $"https://{TargetHost}";
        public IReadOnlyList<string> TargetHosts => [TargetHost];
        public SiteProxyRules Rules { get; }
        public string WebRootPath { get; }
    }
}
