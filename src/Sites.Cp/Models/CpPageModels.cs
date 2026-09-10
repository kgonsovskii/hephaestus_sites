namespace Sites.Cp.Models;

public sealed class SitesListPageModel
{
    public string Profile { get; init; } = string.Empty;

    public string SitesJsonPath { get; init; } = string.Empty;

    public string? WebFtpUrl { get; init; }

    public string? WebRootFullPath { get; init; }

    public IReadOnlyList<SiteListRow> Sites { get; init; } = [];
}

public sealed class SiteListRow
{
    public string TargetHost { get; init; } = string.Empty;

    public string SourceHost { get; init; } = string.Empty;

    public bool PassCookies { get; init; }

    public bool DisableCaching { get; init; }

    public bool HasCodedModule { get; init; }
}

public sealed class SiteEditPageModel
{
    public bool IsAdd { get; init; }

    public string TargetHost { get; init; } = string.Empty;

    public bool HasCodedModule { get; init; }

    public string Profile { get; init; } = string.Empty;

    public string DefinitionJson { get; init; } = "null";
}
