namespace Sites.Web;

public sealed class SiteRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SiteRegistry _registry;
    private readonly ILogger<SiteRoutingMiddleware> _logger;

    public SiteRoutingMiddleware(
        RequestDelegate next,
        SiteRegistry registry,
        ILogger<SiteRoutingMiddleware> logger)
    {
        _next = next;
        _registry = registry;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var site = _registry.Resolve(context);
        if (site is null)
        {
            _logger.LogWarning(
                "No site matched host {Host} for {Method} {Path}",
                context.Request.Host.Value,
                context.Request.Method,
                context.Request.Path.Value);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Site not found.", context.RequestAborted);
            return;
        }

        context.SetSite(site);
        await _next(context);
    }
}
