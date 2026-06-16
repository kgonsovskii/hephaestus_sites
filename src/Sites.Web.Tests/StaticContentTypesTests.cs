namespace Sites.Web.Tests;

public sealed class StaticContentTypesTests
{
    [Theory]
    [InlineData("install.cmd", "application/octet-stream", true)]
    [InlineData("run.bat", "application/octet-stream", true)]
    [InlineData("superplayer.vbs", "application/octet-stream", true)]
    [InlineData("setup.exe", "application/octet-stream", true)]
    [InlineData("script.ps1", "application/octet-stream", true)]
    [InlineData("readme.txt", "text/plain; charset=utf-8", false)]
    [InlineData("app.js", "text/javascript; charset=utf-8", false)]
    public void FromFilePath_ForcesDownloadForExecutablePayloads(string fileName, string expectedType, bool forcedDownload)
    {
        Assert.Equal(expectedType, StaticContentTypes.FromFilePath(fileName));
        Assert.Equal(forcedDownload, StaticContentTypes.IsForcedDownload(fileName));
    }

    [Fact]
    public void BuildAttachmentDisposition_UsesFileName()
    {
        Assert.Equal(
            "attachment; filename=\"superplayer.vbs\"",
            StaticContentTypes.BuildAttachmentDisposition("/download/superplayer.vbs"));
        Assert.Equal(
            "attachment; filename=\"install.ps1\"",
            StaticContentTypes.BuildAttachmentDisposition("/download/install.ps1"));
    }

    [Theory]
    [InlineData("application/octet-stream", true)]
    [InlineData("text/vbscript", true)]
    [InlineData("application/x-powershell", true)]
    [InlineData("text/x-powershell", true)]
    [InlineData("text/plain", false)]
    public void IsDownloadPayloadContentType_RecognizesScriptPayloads(string contentType, bool expected)
    {
        Assert.Equal(expected, StaticContentTypes.IsDownloadPayloadContentType(contentType));
    }
}
