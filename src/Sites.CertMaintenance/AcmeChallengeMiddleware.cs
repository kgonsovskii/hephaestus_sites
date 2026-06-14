namespace Sites.CertMaintenance;

public sealed class AcmeChallengeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AcmeChallengeRegistry _challenges;

    public AcmeChallengeMiddleware(RequestDelegate next, AcmeChallengeRegistry challenges)
    {
        _next = next;
        _challenges = challenges;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        const string prefix = "/.well-known/acme-challenge/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var token = path[prefix.Length..].Trim('/');
        if (token.Length == 0 || !_challenges.TryGetResponse(token, out var keyAuthz))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(keyAuthz, context.RequestAborted);
    }
}
