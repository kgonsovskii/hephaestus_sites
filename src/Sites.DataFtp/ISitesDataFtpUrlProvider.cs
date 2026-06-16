namespace Sites.DataFtp;

public interface ISitesDataFtpUrlProvider
{
    string WebRootFullPath { get; }

    int Port { get; }

    string BuildUrl(string hostName);
}
