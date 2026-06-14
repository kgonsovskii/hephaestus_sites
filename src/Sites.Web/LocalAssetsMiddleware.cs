using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class LocalAssetsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalAssetsMiddleware> _logger;

    public LocalAssetsMiddleware(RequestDelegate next, ILogger<LocalAssetsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var site = context.GetSite();
        var requestPath = NormalizePath(context.Request.Path.Value ?? string.Empty);
        var assets = site.Rules.LocalAssets;

        if (assets.TryGetValue(requestPath, out var mappedRelativePath))
        {
            if (await TryServeMappedAssetAsync(context, site, requestPath, mappedRelativePath))
                return;

            await _next(context);
            return;
        }

        if (TryGetAdditionsFileRelativePath(
                requestPath,
                site.Rules.AdditionsPathPrefix,
                site.TargetHost,
                out var additionsRelativePath) &&
            await TryServeAdditionAsync(context, site, requestPath, additionsRelativePath))
            return;

        await _next(context);
    }

    private async Task<bool> TryServeMappedAssetAsync(
        HttpContext context,
        ISiteModule site,
        string requestPath,
        string relativePath)
    {
        var filePath = ResolveMappedAssetPath(site, relativePath);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning(
                "[{Site}] Local asset mapped for {RequestPath} but file missing at {FilePath}",
                site.Name,
                requestPath,
                filePath);

            return false;
        }

        _logger.LogInformation(
            "[{Site}] Serving local asset {RequestPath} from {FilePath}",
            site.Name,
            requestPath,
            filePath);

        await WriteFileResponseAsync(
            context,
            filePath,
            cacheControl: "no-cache, no-store, must-revalidate");

        return true;
    }

    private async Task<bool> TryServeAdditionAsync(
        HttpContext context,
        ISiteModule site,
        string requestPath,
        string relativePath)
    {
        if (!TryResolveUnderWebRoot(site.WebRootPath, relativePath, out var filePath))
        {
            _logger.LogWarning(
                "[{Site}] Rejected unsafe additions path {RequestPath}",
                site.Name,
                requestPath);

            return false;
        }

        if (!File.Exists(filePath))
            return false;

        _logger.LogInformation(
            "[{Site}] Serving addition {RequestPath} from {FilePath}",
            site.Name,
            requestPath,
            filePath);

        await WriteFileResponseAsync(
            context,
            filePath,
            cacheControl: "public, max-age=3600");

        return true;
    }

    private static async Task WriteFileResponseAsync(
        HttpContext context,
        string filePath,
        string cacheControl)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = StaticContentTypes.FromFilePath(filePath);
        context.Response.Headers.CacheControl = cacheControl;
        context.Response.Headers.Pragma = cacheControl.Contains("no-cache", StringComparison.Ordinal)
            ? "no-cache"
            : string.Empty;
        context.Response.Headers.Expires = cacheControl.Contains("no-cache", StringComparison.Ordinal)
            ? "0"
            : string.Empty;

        await context.Response.SendFileAsync(filePath, context.RequestAborted);
    }

    internal static bool TryGetAdditionsRelativePath(
        string requestPath,
        string additionsPathPrefix,
        out string relativePath) =>
        TryGetAdditionsFileRelativePath(requestPath, additionsPathPrefix, domain: string.Empty, out relativePath);

    internal static bool TryGetAdditionsFileRelativePath(
        string requestPath,
        string additionsPathPrefix,
        string domain,
        out string relativePath)
    {
        relativePath = string.Empty;
        var prefix = NormalizePrefix(additionsPathPrefix);
        if (prefix.Length == 0)
            return false;

        if (!requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var prefixWithoutTrailingSlash = prefix.TrimEnd('/');
            if (!requestPath.Equals(prefixWithoutTrailingSlash, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var underPrefix = requestPath[prefix.TrimEnd('/').Length..].TrimStart('/');
        if (underPrefix.Length == 0)
            underPrefix = "index.html";

        relativePath = string.IsNullOrWhiteSpace(domain)
            ? $"x/{underPrefix}"
            : $"{domain.Trim()}/{underPrefix}";

        return true;
    }

    internal static bool TryResolveUnderWebRoot(string webRootPath, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var normalizedRelative = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        if (normalizedRelative.Contains("..", StringComparison.Ordinal))
            return false;

        var rootFullPath = Path.GetFullPath(webRootPath);
        var candidate = Path.GetFullPath(Path.Combine(rootFullPath, normalizedRelative.Replace('/', Path.DirectorySeparatorChar)));

        if (!candidate.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate, rootFullPath, StringComparison.OrdinalIgnoreCase))
            return false;

        fullPath = candidate;
        return true;
    }

    private static string ResolveMappedAssetPath(ISiteModule site, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var targetHost = site.TargetHost.Trim();

        if (!normalized.StartsWith($"{targetHost}/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalized, targetHost, StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"{targetHost}/{normalized}";
        }

        return ResolveAssetPath(site.WebRootPath, normalized);
    }

    private static string ResolveAssetPath(string webRootPath, string relativePath)
    {
        if (TryResolveUnderWebRoot(webRootPath, relativePath, out var resolved))
            return resolved;

        var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(webRootPath, normalizedRelative));
    }

    private static string NormalizePath(string path)
    {
        if (path.Length == 0)
            return "/";

        return path.StartsWith('/') ? path : $"/{path}";
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = prefix.Trim();
        if (normalized.Length == 0)
            return string.Empty;

        if (!normalized.StartsWith('/'))
            normalized = $"/{normalized}";

        return normalized.EndsWith('/') ? normalized : $"{normalized}/";
    }
}
