using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sites.Cp.Services;

public sealed class SitesRebootService
{
    private readonly ILogger<SitesRebootService> _logger;

    public SitesRebootService(ILogger<SitesRebootService> logger) => _logger = logger;

    public RebootServerResult ScheduleReboot()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new RebootServerResult
            {
                Succeeded = false,
                Message = "Server reboot is only supported when Sites.Host runs on Linux."
            };
        }

        var reboot = new[] { "/usr/sbin/reboot", "/sbin/reboot" }
            .FirstOrDefault(File.Exists);
        if (reboot is null)
        {
            return new RebootServerResult
            {
                Succeeded = false,
                Message = "reboot command not found (/usr/sbin/reboot, /sbin/reboot)."
            };
        }

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
            psi.ArgumentList.Add("--property=Description=Sites CP reboot");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add(reboot);
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"nohup {reboot} </dev/null >/dev/null 2>&1 &");
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            return new RebootServerResult
            {
                Succeeded = false,
                Message = "Failed to start reboot process."
            };
        }

        _logger.LogWarning("Server reboot requested from CP");
        return new RebootServerResult
        {
            Succeeded = true,
            Message = "Reboot scheduled. The server will restart immediately."
        };
    }
}

public sealed class RebootServerResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;
}
