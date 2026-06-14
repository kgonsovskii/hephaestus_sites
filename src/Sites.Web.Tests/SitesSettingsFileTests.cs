using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesSettingsFileTests
{
    [Fact]
    public void LoadOrCreate_WritesDefaultsFromTemplateWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "sites-settings-" + Guid.NewGuid().ToString("N"));
        var profileDir = Path.Combine(root, "profiles", "default");
        Directory.CreateDirectory(profileDir);
        var path = Path.Combine(profileDir, SitesProfileResolver.SettingsJsonFileName);
        var template = TestSitesProxyOptions.CreateTemplate();

        try
        {
            var document = SitesSettingsFile.LoadOrCreate(path, template.CreateDocument());

            Assert.True(File.Exists(path));
            Assert.Equal(TimeSpan.FromDays(7), document.Sites.Cache.Ttl);
            Assert.Contains("video/", document.Sites.Cache.ExcludedContentTypes);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_RoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), "sites-settings-save-" + Guid.NewGuid().ToString("N") + ".json");
        var document = TestSitesProxyOptions.CreateTemplate().CreateDocument();
        document.Sites.UpstreamRequestTimeout = TimeSpan.FromMinutes(3);

        try
        {
            SitesSettingsFile.Save(path, document);
            var loaded = SitesSettingsFile.Load(path);

            Assert.Equal(TimeSpan.FromMinutes(3), loaded.Sites.UpstreamRequestTimeout);
            Assert.Equal(document.Sites.Cache.Ttl, loaded.Sites.Cache.Ttl);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
