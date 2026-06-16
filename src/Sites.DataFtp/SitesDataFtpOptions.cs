namespace Sites.DataFtp;

public sealed class SitesDataFtpOptions
{
    public const string SectionName = "DataFtp";

    public int Port { get; set; } = SitesDataFtpConstants.DefaultPort;
}
