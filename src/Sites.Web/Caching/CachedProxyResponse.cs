namespace Sites.Web.Caching;

public sealed class CachedProxyResponse
{
    public required int StatusCode { get; init; }

    public string? ContentType { get; init; }

    public required byte[] Body { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public string? EntityTag { get; init; }
}
