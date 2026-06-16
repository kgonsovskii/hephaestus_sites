using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class LocalJsSettingsReplacerTests
{
    [Fact]
    public void Replace_SubstitutesKnownSettingPlaceholder()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VideoInterval"] = "30"
        };

        var result = LocalJsSettingsReplacer.Replace(
            "var interval = $VideoInterval$;",
            settings);

        Assert.Equal("var interval = 30;", result);
    }

    [Fact]
    public void Replace_LeavesUnknownPlaceholderUntouched()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VideoInterval"] = "30"
        };

        var result = LocalJsSettingsReplacer.Replace(
            "var x = $Unknown$;",
            settings);

        Assert.Equal("var x = $Unknown$;", result);
    }

    [Fact]
    public void SiteSettingsConverter_ConvertsNumericJsonToString()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("""{"VideoInterval":30}""");
        var settings = doc.RootElement.EnumerateObject()
            .ToDictionary(static property => property.Name, static property => property.Value);

        var converted = SiteSettingsConverter.ToStringDictionary(settings);

        Assert.Equal("30", converted["VideoInterval"]);
    }
}
