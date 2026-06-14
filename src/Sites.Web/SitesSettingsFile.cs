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

    public static SitesProfileSettingsDocument Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Profile settings file not found.", path);

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<SitesProfileSettingsDocument>(json, ReadOptions)
            ?? throw new InvalidOperationException($"{path}: settings document is invalid.");

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
        new()
        {
            Sites = new SitesProxyOptions
            {
                UpstreamRequestTimeout = source.Sites.UpstreamRequestTimeout,
                Cache = new ProxyCacheOptions
                {
                    RootPath = source.Sites.Cache.RootPath,
                    MaxEntryBytes = source.Sites.Cache.MaxEntryBytes,
                    Ttl = source.Sites.Cache.Ttl,
                    RejectRangeRequests = source.Sites.Cache.RejectRangeRequests,
                    ExcludedContentTypes = source.Sites.Cache.ExcludedContentTypes.ToList()
                }
            }
        };
}
