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

    [Fact]
    public void DataPatFile_IsSeparateFromCodePatFile()
    {
        var repoRoot = RepositoryPaths.ResolveRoot();
        Assert.Equal("git-pat.enc", SitesGitPatFile.EncryptedFileName);
        Assert.Equal("git-pat-data.enc", SitesGitPatFile.EncryptedDataFileName);
        Assert.True(File.Exists(SitesGitPatFile.ResolveEncryptedDataPath(repoRoot)));
        Assert.True(SitesGitPatFile.TryLoadDataToken(repoRoot, out var dataToken));
        Assert.StartsWith("github_pat_", dataToken, StringComparison.Ordinal);
        Assert.True(SitesGitPatFile.TryLoadToken(repoRoot, out var codeToken));
        Assert.NotEqual(dataToken, codeToken);
    }
}
