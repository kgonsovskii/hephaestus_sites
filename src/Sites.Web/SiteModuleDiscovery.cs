using System.Reflection;
using Sites.Web.Abstractions;

namespace Sites.Web;

public static class SiteModuleDiscovery
{
    public static IReadOnlyList<ISiteModule> DiscoverSites(
        Assembly modulesAssembly,
        string? sitesJsonPath = null)
    {
        var path = SitesJsonFile.ResolvePath(sitesJsonPath);
        var definitions = SitesJsonFile.Load(path);
        var codedBySourceHost = DiscoverCodedModules(modulesAssembly);

        var sites = new List<ISiteModule>();

        foreach (var (targetHost, definition) in definitions.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (codedBySourceHost.TryGetValue(definition.SourceHost, out var coded))
                sites.Add(coded);
            else
                sites.Add(new JsonSiteModule(definition));
        }

        return sites;
    }

    public static IReadOnlyList<ISiteModule> DiscoverFromAssembly(Assembly assembly) =>
        DiscoverSites(assembly);

    public static IReadOnlyList<ISiteModule> DiscoverFromReferencedAssembly(
        Assembly entryAssembly,
        string assemblyName,
        string? sitesJsonPath = null) =>
        DiscoverSites(ResolveAssembly(entryAssembly, assemblyName), sitesJsonPath);

    internal static Dictionary<string, ISiteModule> DiscoverCodedModules(Assembly assembly)
    {
        var modules = new Dictionary<string, ISiteModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
                continue;

            if (!typeof(SiteModuleBase).IsAssignableFrom(type))
                continue;

            var instance = (ISiteModule)Activator.CreateInstance(type)!;
            if (modules.ContainsKey(instance.SourceHost))
            {
                throw new InvalidOperationException(
                    $"Multiple coded site modules declare sourceHost '{instance.SourceHost}' in {assembly.GetName().Name}.");
            }

            modules[instance.SourceHost] = instance;
        }

        return modules;
    }

    public static Assembly ResolveModulesAssembly(Assembly entryAssembly, string assemblyName) =>
        ResolveAssembly(entryAssembly, assemblyName);

    private static Assembly ResolveAssembly(Assembly entryAssembly, string assemblyName)
    {
        var reference = entryAssembly.GetReferencedAssemblies()
            .FirstOrDefault(name => string.Equals(name.Name, assemblyName, StringComparison.Ordinal));

        if (reference is not null)
            return Assembly.Load(reference);

        var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        if (File.Exists(path))
            return Assembly.LoadFrom(path);

        throw new InvalidOperationException(
            $"Assembly '{assemblyName}' was not found in references or at '{path}'.");
    }
}
