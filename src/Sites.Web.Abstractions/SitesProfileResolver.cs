namespace Sites.Web.Abstractions;

public static class SitesProfileResolver
{
    public const string DefaultProfile = "default";
    public const string ProfileFileName = "profile.txt";
    public const string ProfilesDirectoryName = "profiles";
    public const string SitesJsonFileName = "sites.json";
    public const string SettingsJsonFileName = "settings.json";
    public const string ProfileEnvironmentVariable = "SITES_PROFILE";

    public static string Current { get; private set; } = DefaultProfile;

    public static void Initialize(string? startDirectory = null)
    {
        if (TryReadEnvironmentProfile(out var fromEnv))
        {
            Current = fromEnv;
            return;
        }

        var repoRoot = RepositoryPaths.TryResolveRoot(startDirectory) ?? startDirectory;
        if (repoRoot is not null && TryReadProfileFile(repoRoot, out var fromFile))
        {
            Current = fromFile;
            return;
        }

        Current = DefaultProfile;
    }

    public static string ResolveProfileFilePath(string repositoryRoot)
    {
        var repoRoot = Path.GetFullPath(repositoryRoot);
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve profile file beside repository root '{repoRoot}': no parent directory.");
        return Path.GetFullPath(Path.Combine(parent, ProfileFileName));
    }

    public static string ResolveProfileDirectory(string repositoryRoot, string? profile = null) =>
        Path.GetFullPath(Path.Combine(
            Path.GetFullPath(repositoryRoot),
            ProfilesDirectoryName,
            profile ?? Current));

    public static string ResolveSitesJsonPath(string repositoryRoot, string? profile = null) =>
        Path.Combine(ResolveProfileDirectory(repositoryRoot, profile), SitesJsonFileName);

    public static string ResolveSettingsJsonPath(string repositoryRoot, string? profile = null) =>
        Path.Combine(ResolveProfileDirectory(repositoryRoot, profile), SettingsJsonFileName);

    public static void WriteProfileFile(string repositoryRoot, string profileName)
    {
        var profile = NormalizeProfileName(profileName);
        var path = ResolveProfileFilePath(repositoryRoot);
        File.WriteAllText(path, profile + Environment.NewLine);
        Current = profile;
    }

    public static string ResolveCloneDirectory(string? homeDirectory = null)
    {
        homeDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(homeDirectory))
            homeDirectory = OperatingSystem.IsWindows() ? @"C:\Users\root" : "/root";

        return Path.Combine(homeDirectory, "hephaestus_sites");
    }

    public static string NormalizeProfileName(string value)
    {
        var profile = value.Trim();
        if (profile.Length == 0)
            throw new ArgumentException("Profile name is required.", nameof(value));

        if (profile.Contains('/') || profile.Contains('\\') || profile.Contains(".."))
            throw new ArgumentException("Profile name must be a single path segment.", nameof(value));

        return profile;
    }

    private static bool TryReadEnvironmentProfile(out string profile)
    {
        profile = string.Empty;
        var value = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        profile = NormalizeProfileName(value);
        return true;
    }

    private static bool TryReadProfileFile(string repositoryRoot, out string profile)
    {
        profile = string.Empty;
        var path = ResolveProfileFilePath(repositoryRoot);
        if (!File.Exists(path))
            return false;

        var line = File.ReadLines(path).FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(line))
            throw new InvalidOperationException($"Profile file '{path}' is empty.");

        profile = NormalizeProfileName(line);
        return true;
    }
}
