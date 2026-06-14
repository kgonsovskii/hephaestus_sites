namespace Sites.Web;

public sealed class BlockedPathsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BlockedPathsMiddleware> _logger;

    public BlockedPathsMiddleware(RequestDelegate next, ILogger<BlockedPathsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var site = context.GetSite();
        var path = context.Request.Path.Value ?? "/";
        var blockedPrefixes = site.Rules.BlockedPathPrefixes
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Select(NormalizePrefix)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (IsBlocked(path, blockedPrefixes))
        {
            _logger.LogInformation(
                "[{Site}] Handled local-only path {Method} {Path}",
                site.Name,
                context.Request.Method,
                path);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(DateTimeOffset.Now.ToString("O"), context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool IsBlocked(string path, IReadOnlyList<string> blockedPrefixes)
    {
        foreach (var prefix in blockedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

            var withoutTrailingSlash = prefix.TrimEnd('/');
            if (withoutTrailingSlash.Length > 0 &&
                path.Equals(withoutTrailingSlash, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = prefix.Trim();
        if (!normalized.StartsWith('/'))
            normalized = $"/{normalized}";

        return normalized.EndsWith('/') ? normalized : $"{normalized}/";
    }
}
