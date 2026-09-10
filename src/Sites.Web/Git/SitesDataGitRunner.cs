using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sites.Web.Abstractions;

namespace Sites.Web.Git;

/// <summary>
/// Startup clone of sibling <c>hephaestus_sites_data</c> (same idea as Hephaestus <c>hephaestus_data</c>).
/// Periodic pull/push stays on <see cref="SitesGitService"/>.
/// </summary>
public static class SitesDataGitRunner
{
    public const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_sites_data.git";

    public static void EnsureCloned(ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataDir = SitesProfileResolver.ResolveSitesDataBase();
        if (Directory.Exists(Path.Combine(dataDir, ".git")))
        {
            logger.LogInformation("Sites data git: using existing {DataDir}.", dataDir);
            return;
        }

        var codeRoot = RepositoryPaths.ResolveRoot();
        if (!SitesGitPatFile.TryLoadToken(codeRoot, out _)
            || !SitesGitPatFile.TryBuildAuthenticatedCloneUrl(RepositoryUrl, codeRoot, out var cloneUrl))
        {
            throw new InvalidOperationException(
                $"Cannot clone {RepositoryUrl}: missing PAT at {SitesGitPatFile.ResolveEncryptedPath(codeRoot)}.");
        }

        if (Directory.Exists(dataDir))
        {
            logger.LogInformation("Sites data git: removing non-repository directory {DataDir}.", dataDir);
            Directory.Delete(dataDir, recursive: true);
        }

        var parent = Path.GetDirectoryName(dataDir);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        logger.LogInformation("Sites data git: cloning {RepositoryUrl} into {DataDir}.", RepositoryUrl, dataDir);
        RunGit($"clone \"{cloneUrl}\" \"{dataDir}\"", workingDirectory: null);
        logger.LogInformation("Sites data git: clone finished.");
    }

    private static void RunGit(string arguments, string? workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git.");
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {arguments} failed: {stderr.Trim()}");
    }
}
