using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.DataFtp;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Cp.Controllers;

public sealed class CpController : Controller
{
    private readonly SitesCatalogService _catalog;
    private readonly ISitesDataFtpUrlProvider _dataFtpUrl;

    public CpController(SitesCatalogService catalog, ISitesDataFtpUrlProvider dataFtpUrl)
    {
        _catalog = catalog;
        _dataFtpUrl = dataFtpUrl;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        var coded = _catalog.GetCodedSourceHosts().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sites = _catalog.GetAll()
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new SiteListRow
            {
                TargetHost = entry.Key,
                SourceHost = entry.Value.SourceHost,
                PassCookies = entry.Value.PassCookies,
                DisableCaching = entry.Value.DisableCaching,
                HasCodedModule = coded.Contains(entry.Value.SourceHost)
            })
            .ToArray();

        return View(new SitesListPageModel
        {
            Profile = SitesProfileResolver.Current,
            SitesJsonPath = _catalog.SitesJsonPath,
            WebFtpUrl = _dataFtpUrl.BuildUrl(Request.Host.Host),
            WebRootFullPath = _dataFtpUrl.WebRootFullPath,
            Sites = sites
        });
    }

    [HttpGet("/edit")]
    public IActionResult Edit([FromQuery] string? site)
    {
        if (string.IsNullOrWhiteSpace(site))
        {
            return View(new SiteEditPageModel
            {
                IsAdd = true,
                Profile = SitesProfileResolver.Current,
                DefinitionJson = "null"
            });
        }

        var definition = _catalog.Get(site);
        if (definition is null)
            return NotFound();

        var coded = _catalog.GetCodedSourceHosts();
        return View(new SiteEditPageModel
        {
            IsAdd = false,
            TargetHost = definition.TargetHost,
            HasCodedModule = coded.Contains(definition.SourceHost, StringComparer.OrdinalIgnoreCase),
            Profile = SitesProfileResolver.Current,
            DefinitionJson = JsonSerializer.Serialize(definition, SitesJsonFile.WriteOptions)
        });
    }
}
