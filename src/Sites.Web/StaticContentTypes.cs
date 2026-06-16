namespace Sites.Web;

internal static class StaticContentTypes
{
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
            ".vbs" => "text/vbscript; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            _ => "application/octet-stream"
        };
    }
}
