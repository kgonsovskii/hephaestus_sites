using System.Security.Cryptography.X509Certificates;

namespace Sites.CertMaintenance;

public sealed class TlsCertificateStore : IDisposable
{
    private const X509KeyStorageFlags PfxKeyStorage =
        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable;

    private readonly object _sync = new();
    private X509Certificate2? _certificate;

    public X509Certificate2? Current
    {
        get
        {
            lock (_sync)
                return _certificate;
        }
    }

    public DateTimeOffset? NotAfter =>
        Current is null ? null : Current.NotAfter;

    public bool TryLoadFromPfx(string pfxPath, string? password)
    {
        if (!File.Exists(pfxPath))
            return false;

        var loaded = string.IsNullOrEmpty(password)
            ? X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null, keyStorageFlags: PfxKeyStorage)
            : X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password, PfxKeyStorage);

        lock (_sync)
        {
            _certificate?.Dispose();
            _certificate = loaded;
        }

        return true;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _certificate?.Dispose();
            _certificate = null;
        }
    }
}
