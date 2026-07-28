using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sites.Web;

public static class SitesHttpLoggingExtensions
{
    public static IServiceCollection AddSitesVerboseHttpLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestProperties
                | HttpLoggingFields.RequestHeaders
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.ResponseHeaders
                | HttpLoggingFields.Duration;

            options.RequestHeaders.Add("Host");
            options.RequestHeaders.Add("User-Agent");
            options.RequestHeaders.Add("Referer");
            options.ResponseHeaders.Add("Content-Type");
            options.ResponseHeaders.Add("Content-Length");
            options.ResponseHeaders.Add("Cache-Control");
            options.ResponseHeaders.Add("ETag");
            options.ResponseHeaders.Add("X-Proxy-Cache");
            options.CombineLogs = true;
        });

        return services;
    }

    public static ILoggingBuilder AddSitesVerboseHttpLogLevels(this ILoggingBuilder logging)
    {
        logging.AddFilter("Sites.Web", LogLevel.Information);
        logging.AddFilter("Sites.Web.RequestLoggingMiddleware", LogLevel.Information);
        logging.AddFilter("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware", LogLevel.Information);
        return logging;
    }
}
