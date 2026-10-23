using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// A form POST answers 404, like a page that is not there, unless the form is shown somewhere: its page is published
/// (the page itself and every ancestor, <see cref="SiteSnapshot.IsPublished"/>) AND the page section that renders it is
/// visible (<see cref="PageSectionRegistry.IsVisible"/>; a section that cannot be hidden always is). CTO decisions C5:
/// unpublished page → 404 (Dev 1 final); hidden form section → 404 (final round, Hassan F8 / Yasser L-C5F-2).
/// A resource filter: it runs before model binding and the action, so nothing of the body is read or stored.
/// </summary>
/// <remarks>
/// A form shown in two places (the contact form: the home section and the Contact page) names the second place with
/// <see cref="AlsoPageKey"/>/<see cref="AlsoSectionKey"/>; it stays open while either place shows it. The legacy
/// <see cref="LegacyPath"/> (/Home/Register, /Home/Contact — only the old one-page home ever posted there) counts the
/// first place only.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PublishedFormPageAttribute(string pageKey, string sectionKey) : Attribute, IAsyncResourceFilter
{
    public string PageKey { get; } = pageKey;
    public string SectionKey { get; } = sectionKey;
    public string? AlsoPageKey { get; set; }
    public string? AlsoSectionKey { get; set; }
    public string? LegacyPath { get; set; }

    private static bool Shown(SiteSnapshot s, string page, string section) =>
        s.IsPublished(page) && PageSectionRegistry.IsVisible(s, page, section);

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = context.HttpContext;
        var snapshot = await http.RequestServices.GetRequiredService<ISiteContentCache>().GetAsync(http.RequestAborted);
        var legacy = LegacyPath is not null
                     && string.Equals(http.Request.Path.Value?.TrimEnd('/'), LegacyPath, StringComparison.OrdinalIgnoreCase);
        var open = Shown(snapshot, PageKey, SectionKey)
                   || (!legacy && AlsoPageKey is not null && AlsoSectionKey is not null && Shown(snapshot, AlsoPageKey, AlsoSectionKey));
        if (!open)
        {
            context.Result = new NotFoundResult();
            return;
        }
        await next();
    }
}
