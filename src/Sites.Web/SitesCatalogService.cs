using System.Reflection;
using Sites.Web.Abstractions;

namespace Sites.Web;

public sealed class SitesCatalogService
{
    private readonly object _sync = new();
    private readonly Assembly _modulesAssembly;
    private readonly SiteRegistry _registry;
    private readonly string? _explicitSitesJsonPath;

    public SitesCatalogService(
        Assembly modulesAssembly,
        SiteRegistry registry,
        string? sitesJsonPath = null)
    {
        _modulesAssembly = modulesAssembly;
        _registry = registry;
        _explicitSitesJsonPath = string.IsNullOrWhiteSpace(sitesJsonPath) ? null : sitesJsonPath;
    }

    public string SitesJsonPath => SitesJsonFile.ResolvePath(_explicitSitesJsonPath);

    public IReadOnlyDictionary<string, SiteDefinition> GetAll()
    {
        lock (_sync)
            return SitesJsonFile.Load(SitesJsonPath);
    }

    public SiteDefinition? Get(string targetHost)
    {
        var key = NormalizeTargetHost(targetHost);
        lock (_sync)
            return SitesJsonFile.Load(SitesJsonPath).GetValueOrDefault(key);
    }

    public SiteDefinition Create(string targetHost, SiteDefinition definition)
    {
        var key = NormalizeTargetHost(targetHost);
        ValidateDefinition(key, definition);

        lock (_sync)
        {
            var sites = new Dictionary<string, SiteDefinition>(
                SitesJsonFile.Load(SitesJsonPath),
                StringComparer.OrdinalIgnoreCase);

            if (sites.ContainsKey(key))
                throw new InvalidOperationException($"Site '{key}' already exists.");

            sites[key] = ToStoredDefinition(key, definition);
            PersistAndReload(sites);
            return sites[key];
        }
    }

    public SiteDefinition Update(string targetHost, SiteDefinition definition)
    {
        var key = NormalizeTargetHost(targetHost);
        ValidateDefinition(key, definition);

        lock (_sync)
        {
            var sites = new Dictionary<string, SiteDefinition>(
                SitesJsonFile.Load(SitesJsonPath),
                StringComparer.OrdinalIgnoreCase);

            if (!sites.ContainsKey(key))
                throw new KeyNotFoundException($"Site '{key}' was not found.");

            sites[key] = ToStoredDefinition(key, definition);
            PersistAndReload(sites);
            return sites[key];
        }
    }

    public void Delete(string targetHost)
    {
        var key = NormalizeTargetHost(targetHost);

        lock (_sync)
        {
            var sites = new Dictionary<string, SiteDefinition>(
                SitesJsonFile.Load(SitesJsonPath),
                StringComparer.OrdinalIgnoreCase);

            if (!sites.Remove(key))
                throw new KeyNotFoundException($"Site '{key}' was not found.");

            PersistAndReload(sites);
        }
    }

    public IReadOnlyList<string> GetCodedSourceHosts() =>
        SiteModuleDiscovery.DiscoverCodedModules(_modulesAssembly).Keys.ToArray();

    public int ReloadRegistry()
    {
        lock (_sync)
        {
            var modules = SiteModuleDiscovery.DiscoverSites(_modulesAssembly, SitesJsonPath);
            _registry.ReplaceSites(modules);
            return modules.Count;
        }
    }

    private void PersistAndReload(IReadOnlyDictionary<string, SiteDefinition> sites)
    {
        SitesJsonFile.Save(SitesJsonPath, sites);
        var modules = SiteModuleDiscovery.DiscoverSites(_modulesAssembly, SitesJsonPath);
        _registry.ReplaceSites(modules);
    }

    private static SiteDefinition ToStoredDefinition(string targetHost, SiteDefinition definition) =>
        new()
        {
            SourceHost = definition.SourceHost.Trim(),
            TargetHost = targetHost,
            Name = definition.Name?.Trim(),
            SourceUpstreamHost = definition.SourceUpstreamHost?.Trim(),
            TargetHosts = definition.TargetHosts,
            DisableCaching = definition.DisableCaching,
            PassCookies = definition.PassCookies,
            OutboundRedirectPathPrefixes = definition.OutboundRedirectPathPrefixes,
            BlockedPathPrefixes = definition.BlockedPathPrefixes,
            ExternalRedirectUrl = definition.ExternalRedirectUrl,
            EnableOutboundRedirectPaths = definition.EnableOutboundRedirectPaths,
            RedirectForeignRequests = definition.RedirectForeignRequests,
            RedirectForeignRequestsUrl = definition.RedirectForeignRequestsUrl,
            ContentReplacements = definition.ContentReplacements,
            HtmlInjections = definition.HtmlInjections,
            LocalAssets = definition.LocalAssets,
            Settings = SitesJsonFile.CloneSettings(definition.Settings)
        };

    private static void ValidateDefinition(string targetHost, SiteDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.SourceHost))
            throw new ArgumentException("sourceHost is required.", nameof(definition));

        if (!string.Equals(targetHost, NormalizeTargetHost(targetHost), StringComparison.Ordinal))
            throw new ArgumentException("targetHost is invalid.", nameof(targetHost));
    }

    private static string NormalizeTargetHost(string targetHost)
    {
        var key = targetHost.Trim();
        if (key.Length == 0)
            throw new ArgumentException("targetHost is required.", nameof(targetHost));

        return key;
    }
}
