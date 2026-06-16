using System.Text;
using Sites.Web.Abstractions;

namespace Sites.Web;

public static class HtmlInjector
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static byte[] InjectBytes(
        byte[] body,
        string? contentType,
        string requestPath,
        IReadOnlyList<HtmlInjection> injections)
    {
        if (injections.Count == 0 || !IsHtml(contentType))
            return body;

        var encoding = ContentRewriter.GetEncoding(contentType);
        var text = encoding.GetString(body);
        var rewritten = Inject(text, requestPath, injections);

        return ReferenceEquals(text, rewritten) || text == rewritten
            ? body
            : encoding.GetBytes(rewritten);
    }

    public static string Inject(
        string content,
        string requestPath,
        IReadOnlyList<HtmlInjection> injections)
    {
        if (injections.Count == 0)
            return content;

        var result = content;
        foreach (var injection in injections)
        {
            if (string.IsNullOrWhiteSpace(injection.Snippet))
                continue;

            if (!PathMatches(requestPath, injection.Paths))
                continue;

            result = InsertSnippet(result, injection.Snippet.Trim(), injection.Position);
        }

        return result;
    }

    internal static bool PathMatches(string requestPath, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
            return false;

        var normalized = NormalizePath(requestPath);
        foreach (var pattern in paths)
        {
            var path = pattern.Trim();
            if (path.Length == 0)
                continue;

            if (path == "*")
                return true;

            if (!path.StartsWith('/'))
                path = $"/{path}";

            if (path == "/" &&
                (normalized == "/" || normalized.Equals("/index.html", StringComparison.OrdinalIgnoreCase)))
                return true;

            if (normalized.Equals(path, StringComparison.OrdinalIgnoreCase))
                return true;

            var prefix = path.TrimEnd('/');
            if (prefix.Length > 1 &&
                normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string InsertSnippet(string content, string snippet, HtmlInjectionPosition position)
    {
        var insertIndex = position switch
        {
            HtmlInjectionPosition.AfterHeadOpen => FindHeadOpenInsertIndex(content),
            HtmlInjectionPosition.BeforeBodyClose => FindLastMarkerIndex(content, "</body>"),
            _ => FindLastMarkerIndex(content, "</head>")
        };

        if (insertIndex < 0)
            return content;

        return content.Insert(insertIndex, snippet);
    }

    private static int FindHeadOpenInsertIndex(string content)
    {
        var index = content.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return -1;

        var close = content.IndexOf('>', index);
        return close < 0 ? -1 : close + 1;
    }

    private static int FindLastMarkerIndex(string content, string marker)
    {
        var index = content.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? -1 : index;
    }

    private static bool IsHtml(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        return path.StartsWith('/') ? path : $"/{path}";
    }
}
