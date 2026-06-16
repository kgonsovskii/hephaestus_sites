using System.Text.Json;
using System.Text.Json.Serialization;
using Sites.Web.Abstractions;
using Sites.Web.Caching;

namespace Sites.Web;

public static class SitesSettingsFile
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

    public const string DefaultFileName = "settings.json";

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
            var repoPath = SitesProfileResolver.ResolveSettingsJsonPath(repoRoot);
            if (File.Exists(repoPath))
                return repoPath;
        }

        return Path.GetFullPath(publishedPath);
    }

    public static SitesProfileSettingsDocument LoadOrCreate(string path, SitesProfileSettingsDocument defaults)
    {
        if (!File.Exists(path))
        {
            Save(path, defaults);
            return CloneDocument(defaults);
        }

        return Load(path);
    }

    public static SitesProfileSettingsDocument Load(string path, SitesProfileSettingsDocument? mergeDefaults = null)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Profile settings file not found.", path);

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<SitesProfileSettingsDocument>(json, ReadOptions)
            ?? throw new InvalidOperationException($"{path}: settings document is invalid.");

        if (mergeDefaults is not null)
            MergeMissingSections(document, mergeDefaults);

        SitesProfileSettingsValidator.Validate(document.Sites);
        return document;
    }

    public static void Save(string path, SitesProfileSettingsDocument document)
    {
        SitesProfileSettingsValidator.Validate(document.Sites);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(document, WriteOptions);
        File.WriteAllText(path, json + Environment.NewLine);
    }

    private static SitesProfileSettingsDocument CloneDocument(SitesProfileSettingsDocument source) =>
        new() { Sites = SitesProxyOptionsCloner.Clone(source.Sites) };

    private static void MergeMissingSections(
        SitesProfileSettingsDocument target,
        SitesProfileSettingsDocument defaults)
    {
        target.Sites.Cache ??= new ProxyCacheOptions();
        target.Sites.ClientBandwidth ??= new ClientBandwidthOptions();

        var defaultSites = defaults.Sites;
        if (target.Sites.UpstreamRequestTimeout <= TimeSpan.Zero)
            target.Sites.UpstreamRequestTimeout = defaultSites.UpstreamRequestTimeout;

        if (target.Sites.Cache.Ttl <= TimeSpan.Zero)
        {
            target.Sites.Cache.Ttl = defaultSites.Cache.Ttl;
            target.Sites.Cache.MaxEntryBytes = defaultSites.Cache.MaxEntryBytes;
            target.Sites.Cache.RejectRangeRequests = defaultSites.Cache.RejectRangeRequests;
            if (target.Sites.Cache.ExcludedContentTypes is not { Count: > 0 })
                target.Sites.Cache.ExcludedContentTypes = defaultSites.Cache.ExcludedContentTypes.ToList();
        }

        var bw = target.Sites.ClientBandwidth;
        if (bw.BrowserCacheMaxAge <= TimeSpan.Zero || bw.LocalAssetsMaxAge <= TimeSpan.Zero)
            target.Sites.ClientBandwidth = SitesProxyOptionsCloner.Clone(defaultSites).ClientBandwidth;
    }
}
