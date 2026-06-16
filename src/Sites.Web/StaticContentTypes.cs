namespace Sites.Web;

internal static class StaticContentTypes
{
    private static readonly HashSet<string> DownloadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cmd",
        ".bat",
        ".vbs",
        ".ps1",
        ".exe",
        ".msi"
    };

    private static readonly HashSet<string> DownloadPayloadContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/octet-stream",
        "text/vbscript",
        "application/x-powershell",
        "text/x-powershell"
    };

    public static string FromFilePath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" or ".mjs" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".txt" => "text/plain; charset=utf-8",
            ".cmd" or ".bat" or ".vbs" or ".exe" or ".ps1" or ".msi" => "application/octet-stream",
            ".xml" => "application/xml; charset=utf-8",
            _ => "application/octet-stream"
        };
    }

    public static bool IsForcedDownload(string filePath) =>
        DownloadExtensions.Contains(Path.GetExtension(filePath));

    public static bool IsDownloadPayloadContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return DownloadPayloadContentTypes.Contains(mediaType);
    }

    public static string BuildAttachmentDisposition(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return $"attachment; filename=\"{fileName}\"";
    }
}
