namespace Sites.CertTool;

public sealed class CertToolOptions
{
    public const string SectionName = "CertTool";

    public int HttpChallengePort { get; set; } = 80;
}
