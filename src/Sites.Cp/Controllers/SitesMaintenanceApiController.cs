using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Caching;

namespace Sites.Cp.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class SitesMaintenanceApiController : ControllerBase
{
    private readonly SitesCatalogService _catalog;
    private readonly ProxyDiskCache _cache;

    public SitesMaintenanceApiController(SitesCatalogService catalog, ProxyDiskCache cache)
    {
        _catalog = catalog;
        _cache = cache;
    }

    [HttpPost("clear-cache")]
    public ActionResult<ClearCacheResponse> ClearCache()
    {
        var result = _cache.ClearAll();
        return Ok(new ClearCacheResponse
        {
            Profile = SitesProfileResolver.Current,
            CacheRoot = result.CacheRoot,
            RemovedEntries = result.RemovedEntries
        });
    }

    [HttpPost("invalidate")]
    public ActionResult<InvalidateSitesResponse> Invalidate()
    {
        var siteCount = _catalog.ReloadRegistry();
        return Ok(new InvalidateSitesResponse
        {
            Profile = SitesProfileResolver.Current,
            SiteCount = siteCount,
            SitesJsonPath = _catalog.SitesJsonPath
        });
    }
}
