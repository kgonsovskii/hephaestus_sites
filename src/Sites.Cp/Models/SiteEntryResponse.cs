using Sites.Web.Abstractions;

namespace Sites.Cp.Models;

public sealed class SitesListResponse
{
    public string Profile { get; init; } = string.Empty;

    public string SitesJsonPath { get; init; } = string.Empty;

    public IReadOnlyList<SiteEntryResponse> Sites { get; init; } = [];
}

public sealed class SiteEntryResponse
{
    public string TargetHost { get; init; } = string.Empty;

    public SiteDefinition Definition { get; init; } = new();

    public bool HasCodedModule { get; init; }
}
