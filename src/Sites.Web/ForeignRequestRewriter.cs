using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Sites.Web.Abstractions;

namespace Sites.Web;

public static partial class ForeignRequestRewriter
{
    [GeneratedRegex(
        @"(?<attr>href|action)(?<eq>\s*=\s*)(?<quote>[""'])(?<url>(?:https?:)?//[^""']+)\k<quote>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForeignLinkRegex();

    public static bool IsForeignUrl(ISiteModule site, string url, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return IsForeignHost(site, absolute.Host, request);

        if (url.StartsWith("//", StringComparison.Ordinal) &&
            Uri.TryCreate("https:" + url, UriKind.Absolute, out absolute))
            return IsForeignHost(site, absolute.Host, request);

        return false;
    }

    public static string RewriteForeignLinks(string content, HttpRequest request, ISiteModule site)
    {
        if (!site.RedirectForeignRequests)
            return content;

        var target = RedirectRewriter.ResolveRedirectTarget(
            request,
            site,
            site.RedirectForeignRequestsUrl);

        return ForeignLinkRegex().Replace(content, match =>
        {
            var url = match.Groups["url"].Value;
            if (!IsForeignUrl(site, url, request))
                return match.Value;

            return $"{match.Groups["attr"].Value}{match.Groups["eq"].Value}{match.Groups["quote"].Value}{target}{match.Groups["quote"].Value}";
        });
    }

    private static bool IsForeignHost(ISiteModule site, string host, HttpRequest request)
    {
        if (SourceHostMatcher.MatchesSourceHost(site, host))
            return false;

        if (site.TargetHosts.Any(targetHost =>
                host.Equals(targetHost, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (host.Equals(request.Host.Host, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
