using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sites.Cp.Controllers;
using Sites.Cp.Services;
using Sites.RemoteDeploy;
using Sites.Web;
using Sites.Web.Git;

namespace Sites.Cp;

public static class SitesCpExtensions
{
    public const string AuthCookieName = "SitesCpAuth";

    public static IServiceCollection AddSitesCp(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly entryAssembly,
        string modulesAssemblyName,
        string? sitesJsonPath = null)
    {
        services.Configure<CpOptions>(configuration.GetSection(CpOptions.SectionName));
        services.Configure<DeployOptions>(configuration.GetSection(DeployOptions.SectionName));
        services.AddSitesGitMaintenance(configuration);

        services.AddSingleton<SitesCloneRunStore>();
        services.AddScoped<SitesCloneService>();
        services.AddSingleton<SitesUpdateService>();

        services.AddSingleton(sp =>
        {
            var modulesAssembly = SiteModuleDiscovery.ResolveModulesAssembly(entryAssembly, modulesAssemblyName);
            var registry = sp.GetRequiredService<SiteRegistry>();
            return new SitesCatalogService(modulesAssembly, registry, sitesJsonPath);
        });

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                var pathPrefix = configuration.GetSection(CpOptions.SectionName).Get<CpOptions>()?.PathPrefix ?? "/cp";
                options.Cookie.Name = AuthCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = pathPrefix;
                options.LoginPath = "/auth/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });

        services.AddAuthorization();
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(SitesCpExtensions).Assembly);

        return services;
    }

    public static WebApplication UseSitesCp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<CpOptions>>().Value;
        var cpPrefix = new PathString(options.PathPrefix);

        app.UseWhen(
            context => context.Request.Path.StartsWithSegments(cpPrefix, StringComparison.OrdinalIgnoreCase, out _, out _),
            branch =>
            {
                branch.Use(RewriteCpPrefix);
                ConfigureCpBranch(branch, options);
            });

        return app;
    }

    private static async Task RewriteCpPrefix(HttpContext context, RequestDelegate next)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<CpOptions>>().Value;
        var prefix = new PathString(options.PathPrefix);

        if (!context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining))
        {
            await next(context);
            return;
        }

        var rest = remaining;
        if (!rest.HasValue || string.IsNullOrEmpty(rest.Value))
            rest = new PathString("/");
        else if (rest.Value![0] != '/')
            rest = new PathString("/" + rest.Value);

        context.Request.Path = rest;
        context.Request.PathBase = context.Request.PathBase.Add(matched);
        await next(context);
    }

    private static void ConfigureCpBranch(IApplicationBuilder branch, CpOptions options)
    {
        branch.UseRouting();
        branch.UseAuthentication();
        branch.UseAuthorization();
        branch.Use(CpAuthGate(options));
        branch.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private static Func<HttpContext, RequestDelegate, Task> CpAuthGate(CpOptions options) =>
        async (context, next) =>
        {
            if (string.IsNullOrWhiteSpace(options.AdminPassword))
            {
                await next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            if (path.Equals("/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                await next(context);
                return;
            }

            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Redirect($"{context.Request.PathBase}/auth/login");
        };

    public static bool ValidatePassword(CpOptions options, string password) =>
        !string.IsNullOrWhiteSpace(options.AdminPassword) &&
        string.Equals(options.AdminPassword, password, StringComparison.Ordinal);
}
