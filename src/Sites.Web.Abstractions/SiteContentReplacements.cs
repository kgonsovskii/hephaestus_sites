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

        foreach (var host in new[] { sourceUpstreamHost, sourceHost })
        {
            defaults.Add(new ContentReplacement { From = $"https://{host}", To = targetBaseUrl });
            defaults.Add(new ContentReplacement { From = $"http://{host}", To = targetBaseUrl });
            defaults.Add(new ContentReplacement { From = $"//{host}", To = $"//{targetHost}" });
            defaults.Add(new ContentReplacement { From = host, To = targetHost, WordBoundaryOnly = true });
        }

        if (additional is { Count: > 0 })
            defaults.AddRange(additional);

        return defaults;
    }
}
