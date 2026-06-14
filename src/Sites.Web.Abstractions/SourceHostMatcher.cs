namespace Sites.Web.Abstractions;

public static class SourceHostMatcher
{
    public static bool IsExactSourceHost(ISiteModule site, string host) =>
        host.Equals(site.SourceHost, StringComparison.OrdinalIgnoreCase) ||
        host.Equals(site.SourceUpstreamHost, StringComparison.OrdinalIgnoreCase);

    public static bool IsSourceSubdomain(ISiteModule site, string host) =>
        !IsExactSourceHost(site, host) &&
        host.EndsWith($".{site.SourceHost}", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesSourceHost(ISiteModule site, string host) =>
        IsExactSourceHost(site, host) || IsSourceSubdomain(site, host);

    public static bool ShouldFollowSourceRedirect(ISiteModule site, string host) =>
        MatchesSourceHost(site, host);
}
