using System.Text.Json;
using Sites.Web;
using Sites.Web.Abstractions;

namespace Sites.Web.Tests;

public sealed class SitesJsonDeserializationTests
{
    [Fact]
    public void ConfigureApiJsonOptions_DeserializesCamelCaseHtmlInjectionPosition()
    {
        var options = new JsonSerializerOptions();
        SitesJsonFile.ConfigureApiJsonOptions(options);

        var definition = JsonSerializer.Deserialize<SiteDefinition>(
            """
            {
              "sourceHost": "tube18.sex",
              "htmlInjections": [
                {
                  "paths": ["/video"],
                  "position": "beforeBodyClose",
                  "snippet": "<script></script>"
                }
              ]
            }
            """,
            options);

        Assert.NotNull(definition);
        Assert.Single(definition!.HtmlInjections!);
        Assert.Equal(HtmlInjectionPosition.BeforeBodyClose, definition.HtmlInjections![0].Position);
    }
}
