using System.Text;
using Sites.Web.Abstractions;

namespace Sites.Web;

internal static class JsAssetPatch
{
    public static string SiblingPatchPath(string jsFilePath)
    {
        if (!string.Equals(Path.GetExtension(jsFilePath), ".js", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (jsFilePath.EndsWith(".patch.js", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var directory = Path.GetDirectoryName(jsFilePath);
        var name = Path.GetFileNameWithoutExtension(jsFilePath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(name))
            return string.Empty;

        return Path.Combine(directory, name + ".patch.js");
    }

    public static string? ResolvePatchPath(ISiteModule site, string requestPath)
    {
        var relative = (requestPath ?? string.Empty).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (relative.Length == 0)
            return null;

        var jsOnDisk = Path.Combine(site.WebRootPath, site.TargetHost.Trim(), relative);
        var patch = SiblingPatchPath(jsOnDisk);
        return patch.Length > 0 && File.Exists(patch) ? patch : null;
    }

    public static string Append(string js, string patch)
    {
        if (string.IsNullOrEmpty(patch))
            return js;

        if (js.Length > 0 && !js.EndsWith('\n') && !js.EndsWith("\r\n", StringComparison.Ordinal))
            return js + "\n" + patch;

        return js + patch;
    }

    public static byte[] AppendBytes(byte[] body, string? contentType, string patch, Encoding encoding)
    {
        if (string.IsNullOrEmpty(patch) || !IsJavaScript(contentType))
            return body;

        var js = encoding.GetString(body);
        return encoding.GetBytes(Append(js, patch));
    }

    public static bool IsJavaScript(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return mediaType.Equals("text/javascript", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/x-javascript", StringComparison.OrdinalIgnoreCase);
    }
}
