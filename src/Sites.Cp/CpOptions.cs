namespace Sites.Cp;

public sealed class CpOptions
{
    public const string SectionName = "Cp";

    public string PathPrefix { get; set; } = "/cp";

    public string? AdminPassword { get; set; }
}
