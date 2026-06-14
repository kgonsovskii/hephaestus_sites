using Sites.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Sites.Web.Tests.Integration;

public sealed class Tube18ProxyApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("SITES_SITE", "tube-18.xyz");
        return base.CreateHost(builder);
    }
}
