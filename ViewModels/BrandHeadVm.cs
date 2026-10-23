using InnovestHolding.Infrastructure.Content;

namespace InnovestHolding.ViewModels;

/// <summary>
/// The brand part of every &lt;head&gt; (public layout and login page): theme colour and the uploaded favicon and
/// apple-touch icon, or null for each one that is not uploaded (the view then links the bundled file).
/// </summary>
public sealed record BrandHeadVm(string ThemeColor, MediaRef? Favicon, MediaRef? AppleTouchIcon)
{
    /// <summary>Nothing uploaded, default colour: exactly the head the site had before the brand settings existed.</summary>
    public static readonly BrandHeadVm Default = new(BrandImages.DefaultThemeColor, null, null);

    public static BrandHeadVm From(SiteSnapshot s) => new(
        BrandImages.ThemeColor(s.Content.GetValueOrDefault(BrandImages.ThemeColorKey)?.Ar),
        s.Image(BrandImages.Favicon),
        s.Image(BrandImages.AppleTouchIcon));
}
