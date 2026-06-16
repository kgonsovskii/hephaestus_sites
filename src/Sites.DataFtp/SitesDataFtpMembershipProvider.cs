using FubarDev.FtpServer.AccountManagement;

namespace Sites.DataFtp;

internal sealed class SitesDataFtpMembershipProvider : IMembershipProvider
{
    public Task<MemberValidationResult> ValidateUserAsync(string username, string password)
    {
        if (string.Equals(username, SitesDataFtpConstants.UserName, StringComparison.Ordinal)
            && password == SitesDataFtpConstants.Password)
        {
            var user = new SitesDataFtpUser(username);
            return Task.FromResult(
                new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, user));
        }

        return Task.FromResult(new MemberValidationResult(MemberValidationStatus.InvalidLogin));
    }

    private sealed class SitesDataFtpUser : IFtpUser
    {
        public SitesDataFtpUser(string name) => Name = name;

        public string Name { get; }

        public bool IsInGroup(string group) => false;
    }
}
