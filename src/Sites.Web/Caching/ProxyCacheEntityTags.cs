using System.Security.Cryptography;

namespace Sites.Web.Caching;

internal static class ProxyCacheEntityTags
{
    public static string Compute(ReadOnlySpan<byte> body) =>
        $"\"{Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant()}\"";
}
