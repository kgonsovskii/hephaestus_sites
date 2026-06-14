using System.Text;
using System.Text.RegularExpressions;
using Sites.Web.Abstractions;

namespace Sites.Web;

public static class ContentRewriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly string[] RewritableMediaTypes =
    [
        "text/html",
        "text/css",
        "text/javascript",
        "application/javascript",
        "application/json",
        "text/xml",
        "application/xml",
        "application/xhtml+xml"
    ];

    public static bool ShouldRewrite(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return RewritableMediaTypes.Any(type =>
            mediaType.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    public static Encoding GetEncoding(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Utf8WithoutBom;

        var parts = contentType.Split(';', StringSplitOptions.TrimEntries);
        foreach (var part in parts.Skip(1))
        {
            if (!part.StartsWith("charset=", StringComparison.OrdinalIgnoreCase))
                continue;

            var charset = part["charset=".Length..];
            try
            {
                var encoding = Encoding.GetEncoding(charset);
                if (encoding is UTF8Encoding)
                    return Utf8WithoutBom;

                return encoding;
            }
            catch (ArgumentException)
            {
                break;
            }
        }

        return Utf8WithoutBom;
    }

    public static byte[] RewriteBytes(
        byte[] body,
        string? contentType,
        IReadOnlyList<ContentReplacement> replacements)
    {
        if (replacements.Count == 0)
            return body;

        var encoding = GetEncoding(contentType);
        var text = encoding.GetString(body);
        var rewritten = Rewrite(text, replacements);

        return ReferenceEquals(text, rewritten) || text == rewritten
            ? body
            : encoding.GetBytes(rewritten);
    }

    public static string Rewrite(string content, IReadOnlyList<ContentReplacement> replacements)
    {
        if (replacements.Count == 0)
            return content;

        var result = content;
        foreach (var replacement in replacements.OrderByDescending(r => r.From.Length))
        {
            if (string.IsNullOrEmpty(replacement.From))
                continue;

            result = replacement.WordBoundaryOnly
                ? ReplaceWholeWord(result, replacement.From, replacement.To)
                : result.Replace(replacement.From, replacement.To, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string ReplaceWholeWord(string content, string from, string to)
    {
        var pattern = $@"\b{Regex.Escape(from)}\b";
        return Regex.Replace(content, pattern, to, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
