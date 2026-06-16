namespace Sites.Web.Abstractions;

public sealed class SiteProxyRules
{
    public IReadOnlyList<ContentReplacement> ContentReplacements { get; init; } = [];

    /// <summary>
    /// Raw HTML snippets injected into matching proxied HTML responses.
    /// </summary>
    public IReadOnlyList<HtmlInjection> HtmlInjections { get; init; } = [];

    public Dictionary<string, string> LocalAssets { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional site settings from sites.json (e.g. for $Key$ substitution in local .js assets).
    /// </summary>
    public Dictionary<string, string> Settings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> BlockedPathPrefixes { get; init; } = [];

    public bool EnableOutboundRedirectPaths { get; init; } = true;

    public IReadOnlyList<string> OutboundRedirectPathPrefixes { get; init; } = [];

    /// <summary>
    /// Optional override for outbound redirect paths. When null, users are sent to the public site root.
    /// </summary>
    public string? ExternalRedirectUrl { get; init; }

    /// <summary>
    /// When true, off-site links and upstream redirects to other domains are rewritten to stay on this site.
    /// </summary>
    public bool RedirectForeignRequests { get; init; } = true;

    /// <summary>
    /// Optional override for foreign redirects. When null, users are sent to the public site root.
    /// </summary>
    public string? RedirectForeignRequestsUrl { get; init; }

    /// <summary>
    /// When true, disk cache lookup and storage are skipped for this site.
    /// </summary>
    public bool DisableCaching { get; init; }

    /// <summary>
    /// When true, browser cookies are forwarded upstream and Set-Cookie is passed to the client (rewritten for the proxy host).
    /// Required for JS antibot flows (e.g. Wildberries wbaas).
    /// </summary>
    public bool PassCookies { get; init; } = true;
}
