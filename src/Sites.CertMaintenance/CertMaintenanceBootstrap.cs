using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sites.CertMaintenance;

public static class CertMaintenanceBootstrap
{
    private const int RetryCount = 6;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    public static async Task TryBootstrapAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var worker = services.GetRequiredService<CertMaintenanceWorker>();
        if (!worker.IsEnabled())
            return;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Sites.Host");
        logger.LogInformation("Certificate bootstrap: loading or issuing TLS certificate.");

        for (var attempt = 1; attempt <= RetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await worker.RunOnceAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < RetryCount)
            {
                logger.LogWarning(
                    ex,
                    "Certificate bootstrap attempt {Attempt}/{RetryCount} failed; retrying in {Delay}.",
                    attempt,
                    RetryCount,
                    RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "Certificate bootstrap failed after {RetryCount} attempts; HTTPS may be unavailable until maintenance succeeds.",
                    RetryCount);
            }
        }
    }
}
