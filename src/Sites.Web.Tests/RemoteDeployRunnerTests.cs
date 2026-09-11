using Sites.RemoteDeploy;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class RemoteDeployRunnerTests
{
    [Fact]
    public void PrependDeployExports_UsesExplicitProfileNotCurrent()
    {
        SitesProfileResolver.Initialize();
        var options = new DeployOptions
        {
            GitRepositoryUrl = "https://github.com/example/repo.git",
            ServiceName = "sites-host"
        };

        var script = RemoteDeployRunner.PrependDeployExports(options, "echo ok\n", "staging");

        Assert.Contains("export SITES_PROFILE='staging'", script);
        Assert.Contains("export SITES_DATA_GIT_REPO='https://github.com/kgonsovskii/hephaestus_sites_data.git'", script);
        Assert.DoesNotContain("hephaestus_sites_staging", script);
    }

    [Fact]
    public void PrependDeployExports_ThrowsWhenProfileEmpty()
    {
        var options = new DeployOptions { GitRepositoryUrl = "https://github.com/example/repo.git" };

        Assert.Throws<ArgumentException>(() =>
            RemoteDeployRunner.PrependDeployExports(options, "echo ok\n", "   "));
    }

    [Fact]
    public void InstallRemoteScript_RequiresSitesProfile()
    {
        var repoRoot = RepositoryPaths.TryResolveRoot()
            ?? throw new InvalidOperationException("Repository root not found.");
        var script = File.ReadAllText(Path.Combine(RepositoryPaths.DeployDirectory(repoRoot), "install-remote.txt"));

        Assert.Contains(": \"${SITES_PROFILE:?SITES_PROFILE is required for remote install}\"", script);
        Assert.Contains("PROFILE_FILE=\"$(dirname \"${SITES_CLONE_DIR}\")/profile.txt\"", script);
        Assert.DoesNotContain("SITES_PROFILE=\"${SITES_PROFILE:-default}\"", script);
        Assert.Contains("ensure_pkg git", script);
        Assert.Contains("ensure_pkg ca-certificates", script);
        Assert.Contains("ensure_pkg curl", script);
        Assert.Contains("ensure_pkg dotnet-sdk-10.0", script);
        Assert.Contains("ensure_pkg dotnet-runtime-10.0", script);
        Assert.Contains("ensure_pkg aspnetcore-runtime-10.0", script);
        Assert.DoesNotContain("apt_get install -y git ca-certificates curl", script);
        Assert.DoesNotContain("--only-upgrade", script);
    }

    [Fact]
    public void InstallLocalScript_InstallsPostgres()
    {
        var repoRoot = RepositoryPaths.TryResolveRoot()
            ?? throw new InvalidOperationException("Repository root not found.");
        var deploy = RepositoryPaths.DeployDirectory(repoRoot);
        var installLocal = File.ReadAllText(Path.Combine(deploy, "install-local.sh"));
        var installPostgres = File.ReadAllText(Path.Combine(deploy, "install-postgres.sh"));
        var waitSh = File.ReadAllText(Path.Combine(deploy, "wait.sh"));
        var setupSql = File.ReadAllText(Path.Combine(deploy, "setup-postgres.sql"));

        Assert.Contains("install-postgres.sh", installLocal);
        Assert.Contains("postgresql.service", installLocal);
        Assert.Contains("ensure_pkg()", waitSh);
        Assert.Contains("pkg_installed()", waitSh);
        Assert.Contains("ensure_pkg postgresql", installPostgres);
        Assert.Contains("ensure_pkg postgresql-client", installPostgres);
        Assert.DoesNotContain("apt_get install -y postgresql postgresql-client", installPostgres);
        Assert.Contains("DROP DATABASE IF EXISTS sites", setupSql);
        Assert.Contains("flow", setupSql);
        Assert.Contains("domain", setupSql);
        Assert.Contains("goal_at", setupSql);
    }

    [Fact]
    public void InstallDataScript_ResetsExistingDataRepo()
    {
        var repoRoot = RepositoryPaths.TryResolveRoot()
            ?? throw new InvalidOperationException("Repository root not found.");
        var script = File.ReadAllText(Path.Combine(RepositoryPaths.DeployDirectory(repoRoot), "install-data.sh"));

        Assert.Contains("git-pat-data.enc", script);
        Assert.Contains("updating existing data repo from origin", script);
        Assert.Contains("reset --hard", script);
        Assert.DoesNotContain("skip clone (runtime git syncs it)", script);
    }
}
