namespace Sites.Web.Abstractions;

/// <summary>Optional extra HTML injections applied to every proxied HTML page (e.g. tracking script).</summary>
public interface IHtmlExtras
{
    IReadOnlyList<HtmlInjection> Injections { get; }
}
