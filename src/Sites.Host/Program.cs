using Sites.CertMaintenance;
using Sites.Cp;
using Sites.Host;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Git;

SitesProfileResolver.Initialize();

var selectedSiteName = Environment.GetEnvironmentVariable("SITES_SITE")
    ?? ParseSelectedSiteName(args);

var builder = WebApplication.CreateBuilder(args);
var certificateStore = new TlsCertificateStore();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Limits.MaxRequestHeadersTotalSize = 128 * 1024;
    KestrelCertificateConfiguration.ConfigureEndpoints(
        options,
        context.Configuration,
        certificateStore,
        context.HostingEnvironment);
});

builder.Services.AddSitesCp(
    builder.Configuration,
    typeof(Program).Assembly,
    "Sites.Modules");
builder.Services.AddSitesProxyEngineFromReferencedAssembly(
    builder.Configuration,
    typeof(Program).Assembly,
    "Sites.Modules",
    selectedSiteName);
builder.Services.AddSitesCertMaintenance(builder.Configuration, certificateStore);

var app = builder.Build();

var registry = app.Services.GetRequiredService<SiteRegistry>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Sites.Host");
var repoRoot = RepositoryPaths.TryResolveRoot(app.Environment.ContentRootPath);
if (repoRoot is not null)
{
    logger.LogInformation(
        "Profile '{Profile}' -> sites {SitesJsonPath}, settings {SettingsJsonPath}",
        SitesProfileResolver.Current,
        SitesProfileResolver.ResolveSitesJsonPath(repoRoot),
        SitesProfileResolver.ResolveSettingsJsonPath(repoRoot));
}

BootstrapGitSync(app.Services, logger);
LogHostEndpoints(app.Configuration, app.Environment, logger);

if (registry.IsSingleSiteMode)
{
    var site = registry.ActiveSites[0];
    logger.LogInformation(
        "Sites host running in single-site mode for '{SiteName}' ({Source} -> {Target})",
        site.Name,
        site.SourceBaseUrl,
        site.TargetBaseUrl);
}
else
{
    logger.LogInformation(
        "Sites host running in multi-site mode with {Count} sites: {Sites}",
        registry.ActiveSites.Count,
        string.Join(", ", registry.ActiveSites.Select(site => site.Name)));
}

app.UseSitesCertMaintenance();
app.UseSitesCp();
app.UseSitesProxyPipeline();

await app.StartAsync();
await CertMaintenanceBootstrap.TryBootstrapAsync(app.Services);
await app.WaitForShutdownAsync();

static void LogHostEndpoints(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
{
    var hostOptions = configuration.GetSection(SitesHostOptions.SectionName).Get<SitesHostOptions>()
        ?? new SitesHostOptions();

    if (hostOptions.HttpsPort > 0)
    {
        logger.LogInformation(
            "Listening on HTTP port {HttpPort} and HTTPS port {HttpsPort}",
            hostOptions.HttpPort,
            hostOptions.HttpsPort);
    }
    else
    {
        logger.LogInformation("Listening on HTTP port {HttpPort} (HTTPS disabled)", hostOptions.HttpPort);
    }

    if (environment.IsDevelopment())
        logger.LogInformation("Open http://127.0.0.1:{Port}/", hostOptions.HttpPort);
}

static string? ParseSelectedSiteName(string[] args)
{
    foreach (var arg in args)
    {
        if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Length > 2)
            return arg[2..];

        if (!arg.StartsWith('-') && !arg.Contains('='))
            return arg;
    }

    return null;
}

static void BootstrapGitSync(IServiceProvider services, ILogger logger)
{
    try
    {
        var git = services.GetRequiredService<SitesGitService>();
        var result = git.SyncAsync().GetAwaiter().GetResult();
        if (result.Succeeded)
            logger.LogInformation("Sites git bootstrap sync: {Message}", result.Message);
        else
            logger.LogWarning("Sites git bootstrap sync: {Message}", result.Message);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Sites git bootstrap sync failed.");
    }
}
