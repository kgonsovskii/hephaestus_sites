using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Git;

namespace Sites.Cp.Controllers;

[ApiController]
[Route("api/sites")]
public sealed class SitesApiController : ControllerBase
{
    private readonly SitesCatalogService _catalog;
    private readonly SitesCatalogChangedSignal _catalogChanged;

    public SitesApiController(SitesCatalogService catalog, SitesCatalogChangedSignal catalogChanged)
    {
        _catalog = catalog;
        _catalogChanged = catalogChanged;
    }

    [HttpGet]
    public ActionResult<SitesListResponse> List()
    {
        var coded = _catalog.GetCodedSourceHosts().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sites = _catalog.GetAll()
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new SiteEntryResponse
            {
                TargetHost = entry.Key,
                Definition = entry.Value,
                HasCodedModule = coded.Contains(entry.Value.SourceHost)
            })
            .ToArray();

        return Ok(new SitesListResponse
        {
            Profile = SitesProfileResolver.Current,
            SitesJsonPath = _catalog.SitesJsonPath,
            Sites = sites
        });
    }

    [HttpGet("{targetHost}")]
    public ActionResult<SiteEntryResponse> Get(string targetHost)
    {
        var definition = _catalog.Get(targetHost);
        if (definition is null)
            return NotFound();

        return Ok(ToResponse(definition));
    }

    [HttpPost("{targetHost}")]
    public ActionResult<SiteEntryResponse> Create(string targetHost, [FromBody] SiteDefinition definition)
    {
        try
        {
            var created = _catalog.Create(targetHost, definition);
            _catalogChanged.NotifyCatalogChanged();
            return CreatedAtAction(nameof(Get), new { targetHost = created.TargetHost }, ToResponse(created));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{targetHost}")]
    public ActionResult<SiteEntryResponse> Update(string targetHost, [FromBody] SiteDefinition definition)
    {
        try
        {
            var updated = _catalog.Update(targetHost, definition);
            _catalogChanged.NotifyCatalogChanged();
            return Ok(ToResponse(updated));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{targetHost}")]
    public IActionResult Delete(string targetHost)
    {
        try
        {
            _catalog.Delete(targetHost);
            _catalogChanged.NotifyCatalogChanged();
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private SiteEntryResponse ToResponse(SiteDefinition definition) =>
        new()
        {
            TargetHost = definition.TargetHost,
            Definition = definition,
            HasCodedModule = _catalog.GetCodedSourceHosts().Contains(definition.SourceHost, StringComparer.OrdinalIgnoreCase)
        };
}
