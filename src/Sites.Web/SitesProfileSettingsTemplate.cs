using Microsoft.Extensions.Configuration;
using Sites.Web.Abstractions;
using Sites.Web.Caching;

namespace Sites.Web;

/// <summary>
/// Profile <c>settings.json</c> defaults copied from <c>appsettings.json</c> → <c>Sites</c> section,
/// with fallback to an existing profile settings file when appsettings is missing or stale on deploy.
/// </summary>
public sealed class SitesProfileSettingsTemplate
{
    private readonly SitesProxyOptions _template;

    public SitesProfileSettingsTemplate(IConfiguration configuration)
    {
        _template = Clone(ResolveTemplateOptions(configuration));
    }

    public SitesProfileSettingsDocument CreateDocument() =>
        new() { Sites = Clone(_template) };

    private static SitesProxyOptions ResolveTemplateOptions(IConfiguration configuration)
    {
        var fromAppSettings = BindSection(configuration.GetSection(SitesProxyOptions.SectionName));
        if (IsValid(fromAppSettings))
            return fromAppSettings;

        foreach (var path in CandidateProfileSettingsPaths())
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var fromProfile = SitesSettingsFile.Load(path).Sites;
                if (IsValid(fromProfile))
                    return fromProfile;
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException(
            "Could not resolve Sites settings template. Configure the Sites section in appsettings.json " +
            $"or provide a valid profiles/{{profile}}/settings.json (missing or invalid Cache:Ttl / other fields). " +
            $"Appsettings section '{SitesProxyOptions.SectionName}' bound Ttl={fromAppSettings.Cache.Ttl}.");
    }

    private static IEnumerable<string> CandidateProfileSettingsPaths()
    {
        yield return SitesSettingsFile.ResolvePath();

        var repoRoot = RepositoryPaths.TryResolveRoot();
        if (repoRoot is null)
            yield break;

        yield return SitesProfileResolver.ResolveSettingsJsonPath(repoRoot);
        yield return SitesProfileResolver.ResolveSettingsJsonPath(repoRoot, SitesProfileResolver.DefaultProfile);
    }

    private static SitesProxyOptions BindSection(IConfigurationSection section)
    {
        var options = new SitesProxyOptions();
        section.Bind(options);
        options.Cache ??= new ProxyCacheOptions();
        return options;
    }

    private static bool IsValid(SitesProxyOptions options)
    {
        try
        {
            SitesProfileSettingsValidator.Validate(options);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

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
