using System.Diagnostics;

namespace Sites.Web;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var siteName = "unknown";

        try
        {
            siteName = context.Items.ContainsKey(SiteContext.ItemKey)
                ? context.GetSite().Name
                : siteName;

            _logger.LogInformation(
                "[{Site}] {Method} {Path}{Query} -> upstream",
                siteName,
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value);

            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            if (context.Items.ContainsKey(SiteContext.ItemKey))
                siteName = context.GetSite().Name;

            _logger.LogInformation(
                "[{Site}] {Method} {Path}{Query} -> {StatusCode} ({ElapsedMs} ms)",
                siteName,
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
