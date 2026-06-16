using Microsoft.Extensions.Configuration;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesProfileSettingsTemplateTests
{
    [Fact]
    public void CreateDocument_UsesAppsettingsSitesSection()
    {
        var configuration = TestSitesProxyOptions.CreateConfiguration();
        var template = new SitesProfileSettingsTemplate(configuration);
        var document = template.CreateDocument();

        Assert.Equal(TimeSpan.FromDays(7), document.Sites.Cache.Ttl);
    }

    [Fact]
    public void CreateDocument_FallsBackToExistingProfileSettingsWhenAppsettingsMissing()
    {
        var settingsPath = Path.Combine(
            AppContext.BaseDirectory,
            SitesProfileResolver.ProfilesDirectoryName,
            SitesProfileResolver.DefaultProfile,
            SitesProfileResolver.SettingsJsonFileName);

        var settingsDir = Path.GetDirectoryName(settingsPath)!;
        Directory.CreateDirectory(settingsDir);

        var profileDocument = TestSitesProxyOptions.CreateTemplate().CreateDocument();
        profileDocument.Sites.UpstreamRequestTimeout = TimeSpan.FromMinutes(4);
        SitesSettingsFile.Save(settingsPath, profileDocument);

        var emptyConfiguration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        try
        {
            Environment.SetEnvironmentVariable(SitesProfileResolver.ProfileEnvironmentVariable, SitesProfileResolver.DefaultProfile);
            SitesProfileResolver.Initialize();

            var template = new SitesProfileSettingsTemplate(emptyConfiguration);
            var document = template.CreateDocument();

            Assert.Equal(TimeSpan.FromMinutes(4), document.Sites.UpstreamRequestTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable(SitesProfileResolver.ProfileEnvironmentVariable, null);
            SitesProfileResolver.Initialize();
            if (File.Exists(settingsPath))
                File.Delete(settingsPath);
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, recursive: true);
        }
    }
}
