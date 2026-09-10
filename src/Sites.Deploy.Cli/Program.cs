using Microsoft.Extensions.Configuration;
using Sites.RemoteDeploy;
using Sites.Web.Abstractions;

namespace Sites.Deploy.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var (_, deployArgs) = RemoteDeployProfile.SplitProfileArg(args);

            var configuration = BuildConfiguration(deployArgs);
            var options = configuration.GetSection(DeployOptions.SectionName).Get<DeployOptions>()
                ?? new DeployOptions();

            var targets = RemoteCredsFile.LoadAll(AppContext.BaseDirectory, options);
            var bootstrap = LoadBootstrap(AppContext.BaseDirectory);

            Console.WriteLine($"Sites deploy: {targets.Count} server(s) in parallel ({options.Label})");
            Console.WriteLine("Remote profiles come from deploy/install-remote-creds.txt (overwritten on each target)");
            Console.WriteLine($"Git repo: {options.GitRepositoryUrl}");
            foreach (var t in targets)
                Console.WriteLine($"  - {t.Login}@{t.Server}  profile {t.Profile}");
            Console.WriteLine("SSH: write $HOME/profile.txt, install git/dotnet, clone, publish, restart systemd");

            var sshpass = await RemoteDeployRunner.EnsureSshPassAsync(Console.WriteLine);
            var consoleLock = new object();
            var results = await RemoteDeployParallel.RunAsync(
                sshpass,
                targets,
                creds => RemoteDeployRunner.PrependDeployExports(options, bootstrap, creds.Profile),
                (host, line, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    lock (consoleLock)
                        Console.WriteLine($"[{host}] {line}");
                    return Task.CompletedTask;
                });

            Console.WriteLine();
            Console.WriteLine(RemoteDeployParallel.FormatReport(results));
            return results.All(r => r.Succeeded) ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string LoadBootstrap(string baseDirectory)
    {
        var scriptPath = ResolveRemoteScriptPath(baseDirectory);
        return RemoteDeployRunner.LoadRemoteInstallBootstrapScript(scriptPath);
    }

    private static string ResolveRemoteScriptPath(string baseDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(RepositoryPaths.DeployDirectory(baseDirectory), RemoteDeployRunner.DefaultRemoteScriptFileName),
            Path.Combine(baseDirectory, "deploy", RemoteDeployRunner.DefaultRemoteScriptFileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("Remote install script not found.", candidates[0]);
    }

    private static IConfiguration BuildConfiguration(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("sites-deploy.appsettings.json", optional: true, reloadOnChange: false)
            .AddCommandLine(args);

        var repoRoot = RepositoryPaths.TryResolveRoot(AppContext.BaseDirectory);
        if (repoRoot is not null)
        {
            builder.AddJsonFile(
                Path.Combine(repoRoot, "src", "Sites.Deploy.Cli", "appsettings.json"),
                optional: true,
                reloadOnChange: false);
        }

        return builder.Build();
    }
}
