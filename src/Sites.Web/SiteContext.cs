using Sites.Web.Abstractions;

namespace Sites.Web;

public static class SiteContext
{
    public const string ItemKey = "Sites.Site";

    public static ISiteModule GetSite(this HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is ISiteModule site)
            return site;

        throw new InvalidOperationException("No site was resolved for the current request.");
    }

    public static void SetSite(this HttpContext context, ISiteModule site) =>
        context.Items[ItemKey] = site;
}
