using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class HtmlInjectorTests
{
    [Fact]
    public void Inject_AfterHeadOpen_WorksWithHeadAttributes()
    {
        const string html = "<html><head lang=\"en\"><title>Home</title></head><body></body></html>";
        var injections = new[]
        {
            new HtmlInjection
            {
                Paths = ["*"],
                Position = HtmlInjectionPosition.AfterHeadOpen,
                Snippet = "<script src=\"https://example.com/a.js\"></script>"
            }
        };

        var result = HtmlInjector.Inject(html, "/games/doom", injections);

        Assert.Contains("<head lang=\"en\"><script src=\"https://example.com/a.js\"></script>", result);
    }

    [Fact]
    public void Inject_RootPath_InsertsBeforeHeadClose()
    {
        const string html = "<html><head><title>Home</title></head><body>Hi</body></html>";
        var injections = new[]
        {
            new HtmlInjection
            {
                Paths = ["/"],
                Position = HtmlInjectionPosition.BeforeHeadClose,
                Snippet = "<script src=\"https://example.com/a.js\"></script>"
            }
        };

        var result = HtmlInjector.Inject(html, "/", injections);

        Assert.Contains("<script src=\"https://example.com/a.js\"></script></head>", result);
    }

    [Fact]
    public void Inject_IndexHtmlPath_InsertsSnippet()
    {
        const string html = "<html><head></head><body></body></html>";
        var injections = new[]
        {
            new HtmlInjection
            {
                Paths = ["/"],
                Snippet = "<script></script>"
            }
        };

        var result = HtmlInjector.Inject(html, "/index.html", injections);

        Assert.Contains("<script></script></head>", result);
    }

    [Fact]
    public void Inject_NonMatchingPath_LeavesHtmlUnchanged()
    {
        const string html = "<html><head></head><body></body></html>";
        var injections = new[]
        {
            new HtmlInjection
            {
                Paths = ["/"],
                Snippet = "<script></script>"
            }
        };

        var result = HtmlInjector.Inject(html, "/games/doom", injections);

        Assert.Equal(html, result);
    }
}
