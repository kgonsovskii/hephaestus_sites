using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sites.DataFtp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSitesDataFtp(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SitesDataFtpOptions>(configuration.GetSection(SitesDataFtpOptions.SectionName));
        services.AddSingleton<ISitesWebRootPathProvider, SitesWebRootPathProvider>();
        services.AddSingleton<IMembershipProvider, SitesDataFtpMembershipProvider>();
        services.AddFtpServer(builder => builder
            .UseDotNetFileSystem()
            .UseSingleRoot());
        services.AddOptions<FtpServerOptions>()
            .Configure<IOptions<SitesDataFtpOptions>>((ftp, dataFtp) =>
            {
                ftp.ServerAddress = string.Empty;
                ftp.Port = dataFtp.Value.Port;
            });
        services.AddOptions<DotNetFileSystemOptions>()
            .Configure<ISitesWebRootPathProvider>((opts, webPaths) =>
            {
                opts.RootPath = webPaths.WebRootFullPath;
                opts.AllowNonEmptyDirectoryDelete = true;
            });
        services.AddSingleton<ISitesDataFtpUrlProvider, SitesDataFtpUrlProvider>();
        services.AddHostedService<SitesDataFtpHostedService>();
        return services;
    }
}
