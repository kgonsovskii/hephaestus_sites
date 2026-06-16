using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class LocalAssetsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalAssetsMiddleware> _logger;
    private readonly SitesProfileSettingsService _settings;

    public LocalAssetsMiddleware(
        RequestDelegate next,
        ILogger<LocalAssetsMiddleware> logger,
        SitesProfileSettingsService settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var site = context.GetSite();
        var requestPath = NormalizePath(context.Request.Path.Value ?? string.Empty);
        var assets = site.Rules.LocalAssets;

        if (assets.TryGetValue(requestPath, out var mappedRelativePath) &&
            await TryServeMappedAssetAsync(context, site, requestPath, mappedRelativePath))
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

        await WriteFileResponseAsync(context, filePath);

        return true;
    }

    private async Task WriteFileResponseAsync(HttpContext context, string filePath)
    {
        var bandwidth = _settings.Get().ClientBandwidth;
        var fileInfo = new FileInfo(filePath);
        var entityTag =
            $"\"{fileInfo.LastWriteTimeUtc.Ticks.ToString("x")}-{fileInfo.Length.ToString("x")}\"";

        if (ClientBandwidthResponseHeaders.TryWriteNotModified(
                context,
                bandwidth,
                entityTag,
                ClientBandwidthResponseHeaders.ApplyLocalAssetsCache))
            return;

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = StaticContentTypes.FromFilePath(filePath);
        ClientBandwidthResponseHeaders.ApplyLocalAssetsCache(context, bandwidth);
        ClientBandwidthResponseHeaders.ApplyEntityTag(context, bandwidth, entityTag);

        if (HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.ContentLength = fileInfo.Length;
            return;
        }

        await context.Response.SendFileAsync(filePath, context.RequestAborted);
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
}
