using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sites.CertMaintenance;

public static class CertMaintenanceBootstrap
{
    public static async Task TryBootstrapAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var worker = services.GetRequiredService<CertMaintenanceWorker>();
        if (!worker.IsEnabled())
            return;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Sites.Host");
        try
        {
            logger.LogInformation("Certificate bootstrap: loading or issuing TLS certificate.");
            await worker.RunOnceAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Certificate bootstrap failed; HTTPS may be unavailable until maintenance succeeds.");
        }
    }
}
