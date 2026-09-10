using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sites.Web.Abstractions;

namespace Sites.Web.Git;

/// <summary>
/// Startup clone/pull of sibling <c>hephaestus_sites_data</c> (same idea as Hephaestus <c>hephaestus_data</c>).
/// Periodic pull/push stays on <see cref="SitesGitService"/>.
/// </summary>
public static class SitesDataGitRunner
{
    public const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_sites_data.git";

    private const string DefaultBranch = "main";

    private static string NetworkGitConfig =>
        "-c credential.helper= -c core.askPass= -c credential.useHttpPath=true "
        + (OperatingSystem.IsWindows() ? "-c credential.helperManager= " : "");

    public static void EnsureCloned(ILogger? logger = null) =>
        EnsureSynced(logger, pullIfExists: false);

    /// <summary>
    /// Clone if missing, otherwise fetch and reset to origin so restart/deploy sees current sites.json.
    /// </summary>
    public static void EnsureSynced(ILogger? logger = null) =>
        EnsureSynced(logger, pullIfExists: true);

    private static void EnsureSynced(ILogger? logger, bool pullIfExists)
    {
        logger ??= NullLogger.Instance;
        var dataDir = SitesProfileResolver.ResolveSitesDataBase();
        if (Directory.Exists(Path.Combine(dataDir, ".git")))
        {
            if (pullIfExists)
                PullExisting(dataDir, logger);
            else
                logger.LogInformation("Sites data git: using existing {DataDir}.", dataDir);
            return;
        }

        CloneFresh(dataDir, logger);
    }

    private static void CloneFresh(string dataDir, ILogger logger)
    {
        var codeRoot = RepositoryPaths.ResolveRoot();
        if (!SitesGitPatFile.TryBuildAuthenticatedDataCloneUrl(RepositoryUrl, codeRoot, out var cloneUrl))
        {
            throw new InvalidOperationException(
                $"Cannot clone {RepositoryUrl}: missing PAT at {SitesGitPatFile.ResolveEncryptedDataPath(codeRoot)}.");
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
        RunGit($"{NetworkGitConfig}clone \"{cloneUrl}\" \"{dataDir}\"", workingDirectory: null);
        SetPlainRemote(dataDir);
        logger.LogInformation("Sites data git: clone finished.");
    }

    private static void PullExisting(string dataDir, ILogger logger)
    {
        var codeRoot = RepositoryPaths.ResolveRoot();
        if (!SitesGitPatFile.TryLoadDataToken(codeRoot, out var token))
        {
            logger.LogWarning(
                "Sites data git: skip pull, missing PAT at {PatPath}.",
                SitesGitPatFile.ResolveEncryptedDataPath(codeRoot));
            return;
        }

        try
        {
            var cloneUrl = SitesGitPatFile.BuildAuthenticatedUrl(RepositoryUrl, token);
            RunGit($"remote set-url origin \"{cloneUrl}\"", dataDir);
            RunGit($"{NetworkGitConfig}fetch origin", dataDir);
            var branch = ResolveBranch(dataDir);
            logger.LogInformation("Sites data git: resetting {DataDir} to origin/{Branch}.", dataDir, branch);
            TryRunGit("merge --abort", dataDir);
            RunGit($"reset --hard origin/{branch}", dataDir);
            TryRunGit("clean -fd", dataDir);
            SetPlainRemote(dataDir);
            logger.LogInformation("Sites data git: pull finished.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sites data git: pull failed; continuing with local {DataDir}.", dataDir);
            try
            {
                SetPlainRemote(dataDir);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static void SetPlainRemote(string dataDir) =>
        RunGit($"remote set-url origin \"{RepositoryUrl}\"", dataDir);

    private static string ResolveBranch(string dataDir)
    {
        if (TryRunGit("rev-parse --abbrev-ref HEAD", dataDir, out var branch)
            && !string.Equals(branch.Trim(), "HEAD", StringComparison.Ordinal))
            return branch.Trim();

        return DefaultBranch;
    }

    private static void RunGit(string arguments, string? workingDirectory)
    {
        if (!TryRunGit(arguments, workingDirectory, out var error))
            throw new InvalidOperationException($"git {arguments} failed: {error}");
    }

    private static bool TryRunGit(string arguments, string? workingDirectory) =>
        TryRunGit(arguments, workingDirectory, out _);

    private static bool TryRunGit(string arguments, string? workingDirectory, out string error)
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
        process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
