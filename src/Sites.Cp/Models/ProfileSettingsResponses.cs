namespace Sites.Cp.Models;

public sealed class ProfileSettingsResponse
{
    public string Profile { get; init; } = string.Empty;

    public string SettingsJsonPath { get; init; } = string.Empty;

    public Sites.Web.SitesProfileSettingsDocument Settings { get; init; } = new();
}
