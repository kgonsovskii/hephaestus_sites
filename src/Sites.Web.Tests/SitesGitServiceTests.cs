using Sites.Web.Git;

namespace Sites.Web.Tests;

public sealed class SitesGitServiceTests
{
    [Theory]
    [InlineData("https://github.com/kgonsovskii/hephaestus_sites.git", "ghp_testtoken", "https://x-access-token:ghp_testtoken@github.com/kgonsovskii/hephaestus_sites.git")]
    [InlineData("https://github.com/org/repo/", "github_pat_abc", "https://x-access-token:github_pat_abc@github.com/org/repo")]
    public void BuildAuthenticatedUrl_EncodesTokenInHttpsUrl(string repoUrl, string token, string expected)
    {
        var actual = SitesGitService.BuildAuthenticatedUrl(repoUrl, token);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildAuthenticatedUrl_RejectsNonHttps()
    {
        Assert.Throws<InvalidOperationException>(
            () => SitesGitService.BuildAuthenticatedUrl("git@github.com:org/repo.git", "token"));
    }
}
