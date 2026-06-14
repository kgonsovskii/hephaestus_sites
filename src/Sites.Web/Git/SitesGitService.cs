using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sites.Web.Abstractions;

namespace Sites.Web.Git;

public sealed class SitesGitService
{
    private const string SyncStashMessage = "sites-pre-sync";
    private const string SyncCommitMessage = "Sites CP sync";

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
        var repoRoot = RepositoryPaths.ResolveRoot();
        var status = new SitesGitStatus
        {
            RepositoryRoot = repoRoot,
            HasPat = SitesGitPatFile.TryLoadToken(repoRoot, out _),
            IsRepository = Directory.Exists(Path.Combine(repoRoot, ".git"))
        };

        if (!status.IsRepository)
        {
            status = status with { LastError = "Repository is not a git checkout (.git missing)." };
            return status;
        }

        if (!TryRunGit("rev-parse --abbrev-ref HEAD", repoRoot, out var branch, out var branchError))
        {
            status = status with { LastError = branchError };
            return status;
        }

        var changed = ListChangedFiles(repoRoot);
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

                return PushCore();
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
        var repoRoot = RepositoryPaths.ResolveRoot();
        if (!Directory.Exists(Path.Combine(repoRoot, ".git")))
            return Fail("Not a git repository.", log);

        if (!SitesGitPatFile.TryLoadToken(repoRoot, out var token))
            return Fail($"Missing GitHub PAT at {SitesGitPatFile.ResolveEncryptedPath(repoRoot)}.", log);

        EnsureGitIdentity(repoRoot, log);
        SetAuthenticatedRemote(repoRoot, token, log);

        var stashed = TryStash(repoRoot, log);
        RunGit($"{NetworkGitConfig}fetch origin", repoRoot, log);
        var branch = ResolveBranch(repoRoot, log);
        PullPreferRemote(repoRoot, branch, log);
        if (stashed)
            RestoreStash(repoRoot, log);

        return Success($"Pulled origin/{branch}.", log);
    }

    private SitesGitOperationResult PushCore()
    {
        var log = new List<string>();
        var repoRoot = RepositoryPaths.ResolveRoot();
        if (!Directory.Exists(Path.Combine(repoRoot, ".git")))
            return Fail("Not a git repository.", log);

        if (!SitesGitPatFile.TryLoadToken(repoRoot, out var token))
            return Fail($"Missing GitHub PAT at {SitesGitPatFile.ResolveEncryptedPath(repoRoot)}.", log);

        EnsureGitIdentity(repoRoot, log);
        SetAuthenticatedRemote(repoRoot, token, log);
        var branch = ResolveBranch(repoRoot, log);

        if (!HasWorkingTreeChanges(repoRoot))
            return Success("No local changes to push.", log);

        RunGit("add -A", repoRoot, log);
        if (!TryRunGit($"commit -m \"{SyncCommitMessage}\"", repoRoot, out _, out var commitError)
            && !commitError.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
        {
            return Fail(commitError, log);
        }

        if (!TryRunGit($"{NetworkGitConfig}push origin {branch}", repoRoot, out _, out var pushError))
            return Fail(pushError, log);

        return Success($"Pushed to origin/{branch}.", log);
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
