using System.Text.Json;

namespace Sites.Web.Caching;

public sealed class ProxyDiskCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SitesProfileSettingsService _settings;

    public ProxyDiskCache(SitesProfileSettingsService settings) => _settings = settings;

    private ProxyCacheOptions CacheOptions => _settings.Get().Cache;

    private string? RootOverride =>
        string.IsNullOrWhiteSpace(CacheOptions.RootPath) ? null : CacheOptions.RootPath;

    public async Task<CachedProxyResponse?> TryGetAsync(
        string sourceHost,
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var siteDirectory = ProxyCacheRoot.GetSiteDirectory(sourceHost, RootOverride);
        var metaPath = GetMetaPath(siteDirectory, cacheKey);
        var bodyPath = GetBodyPath(siteDirectory, cacheKey);

        if (!File.Exists(metaPath) || !File.Exists(bodyPath))
            return null;

        await using var metaStream = new FileStream(
            metaPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous);

        var metadata = await JsonSerializer.DeserializeAsync<CacheEntryMetadata>(
            metaStream,
            JsonOptions,
            cancellationToken);

        if (metadata is null || metadata.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        var body = await File.ReadAllBytesAsync(bodyPath, cancellationToken);

        return new CachedProxyResponse
        {
            StatusCode = metadata.StatusCode,
            ContentType = metadata.ContentType,
            Body = body,
            ExpiresAt = metadata.ExpiresAt,
            EntityTag = metadata.EntityTag
        };
    }

    public async Task TrySetAsync(
        string sourceHost,
        string cacheKey,
        int statusCode,
        string? contentType,
        byte[] body,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (body.Length == 0 || body.Length > CacheOptions.MaxEntryBytes)
            return;

        var siteDirectory = ProxyCacheRoot.GetSiteDirectory(sourceHost, RootOverride);
        Directory.CreateDirectory(siteDirectory);

        var shard = cacheKey[..2];
        var shardDirectory = Path.Combine(siteDirectory, shard);
        Directory.CreateDirectory(shardDirectory);

        var metaPath = GetMetaPath(siteDirectory, cacheKey);
        var bodyPath = GetBodyPath(siteDirectory, cacheKey);
        var tempBodyPath = bodyPath + ".tmp";
        var tempMetaPath = metaPath + ".tmp";

        var metadata = new CacheEntryMetadata
        {
            StatusCode = statusCode,
            ContentType = contentType,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
            BodyLength = body.Length,
            EntityTag = ProxyCacheEntityTags.Compute(body)
        };

        await File.WriteAllBytesAsync(tempBodyPath, body, cancellationToken);

        await using (var metaStream = new FileStream(
            tempMetaPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.Asynchronous))
        {
            await JsonSerializer.SerializeAsync(metaStream, metadata, JsonOptions, cancellationToken);
        }

        File.Move(tempBodyPath, bodyPath, overwrite: true);
        File.Move(tempMetaPath, metaPath, overwrite: true);
    }

    public ProxyCacheClearResult ClearAll()
    {
        var cacheRoot = ProxyCacheRoot.Resolve(RootOverride);
        if (!Directory.Exists(cacheRoot))
            return new ProxyCacheClearResult(cacheRoot, 0);

        var removed = 0;
        foreach (var entry in Directory.EnumerateFileSystemEntries(cacheRoot))
        {
            if (Directory.Exists(entry))
                Directory.Delete(entry, recursive: true);
            else
                File.Delete(entry);

            removed++;
        }

        return new ProxyCacheClearResult(cacheRoot, removed);
    }

    public ProxyCacheClearResult ClearNonBinary(ProxyCachePolicy policy)
    {
        var cacheRoot = ProxyCacheRoot.Resolve(RootOverride);
        if (!Directory.Exists(cacheRoot))
            return new ProxyCacheClearResult(cacheRoot, 0);

        var removed = 0;
        foreach (var metaPath in Directory.EnumerateFiles(cacheRoot, "*.meta", SearchOption.AllDirectories))
        {
            CacheEntryMetadata? metadata;
            try
            {
                var json = File.ReadAllText(metaPath);
                metadata = JsonSerializer.Deserialize<CacheEntryMetadata>(json, JsonOptions);
            }
            catch (IOException)
            {
                continue;
            }
            catch (JsonException)
            {
                continue;
            }

            if (metadata is null || !policy.IsTextCacheClearable(metadata.ContentType))
                continue;

            var bodyPath = metaPath[..^".meta".Length] + ".body";
            TryDeleteFile(metaPath);
            TryDeleteFile(bodyPath);
            removed++;
        }

        return new ProxyCacheClearResult(cacheRoot, removed);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    private static string GetMetaPath(string siteDirectory, string cacheKey) =>
        Path.Combine(siteDirectory, cacheKey[..2], $"{cacheKey}.meta");

    private static string GetBodyPath(string siteDirectory, string cacheKey) =>
        Path.Combine(siteDirectory, cacheKey[..2], $"{cacheKey}.body");

    private sealed class CacheEntryMetadata
    {
        public int StatusCode { get; init; }

        public string? ContentType { get; init; }

        public DateTimeOffset ExpiresAt { get; init; }

        public int BodyLength { get; init; }

        public string? EntityTag { get; init; }
    }
}
