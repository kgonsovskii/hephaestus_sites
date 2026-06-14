using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Text;

namespace Sites.RemoteDeploy;

public static class RemoteDeployRunner
{
    public const string DefaultRemoteScriptFileName = "install-remote.txt";
    public const string DefaultWaitScriptFileName = "wait.sh";

    private static readonly string[] SshCommonOpts =
    [
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "ConnectTimeout=30",
        "-o", "ServerAliveInterval=15",
        "-o", "ServerAliveCountMax=4"
    ];

    public static string LoadRemoteInstallBootstrapScript(string installRemoteTxtPath)
    {
        if (!File.Exists(installRemoteTxtPath))
            throw new FileNotFoundException("Remote install script not found.", installRemoteTxtPath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(installRemoteTxtPath))
            ?? Directory.GetCurrentDirectory();

        var waitPath = Path.Combine(directory, DefaultWaitScriptFileName);
        if (!File.Exists(waitPath))
            throw new FileNotFoundException(
                $"{DefaultWaitScriptFileName} must exist next to {DefaultRemoteScriptFileName}.",
                waitPath);

        return NormalizeRemoteShellText(File.ReadAllText(waitPath, Encoding.UTF8))
            + NormalizeRemoteShellText(File.ReadAllText(installRemoteTxtPath, Encoding.UTF8));
    }

    public static string PrependDeployExports(DeployOptions options, string remoteScriptText, string profile)
    {
        var profileName = Sites.Web.Abstractions.SitesProfileResolver.NormalizeProfileName(profile);
        var exports = new List<string>
        {
            $"export SITES_PROFILE='{EscapeShell(profileName)}'",
            $"export SITES_GIT_REPO='{EscapeShell(options.GitRepositoryUrl)}'",
            $"export SITES_SERVICE_NAME='{EscapeShell(options.ServiceName)}'",
            $"export SITES_RUNTIME_IDENTIFIER='{EscapeShell(options.RuntimeIdentifier)}'"
        };

        if (!string.IsNullOrWhiteSpace(options.CloneDirectory))
            exports.Add($"export SITES_CLONE_DIR='{EscapeShell(options.CloneDirectory)}'");

        if (!string.IsNullOrWhiteSpace(options.PublishDirectory))
            exports.Add($"export SITES_PUBLISH_DIR='{EscapeShell(options.PublishDirectory)}'");

        var repoRoot = Sites.Web.Abstractions.RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null
            && Sites.Web.Abstractions.SitesGitPatFile.TryBuildAuthenticatedCloneUrl(
                options.GitRepositoryUrl,
                repoRoot,
                out var cloneUrl))
        {
            exports.Add($"export SITES_GIT_CLONE_URL='{EscapeShell(cloneUrl)}'");
        }

        return string.Join('\n', exports) + '\n' + remoteScriptText;
    }

    public static string? FindSshPassOnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "sshpass.exe", "sshpass" })
            {
                var full = Path.Combine(dir.Trim(), name);
                if (File.Exists(full))
                    return full;
            }
        }

        return null;
    }

    public static async Task<string> EnsureSshPassAsync(
        Action<string>? logInfo = null,
        CancellationToken cancellationToken = default)
    {
        var found = FindSshPassOnPath();
        if (found is not null)
            return found;

        if (OperatingSystem.IsWindows())
            return await WindowsSshPassBootstrap.EnsureAsync(logInfo, cancellationToken);

        if (OperatingSystem.IsLinux())
            return await LinuxSshPassBootstrap.EnsureAsync(logInfo, cancellationToken);

        throw new InvalidOperationException("sshpass not found on PATH.");
    }

    public static async Task<int> RunRemoteBashAsync(
        string sshPassExecutable,
        string host,
        string user,
        string password,
        string remoteScriptText,
        Func<string, CancellationToken, Task>? emitLineAsync = null,
        CancellationToken cancellationToken = default)
    {
        var script = NormalizeRemoteShellText(remoteScriptText);
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
        var remoteShell = $"echo {b64} | base64 -d | bash";

        var args = new List<string> { "-e", "ssh", "-tt" };
        args.AddRange(SshCommonOpts);
        args.Add($"{user}@{host}");
        args.Add(remoteShell);
        return await RunSshPassProcessAsync(sshPassExecutable, password, args, emitLineAsync, cancellationToken);
    }

    private static string NormalizeRemoteShellText(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string EscapeShell(string value) =>
        value.Replace("'", "'\\''", StringComparison.Ordinal);

    private static Task DefaultEmitAsync(string _, CancellationToken __) => Task.CompletedTask;

    private static async Task<int> RunSshPassProcessAsync(
        string sshPassExecutable,
        string password,
        IReadOnlyList<string> args,
        Func<string, CancellationToken, Task>? emitLineAsync,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = sshPassExecutable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        psi.Environment["SSHPASS"] = password;

        using var proc = new Process { StartInfo = psi };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start sshpass process.");

        var emit = emitLineAsync ?? DefaultEmitAsync;
        var stdout = PumpLinesAsync(proc.StandardOutput, null, emit, cancellationToken);
        var stderr = PumpLinesAsync(proc.StandardError, "[stderr] ", emit, cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdout, stderr);
        return proc.ExitCode;
    }

    private static async Task PumpLinesAsync(
        StreamReader reader,
        string? prefix,
        Func<string, CancellationToken, Task> emit,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;

            await emit(prefix is null ? line : prefix + line, cancellationToken);
        }
    }
}

[SupportedOSPlatform("linux")]
internal static class LinuxSshPassBootstrap
{
    public static async Task<string> EnsureAsync(Action<string>? logInfo, CancellationToken cancellationToken)
    {
        var found = RemoteDeployRunner.FindSshPassOnPath();
        if (found is not null)
            return found;

        logInfo?.Invoke("sshpass not found; installing via apt (like Hephaestus install-remote)...");
        var exitCode = await RunShellAsync(
            """
            set -e
            if command -v sshpass >/dev/null 2>&1; then
              exit 0
            fi
            if ! command -v apt-get >/dev/null 2>&1; then
              echo "apt-get is required to install sshpass on this host." >&2
              exit 1
            fi
            if [ "$(id -u)" -eq 0 ]; then
              apt-get update
              DEBIAN_FRONTEND=noninteractive apt-get install -y sshpass
            else
              sudo apt-get update
              sudo DEBIAN_FRONTEND=noninteractive apt-get install -y sshpass
            fi
            """,
            logInfo,
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                "sshpass not found on PATH and automatic install failed (install manually: sudo apt install sshpass).");
        }

        found = RemoteDeployRunner.FindSshPassOnPath() ?? "/usr/bin/sshpass";
        if (!File.Exists(found))
            throw new InvalidOperationException("sshpass install reported success but binary is still missing.");

        logInfo?.Invoke($"sshpass ready: {found}");
        return found;
    }

    private static async Task<int> RunShellAsync(
        string script,
        Action<string>? logInfo,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(script);

        using var proc = new Process { StartInfo = psi };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start bash for sshpass install.");

        var readStdout = PumpInstallLineAsync(proc.StandardOutput, logInfo, cancellationToken);
        var readStderr = PumpInstallLineAsync(proc.StandardError, logInfo, cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(readStdout, readStderr);
        return proc.ExitCode;
    }

    private static async Task PumpInstallLineAsync(
        StreamReader reader,
        Action<string>? logInfo,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;

            logInfo?.Invoke(line);
        }
    }
}

[SupportedOSPlatform("windows")]
internal static class WindowsSshPassBootstrap
{
    private const string SshPassWin64ReleaseTag = "1.10.0";

    public static async Task<string> EnsureAsync(Action<string>? logInfo, CancellationToken cancellationToken)
    {
        var found = RemoteDeployRunner.FindSshPassOnPath()
            ?? FindUnderChocolatey()
            ?? FindUnderLocalTools();
        if (found is not null)
            return found;

        found = await DownloadPortableAsync(logInfo, cancellationToken);
        logInfo?.Invoke($"sshpass ready: {found}");
        return found;
    }

    private static string? FindUnderChocolatey()
    {
        var lib = Environment.GetEnvironmentVariable("ChocolateyInstall");
        lib = string.IsNullOrEmpty(lib)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "lib")
            : Path.Combine(lib, "lib");

        return Directory.Exists(lib)
            ? Directory.EnumerateFiles(lib, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private static string? FindUnderLocalTools()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sites-tools");
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private static async Task<string> DownloadPortableAsync(Action<string>? logInfo, CancellationToken cancellationToken)
    {
        var tag = SshPassWin64ReleaseTag;
        var destRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "sites-tools",
            $"sshpass-win64-{tag}");
        Directory.CreateDirectory(destRoot);

        var existing = Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (existing is not null)
            return existing;

        var zipUrl = $"https://github.com/sharpninja/sshpass-win64/releases/download/v{tag}/sshpass-win64-{tag}.zip";
        var tmpZip = Path.Combine(Path.GetTempPath(), $"sites-sshpass-{tag}.zip");
        logInfo?.Invoke($"Downloading portable sshpass-win64 v{tag}...");

        using (var http = new HttpClient())
        {
            await using var input = await http.GetStreamAsync(new Uri(zipUrl), cancellationToken);
            await using var output = File.Create(tmpZip);
            await input.CopyToAsync(output, cancellationToken);
        }

        try
        {
            ZipFile.ExtractToDirectory(tmpZip, destRoot, overwriteFiles: true);
        }
        finally
        {
            try { File.Delete(tmpZip); } catch { /* ignored */ }
        }

        return Directory.EnumerateFiles(destRoot, "sshpass.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"sshpass.exe not found after download from {zipUrl}");
    }
}
