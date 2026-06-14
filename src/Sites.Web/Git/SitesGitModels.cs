namespace Sites.Web.Git;

public sealed record SitesGitStatus
{
    public string RepositoryRoot { get; init; } = string.Empty;

    public string Branch { get; init; } = string.Empty;

    public bool HasPat { get; init; }

    public bool IsRepository { get; init; }

    public bool HasLocalChanges { get; init; }

    public IReadOnlyList<string> ChangedFiles { get; init; } = [];

    public string? LastError { get; init; }
}

public sealed class SitesGitOperationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Log { get; init; } = [];
}
