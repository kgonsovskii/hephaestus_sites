using Sites.Web;
using Microsoft.AspNetCore.Http;

namespace Sites.Web.Tests;

public sealed class OversizedCookieMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SmallCookie_IsPreserved()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "session=abc123";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        await new OversizedCookieMiddleware(next).InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("session=abc123", context.Request.Headers.Cookie.ToString());
    }

    [Fact]
    public async Task InvokeAsync_OversizedCookie_IsRemoved()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "a=" + new string('x', 25 * 1024);

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        await new OversizedCookieMiddleware(next).InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.False(context.Request.Headers.ContainsKey("Cookie"));
    }
}
