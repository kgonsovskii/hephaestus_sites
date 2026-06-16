using System.Text.RegularExpressions;

namespace Sites.Web;

internal static partial class LocalJsSettingsReplacer
{
    [GeneratedRegex(@"\$([A-Za-z_][A-Za-z0-9_]*)\$", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();

    public static string Replace(string content, IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Count == 0 || content.IndexOf('$') < 0)
            return content;

        return PlaceholderPattern().Replace(content, match =>
        {
            var key = match.Groups[1].Value;
            return settings.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
