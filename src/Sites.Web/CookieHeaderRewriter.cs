using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web;

internal static class CookieHeaderRewriter
{
    public static bool UseNamespacing(HttpRequest request)
    {
        var host = request.Host.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    public static string PrefixForSite(ISiteModule site)
    {
        var slug = site.TargetHost
            .Replace(".", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
        return $"__s.{slug}.";
    }

    public static string RewriteSetCookieForProxy(string setCookie, ISiteModule site, HttpRequest request)
    {
        var parts = setCookie.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return setCookie;

        var first = parts[0];
        if (UseNamespacing(request) && site.PassCookies)
            first = PrefixCookiePair(first, PrefixForSite(site));

        var rewritten = new List<string> { first };

        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.StartsWith("domain=", StringComparison.OrdinalIgnoreCase))
                continue;

            if (part.StartsWith("samesite=", StringComparison.OrdinalIgnoreCase)
                && request.IsHttps
                && part.Contains("none", StringComparison.OrdinalIgnoreCase))
            {
                rewritten.Add("SameSite=None");
                rewritten.Add("Secure");
                continue;
            }

            rewritten.Add(part);
        }

        if (request.IsHttps && !rewritten.Any(p => p.StartsWith("Secure", StringComparison.OrdinalIgnoreCase)))
            rewritten.Add("Secure");

        return string.Join("; ", rewritten);
    }

    public static string? FilterCookiesForUpstream(string? cookieHeader, ISiteModule site, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(cookieHeader))
            return null;

        if (!UseNamespacing(request) || !site.PassCookies)
            return cookieHeader;

        var prefix = PrefixForSite(site);
        var upstream = new List<string>();

        foreach (var segment in cookieHeader.Split(';', StringSplitOptions.TrimEntries))
        {
            if (!segment.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var stripped = segment[prefix.Length..].Trim();
            if (stripped.Length > 0)
                upstream.Add(stripped);
        }

        return upstream.Count == 0 ? null : string.Join("; ", upstream);
    }

    private static string PrefixCookiePair(string nameValuePair, string prefix)
    {
        var equalsIndex = nameValuePair.IndexOf('=');
        if (equalsIndex <= 0)
            return prefix + nameValuePair;

        var name = nameValuePair[..equalsIndex];
        var value = nameValuePair[equalsIndex..];
        return prefix + name + value;
    }
}
