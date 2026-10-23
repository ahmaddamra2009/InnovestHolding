using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Localization;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.ViewModels;

/// <summary>
/// The login page's brand: the Arabic site name (the admin language) and the uploaded logo on dark (null = the bundled
/// logo). The login page must open even when the content cannot be read (database down): then, and when site_name is
/// empty, the name comes from the resx (C4's value) and the bundled logo is shown.
/// </summary>
public sealed record LoginBrandVm(string SiteName, MediaRef? Logo)
{
    public static async Task<LoginBrandVm> LoadAsync(HttpContext context)
    {
        var services = context.RequestServices;
        var fallback = services.GetRequiredService<IStringLocalizer<SharedResource>>()[MessageKeys.Admin.LoginSiteName].Value;
        try
        {
            var snapshot = await services.GetRequiredService<ISiteContentCache>().GetAsync(context.RequestAborted);
            var name = new ContentVm(snapshot.Content, false).Text("site_name").Value;
            return new LoginBrandVm(string.IsNullOrWhiteSpace(name) ? fallback : name, snapshot.Image(SiteImages.LogoOnDark));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            services.GetRequiredService<ILogger<LoginBrandVm>>()
                .LogWarning(ex, "Login page: the site content could not be loaded; the bundled logo and name are used.");
            return new LoginBrandVm(fallback, null);
        }
    }
}
