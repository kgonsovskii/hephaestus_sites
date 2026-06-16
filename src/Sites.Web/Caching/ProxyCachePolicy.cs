using Microsoft.AspNetCore.Http;

namespace Sites.Web.Caching;

public sealed class ProxyCachePolicy
{
    private readonly SitesProfileSettingsService _settings;

    public ProxyCachePolicy(SitesProfileSettingsService settings) => _settings = settings;

    private ProxyCacheOptions Options => _settings.Get().Cache;

    private SitesProxyOptions Settings => _settings.Get();

    public long MaxEntryBytes => Options.MaxEntryBytes;

    public TimeSpan Ttl => Options.Ttl;

    public bool IsCacheLookupRequest(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method))
            return IsCacheableRequest(request);

        if (!HttpMethods.IsHead(request.Method))
            return false;

        if (!Settings.ClientBandwidth.ServeHeadFromCache)
            return false;

        if (Options.RejectRangeRequests && request.Headers.ContainsKey("Range"))
            return false;

        return true;
    }

    public bool IsCacheableRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;

        if (Options.RejectRangeRequests && request.Headers.ContainsKey("Range"))
            return false;

        return true;
    }

    public bool MightBeCacheableContentType(string? contentType) =>
        !IsExcludedContentType(contentType);

    public bool IsTextCacheClearable(string? contentType)
    {
        if (IsExcludedContentType(contentType))
            return false;

        if (string.IsNullOrWhiteSpace(contentType))
            return true;

        var mediaType = contentType.Split(';', 2)[0].Trim();

        if (StaticContentTypes.IsDownloadPayloadContentType(mediaType))
            return true;

        return MightBeCacheableContentType(contentType);
    }

    public bool IsCacheableResponse(
        HttpRequest request,
        int statusCode,
        string? contentType,
        long bodyLength)
    {
        if (!IsCacheableRequest(request))
            return false;

        if (statusCode != StatusCodes.Status200OK)
            return false;

        if (bodyLength <= 0 || bodyLength > Options.MaxEntryBytes)
            return false;

        return !IsExcludedContentType(contentType);
    }

    public static bool ShouldRewriteBeforeCaching(string? contentType) =>
        ContentRewriter.ShouldRewrite(contentType);

    public TimeSpan GetTtl() => Options.Ttl;

    private bool IsExcludedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();

        foreach (var excluded in Options.ExcludedContentTypes)
        {
            if (string.IsNullOrWhiteSpace(excluded))
                continue;

            if (excluded.EndsWith('/'))
            {
                if (mediaType.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (mediaType.Equals(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
