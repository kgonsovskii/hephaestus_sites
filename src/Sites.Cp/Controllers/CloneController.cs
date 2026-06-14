using Microsoft.AspNetCore.Mvc;
using Sites.Web.Abstractions;

namespace Sites.Cp.Controllers;

public sealed class CloneController : Controller
{
    [HttpGet("/clone")]
    public IActionResult Index()
    {
        ViewData["Profile"] = SitesProfileResolver.Current;
        return View();
    }
}
