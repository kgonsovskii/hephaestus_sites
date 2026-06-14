using Microsoft.Extensions.Options;
using Sites.Web.Caching;

namespace Sites.Web;

/// <summary>
/// Profile <c>settings.json</c> defaults copied from <c>appsettings.json</c> → <c>Sites</c> section.
/// </summary>
public sealed class SitesProfileSettingsTemplate
{
    private readonly SitesProxyOptions _template;

    public SitesProfileSettingsTemplate(IOptions<SitesProxyOptions> options)
    {
        var value = options.Value;
        SitesProfileSettingsValidator.Validate(value);
        _template = Clone(value);
    }

    public SitesProfileSettingsDocument CreateDocument() =>
        new() { Sites = Clone(_template) };

    private static SitesProxyOptions Clone(SitesProxyOptions source) => new()
    {
        UpstreamRequestTimeout = source.UpstreamRequestTimeout,
        Cache = new ProxyCacheOptions
        {
            RootPath = source.Cache.RootPath,
            MaxEntryBytes = source.Cache.MaxEntryBytes,
            Ttl = source.Cache.Ttl,
            RejectRangeRequests = source.Cache.RejectRangeRequests,
            ExcludedContentTypes = source.Cache.ExcludedContentTypes.ToList()
        }
    };
}
