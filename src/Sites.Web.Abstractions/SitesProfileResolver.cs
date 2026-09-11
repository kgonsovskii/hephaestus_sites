namespace Sites.Web.Abstractions;

public static class SitesProfileResolver
{
    public const string DefaultProfile = "default";
    public const string ProfileFileName = "profile.txt";
    public const string SitesDataDirectoryName = "hephaestus_sites_data";
    public const string ProfilesDirectoryName = "profiles";
    public const string SitesJsonFileName = "sites.json";
    public const string SettingsJsonFileName = "settings.json";
    public const string ProfileEnvironmentVariable = "SITES_PROFILE";

    public static string Current { get; private set; } = DefaultProfile;

    public static void Use(string profileName) =>
        Current = NormalizeProfileName(profileName);

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

    public static string ResolveSitesDataBase(string? repositoryRoot = null)
    {
        var repoRoot = Path.GetFullPath(repositoryRoot ?? RepositoryPaths.ResolveRoot());
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve {SitesDataDirectoryName} beside repository root '{repoRoot}': no parent directory.");

        var data = Path.GetFullPath(Path.Combine(parent, SitesDataDirectoryName));
        if (data.StartsWith(repoRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || data.StartsWith(repoRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(data, repoRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{SitesDataDirectoryName} must be a sibling of the sites repository, not inside it.");
        }

        return data;
    }

    public static string ResolveProfileDirectory(string repositoryRoot, string? profile = null) =>
        Path.GetFullPath(Path.Combine(ResolveSitesDataBase(repositoryRoot), profile ?? Current));

    public static string ResolveSitesJsonPath(string repositoryRoot, string? profile = null) =>
        Path.Combine(ResolveProfileDirectory(repositoryRoot, profile), SitesJsonFileName);

    public static string ResolveSettingsJsonPath(string repositoryRoot, string? profile = null) =>
        Path.Combine(ResolveProfileDirectory(repositoryRoot, profile), SettingsJsonFileName);

    public static string ResolveWebRootPath(string? repositoryRoot = null, string? profile = null)
    {
        profile ??= Current;
        var webRootDirectory = WebRootPaths.DefaultDirectoryName;

        var repoRoot = repositoryRoot ?? RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null)
        {
            var path = Path.Combine(ResolveProfileDirectory(repoRoot, profile), webRootDirectory);
            Directory.CreateDirectory(path);
            return Path.GetFullPath(path);
        }

        var dataBase = Directory.GetParent(Path.GetFullPath(AppContext.BaseDirectory))?.FullName;
        var besideApp = Path.Combine(
            dataBase ?? AppContext.BaseDirectory,
            SitesDataDirectoryName,
            profile,
            webRootDirectory);
        Directory.CreateDirectory(besideApp);
        return Path.GetFullPath(besideApp);
    }

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

    public static bool IsCloneDisallowedProfile(string profile) =>
        string.Equals(NormalizeProfileName(profile), DefaultProfile, StringComparison.OrdinalIgnoreCase);

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
