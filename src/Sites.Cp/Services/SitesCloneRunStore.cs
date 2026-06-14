using System.Collections.Concurrent;

namespace Sites.Cp.Services;

public sealed class SitesCloneRunStore
{
    private readonly ConcurrentDictionary<string, SitesCloneRunState> _runs = new(StringComparer.Ordinal);

    public string Start(Func<SitesCloneRunState, CancellationToken, Task<SitesCloneResult>> work, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        var state = new SitesCloneRunState(runId);
        _runs[runId] = state;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await work(state, cancellationToken);
                state.Complete(result);
            }
            catch (Exception ex)
            {
                state.Fail(ex.Message);
            }
        }, CancellationToken.None);

        return runId;
    }

    public bool TryGet(string runId, out SitesCloneRunState? state) =>
        _runs.TryGetValue(runId, out state);
}

public sealed class SitesCloneRunState
{
    private readonly object _sync = new();
    private readonly List<string> _lines = [];

    public SitesCloneRunState(string runId) => RunId = runId;

    public string RunId { get; }

    public bool Done { get; private set; }

    public int? ExitCode { get; private set; }

    public string? Error { get; private set; }

    public void AppendLine(string line)
    {
        lock (_sync)
            _lines.Add(line);
    }

    public IReadOnlyList<string> SnapshotLines()
    {
        lock (_sync)
            return _lines.ToArray();
    }

    public void Complete(SitesCloneResult result)
    {
        lock (_sync)
        {
            Done = true;
            ExitCode = result.ExitCode;
        }
    }

    public void Fail(string message)
    {
        lock (_sync)
        {
            Error = message;
            Done = true;
            ExitCode = -1;
        }
    }
}
