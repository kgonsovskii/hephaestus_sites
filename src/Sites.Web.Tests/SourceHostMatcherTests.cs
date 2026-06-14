using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SourceHostMatcherTests
{
    [Theory]
    [InlineData("stor6.tube18.sex")]
    [InlineData("www.tube18.sex")]
    [InlineData("tube18.sex")]
    public void ShouldFollowSourceRedirect_MatchesAllSourceHosts(string host)
    {
        var site = TestSites.Tube18();

        Assert.True(SourceHostMatcher.ShouldFollowSourceRedirect(site, host));
    }

    [Fact]
    public void ShouldFollowSourceRedirect_RejectsExternalHost()
    {
        var site = TestSites.Tube18();

        Assert.False(SourceHostMatcher.ShouldFollowSourceRedirect(site, "example.com"));
    }
}
