namespace InnovestHolding.Infrastructure.Content;

/// <summary>An image of the site itself, uploaded from the Content editor (its own small form per image).</summary>
/// <param name="Key">The SiteContents key that holds the FileKey of the uploaded image (not a text field of the registry).</param>
/// <param name="Label">Arabic admin label (the admin is Arabic only).</param>
/// <param name="Hint">Arabic admin hint: where the image appears and what is shown while it is empty.</param>
/// <param name="IsLogo">Uploaded under the logo caps (<see cref="Media.ImageUse.Logo"/>: 1 MB, 1200 px), like partner logos.</param>
public sealed record SiteImage(string Key, string Label, string Hint, bool IsLogo = false);

/// <summary>
/// The site's uploadable images. Each one falls back to what the site showed before it existed (the bundled brand
/// files, the drawn triangle) while nothing is uploaded, so an empty value never leaves a hole.
/// </summary>
public static class SiteImages
{
    public const string LogoOnDark = "logo_on_dark";
    public const string LogoOnLight = "logo_on_light";
    public const string HeroImage = "hero_image";

    public static readonly IReadOnlyList<SiteImage> All =
    [
        new(LogoOnDark, "الشعار على خلفية داكنة",
            "يظهر في رأس الموقع وتذييله وبطاقة «من نحن» في الرئيسية. يُفضّل PNG أو WebP بخلفية شفافة. فارغ = الشعار المرفق مع الموقع.", IsLogo: true),
        new(LogoOnLight, "الشعار على خلفية فاتحة",
            "شعار المؤسسة الذي تقرؤه محركات البحث (بيانات المؤسسة). يُفضّل PNG أو WebP بخلفية شفافة. فارغ = الشعار المرفق مع الموقع.", IsLogo: true),
        new(HeroImage, "صورة الواجهة في الرئيسية",
            "تظهر بجانب عنوان الصفحة الرئيسية مكان المثلث البرتقالي. فارغ = المثلث."),
        new(ContentKeys.FounderPhoto, "صورة المؤسس", "قسم كلمة المؤسس في صفحة «من نحن» (يظهر القسم عند كتابة الكلمة)."),
    ];

    public static SiteImage? Find(string? key) => All.FirstOrDefault(i => i.Key == key);
}
