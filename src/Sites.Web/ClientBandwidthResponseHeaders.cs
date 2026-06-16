using System.Globalization;
using Microsoft.AspNetCore.Http;
using Sites.Web.Caching;

namespace Sites.Web;

internal static class ClientBandwidthResponseHeaders
{
    public static string ComputeEntityTag(ReadOnlySpan<byte> body) => ProxyCacheEntityTags.Compute(body);

    public static void ApplyBrowserCache(HttpContext context, ClientBandwidthOptions options)
    {
        if (!options.SendCacheControl)
            return;

        var maxAgeSeconds = (int)Math.Min(options.BrowserCacheMaxAge.TotalSeconds, int.MaxValue);
        context.Response.Headers.CacheControl = $"public, max-age={maxAgeSeconds.ToString(CultureInfo.InvariantCulture)}";
        context.Response.Headers.Remove("Pragma");
        context.Response.Headers.Remove("Expires");
    }

    public static void ApplyEntityTag(HttpContext context, ClientBandwidthOptions options, string entityTag)
    {
        if (!options.EnableNotModified)
            return;

        context.Response.Headers.ETag = entityTag;
    }

    public static bool TryWriteNotModified(
        HttpContext context,
        ClientBandwidthOptions options,
        string entityTag,
        Action<HttpContext, ClientBandwidthOptions>? applyCacheHeaders = null)
    {
        if (!options.EnableNotModified)
            return false;

        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            return false;

        if (!context.Request.Headers.IfNoneMatch.Contains(entityTag, StringComparer.Ordinal))
            return false;

        context.Response.StatusCode = StatusCodes.Status304NotModified;
        context.Response.Headers.ETag = entityTag;
        (applyCacheHeaders ?? ApplyBrowserCache)(context, options);
        context.Response.Headers["X-Proxy-Cache"] = "HIT";
        return true;
    }

    public static void ApplyLocalAssetsCache(HttpContext context, ClientBandwidthOptions options)
    {
        if (!options.SendCacheControl)
            return;

        context.Response.Headers.CacheControl = BuildLocalAssetsCacheControl(options);
        context.Response.Headers.Remove("Pragma");
        context.Response.Headers.Remove("Expires");
    }

    public static void ApplyForcedDownloadCache(HttpContext context, ClientBandwidthOptions options)
    {
        if (!options.SendCacheControl)
            return;

        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Remove("Pragma");
        context.Response.Headers.Remove("Expires");
    }

    public static string BuildLocalAssetsCacheControl(ClientBandwidthOptions options)
    {
        var maxAgeSeconds = (int)Math.Min(options.LocalAssetsMaxAge.TotalSeconds, int.MaxValue);
        return $"public, max-age={maxAgeSeconds.ToString(CultureInfo.InvariantCulture)}";
    }
}
