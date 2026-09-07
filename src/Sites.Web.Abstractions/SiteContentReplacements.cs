using System.Net;
using System.Text.RegularExpressions;

namespace Sites.Web.Abstractions;

public static class SiteContentReplacements
{
    public static IReadOnlyList<ContentReplacement> BuildDefaults(
        string sourceHost,
        string sourceUpstreamHost,
        string targetBaseUrl,
        string targetHost,
        IReadOnlyList<ContentReplacement>? additional = null)
    {
        var defaults = new List<ContentReplacement>();
        var loopback = IsLoopbackRewriteHost(targetHost);

        foreach (var host in new[] { sourceUpstreamHost, sourceHost }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(host))
                continue;

            defaults.Add(new ContentReplacement { From = $"https://{host}", To = targetBaseUrl });
            defaults.Add(new ContentReplacement { From = $"http://{host}", To = targetBaseUrl });
            defaults.Add(new ContentReplacement { From = $"//{host}", To = $"//{targetHost}" });

            if (loopback)
            {
                var hostPattern = $@"(?:[A-Za-z0-9-]+\.)*{Regex.Escape(host)}";
                defaults.Add(new ContentReplacement
                {
                    From = $@"https?://{hostPattern}",
                    To = targetBaseUrl,
                    IsRegex = true
                });
                defaults.Add(new ContentReplacement
                {
                    From = $@"//{hostPattern}",
                    To = $"//{targetHost}",
                    IsRegex = true
                });
                defaults.Add(new ContentReplacement
                {
                    From = hostPattern,
                    To = targetHost,
                    IsRegex = true
                });
            }
            else
            {
                defaults.Add(new ContentReplacement { From = host, To = targetHost, WordBoundaryOnly = true });
            }
        }

        if (additional is { Count: > 0 })
            defaults.AddRange(additional);

        return defaults;
    }

    internal static bool IsLoopbackRewriteHost(string targetHost)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
            return false;

        var host = targetHost.Trim();
        if (host.StartsWith('['))
        {
            var end = host.IndexOf(']');
            if (end > 1 && IPAddress.TryParse(host[1..end], out var v6))
                return IPAddress.IsLoopback(v6);
            return false;
        }

        var colon = host.LastIndexOf(':');
        var name = colon > 0 && int.TryParse(host[(colon + 1)..], out _)
            ? host[..colon]
            : host;

        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return IPAddress.TryParse(name, out var ip) && IPAddress.IsLoopback(ip);
    }
}
