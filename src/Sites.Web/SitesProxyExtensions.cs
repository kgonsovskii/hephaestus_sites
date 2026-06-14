using System.Net;
using System.Net.Http;
using System.Reflection;
using Sites.Web.Abstractions;
using Sites.Web.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sites.Web;

public static class SitesProxyExtensions
{
    public static IServiceCollection AddSitesProxyEngineFromAssembly(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly siteAssembly,
        string? selectedSiteName = null) =>
        services.AddSitesProxyEngine(
            configuration,
            SiteModuleDiscovery.DiscoverFromAssembly(siteAssembly),
            selectedSiteName);

    public static IServiceCollection AddSitesProxyEngineFromReferencedAssembly(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly entryAssembly,
        string assemblyName,
        string? selectedSiteName = null,
        string? sitesJsonPath = null) =>
        services.AddSitesProxyEngine(
            configuration,
            SiteModuleDiscovery.DiscoverFromReferencedAssembly(entryAssembly, assemblyName, sitesJsonPath),
            selectedSiteName);

    public static IServiceCollection AddSitesProxyEngine(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<ISiteModule> sites,
        string? selectedSiteName = null)
    {
        var registry = new SiteRegistry(sites, selectedSiteName);
        services.AddSingleton(registry);
        services.AddSingleton<IReadOnlyList<ISiteModule>>(registry.ActiveSites);

        services
            .AddOptions<SitesProxyOptions>()
            .Bind(configuration.GetSection(SitesProxyOptions.SectionName))
            .Validate(options =>
            {
                SitesProfileSettingsValidator.Validate(options);
                return true;
            })
            .ValidateOnStart();

        services.AddSingleton<SitesProfileSettingsTemplate>();
        services.AddSingleton<SitesProfileSettingsService>();
        services.AddSingleton<ProxyDiskCache>();
        services.AddSingleton<ProxyCachePolicy>();

        services.AddHttpClient("reverse-proxy", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<SitesProfileSettingsService>();
            client.Timeout = settings.Get().UpstreamRequestTimeout;
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = false,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(15)
        });

        return services;
    }

    public static WebApplication UseSitesProxyPipeline(this WebApplication app)
    {
        app.UseMiddleware<OversizedCookieMiddleware>();
        app.UseMiddleware<SiteRoutingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<BlockedPathsMiddleware>();
        app.UseMiddleware<LocalAssetsMiddleware>();
        app.UseMiddleware<ReverseProxyMiddleware>();
        return app;
    }
}

