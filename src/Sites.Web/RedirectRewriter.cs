using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web;

public static class RedirectRewriter
{
    public static bool IsOutboundRedirectPath(HttpRequest request, ISiteModule site)
    {
        if (!site.EnableOutboundRedirectPaths)
            return false;

        var prefixes = site.OutboundRedirectPathPrefixes;
        if (prefixes.Count == 0)
            return false;

        var path = request.Path.Value ?? "/";
        return prefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            (path.Length == prefix.Length || path[prefix.Length] == '/'));
    }

    public static string ResolveOutboundRedirectTarget(HttpRequest request, ISiteModule site) =>
        ResolveRedirectTarget(request, site, site.ExternalRedirectUrl);

    public static string ResolveRedirectTarget(HttpRequest request, ISiteModule site, string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Uri.TryCreate(configured, UriKind.Absolute, out _))
                return configured;

            var publicBase = PublicTargetResolver.Resolve(request, site).BaseUrl.TrimEnd('/');
            return $"{publicBase}/{configured.TrimStart('/')}";
        }

        return $"{PublicTargetResolver.Resolve(request, site).BaseUrl.TrimEnd('/')}/";
    }

    public static string RewriteAllowedLocation(
        string location,
        HttpRequest request,
        ISiteModule site)
    {
        if (location.Length == 0)
            return location;

        if (site.RedirectForeignRequests && ForeignRequestRewriter.IsForeignUrl(site, location, request))
            return ResolveRedirectTarget(request, site, site.RedirectForeignRequestsUrl);

        if (!Uri.TryCreate(location, UriKind.Absolute, out var absolute))
        {
            if (Uri.TryCreate(location, UriKind.Relative, out _))
            {
                var baseUrl = PublicTargetResolver.Resolve(request, site).BaseUrl.TrimEnd('/');
                return $"{baseUrl}/{location.TrimStart('/')}";
            }

            return location;
        }

        if (!SourceHostMatcher.MatchesSourceHost(site, absolute.Host))
            return location;

        var publicBase = PublicTargetResolver.Resolve(request, site).BaseUrl;
        return $"{publicBase}{absolute.PathAndQuery}";
    }
}
