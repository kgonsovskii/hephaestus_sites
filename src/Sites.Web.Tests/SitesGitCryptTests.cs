using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesGitCryptTests
{
    [Fact]
    public void EncryptDecrypt_RoundTripsToken()
    {
        const string token = "github_pat_example_token_12345";
        var encrypted = SitesGitCrypt.Encrypt(token);
        Assert.DoesNotContain("github_pat", encrypted, StringComparison.Ordinal);
        Assert.Equal(token, SitesGitCrypt.Decrypt(encrypted));
    }

    [Fact]
    public void BuildAuthenticatedUrl_UsesAccessTokenPrefix()
    {
        var url = SitesGitPatFile.BuildAuthenticatedUrl(
            "https://github.com/kgonsovskii/hephaestus_sites.git",
            "github_pat_abc");
        Assert.Equal(
            "https://x-access-token:github_pat_abc@github.com/kgonsovskii/hephaestus_sites.git",
            url);
    }
}
