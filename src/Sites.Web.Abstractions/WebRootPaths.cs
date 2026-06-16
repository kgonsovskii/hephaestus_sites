namespace Sites.Web.Abstractions;

public static class WebRootPaths
{
    public const string DefaultDirectoryName = "wwwroot";

    public static string Resolve(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        return SitesProfileResolver.ResolveWebRootPath();
    }
}
