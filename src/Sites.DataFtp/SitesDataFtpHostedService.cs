using FubarDev.FtpServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sites.DataFtp;

internal sealed class SitesDataFtpHostedService : IHostedService
{
    private readonly IFtpServerHost _ftpServerHost;
    private readonly ILogger<SitesDataFtpHostedService> _logger;

    public SitesDataFtpHostedService(IFtpServerHost ftpServerHost, ILogger<SitesDataFtpHostedService> logger)
    {
        _ftpServerHost = ftpServerHost;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _ftpServerHost.StartAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Sites data FTP server started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sites data FTP server failed to start.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _ftpServerHost.StopAsync(cancellationToken);
}
