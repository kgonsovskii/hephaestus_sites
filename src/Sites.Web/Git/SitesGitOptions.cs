namespace Sites.Web.Git;

public sealed class SitesGitOptions
{
    public const string SectionName = "Git";

    public bool Enabled { get; set; } = true;

    public string RepositoryUrl { get; set; } = "https://github.com/kgonsovskii/hephaestus_sites_data.git";

    public string DefaultBranch { get; set; } = "main";

    public string EncryptedPatFileName { get; set; } = "git-pat-data.enc";

    /// <summary>How often the host pulls <c>hephaestus_sites_data</c>, reloads sites.json, then pushes local changes.</summary>
    public TimeSpan PullInterval { get; set; } = TimeSpan.FromHours(6);

    public string CommitUserName { get; set; } = "sites-host";

    public string CommitEmail { get; set; } = "sites-host@local";
}
