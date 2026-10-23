using System.Globalization;
using System.Text;
using InnovestHolding.Infrastructure.Media;
using InnovestHolding.Infrastructure.Site;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// A colour of the forum identity: a SiteContents key holding #RRGGBB (neutral, in ContentAr) and the CSS custom
/// property it sets on <c>body.theme-forum</c> (plus <c>{Variable}-rgb</c> as "r, g, b" for rgba()). Empty key = the
/// default of wwwroot/css/forum.css (Layla's palette, FORUM-IDENTITY.md), which <see cref="Default"/> repeats for the
/// contrast check.
/// </summary>
public sealed record ForumColor(string Key, string Label, string Hint, string Default, string Variable);

/// <summary>The bundled forum files (from the event profile PDF, FORUM-IDENTITY.md): used while nothing is uploaded.</summary>
public static class ForumAssets
{
    /// <summary>The forum symbol (∞ arrows), trimmed, 80 px high (2× of the 40 px header size). Header fallback, followed by
    /// the forum name as text: the stacked logo's lines are unreadable at header height (Layla §1).</summary>
    public const string Symbol = "/assets/forum/forum-symbol-80h.png";
    public const int SymbolWidth = 152;
    public const int SymbolHeight = 80;

    /// <summary>The full stacked logo for light backgrounds (480×449): the forum Organization's JSON-LD logo.</summary>
    public const string LogoOnLight = "/assets/forum/forum-logo-on-light-480w.png";
}

/// <summary>A font the admin may choose (a fixed table: the stored value is the key, never a family name).</summary>
/// <param name="Stack">The CSS font-family stack (code constant).</param>
public sealed record ForumFont(string Key, string Label, string Stack);

/// <summary>A contrast rule checked on save: foreground on background must reach <paramref name="Min"/> (4.5 = WCAG AA body
/// text; 3 = large text or a UI graphic). A side is a colour key of <see cref="ForumIdentity.Colors"/> or a fixed #rrggbb.</summary>
public sealed record ContrastPair(string Foreground, string Background, string Where, double Min = ForumIdentity.MinContrast);

/// <summary>A pair that failed, with its effective colours and measured ratio.</summary>
public sealed record ContrastFailure(ContrastPair Pair, string Foreground, string Background, double Ratio);

/// <summary>
/// The forum's visual identity (Admin › «هوية المنتدى»): colours, fonts, logos, hero and share image. Values are
/// SiteContents keys (colours and fonts) and MediaFiles FileKeys held by SiteContents keys (images, the SiteImages
/// pattern) — no table, no migration. Applied only on forum pages (<see cref="IsForumPage"/>, decided from the route
/// table on the server) through the stylesheet /forum-theme.css (<see cref="ForumThemeStylesheet"/>), which is built only
/// from validated values: #RRGGBB colours (checked on save AND on read), font keys of <see cref="Fonts"/>, media URLs of
/// stored keys. Admin labels and messages are Arabic literals (the admin is Arabic only, like BrandImages).
/// </summary>
public static class ForumIdentity
{
    public const double MinContrast = 4.5;

    // ── Colour keys ──
    public const string Primary = "forum_color_primary";
    public const string PrimaryDeep = "forum_color_primary_deep";
    public const string HeroStart = "forum_color_hero_start";
    public const string Gold = "forum_color_gold";
    public const string GoldInk = "forum_color_gold_ink";
    public const string Link = "forum_color_link";
    public const string Surface = "forum_color_surface";
    public const string Ink = "forum_color_ink";
    public const string Text = "forum_color_text";
    public const string OnPrimary = "forum_color_on_primary";

    /// <summary>Fixed surfaces the theme does not change: the page background and the company header bar.</summary>
    public const string White = "#ffffff";
    public const string HeaderNavy = "#111145";

    /// <summary>The forum palette (defaults = wwwroot/css/forum.css; a test keeps the two equal).</summary>
    public static readonly IReadOnlyList<ForumColor> Colors =
    [
        new(Primary, "الأحمر الأساسي", "العناوين، الأزرار الرئيسية، زر الرأس، خط التبويب والقائمة النشطة.", "#d21e33", "--forum-primary"),
        new(PrimaryDeep, "الأحمر الداكن", "لون الأزرار عند التمرير ونهاية تدرّج شريط العنوان.", "#a11c34", "--forum-primary-deep"),
        new(HeroStart, "بداية تدرّج شريط العنوان", "اللون الذي يبدأ به تدرّج شريط عنوان الصفحة.", "#d51d33", "--forum-hero-start"),
        new(Gold, "الذهبي", "الحلقة الذهبية في شريط العنوان وإطار الأزرار الثانوية (زخرفة، لا نص أبيض عليه).", "#bd932e", "--forum-gold"),
        new(GoldInk, "الذهبي للنصوص", "النصوص الذهبية على الخلفية البيضاء: نص الأزرار الثانوية والتبويب عند التمرير.", "#8c6510", "--forum-gold-ink"),
        new(Link, "لون الروابط", "الروابط والنصوص المميَّزة على الخلفيات الفاتحة.", "#a11c34", "--forum-link"),
        new(Surface, "لون الأقسام الفاتحة", "خلفية الأقسام الفاتحة (الكريمي) بالتناوب مع الأبيض.", "#f1e8d2", "--forum-surface"),
        new(Ink, "اللون الداكن", "النصوص الداكنة (عناوين البطاقات والحقول) والنص فوق الذهبي.", "#252626", "--forum-ink"),
        new(Text, "لون نص الفقرات", "نص الفقرات على الخلفيات الفاتحة.", "#4b4c4c", "--forum-text"),
        new(OnPrimary, "النص على الأحمر", "نص الأزرار الرئيسية وشريط العنوان.", "#ffffff", "--forum-on-primary"),
    ];

    /// <summary>
    /// The pairs the theme puts on top of each other. The effective colour of a pair is the admin's value, else the
    /// default — so changing one colour is checked against the others as they will render.
    /// </summary>
    public static readonly IReadOnlyList<ContrastPair> Pairs =
    [
        new(OnPrimary, Primary, "نص الأزرار الرئيسية وزر الرأس على الأحمر"),
        new(OnPrimary, PrimaryDeep, "نص الأزرار عند التمرير ونهاية شريط العنوان"),
        new(OnPrimary, HeroStart, "عنوان الصفحة على بداية تدرّج شريط العنوان"),
        new(Primary, White, "العناوين الحمراء على الخلفية البيضاء (نص كبير)", 3.0),
        new(Primary, Surface, "العناوين الحمراء على الأقسام الفاتحة (نص كبير)", 3.0),
        new(Primary, HeaderNavy, "خط القائمة النشطة الأحمر على الرأس الكحلي (عنصر رسومي)", 3.0),
        new(GoldInk, White, "النص الذهبي على الخلفية البيضاء"),
        new(Ink, Gold, "النص الداكن على الذهبي (الزر الثانوي عند التمرير)"),
        new(Link, White, "الروابط على الخلفية البيضاء"),
        new(Link, Surface, "الروابط على الأقسام الفاتحة"),
        new(Ink, White, "النصوص الداكنة على الخلفية البيضاء"),
        new(Ink, Surface, "النصوص الداكنة على الأقسام الفاتحة"),
        new(Text, White, "نص الفقرات على الخلفية البيضاء"),
        new(Text, Surface, "نص الفقرات على الأقسام الفاتحة"),
    ];

    // ── Fonts (only families the layout already loads: Cairo, Montserrat — no new font request) ──
    public const string FontArKey = "forum_font_ar";
    public const string FontEnKey = "forum_font_en";

    public static readonly IReadOnlyList<ForumFont> Fonts =
    [
        new("cairo", "Cairo", "'Cairo', system-ui, sans-serif"),
        new("montserrat", "Montserrat (لاتيني، مع Cairo للعربي)", "'Montserrat', 'Cairo', system-ui, sans-serif"),
    ];

    public static ForumFont? FindFont(string? key) => Fonts.FirstOrDefault(f => f.Key == key);

    // ── Images (each a SiteContents key holding a FileKey, uploaded under the BrandImages slot rules) ──
    public const string LogoOnDark = "forum_logo_on_dark";
    public const string LogoOnLight = "forum_logo_on_light";
    public const string HeroImage = "forum_hero_image";
    public const string OgDefault = "forum_og_default";

    public static readonly IReadOnlyList<BrandImage> Images =
    [
        new(LogoOnDark, "شعار المنتدى على خلفية داكنة",
            "يظهر في رأس صفحات المنتدى بجانب شعار إنوفست. PNG أو WebP بخلفية شفافة، من 120×32 بكسل حتى 1200 بكسل لأطول ضلع، حتى 1 MB. فارغ = رمز المنتدى المرفق مع اسم المنتدى نصاً (من عنوان صفحة المنتدى).",
            [ImageFormat.Png, ImageFormat.WebP], 120, 32, 1200, Square: false, RejectAlpha: false, MaxBytes: 1024 * BrandImages.KB,
            DefaultPath: ForumAssets.Symbol, DefaultSize: "152×80 + اسم المنتدى", HasAlt: true),
        new(LogoOnLight, "شعار المنتدى على خلفية فاتحة",
            "شعار المنتدى الذي تقرؤه محركات البحث (بيانات المنتدى المنظَّمة). PNG أو WebP، مربع أو شبه مربع، من 112×112 بكسل حتى 1200 بكسل، حتى 1 MB. فارغ = شعار المنتدى المرفق. لا يُعرض في الصفحات، لذلك لا نص بديل له.",
            [ImageFormat.Png, ImageFormat.WebP], 112, 112, 1200, Square: false, RejectAlpha: false, MaxBytes: 1024 * BrandImages.KB,
            DefaultPath: ForumAssets.LogoOnLight, DefaultSize: "480×449"),
        new(HeroImage, "صورة خلفية شريط العنوان",
            "تظهر خلف عنوان كل صفحة من صفحات المنتدى، تحت طبقة من اللون الأساسي الداكن ليبقى النص مقروءاً. JPG أو PNG أو WebP، من 1200×400 بكسل حتى 4000 بكسل و12 ميغابكسل، حتى 5 MB. صورة زخرفية: لا نص بديل.",
            [ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.WebP], 1200, 400, 4000, Square: false, RejectAlpha: false, MaxBytes: MediaStore.MaxBytes,
            DefaultPath: "", DefaultSize: "", MaxPixels: 12_000_000),
        new(OgDefault, "صورة مشاركة المنتدى (og:image)",
            "تُستخدم عند مشاركة صفحات المنتدى التي ليست لها صورة خاصة بها (قبل صورة المشاركة العامة للموقع). JPG أو PNG، من 1200×630 بكسل (بنسبة 1.91:1) حتى 4000 بكسل لأطول ضلع و12 ميغابكسل، حتى 5 MB (يُفضَّل أقل من 300 KB).",
            [ImageFormat.Jpeg, ImageFormat.Png], 1200, 630, 4000, Square: false, RejectAlpha: false, MaxBytes: MediaStore.MaxBytes,
            DefaultPath: "", DefaultSize: "", HasAlt: true, MaxPixels: 12_000_000),
    ];

    public static BrandImage? FindImage(string? key) => Images.FirstOrDefault(i => i.Key == key);

    /// <summary>Every colour and font key (what «استعادة الافتراضي» removes).</summary>
    public static IEnumerable<string> SettingKeys => Colors.Select(c => c.Key).Append(FontArKey).Append(FontEnKey);

    // ── Scope ──

    /// <summary>
    /// True for the forum hub and every page under it (events, speakers, partners, registration — and their detail
    /// pages, whose shell key is their list page). Decided from the route table only, never from the request.
    /// </summary>
    public static bool IsForumPage(string? key) =>
        key is not null && SiteRoutes.Chain(key).Any(p => p.Key == PageKeys.Forum);

    // ── Reading (every value is validated again here: a bad row never reaches a page) ──

    /// <summary>The stored colour as lower-case #rrggbb, or null when the key is empty or invalid.</summary>
    public static string? StoredColor(IReadOnlyDictionary<string, TextPair> content, string key) =>
        content.GetValueOrDefault(key)?.Ar?.Trim() is { Length: 7 } v && BrandImages.HexColor().IsMatch(v) ? v.ToLowerInvariant() : null;

    /// <summary>The stored font, or null when the key is empty or not in <see cref="Fonts"/>.</summary>
    public static ForumFont? StoredFont(IReadOnlyDictionary<string, TextPair> content, string key) =>
        FindFont(content.GetValueOrDefault(key)?.Ar?.Trim());

    /// <summary>The colour each key renders with: the given value (null = not set), else the default.</summary>
    public static IReadOnlyDictionary<string, string> Effective(IReadOnlyDictionary<string, string?> values)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal) { [White] = White, [HeaderNavy] = HeaderNavy };
        foreach (var c in Colors)
            result[c.Key] = values.GetValueOrDefault(c.Key) ?? c.Default;
        return result;
    }

    /// <summary>The pairs below their minimum for these values (null = the default).</summary>
    public static IReadOnlyList<ContrastFailure> ContrastFailures(IReadOnlyDictionary<string, string?> values)
    {
        var eff = Effective(values);
        var failures = new List<ContrastFailure>();
        foreach (var pair in Pairs)
        {
            var (fg, bg) = (eff[pair.Foreground], eff[pair.Background]);
            var ratio = Contrast(fg, bg);
            if (ratio < pair.Min) failures.Add(new ContrastFailure(pair, fg, bg, ratio));
        }
        return failures;
    }

    /// <summary>WCAG 2 contrast ratio of two #rrggbb colours (1 … 21).</summary>
    public static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        var (r, g, b) = Rgb(hex);
        static double Channel(int v)
        {
            var c = v / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }

    /// <summary>The channels of a validated #rrggbb colour.</summary>
    public static (int R, int G, int B) Rgb(string hex) => (
        int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    private static string RgbList(string hex) => Rgb(hex) is var (r, g, b) ? $"{r}, {g}, {b}" : "";

    /// <summary>
    /// The generated part of /forum-theme.css: one rule on <c>body.theme-forum</c> with the admin's colours (each as
    /// #rrggbb and as "r, g, b"), the font of each language, and the hero background image under the red overlay.
    /// Empty when nothing is set. Built only from values re-validated here (hex, font key, media key + format); nothing
    /// else from the database is written.
    /// </summary>
    public static string GeneratedCss(SiteSnapshot? s)
    {
        if (s is null) return "";
        var css = new StringBuilder();
        var vars = new StringBuilder();
        foreach (var c in Colors)
        {
            if (StoredColor(s.Content, c.Key) is not { } v) continue;
            vars.Append(c.Variable).Append(':').Append(v).Append(';')
                .Append(c.Variable).Append("-rgb:").Append(RgbList(v)).Append(';');
        }
        if (vars.Length > 0) css.Append("body.theme-forum{").Append(vars).Append("}\n");

        if (StoredFont(s.Content, FontArKey) is { } ar)
            css.Append("html[lang=\"ar\"] body.theme-forum{--font-family:").Append(ar.Stack).Append(";}\n");
        if (StoredFont(s.Content, FontEnKey) is { } en)
            css.Append("html[lang=\"en\"] body.theme-forum{--font-family:").Append(en.Stack).Append(";}\n");

        if (HeroUrl(s) is { } hero)
            css.Append("body.theme-forum .page-hero{background-image:")
                .Append("linear-gradient(135deg, rgba(var(--forum-hero-start-rgb), 0.88) 0%, rgba(var(--forum-primary-deep-rgb), 0.94) 100%),")
                .Append("url(\"").Append(hero).Append("\");background-size:cover;background-position:center;}\n");
        return css.ToString();
    }

    /// <summary>The hero image's /media URL when its stored key and format are valid, else null.</summary>
    private static string? HeroUrl(SiteSnapshot s) =>
        s.Image(HeroImage) is { } m && MediaStore.KeyPattern().IsMatch(m.FileKey) && ImageInspector.FromName(m.Format) is not null
            ? m.Url : null;
}
