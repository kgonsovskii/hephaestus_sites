using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class RepositoryPathsTests
{
    [Fact]
    public void TryResolveRoot_FromSharedOutputDirectory_FindsRepoRootNotOutput()
    {
        var repoRoot = FindRepoRoot();
        var outputDir = Path.Combine(repoRoot, "output");

        Directory.CreateDirectory(outputDir);

        var resolved = RepositoryPaths.TryResolveRoot(outputDir);

        Assert.Equal(repoRoot, resolved);
    }

    [Fact]
    public void DeployDirectory_FromOutput_UsesRepoRootDeployFolder()
    {
        var repoRoot = FindRepoRoot();
        var outputDir = Path.Combine(repoRoot, "output");

        var deployDir = RepositoryPaths.DeployDirectory(outputDir);

        Assert.Equal(Path.Combine(repoRoot, "deploy"), deployDir);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "Sites.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
