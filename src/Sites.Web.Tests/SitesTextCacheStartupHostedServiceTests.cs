using Microsoft.Extensions.Logging.Abstractions;
using Sites.Web;
using Sites.Web.Caching;

namespace Sites.Web.Tests;

public sealed class SitesTextCacheStartupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ClearsHtmlCache_KeepsVideo()
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-startup-cache-" + Guid.NewGuid().ToString("N"));
        var siteDir = Path.Combine(root, "tube18.sex", "ab");
        Directory.CreateDirectory(siteDir);
        WriteEntry(siteDir, "html1", "text/html", "<html>old</html>");
        WriteEntry(siteDir, "vid1", "video/mp4", "binary");

        var settingsPath = Path.Combine(root, "settings.json");
        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var settings = new SitesProfileSettingsService(template, settingsPath);
        var cache = new ProxyDiskCache(settings);
        var policy = new ProxyCachePolicy(settings);
        var service = new SitesTextCacheStartupHostedService(
            cache,
            policy,
            NullLogger<SitesTextCacheStartupHostedService>.Instance);

        try
        {
            await service.StartAsync(CancellationToken.None);

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
