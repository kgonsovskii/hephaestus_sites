using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Controllers;
using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Caching;

namespace Sites.Web.Tests;

public sealed class SitesApiSaveClearsTextCacheTests
{
    [Fact]
    public void UpdateSite_ClearsHtmlCache_KeepsVideo()
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-save-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var jsonPath = Path.Combine(root, "sites.json");
        File.WriteAllText(jsonPath, "{}");

        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var settings = new SitesProfileSettingsService(template, Path.Combine(root, "settings.json"));
        var cache = new ProxyDiskCache(settings);
        var policy = new ProxyCachePolicy(settings);
        var catalog = new SitesCatalogService(typeof(SitesModulesAnchor).Assembly, new SiteRegistry([], null), jsonPath);
        catalog.Create("mirror.example", new SiteDefinition { SourceHost = "upstream.example" });

        var siteDir = Path.Combine(root, "upstream.example", "ab");
        Directory.CreateDirectory(siteDir);
        WriteEntry(siteDir, "html1", "text/html", "<html>old</html>");
        WriteEntry(siteDir, "vid1", "video/mp4", "binary");

        var result = new SitesApiController(catalog, cache, policy).Update(
            "mirror.example",
            new SiteDefinition { SourceHost = "upstream.example" });

        try
        {
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.False(File.Exists(Path.Combine(siteDir, "html1.meta")));
            Assert.False(File.Exists(Path.Combine(siteDir, "html1.body")));
            Assert.True(File.Exists(Path.Combine(siteDir, "vid1.meta")));
            Assert.True(File.Exists(Path.Combine(siteDir, "vid1.body")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteEntry(string siteDir, string key, string contentType, string body)
    {
        File.WriteAllText(
            Path.Combine(siteDir, $"{key}.meta"),
            $$"""{"statusCode":200,"contentType":"{{contentType}}","expiresAt":"2099-01-01T00:00:00Z","bodyLength":1}""");
        File.WriteAllText(Path.Combine(siteDir, $"{key}.body"), body);
    }
}
