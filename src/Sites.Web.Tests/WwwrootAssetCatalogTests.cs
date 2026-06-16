using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class WwwrootAssetCatalogTests
{
    [Fact]
    public void Scan_MapsFilesToUrlPaths()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "tube-18.xyz/player/kt_player.js", "player");
            WriteFile(root, "tube-18.xyz/videoscript.js", "script");
        });

        try
        {
            var assets = WwwrootAssetCatalog.Scan(webRoot, "tube-18.xyz");

            Assert.Equal("player/kt_player.js", assets["/player/kt_player.js"]);
            Assert.Equal("videoscript.js", assets["/videoscript.js"]);
            Assert.False(assets.ContainsKey("/player/kt_player.patch.js"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void Build_MergesOverridesOntoScan()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "veryoldgames.xyz/img/layout/logo.png", "png");
        });

        try
        {
            var assets = WwwrootAssetCatalog.Build(
                webRoot,
                "veryoldgames.xyz",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/img/layout/logo-vog.png"] = "img/layout/logo.png"
                });

            Assert.Equal("img/layout/logo.png", assets["/img/layout/logo.png"]);
            Assert.Equal("img/layout/logo.png", assets["/img/layout/logo-vog.png"]);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void Scan_ExcludesInternalFiles()
    {
        var webRoot = CreateWebRoot(root =>
        {
            WriteFile(root, "tube-18.xyz/videoscript.js", "ok");
            WriteFile(root, "tube-18.xyz/player/kt_player.patch.js", "patch");
            WriteFile(root, "tube-18.xyz/README.txt", "docs");
        });

        try
        {
            var assets = WwwrootAssetCatalog.Scan(webRoot, "tube-18.xyz");

            Assert.True(assets.ContainsKey("/videoscript.js"));
            Assert.False(assets.ContainsKey("/player/kt_player.patch.js"));
            Assert.False(assets.ContainsKey("/README.txt"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
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
}
