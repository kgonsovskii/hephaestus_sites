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
        var siteName = ResolveSiteName(context);
        var request = context.Request;
        var host = request.Host.Value ?? string.Empty;
        var path = request.Path.Value ?? "/";
        var query = request.QueryString.Value ?? string.Empty;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "-";
        var userAgent = Truncate(request.Headers.UserAgent.ToString(), 120);

        _logger.LogInformation(
            "[{Site}] >> {RemoteIp} {Method} {Host}{Path}{Query} ua={UserAgent}",
            siteName,
            remoteIp,
            request.Method,
            host,
            path,
            query,
            userAgent);

        Exception? fault = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            fault = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            siteName = ResolveSiteName(context);

            var response = context.Response;
            var contentType = response.ContentType ?? "-";
            var contentLength = response.ContentLength?.ToString() ?? "-";
            var cacheHeader = response.Headers.TryGetValue("X-Proxy-Cache", out var cacheValues)
                ? cacheValues.ToString()
                : "-";

            if (fault is null)
            {
                _logger.LogInformation(
                    "[{Site}] << {RemoteIp} {Method} {Host}{Path}{Query} -> {StatusCode} type={ContentType} len={ContentLength} cache={Cache} ({ElapsedMs} ms)",
                    siteName,
                    remoteIp,
                    request.Method,
                    host,
                    path,
                    query,
                    response.StatusCode,
                    contentType,
                    contentLength,
                    cacheHeader,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    fault,
                    "[{Site}] !! {RemoteIp} {Method} {Host}{Path}{Query} -> {StatusCode} type={ContentType} len={ContentLength} cache={Cache} ({ElapsedMs} ms)",
                    siteName,
                    remoteIp,
                    request.Method,
                    host,
                    path,
                    query,
                    response.StatusCode,
                    contentType,
                    contentLength,
                    cacheHeader,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static string ResolveSiteName(HttpContext context) =>
        context.Items.ContainsKey(SiteContext.ItemKey)
            ? context.GetSite().Name
            : "unknown";

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return "-";

        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
