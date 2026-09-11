using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Track;

public sealed class TrackMiddleware
{
    private static readonly string Script = LoadScript();

    private readonly RequestDelegate _next;
    private readonly TrackStore _store;
    private readonly IOptions<TrackOptions> _options;
    private readonly ILogger<TrackMiddleware> _logger;

    public TrackMiddleware(
        RequestDelegate next,
        TrackStore store,
        IOptions<TrackOptions> options,
        ILogger<TrackMiddleware>? logger = null)
    {
        _next = next;
        _store = store;
        _options = options;
        _logger = logger ?? NullLogger<TrackMiddleware>.Instance;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (HttpMethods.IsGet(context.Request.Method) && IsTrackScriptPath(path))
        {
            await WriteScriptAsync(context);
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) && IsPlayBeaconPath(path))
        {
            await HandleBeaconAsync(context);
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) &&
            path.Equals("/internal/track/goal", StringComparison.OrdinalIgnoreCase))
        {
            await HandleGoalAsync(context);
            return;
        }

        TrackCookie.TryWriteFromQuery(context);

        if (HttpMethods.IsGet(context.Request.Method))
            TryRecordPage(context, path);

        await _next(context);
    }

    private void Record(HttpContext context, TrackEventKind kind)
    {
        var flow = TrackCookie.ReadFlow(context.Request);
        var ip = TrackCookie.ClientIp(context);
        if (string.IsNullOrEmpty(flow) || string.IsNullOrEmpty(ip))
            return;

        string siteName;
        try
        {
            siteName = context.GetSite().TargetHost;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        var domain = TrackCookie.RequestDomain(context.Request);
        if (domain.Length == 0)
            domain = TrackCookie.RegistrableDomain(siteName);

        var (t1, t2) = TrackCookie.ReadTargets(context.Request);
        var now = DateTime.UtcNow;
        _store.Touch(
            DateOnly.FromDateTime(now),
            ip,
            flow,
            domain,
            siteName,
            t1,
            t2,
            kind,
            now);
    }

    private void TryRecordPage(HttpContext context, string path)
    {
        if (!TrackPath.ShouldRecordPage(path))
            return;

        Record(context, TrackPath.PageEvent(path));
    }

    private async Task HandleBeaconAsync(HttpContext context)
    {
        var flow = TrackCookie.ReadFlow(context.Request);
        var ip = TrackCookie.ClientIp(context);
        if (string.IsNullOrEmpty(flow) || string.IsNullOrEmpty(ip))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        var kind = TrackEventKind.Play;
        try
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            if (doc.RootElement.TryGetProperty("e", out var e) &&
                e.GetString()?.Equals("play", StringComparison.OrdinalIgnoreCase) == true)
                kind = TrackEventKind.Play;
        }
        catch (JsonException)
        {
        }

        Record(context, kind);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private async Task HandleGoalAsync(HttpContext context)
    {
        var expectedKey = _options.Value.GoalKey;
        if (!string.IsNullOrWhiteSpace(expectedKey))
        {
            var provided = context.Request.Headers["X-Sites-Goal-Key"].ToString();
            if (!string.Equals(provided, expectedKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        string? ip = null;
        try
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            if (doc.RootElement.TryGetProperty("ip", out var ipEl))
                ip = ipEl.GetString();
        }
        catch (JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        ip = TrackCookie.NormalizeClientIp(ip);
        if (ip.Length == 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var now = DateTime.UtcNow;
        var marked = _store.TryMarkGoal(ip, _options.Value.GoalLock, now, out var visit);
        if (marked)
            _logger.LogInformation("Track goal marked ip={Ip} flow={Flow} domain={Domain}", ip, visit?.Flow, visit?.Domain);
        else
            _logger.LogWarning("Track goal unmatched ip={Ip} (no visit with flow in the last {Hours}h)", ip, _options.Value.GoalLock.TotalHours);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task WriteScriptAsync(HttpContext context)
    {
        context.Response.ContentType = "text/javascript; charset=utf-8";
        context.Response.Headers.CacheControl = "public, max-age=3600";
        await context.Response.WriteAsync(Script, context.RequestAborted);
    }

    private static string LoadScript()
    {
        var assembly = typeof(TrackMiddleware).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("track.js", StringComparison.OrdinalIgnoreCase));
        if (name is null)
            return "/* track.js missing */";

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            return "/* track.js missing */";

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static bool IsTrackScriptPath(string path) =>
        path.Equals("/_s/s.js", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/_s/track.js", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlayBeaconPath(string path) =>
        path.Equals("/_s/e", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/t/e", StringComparison.OrdinalIgnoreCase);
}
