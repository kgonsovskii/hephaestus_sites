using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Sites.Web;

internal sealed class SitesResponseCompressionConfigurer : IConfigureOptions<ResponseCompressionOptions>
{
    private readonly SitesProfileSettingsService _settings;

    public SitesResponseCompressionConfigurer(SitesProfileSettingsService settings) => _settings = settings;

    public void Configure(ResponseCompressionOptions options)
    {
        var bandwidth = _settings.Get().ClientBandwidth;
        options.EnableForHttps = true;
        options.MimeTypes = bandwidth.CompressionContentTypes;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    }
}

internal sealed class SitesBrotliCompressionConfigurer : IConfigureOptions<BrotliCompressionProviderOptions>
{
    public void Configure(BrotliCompressionProviderOptions options) =>
        options.Level = CompressionLevel.Fastest;
}

internal sealed class SitesGzipCompressionConfigurer : IConfigureOptions<GzipCompressionProviderOptions>
{
    public void Configure(GzipCompressionProviderOptions options) =>
        options.Level = CompressionLevel.Fastest;
}
