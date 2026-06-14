using Sites.Web.Caching;

namespace Sites.Web;

public static class SitesProfileSettingsValidator
{
    public static void Validate(SitesProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.UpstreamRequestTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("Sites.UpstreamRequestTimeout must be greater than zero.");

        var cache = options.Cache ?? throw new InvalidOperationException("Sites.Cache is required.");

        if (cache.Ttl <= TimeSpan.Zero)
            throw new InvalidOperationException("Sites.Cache.Ttl must be greater than zero.");

        if (cache.MaxEntryBytes <= 0)
            throw new InvalidOperationException("Sites.Cache.MaxEntryBytes must be greater than zero.");

        if (cache.ExcludedContentTypes is not { Count: > 0 })
            throw new InvalidOperationException("Sites.Cache.ExcludedContentTypes must list at least one entry.");
    }
}
