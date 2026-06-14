using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sites.RemoteDeploy;
using Sites.Web.Abstractions;

namespace Sites.Cp.Services;

public sealed class SitesCloneService
{
    private readonly DeployOptions _options;
    private readonly ILogger<SitesCloneService> _logger;

    public SitesCloneService(IOptions<DeployOptions> options, ILogger<SitesCloneService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SitesCloneResult> CloneToHostAsync(
        string profile,
        string host,
        string user,
        string password,
        Action<string>? onLine = null,
        CancellationToken cancellationToken = default)
    {
        var profileName = SitesProfileResolver.NormalizeProfileName(profile);
        var repoRoot = RepositoryPaths.ResolveRoot();

        var scriptPath = Path.Combine(
            RepositoryPaths.DeployDirectory(repoRoot),
            RemoteDeployRunner.DefaultRemoteScriptFileName);
        var bootstrap = RemoteDeployRunner.LoadRemoteInstallBootstrapScript(scriptPath);
        var remoteScript = RemoteDeployRunner.PrependDeployExports(_options, bootstrap, profileName);
        var log = new List<string>();

        void AddLine(string line)
        {
            log.Add(line);
            onLine?.Invoke(line);
        }

        var sshpass = await RemoteDeployRunner.EnsureSshPassAsync(AddLine, cancellationToken);
        var exitCode = await RemoteDeployRunner.RunRemoteBashAsync(
            sshpass,
            host.Trim(),
            user.Trim(),
            password,
            remoteScript,
            (line, _) =>
            {
                AddLine(line);
                return Task.CompletedTask;
            },
            cancellationToken);

        _logger.LogInformation(
            "Clone to {User}@{Host} profile {Profile} finished with exit {ExitCode}",
            user,
            host,
            profileName,
            exitCode);

        return new SitesCloneResult
        {
            ExitCode = exitCode,
            Profile = profileName,
            Host = host.Trim(),
            Log = log
        };
    }
}

public sealed class SitesCloneResult
{
    public int ExitCode { get; init; }

    public string Profile { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public IReadOnlyList<string> Log { get; init; } = [];
}
