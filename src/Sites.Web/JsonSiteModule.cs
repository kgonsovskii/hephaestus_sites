using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class JsonSiteModule : ISiteModule
{
    private readonly SiteProxyRules _rules;

    public JsonSiteModule(SiteDefinition definition, string? webRootPath = null)
    {
        Definition = definition;
        WebRootPath = WebRootPaths.Resolve(webRootPath);
        _rules = BuildRules();
    }

    public SiteDefinition Definition { get; }

    public string SourceHost => Definition.SourceHost;

    public string SourceUpstreamHost =>
        string.IsNullOrWhiteSpace(Definition.SourceUpstreamHost)
            ? $"www.{SourceHost}"
            : Definition.SourceUpstreamHost;

    public string SourceBaseUrl => $"https://{SourceUpstreamHost}";

    public string TargetHost => Definition.TargetHost;

    public string TargetBaseUrl => $"https://{TargetHost}";

    public string Name =>
        string.IsNullOrWhiteSpace(Definition.Name)
            ? SourceHost.Split('.', 2)[0]
            : Definition.Name;

    public IReadOnlyList<string> TargetHosts =>
        Definition.TargetHosts is { Count: > 0 }
            ? Definition.TargetHosts
            : [TargetHost, $"www.{TargetHost}"];

    public string WebRootPath { get; }

    public SiteProxyRules Rules => _rules;

    private SiteProxyRules BuildRules() =>
        new()
        {
            ContentReplacements = SiteContentReplacements.BuildDefaults(
                SourceHost,
                SourceUpstreamHost,
                TargetBaseUrl,
                TargetHost,
                Definition.ContentReplacements),
            LocalAssets = WwwrootAssetCatalog.Build(
                WebRootPath,
                TargetHost,
                Definition.LocalAssets),
            BlockedPathPrefixes = Definition.BlockedPathPrefixes ?? [],
            EnableOutboundRedirectPaths = Definition.EnableOutboundRedirectPaths,
            OutboundRedirectPathPrefixes = Definition.OutboundRedirectPathPrefixes ?? [],
            ExternalRedirectUrl = Definition.ExternalRedirectUrl,
            RedirectForeignRequests = Definition.RedirectForeignRequests,
            RedirectForeignRequestsUrl = Definition.RedirectForeignRequestsUrl,
            DisableCaching = Definition.DisableCaching,
            PassCookies = Definition.PassCookies,
            HtmlInjections = Definition.HtmlInjections ?? []
        };
}
