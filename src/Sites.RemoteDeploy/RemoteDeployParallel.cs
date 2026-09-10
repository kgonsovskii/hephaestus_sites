namespace Sites.RemoteDeploy;

public sealed record RemoteDeployServerResult(
    string Server,
    string Login,
    string Profile,
    int ExitCode,
    string? Error)
{
    public bool Succeeded => ExitCode == 0 && string.IsNullOrEmpty(Error);
}

/// <summary>Runs <see cref="RemoteDeployRunner"/> against several hosts at once.</summary>
public static class RemoteDeployParallel
{
    public static async Task<IReadOnlyList<RemoteDeployServerResult>> RunAsync(
        string sshPassExecutable,
        IReadOnlyList<RemoteCreds> targets,
        Func<RemoteCreds, string> remoteCmdForTarget,
        Func<string, string, CancellationToken, Task> emitHostLineAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(remoteCmdForTarget);
        if (targets.Count == 0)
            throw new ArgumentException("At least one SSH target is required.", nameof(targets));

        var tasks = new Task<RemoteDeployServerResult>[targets.Count];
        for (var i = 0; i < targets.Count; i++)
        {
            var creds = targets[i];
            tasks[i] = RunOneAsync(
                sshPassExecutable,
                creds,
                remoteCmdForTarget(creds),
                emitHostLineAsync,
                cancellationToken);
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public static string FormatReport(IReadOnlyList<RemoteDeployServerResult> results)
    {
        var ok = results.Count(r => r.Succeeded);
        var fail = results.Count - ok;
        var lines = new List<string>(results.Count + 3)
        {
            "=== Sites deploy report ==="
        };
        foreach (var r in results)
        {
            var who = $"{r.Login}@{r.Server}  profile {r.Profile}";
            if (r.Succeeded)
                lines.Add($"  OK    {who}");
            else if (!string.IsNullOrEmpty(r.Error))
                lines.Add($"  FAIL  {who}  {r.Error}");
            else
                lines.Add($"  FAIL  {who}  exit {r.ExitCode}");
        }

        lines.Add($"{results.Count} server(s): {ok} succeeded, {fail} failed");
        return string.Join(Environment.NewLine, lines);
    }

    private static async Task<RemoteDeployServerResult> RunOneAsync(
        string sshPassExecutable,
        RemoteCreds creds,
        string remoteCmd,
        Func<string, string, CancellationToken, Task> emitHostLineAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = await RemoteDeployRunner.RunRemoteBashAsync(
                    sshPassExecutable,
                    creds.Server,
                    creds.Login,
                    creds.Password,
                    remoteCmd,
                    (line, ct) => emitHostLineAsync(creds.Server, line, ct),
                    cancellationToken)
                .ConfigureAwait(false);

            return new RemoteDeployServerResult(creds.Server, creds.Login, creds.Profile, code, null);
        }
        catch (Exception ex)
        {
            try
            {
                await emitHostLineAsync(creds.Server, "[error] " + ex.Message, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // report still includes the exception
            }

            return new RemoteDeployServerResult(creds.Server, creds.Login, creds.Profile, 1, ex.Message);
        }
    }
}
