using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.Web;
using Sites.Web.Abstractions;
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

    public SitesSettingsApiController(
        SitesProfileSettingsService settings,
        SitesCatalogChangedSignal catalogChanged)
    {
        _settings = settings;
        _catalogChanged = catalogChanged;
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
