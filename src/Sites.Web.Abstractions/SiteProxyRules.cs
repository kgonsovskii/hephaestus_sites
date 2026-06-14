namespace Sites.Web.Abstractions;

public sealed class SiteProxyRules
{
    public IReadOnlyList<ContentReplacement> ContentReplacements { get; init; } = [];

    /// <summary>
    /// Raw HTML snippets injected into matching proxied HTML responses.
    /// </summary>
    public IReadOnlyList<HtmlInjection> HtmlInjections { get; init; } = [];

    public Dictionary<string, string> LocalAssets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> BlockedPathPrefixes { get; init; } = [];

    /// <summary>
    /// URL prefix for local static additions served from repo-root wwwroot (e.g. /x/).
    /// </summary>
    public string AdditionsPathPrefix { get; init; } = "/x/";
    public IReadOnlyList<string> OutboundRedirectPathPrefixes { get; init; } = [];

    /// <summary>
    /// Optional override for outbound redirect paths. When null, users are sent to the public site root.
    /// </summary>
    public string? ExternalRedirectUrl { get; init; }

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
