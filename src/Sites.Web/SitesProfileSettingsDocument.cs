using Sites.Web.Caching;

namespace Sites.Web;

public sealed class SitesProfileSettingsDocument
{
    public SitesProxyOptions Sites { get; set; } = new();
}
