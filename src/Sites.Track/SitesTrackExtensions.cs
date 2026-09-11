using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sites.Web.Abstractions;

namespace Sites.Track;

public static class SitesTrackExtensions
{
    public static IServiceCollection AddSitesTrack(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TrackOptions>(configuration.GetSection(TrackOptions.SectionName));
        services.PostConfigure<TrackOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                options.ConnectionString = configuration.GetConnectionString("Default");
        });
        services.AddSingleton<TrackStore>();
        services.AddSingleton<TrackPostgres>();
        services.AddSingleton<IHtmlExtras, TrackHtmlExtras>();
        services.AddHostedService<TrackFlushHostedService>();
        return services;
    }

    public static IApplicationBuilder UseSitesTrack(this IApplicationBuilder app)
    {
        app.UseMiddleware<TrackMiddleware>();
        return app;
    }
}
