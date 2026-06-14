using Microsoft.AspNetCore.Mvc;

namespace Sites.Cp.Controllers;

public sealed class CpController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
