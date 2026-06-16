namespace Sites.Web.Abstractions;
public abstract class SiteModuleBase : ISiteModule
{
    protected SiteModuleBase()
    {
        WebRootPath = WebRootPaths.Resolve();
    }

    public virtual string Name => SourceHost.Split('.', 2)[0];
    public abstract string SourceHost { get; }
    public virtual string SourceUpstreamHost => $"www.{SourceHost}";
    public virtual string SourceBaseUrl => $"https://{SourceUpstreamHost}";
    public abstract string TargetHost { get; }
    public virtual string TargetBaseUrl => $"https://{TargetHost}";
    public virtual IReadOnlyList<string> TargetHosts =>
    [
        TargetHost,
        $"www.{TargetHost}"
    ];

    public virtual SiteProxyRules Rules => new()
    {
        ContentReplacements = BuildContentReplacements(TargetBaseUrl, TargetHost),
        LocalAssets = LocalAssets,
        BlockedPathPrefixes = BlockedPathPrefixes,
        AdditionsPathPrefix = AdditionsPathPrefix,
        EnableOutboundRedirectPaths = EnableOutboundRedirectPaths,
        OutboundRedirectPathPrefixes = OutboundRedirectPathPrefixes,
        ExternalRedirectUrl = ExternalRedirectUrl,
        RedirectForeignRequests = RedirectForeignRequests,
        RedirectForeignRequestsUrl = RedirectForeignRequestsUrl,
        DisableCaching = DisableCaching
    };

    /// <summary>
    /// When true, disk cache lookup and storage are skipped for this site.
    /// </summary>
    protected virtual bool DisableCaching => false;

    public string WebRootPath { get; }

    protected virtual IReadOnlyList<ContentReplacement> AdditionalContentReplacements => [];

    protected virtual Dictionary<string, string> LocalAssets =>
        new(StringComparer.OrdinalIgnoreCase);

    protected virtual IReadOnlyList<string> BlockedPathPrefixes => [];

    /// <summary>
    /// URL prefix for local static files under repo-root wwwroot (default /x/).
    /// </summary>
    protected virtual string AdditionsPathPrefix => "/x/";

    /// <summary>
    /// Optional override for outbound redirect paths. When null, users are sent to the public site root.
    /// </summary>
    protected virtual string? ExternalRedirectUrl => null;

    protected virtual bool EnableOutboundRedirectPaths => OutboundRedirectPathPrefixes.Count > 0;

    protected virtual IReadOnlyList<string> OutboundRedirectPathPrefixes => [];

    protected virtual bool RedirectForeignRequests => false;

    protected virtual string? RedirectForeignRequestsUrl => null;

    private IReadOnlyList<ContentReplacement> BuildContentReplacements(
        string targetBaseUrl,
        string targetHost) =>
        SiteContentReplacements.BuildDefaults(
            SourceHost,
            SourceUpstreamHost,
            targetBaseUrl,
            targetHost,
            AdditionalContentReplacements);

    public IReadOnlyList<ContentReplacement> GetContentReplacements(
        string targetBaseUrl,
        string targetHost) =>
        SiteContentReplacements.BuildDefaults(
            SourceHost,
            SourceUpstreamHost,
            targetBaseUrl.TrimEnd('/'),
            targetHost,
            AdditionalContentReplacements);

}
