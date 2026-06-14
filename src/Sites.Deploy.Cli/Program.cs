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
            var (remoteProfile, deployArgs) = RemoteDeployProfile.SplitProfileArg(args);

            var configuration = BuildConfiguration(deployArgs);
            var options = configuration.GetSection(DeployOptions.SectionName).Get<DeployOptions>()
                ?? new DeployOptions();

            var creds = RemoteCredsFile.Load(AppContext.BaseDirectory, options);
            var remoteScript = BuildRemoteScript(AppContext.BaseDirectory, options, remoteProfile);

            Console.WriteLine($"Sites deploy -> {creds.Login}@{creds.Server} ({options.Label})");
            Console.WriteLine($"Remote profile: {remoteProfile} (local profile.txt is ignored)");
            Console.WriteLine($"Git repo: {options.GitRepositoryUrl}");

            Console.WriteLine("[1/1] SSH: install git/dotnet, clone GitHub repo, dotnet publish on VPS, restart systemd");

            var sshpass = await RemoteDeployRunner.EnsureSshPassAsync(Console.WriteLine);
            var emit = static (string line, CancellationToken _) =>
            {
                Console.WriteLine(line);
                return Task.CompletedTask;
            };

            var sshCode = await RemoteDeployRunner.RunRemoteBashAsync(
                sshpass,
                creds.Server,
                creds.Login,
                creds.Password,
                remoteScript,
                emit);

            if (sshCode != 0)
            {
                Console.Error.WriteLine($"Remote install failed with exit {sshCode}");
                return sshCode;
            }

            Console.WriteLine("Deploy done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string BuildRemoteScript(string baseDirectory, DeployOptions options, string remoteProfile)
    {
        var scriptPath = ResolveRemoteScriptPath(baseDirectory);
        var bootstrap = RemoteDeployRunner.LoadRemoteInstallBootstrapScript(scriptPath);
        return RemoteDeployRunner.PrependDeployExports(options, bootstrap, remoteProfile);
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
