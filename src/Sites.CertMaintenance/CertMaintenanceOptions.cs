namespace Sites.CertMaintenance;

public sealed class CertMaintenanceOptions
{
    public const string SectionName = "CertMaintenance";

    public bool Enabled { get; set; } = true;

    public string AcmeEmail { get; set; } = "admin@example.com";

    public bool UseStaging { get; set; }

    public bool TermsOfServiceAgreed { get; set; } = true;

    public string CertDirectory { get; set; } = "cert";

    public string CertPfxFileName { get; set; } = "sites.pfx";

    public string? CertPfxPassword { get; set; }

    public string AccountKeyFileName { get; set; } = "acme-account.pem";

    /// <summary>
    /// Renew when the certificate expires within this many days.
    /// </summary>
    public int RenewBeforeDays { get; set; } = 30;

    /// <summary>
    /// How often the background service checks certificate health.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Optional delay before the first maintenance run after startup.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(15);
}
