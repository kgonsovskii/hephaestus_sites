using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sites.Web.Git;

public static class SitesGitServiceCollectionExtensions
{
    public static IServiceCollection AddSitesGitMaintenance(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SitesGitOptions>(configuration.GetSection(SitesGitOptions.SectionName));
        services.AddSingleton<SitesGitService>();
        services.AddSingleton<SitesCatalogChangedSignal>();
        services.AddHostedService<SitesGitMaintenanceHostedService>();

        return services;
    }
}
