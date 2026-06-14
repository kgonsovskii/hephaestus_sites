namespace Sites.Web.Abstractions;

public interface ISiteModule
{
    string Name { get; }
    string SourceBaseUrl { get; }
    string SourceHost { get; }
    string SourceUpstreamHost { get; }
    string TargetHost { get; }
    string TargetBaseUrl { get; }
    IReadOnlyList<string> TargetHosts { get; }
    SiteProxyRules Rules { get; }
    string WebRootPath { get; }

    /// <summary>
    /// Optional override for outbound redirect paths. When null, users are sent to the public site root.
    /// </summary>
    string? ExternalRedirectUrl => Rules.ExternalRedirectUrl;

    IReadOnlyList<string> OutboundRedirectPathPrefixes => Rules.OutboundRedirectPathPrefixes;

    bool DisableCaching => Rules.DisableCaching;

    bool PassCookies => Rules.PassCookies;
}
