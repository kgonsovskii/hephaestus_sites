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
}
