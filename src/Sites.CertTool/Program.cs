using Microsoft.Extensions.Configuration;
using Sites.CertMaintenance;
using Sites.Web.Abstractions;

namespace Sites.CertTool;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            SitesProfileResolver.Initialize();
            var configuration = BuildConfiguration(args);
            var maintenanceOptions = configuration.GetSection(CertMaintenanceOptions.SectionName).Get<CertMaintenanceOptions>()
                ?? new CertMaintenanceOptions();
            var toolOptions = configuration.GetSection(CertToolOptions.SectionName).Get<CertToolOptions>()
                ?? new CertToolOptions();

            if (args.Any(arg => arg.Equals("--staging", StringComparison.OrdinalIgnoreCase)))
                maintenanceOptions.UseStaging = true;

            var repositoryRoot = RepositoryPaths.ResolveRoot();
            var command = ResolveCommand(args);

            return command switch
            {
                "list" => ListDomains(),
                "check" => await CheckAsync(maintenanceOptions, repositoryRoot),
                "publish" => await PublishAsync(maintenanceOptions, toolOptions, repositoryRoot),
                _ => PrintUsage()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int ListDomains()
    {
        var domains = PublishDomainDiscovery.DiscoverFromModulesAssembly();
        Console.WriteLine("Publish domains discovered from site modules:");
        foreach (var domain in domains)
            Console.WriteLine($"  {domain}");

        return 0;
    }

    private static async Task<int> CheckAsync(CertMaintenanceOptions options, string repositoryRoot)
    {
        var domains = PublishDomainDiscovery.DiscoverFromModulesAssembly();
        Console.WriteLine("DNS check (compare with this machine's public IP):");
        await DnsCheck.CheckAsync(domains);
        Console.WriteLine();
        Console.WriteLine($"Certificate output: {Path.Combine(repositoryRoot, options.CertDirectory, options.CertPfxFileName)}");
        return 0;
    }

    private static async Task<int> PublishAsync(
        CertMaintenanceOptions maintenanceOptions,
        CertToolOptions toolOptions,
        string repositoryRoot)
    {
        var domains = PublishDomainDiscovery.DiscoverFromModulesAssembly();
        Console.WriteLine("Publishing TLS certificate for:");
        foreach (var domain in domains)
            Console.WriteLine($"  {domain}");

        Console.WriteLine();
        Console.WriteLine("DNS check:");
        await DnsCheck.CheckAsync(domains);
        Console.WriteLine();

        if (maintenanceOptions.UseStaging)
            Console.WriteLine("Using Let's Encrypt STAGING (not browser-trusted).");
        else
            Console.WriteLine("Using Let's Encrypt PRODUCTION.");

        WarnIfHostIsRunning();

        if (!Path.IsPathRooted(maintenanceOptions.CertDirectory.Trim()))
            maintenanceOptions.CertDirectory = Path.Combine(repositoryRoot, maintenanceOptions.CertDirectory.Trim());

        var issuer = new LetsEncryptCertificateIssuer();
        await using var challengeServer = new StandaloneAcmeChallengeServer(toolOptions.HttpChallengePort);
        challengeServer.Start();
        Console.WriteLine(
            $"Listening for HTTP-01 challenges on port {toolOptions.HttpChallengePort}. Stop Sites.Host if it is running.");

        var result = await issuer.IssueOrRenewAsync(
            maintenanceOptions,
            domains,
            challengeServer);

        Console.WriteLine();
        Console.WriteLine("Done.");
        Console.WriteLine($"  PFX: {result.PfxPath}");
        Console.WriteLine($"  CER: {result.PublicCerPath}");
        Console.WriteLine("Next: dotnet run --project Sites.Host");
        if (result.UsedStaging)
            Console.WriteLine("Staging cert: use only for testing. Re-run without --staging for production.");

        return 0;
    }

    private static void WarnIfHostIsRunning()
    {
        if (System.Diagnostics.Process.GetProcessesByName("Sites.Host").Length == 0)
            return;

        Console.WriteLine("Warning: Sites.Host is running. Prefer leaving it running — it renews certificates automatically.");
    }

    private static string ResolveCommand(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith('-'))
                continue;

            return arg.ToLowerInvariant() switch
            {
                "list" or "check" or "publish" => arg.ToLowerInvariant(),
                _ => "publish"
            };
        }

        return "publish";
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
            Sites.CertTool — manual Let's Encrypt publish (optional)

            Sites.Host renews certificates automatically when CertMaintenance:Enabled is true.
            Use this tool only for a one-off issue while Sites.Host is stopped.

            Commands:
              publish   Issue cert now (default)
              check     DNS preflight
              list      Show publish domains

            Options:
              --staging   Let's Encrypt staging environment

            Examples:
              dotnet run --project Sites.CertTool
              dotnet run --project Sites.CertTool -- check
            """);

        return 0;
    }

    private static IConfiguration BuildConfiguration(string[] args) =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddCommandLine(args)
            .Build();
}
