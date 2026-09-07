namespace Sites.Web.Abstractions;

public sealed class ContentReplacement
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;

    /// <summary>
    /// When true, only replaces whole words (e.g. "RedTube" in text, not inside "desiredtube.com").
    /// </summary>
    public bool WordBoundaryOnly { get; init; }

    /// <summary>
    /// When true, <see cref="From"/> is a regular expression (ignore-case).
    /// </summary>
    public bool IsRegex { get; init; }
}
