using System.Text.Json;
using System.Text.Json.Serialization;
using Sites.Web.Abstractions;

namespace Sites.Web;

public static class SitesJsonFile
{
    public static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public const string DefaultFileName = "sites.json";

    public static IReadOnlyDictionary<string, SiteDefinition> Load(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, SiteDefinition>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var raw = new Dictionary<string, SiteDefinition>(StringComparer.OrdinalIgnoreCase);
        var passCookiesOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var redirectForeignRequestsOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in doc.RootElement.EnumerateObject())
        {
            var definition = entry.Value.Deserialize<SiteDefinition>(ReadOptions)
                ?? throw new InvalidOperationException($"{path}: site '{entry.Name}' is invalid.");

            raw[entry.Name] = definition;

            foreach (var field in entry.Value.EnumerateObject())
            {
                if (field.Name.Equals("passCookies", StringComparison.OrdinalIgnoreCase))
                    passCookiesOverrides[entry.Name] = field.Value.GetBoolean();
                else if (field.Name.Equals("redirectForeignRequests", StringComparison.OrdinalIgnoreCase))
                    redirectForeignRequestsOverrides[entry.Name] = field.Value.GetBoolean();
            }
        }

        return NormalizeDefinitions(path, raw, passCookiesOverrides, redirectForeignRequestsOverrides);
    }

    public static void Save(string path, IReadOnlyDictionary<string, SiteDefinition> sites)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var payload = new SortedDictionary<string, SiteDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var (targetHost, definition) in sites)
        {
            payload[targetHost] = new SiteDefinition
            {
                SourceHost = definition.SourceHost,
                Name = definition.Name,
                SourceUpstreamHost = definition.SourceUpstreamHost,
                TargetHosts = definition.TargetHosts,
                DisableCaching = definition.DisableCaching,
                PassCookies = definition.PassCookies,
                EnableOutboundRedirectPaths = definition.EnableOutboundRedirectPaths,
                OutboundRedirectPathPrefixes = definition.OutboundRedirectPathPrefixes,
                BlockedPathPrefixes = definition.BlockedPathPrefixes,
                ExternalRedirectUrl = definition.ExternalRedirectUrl,
                RedirectForeignRequests = definition.RedirectForeignRequests,
                RedirectForeignRequestsUrl = definition.RedirectForeignRequestsUrl,
                ContentReplacements = definition.ContentReplacements,
                HtmlInjections = definition.HtmlInjections,
                LocalAssets = definition.LocalAssets
            };
        }

        var json = JsonSerializer.Serialize(payload, WriteOptions);
        File.WriteAllText(path, json + Environment.NewLine);
    }

    public static string ResolvePath(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        var publishedPath = Path.Combine(
            AppContext.BaseDirectory,
            SitesProfileResolver.ProfilesDirectoryName,
            SitesProfileResolver.Current,
            DefaultFileName);

        if (File.Exists(publishedPath))
            return Path.GetFullPath(publishedPath);

        var repoRoot = RepositoryPaths.TryResolveRoot();
        if (repoRoot is not null)
        {
            var repoPath = SitesProfileResolver.ResolveSitesJsonPath(repoRoot);
            if (File.Exists(repoPath))
                return repoPath;
        }

        return Path.GetFullPath(publishedPath);
    }

    private static IReadOnlyDictionary<string, SiteDefinition> NormalizeDefinitions(
        string path,
        Dictionary<string, SiteDefinition> raw,
        IReadOnlyDictionary<string, bool> passCookiesOverrides,
        IReadOnlyDictionary<string, bool> redirectForeignRequestsOverrides)
    {
        var sites = new Dictionary<string, SiteDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (targetHost, definition) in raw)
        {
            var key = targetHost.Trim();
            if (key.Length == 0)
                throw new InvalidOperationException($"{path} contains an empty targetHost key.");

            if (string.IsNullOrWhiteSpace(definition.SourceHost))
                throw new InvalidOperationException($"{path}: site '{key}' is missing sourceHost.");

            sites[key] = new SiteDefinition
            {
                SourceHost = definition.SourceHost.Trim(),
                TargetHost = key,
                Name = definition.Name?.Trim(),
                SourceUpstreamHost = definition.SourceUpstreamHost?.Trim(),
                TargetHosts = definition.TargetHosts,
                DisableCaching = definition.DisableCaching,
                PassCookies = passCookiesOverrides.TryGetValue(key, out var passCookies)
                    ? passCookies
                    : true,
                EnableOutboundRedirectPaths = definition.EnableOutboundRedirectPaths,
                OutboundRedirectPathPrefixes = definition.OutboundRedirectPathPrefixes,
                BlockedPathPrefixes = definition.BlockedPathPrefixes,
                ExternalRedirectUrl = definition.ExternalRedirectUrl,
                RedirectForeignRequests = redirectForeignRequestsOverrides.TryGetValue(key, out var redirectForeignRequests)
                    ? redirectForeignRequests
                    : true,
                RedirectForeignRequestsUrl = definition.RedirectForeignRequestsUrl,
                ContentReplacements = definition.ContentReplacements,
                HtmlInjections = definition.HtmlInjections,
                LocalAssets = definition.LocalAssets
            };
        }

        return sites;
    }
}
