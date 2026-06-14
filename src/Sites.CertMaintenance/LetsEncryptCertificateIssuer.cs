using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using IO = System.IO;

namespace Sites.CertMaintenance;

public sealed class LetsEncryptCertificateIssuer
{
    public async Task<LetsEncryptIssueResult> IssueOrRenewAsync(
        CertMaintenanceOptions options,
        IReadOnlyList<string> domains,
        IAcmeChallengePublisher challengePublisher,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(options, domains);

        var certDirectory = CertPathResolver.ResolveCertDirectory(options);
        IO.Directory.CreateDirectory(certDirectory);

        var accountKeyPath = CertPathResolver.ResolveAccountKeyPath(options);
        var pfxPath = CertPathResolver.ResolvePfxPath(options);
        var acmeServer = options.UseStaging
            ? WellKnownServers.LetsEncryptStagingV2
            : WellKnownServers.LetsEncryptV2;

        IKey? accountKey = null;
        if (IO.File.Exists(accountKeyPath))
            accountKey = KeyFactory.FromPem(await IO.File.ReadAllTextAsync(accountKeyPath, cancellationToken));

        var acme = accountKey is null
            ? new AcmeContext(acmeServer)
            : new AcmeContext(acmeServer, accountKey);

        if (accountKey is null)
        {
            if (!options.TermsOfServiceAgreed)
                throw new InvalidOperationException("Set CertMaintenance:TermsOfServiceAgreed to true.");

            await acme.NewAccount(options.AcmeEmail, true);
            await IO.File.WriteAllTextAsync(accountKeyPath, acme.AccountKey.ToPem(), cancellationToken);
        }

        var order = await acme.NewOrder(domains.ToList());
        challengePublisher.ClearChallenges();

        try
        {
            foreach (var authz in await order.Authorizations())
            {
                var httpChallenge = await authz.Http();
                challengePublisher.PublishChallenge(httpChallenge.Token, httpChallenge.KeyAuthz);
                await httpChallenge.Validate();

                var deadline = DateTimeOffset.UtcNow.AddMinutes(3);
                while (DateTimeOffset.UtcNow < deadline)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var status = (await httpChallenge.Resource()).Status;
                    if (status == ChallengeStatus.Valid)
                        break;

                    if (status == ChallengeStatus.Invalid)
                    {
                        var authorization = await authz.Resource();
                        throw new InvalidOperationException(
                            $"ACME HTTP-01 validation failed for {authorization.Identifier.Value}.");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
        }
        finally
        {
            challengePublisher.ClearChallenges();
        }

        var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
        var certificate = await order.Generate(new CsrInfo
        {
            CommonName = domains[0],
            CountryName = "US",
            State = "NA",
            Locality = "NA",
            Organization = "Sites",
            OrganizationUnit = "Publish"
        }, privateKey);

        var pfxPassword = options.CertPfxPassword ?? string.Empty;
        var pfxBytes = certificate.ToPfx(privateKey).Build("sites", pfxPassword);
        await IO.File.WriteAllBytesAsync(pfxPath, pfxBytes, cancellationToken);

        var publicCerPath = Path.ChangeExtension(pfxPath, ".cer");
        await IO.File.WriteAllBytesAsync(publicCerPath, certificate.Certificate.ToDer(), cancellationToken);

        return new LetsEncryptIssueResult(pfxPath, publicCerPath, domains, options.UseStaging);
    }

    private static void ValidateOptions(CertMaintenanceOptions options, IReadOnlyList<string> domains)
    {
        if (domains.Count == 0)
            throw new InvalidOperationException("No publish domains were discovered from site modules.");

        if (string.IsNullOrWhiteSpace(options.AcmeEmail) || !options.AcmeEmail.Contains('@'))
            throw new InvalidOperationException("Set CertMaintenance:AcmeEmail.");

        if (options.AcmeEmail.Contains("example.com", StringComparison.OrdinalIgnoreCase)
            || options.AcmeEmail.Contains("example.org", StringComparison.OrdinalIgnoreCase)
            || options.AcmeEmail.Contains("test@", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CertMaintenance:AcmeEmail must be a real address. Let's Encrypt rejects example.com and similar placeholders.");
        }
    }
}

public sealed record LetsEncryptIssueResult(
    string PfxPath,
    string PublicCerPath,
    IReadOnlyList<string> Domains,
    bool UsedStaging);
