using Microsoft.Extensions.Options;

namespace Sites.DataFtp;

public sealed class SitesDataFtpUrlProvider : ISitesDataFtpUrlProvider
{
    private readonly ISitesWebRootPathProvider _webPaths;
    private readonly IOptions<SitesDataFtpOptions> _options;

    public SitesDataFtpUrlProvider(
        ISitesWebRootPathProvider webPaths,
        IOptions<SitesDataFtpOptions> options)
    {
        _webPaths = webPaths;
        _options = options;
    }

    public string WebRootFullPath => _webPaths.WebRootFullPath;

    public int Port => _options.Value.Port;

    public string BuildUrl(string hostName)
    {
        var host = hostName.Trim();
        if (host.Length == 0)
            host = "localhost";

        var port = Port;
        var user = Uri.EscapeDataString(SitesDataFtpConstants.UserName);
        var pass = Uri.EscapeDataString(SitesDataFtpConstants.Password);
        var authority = port == 21
            ? $"{user}:{pass}@{host}"
            : $"{user}:{pass}@{host}:{port}";
        return $"ftp://{authority}/";
    }
}
