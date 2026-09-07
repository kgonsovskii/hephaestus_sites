using System.Security.Cryptography.X509Certificates;

namespace Sites.CertMaintenance;

public static class CertificateDomainCoverage
{
    public static IReadOnlyList<string> MissingDomains(
        X509Certificate2? certificate,
        IReadOnlyList<string> requiredHosts)
    {
        if (requiredHosts.Count == 0)
            return [];

        if (certificate is null)
            return requiredHosts.ToArray();

        var names = GetDnsNames(certificate);
        return requiredHosts
            .Where(host => !names.Contains(host.Trim().TrimEnd('.'), StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlySet<string> GetDnsNames(X509Certificate2 certificate)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var cn = certificate.GetNameInfo(X509NameType.DnsName, forIssuer: false);
        if (!string.IsNullOrWhiteSpace(cn))
            names.Add(cn.Trim().TrimEnd('.'));

        foreach (var extension in certificate.Extensions)
        {
            if (extension is not X509SubjectAlternativeNameExtension san)
                continue;

            foreach (var dns in san.EnumerateDnsNames())
            {
                if (!string.IsNullOrWhiteSpace(dns))
                    names.Add(dns.Trim().TrimEnd('.'));
            }
        }

        return names;
    }
}
