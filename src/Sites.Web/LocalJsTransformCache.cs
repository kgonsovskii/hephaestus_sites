using System.Collections.Concurrent;
using System.Text;

namespace Sites.Web;

internal static class LocalJsTransformCache
{
    private sealed record CacheEntry(byte[] Body, string EntityTag);

    private static readonly ConcurrentDictionary<string, CacheEntry> Entries = new(StringComparer.Ordinal);

    public static bool TryGet(string cacheKey, out byte[] body, out string entityTag)
    {
        if (Entries.TryGetValue(cacheKey, out var entry))
        {
            body = entry.Body;
            entityTag = entry.EntityTag;
            return true;
        }

        body = [];
        entityTag = string.Empty;
        return false;
    }

    public static void Set(string cacheKey, byte[] body, string entityTag) =>
        Entries[cacheKey] = new CacheEntry(body, entityTag);

    public static string BuildKey(
        string targetHost,
        string filePath,
        FileInfo fileInfo,
        IReadOnlyDictionary<string, string> settings)
    {
        var builder = new StringBuilder(256);
        builder.Append(targetHost);
        builder.Append('|');
        builder.Append(filePath);
        builder.Append('|');
        builder.Append(fileInfo.LastWriteTimeUtc.Ticks.ToString("x"));
        builder.Append('|');
        builder.Append(fileInfo.Length.ToString("x"));
        builder.Append('|');

        foreach (var pair in settings.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value);
            builder.Append(';');
        }

        return builder.ToString();
    }

    internal static void ClearForTests() => Entries.Clear();

    public static void Clear() => Entries.Clear();
}
