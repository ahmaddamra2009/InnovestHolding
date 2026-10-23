using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// A lead-form POST whose page is unpublished (the page itself or an ancestor, <see cref="SiteSnapshot.IsPublished"/>)
/// answers 404, like the page. A resource filter: it runs before model binding and the action, so nothing of the body
/// is read or stored (CTO decision, C5 Dev 1 final). The forms of the home page (register, contact) are not marked: the
/// home page is always published.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PublishedFormPageAttribute(string pageKey) : Attribute, IAsyncResourceFilter
{
    public string PageKey { get; } = pageKey;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = context.HttpContext;
        var snapshot = await http.RequestServices.GetRequiredService<ISiteContentCache>().GetAsync(http.RequestAborted);
        if (!snapshot.IsPublished(PageKey))
        {
            context.Result = new NotFoundResult();
            return;
        }
        await next();
    }
}
