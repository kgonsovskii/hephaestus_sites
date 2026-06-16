using Sites.Web;
using Sites.Web.Caching;

namespace Sites.Web.Tests;

public sealed class ProxyDiskCacheClearTests
{
    [Fact]
    public void ClearAll_RemovesEverySiteCacheFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-cache-clear-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tube18.sex", "ab"));
        Directory.CreateDirectory(Path.Combine(root, "bestoldgames.net", "cd"));
        File.WriteAllText(Path.Combine(root, "tube18.sex", "ab", "key.meta"), "{}");

        var settingsPath = Path.Combine(root, "settings.json");
        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var cache = new ProxyDiskCache(new SitesProfileSettingsService(template, settingsPath));

        try
        {
            var result = cache.ClearAll();

            Assert.Equal(root, result.CacheRoot);
            Assert.Equal(2, result.RemovedEntries);
            Assert.False(Directory.Exists(Path.Combine(root, "tube18.sex")));
            Assert.False(Directory.Exists(Path.Combine(root, "bestoldgames.net")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ClearNonBinary_RemovesTextEntries_KeepsVideo()
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-cache-clear-nb-" + Guid.NewGuid().ToString("N"));
        var siteDir = Path.Combine(root, "tube18.sex", "ab");
        Directory.CreateDirectory(siteDir);

        WriteEntry(siteDir, "html1", "text/html", "<html></html>");
        WriteEntry(siteDir, "js1", "application/javascript", "console.log(1)");
        WriteEntry(siteDir, "vid1", "video/mp4", "binary");

        var settingsPath = Path.Combine(root, "settings.json");
        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var settings = new SitesProfileSettingsService(template, settingsPath);
        var cache = new ProxyDiskCache(settings);
        var policy = new ProxyCachePolicy(settings);

        try
        {
            var result = cache.ClearNonBinary(policy);

            Assert.Equal(root, result.CacheRoot);
            Assert.Equal(2, result.RemovedEntries);
            Assert.False(File.Exists(Path.Combine(siteDir, "html1.meta")));
            Assert.False(File.Exists(Path.Combine(siteDir, "js1.meta")));
            Assert.True(File.Exists(Path.Combine(siteDir, "vid1.meta")));
            Assert.True(File.Exists(Path.Combine(siteDir, "vid1.body")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("application/octet-stream", "vbs1")]
    [InlineData("text/vbscript", "vbsLegacy")]
    [InlineData("application/x-powershell", "ps1Legacy")]
    public void ClearNonBinary_RemovesDownloadPayloadEntries(string contentType, string key)
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-cache-clear-dl-" + Guid.NewGuid().ToString("N"));
        var siteDir = Path.Combine(root, "tube18.sex", "ab");
        Directory.CreateDirectory(siteDir);

        WriteEntry(siteDir, key, contentType, "payload");
        WriteEntry(siteDir, "vid1", "video/mp4", "binary");

        var settingsPath = Path.Combine(root, "settings.json");
        var template = TestSitesProxyOptions.CreateTemplate(options => options.Cache.RootPath = root);
        var cache = new ProxyDiskCache(new SitesProfileSettingsService(template, settingsPath));
        var policy = new ProxyCachePolicy(new SitesProfileSettingsService(template, settingsPath));

        try
        {
            var result = cache.ClearNonBinary(policy);

            Assert.Equal(1, result.RemovedEntries);
            Assert.False(File.Exists(Path.Combine(siteDir, $"{key}.meta")));
            Assert.False(File.Exists(Path.Combine(siteDir, $"{key}.body")));
            Assert.True(File.Exists(Path.Combine(siteDir, "vid1.meta")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteEntry(string siteDir, string key, string contentType, string body)
    {
        var metaPath = Path.Combine(siteDir, $"{key}.meta");
        var bodyPath = Path.Combine(siteDir, $"{key}.body");
        File.WriteAllText(metaPath,
            $$"""{"statusCode":200,"contentType":"{{contentType}}","expiresAt":"2099-01-01T00:00:00Z","bodyLength":1}""");
        File.WriteAllText(bodyPath, body);
    }
}
