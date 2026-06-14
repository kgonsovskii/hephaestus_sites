using Sites.Host;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sites.Web.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class Tube18VideoPageIntegrationTests : IClassFixture<Tube18ProxyApplicationFactory>
{
    private const string VideoPagePath =
        "/video/8230/step-sista-pokes-her-way-to-school-with-bro-s-help/";

    private readonly HttpClient _client;

    public Tube18VideoPageIntegrationTests(Tube18ProxyApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5080"),
            AllowAutoRedirect = true
        });
        _client.Timeout = TimeSpan.FromMinutes(2);
    }

    [Fact]
    public async Task VideoPage_DoesNotExposeRawCdnSubdomainUrls()
    {
        using var response = await _client.GetAsync(VideoPagePath);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("://stor", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".tube18.sex/get_file/", html, StringComparison.OrdinalIgnoreCase);
    }
}
