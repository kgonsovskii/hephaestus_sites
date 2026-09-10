namespace Sites.Web.Abstractions;

public static class SitesGitPatFile
{
    public const string EncryptedFileName = "git-pat.enc";
    public const string EncryptedDataFileName = "git-pat-data.enc";
    public const string LegacyPlainFileName = "github-pat.txt";

    public static string ResolveEncryptedPath(string? repoRoot = null) =>
        Path.Combine(RepositoryPaths.DeployDirectory(repoRoot), EncryptedFileName);

    public static string ResolveEncryptedDataPath(string? repoRoot = null) =>
        Path.Combine(RepositoryPaths.DeployDirectory(repoRoot), EncryptedDataFileName);

    public static string ResolveLegacyPlainPath(string? repoRoot = null) =>
        Path.Combine(RepositoryPaths.DeployDirectory(repoRoot), LegacyPlainFileName);

    public static bool TryLoadToken(string? repoRoot, out string token)
    {
        repoRoot ??= RepositoryPaths.ResolveRoot();
        if (TryLoadEncrypted(ResolveEncryptedPath(repoRoot), out token))
            return true;

        var plainPath = ResolveLegacyPlainPath(repoRoot);
        if (!File.Exists(plainPath))
            return false;

        token = File.ReadAllText(plainPath).Trim();
        return IsUsableToken(token);
    }

    public static bool TryLoadDataToken(string? repoRoot, out string token) =>
        TryLoadEncrypted(ResolveEncryptedDataPath(repoRoot), out token);

    public static string TokenFingerprint(string token) =>
        token.Length >= 20 ? token[..20] + "..." : token;

    public static string BuildAuthenticatedUrl(string repositoryUrl, string token)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid repository URL: {repositoryUrl}");

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only https repository URLs are supported.");

        return $"https://x-access-token:{token}@{uri.Host}{uri.PathAndQuery.TrimEnd('/')}";
    }

    public static bool TryBuildAuthenticatedCloneUrl(string repositoryUrl, string? repoRoot, out string cloneUrl) =>
        TryBuildAuthenticatedUrl(repositoryUrl, TryLoadToken, repoRoot, out cloneUrl);

    public static bool TryBuildAuthenticatedDataCloneUrl(string repositoryUrl, string? repoRoot, out string cloneUrl) =>
        TryBuildAuthenticatedUrl(repositoryUrl, TryLoadDataToken, repoRoot, out cloneUrl);

    private static bool TryBuildAuthenticatedUrl(
        string repositoryUrl,
        TryLoadPat load,
        string? repoRoot,
        out string cloneUrl)
    {
        if (!load(repoRoot, out var token))
        {
            cloneUrl = repositoryUrl;
            return false;
        }

        cloneUrl = BuildAuthenticatedUrl(repositoryUrl, token);
        return true;
    }

    private static bool TryLoadEncrypted(string encPath, out string token)
    {
        token = string.Empty;
        if (!File.Exists(encPath))
            return false;

        try
        {
            token = SitesGitCrypt.Decrypt(File.ReadAllText(encPath)).Trim();
            return IsUsableToken(token);
        }
        catch
        {
            token = string.Empty;
            return false;
        }
    }

    private static bool IsUsableToken(string token) =>
        token.Length > 0
        && !token.StartsWith("ghp_your", StringComparison.OrdinalIgnoreCase)
        && !token.StartsWith("github_pat_your", StringComparison.OrdinalIgnoreCase);

    private delegate bool TryLoadPat(string? repoRoot, out string token);
}
