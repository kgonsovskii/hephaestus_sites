using System.Security.Cryptography;
using System.Text;
using Sites.Web.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Caching;

public static class ProxyCacheKeyBuilder
{
    public static string Build(HttpRequest request, ISiteModule site, bool includePublicHost)
    {
        var path = request.Path.Value ?? "/";
        var query = request.QueryString.Value ?? string.Empty;
        var material = includePublicHost
            ? $"{PublicTargetResolver.Resolve(request, site).Host}{path}{query}"
            : $"{path}{query}";

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }
}
