using Sites.Web.Abstractions;

namespace Sites.Track;

public sealed class TrackHtmlExtras : IHtmlExtras
{
    public static readonly HtmlInjection Script = new()
    {
        Paths = ["*"],
        Position = HtmlInjectionPosition.BeforeBodyClose,
        Snippet = """<script src="/_s/s.js"></script>"""
    };

    public IReadOnlyList<HtmlInjection> Injections { get; } = [Script];
}
