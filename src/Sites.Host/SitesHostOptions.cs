namespace Sites.Host;

public sealed class SitesHostOptions
{
    public const string SectionName = "Host";

    public int HttpPort { get; set; } = 80;

    /// <summary>
    /// When 0, HTTPS is not enabled.
    /// </summary>
    public int HttpsPort { get; set; } = 443;

    public string CertDirectory { get; set; } = "cert";

    public string CertPfxFileName { get; set; } = "sites.pfx";

    public string? CertPfxPassword { get; set; }
}
