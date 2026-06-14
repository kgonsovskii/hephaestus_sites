using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sites.CertMaintenance;

namespace Sites.Host;

internal static class KestrelCertificateConfiguration
{
    private const X509KeyStorageFlags PfxKeyStorage =
        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable;

    public static void ConfigureEndpoints(
        KestrelServerOptions options,
        IConfiguration configuration,
        TlsCertificateStore certificateStore,
        IHostEnvironment environment)
    {
        var hostOptions = configuration.GetSection(SitesHostOptions.SectionName).Get<SitesHostOptions>()
            ?? new SitesHostOptions();
        var maintenanceOptions = configuration.GetSection(CertMaintenanceOptions.SectionName).Get<CertMaintenanceOptions>()
            ?? new CertMaintenanceOptions();

        var httpPort = Math.Clamp(hostOptions.HttpPort, 1, 65535);
        if (environment.IsDevelopment())
            options.ListenLocalhost(httpPort);
        else
            options.ListenAnyIP(httpPort);

        if (hostOptions.HttpsPort <= 0)
            return;

        var httpsPort = Math.Clamp(hostOptions.HttpsPort, 1, 65535);
        var pfxPath = ResolvePfxPath(hostOptions);
        var pfxPassword = hostOptions.CertPfxPassword ?? maintenanceOptions.CertPfxPassword;

        if (maintenanceOptions.Enabled)
        {
            certificateStore.TryLoadFromPfx(pfxPath, pfxPassword);
            if (environment.IsDevelopment())
                options.ListenLocalhost(httpsPort, listen => listen.UseHttps(CreateHttpsOptions(certificateStore)));
            else
                options.ListenAnyIP(httpsPort, listen => listen.UseHttps(CreateHttpsOptions(certificateStore)));
            return;
        }

        if (!File.Exists(pfxPath))
            throw new InvalidOperationException($"TLS certificate PFX not found: {pfxPath}");

        var serverCert = string.IsNullOrEmpty(pfxPassword)
            ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null, keyStorageFlags: PfxKeyStorage)
            : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, pfxPassword, PfxKeyStorage);

        certificateStore.TryLoadFromPfx(pfxPath, pfxPassword);
        if (environment.IsDevelopment())
            options.ListenLocalhost(httpsPort, listen => listen.UseHttps(serverCert));
        else
            options.ListenAnyIP(httpsPort, listen => listen.UseHttps(serverCert));
    }

    private static HttpsConnectionAdapterOptions CreateHttpsOptions(TlsCertificateStore certificateStore) =>
        new()
        {
            ServerCertificateSelector = (_, _) => certificateStore.Current
        };

    private static string ResolvePfxPath(SitesHostOptions hostOptions)
    {
        var certDirectory = hostOptions.CertDirectory.Trim();
        if (Path.IsPathRooted(certDirectory))
            return Path.Combine(certDirectory, hostOptions.CertPfxFileName);

        return Path.Combine(AppContext.BaseDirectory, certDirectory, hostOptions.CertPfxFileName);
    }
}
