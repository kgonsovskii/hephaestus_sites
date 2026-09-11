using Microsoft.AspNetCore.Http;

namespace Sites.Track;

public static class TrackCookie
{
    public const string FlowName = "sf";
    public const string Target1Name = "st1";
    public const string Target2Name = "st2";
    public const int FlowMaxLength = 100;
    public const int TargetMaxLength = 200;

    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);

    public static string Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public static string? ReadFlow(HttpRequest request)
    {
        var fromQuery = Normalize(request.Query["flow"].ToString(), FlowMaxLength);
        if (fromQuery.Length > 0)
            return fromQuery;

        if (request.Cookies.TryGetValue(FlowName, out var cookie))
        {
            var fromCookie = Normalize(cookie, FlowMaxLength);
            if (fromCookie.Length > 0)
                return fromCookie;
        }

        return null;
    }

    public static (string Target1, string Target2) ReadTargets(HttpRequest request)
    {
        var t1 = FirstNonEmpty(
            Normalize(request.Query["target1"].ToString(), TargetMaxLength),
            Normalize(request.Query["t1"].ToString(), TargetMaxLength),
            request.Cookies.TryGetValue(Target1Name, out var c1) ? Normalize(c1, TargetMaxLength) : "");
        var t2 = FirstNonEmpty(
            Normalize(request.Query["target2"].ToString(), TargetMaxLength),
            Normalize(request.Query["t2"].ToString(), TargetMaxLength),
            request.Cookies.TryGetValue(Target2Name, out var c2) ? Normalize(c2, TargetMaxLength) : "");
        return (t1, t2);
    }

    public static bool TryWriteFromQuery(HttpContext context)
    {
        var flow = Normalize(context.Request.Query["flow"].ToString(), FlowMaxLength);
        if (flow.Length == 0)
            return false;

        var t1 = FirstNonEmpty(
            Normalize(context.Request.Query["target1"].ToString(), TargetMaxLength),
            Normalize(context.Request.Query["t1"].ToString(), TargetMaxLength));
        var t2 = FirstNonEmpty(
            Normalize(context.Request.Query["target2"].ToString(), TargetMaxLength),
            Normalize(context.Request.Query["t2"].ToString(), TargetMaxLength));

        var options = new CookieOptions
        {
            Path = "/",
            MaxAge = CookieLifetime,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            IsEssential = true,
            Secure = context.Request.IsHttps
        };

        context.Response.Cookies.Append(FlowName, flow, options);
        if (t1.Length > 0)
            context.Response.Cookies.Append(Target1Name, t1, options);
        if (t2.Length > 0)
            context.Response.Cookies.Append(Target2Name, t2, options);

        return true;
    }

    public static string? ClientIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null)
            return null;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        return ip.ToString();
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return "";
    }
}
