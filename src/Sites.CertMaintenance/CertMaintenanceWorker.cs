using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sites.Web;

namespace Sites.CertMaintenance;

public sealed class CertMaintenanceWorker
{
    private readonly ILogger<CertMaintenanceWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<CertMaintenanceOptions> _options;
    private readonly TlsCertificateStore _certificateStore;
    private readonly AcmeChallengeRegistry _challengeRegistry;
    private readonly SiteRegistry _registry;
    private readonly LetsEncryptCertificateIssuer _issuer = new();

    public CertMaintenanceWorker(
        ILogger<CertMaintenanceWorker> logger,
        IConfiguration configuration,
        IOptionsMonitor<CertMaintenanceOptions> options,
        TlsCertificateStore certificateStore,
        AcmeChallengeRegistry challengeRegistry,
        SiteRegistry registry)
    {
        _logger = logger;
        _configuration = configuration;
        _options = options;
        _certificateStore = certificateStore;
        _challengeRegistry = challengeRegistry;
        _registry = registry;
    }

    public bool IsEnabled()
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
            return false;

        return _configuration.GetValue("Host:HttpsPort", 443) > 0;
    }

    public bool CertificateFileExists()
    {
        var options = _options.CurrentValue;
        return File.Exists(CertPathResolver.ResolvePfxPath(options));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!IsEnabled())
            return;

        var pfxPath = CertPathResolver.ResolvePfxPath(options);
        if (!_certificateStore.TryLoadFromPfx(pfxPath, options.CertPfxPassword))
        {
            _logger.LogWarning("TLS certificate missing at {PfxPath}; requesting a new one.", pfxPath);
        }
        else
        {
            _logger.LogInformation(
                "Loaded TLS certificate {Subject} (expires {Expires:u}).",
                _certificateStore.Current?.Subject,
                _certificateStore.NotAfter);
        }

        var domains = PublishDomainDiscovery.DiscoverFromRegistry(_registry);
        if (domains.Count == 0)
        {
            var sitesJsonPath = SitesJsonFile.ResolvePath();
            domains = PublishDomainDiscovery.DiscoverFromModulesAssembly(sitesJsonPath);
            if (domains.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No publish domains were discovered. Profile '{Sites.Web.Abstractions.SitesProfileResolver.Current}', " +
                    $"sites.json '{sitesJsonPath}', active sites {_registry.ActiveSites.Count}.");
            }

            _logger.LogWarning(
                "SiteRegistry had no domains; using module discovery from {SitesJsonPath}.",
                sitesJsonPath);
        }

        var missingHosts = CertificateDomainCoverage.MissingDomains(_certificateStore.Current, domains);
        var renewForExpiry = ShouldRenew(_certificateStore.NotAfter, options.RenewBeforeDays);
        if (!renewForExpiry && missingHosts.Count == 0)
        {
            _logger.LogInformation(
                "TLS certificate covers {Count} site host(s) and is not due for renewal.",
                domains.Count);
            return;
        }

        if (missingHosts.Count > 0)
        {
            _logger.LogWarning(
                "TLS certificate is missing site host(s): {Hosts}. Reissuing so HTTPS names match sites.json.",
                string.Join(", ", missingHosts));
        }

        _logger.LogInformation(
            "Requesting Let's Encrypt certificate for {Domains}.",
            string.Join(", ", domains));

        var result = await _issuer.IssueOrRenewAsync(
            options,
            domains,
            _challengeRegistry,
            cancellationToken);

        if (!_certificateStore.TryLoadFromPfx(result.PfxPath, options.CertPfxPassword))
            throw new InvalidOperationException($"Failed to load issued certificate from {result.PfxPath}.");

        _logger.LogInformation(
            "TLS certificate renewed ({Staging}). Expires {Expires:u}. PFX: {PfxPath}",
            result.UsedStaging ? "staging" : "production",
            _certificateStore.NotAfter,
            result.PfxPath);
    }

    private static bool ShouldRenew(DateTimeOffset? notAfter, int renewBeforeDays)
    {
        if (notAfter is null)
            return true;

        var threshold = Math.Clamp(renewBeforeDays, 1, 89);
        return notAfter.Value <= DateTimeOffset.UtcNow.AddDays(threshold);
    }
}
