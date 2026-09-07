namespace Sites.Web.Git;

public sealed class SitesGitOptions
{
    public const string SectionName = "Git";

    public string RepositoryUrl { get; set; } = "https://github.com/kgonsovskii/hephaestus_sites.git";

    public string DefaultBranch { get; set; } = "main";

    public string EncryptedPatFileName { get; set; } = "git-pat.enc";

    /// <summary>How often the host pulls from origin. Push stays manual (CP buttons).</summary>
    public TimeSpan PullInterval { get; set; } = TimeSpan.FromHours(6);

    public string CommitUserName { get; set; } = "sites-host";

    public string CommitEmail { get; set; } = "sites-host@local";
}
