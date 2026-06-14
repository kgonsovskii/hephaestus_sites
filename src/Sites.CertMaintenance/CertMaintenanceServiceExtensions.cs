using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sites.CertMaintenance;

public static class CertMaintenanceServiceExtensions
{
    public static IServiceCollection AddSitesCertMaintenance(
        this IServiceCollection services,
        IConfiguration configuration,
        TlsCertificateStore certificateStore)
    {
        services.AddSingleton(certificateStore);
        services.AddSingleton<AcmeChallengeRegistry>();
        services.AddSingleton<LetsEncryptCertificateIssuer>();
        services
            .AddOptions<CertMaintenanceOptions>()
            .Bind(configuration.GetSection(CertMaintenanceOptions.SectionName));
        services.AddSingleton<CertMaintenanceWorker>();
        services.AddHostedService<CertMaintenanceHostedService>();
        return services;
    }

    public static WebApplication UseSitesCertMaintenance(this WebApplication app)
    {
        app.UseMiddleware<AcmeChallengeMiddleware>();
        return app;
    }
}
