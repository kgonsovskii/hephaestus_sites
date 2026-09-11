using Microsoft.AspNetCore.Http;
using Sites.Track;
using Sites.Web;

namespace Sites.Web.Tests;

public sealed class TrackCookieTests
{
    [Fact]
    public void ReadFlow_PrefersQueryOverCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?flow=from-query");
        context.Request.Headers.Cookie = "sf=from-cookie";

        Assert.Equal("from-query", TrackCookie.ReadFlow(context.Request));
    }

    [Fact]
    public void ReadFlow_UsesCookieWhenQueryMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "sf=abc123";

        Assert.Equal("abc123", TrackCookie.ReadFlow(context.Request));
    }

    [Fact]
    public void TryWriteFromQuery_SetsFlowCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?flow=camp1&t1=a&target2=b");

        Assert.True(TrackCookie.TryWriteFromQuery(context));
        Assert.Contains("sf=camp1", context.Response.Headers.SetCookie.ToString());
        Assert.Contains("st1=a", context.Response.Headers.SetCookie.ToString());
        Assert.Contains("st2=b", context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public void TryWriteFromQuery_IgnoresMissingFlow()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?t1=x");

        Assert.False(TrackCookie.TryWriteFromQuery(context));
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void ReadFlow_IgnoresPartnerQuery()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?partner=old-id");

        Assert.Null(TrackCookie.ReadFlow(context.Request));
    }
}

public sealed class TrackPathTests
{
    [Fact]
    public void PageEvent_VideoPrefix()
    {
        Assert.Equal(TrackEventKind.Video, TrackPath.PageEvent("/video/8230/slug"));
        Assert.Equal(TrackEventKind.Hit, TrackPath.PageEvent("/"));
        Assert.Equal(TrackEventKind.Hit, TrackPath.PageEvent("/categories"));
    }

    [Fact]
    public void ShouldRecordPage_SkipsAssetsAndApi()
    {
        Assert.False(TrackPath.ShouldRecordPage("/api/getrom/1"));
        Assert.False(TrackPath.ShouldRecordPage("/vendor/x.js"));
        Assert.False(TrackPath.ShouldRecordPage("/logo.png"));
        Assert.False(TrackPath.ShouldRecordPage("/_s/track.js"));
        Assert.True(TrackPath.ShouldRecordPage("/"));
        Assert.True(TrackPath.ShouldRecordPage("/video/1"));
    }
}

public sealed class TrackStoreTests
{
    [Fact]
    public void Touch_SetsVideoAndHit()
    {
        var store = new TrackStore();
        var now = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);
        var visit = store.Touch(DateOnly.FromDateTime(now), "1.2.3.4", "flow-a", "4tube.xyz", "", "", TrackEventKind.Video, now);

        Assert.True(visit.Hit);
        Assert.True(visit.Video);
        Assert.False(visit.Play);
    }

    [Fact]
    public void TryMarkGoal_LocksIpForWindow()
    {
        var store = new TrackStore();
        var now = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);
        store.Touch(DateOnly.FromDateTime(now), "1.2.3.4", "flow-a", "4tube.xyz", "", "", TrackEventKind.Play, now);

        Assert.True(store.TryMarkGoal("1.2.3.4", TimeSpan.FromHours(24), now, out var first));
        Assert.True(first!.Goal);
        Assert.False(store.TryMarkGoal("1.2.3.4", TimeSpan.FromHours(24), now.AddHours(1), out _));

        var later = now.AddHours(25);
        store.Touch(DateOnly.FromDateTime(later), "1.2.3.4", "flow-a", "4tube.xyz", "", "", TrackEventKind.Hit, later);
        Assert.True(store.TryMarkGoal("1.2.3.4", TimeSpan.FromHours(24), later, out _));
    }

    [Fact]
    public void TryMarkGoal_PrefersPlayRow()
    {
        var store = new TrackStore();
        var now = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);
        store.Touch(DateOnly.FromDateTime(now), "9.9.9.9", "flow-home", "4tube.xyz", "", "", TrackEventKind.Hit, now);
        store.Touch(DateOnly.FromDateTime(now), "9.9.9.9", "flow-play", "4tube.xyz", "", "", TrackEventKind.Play, now.AddMinutes(1));

        Assert.True(store.TryMarkGoal("9.9.9.9", TimeSpan.FromHours(24), now.AddMinutes(2), out var visit));
        Assert.Equal("flow-play", visit!.Flow);
    }
}

public sealed class TrackMiddlewareTests
{
    [Fact]
    public async Task GetHome_WithFlow_SetsCookieAndHit()
    {
        var store = new TrackStore();
        var middleware = Create(store);
        var context = CreateContext("GET", "/", "?flow=camp1", "1.2.3.4");

        await middleware.InvokeAsync(context);

        Assert.Contains("sf=camp1", context.Response.Headers.SetCookie.ToString());
        var visit = store.TryGet(new TrackVisitKey(DateOnly.FromDateTime(DateTime.UtcNow), "1.2.3.4", "camp1", "4tube.xyz"));
        Assert.NotNull(visit);
        Assert.True(visit!.Hit);
        Assert.False(visit.Video);
    }

    [Fact]
    public async Task GetVideo_WithFlowCookie_SetsVideo()
    {
        var store = new TrackStore();
        var middleware = Create(store);
        var context = CreateContext("GET", "/video/8230/slug", "", "8.8.8.8");
        context.Request.Headers.Cookie = "sf=camp1";

        await middleware.InvokeAsync(context);

        var visit = store.TryGet(new TrackVisitKey(DateOnly.FromDateTime(DateTime.UtcNow), "8.8.8.8", "camp1", "4tube.xyz"));
        Assert.NotNull(visit);
        Assert.True(visit!.Video);
    }

    [Fact]
    public async Task PostPlayBeacon_SetsPlay()
    {
        var store = new TrackStore();
        var middleware = Create(store);
        var context = CreateContext("POST", "/t/e", "", "9.9.9.9");
        context.Request.Headers.Cookie = "sf=camp1";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""{"e":"play"}"""));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        var visit = store.TryGet(new TrackVisitKey(DateOnly.FromDateTime(DateTime.UtcNow), "9.9.9.9", "camp1", "4tube.xyz"));
        Assert.NotNull(visit);
        Assert.True(visit!.Play);
    }

    [Fact]
    public async Task PostGoal_MarksLockedVisit()
    {
        var store = new TrackStore();
        var now = DateTime.UtcNow;
        store.Touch(DateOnly.FromDateTime(now), "5.5.5.5", "camp1", "4tube.xyz", "", "", TrackEventKind.Play, now);
        var middleware = Create(store);
        var context = CreateContext("POST", "/internal/track/goal", "", "127.0.0.1");
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""{"ip":"5.5.5.5"}"""));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.True(store.TryGet(new TrackVisitKey(DateOnly.FromDateTime(now), "5.5.5.5", "camp1", "4tube.xyz"))!.Goal);
    }

    [Fact]
    public async Task GetTrackJs_ReturnsBeaconScript()
    {
        var middleware = Create(new TrackStore());
        var context = CreateContext("GET", "/_s/track.js", "", "1.1.1.1");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("sendBeacon", body);
        Assert.Contains("tube18:player", body);
        Assert.Equal("text/javascript; charset=utf-8", context.Response.ContentType);
    }

    private static TrackMiddleware Create(TrackStore store) =>
        new(_ => Task.CompletedTask, store, Microsoft.Extensions.Options.Options.Create(new TrackOptions()));

    private static DefaultHttpContext CreateContext(string method, string path, string query, string ip)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        context.SetSite(new TrackTestSite());
        return context;
    }

    private sealed class TrackTestSite : Sites.Web.Abstractions.ISiteModule
    {
        public string Name => "4tube";
        public string SourceBaseUrl => "https://www.example.com";
        public string SourceHost => "example.com";
        public string SourceUpstreamHost => "www.example.com";
        public string TargetHost => "4tube.xyz";
        public string TargetBaseUrl => "https://4tube.xyz";
        public IReadOnlyList<string> TargetHosts => ["4tube.xyz"];
        public Sites.Web.Abstractions.SiteProxyRules Rules { get; } = new();
        public string WebRootPath => "";
    }
}
