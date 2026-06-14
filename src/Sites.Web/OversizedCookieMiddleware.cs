namespace Sites.Web;

public sealed class OversizedCookieMiddleware
{
    private const int MaxCookieHeaderLength = 24 * 1024;

    private readonly RequestDelegate _next;

    public OversizedCookieMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Cookie.Count > 0)
        {
            var cookieHeader = context.Request.Headers.Cookie.ToString();
            if (cookieHeader.Length > MaxCookieHeaderLength)
                context.Request.Headers.Remove("Cookie");
        }

        await _next(context);
    }
}
