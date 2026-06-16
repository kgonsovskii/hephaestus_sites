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
    private readonly ProxyCachePolicy _cachePolicy;

    public SitesMaintenanceApiController(
        SitesCatalogService catalog,
        ProxyDiskCache cache,
        ProxyCachePolicy cachePolicy)
    {
        _catalog = catalog;
        _cache = cache;
        _cachePolicy = cachePolicy;
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

    [HttpPost("clear-non-binary-cache")]
    public ActionResult<ClearCacheResponse> ClearNonBinaryCache()
    {
        var result = SitesMaintenanceCaches.ClearTextCache(_cache, _cachePolicy);
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
