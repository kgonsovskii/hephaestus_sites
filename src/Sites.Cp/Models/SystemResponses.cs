namespace Sites.Cp.Models;

public sealed class SystemInfoResponse
{
    public string Profile { get; init; } = string.Empty;

    public string ProfileFilePath { get; init; } = string.Empty;

    public string RepositoryRoot { get; init; } = string.Empty;

    public string CloneDirectory { get; init; } = string.Empty;

    public string ProfileDataDirectory { get; init; } = string.Empty;

    public string SitesJsonPath { get; init; } = string.Empty;

    public string SettingsJsonPath { get; init; } = string.Empty;

    public bool IsLinux { get; init; }

    public string WebRootFullPath { get; init; } = string.Empty;

    public string WebFtpUrl { get; init; } = string.Empty;
}

public sealed class ProfileUpdateRequest
{
    public string Profile { get; set; } = string.Empty;
}

public sealed class ProfileUpdateResponse
{
    public string Profile { get; init; } = string.Empty;

    public string ProfileFilePath { get; init; } = string.Empty;

    public string SitesJsonPath { get; init; } = string.Empty;

    public int SiteCount { get; init; }
}

public sealed class CloneStartRequest
{
    public string Profile { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class CloneStartResponse
{
    public string RunId { get; init; } = string.Empty;
}

public sealed class CloneStatusResponse
{
    public string RunId { get; init; } = string.Empty;

    public bool Done { get; init; }

    public int? ExitCode { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<string> Log { get; init; } = [];
}

public sealed class RebootServerResponse
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;
}
