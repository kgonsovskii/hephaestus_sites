namespace Sites.RemoteDeploy;

public static class RemoteDeployProfile
{
    /// <summary>
    /// Profile sent to the remote VPS. Uses the first CLI argument when present;
    /// otherwise <see cref="Sites.Web.Abstractions.SitesProfileResolver.DefaultProfile"/>.
    /// Never reads local profile.txt.
    /// </summary>
    public static string ResolveForRemoteInstall(IReadOnlyList<string> args)
    {
        if (args.Count > 0
            && !args[0].StartsWith('-')
            && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Sites.Web.Abstractions.SitesProfileResolver.NormalizeProfileName(args[0]);
        }

        return Sites.Web.Abstractions.SitesProfileResolver.DefaultProfile;
    }

    public static (string profile, string[] remainingArgs) SplitProfileArg(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-') || string.IsNullOrWhiteSpace(args[0]))
            return (Sites.Web.Abstractions.SitesProfileResolver.DefaultProfile, args);

        return (
            Sites.Web.Abstractions.SitesProfileResolver.NormalizeProfileName(args[0]),
            args[1..]);
    }
}
