using Sites.Web;

namespace Sites.Web.Tests;

public sealed class JsAssetPatchTests
{
    [Fact]
    public void SiblingPatchPath_MapsJsToPatchJs()
    {
        var js = Path.Combine("wwwroot", "tubepleasure.xyz", "player", "kt_player.js");
        var patch = JsAssetPatch.SiblingPatchPath(js);

        Assert.Equal(Path.Combine("wwwroot", "tubepleasure.xyz", "player", "kt_player.patch.js"), patch);
    }

    [Fact]
    public void SiblingPatchPath_IgnoresPatchFiles()
    {
        var patch = Path.Combine("player", "kt_player.patch.js");
        Assert.Equal(string.Empty, JsAssetPatch.SiblingPatchPath(patch));
    }

    [Fact]
    public void Append_InsertsNewlineWhenMissing()
    {
        var result = JsAssetPatch.Append("window.kt_player=function(){};", "window.__patched=1;");
        Assert.Equal("window.kt_player=function(){};\nwindow.__patched=1;", result);
    }
}
