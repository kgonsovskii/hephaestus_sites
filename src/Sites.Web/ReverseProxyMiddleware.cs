using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sites.Web.Abstractions;
using Sites.Web.Caching;

namespace Sites.Web;

public sealed class ReverseProxyMiddleware
{
    private const int MaxUpstreamRedirects = 10;

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailers",
        "Transfer-Encoding",
        "Upgrade"
    };

    private static readonly HashSet<string> RequestHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Accept-Encoding"
    };

    private static readonly HashSet<string> ResponseHeadersToSkipAlways = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Encoding",
        "Content-Length"
    };

    private static readonly HashSet<string> ResponseHeadersToSkipWithoutCookies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Set-Cookie"
    };

    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProxyDiskCache _cache;
    private readonly ProxyCachePolicy _cachePolicy;
    private readonly SitesProfileSettingsService _settings;

    public ReverseProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        ProxyDiskCache cache,
        ProxyCachePolicy cachePolicy,
        SitesProfileSettingsService settings)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _cachePolicy = cachePolicy;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        var site = context.GetSite();

        if (RedirectRewriter.IsOutboundRedirectPath(context.Request, site))
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location =
                RedirectRewriter.ResolveOutboundRedirectTarget(context.Request, site);
            return;
        }

        var cachingEnabled = IsCachingEnabled(context, site);
        var cacheLookupEnabled = cachingEnabled && _cachePolicy.IsCacheLookupRequest(context.Request);

        if (cacheLookupEnabled)
        {
            var cached = await TryGetCachedAsync(context, site);
            if (cached is not null)
            {
                await WriteCachedResponseAsync(context, cached);
                return;
            }
        }

        if (HttpMethods.IsHead(context.Request.Method))
        {
            await ProxyHeadMissAsync(context, site, cachingEnabled);
            return;
        }

        var client = _httpClientFactory.CreateClient("reverse-proxy");

        using var response = await SendUpstreamFollowingRedirectsAsync(
            client,
            context,
            site,
            context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(context, response, site);

        var contentType = response.Content.Headers.ContentType?.ToString();

        if (ContentRewriter.ShouldRewrite(contentType))
        {
            var replacements = GetContentReplacements(site, context.Request);
            var injections = site.Rules.HtmlInjections;
            if (replacements.Count == 0 && injections.Count == 0)
            {
                context.Response.Headers["X-Proxy-Cache"] = cachingEnabled ? "BYPASS" : "DISABLED";
                await context.Response.StartAsync(context.RequestAborted);
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
                return;
            }

            var body = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
            if (replacements.Count > 0)
            {
                body = ContentRewriter.RewriteBytes(
                    body,
                    contentType,
                    replacements);
            }

            if (injections.Count > 0)
            {
                body = HtmlInjector.InjectBytes(
                    body,
                    contentType,
                    context.Request.Path.Value ?? "/",
                    injections);
            }

            if (cachingEnabled &&
                ProxyCachePolicy.ShouldRewriteBeforeCaching(contentType) &&
                _cachePolicy.MightBeCacheableContentType(contentType))
            {
                await TryStoreCacheAsync(
                    context,
                    site,
                    contentType,
                    response,
                    body,
                    includePublicHost: true);
            }

            context.Response.Headers.Remove("Content-Encoding");
            context.Response.Headers.Remove("Content-Length");
            context.Response.ContentLength = body.Length;
            context.Response.Headers["X-Proxy-Cache"] = cachingEnabled ? "MISS" : "DISABLED";
            if (!ApplyClientBandwidthHeaders(context, body))
            {
                await context.Response.StartAsync(context.RequestAborted);
                await context.Response.Body.WriteAsync(body, context.RequestAborted);
            }

            return;
        }

        if (cachingEnabled &&
            _cachePolicy.IsCacheableRequest(context.Request) &&
            response.StatusCode == HttpStatusCode.OK &&
            _cachePolicy.MightBeCacheableContentType(contentType))
        {
            var bodyBytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);

            if (_cachePolicy.IsCacheableResponse(
                    context.Request,
                    (int)response.StatusCode,
                    contentType,
                    bodyBytes.Length))
            {
                await TryStoreCacheAsync(context, site, contentType, response, bodyBytes, includePublicHost: false);

                if (!string.IsNullOrWhiteSpace(contentType))
                    context.Response.ContentType = contentType;

                context.Response.ContentLength = bodyBytes.Length;
                context.Response.Headers["X-Proxy-Cache"] = "MISS";
                if (!ApplyClientBandwidthHeaders(context, bodyBytes))
                    await context.Response.Body.WriteAsync(bodyBytes, context.RequestAborted);
                return;
            }

            if (!string.IsNullOrWhiteSpace(contentType))
                context.Response.ContentType = contentType;

            context.Response.ContentLength = bodyBytes.Length;
            context.Response.Headers["X-Proxy-Cache"] = "BYPASS";
            if (!ApplyClientBandwidthHeaders(context, bodyBytes))
                await context.Response.Body.WriteAsync(bodyBytes, context.RequestAborted);
            await context.Response.Body.WriteAsync(bodyBytes, context.RequestAborted);
            return;
        }

        if (response.Content.Headers.ContentLength is long contentLength)
            context.Response.ContentLength = contentLength;

        context.Response.Headers["X-Proxy-Cache"] = cachingEnabled ? "BYPASS" : "DISABLED";
        await context.Response.StartAsync(context.RequestAborted);
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private static bool IsCachingEnabled(HttpContext context, ISiteModule site)
    {
        if (!site.DisableCaching)
            return true;

        var environment = context.RequestServices.GetService<IHostEnvironment>();
        return environment?.IsDevelopment() == true;
    }

    private async Task<CachedProxyResponse?> TryGetCachedAsync(HttpContext context, ISiteModule site)
    {
        var sharedKey = ProxyCacheKeyBuilder.Build(context.Request, site, includePublicHost: false);
        var cached = await _cache.TryGetAsync(site.SourceHost, sharedKey, context.RequestAborted);
        if (cached is not null)
            return cached;

        var hostSpecificKey = ProxyCacheKeyBuilder.Build(context.Request, site, includePublicHost: true);
        return await _cache.TryGetAsync(site.SourceHost, hostSpecificKey, context.RequestAborted);
    }

    private async Task TryStoreCacheAsync(
        HttpContext context,
        ISiteModule site,
        string? contentType,
        HttpResponseMessage response,
        byte[] body,
        bool includePublicHost)
    {
        if (!_cachePolicy.IsCacheableResponse(
                context.Request,
                (int)response.StatusCode,
                contentType,
                body.Length))
            return;

        var cacheKey = ProxyCacheKeyBuilder.Build(context.Request, site, includePublicHost);
        var ttl = _cachePolicy.GetTtl();
        await _cache.TrySetAsync(
            site.SourceHost,
            cacheKey,
            (int)response.StatusCode,
            contentType,
            body,
            ttl,
            context.RequestAborted);
    }

    private static async Task WriteCachedResponseAsync(HttpContext context, CachedProxyResponse cached)
    {
        var bandwidth = context.RequestServices.GetRequiredService<SitesProfileSettingsService>().Get().ClientBandwidth;
        var entityTag = cached.EntityTag ?? ClientBandwidthResponseHeaders.ComputeEntityTag(cached.Body);

        if (ClientBandwidthResponseHeaders.TryWriteNotModified(context, bandwidth, entityTag))
            return;

        context.Response.StatusCode = cached.StatusCode;

        if (!string.IsNullOrWhiteSpace(cached.ContentType))
            context.Response.ContentType = cached.ContentType;

        context.Response.ContentLength = cached.Body.Length;
        context.Response.Headers["X-Proxy-Cache"] = "HIT";
        ClientBandwidthResponseHeaders.ApplyBrowserCache(context, bandwidth);
        ClientBandwidthResponseHeaders.ApplyEntityTag(context, bandwidth, entityTag);

        if (HttpMethods.IsHead(context.Request.Method))
            return;

        await context.Response.Body.WriteAsync(cached.Body, context.RequestAborted);
    }

    private bool ApplyClientBandwidthHeaders(HttpContext context, ReadOnlySpan<byte> body)
    {
        var bandwidth = _settings.Get().ClientBandwidth;
        var entityTag = ClientBandwidthResponseHeaders.ComputeEntityTag(body);
        if (ClientBandwidthResponseHeaders.TryWriteNotModified(context, bandwidth, entityTag))
            return true;

        ClientBandwidthResponseHeaders.ApplyBrowserCache(context, bandwidth);
        ClientBandwidthResponseHeaders.ApplyEntityTag(context, bandwidth, entityTag);
        return false;
    }

    private async Task ProxyHeadMissAsync(HttpContext context, ISiteModule site, bool cachingEnabled)
    {
        var client = _httpClientFactory.CreateClient("reverse-proxy");

        using var response = await SendUpstreamFollowingRedirectsAsync(
            client,
            context,
            site,
            context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(context, response, site);
        context.Response.Headers["X-Proxy-Cache"] = cachingEnabled ? "BYPASS" : "DISABLED";
        await context.Response.StartAsync(context.RequestAborted);
    }

    private static async Task<HttpResponseMessage> SendUpstreamFollowingRedirectsAsync(
        HttpClient client,
        HttpContext context,
        ISiteModule site,
        CancellationToken cancellationToken)
    {
        var upstreamUri = BuildUpstreamUri(context.Request, site);
        var upstreamReferer = BuildUpstreamReferer(context, site);
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), upstreamUri);
        CopyRequestHeaders(context, request, site, upstreamUri, upstreamReferer);
        await CopyRequestBodyAsync(context, request);

        for (var hop = 0; hop < MaxUpstreamRedirects; hop++)
        {
            var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var statusCode = (int)response.StatusCode;
            if (statusCode is < 300 or >= 400)
                return response;

            var location = response.Headers.Location;
            if (location is null)
                return response;

            var nextUri = location.IsAbsoluteUri
                ? location
                : new Uri(request.RequestUri!, location);

            if (!SourceHostMatcher.ShouldFollowSourceRedirect(site, nextUri.Host))
                return response;

            request.Dispose();
            response.Dispose();

            request = new HttpRequestMessage(new HttpMethod(context.Request.Method), nextUri);
            CopyRequestHeaders(context, request, site, nextUri, upstreamReferer);
        }

        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static Uri BuildUpstreamUri(HttpRequest request, ISiteModule site)
    {
        var path = request.Path.HasValue ? request.Path.Value! : "/";
        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        return new Uri($"https://{site.SourceUpstreamHost.TrimEnd('/')}{path}{query}");
    }

    private static void CopyRequestHeaders(
        HttpContext context,
        HttpRequestMessage request,
        ISiteModule site,
        Uri upstreamUri,
        Uri? upstreamReferer)
    {
        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            if (header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                if (!site.PassCookies)
                    continue;

                var cookieHeader = CookieHeaderRewriter.FilterCookiesForUpstream(
                    header.Value.ToString(),
                    site,
                    context.Request);
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                    request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

                continue;
            }

            if (RequestHeadersToSkip.Contains(header.Key))
                continue;

            if (header.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        request.Headers.Host = upstreamUri.Host;

        if (upstreamReferer is not null)
            request.Headers.Referrer = upstreamReferer;
    }

    private static Uri BuildUpstreamReferer(HttpContext context, ISiteModule site)
    {
        var refererHeader = context.Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(refererHeader) &&
            Uri.TryCreate(refererHeader, UriKind.Absolute, out var clientReferer) &&
            ShouldRewriteRefererToSource(clientReferer, context, site))
        {
            return new Uri($"{site.SourceBaseUrl.TrimEnd('/')}{clientReferer.PathAndQuery}");
        }

        return new Uri($"{site.SourceBaseUrl.TrimEnd('/')}/");
    }

    private static bool ShouldRewriteRefererToSource(Uri clientReferer, HttpContext context, ISiteModule site)
    {
        var (_, publicHost) = PublicTargetResolver.Resolve(context.Request, site);

        if (clientReferer.Authority.Equals(publicHost, StringComparison.OrdinalIgnoreCase))
            return true;

        return site.TargetHosts.Any(targetHost =>
            clientReferer.Host.Equals(targetHost, StringComparison.OrdinalIgnoreCase) ||
            clientReferer.Authority.Equals(targetHost, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task CopyRequestBodyAsync(HttpContext context, HttpRequestMessage request)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsDelete(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
            return;

        if (context.Request.ContentLength is 0)
            return;

        request.Content = new StreamContent(context.Request.Body);

        if (!string.IsNullOrEmpty(context.Request.ContentType))
            request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
    }

    private static void CopyResponseHeaders(HttpContext context, HttpResponseMessage response, ISiteModule site)
    {
        foreach (var header in response.Headers)
            CopyResponseHeader(context, site, header.Key, header.Value);

        foreach (var header in response.Content.Headers)
            CopyResponseHeader(context, site, header.Key, header.Value);

        context.Response.Headers.Remove("transfer-encoding");
    }

    private static void CopyResponseHeader(
        HttpContext context,
        ISiteModule site,
        string key,
        IEnumerable<string> values)
    {
        if (HopByHopHeaders.Contains(key))
            return;

        if (ResponseHeadersToSkipAlways.Contains(key))
            return;

        if (!site.PassCookies && ResponseHeadersToSkipWithoutCookies.Contains(key))
            return;

        if (key.Equals("Location", StringComparison.OrdinalIgnoreCase))
        {
            var location = values.FirstOrDefault() ?? string.Empty;
            var rewritten = RedirectRewriter.RewriteAllowedLocation(location, context.Request, site);
            if (!IsRedirectLoop(context.Request, rewritten))
                context.Response.Headers.Location = rewritten;

            return;
        }

        if (site.PassCookies && key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var cookie in values)
            {
                context.Response.Headers.Append(
                    "Set-Cookie",
                    CookieHeaderRewriter.RewriteSetCookieForProxy(cookie, site, context.Request));
            }

            return;
        }

        context.Response.Headers[key] = values.ToArray();
    }

    private static IReadOnlyList<ContentReplacement> GetContentReplacements(
        ISiteModule site,
        HttpRequest request)
    {
        var (baseUrl, host) = PublicTargetResolver.Resolve(request, site);

        if (site is SiteModuleBase module)
            return module.GetContentReplacements(baseUrl, host);

        if (site is JsonSiteModule jsonSite)
        {
            return SiteContentReplacements.BuildDefaults(
                jsonSite.SourceHost,
                jsonSite.SourceUpstreamHost,
                baseUrl,
                host,
                jsonSite.Definition.ContentReplacements);
        }

        return SiteContentReplacements.BuildDefaults(
            site.SourceHost,
            site.SourceUpstreamHost,
            baseUrl,
            host);
    }

    private static bool IsRedirectLoop(HttpRequest request, string rewrittenLocation)
    {
        if (!Uri.TryCreate(rewrittenLocation, UriKind.Absolute, out var target))
            return false;

        var currentAuthority = request.Host.Value;
        var currentPath = request.Path.HasValue ? request.Path.Value! : "/";
        var currentQuery = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;

        return string.Equals(target.Authority, currentAuthority, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(target.PathAndQuery, $"{currentPath}{currentQuery}", StringComparison.OrdinalIgnoreCase);
    }
}
