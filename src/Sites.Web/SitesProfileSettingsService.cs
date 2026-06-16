using Sites.Web.Caching;

namespace Sites.Web;

public sealed class SitesProfileSettingsService
{
    private readonly object _sync = new();
    private readonly SitesProfileSettingsTemplate _template;
    private readonly string? _explicitSettingsPath;
    private SitesProxyOptions _options = new();

    public SitesProfileSettingsService(
        SitesProfileSettingsTemplate template,
        string? explicitSettingsPath = null)
    {
        _template = template;
        _explicitSettingsPath = string.IsNullOrWhiteSpace(explicitSettingsPath) ? null : explicitSettingsPath;
        Reload();
    }

    public string SettingsJsonPath
    {
        get
        {
            lock (_sync)
                return SitesSettingsFile.ResolvePath(_explicitSettingsPath);
        }
    }

    public SitesProxyOptions Get()
    {
        lock (_sync)
            return _options;
    }

    public SitesProfileSettingsDocument GetDocument()
    {
        lock (_sync)
            return new SitesProfileSettingsDocument { Sites = SitesProxyOptionsCloner.Clone(_options) };
    }

    public void Reload()
    {
        lock (_sync)
        {
            var path = SitesSettingsFile.ResolvePath(_explicitSettingsPath);
            var defaults = _template.CreateDocument();
            var document = File.Exists(path)
                ? SitesSettingsFile.Load(path, defaults)
                : SitesSettingsFile.LoadOrCreate(path, defaults);
            _options = document.Sites;
        }
    }

    public SitesProfileSettingsDocument Save(SitesProfileSettingsDocument document)
    {
        lock (_sync)
        {
            SitesProfileSettingsValidator.Validate(document.Sites);
            var path = SitesSettingsFile.ResolvePath(_explicitSettingsPath);
            SitesSettingsFile.Save(path, document);
            _options = document.Sites;
            return GetDocument();
        }
    }
}
