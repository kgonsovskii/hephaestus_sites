using Sites.Modules;
using Sites.Web;
using Sites.Web.Abstractions;
namespace Sites.CertMaintenance;

public static class PublishDomainDiscovery
{
    public static IReadOnlyList<string> DiscoverFromRegistry(SiteRegistry registry) =>
        CollectDomains(registry.ActiveSites);

    public static IReadOnlyList<string> DiscoverFromModulesAssembly(string? sitesJsonPath = null)
    {
        var sites = SiteModuleDiscovery.DiscoverSites(typeof(SitesModulesAnchor).Assembly, sitesJsonPath);
        return CollectDomains(sites);
    }

    private static IReadOnlyList<string> CollectDomains(IEnumerable<ISiteModule> sites)
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var site in sites)
        {
            foreach (var host in site.TargetHosts)
            {
                var normalized = NormalizeHost(host);
                if (normalized.Length > 0)
                    domains.Add(normalized);
            }
        }

        var list = domains.ToList();
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    private static string NormalizeHost(string host) =>
        host.Trim().TrimEnd('.');
}
