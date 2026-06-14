using System.Text;
using Sites.RemoteDeploy;
using Sites.Web.Abstractions;

namespace Sites.Deploy.Cli;

public static class RemoteCredsFile
{
    public const string DefaultFileName = "install-remote-creds.txt";

    public static RemoteCreds Load(string baseDirectory, DeployOptions fallback)
    {
        var path = ResolveCredsPath(baseDirectory);
        if (File.Exists(path))
            return LoadFromPath(path);

        if (string.IsNullOrWhiteSpace(fallback.Server) || string.IsNullOrWhiteSpace(fallback.Password))
            throw new FileNotFoundException(
                $"Missing {DefaultFileName} and Deploy:Server/Password are not set. Path tried: {path}",
                path);

        return new RemoteCreds(fallback.Server.Trim(), fallback.Login.Trim(), fallback.Password);
    }

    public static RemoteCreds LoadFromPath(string path)
    {
        var lines = File.ReadAllText(path, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        var taken = new List<string>();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#'))
                continue;

            taken.Add(t);
            if (taken.Count == 3)
                break;
        }

        if (taken.Count < 3)
            throw new InvalidOperationException(
                $"{path} must contain three non-empty lines: host, login, password (got {taken.Count}).");

        return new RemoteCreds(taken[0], taken[1], taken[2]);
    }

    public static string ResolveCredsPath(string baseDirectory, string fileName = DefaultFileName)
    {
        var candidates = new[]
        {
            Path.Combine(RepositoryPaths.DeployDirectory(baseDirectory), fileName),
            Path.Combine(baseDirectory, "deploy", fileName),
            Path.Combine(baseDirectory, fileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return candidates[0];
    }
}
