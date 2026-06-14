using Sites.RemoteDeploy;

namespace Sites.Web.Tests;

public sealed class RemoteDeployProfileTests
{
    [Fact]
    public void ResolveForRemoteInstall_UsesDefaultWhenNoArgs()
    {
        Assert.Equal("default", RemoteDeployProfile.ResolveForRemoteInstall([]));
    }

    [Fact]
    public void ResolveForRemoteInstall_UsesFirstArgNotLocalProfileFile()
    {
        var repo = Path.Combine(Path.GetTempPath(), "sites-deploy-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            File.WriteAllText(Path.Combine(repo, "src", "Sites.sln"), "");
            File.WriteAllText(Path.Combine(Directory.GetParent(repo)!.FullName, "profile.txt"), "staging");

            Sites.Web.Abstractions.SitesProfileResolver.Initialize(repo);
            Assert.Equal("staging", Sites.Web.Abstractions.SitesProfileResolver.Current);

            Assert.Equal("test", RemoteDeployProfile.ResolveForRemoteInstall(["test"]));
            Assert.Equal("default", RemoteDeployProfile.ResolveForRemoteInstall([]));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(repo)!, recursive: true);
        }
    }

    [Fact]
    public void SplitProfileArg_StripsProfileFromRemainingArgs()
    {
        var (profile, rest) = RemoteDeployProfile.SplitProfileArg(["staging", "--", "extra"]);
        Assert.Equal("staging", profile);
        Assert.Equal(["--", "extra"], rest);
    }
}
