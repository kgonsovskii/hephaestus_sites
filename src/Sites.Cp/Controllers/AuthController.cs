using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Sites.Cp.Controllers;

public sealed class AuthController : Controller
{
    private readonly CpOptions _options;

    public AuthController(IOptions<CpOptions> options) => _options = options.Value;

    [HttpGet("/auth/login")]
    [AllowAnonymous]
    public IActionResult Login() => View();

    [HttpPost("/auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string password, [FromForm] string? returnUrl)
    {
        if (!SitesCpExtensions.ValidatePassword(_options, password))
        {
            ViewData["Error"] = "Invalid password.";
            return View();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin")],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect($"{Request.PathBase}/");
    }

    [HttpPost("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect($"{Request.PathBase}/auth/login");
    }
}
