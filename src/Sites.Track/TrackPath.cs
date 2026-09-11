namespace Sites.Track;

public static class TrackPath
{
    private static readonly string[] SkipPrefixes =
    [
        "/_s/",
        "/t/",
        "/internal/",
        "/cp",
        "/api/",
        "/vendor/",
        "/player/"
    ];

    private static readonly HashSet<string> SkipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".css", ".mjs", ".map",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".avif",
        ".woff", ".woff2", ".ttf", ".eot",
        ".mp4", ".webm", ".m3u8", ".mpd", ".mp3",
        ".zip", ".vbs", ".cmd", ".json", ".xml", ".txt"
    };

    public static bool ShouldRecordPage(string path)
    {
        var normalized = Normalize(path);
        if (normalized == "/t/e" ||
            normalized == "/_s/e" ||
            normalized == "/_s/s.js" ||
            normalized == "/_s/track.js")
            return false;

        foreach (var prefix in SkipPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var ext = Path.GetExtension(normalized);
        if (ext.Length > 0 && SkipExtensions.Contains(ext))
            return false;

        return true;
    }

    public static TrackEventKind PageEvent(string path) =>
        IsVideoPage(path) ? TrackEventKind.Video : TrackEventKind.Hit;

    public static bool IsVideoPage(string path)
    {
        var normalized = Normalize(path);
        return normalized.Equals("/video", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/video/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsVideoPageFromReferer(string? referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            return false;

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate(referer, UriKind.Relative, out uri))
            return false;

        return IsVideoPage(uri.IsAbsoluteUri ? uri.AbsolutePath : referer);
    }

    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        return path.StartsWith('/') ? path : "/" + path;
    }
}
