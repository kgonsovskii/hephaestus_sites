using System.Text.Json;

namespace Sites.Web.Abstractions;

public static class SiteSettingsConverter
{
    public static Dictionary<string, string> ToStringDictionary(Dictionary<string, JsonElement>? settings)
    {
        if (settings is null || settings.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string>(settings.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in settings)
            result[key] = JsonElementToString(value);

        return result;
    }

    private static string JsonElementToString(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
}
