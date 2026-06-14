namespace Sites.Web.Abstractions;

public static class RepositoryPaths
{
    public const string SitesSolutionRelativePath = "src/Sites.sln";

    public static string? TryResolveRoot(string? startDirectory = null)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(startDirectory ?? AppContext.BaseDirectory));
        while (dir is not null)
        {
            if (IsRepositoryRoot(dir.FullName))
                return dir.FullName;

            if (File.Exists(Path.Combine(dir.FullName, "Sites.sln")))
            {
                if (string.Equals(dir.Name, "src", StringComparison.OrdinalIgnoreCase) && dir.Parent is not null)
                    return dir.Parent.FullName;

                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static bool IsRepositoryRoot(string directory) =>
        File.Exists(Path.Combine(directory, SitesSolutionRelativePath));

    public static string ResolveRoot(string? startDirectory = null) =>
        TryResolveRoot(startDirectory)
        ?? throw new DirectoryNotFoundException(
            "Could not resolve repository root (expected src/Sites.sln).");

    public static string DeployDirectory(string? startDirectory = null) =>
        Path.Combine(ResolveRoot(startDirectory), "deploy");

    public static string ProfilesDirectory(string? startDirectory = null) =>
        Path.Combine(ResolveRoot(startDirectory), SitesProfileResolver.ProfilesDirectoryName);

    public static string OutputDirectory(string? startDirectory = null) =>
        Path.Combine(ResolveRoot(startDirectory), "output");

    public static string ReleaseDirectory(string? startDirectory = null) =>
        Path.Combine(ResolveRoot(startDirectory), "release");
}
