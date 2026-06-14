using System.Text;

namespace Sites.Web.Abstractions;

/// <summary>XOR obfuscation for Git PAT (same algorithm as deploy/crypt-git-pat.*).</summary>
public static class SitesGitCrypt
{
    private const string Key = "SitesGitKey42";

    public static string Encrypt(string plain)
    {
        var key = Encoding.UTF8.GetBytes(Key);
        var bytes = Encoding.UTF8.GetBytes(plain);
        var hex = new StringBuilder(bytes.Length * 2);
        for (var i = 0; i < bytes.Length; i++)
            hex.Append((bytes[i] ^ key[i % key.Length]).ToString("x2"));

        return hex.ToString();
    }

    public static string Decrypt(string encryptedHex)
    {
        var key = Encoding.UTF8.GetBytes(Key);
        var t = encryptedHex.Trim();
        if (t.Length == 0 || t.Length % 2 != 0)
            throw new InvalidOperationException("Encrypted Git PAT hex is empty or has odd length.");

        var bytes = new byte[t.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(t.Substring(i * 2, 2), 16);

        for (var i = 0; i < bytes.Length; i++)
            bytes[i] ^= key[i % key.Length];

        return Encoding.UTF8.GetString(bytes);
    }
}
