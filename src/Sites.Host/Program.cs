using Sites.CertMaintenance;
using Sites.Cp;
using Sites.DataFtp;
using Sites.Host;
using Sites.Track;
using Sites.Web;
using Sites.Web.Abstractions;

SitesProfileResolver.Initialize();

var selectedSiteName = Environment.GetEnvironmentVariable("SITES_SITE")
    ?? ParseSelectedSiteName(args);
if (selectedSiteName is not null)
    SitesProfileForSite.UseProfileThatContains(selectedSiteName);

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration.GetValue("Git:Enabled", true))
{
    using var bootLog = LoggerFactory.Create(b => b.AddSimpleConsole(options => options.SingleLine = true));
    Sites.Web.Git.SitesDataGitRunner.EnsureSynced(bootLog.CreateLogger("Sites.Host"));
}
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
builder.Services.AddSitesVerboseHttpLogging();
builder.Logging.AddSitesVerboseHttpLogLevels();
builder.Services.AddSitesDataFtp(builder.Configuration);
builder.Services.AddSitesProxyEngineFromReferencedAssembly(
    builder.Configuration,
    typeof(Program).Assembly,
    "Sites.Modules",
    selectedSiteName);
builder.Services.AddSitesCertMaintenance(builder.Configuration, certificateStore);
builder.Services.AddSitesTrack(builder.Configuration);

var app = builder.Build();

var registry = app.Services.GetRequiredService<SiteRegistry>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Sites.Host");
var repoRoot = RepositoryPaths.TryResolveRoot(app.Environment.ContentRootPath);
if (repoRoot is not null)
{
    logger.LogInformation(
        "Profile '{Profile}' -> data {DataDir}, sites {SitesJsonPath}, settings {SettingsJsonPath}, wwwroot {WebRootPath}",
        SitesProfileResolver.Current,
        SitesProfileResolver.ResolveSitesDataBase(repoRoot),
        SitesProfileResolver.ResolveSitesJsonPath(repoRoot),
        SitesProfileResolver.ResolveSettingsJsonPath(repoRoot),
        SitesProfileResolver.ResolveWebRootPath(repoRoot));
}

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

app.UseHttpLogging();
app.UseSitesCertMaintenance();
app.UseSitesCp();
app.UseSitesProxyPipeline(branch => branch.UseSitesTrack());

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
        logger.LogInformation("Open http://127.0.0.1:{Port}/ (single-site: run.bat <targetHost>)", hostOptions.HttpPort);
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

