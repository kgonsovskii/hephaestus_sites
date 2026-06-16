using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Caching;
using Sites.Web.Git;

namespace Sites.Cp.Controllers;

public sealed class SettingsController : Controller
{
    [HttpGet("/settings")]
    public IActionResult Index() => View();
}

[ApiController]
[Route("api/settings")]
public sealed class SitesSettingsApiController : ControllerBase
{
    private readonly SitesProfileSettingsService _settings;
    private readonly SitesCatalogChangedSignal _catalogChanged;
    private readonly ProxyDiskCache _cache;
    private readonly ProxyCachePolicy _cachePolicy;

    public SitesSettingsApiController(
        SitesProfileSettingsService settings,
        SitesCatalogChangedSignal catalogChanged,
        ProxyDiskCache cache,
        ProxyCachePolicy cachePolicy)
    {
        _settings = settings;
        _catalogChanged = catalogChanged;
        _cache = cache;
        _cachePolicy = cachePolicy;
    }

    [HttpGet]
    public ActionResult<ProfileSettingsResponse> Get() => Ok(ToResponse());

    [HttpPut]
    public ActionResult<ProfileSettingsResponse> Save([FromBody] SitesProfileSettingsDocument document)
    {
        try
        {
            _settings.Save(document);
            _catalogChanged.NotifyCatalogChanged();
            SitesMaintenanceCaches.ClearTextCache(_cache, _cachePolicy);
            return Ok(ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private ProfileSettingsResponse ToResponse() => new()
    {
        Profile = SitesProfileResolver.Current,
        SettingsJsonPath = _settings.SettingsJsonPath,
        Settings = _settings.GetDocument()
    };
}
