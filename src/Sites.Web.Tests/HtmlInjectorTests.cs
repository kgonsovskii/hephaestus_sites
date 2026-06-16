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

    [Fact]
    public void PathMatches_PrefixMatchesNestedPaths()
    {
        Assert.True(HtmlInjector.PathMatches("/video/8230/step-sista/", ["/video"]));
        Assert.True(HtmlInjector.PathMatches("/video/8230", ["/video"]));
        Assert.False(HtmlInjector.PathMatches("/videos/latest", ["/video"]));
        Assert.False(HtmlInjector.PathMatches("/latest-video/1", ["/video"]));
    }

    [Fact]
    public void Inject_VideoPrefix_InsertsOnNestedPath()
    {
        const string html = "<html><head><title>Video</title></head><body></body></html>";
        var injections = new[]
        {
            new HtmlInjection
            {
                Paths = ["/video"],
                Position = HtmlInjectionPosition.BeforeHeadClose,
                Snippet = "<script src=\"/x/videoscript.js\"></script>"
            }
        };

        var result = HtmlInjector.Inject(html, "/video/8230/step-sista/", injections);

        Assert.Contains("<script src=\"/x/videoscript.js\"></script></head>", result);
    }
}
