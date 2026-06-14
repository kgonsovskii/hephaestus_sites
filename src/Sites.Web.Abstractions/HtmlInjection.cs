namespace Sites.Web.Abstractions;

public enum HtmlInjectionPosition
{
    BeforeHeadClose,
    AfterHeadOpen,
    BeforeBodyClose
}

public sealed class HtmlInjection
{
    /// <summary>
    /// Request paths that receive the snippet. Use "/" for home, "*" for all HTML pages.
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = ["/"];

    public HtmlInjectionPosition Position { get; init; } = HtmlInjectionPosition.BeforeHeadClose;

    /// <summary>
    /// Raw HTML to inject (e.g. a script tag).
    /// </summary>
    public string Snippet { get; init; } = string.Empty;
}
