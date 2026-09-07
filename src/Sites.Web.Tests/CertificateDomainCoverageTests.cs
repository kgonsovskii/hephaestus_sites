using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Sites.CertMaintenance;

namespace Sites.Web.Tests;

public sealed class CertificateDomainCoverageTests
{
    [Fact]
    public void MissingDomains_NullCertificate_ReturnsAllRequired()
    {
        var missing = CertificateDomainCoverage.MissingDomains(null, ["tubepleasure.xyz", "www.tubepleasure.xyz"]);
        Assert.Equal(["tubepleasure.xyz", "www.tubepleasure.xyz"], missing);
    }

    [Fact]
    public void MissingDomains_OldHostCert_ReportsNewSiteHost()
    {
        using var cert = CreateCert("tube-18.xyz", "www.tube-18.xyz");
        var missing = CertificateDomainCoverage.MissingDomains(
            cert,
            ["tube-18.xyz", "www.tube-18.xyz", "tubepleasure.xyz", "www.tubepleasure.xyz"]);

        Assert.Contains("tubepleasure.xyz", missing);
        Assert.Contains("www.tubepleasure.xyz", missing);
        Assert.DoesNotContain("tube-18.xyz", missing);
    }

    [Fact]
    public void MissingDomains_MatchingSans_IsEmpty()
    {
        using var cert = CreateCert("tubepleasure.xyz", "www.tubepleasure.xyz");
        var missing = CertificateDomainCoverage.MissingDomains(
            cert,
            ["tubepleasure.xyz", "www.tubepleasure.xyz"]);
        Assert.Empty(missing);
    }

    private static X509Certificate2 CreateCert(params string[] dnsNames)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={dnsNames[0]}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        foreach (var dns in dnsNames)
            san.AddDnsName(dns);
        request.CertificateExtensions.Add(san.Build());

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(90));
    }
}
