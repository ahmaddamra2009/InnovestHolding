using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// Adds the content-hash <c>?v=</c> to a static file path, the same value <c>asp-append-version</c> writes, so the file
/// gets the long immutable cache. Used where no tag helper can do it (a &lt;source srcset&gt;) and for every brand image,
/// so all of them are versioned the same way.
/// </summary>
public static class AssetVersion
{
    public static string Versioned(this HttpContext context, string path) =>
        context.RequestServices.GetRequiredService<IFileVersionProvider>().AddFileVersionToPath(context.Request.PathBase, path);
}
