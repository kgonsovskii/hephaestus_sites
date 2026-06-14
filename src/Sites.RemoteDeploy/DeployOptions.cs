namespace Sites.RemoteDeploy;

public sealed class DeployOptions
{
    public const string SectionName = "Deploy";

    public string Server { get; set; } = string.Empty;

    public string Login { get; set; } = "root";

    public string Password { get; set; } = string.Empty;

    public string Label { get; set; } = "sites";

    public string GitRepositoryUrl { get; set; } = "https://github.com/kgonsovskii/hephaestus_sites.git";

    /// <summary>Remote git checkout directory. Empty = $HOME/hephaestus_sites on the VPS.</summary>
    public string CloneDirectory { get; set; } = string.Empty;

    /// <summary>Remote publish output. Empty = ${CloneDirectory}/release (linux service working dir).</summary>
    public string PublishDirectory { get; set; } = string.Empty;

    public string ServiceName { get; set; } = "sites-host";

    public string RuntimeIdentifier { get; set; } = "linux-x64";
}

public sealed record RemoteCreds(string Server, string Login, string Password);
