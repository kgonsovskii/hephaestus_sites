using System.Text.Json;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sites.Web.Tests;

public sealed class LocalAssetsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ServesScannedAssetAtOriginalPath()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "tube-18.xyz/js/app.js", "console.log('ok');");
        });

        try
        {
            var context = CreateContext(
                "/js/app.js",
                webRoot,
                targetHost: "tube-18.xyz",
                rules: new SiteProxyRules
                {
                    LocalAssets = WwwrootAssetCatalog.Scan(webRoot, "tube-18.xyz")
                });

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await CreateMiddleware(next).InvokeAsync(context);

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
    public async Task InvokeAsync_ServesAliasMapping()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "veryoldgames.xyz/img/layout/logo.png", "png");
        });

        try
        {
            var context = CreateContext(
                "/img/layout/logo-vog.png",
                webRoot,
                targetHost: "veryoldgames.xyz",
                rules: new SiteProxyRules
                {
                    LocalAssets = WwwrootAssetCatalog.Build(
                        webRoot,
                        "veryoldgames.xyz",
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["/img/layout/logo-vog.png"] = "img/layout/logo.png"
                        })
                });

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await CreateMiddleware(next).InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("image/png", context.Response.ContentType);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_ServesVbsWithVbscriptContentType()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "example.xyz/launcher.vbs", "WScript.Echo \"ok\"");
        });

        try
        {
            var context = CreateContext(
                "/launcher.vbs",
                webRoot,
                targetHost: "example.xyz",
                rules: new SiteProxyRules
                {
                    LocalAssets = WwwrootAssetCatalog.Scan(webRoot, "example.xyz")
                });

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await CreateMiddleware(next).InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/octet-stream", context.Response.ContentType);
            Assert.Equal("attachment; filename=\"launcher.vbs\"", context.Response.Headers.ContentDisposition.ToString());
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            Assert.Equal("WScript.Echo \"ok\"", await reader.ReadToEndAsync());
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_ServesCmdWithPlainTextContentType()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "example.xyz/download/install.cmd", "@echo off\r\necho ok");
        });

        try
        {
            var context = CreateContext(
                "/download/install.cmd",
                webRoot,
                targetHost: "example.xyz",
                rules: new SiteProxyRules
                {
                    LocalAssets = WwwrootAssetCatalog.Scan(webRoot, "example.xyz")
                });

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            await CreateMiddleware(next).InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/octet-stream", context.Response.ContentType);
            Assert.Equal("attachment; filename=\"install.cmd\"", context.Response.Headers.ContentDisposition.ToString());
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            Assert.Equal("@echo off\r\necho ok", await reader.ReadToEndAsync());
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_ReplacesSettingsPlaceholdersInJs()
    {
        LocalJsTransformCache.ClearForTests();

        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "tube-18.xyz/videoscript.js", "var interval = $VideoInterval$;");
        });

        try
        {
            var context = CreateContext(
                "/videoscript.js",
                webRoot,
                targetHost: "tube-18.xyz",
                rules: new SiteProxyRules
                {
                    LocalAssets = WwwrootAssetCatalog.Scan(webRoot, "tube-18.xyz"),
                    Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["VideoInterval"] = "30"
                    }
                });

            await CreateMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            Assert.Equal("var interval = 30;", body);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void JsonSiteModule_BuildRules_ScansWwwrootOnConstruction()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "tube-18.xyz/videoscript.js", "ok");
            WriteFile(root, "tube-18.xyz/player/kt_player.js", "player");
        });

        try
        {
            var module = new JsonSiteModule(
                new SiteDefinition
                {
                    SourceHost = "tube18.sex",
                    TargetHost = "tube-18.xyz",
                    Settings = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["VideoInterval"] = JsonSerializer.Deserialize<JsonElement>("30")
                    }
                },
                webRoot);

            Assert.Equal("30", module.Rules.Settings["VideoInterval"]);
            Assert.Equal("videoscript.js", module.Rules.LocalAssets["/videoscript.js"]);
            Assert.Equal("player/kt_player.js", module.Rules.LocalAssets["/player/kt_player.js"]);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    private static LocalAssetsMiddleware CreateMiddleware(RequestDelegate next)
    {
        var settings = new SitesProfileSettingsService(TestSitesProxyOptions.CreateTemplate());
        return new LocalAssetsMiddleware(next, NullLogger<LocalAssetsMiddleware>.Instance, settings);
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

    private static string CreateWebRoot(Action<string> setup)
    {
        var webRoot = Path.Combine(Path.GetTempPath(), "sites-wwwroot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(webRoot);
        setup(webRoot);
        return webRoot;
    }

    private static void WriteFile(string webRoot, string relativePath, string content)
    {
        var fullPath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
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
