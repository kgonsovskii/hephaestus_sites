namespace Sites.CertMaintenance;

internal static class CertPathResolver
{
    public static string ResolveCertDirectory(CertMaintenanceOptions options) =>
        Path.IsPathRooted(options.CertDirectory.Trim())
            ? options.CertDirectory.Trim()
            : Path.Combine(AppContext.BaseDirectory, options.CertDirectory.Trim());

    public static string ResolvePfxPath(CertMaintenanceOptions options) =>
        Path.Combine(ResolveCertDirectory(options), options.CertPfxFileName);

    public static string ResolveAccountKeyPath(CertMaintenanceOptions options) =>
        Path.Combine(ResolveCertDirectory(options), options.AccountKeyFileName);
}
