using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sites.Track;

public static class TrackCookie
{
    public const string FlowName = "sf";
    public const string Target1Name = "st1";
    public const string Target2Name = "st2";
    public const string DefaultFlow = "_default";
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

    public static string NormalizeClientIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        var comma = trimmed.IndexOf(',');
        if (comma >= 0)
            trimmed = trimmed[..comma].Trim();
        if (trimmed.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return "";
        if (!IPAddress.TryParse(trimmed, out var ip))
            return Normalize(trimmed, 45);
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        return ip.ToString();
    }

    public static bool SameClientIp(string? left, string? right) =>
        string.Equals(NormalizeClientIp(left), NormalizeClientIp(right), StringComparison.OrdinalIgnoreCase);

    public static string NormalizeFlow(string? value)
    {
        var flow = Normalize(value, FlowMaxLength);
        if (flow.Length == 0)
            return DefaultFlow;
        if (flow.Equals("undefined", StringComparison.OrdinalIgnoreCase) ||
            flow.Equals("null", StringComparison.OrdinalIgnoreCase) ||
            flow.Equals("none", StringComparison.OrdinalIgnoreCase))
            return DefaultFlow;
        return flow;
    }

    public static string ReadFlow(HttpRequest request)
    {
        var fromQuery = Normalize(request.Query["flow"].ToString(), FlowMaxLength);
        if (fromQuery.Length > 0)
            return NormalizeFlow(fromQuery);

        if (request.Cookies.TryGetValue(FlowName, out var cookie))
        {
            var fromCookie = Normalize(cookie, FlowMaxLength);
            if (fromCookie.Length > 0)
                return NormalizeFlow(fromCookie);
        }

        return DefaultFlow;
    }

    public static string RequestDomain(HttpRequest request)
    {
        var host = request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
            return "";

        return RegistrableDomain(host);
    }

    /// <summary>www.4tube.xyz and 4tube.xyz both become 4tube.xyz (last two DNS labels).</summary>
    public static string RegistrableDomain(string? host)
    {
        var normalized = Normalize(host, 200).Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.Length == 0)
            return "";
        if (IPAddress.TryParse(normalized, out _))
            return normalized;

        var labels = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length <= 2)
            return string.Join('.', labels);

        return labels[^2] + "." + labels[^1];
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
        if (!context.Request.Query.ContainsKey("flow"))
            return false;

        var flow = NormalizeFlow(context.Request.Query["flow"].ToString());

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
        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && !IsPrivate(remote))
            return Format(remote);

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) ||
            context.Request.Headers.TryGetValue("HTTP_X_FORWARDED_FOR", out forwarded))
        {
            var fromHeader = NormalizeClientIp(forwarded.ToString());
            if (fromHeader.Length > 0)
                return fromHeader;
        }

        return remote is null ? null : Format(remote);
    }

    private static string Format(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        return ip.ToString();
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;
        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
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
