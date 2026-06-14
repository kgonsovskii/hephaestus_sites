using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sites.Web.Abstractions;

namespace Sites.Cp.Services;

public sealed class SitesUpdateService
{
    private readonly ILogger<SitesUpdateService> _logger;

    public SitesUpdateService(ILogger<SitesUpdateService> logger) => _logger = logger;

    public SitesUpdateResult ScheduleUpdate()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new SitesUpdateResult
            {
                Succeeded = false,
                Message = "Self-update is only supported when Sites.Host runs on Linux."
            };
        }

        var repoRoot = RepositoryPaths.ResolveRoot();
        var updateScript = Path.Combine(repoRoot, "deploy", "update.sh");
        if (!File.Exists(updateScript))
        {
            return new SitesUpdateResult
            {
                Succeeded = false,
                Message = $"Missing script: {updateScript}"
            };
        }

        var profile = SitesProfileResolver.Current;
        var workDir = Path.Combine(repoRoot, "deploy");
        var systemdRun = new[] { "/usr/bin/systemd-run", "/bin/systemd-run" }
            .FirstOrDefault(File.Exists);

        ProcessStartInfo psi;
        if (systemdRun is not null)
        {
            psi = new ProcessStartInfo
            {
                FileName = systemdRun,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--no-block");
            psi.ArgumentList.Add("--collect");
            psi.ArgumentList.Add("--property=Description=Sites self-update");
            psi.ArgumentList.Add($"--working-directory={workDir}");
            psi.ArgumentList.Add($"--setenv=SITES_CLONE_DIR={repoRoot}");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add("/bin/bash");
            psi.ArgumentList.Add(updateScript);
            psi.ArgumentList.Add(profile);
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["SITES_CLONE_DIR"] = repoRoot;
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"nohup /bin/bash \"{updateScript}\" \"{profile}\" </dev/null >/tmp/sites-update.log 2>&1 &");
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            return new SitesUpdateResult
            {
                Succeeded = false,
                Message = "Failed to start update process."
            };
        }

        _logger.LogInformation("Scheduled self-update for profile {Profile}", profile);
        return new SitesUpdateResult
        {
            Succeeded = true,
            Message = $"Self-update scheduled for profile '{profile}'. Service will restart shortly.",
            LogPath = systemdRun is null ? "/tmp/sites-update.log" : "/var/log/sites-update.log"
        };
    }
}

public sealed class SitesUpdateResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? LogPath { get; init; }
}
