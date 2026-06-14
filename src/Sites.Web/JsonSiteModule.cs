using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class JsonSiteModule : ISiteModule
{
    public JsonSiteModule(SiteDefinition definition, string? webRootPath = null)
    {
        Definition = definition;
        WebRootPath = WebRootPaths.Resolve(webRootPath);
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

    public SiteProxyRules Rules => new()
    {
        ContentReplacements = SiteContentReplacements.BuildDefaults(
            SourceHost,
            SourceUpstreamHost,
            TargetBaseUrl,
            TargetHost,
            Definition.ContentReplacements),
        LocalAssets = Definition.LocalAssets is { Count: > 0 }
            ? new Dictionary<string, string>(Definition.LocalAssets, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        BlockedPathPrefixes = Definition.BlockedPathPrefixes ?? [],
        AdditionsPathPrefix = string.IsNullOrWhiteSpace(Definition.AdditionsPathPrefix)
            ? "/x/"
            : Definition.AdditionsPathPrefix,
        OutboundRedirectPathPrefixes = Definition.OutboundRedirectPathPrefixes ?? [],
        ExternalRedirectUrl = Definition.ExternalRedirectUrl,
        DisableCaching = Definition.DisableCaching,
        PassCookies = Definition.PassCookies,
        HtmlInjections = Definition.HtmlInjections ?? []
    };
}
