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

        if (TryParseRedirectUri(location, out var absolute) && absolute is not null)
        {
            if (SourceHostMatcher.MatchesSourceHost(site, absolute.Host))
                return RewriteSourceRedirect(request, site, absolute);

            if (site.RedirectForeignRequests && ForeignRequestRewriter.IsForeignUrl(site, location, request))
                return ResolveRedirectTarget(request, site, site.RedirectForeignRequestsUrl);

            return location;
        }

        if (Uri.TryCreate(location, UriKind.Relative, out _))
        {
            var baseUrl = PublicTargetResolver.Resolve(request, site).BaseUrl.TrimEnd('/');
            return $"{baseUrl}/{location.TrimStart('/')}";
        }

        return location;
    }

    private static bool TryParseRedirectUri(string location, out Uri? absolute)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out absolute))
            return true;

        if (location.StartsWith("//", StringComparison.Ordinal) &&
            Uri.TryCreate("https:" + location, UriKind.Absolute, out absolute))
            return true;

        absolute = null;
        return false;
    }

    private static string RewriteSourceRedirect(HttpRequest request, ISiteModule site, Uri absolute)
    {
        var publicBase = PublicTargetResolver.Resolve(request, site).BaseUrl.TrimEnd('/');
        return $"{publicBase}{absolute.PathAndQuery}";
    }
}
