using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class SiteRegistry
{
    private readonly object _sync = new();
    private readonly string? _selectedSiteName;
    private Dictionary<string, ISiteModule> _sitesByHost;
    private Dictionary<string, ISiteModule> _sitesByName;
    private ISiteModule? _singleSite;
    private IReadOnlyList<ISiteModule> _activeSites;

    public SiteRegistry(IReadOnlyList<ISiteModule> sites, string? selectedSiteName)
    {
        _selectedSiteName = string.IsNullOrWhiteSpace(selectedSiteName) ? null : selectedSiteName.Trim();
        _sitesByName = sites.ToDictionary(site => site.Name, StringComparer.OrdinalIgnoreCase);

        if (_selectedSiteName is not null)
        {
            var selected = TryResolveSelectedSite(sites, _selectedSiteName)
                ?? throw new InvalidOperationException($"Site '{_selectedSiteName}' is not registered.");

            _singleSite = selected;
            _activeSites = [selected];
            _sitesByHost = [];
            return;
        }

        _singleSite = null;
        _activeSites = sites;
        _sitesByHost = BuildHostIndex(sites);
    }

    public IReadOnlyList<ISiteModule> ActiveSites
    {
        get
        {
            lock (_sync)
                return _activeSites;
        }
    }

    public bool IsSingleSiteMode => _singleSite is not null;

    public bool CanReload => true;

    public ISiteModule? Resolve(HttpContext context)
    {
        lock (_sync)
        {
            if (_singleSite is not null)
                return _singleSite;

            var host = NormalizeHost(context.Request.Host.Host);
            return _sitesByHost.GetValueOrDefault(host);
        }
    }

    public ISiteModule? GetByName(string name)
    {
        lock (_sync)
            return _sitesByName.GetValueOrDefault(name);
    }

    public void ReplaceSites(IReadOnlyList<ISiteModule> sites)
    {
        lock (_sync)
        {
            _sitesByName = sites.ToDictionary(site => site.Name, StringComparer.OrdinalIgnoreCase);

            if (_selectedSiteName is not null)
            {
                var selected = TryResolveSelectedSite(sites, _selectedSiteName)
                    ?? throw new InvalidOperationException(
                        $"Site '{_selectedSiteName}' is not registered after reload.");

                _singleSite = selected;
                _activeSites = [selected];
                return;
            }

            _singleSite = null;
            _activeSites = sites;
            _sitesByHost = BuildHostIndex(sites);
        }
    }

    private static ISiteModule? TryResolveSelectedSite(IReadOnlyList<ISiteModule> sites, string selectedSiteName)
    {
        var normalized = NormalizeHost(selectedSiteName);
        foreach (var site in sites)
        {
            if (string.Equals(NormalizeHost(site.TargetHost), normalized, StringComparison.OrdinalIgnoreCase))
                return site;

            foreach (var host in site.TargetHosts)
            {
                if (string.Equals(NormalizeHost(host), normalized, StringComparison.OrdinalIgnoreCase))
                    return site;
            }
        }

        return null;
    }

    private static Dictionary<string, ISiteModule> BuildHostIndex(IReadOnlyList<ISiteModule> sites)
    {
        var index = new Dictionary<string, ISiteModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var site in sites)
        {
            foreach (var host in site.TargetHosts)
            {
                var normalized = NormalizeHost(host);
                if (index.ContainsKey(normalized))
                    throw new InvalidOperationException($"Host '{host}' is registered for multiple sites.");

                index[normalized] = site;
            }
        }

        return index;
    }

    private static string NormalizeHost(string host) =>
        host.Trim().TrimEnd('.');
}
