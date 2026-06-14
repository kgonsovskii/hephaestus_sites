using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web;

public static class PublicTargetResolver
{
    public static (string BaseUrl, string Host) Resolve(HttpRequest request, ISiteModule site)
    {
        var requestHost = NormalizeHost(request.Host.Host);
        if (site.TargetHosts.Any(host =>
                string.Equals(NormalizeHost(host), requestHost, StringComparison.OrdinalIgnoreCase)))
            return (site.TargetBaseUrl.TrimEnd('/'), site.TargetHost);

        var host = request.Host.Value;
        if (string.IsNullOrWhiteSpace(host))
            host = request.Host.Host;

        return ($"{request.Scheme}://{host}".TrimEnd('/'), host);
    }

    private static string NormalizeHost(string host) =>
        host.Trim().TrimEnd('.');
}
