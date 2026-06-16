namespace Sites.Web.Abstractions;

public sealed class SiteDefinition
{
    public string SourceHost { get; init; } = string.Empty;

    public string TargetHost { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? SourceUpstreamHost { get; init; }

    public IReadOnlyList<string>? TargetHosts { get; init; }

    public bool DisableCaching { get; init; }

    public bool PassCookies { get; init; } = true;

    public bool EnableOutboundRedirectPaths { get; init; } = true;

    public IReadOnlyList<string>? OutboundRedirectPathPrefixes { get; init; }

    public IReadOnlyList<string>? BlockedPathPrefixes { get; init; }

    public string? AdditionsPathPrefix { get; init; }

    public string? ExternalRedirectUrl { get; init; }

    public bool RedirectForeignRequests { get; init; } = true;

    public string? RedirectForeignRequestsUrl { get; init; }

    public IReadOnlyList<ContentReplacement>? ContentReplacements { get; init; }

    /// <summary>
    /// HTML snippets injected into proxied pages on matching paths (e.g. partner scripts on home page).
    /// </summary>
    public IReadOnlyList<HtmlInjection>? HtmlInjections { get; init; }

    public Dictionary<string, string>? LocalAssets { get; init; }
}
