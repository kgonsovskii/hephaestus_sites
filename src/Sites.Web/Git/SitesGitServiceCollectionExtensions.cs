using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sites.Web.Git;

public static class SitesGitServiceCollectionExtensions
{
    public static IServiceCollection AddSitesGit(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SitesGitOptions>(configuration.GetSection(SitesGitOptions.SectionName));
        services.AddSingleton<SitesGitService>();
        return services;
    }
}
