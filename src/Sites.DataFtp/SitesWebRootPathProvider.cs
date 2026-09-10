using Microsoft.Extensions.Logging;
using Sites.Web.Abstractions;

namespace Sites.DataFtp;

public sealed class SitesWebRootPathProvider : ISitesWebRootPathProvider
{
    private readonly ILogger<SitesWebRootPathProvider> _logger;
    private string? _cachedProfile;
    private string? _cachedPath;

    public SitesWebRootPathProvider(ILogger<SitesWebRootPathProvider> logger) =>
        _logger = logger;

    public string WebRootFullPath
    {
        get
        {
            var profile = SitesProfileResolver.Current;
            if (_cachedProfile == profile && _cachedPath is not null)
                return _cachedPath;

            var repoRoot = RepositoryPaths.TryResolveRoot() ?? RepositoryPaths.ResolveRoot();
            _cachedPath = SitesProfileResolver.ResolveProfileDirectory(repoRoot, profile);
            _cachedProfile = profile;
            Directory.CreateDirectory(_cachedPath);
            _logger.LogInformation(
                "Sites FTP root (profile {Profile}): {FtpRoot}",
                profile,
                _cachedPath);
            return _cachedPath;
        }
    }
}
