using Microsoft.AspNetCore.Http;
using Sites.Web.Caching;

namespace Sites.Web.Tests;

public sealed class ClientBandwidthResponseHeadersTests
{
    [Fact]
    public void TryWriteNotModified_Returns304WhenEntityTagMatches()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.IfNoneMatch = "\"abc\"";

        var bandwidth = TestSitesProxyOptions.Create().ClientBandwidth;
        var matched = ClientBandwidthResponseHeaders.TryWriteNotModified(context, bandwidth, "\"abc\"");

        Assert.True(matched);
        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
        Assert.Equal("public, max-age=604800", context.Response.Headers.CacheControl.ToString());
    }
}
