using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sites.Web.Abstractions;

namespace Sites.Web.Git;

public sealed class SitesGitService
{
    private const string SyncStashMessage = "sites-pre-sync";
    private const string SyncCommitMessage = "Sites data sync";

    private static string NetworkGitConfig =>
        "-c credential.helper= -c core.askPass= -c credential.useHttpPath=true "
        + (OperatingSystem.IsWindows() ? "-c credential.helperManager= " : "");

    private readonly SitesGitOptions _options;
    private readonly ILogger<SitesGitService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SitesGitService(IOptions<SitesGitOptions> options, ILogger<SitesGitService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public SitesGitStatus GetStatus()
    {
        var dataRoot = SitesProfileResolver.ResolveSitesDataBase();
        var status = new SitesGitStatus
        {
            RepositoryRoot = dataRoot,
            HasPat = SitesGitPatFile.TryLoadToken(RepositoryPaths.ResolveRoot(), out _),
            IsRepository = Directory.Exists(Path.Combine(dataRoot, ".git"))
        };

        if (!status.IsRepository)
        {
            status = status with { LastError = "hephaestus_sites_data is not a git checkout (.git missing)." };
            return status;
        }

        if (!TryRunGit("rev-parse --abbrev-ref HEAD", dataRoot, out var branch, out var branchError))
        {
            status = status with { LastError = branchError };
            return status;
        }

        var changed = ListChangedFiles(dataRoot);
        return status with
        {
            Branch = branch.Trim(),
            HasLocalChanges = changed.Count > 0,
            ChangedFiles = changed
        };
    }

    public async Task<SitesGitOperationResult> PullAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => PullCore(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SitesGitOperationResult> PushAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => PushCore(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SitesGitOperationResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                var pull = PullCore();
                if (!pull.Succeeded)
                    return pull;

                var push = PushCore();
                var log = pull.Log.Concat(push.Log).ToList();
                if (!push.Succeeded)
                    return Fail($"{pull.Message} {push.Message}", log);

                return Success($"{pull.Message} {push.Message}", log);
            }, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private SitesGitOperationResult PullCore()
    {
        var log = new List<string>();
        var dataRoot = SitesProfileResolver.ResolveSitesDataBase();
        var codeRoot = RepositoryPaths.ResolveRoot();
        if (!Directory.Exists(Path.Combine(dataRoot, ".git")))
        {
            try
            {
                SitesDataGitRunner.EnsureCloned(_logger);
                log.Add($"cloned {SitesDataGitRunner.RepositoryUrl}");
                return Success($"Cloned hephaestus_sites_data.", log);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, log);
            }
        }

        if (!SitesGitPatFile.TryLoadToken(codeRoot, out var token))
            return Fail($"Missing GitHub PAT at {SitesGitPatFile.ResolveEncryptedPath(codeRoot)}.", log);

        EnsureGitIdentity(dataRoot, log);
        SetAuthenticatedRemote(dataRoot, token, log);

        var stashed = TryStash(dataRoot, log);
        RunGit($"{NetworkGitConfig}fetch origin", dataRoot, log);
        var branch = ResolveBranch(dataRoot, log);
        PullPreferRemote(dataRoot, branch, log);
        if (stashed)
            RestoreStash(dataRoot, log);

        return Success($"Pulled origin/{branch} (hephaestus_sites_data).", log);
    }

    private SitesGitOperationResult PushCore()
    {
        var log = new List<string>();
        var dataRoot = SitesProfileResolver.ResolveSitesDataBase();
        var codeRoot = RepositoryPaths.ResolveRoot();
        if (!Directory.Exists(Path.Combine(dataRoot, ".git")))
            return Fail("hephaestus_sites_data is not a git repository.", log);

        if (!SitesGitPatFile.TryLoadToken(codeRoot, out var token))
            return Fail($"Missing GitHub PAT at {SitesGitPatFile.ResolveEncryptedPath(codeRoot)}.", log);

        EnsureGitIdentity(dataRoot, log);
        SetAuthenticatedRemote(dataRoot, token, log);
        var branch = ResolveBranch(dataRoot, log);

        if (!HasWorkingTreeChanges(dataRoot))
            return Success("No local data changes to push.", log);

        RunGit("add -A", dataRoot, log);
        if (!TryRunGit($"commit -m \"{SyncCommitMessage}\"", dataRoot, out _, out var commitError)
            && !commitError.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
        {
            return Fail(commitError, log);
        }

        if (!TryRunGit($"{NetworkGitConfig}push origin {branch}", dataRoot, out _, out var pushError))
            return Fail(pushError, log);

        return Success($"Pushed hephaestus_sites_data to origin/{branch}.", log);
    }

    private void PullPreferRemote(string repoRoot, string branch, List<string> log)
    {
        var pullArgs = $"{NetworkGitConfig}pull origin {branch} --no-rebase --no-edit -X theirs";
        if (TryRunGit(pullArgs, repoRoot, out _, out _))
        {
            log.Add($"pull origin/{branch} OK");
            return;
        }

        log.Add($"pull failed; hard-reset to origin/{branch}");
        TryRunGit("merge --abort", repoRoot, out _, out _);
        RunGit($"reset --hard origin/{branch}", repoRoot, log);
        TryRunGit("clean -fd", repoRoot, out _, out _);
    }

    private static bool TryStash(string repoRoot, List<string> log)
    {
        if (!HasWorkingTreeChanges(repoRoot))
            return false;

        if (!TryRunGit($"stash push -u -m \"{SyncStashMessage}\"", repoRoot, out _, out var error))
        {
            log.Add($"stash skipped: {error}");
            return false;
        }

        log.Add("stashed local changes");
        return true;
    }

    private static void RestoreStash(string repoRoot, List<string> log)
    {
        if (!TryRunGit("stash pop", repoRoot, out _, out var error))
            log.Add($"stash pop warning: {error}");
    }

    private void EnsureGitIdentity(string repoRoot, List<string> log)
    {
        TryRunGit($"config user.email \"{_options.CommitEmail}\"", repoRoot, out _, out _);
        TryRunGit($"config user.name \"{_options.CommitUserName}\"", repoRoot, out _, out _);
    }

    private void SetAuthenticatedRemote(string repoRoot, string token, List<string> log)
    {
        var cloneUrl = BuildAuthenticatedUrl(_options.RepositoryUrl, token);
        RunGit($"remote set-url origin \"{cloneUrl}\"", repoRoot, log);
    }

    private string ResolveBranch(string repoRoot, List<string> log)
    {
        if (TryRunGit("rev-parse --abbrev-ref HEAD", repoRoot, out var branch, out _)
            && !string.Equals(branch.Trim(), "HEAD", StringComparison.Ordinal))
            return branch.Trim();

        log.Add($"using default branch {_options.DefaultBranch}");
        return _options.DefaultBranch;
    }

    private static List<string> ListChangedFiles(string repoRoot)
    {
        if (!TryRunGit("status --porcelain", repoRoot, out var output, out _))
            return [];

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Length > 3 ? line[3..].Trim() : line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private static bool HasWorkingTreeChanges(string repoRoot) =>
        ListChangedFiles(repoRoot).Count > 0;

    private void RunGit(string arguments, string repoRoot, List<string> log)
    {
        if (!TryRunGit(arguments, repoRoot, out var output, out var error))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "git failed" : error);

        if (!string.IsNullOrWhiteSpace(output))
            log.Add(output.Trim());
    }

    private static bool TryRunGit(string arguments, string repoRoot, out string output, out string error)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in SplitArguments(arguments))
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git.");

        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static IEnumerable<string> SplitArguments(string arguments)
    {
        var current = new StringBuilder();
        var inQuotes = false;
        foreach (var ch in arguments)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }

    internal static string BuildAuthenticatedUrl(string repositoryUrl, string token) =>
        SitesGitPatFile.BuildAuthenticatedUrl(repositoryUrl, token);

    private static SitesGitOperationResult Success(string message, List<string> log) =>
        new() { Succeeded = true, Message = message, Log = log };

    private SitesGitOperationResult Fail(string message, List<string> log)
    {
        _logger.LogWarning("Sites git operation failed: {Message}", message);
        return new SitesGitOperationResult { Succeeded = false, Message = message, Log = log };
    }
}
